using MovieMoverCore.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Xml.Linq;

namespace MovieMoverCore.Services
{
    public interface ISharedData<TElement> 
    {
        public IList<TElement> get(string identifier);
        public IList<TElement> this[string identifier] { get => get(identifier); }
        public void Add(string identifier, TElement element);
        public void SetValidity(string identifier, TimeSpan validity);
    }

    public class SharedData<TElement> : ISharedData<TElement>
    {
        private readonly Dictionary<string, List<(DateTime dtAdded, TElement element)>> _data;
        private readonly Dictionary<string, (ReaderWriterLockSlim lockObj, TimeSpan validity)> _config;
        private readonly ReaderWriterLockSlim _mainLock;

        public SharedData()
        {
            _data = [];
            _config = [];
            _mainLock = new ReaderWriterLockSlim();
        }

        public IList<TElement> get(string identifier)
        {
            try
            {
                _mainLock.EnterUpgradeableReadLock();

                if (!_data.ContainsKey(identifier))
                {
                    try
                    {
                        _mainLock.EnterWriteLock();
                        if (!_data.ContainsKey(identifier))
                        {
                            _data.Add(identifier, new List<(DateTime dtAdded, TElement element)>());
                            _config.Add(identifier, (new ReaderWriterLockSlim(), new TimeSpan(1, 00, 0)));
                        }
                    } finally
                    {
                        _mainLock.ExitWriteLock();
                    }
                }
                return _data[identifier].Select(e => e.element).ToList();
            } finally
            {
                _mainLock.ExitUpgradeableReadLock();
            }
        }

        public void Add(string identifier, TElement element)
        {
            try
            {
                _mainLock.EnterUpgradeableReadLock();

                if (!_data.ContainsKey(identifier))
                {
                    try
                    {
                        _mainLock.EnterWriteLock();
                        if (!_data.ContainsKey(identifier))
                        {
                            _data.Add(identifier, new List<(DateTime dtAdded, TElement element)>());
                            _config.Add(identifier, (new ReaderWriterLockSlim(), new TimeSpan(1, 00, 0)));
                        }
                    }
                    finally
                    {
                        _mainLock.ExitWriteLock();
                    }
                }

                var _list = _data[identifier];
                var _conf = _config[identifier];

                try
                {
                    _conf.lockObj.EnterWriteLock();
                    var oldest = DateTime.Now - _conf.validity;
                    while (_list.Count > 0 && _list[0].dtAdded < oldest)
                    {
                        _list.RemoveAt(0);
                    }
                    _list.Add((DateTime.Now, element));
                } finally
                {
                    _conf.lockObj.ExitWriteLock();
                }
            }
            finally
            {
                _mainLock.ExitUpgradeableReadLock();
            }
        }

        public void SetValidity(string identifier, TimeSpan validity)
        {
            try
            {
                _mainLock.EnterUpgradeableReadLock();

                if (!_data.ContainsKey(identifier))
                {
                    try
                    {
                        _mainLock.EnterWriteLock();
                        if (!_data.ContainsKey(identifier))
                        {
                            _data.Add(identifier, new List<(DateTime dtAdded, TElement element)>());
                            _config.Add(identifier, (new ReaderWriterLockSlim(), new TimeSpan(1, 00, 0)));
                        }
                    }
                    finally
                    {
                        _mainLock.ExitWriteLock();
                    }
                }

                var _conf = _config[identifier];

                try
                {
                    _conf.lockObj.EnterWriteLock();
                    _config[identifier] = (_conf.lockObj, validity);
                }
                finally
                {
                    _conf.lockObj.ExitWriteLock();
                }
            }
            finally
            {
                _mainLock.ExitUpgradeableReadLock();
            }
        }
    }
}
