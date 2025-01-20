using GenesisFEPortalWebApp.Models.Entities.Common;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GenesisFEPortalWebApp.Models.Entities.Security
{
    [Table("SecurityLogs", Schema = "Security")]
    public class SecurityLogModel : BaseEntity
    {
        public string EventType { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public bool Success { get; set; }
        public string Details { get; set; } = string.Empty;
        public string? IpAddress { get; set; }
    }
}
