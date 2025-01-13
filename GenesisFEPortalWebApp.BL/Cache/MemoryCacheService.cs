using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GenesisFEPortalWebApp.BL.Cache
{
    public class MemoryCacheService : ICacheService
    {
        private readonly IMemoryCache _memoryCache;
        private readonly TimeSpan _defaultExpirationTime = TimeSpan.FromMinutes(30);
        private readonly ConcurrentDictionary<string, string> _keys = new();

        public MemoryCacheService(IMemoryCache memoryCache)
        {
            _memoryCache = memoryCache;
        }

        public T? Get<T>(string key)
        {
            return _memoryCache.Get<T>(key);
        }

        public void Set<T>(string key, T value, TimeSpan? expirationTime = null)
        {
            var cacheEntryOptions = new MemoryCacheEntryOptions()
                .SetAbsoluteExpiration(expirationTime ?? _defaultExpirationTime)
                .RegisterPostEvictionCallback(
                    (key, value, reason, state) =>
                    {
                        _keys.TryRemove(key.ToString()!, out _);
                    });

            _memoryCache.Set(key, value, cacheEntryOptions);
            _keys.TryAdd(key, key);
        }

        public void Remove(string key)
        {
            _memoryCache.Remove(key);
            _keys.TryRemove(key, out _);
        }

        public bool Exists(string key)
        {
            return _memoryCache.TryGetValue(key, out _);
        }

        public IEnumerable<string> GetKeys()
        {
            return _keys.Keys;
        }

        public void RemoveByPattern(string pattern)
        {
            var keys = _keys.Keys.Where(k => k.Contains(pattern, StringComparison.OrdinalIgnoreCase));
            foreach (var key in keys)
            {
                Remove(key);
            }
        }
    }
}
