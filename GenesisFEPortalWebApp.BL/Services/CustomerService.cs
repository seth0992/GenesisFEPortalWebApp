using GenesisFEPortalWebApp.BL.Repositories;
using GenesisFEPortalWebApp.Models.Entities.Core;
using GenesisFEPortalWebApp.Models.Services;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GenesisFEPortalWebApp.BL.Services
{
    public interface ICustomerService
    {
        Task<List<CustomerModel>> GetCustomersByCurrentTenantAsync();
        Task<CustomerModel?> GetCustomerByIdAsync(long id);
        Task<(bool Success, string? ErrorMessage, CustomerModel? Customer)> CreateCustomerAsync(CustomerModel customer);
        Task<(bool Success, string? ErrorMessage, CustomerModel? Customer)> UpdateCustomerAsync(CustomerModel customer);
        Task<(bool Success, string? ErrorMessage)> DeleteCustomerAsync(long id);

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
            return await _customerRepository.GetCustomerByIdAsync(id, tenantId);
        }

        public async Task<(bool Success, string? ErrorMessage, CustomerModel? Customer)>
            CreateCustomerAsync(CustomerModel customer)
        {
            try
            {
                var tenantId = _tenantService.GetCurrentTenantId();

                // Validar que la identificación no exista en el tenant
                if (await _customerRepository.ExistsIdentificationInTenantAsync(
                    customer.Identification, tenantId))
                {
                    return (false, "Ya existe un cliente con esta identificación", null);
                }

                customer.TenantId = tenantId;
                customer.CreatedAt = DateTime.UtcNow;
                customer.IsActive = true;

                var createdCustomer = await _customerRepository.CreateCustomerAsync(customer);
                return (true, null, createdCustomer);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear el cliente");
                return (false, "Error al crear el cliente", null);
            }
        }

        public async Task<(bool Success, string? ErrorMessage, CustomerModel? Customer)>
            UpdateCustomerAsync(CustomerModel customer)
        {
            try
            {
                var tenantId = _tenantService.GetCurrentTenantId();

                // Verificar que el cliente existe y pertenece al tenant
                var existingCustomer = await _customerRepository.GetCustomerByIdAsync(
                    customer.ID, tenantId);

                if (existingCustomer == null)
                {
                    return (false, "Cliente no encontrado", null);
                }

                // Validar identificación única
                if (await _customerRepository.ExistsIdentificationInTenantAsync(
                    customer.Identification, tenantId, customer.ID))
                {
                    return (false, "Ya existe un cliente con esta identificación", null);
                }

                customer.TenantId = tenantId;
                customer.UpdatedAt = DateTime.UtcNow;

                var updatedCustomer = await _customerRepository.UpdateCustomerAsync(customer);
                return (true, null, updatedCustomer);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al actualizar el cliente");
                return (false, "Error al actualizar el cliente", null);
            }
        }

        public async Task<(bool Success, string? ErrorMessage)> DeleteCustomerAsync(long id)
        {
            try
            {
                var tenantId = _tenantService.GetCurrentTenantId();
                var result = await _customerRepository.DeleteCustomerAsync(id, tenantId);

                return result
                    ? (true, null)
                    : (false, "Cliente no encontrado");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al eliminar el cliente");
                return (false, "Error al eliminar el cliente");
            }
        }
    }
}
