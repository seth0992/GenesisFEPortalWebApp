using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GenesisFEPortalWebApp.Models.Models.Customer
{
    // En GenesisFEPortalWebApp.Models/Models/Customer/CreateCustomerDto.cs
    public class CreateCustomerDto
    {
        // Solo incluir propiedades necesarias para la creación
        [Required]
        public string CustomerName { get; set; }

        public string? CommercialName { get; set; }

        [Required]
        public string Identification { get; set; }

        [Required]
        public string IdentificationTypeId { get; set; }

        [EmailAddress]
        public string? Email { get; set; }

        public string? PhoneCode { get; set; }

        public string? Phone { get; set; }

        public string? Address { get; set; }

        public string? Neighborhood { get; set; }

        public int? DistrictId { get; set; }
    }
}
