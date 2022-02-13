using Microsoft.Extensions.Logging;
using MovieMoverCore.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace MovieMoverCore.Models
{
    public interface IFileOperation : ICloneable
    {
        uint ID { get; set; }
        FileOperationState CurrentState { get; set; }
        DateTime? Finished { get; set; }
        string ErrorMessage { get; set; }

        void PerformOperation(IJDownloader _jDownloader, IPlex _plex, ILogger<FileOperationsWorker> _logger);
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

        public void PerformOperation(IJDownloader _jDownloader, IPlex _plex, ILogger<FileOperationsWorker> _logger)
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
        
        public void PerformOperation(IJDownloader _jDownloader, IPlex _plex, ILogger<FileOperationsWorker> _logger)
        {
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
                _jDownloader.RemoveDownloadPackageAsync(Path.GetFileName(Source)).FireForget(_logger);
            });
        }
    }
}
