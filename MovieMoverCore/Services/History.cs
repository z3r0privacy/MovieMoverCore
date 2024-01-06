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
        public IList<(DateTime, TElement)> Items { get; }
        public void Add(TElement element, DateTime? dt = null);
    }
    public interface IHistoryCollection <TElement>
    {
        public IHistory<TElement> GetHistory(string name);
    }

    public class History<TElement> : IHistory<TElement>
    {
        private static int HIST_SIZE = 20;
        public int Count { get; private set; } = 0;
        public IList<(DateTime, TElement)> Items
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

        private readonly (DateTime, TElement)[] _items;
        private int _idx = 0;
        private readonly ReaderWriterLockSlim _histRWLock;

        public History()
        {
            _items = new (DateTime, TElement)[HIST_SIZE];
            _histRWLock = new ReaderWriterLockSlim();
        }

        public void Add(TElement element, DateTime? dt = null)
        {
            _histRWLock.EnterWriteLock();
            try
            {
                var dt_added = dt.HasValue ? dt.Value : DateTime.Now;
                _items[_idx] = (dt_added, element);
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

        public HistoryCollection()
        {
            _histories = [];
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
