using GenesisFEPortalWebApp.BL.Repositories;
using GenesisFEPortalWebApp.Models.Entities.Core;
using GenesisFEPortalWebApp.Models.Models.Customer;
using GenesisFEPortalWebApp.Models.Services;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GenesisFEPortalWebApp.BL.Services
{
    public interface ICustomerService
    {
        Task<List<CustomerModel>> GetCustomersByCurrentTenantAsync();
        Task<CustomerModel> CreateCustomerAsync(CreateCustomerDto customer);
    }

    public class CustomerService : ICustomerService
    {
        private readonly ICustomerRepository _customerRepository;
        private readonly ITenantService _tenantService;
        private readonly ILogger<CustomerService> _logger;

        public CustomerService(
            ICustomerRepository customerRepository,
            ITenantService tenantService,
            ILogger<CustomerService> logger)
        {
            _customerRepository = customerRepository;
            _tenantService = tenantService;
            _logger = logger;
        }
      
        public async Task<CustomerModel> CreateCustomerAsync(CreateCustomerDto customerDto)
        {
            try
            {
                // Get the current tenant ID from the tenant service
                var tenantId = _tenantService.GetCurrentTenantId();

                // Validate tenant ID
                if (tenantId == 0)
                {
                    _logger.LogWarning("No tenant ID found when creating customer");
                    throw new InvalidOperationException("Cannot create customer without a valid tenant");
                }

                // Map DTO to CustomerModel with tenant-specific logic
                var customer = new CustomerModel
                {
                    // Explicitly set tenant-specific properties
                    TenantId = tenantId,
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true,

                    // Map properties from DTO
                    CustomerName = customerDto.CustomerName,
                    CommercialName = customerDto.CommercialName,
                    Identification = customerDto.Identification,
                    IdentificationTypeId = customerDto.IdentificationTypeId,
                    Email = customerDto.Email,
                    PhoneCode = customerDto.PhoneCode,
                    Phone = customerDto.Phone,
                    Address = customerDto.Address,
                    Neighborhood = customerDto.Neighborhood,
                    DistrictId = customerDto.DistrictId
                };

                // Validate customer before creation (optional but recommended)
                ValidateCustomer(customer);

                // Create and return the customer
                return await _customerRepository.CreateCustomerAsync(customer);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating customer");
                throw; // Re-throw to allow caller to handle specific errors
            }
        }
        private void ValidateCustomer(CustomerModel customer)
        {
            // Perform additional validation if needed
            if (string.IsNullOrWhiteSpace(customer.CustomerName))
            {
                throw new ValidationException("Customer name is required");
            }

            if (string.IsNullOrWhiteSpace(customer.Identification))
            {
                throw new ValidationException("Identification is required");
            }

            // Add more specific validations as needed
        }
        public async Task<List<CustomerModel>> GetCustomersByCurrentTenantAsync()
        {
            try
            {
                // Obtener el tenant del usuario actual
                var tenantId = _tenantService.GetCurrentTenantId();

                if (tenantId == 0)
                {
                    _logger.LogWarning("No se pudo obtener el tenant del usuario actual");
                    return new List<CustomerModel>();
                }

                // Obtener clientes del tenant
                return await _customerRepository.GetCustomersByTenantIdAsync(tenantId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener clientes por tenant");
                throw;
            }
        }
    }
}
