using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GenesisFEPortalWebApp.BL.Cache.Configuration
{
    /// <summary>
    /// Opciones de configuración para el sistema de caché
    /// </summary>
    public class CacheOptions
    {
        public const string ConfigurationSection = "Cache";

        /// <summary>
        /// Tiempo predeterminado de expiración del caché
        /// </summary>
        public TimeSpan DefaultExpiration { get; set; } = TimeSpan.FromMinutes(30);

        /// <summary>
        /// Tiempo de expiración específico para catálogos
        /// </summary>
        public TimeSpan CatalogExpiration { get; set; } = TimeSpan.FromHours(24);

        /// <summary>
        /// Indica si el caché está habilitado
        /// </summary>
        public bool EnableCaching { get; set; } = true;
    }
}
