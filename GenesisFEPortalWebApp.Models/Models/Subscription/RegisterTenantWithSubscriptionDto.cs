using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GenesisFEPortalWebApp.Models.Models.Subscription
{
    public class RegisterTenantWithSubscriptionDto
    {
        public string CompanyName { get; set; } = null!;
        public string Identification { get; set; } = null!;  
        public string IdentificationTypeId { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string FirstName { get; set; } = null!;
        public string LastName { get; set; } = null!;
        public string Password { get; set; } = null!;
        public long SubscriptionTypeId { get; set; }
        public string? PaymentMethod { get; set; }
        public string? TransactionId { get; set; }
    }
}
