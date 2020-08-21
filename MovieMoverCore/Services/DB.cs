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

        // extra Series
        

        // CRUD ToDownload
        //TBD
    }

    public class DB : IDatabase
    {
        private readonly ILogger<DB> _logger;
        private List<Series> _series;
        private ReaderWriterLockSlim _seriesRwLock;
        private static string _dbDirectory = "/appdata";
        private static string _dbFile = Path.Combine(_dbDirectory, "db.json");

        public DB(ILogger<DB> logger, IHostApplicationLifetime hostApplicationLifetime)
        {
            _logger = logger;

            if (!Directory.Exists(_dbDirectory))
            {
                _logger.LogError($"{_dbDirectory} is not mounted - database cannot be read or written, exiting...");
                hostApplicationLifetime.StopApplication();
            }

            _seriesRwLock = new ReaderWriterLockSlim();

            if (File.Exists(_dbFile))
            {
                var jsonData = File.ReadAllText(_dbFile);
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
                var nextId = _series.Max(s => s.Id) + 1;
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

        public List<Series> GetSeries()
        {
            _seriesRwLock.EnterReadLock();
            try
            {
                return _series.Select(s => s.Clone()).ToList();
            } finally
            {
                _seriesRwLock.ExitReadLock();
            }
        }

        public Series GetSeries(int id)
        {
            _seriesRwLock.EnterReadLock();
            try
            {
                var tbr = _series.FirstOrDefault(s => s.Id == id);
                if (tbr == null)
                {
                    throw new KeyNotFoundException("Could not find a series with the given id.");
                }
                return tbr.Clone();
            } finally
            {
                _seriesRwLock.ExitReadLock();
            }
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
    }
}
