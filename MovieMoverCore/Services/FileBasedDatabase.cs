using Microsoft.IdentityModel.Tokens;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;

namespace MovieMoverCore.Services
{ 
    public interface IFileBasedDatabaseItem
    {
        public int Id { get; set;  }
        public IFileBasedDatabaseItem Clone();
    }
    public interface IFileBasedDatabase<T> : IEnumerable<T>
    {
        public T this[int idx] { get; }
        public T Add(T data);
        public void Update(T data);
        public bool Remove(T data);
        public bool Remove(int id);
    }

    public class FileBasedDatabase<T> : IFileBasedDatabase<T> where T : IFileBasedDatabaseItem
    {
        private readonly string _filePath;

        private List<T> _data;
        private ReaderWriterLockSlim _rwlock;

        public T this[int idx]
        {
            get {
                _rwlock.EnterReadLock();
                try
                {
                    return (T)_data.FirstOrDefault(t => t.Id == idx)?.Clone();
                } finally
                {
                    _rwlock.ExitReadLock();
                }
            }
        }

        public FileBasedDatabase(ISettings settings, string filePath)
        {
            _filePath = Path.Join(settings.AppDataDirectory, filePath);
            _rwlock = new();

            if (!File.Exists(_filePath))
            {
                File.Create(_filePath);
            }

            using StreamReader r = new(_filePath);
            string json = r.ReadToEnd();
            if (json.IsNullOrEmpty())
            {
                json = "[]";
            }
            _data = JsonSerializer.Deserialize<List<T>>(json);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            _rwlock.EnterReadLock();
            try
            {
                return _data.ToList().GetEnumerator();
            }
            finally
            {
                _rwlock.ExitReadLock();
            }
        }
        public IEnumerator<T> GetEnumerator()
        {
            _rwlock.EnterReadLock();
            try
            {
                return _data.ToList().GetEnumerator();
            }
            finally
            {
                _rwlock.ExitReadLock();
            }
        }

        public T Add(T data)
        {
            _rwlock.EnterWriteLock();
            try
            {
                var new_id = _data.Count == 0 ? 1 : _data.Max(t => t.Id) + 1;
                data.Id = new_id;
                _data.Add(data);
                Write();
                return data;
            } finally
            {
                _rwlock.ExitWriteLock();
            }
        }

        public bool Remove(T data)
        {
            return Remove(data.Id);
        }
        public bool Remove(int id)
        {
            _rwlock.EnterUpgradeableReadLock();
            try
            {
                var obj = _data.FirstOrDefault(t => t.Id == id);
                if (obj == null) 
                {
                    return false;
                }
                _rwlock.EnterWriteLock();
                try
                {
                    if (_data.Contains(obj))
                    {
                        return _data.Remove(obj);
                    } else
                    {
                        return false;
                    }
                } finally
                {
                    _rwlock.ExitWriteLock();
                }
            } finally
            {
                _rwlock.ExitUpgradeableReadLock();
            }
        }

        public void Update(T data)
        {
            _rwlock.EnterUpgradeableReadLock();
            try
            {
                for (var i = 0; i < _data.Count; i++)
                {
                    if (_data[i].Id == data.Id)
                    {
                        _rwlock.EnterWriteLock();
                        try
                        {
                            _data[i] = data;
                            Write();
                            return;
                        } finally
                        {
                            _rwlock.ExitWriteLock();
                        }
                    }
                }
                throw new KeyNotFoundException();
            } finally
            {
                _rwlock.ExitUpgradeableReadLock();
            }
        }

        private void Write()
        {
            var json = JsonSerializer.Serialize(_data);
            using StreamWriter w = new(_filePath);
            w.Write(json);
        }
    }
}
