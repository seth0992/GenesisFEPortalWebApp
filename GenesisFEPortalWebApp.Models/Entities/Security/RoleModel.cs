using GenesisFEPortalWebApp.Models.Entities.Common;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GenesisFEPortalWebApp.Models.Entities.Security
{
    [Table("Roles", Schema = "Security")]
    public class RoleModel : BaseEntity
    {
        public string Name { get; set; } = null!;
        public string? Description { get; set; }
        public string? Permissions { get; set; }
        public bool IsSystem { get; set; }

        public virtual ICollection<UserModel> Users { get; set; } = new List<UserModel>();
    }
}
