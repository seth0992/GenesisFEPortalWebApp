using GenesisFEPortalWebApp.Models.Entities.Catalog;
using GenesisFEPortalWebApp.Models.Entities.Common;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace GenesisFEPortalWebApp.Models.Entities.Core
{
    [Table("Customers", Schema = "Core")]
    public class CustomerModel : BaseEntity, IHasTenant
    {
        public long TenantId { get; set; }
        public string CustomerName { get; set; } = null!;
        public string? CommercialName { get; set; }
        public string Identification { get; set; } = null!;
        public string IdentificationTypeId { get; set; } = null!;
        public string? Email { get; set; }
        public string? PhoneCode { get; set; }
        public string? Phone { get; set; }
        public string? Address { get; set; }
        public string? Neighborhood { get; set; }
        public int? DistrictId { get; set; }

        public virtual TenantModel Tenant { get; set; } = null!;
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public virtual IdentificationTypeModel IdentificationType { get; set; } = null!;
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public virtual DistrictModel? District { get; set; }
    }
}
