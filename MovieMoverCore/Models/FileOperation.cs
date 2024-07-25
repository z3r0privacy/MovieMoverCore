using Microsoft.Extensions.Logging;
using MovieMoverCore.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

namespace MovieMoverCore.Models
{
    public interface IFileOperation : ICloneable
    {
        uint ID { get; set; }
        FileOperationState CurrentState { get; set; }
        DateTime? Finished { get; set; }
        string ErrorMessage { get; set; }

        void PerformOperation(IJDownloader _jDownloader, IPlex _plex, ILogger<FileOperationsWorker> _logger, ISettings _settings);
    }

    public class FileDeleteOperation : IFileOperation
    {
        public uint ID { get; set; }
        public FileOperationState CurrentState { get; set; }
        public DateTime? Finished { get; set; }
        public string ErrorMessage { get; set; }
        public string Source { get; set; }

        public FileDeleteOperation Clone()
        {
            return new FileDeleteOperation()
            {
                ID = ID,
                CurrentState = CurrentState,
                Finished = Finished,
                ErrorMessage = ErrorMessage,
                Source = Source
            };
        }

        public override string ToString()
        {
            return $"Deleting {Source}";
        }

        object ICloneable.Clone()
        {
            return Clone();
        }

        public void PerformOperation(IJDownloader _jDownloader, IPlex _plex, ILogger<FileOperationsWorker> _logger, ISettings _settings)
        {
            if (Directory.Exists(Source))
            {
                Directory.Delete(Source, true);
            } else if (File.Exists(Source))
            {
                File.Delete(Source);
            } else
            {
                throw new FileNotFoundException("Could not find the given source file or directory");
            }

            Task.Run(() =>
            {
                _jDownloader.RemoveDownloadPackageAsync(Path.GetFileName(Source)).FireForget(_logger);
            });
        }
    }

    public class FileMoveOperation : IFileOperation
    {
        public uint ID { get; set; }
        public FileOperationState CurrentState { get; set; }
        public string Source { get; set; }
        public string Destination { get; set; }
        public PlexSection PlexSection { get; set; }
        public DateTime? Finished { get; set; }
        public string ErrorMessage { get; set; }
        public string Name { get; set; }

        public FileMoveOperation Clone()
        {
            return new FileMoveOperation
            {
                CurrentState = CurrentState,
                Destination = Destination,
                ErrorMessage = ErrorMessage,
                Finished = Finished,
                ID = ID,
                Name = Name,
                PlexSection = PlexSection,
                Source = Source
            };
        }

        object ICloneable.Clone()
        {
            return Clone();
        }

        public override string ToString()
        {
            return $"Moving {Source}";
        }

        private static void MoveDir(string source, string destination)
        {
            if (!Directory.Exists(destination))
            {
                Directory.Move(source, destination);
                return;
            }

            foreach (var srcFile in Directory.GetFiles(source))
            {
                var dstFile = Path.Combine(destination, Path.GetFileName(srcFile));
                File.Move(srcFile, dstFile);
            }
            foreach (var srcDir in Directory.GetDirectories(source))
            {
                // GetfileName also returns the name of a directory
                var dstDir = Path.Combine(destination, Path.GetFileName(srcDir));
                MoveDir(srcDir, dstDir);
            }
            Directory.Delete(source);
        }
        
        private bool CanBeRenamed(string path, IList<(string rgx, string pattern)> renamings, out string newPath)
        {
            var name = Path.GetFileName(path);
            foreach ((var rgx, var pattern) in renamings)
            {
                var match = Regex.Match(name, rgx);
                if (match.Success)
                {
                    newPath = pattern;
                    var failed = false;
                    while (newPath.IndexOf("[[") >= 0)
                    {
                        var start = newPath.IndexOf("[[");
                        var end = newPath.IndexOf("]]");
                        var val = match.Groups[newPath.Substring(start+2, end-start-2)].Value;
                        if (string.IsNullOrEmpty(val))
                        {
                            failed = true;
                            break;
                        }
                        newPath = string.Concat(newPath.AsSpan(0, start), val, newPath.AsSpan(end + 2));
                    }
                    if (failed)
                    {
                        newPath = null;
                        continue;
                    }
                    newPath = newPath.Replace('.', ' ');
                    if (File.Exists(path))
                    {
                        var ending = path.Split('.')[^1];
                        newPath += "." + ending;
                    }
                    newPath = Path.Combine(Path.GetDirectoryName(path), newPath);
                    return true;
                }
            }
            newPath = null;
            return false;
        }
        private void ApplyRenamingsDir(string currPath, IList<(string rgx, string pattern)> renamings)
        {
            foreach (var entry in Directory.GetFileSystemEntries(currPath))
            {
                if (Directory.Exists(entry))
                {
                    ApplyRenamingsDir(entry, renamings);
                }
                if (CanBeRenamed(entry, renamings, out var newPath))
                {
                    if (Directory.Exists(entry))
                    {
                        Directory.Move(entry, newPath);
                    } else
                    {
                        File.Move(entry, newPath);
                    }
                }
            }
        }
        private void ApplyRenamings(IList<(string rgx, string pattern)> renamings)
        {
            var srcRenameable = CanBeRenamed(Source, renamings, out var newSource);
            if (Directory.Exists(Source))
            {
                ApplyRenamingsDir(Source, renamings);
                if (srcRenameable)
                {
                    Directory.Move(Source, newSource);
                }
            } else
            {
                if (srcRenameable)
                {
                    File.Move(Source, newSource);
                }
            }
            if (srcRenameable)
            {
                Source = newSource;
            }
            if (CanBeRenamed(Destination, renamings, out var newDest))
            {
                Destination = newDest;
            }
        }

        public void PerformOperation(IJDownloader _jDownloader, IPlex _plex, ILogger<FileOperationsWorker> _logger, ISettings _settings)
        {
            var originalSource = Source;
            try
            {
                ApplyRenamings(_settings.Files_RenameSchemes);
            } catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to apply renames");
            }
            if (Directory.Exists(Source))
            {
                var parentDir = Path.GetDirectoryName(Destination);
                if (!Directory.Exists(parentDir))
                {
                    Directory.CreateDirectory(parentDir);
                }
                MoveDir(Source, Destination);
            }
            else if (File.Exists(Source))
            {
                File.Move(Source, Destination);
            }
            else
            {
                throw new FileNotFoundException("Could not find the source file", Source);
            }

            Task.Run(() =>
            {
                _plex.RefreshSectionAsync(PlexSection, Destination).FireForget(_logger);
                _jDownloader.RemoveDownloadPackageAsync(Path.GetFileName(originalSource)).FireForget(_logger);
            });
        }
    }
}
