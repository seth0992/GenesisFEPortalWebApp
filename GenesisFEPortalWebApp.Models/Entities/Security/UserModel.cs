using GenesisFEPortalWebApp.Models.Entities.Common;
using GenesisFEPortalWebApp.Models.Entities.Core;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GenesisFEPortalWebApp.Models.Entities.Security
{
    [Table("Users", Schema = "Security")]
    public class UserModel : BaseEntity
    {
        public long TenantId { get; set; }
        public string Email { get; set; } = null!;
        public string Username { get; set; } = null!;
        public string PasswordHash { get; set; } = null!;
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public long RoleId { get; set; }
        public string? PhoneNumber { get; set; }
        public bool EmailConfirmed { get; set; }
        public bool TwoFactorEnabled { get; set; }
        public DateTime? LockoutEnd { get; set; }
        public int AccessFailedCount { get; set; }
        public DateTime? LastLoginDate { get; set; }

        public virtual RoleModel Role { get; set; } = null!;
        public virtual TenantModel Tenant { get; set; } = null!;
        public virtual ICollection<RefreshTokenModel> RefreshTokens { get; set; } = new List<RefreshTokenModel>();

        // Bloqueo de cuentas 
        public DateTime? LastSuccessfulLogin { get; set; }
        public string? SecurityStamp { get; set; }
        public DateTime? LastPasswordChangeDate { get; set; }

    }
}
