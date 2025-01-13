using GenesisFEPortalWebApp.Models.Entities.Core;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace GenesisFEPortalWebApp.Models.Entities.Catalog
{
    [Table("Districts", Schema = "Catalog")]
    public class DistrictModel
    {
        [Key]
        public int DistrictID { get; set; }
        public string DistrictName { get; set; } = string.Empty;
        public int CantonId { get; set; }
        public int RegionID { get; set; }

        public CantonModel? Canton { get; set; }
        public RegionModel? Region { get; set; }

        [JsonIgnore]
        public ICollection<CustomerModel>? CustomerModels { get; }
    }
}
