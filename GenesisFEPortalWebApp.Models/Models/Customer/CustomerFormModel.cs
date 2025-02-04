using GenesisFEPortalWebApp.Models.Models.Validations;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GenesisFEPortalWebApp.Models.Models.Customer
{
    public class CustomerFormModel
    {
        [Required(ErrorMessage = "El tipo de identificación es requerido")]
        public string IdentificationTypeId { get; set; } = string.Empty;

        [Required(ErrorMessage = "La identificación es requerida")]
        [IdentificationValidator]
        public string Identification { get; set; } = string.Empty;

        [Required(ErrorMessage = "El nombre es requerido")]
        [StringLength(255, ErrorMessage = "El nombre no puede exceder los 255 caracteres")]
        public string CustomerName { get; set; } = string.Empty;

        [StringLength(255, ErrorMessage = "El nombre comercial no puede exceder los 255 caracteres")]
        public string? CommercialName { get; set; }

        [EmailAddress(ErrorMessage = "El formato del correo electrónico no es válido")]
        public string? Email { get; set; }

        [RegularExpression(@"^\+?\d{1,3}$", ErrorMessage = "Código de país inválido")]
        public string? PhoneCode { get; set; }

        [RegularExpression(@"^\d{8,15}$", ErrorMessage = "Número de teléfono inválido")]
        public string? Phone { get; set; }

        [Required(ErrorMessage = "La provincia es requerida")]
        public int? ProvinceId { get; set; }

        [Required(ErrorMessage = "El cantón es requerido")]
        public int? CantonId { get; set; }

        [Required(ErrorMessage = "El distrito es requerido")]
        public int? DistrictId { get; set; }

        public string? Neighborhood { get; set; }

        public string? Address { get; set; }
    }
}
