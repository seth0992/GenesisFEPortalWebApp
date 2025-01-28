using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GenesisFEPortalWebApp.BL.Services
{
    /// <summary>
    /// Opciones de configuración para servicios de seguridad
    /// </summary>
    public class SecurityOptions
    {
        /// <summary>
        /// Clave maestra para la derivación de claves de encriptación
        /// </summary>
        public string MasterKey { get; set; } = string.Empty;

        /// <summary>
        /// Tiempo de expiración de tokens en minutos
        /// </summary>
        public int TokenExpirationMinutes { get; set; } = 60;
    }
}
