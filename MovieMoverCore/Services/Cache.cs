using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MovieMoverCore.Services
{
    public interface ICache<TOwner, TKey, TElement>
    {
        public DateTime EoD { get; }

        void InvalidateAll();
        bool Invalidate(TKey key);
        void Clean();
        bool Retrieve(TKey key, out TElement element);
        void UpdateOrAdd(TKey key, TElement element, DateTime validity);
        void UpdateOrAdd(TKey key, TElement element, TimeSpan validity);
        void UpdateOrAdd(IEnumerable<(TKey key, TElement element)> elements, DateTime validity);
        void UpdateOrAdd(IEnumerable<(TKey key, TElement element)> elements, TimeSpan validity);
    }

    public class Cache<TOwner, TKey, TElement> : ICache<TOwner, TKey, TElement>
    {
        private ReaderWriterLockSlim _cacheRWLock;
        private Dictionary<TKey, (TElement element, DateTime validUntil)> _data;

        public DateTime EoD => DateTime.Now.Date.Add(new TimeSpan(1, 0, 0, 0) - new TimeSpan(1));

        public Cache()
        {
            _cacheRWLock = new ReaderWriterLockSlim();
            _data = new Dictionary<TKey, (TElement element, DateTime validUntil)>();
        }
        public bool Invalidate(TKey key)
        {
            _cacheRWLock.EnterWriteLock();
            try
            {
                if (_data.ContainsKey(key))
                {
                    _data.Remove(key);
                    return true;
                }
                return false;
            } finally
            {
                _cacheRWLock.ExitWriteLock();
            }
        }

        public void InvalidateAll()
        {
            _cacheRWLock.EnterWriteLock();
            try
            {
                _data.Clear();
            }
            finally
            {
                _cacheRWLock.ExitWriteLock();
            }
        }

        public bool Retrieve(TKey key, out TElement element)
        {
            _cacheRWLock.EnterReadLock();
            try
            {
                if (_data.TryGetValue(key, out var o))
                {
                    if (o.validUntil >= DateTime.Now)
                    {
                        element = o.element;
                        return true;
                    }
                }
            } finally
            {
                _cacheRWLock.ExitReadLock();
            }

            element = default;
            return false;
        }

        private void UpdateOrAdd_Int(TKey key, TElement element, DateTime validity)
        {
            if (!_cacheRWLock.IsWriteLockHeld)
            {
                throw new InvalidOperationException("The writer lock is not held but should be held.");
            }

            if (_data.ContainsKey(key))
            {
                _data[key] = (element, validity);
            } else
            {
                _data.Add(key, (element, validity));
            }
        }

        public void UpdateOrAdd(TKey key, TElement element, DateTime validity)
        {
            _cacheRWLock.EnterWriteLock();
            try
            {
                UpdateOrAdd_Int(key, element, validity);
            } finally
            {
                _cacheRWLock.ExitWriteLock();
            }
        }

        public void UpdateOrAdd(TKey key, TElement element, TimeSpan validity)
        {
            UpdateOrAdd(key, element, DateTime.Now.Add(validity));
        }

        public void UpdateOrAdd(IEnumerable<(TKey key, TElement element)> elements, DateTime validity)
        {
            _cacheRWLock.EnterWriteLock();
            try
            {
                foreach (var (key, element) in elements)
                {
                    UpdateOrAdd(key, element, validity);
                }
            } finally
            {
                _cacheRWLock.ExitWriteLock();
            }
        }

        public void UpdateOrAdd(IEnumerable<(TKey key, TElement element)> elements, TimeSpan validity)
        {
            UpdateOrAdd(elements, DateTime.Now.Add(validity));
        }

        public void Clean()
        {
            _cacheRWLock.EnterWriteLock();
            try
            {
                foreach (var k in _data.Keys.ToList())
                {
                    if (_data[k].validUntil < DateTime.Now)
                    {
                        _data.Remove(k);
                    }
                }
            } finally
            {
                _cacheRWLock.ExitWriteLock();
            }
        }
    }
}
