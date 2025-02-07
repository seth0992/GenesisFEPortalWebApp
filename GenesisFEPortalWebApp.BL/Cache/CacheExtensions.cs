using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GenesisFEPortalWebApp.BL.Cache
{
    public static class CacheExtensions
    {
        public static void ClearCatalogCache(this ICacheService cacheService)
        {
            var keysToRemove = new[]
            {
            "IdentificationTypes",
            "Provinces",
            "Cantons",
            "Districts"
        };

            foreach (var key in keysToRemove)
            {
                cacheService.Remove(key);
            }
        }
    }
}
