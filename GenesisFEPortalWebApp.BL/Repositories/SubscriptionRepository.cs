using GenesisFEPortalWebApp.Database.Data;
using GenesisFEPortalWebApp.Models.Entities.Core;
using GenesisFEPortalWebApp.Models.Entities.Security;
using GenesisFEPortalWebApp.Models.Entities.Subscription;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GenesisFEPortalWebApp.BL.Repositories
{
    public interface ISubscriptionRepository
    {
        Task<SubscriptionTypeModel?> GetByIdAsync(long id);
        Task<List<SubscriptionTypeModel>> GetActiveAsync();
        Task<SubscriptionHistoryModel> CreateHistoryAsync(SubscriptionHistoryModel history);
        Task<List<TenantModel>> GetExpiringSubscriptionsAsync(int daysThreshold);
        Task<List<TenantModel>> GetExpiredTrialsAsync();
        Task<TenantModel?> GetTenantWithSubscriptionAsync(long tenantId);
        Task UpdateTenantSubscriptionAsync(TenantModel tenant);
        Task<List<SubscriptionHistoryModel>> GetSubscriptionHistoryAsync(long tenantId);
        Task<(bool Success, string? ErrorMessage, TenantModel? Tenant)> RegisterTenantWithSubscriptionAsync(
    TenantModel tenant,
    UserModel adminUser,
    SubscriptionHistoryModel history);
    }

    public class SubscriptionRepository : ISubscriptionRepository
    {
        private readonly AppDbContext _context;
        private readonly ILogger<SubscriptionRepository> _logger;

        public SubscriptionRepository(AppDbContext context, ILogger<SubscriptionRepository> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<SubscriptionTypeModel?> GetByIdAsync(long id)
        {
            return await _context.SubscriptionTypes
                .FirstOrDefaultAsync(s => s.ID == id && s.IsActive);
        }

        public async Task<List<SubscriptionTypeModel>> GetActiveAsync()
        {
            return await _context.SubscriptionTypes
                .Where(s => s.IsActive)
                .OrderBy(s => s.Price)
                .ToListAsync();
        }

        public async Task<SubscriptionHistoryModel> CreateHistoryAsync(SubscriptionHistoryModel history)
        {
            _context.SubscriptionHistory.Add(history);
            await _context.SaveChangesAsync();
            return history;
        }
        public async Task<(bool Success, string? ErrorMessage, TenantModel? Tenant)> RegisterTenantWithSubscriptionAsync(
           TenantModel tenant,
           UserModel adminUser,
           SubscriptionHistoryModel history)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var addedTenant = await _context.Tenants.AddAsync(tenant);
                await _context.SaveChangesAsync();

                adminUser.TenantId = addedTenant.Entity.ID;
                history.TenantId = addedTenant.Entity.ID;

                await _context.Users.AddAsync(adminUser);
                await _context.SubscriptionHistory.AddAsync(history);
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();
                return (true, null, addedTenant.Entity);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error registrando tenant con suscripción");
                return (false, "Error en el registro", null);
            }
        }

        public async Task<List<TenantModel>> GetExpiringSubscriptionsAsync(int daysThreshold)
        {
            var thresholdDate = DateTime.UtcNow.AddDays(daysThreshold);
            return await _context.Tenants
                .Include(t => t.SubscriptionType)
                .Where(t => t.IsActive &&
                           t.SubscriptionEndDate <= thresholdDate &&
                           t.SubscriptionEndDate > DateTime.UtcNow)
                .ToListAsync();
        }

        public async Task<List<TenantModel>> GetExpiredTrialsAsync()
        {
            return await _context.Tenants
                .Where(t => t.IsActive &&
                           t.IsTrialPeriod &&
                           t.SubscriptionEndDate <= DateTime.UtcNow)
                .ToListAsync();
        }

        public async Task<TenantModel?> GetTenantWithSubscriptionAsync(long tenantId)
        {
            return await _context.Tenants
                .Include(t => t.SubscriptionType)
                .FirstOrDefaultAsync(t => t.ID == tenantId && t.IsActive);
        }

        public async Task UpdateTenantSubscriptionAsync(TenantModel tenant)
        {
            _context.Tenants.Update(tenant);
            await _context.SaveChangesAsync();
        }

        public async Task<List<SubscriptionHistoryModel>> GetSubscriptionHistoryAsync(long tenantId)
        {
            return await _context.SubscriptionHistory
                .Include(sh => sh.SubscriptionType)
                .Where(sh => sh.TenantId == tenantId)
                .OrderByDescending(sh => sh.CreatedAt)
                .ToListAsync();
        }
    }
}
