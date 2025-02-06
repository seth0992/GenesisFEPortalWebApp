using GenesisFEPortalWebApp.BL.Repositories;
using GenesisFEPortalWebApp.Models.Entities.Core;
using GenesisFEPortalWebApp.Models.Models.Customer;
using GenesisFEPortalWebApp.Models.Services;
using GenesisFEPortalWebApp.Utilities;
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
        Task<CustomerModel?> GetCustomerByIdAsync(long id);
        Task<CustomerModel> UpdateCustomerAsync(UpdateCustomerDto customer);
        Task<bool> DeleteCustomerAsync(long id);

        Task<bool> ActivateCustomerAsync(long id);
        Task<bool> CheckDuplicateIdentificationAsync(string identification, long? excludeId = null);

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
                // Verificar duplicados
                if (await CheckDuplicateIdentificationAsync(customerDto.Identification))
                {
                    throw new ValidationException("Ya existe un cliente con esta identificación en su organización");
                }

                // Validar identificación
                var validationResult = IdentificationValidationUtils.ValidateIdentification(
                    customerDto.IdentificationTypeId,
                    customerDto.Identification);

                if (validationResult != ValidationResult.Success)
                {
                    throw new ValidationException(validationResult.ErrorMessage);
                }

                // Get the current tenant ID from the tenant service
                var tenantId = _tenantService.GetCurrentTenantId();

                // Validate tenant ID
                if (tenantId == 0)
                {
                    _logger.LogWarning("No tenant ID found when creating customer");
                    throw new InvalidOperationException("Cannot create customer without a valid tenant");
                }

                // Formatear identificación
                customerDto.Identification = IdentificationValidationUtils.FormatIdentification(
                    customerDto.IdentificationTypeId,
                    customerDto.Identification);

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
        public async Task<bool> ActivateCustomerAsync(long id)
        {
            var tenantId = _tenantService.GetCurrentTenantId();
            return await _customerRepository.ActivateCustomerAsync(id, tenantId);
        }

        public async Task<bool> CheckDuplicateIdentificationAsync(string identification, long? excludeId = null)
        {
            var tenantId = _tenantService.GetCurrentTenantId();
            return await _customerRepository.ExistsInTenantAsync(identification, tenantId, excludeId);
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

        public async Task<CustomerModel?> GetCustomerByIdAsync(long id)
        {
            var tenantId = _tenantService.GetCurrentTenantId();

            if (tenantId == 0)
            {
                _logger.LogWarning("No se pudo obtener el tenant del usuario actual");
                return null;
            }

            return await _customerRepository.GetByIdAsync(id, tenantId);
        }

        public async Task<CustomerModel> UpdateCustomerAsync(UpdateCustomerDto customerDto)
        {
            try
            {
                var tenantId = _tenantService.GetCurrentTenantId();
                if (tenantId == 0)
                {
                    throw new InvalidOperationException("No se pudo obtener el tenant actual");
                }

                // Validar la identificación según el tipo
                var validationResult = IdentificationValidationUtils.ValidateIdentification(
                    customerDto.IdentificationTypeId,
                    customerDto.Identification);

                if (validationResult != ValidationResult.Success)
                {
                    throw new ValidationException(validationResult.ErrorMessage);
                }

                var existingCustomer = await _customerRepository.GetByIdAsync(customerDto.ID, tenantId);
                if (existingCustomer == null)
                {
                    throw new KeyNotFoundException($"No se encontró el cliente con ID {customerDto.ID}");
                }

                // Actualizar propiedades
                existingCustomer.CustomerName = customerDto.CustomerName;
                existingCustomer.CommercialName = customerDto.CommercialName;
                existingCustomer.Identification = customerDto.Identification;
                existingCustomer.IdentificationTypeId = customerDto.IdentificationTypeId;
                existingCustomer.Email = customerDto.Email;
                existingCustomer.PhoneCode = customerDto.PhoneCode;
                existingCustomer.Phone = customerDto.Phone;
                existingCustomer.Address = customerDto.Address;
                existingCustomer.Neighborhood = customerDto.Neighborhood;
                existingCustomer.DistrictId = customerDto.DistrictId;
                existingCustomer.UpdatedAt = DateTime.UtcNow;

                return await _customerRepository.UpdateAsync(existingCustomer);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error actualizando cliente {CustomerId}", customerDto.ID);
                throw;
            }
        }

        public async Task<bool> DeleteCustomerAsync(long id)
        {
            try
            {
                var tenantId = _tenantService.GetCurrentTenantId();
                if (tenantId == 0)
                {
                    throw new InvalidOperationException("No se pudo obtener el tenant actual");
                }

                // Primero verificamos si el cliente existe
                var customer = await _customerRepository.GetByIdAsync(id, tenantId);
                if (customer == null)
                {
                    _logger.LogWarning("Cliente no encontrado: {CustomerId}", id);
                    return false;
                }

                // Realizamos la eliminación lógica
                customer.IsActive = false;
                customer.UpdatedAt = DateTime.UtcNow;

                await _customerRepository.UpdateAsync(customer);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error eliminando cliente {CustomerId}", id);
                throw;
            }
        }
    }
}
