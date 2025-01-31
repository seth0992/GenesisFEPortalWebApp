using GenesisFEPortalWebApp.Models.Entities.Common;
using GenesisFEPortalWebApp.Models.Entities.Core;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GenesisFEPortalWebApp.Models.Entities.Security
{
    /// <summary>
    /// Modelo para gestionar secretos y configuraciones sensibles por tenant/usuario
    /// </summary>
    [Table("Secrets", Schema = "Security")]
    public class SecretModel : BaseEntity
    {
        /// <summary>
        /// ID del tenant propietario del secreto
        /// </summary>
        public long TenantId { get; set; }

        /// <summary>
        /// Clave única para identificar el secreto
        /// </summary>
        public string Key { get; set; } = string.Empty;

        /// <summary>
        /// Valor del secreto
        /// </summary>
        public string Value { get; set; } = string.Empty;  // Añadimos esta propiedad

        /// <summary>
        /// Descripción del propósito del secreto
        /// </summary>
        public string Description { get; set; } = string.Empty;

        // Relaciones de navegación
        public virtual TenantModel Tenant { get; set; } = null!;
    }
}
