using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MovieMoverCore.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace MovieMoverCore.Services
{
    public interface IDatabase
    {
        // CRUD Series
        Series AddSeries(Series series);
        List<Series> GetSeries();
        Series GetSeries(int id);
        bool UpdateSeries(Series series);
        bool DeleteSeries(Series series);
        Task SaveSeriesChangesAsync();

        // extra Series
        bool SeriesExists(int id);
        List<Series> GetSeries(Func<Series, bool> selector);

        // CRUD ToDownload
        //TBD
    }

    public class DB : IDatabase
    {
        private readonly ILogger<DB> _logger;
        private List<Series> _series;
        private ReaderWriterLockSlim _seriesRwLock;
        private string _dbFileSeries; // = Path.Combine(_dbDirectory, "db.json");

        public DB(ILogger<DB> logger, ISettings settings, IHostApplicationLifetime hostApplicationLifetime)
        {
            _logger = logger;

            var _dbDirectory = settings.AppDataDirectory;
            _dbFileSeries = Path.Combine(_dbDirectory, "db.json");

            if (!Directory.Exists(_dbDirectory))
            {
                _logger.LogError($"{_dbDirectory} is not mounted - database cannot be read or written, exiting...");
                hostApplicationLifetime.StopApplication();
            }

            _seriesRwLock = new ReaderWriterLockSlim();

            if (File.Exists(_dbFileSeries))
            {
                var jsonData = File.ReadAllText(_dbFileSeries);
                _series = JsonSerializer.Deserialize<List<Series>>(jsonData);
            } else
            {
                _series = new List<Series>();
            }
        }

        public Series AddSeries(Series series)
        {
            _seriesRwLock.EnterWriteLock();
            try
            {
                var nextId = (_series.Any() ? _series.Max(s => s.Id) : 0 ) + 1;
                series.Id = nextId;
                _series.Add(series.Clone());
            } finally
            {
                _seriesRwLock.ExitWriteLock();
            }
            return series;
        }

        public bool DeleteSeries(Series series)
        {
            _seriesRwLock.EnterWriteLock();
            try
            {
                var tbd = _series.FirstOrDefault(s => s.Id == series.Id);
                if (tbd == null)
                {
                    throw new KeyNotFoundException("The requested series id to be deleted was not found");
                }
                _series.Remove(tbd);
                return true;
            } finally
            {
                _seriesRwLock.ExitWriteLock();
            }
        }

        public List<Series> GetSeries(Func<Series, bool> selector)
        {
            _seriesRwLock.EnterReadLock();
            try
            {
                return _series.Where(selector).Select(s => s.Clone()).ToList();
            }
            finally
            {
                _seriesRwLock.ExitReadLock();
            }
        }

        public List<Series> GetSeries()
        {
            return GetSeries(s => true);
        }

        public Series GetSeries(int id)
        {
            return GetSeries(s => s.Id == id).FirstOrDefault();
        }

        public bool UpdateSeries(Series series)
        {
            _seriesRwLock.EnterWriteLock();
            try
            {
                var tbu = _series.FirstOrDefault(s => s.Id == series.Id);
                if (tbu == null)
                {
                    throw new KeyNotFoundException("The given series to be updated was not found.");
                }
                tbu.Apply(series);
                return true;
            } finally
            {
                _seriesRwLock.ExitWriteLock();
            }
        }

        public async Task SaveSeriesChangesAsync()
        {
            await Task.Run(() =>
            {
                lock (_dbFileSeries)
                {
                    _seriesRwLock.EnterReadLock();
                    try
                    {
                        var data = JsonSerializer.Serialize(_series);
                        File.WriteAllText(_dbFileSeries, data);
                    }
                    finally
                    {
                        _seriesRwLock.ExitReadLock();
                    }
                }
            });
        }

        public bool SeriesExists(int id)
        {
            _seriesRwLock.EnterReadLock();
            try
            {
                return _series.Any(s => s.Id == id);
            } finally
            {
                _seriesRwLock.ExitReadLock();
            }
        }
    }
}
