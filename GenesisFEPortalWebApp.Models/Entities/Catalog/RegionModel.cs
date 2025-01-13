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
    [Table("Region", Schema = "Catalog")]
    public class RegionModel
    {
        [Key]
        public int RegionID { get; set; }
        public string RegionName { get; set; } = string.Empty;

        [JsonIgnore]
        public ICollection<DistrictModel>? Districts { get; }
    }
}
