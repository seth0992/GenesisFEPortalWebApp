using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GenesisFEPortalWebApp.BL.Cache.Monitors
{
    /// <summary>
    /// Monitor para detectar y manejar cambios en los catálogos
    /// </summary>
    public class CatalogChangeMonitor
    {
        private readonly ICacheService _cacheService;
        private readonly ILogger<CatalogChangeMonitor> _logger;

        public CatalogChangeMonitor(
            ICacheService cacheService,
            ILogger<CatalogChangeMonitor> logger)
        {
            _cacheService = cacheService;
            _logger = logger;
        }

        /// <summary>
        /// Notifica que ha habido cambios en los catálogos y limpia el caché
        /// </summary>
        /// <param name="catalogType">Tipo de catálogo que cambió (opcional)</param>
        public void NotifyCatalogChanged(string? catalogType = null)
        {
            _logger.LogInformation("Limpiando caché de catálogos. Tipo: {CatalogType}",
                catalogType ?? "Todos");
            _cacheService.ClearCatalogCache();
        }
    }
}
