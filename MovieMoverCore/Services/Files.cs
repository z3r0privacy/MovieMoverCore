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
    public interface IFileMoveWorker
    {
        FileMoveOperation QueueMoveOperation(string name, string source, string destination, PlexSection plexSection);
        List<FileMoveOperation> QueryStates();
        FileMoveState QueryState(FileMoveOperation fmo);
    }

    public interface IFileMover
    {
        FileMoveOperation CreateMoviesMoveOperation(string downloadName);
        FileMoveOperation CreateSeriesMoveOperation(string downloadName, Series series, int? season);
        List<string> GetDownloadEntries();
        List<string> GetSeriesEntries();
        Task<bool> AddSubtitle(string file, Series series);
        bool ValidateSeriesPath(Series series, bool isNewEntry = false);
    }

    public class FileMoveWorker : IFileMoveWorker
    {
        // uses a concurrentqueue in implementation
        private BlockingCollection<FileMoveOperation> _operationsPending;
        private List<FileMoveOperation> _allOperations;
        private ReaderWriterLockSlim _allOperationsRWLock;
        private Task _moveTask;
        private IPlex _plex;
        private ISettings _settings;
        private ILogger<FileMoveWorker> _logger;
        private uint _ID;

        public FileMoveWorker(IPlex plex, ISettings settings, ILogger<FileMoveWorker> logger)
        {
            _plex = plex;
            _settings = settings;
            _logger = logger;
            _operationsPending = new BlockingCollection<FileMoveOperation>();
            _allOperations = new List<FileMoveOperation>();
            _allOperationsRWLock = new ReaderWriterLockSlim();
            _moveTask = Task.Run(MoveWorker);
            _ID = 0;
        }

        public List<FileMoveOperation> QueryStates()
        {
            _allOperationsRWLock.EnterWriteLock(); 
            try
            {
                var toRemove = _allOperations.Where(fmo => fmo.CurrentState == FileMoveState.Success && fmo.Finished.HasValue && fmo.Finished.Value.AddMinutes(_settings.Files_KeepSuccess) < DateTime.Now);
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
                return _allOperations.Select(fmo => fmo.Clone()).ToList();
            } finally
            {
                _allOperationsRWLock.ExitReadLock();
            }
        }

        public FileMoveOperation QueueMoveOperation(string name, string source, string destination, PlexSection plexSection)
        {
            var fmo = new FileMoveOperation
            {
                CurrentState = FileMoveState.Queued,
                Destination = destination,
                ErrorMessage = null,
                Finished = null,
                Name = name,
                PlexSection = plexSection,
                Source = source
            };

            _allOperationsRWLock.ExitWriteLock();
            try
            {
                fmo.ID = _ID;
                ++_ID;
                _allOperations.Add(fmo);
            } finally
            {
                _allOperationsRWLock.ExitWriteLock();
            }
            _operationsPending.Add(fmo);

            _logger.LogDebug("Queued new FileMoveOperation named {0}", name);
            
            return fmo.Clone();
        }

        private void MoveWorker()
        {
            try
            {
                while (!_operationsPending.IsCompleted)
                {
                    var moveOp = _operationsPending.Take();
                    moveOp.CurrentState = FileMoveState.Moving;
                    try
                    {
                        if (Directory.Exists(moveOp.Source))
                        {
                            Directory.Move(moveOp.Source, moveOp.Destination);
                        }
                        else if (File.Exists(moveOp.Source))
                        {
                            File.Move(moveOp.Source, moveOp.Destination);
                        }
                        else
                        {
                            throw new FileNotFoundException("Could not find the source file", moveOp.Source);
                        }

                        _plex.RefreshSection(moveOp.PlexSection, moveOp.Destination);

                        moveOp.CurrentState = FileMoveState.Success;
                    }
                    catch (Exception ex)
                    {
                        moveOp.ErrorMessage = ex.Message;
                        moveOp.CurrentState = FileMoveState.Failed;
                    }
                    moveOp.Finished = DateTime.Now;
                }
            } catch (Exception ex)
            {
                _logger.LogError(ex, "File mover thread crashed due to an exception - Files will no longer be moved.");
                throw;
            }
        }

        public FileMoveState QueryState(FileMoveOperation fmo)
        {
            _allOperationsRWLock.EnterReadLock();
            try
            {
                var op = _allOperations.FirstOrDefault(o => o.ID == fmo.ID);
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
        private IFileMoveWorker _fileMoveWorker;
        private ISettings _settings;
        private IPlex _plex;

        public FileMover(IFileMoveWorker fileMoveWorker, ILogger<FileMover> logger, ISettings settings, IPlex plex)
        {
            _logger = logger;
            _fileMoveWorker = fileMoveWorker;
            _settings = settings;
            _plex = plex;
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

        public async Task<bool> AddSubtitle(string file, Series series)
        {
            if (!GetEpisodeFromSrt(file, out var season, out var episode))
            {
                throw new ArgumentException("No season and episode information could be extracted from the given filename.");
            }

            var videoPath = await _plex.GetFilePathOfEpisode(series, season, episode);
            if (videoPath == null)
            {
                var dir = Path.Combine(_settings.Files_SeriesPath, series.DirectoryName, $"S{season:00}");
                if (Directory.Exists(dir))
                {
                    videoPath = FindEpisodeInDirectory(dir, season, episode);
                }
            }

            if (videoPath == null)
            {
                throw new VideoDependencyNotFoundException(series, season, episode);
            }

            var target = Path.GetFileNameWithoutExtension(videoPath) + ".srt";
            try
            {
                File.Copy(file, target);
                return true;
            } catch (Exception ex)
            {
                _logger.LogWarning(ex, $"Could not copy {file} to {target}");
                return false;
            }
            finally
            {
                File.Delete(file);
            }
        }

        public FileMoveOperation CreateMoviesMoveOperation(string downloadName)
        {
            if (!IsDownloadNameLegal(downloadName, out var source))
            {
                throw new ArgumentException("Illegal download name provided. File or Folder could not be found");
            }
            return _fileMoveWorker.QueueMoveOperation(downloadName, source,
                Path.Combine(_settings.Files_MoviesPath, downloadName), PlexSection.Movies);
        }

        public FileMoveOperation CreateSeriesMoveOperation(string downloadName, Series series, int? season)
        {
            var dest = Path.Combine(_settings.Files_SeriesPath, series.DirectoryName);
            if (season.HasValue)
            {
                dest = Path.Combine(dest, "S" + season.Value.ToString("00"));
            }
            return _fileMoveWorker.QueueMoveOperation(downloadName, Path.Combine(_settings.Files_DownloadsPath, downloadName),
                dest, PlexSection.Series);
        }

        public List<string> GetDownloadEntries()
        {
            return Directory.EnumerateFileSystemEntries(_settings.Files_DownloadsPath).ToList();
        }

        private bool IsDownloadNameLegal(string name, out string fullPath)
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
