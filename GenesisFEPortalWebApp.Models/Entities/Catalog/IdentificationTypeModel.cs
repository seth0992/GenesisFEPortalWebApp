using GenesisFEPortalWebApp.Models.Entities.Core;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace GenesisFEPortalWebApp.Models.Entities.Catalog
{
    [Table("IdentificationTypes", Schema = "Catalog")]
    public class IdentificationTypeModel
    {
        public string ID { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;

        [JsonIgnore]
        public ICollection<CustomerModel>? Customers { get; }
    }
}
