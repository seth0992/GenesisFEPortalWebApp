using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GenesisFEPortalWebApp.Models.Models.Customer
{
    public class UpdateCustomerDto
    {
        public long ID { get; set; }
        public string CustomerName { get; set; }
        public string? CommercialName { get; set; }
        public string Identification { get; set; }
        public string IdentificationTypeId { get; set; }
        public string? Email { get; set; }
        public string? PhoneCode { get; set; }
        public string? Phone { get; set; }
        public string? Address { get; set; }
        public string? Neighborhood { get; set; }
        public int? DistrictId { get; set; }
    }
}
