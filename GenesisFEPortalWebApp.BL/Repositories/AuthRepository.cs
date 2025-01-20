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
        Task CreateUserAsync(UserModel user);
        Task CreateRefreshTokenAsync(RefreshTokenModel refreshToken);
        Task UpdateRefreshTokenAsync(RefreshTokenModel refreshToken);
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
                .Where(rt => rt.UserId == userId && rt.RevokedAt == null)
                .ToListAsync();
        }

        public async Task<bool> EmailExistsAsync(string email)
        {
            return await _context.Users.AnyAsync(u => u.Email == email);
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
            await _context.SaveChangesAsync();
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}
