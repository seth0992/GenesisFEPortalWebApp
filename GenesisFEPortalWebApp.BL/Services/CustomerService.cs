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
    }
}
