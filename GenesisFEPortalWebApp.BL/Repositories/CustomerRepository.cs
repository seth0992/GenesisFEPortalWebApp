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
    }
}
