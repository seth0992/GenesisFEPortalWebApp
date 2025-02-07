using GenesisFEPortalWebApp.Models.Entities.Common;
using GenesisFEPortalWebApp.Models.Entities.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GenesisFEPortalWebApp.Models.Entities.Subscription
{
    public class SubscriptionHistoryModel : BaseEntity
    {
        public long TenantId { get; set; }
        public long SubscriptionTypeId { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public decimal Amount { get; set; }
        public string? TransactionId { get; set; }
        public string PaymentStatus { get; set; } = null!;

        public virtual TenantModel Tenant { get; set; } = null!;
        public virtual SubscriptionTypeModel SubscriptionType { get; set; } = null!;
    }
}
