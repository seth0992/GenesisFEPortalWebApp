using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GenesisFEPortalWebApp.BL.Cache
{
    public interface ICacheService
    {
        T? Get<T>(string key);
        void Set<T>(string key, T value, TimeSpan? expirationTime = null);
        void Remove(string key);
        bool Exists(string key);
        IEnumerable<string> GetKeys();
        void RemoveByPattern(string pattern);
    }
}
