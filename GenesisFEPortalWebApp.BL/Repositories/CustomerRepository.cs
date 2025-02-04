using GenesisFEPortalWebApp.Database.Data;
using GenesisFEPortalWebApp.Models.Entities.Core;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GenesisFEPortalWebApp.BL.Repositories
{
    public interface ICustomerRepository
    {
        // Método específico para obtener clientes por tenant
        Task<List<CustomerModel>> GetCustomersByTenantIdAsync(long tenantId);

        Task<CustomerModel> CreateCustomerAsync(CustomerModel customer);
    }

    public class CustomerRepository : ICustomerRepository
    {
        private readonly AppDbContext _context;

        public CustomerRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<CustomerModel> CreateCustomerAsync(CustomerModel customer)
        {
            try
            {
                // Basic validation
                if (customer == null)
                {
                    throw new ArgumentNullException(nameof(customer), "Customer cannot be null");
                }

                if (customer.TenantId == 0)
                {
                    throw new InvalidOperationException("TenantId must be set before creating a customer");
                }

                // Add the customer to the context and save changes
                await _context.Customers.AddAsync(customer);
                await _context.SaveChangesAsync();

                // Return the created customer with its new ID
                return customer;
            }
            catch (Exception ex)
            {
                // Log the exception (assuming you have logging set up)
                // _logger.LogError(ex, "Error creating customer");
                throw; // Re-throw to allow caller to handle
            }
        }

        public async Task<List<CustomerModel>> GetCustomersByTenantIdAsync(long tenantId)
        {
            // Incluir relaciones para obtener datos completos
            return await _context.Customers
                .Include(c => c.IdentificationType)
                .Include(c => c.District)
                    .ThenInclude(d => d.Canton)
                .Where(c => c.TenantId == tenantId)
                .ToListAsync();
        }
    }
}
