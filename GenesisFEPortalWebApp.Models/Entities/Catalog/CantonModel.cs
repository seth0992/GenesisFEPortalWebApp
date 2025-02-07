using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace GenesisFEPortalWebApp.Models.Entities.Catalog
{
    [Table("Cantons", Schema = "Catalog")]
    public class CantonModel
    {
        [Key]
        public int CantonID { get; set; }
        public string CantonName { get; set; } = string.Empty;
        public int ProvinceId { get; set; }

        public ProvinceModel? Province { get; set; }

        [JsonIgnore]
        public ICollection<DistrictModel>? Districts { get; }

    }
}
