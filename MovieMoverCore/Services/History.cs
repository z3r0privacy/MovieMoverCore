using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace MovieMoverCore.Services
{
    public interface IHistory<TElement>
    {
        public int Count { get; }
        public IEnumerable<(DateTime, TElement, int)> Items { get; }
        public (DateTime, TElement, int) this[int index] { get; }
        public void Add(TElement element, DateTime? dt = null);
    }
    public interface IHistoryCollection <TElement>
    {
        public IHistory<TElement> this[string name] { get; }
        public IHistory<TElement> GetHistory(string name);
    }

    public class History<TElement> : IHistory<TElement>
    {
        /*
         * Not the easiest implementation, but should be pretty performant ;)
         * The idea is to use the array as a ring, where the items behind the
         * current are getting older. By adding before the current element,
         * the oldest entries are overwritten.
         * The `_index` Dictionary is to access the items by their id (hashvalue).
         * Obviously, this dict needs to be updated to
         */
        private static readonly int HIST_SIZE = 20;
        private readonly (DateTime, TElement, int)[] _items;
        private int _idx = 0;
        private readonly ReaderWriterLockSlim _histRWLock;
        private readonly Dictionary<int, (DateTime, TElement, int)> _index;

        public int Count { get; private set; } = 0;
        public (DateTime, TElement, int) this[int index]
        {
            get
            {
                _histRWLock.EnterReadLock();
                try
                {
                    if (_index.TryGetValue(index, out var value))
                    {
                        return value;
                    }
                    throw new KeyNotFoundException($"Index {index} is not or no longer present in history");
                }
                finally
                {
                    _histRWLock.ExitReadLock();
                }
            }
        }
        public IEnumerable<(DateTime, TElement, int)> Items
        {
            get
            {
                _histRWLock.EnterReadLock();
                try
                {
                    var l = _items[0.._idx].Reverse().ToList();
                    if (Count == HIST_SIZE)
                    {
                        l.AddRange(_items[_idx..].Reverse());
                    }
                    return l;
                } finally
                {
                    _histRWLock.ExitReadLock();
                }
            }
        }

        public History()
        {
            _items = new (DateTime, TElement, int)[HIST_SIZE];
            _histRWLock = new ReaderWriterLockSlim();
            _index = [];
        }

        public void Add(TElement element, DateTime? dt = null)
        {
            _histRWLock.EnterWriteLock();
            try
            {
                var dt_added = dt.HasValue ? dt.Value : DateTime.Now;
                if (Count == HIST_SIZE)
                {
                    _index.Remove(_items[_idx].Item3);
                }
                _items[_idx] = (dt_added, element, element.GetHashCode());
                _index.Add(_items[_idx].Item3, _items[_idx]);
                _idx = (_idx + 1) % HIST_SIZE;
                Count = Math.Min(HIST_SIZE, Count + 1);
            } finally
            {
                _histRWLock.ExitWriteLock();
            }
        }
    }

    public class HistoryCollection<TElement> : IHistoryCollection<TElement>
    {
        private readonly Dictionary<string, IHistory<TElement>> _histories;
        private readonly ReaderWriterLockSlim _histColRWLock;

        public IHistory<TElement> this[string name] 
        {
            get
            {
                return GetHistory(name);
            }
        }

        public HistoryCollection()
        {
            _histories = [];
            _histColRWLock = new ReaderWriterLockSlim();
        }

        public IHistory<TElement> GetHistory(string name)
        {
            _histColRWLock.EnterUpgradeableReadLock();
            try
            {
                if (!_histories.ContainsKey(name))
                {
                    _histColRWLock.EnterWriteLock();
                    try
                    {
                        // now that the lock is upgraded, check again
                        if (!_histories.ContainsKey(name))
                        {
                            _histories[name] = new History<TElement>();
                        }
                    }
                    finally
                    {
                        _histColRWLock.ExitWriteLock();
                    }
                }
                return _histories[name];
            } finally
            {
                _histColRWLock.ExitUpgradeableReadLock();
            }
        }
    }
}
