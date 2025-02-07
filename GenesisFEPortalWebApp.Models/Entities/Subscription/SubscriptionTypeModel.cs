using GenesisFEPortalWebApp.Models.Entities.Common;
using GenesisFEPortalWebApp.Models.Entities.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GenesisFEPortalWebApp.Models.Entities.Subscription
{
    public class SubscriptionTypeModel : BaseEntity
    {
        public string Name { get; set; } = null!;
        public string? Description { get; set; }
        public decimal Price { get; set; }
        public int DurationInDays { get; set; }
        public int MaxUsers { get; set; }
        public bool IncludeSupport { get; set; }
        public string Features { get; set; } = null!;

        public virtual ICollection<TenantModel> Tenants { get; set; } = new List<TenantModel>();
        public virtual ICollection<SubscriptionHistoryModel> SubscriptionHistory { get; set; } = new List<SubscriptionHistoryModel>();
    }

}
