using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GenesisFEPortalWebApp.Models.Models.Subscription
{
    public class SubscriptionUpgradeDto
    {
        public long TenantId { get; set; }
        public long SubscriptionTypeId { get; set; }
        public string PaymentMethod { get; set; } = null!;
        public string? TransactionId { get; set; }
    }
}
