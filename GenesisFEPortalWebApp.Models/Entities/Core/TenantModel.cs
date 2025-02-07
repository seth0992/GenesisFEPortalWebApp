using GenesisFEPortalWebApp.Models.Entities.Common;
using GenesisFEPortalWebApp.Models.Entities.Security;
using GenesisFEPortalWebApp.Models.Entities.Subscription;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GenesisFEPortalWebApp.Models.Entities.Core
{
    [Table("Tenants", Schema = "Core")]
    public class TenantModel : BaseEntity
    {
        public string Name { get; set; } = null!;
        public string Identification { get; set; } = null!;
        public string? CommercialName { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string? Address { get; set; }
        public byte[]? Logo { get; set; }

        // Nuevas propiedades de suscripción
        public long? SubscriptionTypeId { get; set; }
        public DateTime? SubscriptionStartDate { get; set; }
        public DateTime? SubscriptionEndDate { get; set; }
        public bool IsTrialPeriod { get; set; }
        public decimal? SubscriptionAmount { get; set; }
        public string? PaymentMethod { get; set; }
        public string? PaymentStatus { get; set; }

        // Relaciones de navegación
        public virtual SubscriptionTypeModel? SubscriptionType { get; set; }
        public virtual ICollection<UserModel> Users { get; set; } = new List<UserModel>();
        public virtual ICollection<CustomerModel> Customers { get; set; } = new List<CustomerModel>();
        public virtual ICollection<SubscriptionHistoryModel> SubscriptionHistory { get; set; } = new List<SubscriptionHistoryModel>();

    }
}
