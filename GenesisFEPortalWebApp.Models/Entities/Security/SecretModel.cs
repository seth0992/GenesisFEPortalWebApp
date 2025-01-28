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
        /// ID del usuario propietario (opcional)
        /// </summary>
        public long? UserId { get; set; }

        /// <summary>
        /// Clave única para identificar el secreto
        /// </summary>
        [Required]
        [MaxLength(100)]
        public string Key { get; set; } = string.Empty;

        /// <summary>
        /// Valor del secreto (encriptado si IsEncrypted es true)
        /// </summary>
        [Required]
        public string EncryptedValue { get; set; } = string.Empty;

        /// <summary>
        /// Descripción del propósito del secreto
        /// </summary>
        [MaxLength(500)]
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Fecha de expiración opcional
        /// </summary>
        public DateTime? ExpirationDate { get; set; }

        /// <summary>
        /// Indica si el valor está encriptado
        /// </summary>
        public bool IsEncrypted { get; set; } = true;

        // Relaciones de navegación
        public virtual TenantModel Tenant { get; set; } = null!;
        public virtual UserModel? User { get; set; }
    }
}
