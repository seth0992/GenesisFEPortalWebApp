using GenesisFEPortalWebApp.Database.Data;
using GenesisFEPortalWebApp.Models.Entities.Security;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GenesisFEPortalWebApp.BL.Repositories
{
    public interface IAuthRepository
    {
        Task<UserModel?> GetUserByEmailAsync(string email, bool includeRelations = true);
        Task<RoleModel?> GetRoleByNameAsync(string roleName);
        Task<RefreshTokenModel?> GetRefreshTokenAsync(long userId, string token);
        Task<List<RefreshTokenModel>> GetActiveRefreshTokensByUserIdAsync(long userId);
        Task<bool> EmailExistsAsync(string email);
        Task<bool> EmailExistsInTenantAsync(string email, long tenantId);
        Task CreateUserAsync(UserModel user);
        Task CreateRefreshTokenAsync(RefreshTokenModel refreshToken);
        Task UpdateRefreshTokenAsync(RefreshTokenModel refreshToken);
        Task UpdateUserLastLoginAsync(long userId, DateTime loginDate);
        Task UpdateUserSecurityStampAsync(long userId, string securityStamp);
        Task IncrementAccessFailedCountAsync(long userId);
        Task ResetAccessFailedCountAsync(long userId);
        Task UpdateUserLockoutAsync(long userId, DateTime? lockoutEnd);
        Task<bool> IsUserLockedOutAsync(long userId);
        Task<int> GetAccessFailedCountAsync(long userId);
        Task RevokeAllActiveRefreshTokensAsync(long userId);
        Task SaveChangesAsync();
    }


    public class AuthRepository : IAuthRepository
    {
        private readonly AppDbContext _context;

        public AuthRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<UserModel?> GetUserByEmailAsync(string email, bool includeRelations = true)
        {
            var query = _context.Users.AsQueryable();

            if (includeRelations)
            {
                query = query
                    .Include(u => u.Role)
                    .Include(u => u.Tenant);
            }

            return await query.FirstOrDefaultAsync(u => u.Email == email);
        }

        public async Task<RoleModel?> GetRoleByNameAsync(string roleName)
        {
            return await _context.Roles.FirstOrDefaultAsync(r => r.Name == roleName);
        }

        public async Task<RefreshTokenModel?> GetRefreshTokenAsync(long userId, string token)
        {
            return await _context.RefreshTokens
                .FirstOrDefaultAsync(rt => rt.UserId == userId && rt.Token == token);
        }

        public async Task<List<RefreshTokenModel>> GetActiveRefreshTokensByUserIdAsync(long userId)
        {
            return await _context.RefreshTokens
                .Where(rt => rt.UserId == userId && rt.RevokedAt == null && rt.ExpiryDate > DateTime.UtcNow)
                .ToListAsync();
        }

        public async Task<bool> EmailExistsAsync(string email)
        {
            return await _context.Users.AnyAsync(u => u.Email == email);
        }

        public async Task<bool> EmailExistsInTenantAsync(string email, long tenantId)
        {
            return await _context.Users
                .AnyAsync(u => u.Email == email && u.TenantId == tenantId);
        }

        public async Task CreateUserAsync(UserModel user)
        {
            await _context.Users.AddAsync(user);
        }

        public async Task CreateRefreshTokenAsync(RefreshTokenModel refreshToken)
        {
            await _context.RefreshTokens.AddAsync(refreshToken);
        }

        public async Task UpdateRefreshTokenAsync(RefreshTokenModel refreshToken)
        {
            _context.RefreshTokens.Update(refreshToken);
            await SaveChangesAsync();
        }

        public async Task UpdateUserLastLoginAsync(long userId, DateTime loginDate)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user != null)
            {
                user.LastLoginDate = loginDate;
                user.LastSuccessfulLogin = loginDate;
                await SaveChangesAsync();
            }
        }

        public async Task UpdateUserSecurityStampAsync(long userId, string securityStamp)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user != null)
            {
                user.SecurityStamp = securityStamp;
                await SaveChangesAsync();
            }
        }

        public async Task IncrementAccessFailedCountAsync(long userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user != null)
            {
                user.AccessFailedCount++;
                await SaveChangesAsync();
            }
        }

        public async Task ResetAccessFailedCountAsync(long userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user != null)
            {
                user.AccessFailedCount = 0;
                await SaveChangesAsync();
            }
        }

        public async Task UpdateUserLockoutAsync(long userId, DateTime? lockoutEnd)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user != null)
            {
                user.LockoutEnd = lockoutEnd;
                await SaveChangesAsync();
            }
        }

        public async Task<bool> IsUserLockedOutAsync(long userId)
        {
            var user = await _context.Users.FindAsync(userId);
            return user?.LockoutEnd != null && user.LockoutEnd > DateTime.UtcNow;
        }

        public async Task<int> GetAccessFailedCountAsync(long userId)
        {
            var user = await _context.Users.FindAsync(userId);
            return user?.AccessFailedCount ?? 0;
        }

        public async Task RevokeAllActiveRefreshTokensAsync(long userId)
        {
            var activeTokens = await GetActiveRefreshTokensByUserIdAsync(userId);
            foreach (var token in activeTokens)
            {
                token.RevokedAt = DateTime.UtcNow;
            }
            await SaveChangesAsync();
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}
}
