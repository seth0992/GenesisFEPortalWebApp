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
    [Table("Provinces", Schema = "Catalog")]
    public class ProvinceModel
    {
        [Key]
        public int ProvinceID { get; set; }
        public string ProvinceName { get; set; } = string.Empty;

        [JsonIgnore]
        public ICollection<CantonModel>? Cantons { get; }

    }
}
