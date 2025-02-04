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
        Task<CustomerModel?> GetCustomerByIdAsync(long id, long tenantId);
        Task<CustomerModel> CreateCustomerAsync(CustomerModel customer);
        Task<CustomerModel> UpdateCustomerAsync(CustomerModel customer);
        Task<bool> DeleteCustomerAsync(long id, long tenantId);
        Task<bool> ExistsIdentificationInTenantAsync(string identification, long tenantId, long? excludeId = null);

    }

    public class CustomerRepository : ICustomerRepository
    {
        private readonly AppDbContext _context;

        public CustomerRepository(AppDbContext context)
        {
            _context = context;
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
        public async Task<CustomerModel?> GetCustomerByIdAsync(long id, long tenantId)
        {
            return await _context.Customers
                .Include(c => c.IdentificationType)
                .Include(c => c.District)
                    .ThenInclude(d => d.Canton)
                .FirstOrDefaultAsync(c => c.ID == id && c.TenantId == tenantId);
        }

        public async Task<CustomerModel> CreateCustomerAsync(CustomerModel customer)
        {
            await _context.Customers.AddAsync(customer);
            await _context.SaveChangesAsync();
            return customer;
        }

        public async Task<CustomerModel> UpdateCustomerAsync(CustomerModel customer)
        {
            _context.Customers.Update(customer);
            await _context.SaveChangesAsync();
            return customer;
        }

        public async Task<bool> DeleteCustomerAsync(long id, long tenantId)
        {
            var customer = await _context.Customers
                .FirstOrDefaultAsync(c => c.ID == id && c.TenantId == tenantId);

            if (customer == null) return false;

            customer.IsActive = false;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ExistsIdentificationInTenantAsync(
            string identification,
            long tenantId,
            long? excludeId = null)
        {
            var query = _context.Customers
                .Where(c => c.TenantId == tenantId &&
                           c.Identification == identification);

            if (excludeId.HasValue)
            {
                query = query.Where(c => c.ID != excludeId.Value);
            }

            return await query.AnyAsync();
        }
    }
}
