using Microsoft.AspNetCore.Mvc.TagHelpers;
using Microsoft.Extensions.Logging;
using MovieMoverCore.Models;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Security.Permissions;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace MovieMoverCore.Services
{
    public interface IFileOperationsWorker
    {
        FileDeleteOperation QueueDeleteOperation(string source);
        FileMoveOperation QueueMoveOperation(string name, string source, string destination, MultimediaType plexSection);
        List<IFileOperation> QueryStates();
        FileOperationState QueryState(IFileOperation fo);
        bool DismissState(int id);
    }

    public interface IFileMover
    {
        FileMoveOperation CreateMoviesMoveOperation(string downloadName);
        FileMoveOperation CreateSeriesMoveOperation(string downloadName, Series series, int? season);
        List<string> GetDownloadEntries();
        List<string> GetSeriesEntries();
        Task<bool> AddSubtitleAsync(string fileName, byte[] content, Series series);
        bool ValidateSeriesPath(Series series, bool isNewEntry = false);
        bool IsDownloadNameLegal(string name, out string fullPath);
    }

    public class FileOperationsWorker : IFileOperationsWorker
    {
        // uses a concurrentqueue in implementation
        private BlockingCollection<IFileOperation> _operationsPending;
        private List<IFileOperation> _allOperations;
        private ReaderWriterLockSlim _allOperationsRWLock;
        private Task _moveTask;
        private IMultimediaMetadataProvider _metadataProvider;
        private IMultimediaServerManagerCollection _multimediaManagers;
        private IJDownloader _jDownloader;
        private ISettings _settings;
        private ILogger<FileOperationsWorker> _logger;
        private uint _ID;

        public FileOperationsWorker(IMultimediaMetadataProvider metadataProvider, IMultimediaServerManagerCollection multimediaManagers, IJDownloader jDownloader, ISettings settings, ILogger<FileOperationsWorker> logger)
        {
            _metadataProvider = metadataProvider;
            _multimediaManagers = multimediaManagers;
            _jDownloader = jDownloader;
            _settings = settings;
            _logger = logger;
            _operationsPending = new BlockingCollection<IFileOperation>();
            _allOperations = new List<IFileOperation>();
            _allOperationsRWLock = new ReaderWriterLockSlim();
            _moveTask = Task.Run(Worker);
            _ID = 0;
        }

        public bool DismissState(int id)
        {
            _allOperationsRWLock.EnterWriteLock();
            try
            {
                var el = _allOperations.FirstOrDefault(fmo => fmo.ID == id);
                if (el == null || !el.Finished.HasValue)
                {
                    return false;
                }
                _allOperations.Remove(el);
                return true;
            } finally
            {
                _allOperationsRWLock.ExitWriteLock();
            }
        }

        public List<IFileOperation> QueryStates()
        {
            _allOperationsRWLock.EnterWriteLock(); 
            try
            {
                var toRemove = _allOperations.Where(fo => fo.CurrentState == FileOperationState.Success && fo.Finished.HasValue && fo.Finished.Value.AddMinutes(_settings.Files_KeepSuccess) < DateTime.Now).ToList();
                if (toRemove.Any())
                {
                    foreach (var rem in toRemove)
                    {
                        _allOperations.Remove(rem);
                    }
                    _logger.LogDebug("Removed {0} successfully finisehd item(s) from file move operations list", toRemove.Count());
                }
            } finally
            {
                _allOperationsRWLock.ExitWriteLock();
            }

            _allOperationsRWLock.EnterReadLock();
            try
            {
                return _allOperations.Select(fo => (IFileOperation)fo.Clone()).ToList();
            } finally
            {
                _allOperationsRWLock.ExitReadLock();
            }
        }

        private void AddToAllOperationsList(IFileOperation fo)
        {
            _allOperationsRWLock.EnterWriteLock();
            try
            {
                fo.ID = _ID;
                ++_ID;
                _allOperations.Add(fo);
            }
            finally
            {
                _allOperationsRWLock.ExitWriteLock();
            }
        }

        public FileDeleteOperation QueueDeleteOperation(string source)
        {
            var fdo = new FileDeleteOperation
            {
                CurrentState = FileOperationState.Queued,
                ErrorMessage = null,
                Finished = null,
                Source = source
            };

            AddToAllOperationsList(fdo);
            _operationsPending.Add(fdo);
            _logger.LogDebug("Queued new FileDeleteOperation with source {0}", source);

            return fdo.Clone();
        }

        public FileMoveOperation QueueMoveOperation(string name, string source, string destination, MultimediaType plexSection)
        {
            var fmo = new FileMoveOperation
            {
                CurrentState = FileOperationState.Queued,
                Destination = destination,
                ErrorMessage = null,
                Finished = null,
                Name = name,
                MultimediaType = plexSection,
                Source = source
            };

            // todo: test from here
            AddToAllOperationsList(fmo);

            _operationsPending.Add(fmo);

            _logger.LogDebug("Queued new FileMoveOperation named {0}", name);
            
            return fmo.Clone();
        }

        

        private void Worker()
        {
            try
            {
                while (!_operationsPending.IsCompleted)
                {
                    var moveOp = _operationsPending.Take();
                    moveOp.CurrentState = FileOperationState.InOperation;
                    try
                    {
                        moveOp.PerformOperation(_jDownloader, _multimediaManagers, _logger, _settings);
                        moveOp.CurrentState = FileOperationState.Success;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Move operation failed");
                        moveOp.ErrorMessage = ex.Message;
                        moveOp.CurrentState = FileOperationState.Failed;
                    }
                    moveOp.Finished = DateTime.Now;
                }
            } catch (Exception ex)
            {
                _logger.LogError(ex, "File mover thread crashed due to an exception - Files will no longer be moved.");
                throw;
            }
        }

        public FileOperationState QueryState(IFileOperation fo)
        {
            _allOperationsRWLock.EnterReadLock();
            try
            {
                var op = _allOperations.FirstOrDefault(o => o.ID == fo.ID);
                if (op == null)
                {
                    return op.CurrentState;
                }
                throw new KeyNotFoundException("The given ID does not exist in the operations table");
            } finally {
                _allOperationsRWLock.ExitReadLock();
            }
        }
    }

    public class FileMover : IFileMover
    {
        private ILogger<FileMover> _logger;
        private IFileOperationsWorker _fileMoveWorker;
        private ISettings _settings;
        private IMultimediaMetadataProvider _metadataProvider;
        private IMultimediaServerManagerCollection _multimediaManagers;

        public FileMover(IFileOperationsWorker fileMoveWorker, ILogger<FileMover> logger, ISettings settings, IMultimediaMetadataProvider metadataProvider, IMultimediaServerManagerCollection multimediaManagers)
        {
            _logger = logger;
            _fileMoveWorker = fileMoveWorker;
            _settings = settings;
            _metadataProvider = metadataProvider;
            _multimediaManagers = multimediaManagers;
        }

        private bool GetEpisodeFromSrt(string name, out int season, out int episode)
        {
            var patterns = new[]
            {
                @"(?i)s(\d{1,2})e(\d{1,2})",
                @"(?i)(\d{1,2})x(\d{1,2})"
            };

            foreach (var pattern in patterns)
            {
                var regex = new Regex(pattern);
                var m = regex.Match(name);
                if (m.Success && m.Groups.Count == 3)
                {
                    season = int.Parse(m.Groups[1].Value);
                    episode = int.Parse(m.Groups[2].Value);
                    return true;
                }
            }

            _logger.LogWarning("Could not extract season and episode from filename {name}", name);
            season = -1;
            episode = -1;
            return false;
        }

        private string FindEpisodeInDirectory(string directory, int season, int episode)
        {
            foreach (var f in Directory.EnumerateFiles(directory))
            {
                if (Path.GetFileName(f).Contains($"S{season:00}E{episode:00}", StringComparison.OrdinalIgnoreCase))
                {
                    return f;
                }
            }
            foreach (var d in Directory.EnumerateDirectories(directory))
            {
                var res = FindEpisodeInDirectory(d, season, episode);
                if (res != null)
                {
                    return res;
                }
            }

            return null;
        }

        public async Task<bool> AddSubtitleAsync(string fileName, byte[] content, Series series)
        {

            if (!GetEpisodeFromSrt(fileName, out var season, out var episode))
            {
                // Todo: find better solution for returning errors
                throw new ArgumentException("No season and episode information could be extracted from the given filename.");
            }

            var videoPath = await _metadataProvider.GetFilePathOfEpisode(series, season, episode);
            if (videoPath == null)
            {
                var dir = Path.Combine(_settings.Files_SeriesPath, series.DirectoryName, $"S{season:00}");
                if (Directory.Exists(dir))
                {
                    videoPath = FindEpisodeInDirectory(dir, season, episode);
                }
            }

            // calculate root from mover container
            var commonBase = Environment.GetEnvironmentVariable("FILES_Series");
            var startCommonPath = videoPath.IndexOf(commonBase);
            if (startCommonPath >= 0)
            {
                videoPath = Path.Combine(_settings.Files_SeriesPath, videoPath[(startCommonPath + commonBase.Length)..].TrimStart('/'));
            }

            if (videoPath == null)
            {
                throw new VideoDependencyNotFoundException(series, season, episode);
            }

            var target = Path.Combine(Path.GetDirectoryName(videoPath), Path.GetFileNameWithoutExtension(videoPath) + ".srt");
            try
            {
                await File.WriteAllBytesAsync(target, content);
                _multimediaManagers.Managers.DoForEach(mgr => mgr.InformUpdatedFilesAsync(MultimediaType.Series, Path.GetDirectoryName(target)).FireForget(_logger));
                // _metadataProvider.RefreshSectionAsync(MultimediaType.Series, Path.GetDirectoryName(target)).FireForget(_logger);
                return true;
            } catch (Exception ex)
            {
                _logger.LogWarning(ex, $"Could not write content to {target}");
                throw;
            }
        }

        public FileMoveOperation CreateMoviesMoveOperation(string downloadName)
        {
            if (!IsDownloadNameLegal(downloadName, out var source))
            {
                throw new ArgumentException("Illegal download name provided. File or Folder could not be found");
            }
            return _fileMoveWorker.QueueMoveOperation(downloadName, source,
                Path.Combine(_settings.Files_MoviesPath, downloadName), MultimediaType.Movies);
        }

        public FileMoveOperation CreateSeriesMoveOperation(string downloadName, Series series, int? season)
        {
            if (!IsDownloadNameLegal(downloadName, out var source))
            {
                throw new ArgumentException("Illegal download name provided. File or Folder could not be found");
            }
            var dest = Path.Combine(_settings.Files_SeriesPath, series.DirectoryName);
            if (season.HasValue)
            {
                dest = Path.Combine(dest, "S" + season.Value.ToString("00"));
            }
            return _fileMoveWorker.QueueMoveOperation(downloadName, source,
                Path.Combine(dest, downloadName), MultimediaType.Series);
        }

        public List<string> GetDownloadEntries()
        {
            return Directory.EnumerateFileSystemEntries(_settings.Files_DownloadsPath).ToList();
        }

        public bool IsDownloadNameLegal(string name, out string fullPath)
        {
            foreach (var entry in GetDownloadEntries())
            {
                var ename = Path.GetFileName(entry);
                if (ename.Equals(name, StringComparison.OrdinalIgnoreCase))
                {
                    fullPath = entry;
                    return true;
                }
            }
            fullPath = null;
            return false;
        }

        public bool ValidateSeriesPath(Series series, bool isNewEntry)
        {
            foreach (var d in GetSeriesEntries())
            {
                if (string.Equals(d, series.DirectoryName, StringComparison.CurrentCultureIgnoreCase))
                {
                    return true;
                }
            }

            if (!isNewEntry)
            {
                return false;
            }

            var pathToCreate = Path.GetFullPath(Path.Combine(_settings.Files_SeriesPath, series.DirectoryName));
            if (!pathToCreate.StartsWith(_settings.Files_SeriesPath + Path.DirectorySeparatorChar))
            {
                _logger.LogWarning($"Tried to create a directory in the path '{pathToCreate}' using argument '{series.DirectoryName}'");
                return false;
            }

            if (pathToCreate.Count(c => c == Path.DirectorySeparatorChar) - _settings.Files_SeriesPath.Count(c => c == Path.DirectorySeparatorChar) != 1)
            {
                _logger.LogWarning($"Tried to create a directory in the path '{pathToCreate}' using argument '{series.DirectoryName}'");
                return false;
            }

            Directory.CreateDirectory(pathToCreate);
            _logger.LogInformation($"Created series directory {pathToCreate}");

            return true;
        }

        public List<string> GetSeriesEntries()
        {
            return Directory.GetDirectories(_settings.Files_SeriesPath).Select(p => Path.GetFileName(p)).ToList();
//            return Directory.EnumerateDirectories(_settings.Files_SeriesPath).ToList();
        }
    }
}
