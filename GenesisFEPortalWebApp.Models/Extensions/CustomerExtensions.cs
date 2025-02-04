using GenesisFEPortalWebApp.Models.Entities.Core;
using GenesisFEPortalWebApp.Models.Models.Customer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GenesisFEPortalWebApp.Models.Extensions
{
    public static class CustomerExtensions
    {
        public static CustomerModel ToEntity(this CustomerFormModel formModel, long? id = null)
        {
            return new CustomerModel
            {
                ID = id ?? 0, // Si es creación, será 0
                CustomerName = formModel.CustomerName,
                CommercialName = formModel.CommercialName,
                Identification = formModel.Identification,
                IdentificationTypeId = formModel.IdentificationTypeId,
                Email = formModel.Email,
                PhoneCode = formModel.PhoneCode,
                Phone = formModel.Phone,
                DistrictId = formModel.DistrictId,
                Address = formModel.Address,
                Neighborhood = formModel.Neighborhood,
                IsActive = true
            };
        }

        public static CustomerFormModel ToFormModel(this CustomerModel entity)
        {
            return new CustomerFormModel
            {
                CustomerName = entity.CustomerName,
                CommercialName = entity.CommercialName,
                Identification = entity.Identification,
                IdentificationTypeId = entity.IdentificationTypeId,
                Email = entity.Email,
                PhoneCode = entity.PhoneCode,
                Phone = entity.Phone,
                ProvinceId = entity.District?.Canton?.ProvinceId,
                CantonId = entity.District?.CantonId,
                DistrictId = entity.DistrictId,
                Address = entity.Address,
                Neighborhood = entity.Neighborhood
            };
        }
    }
}
