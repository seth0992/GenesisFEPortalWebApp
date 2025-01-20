using GenesisFEPortalWebApp.BL.Repositories;
using GenesisFEPortalWebApp.Database.Data;
using GenesisFEPortalWebApp.Models.Entities.Core;
using GenesisFEPortalWebApp.Models.Entities.Security;
using GenesisFEPortalWebApp.Models.Models.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GenesisFEPortalWebApp.BL.Services
{
    public interface ITenantRegistrationService
    {
        Task<(bool Success, string? ErrorMessage, TenantModel? Tenant)> RegisterTenantWithAdminAsync(RegisterTenantDto model);
    }

    public class TenantRegistrationService : ITenantRegistrationService
    {
        private readonly ITenantRepository _tenantRepository;
        private readonly IAuthRepository _authRepository;
        private readonly IPasswordHasher _passwordHasher;
        private readonly AppDbContext _context;

        public TenantRegistrationService(
            ITenantRepository tenantRepository,
            IAuthRepository authRepository,
            IPasswordHasher passwordHasher,
            AppDbContext context)
        {
            _tenantRepository = tenantRepository;
            _authRepository = authRepository;
            _passwordHasher = passwordHasher;
            _context = context;
        }

        public async Task<(bool Success, string? ErrorMessage, TenantModel? Tenant)> RegisterTenantWithAdminAsync(RegisterTenantDto model)
        {
            // Validaciones iniciales
            if (await _tenantRepository.IdentificationExistsAsync(model.Identification))
            {
                return (false, "Ya existe un tenant con esta identificación", null);
            }

            if (await _authRepository.EmailExistsAsync(model.Email))
            {
                return (false, "El email del administrador ya está registrado", null);
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // 1. Crear el tenant
                var tenant = new TenantModel
                {
                    Name = model.CompanyName,
                    Identification = model.Identification,
                    CommercialName = model.CompanyName,
                    Email = model.Email,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                tenant = await _tenantRepository.CreateTenantAsync(tenant);
                await _tenantRepository.SaveChangesAsync();

                // 2. Obtener el rol de TenantAdmin
                var adminRole = await _authRepository.GetRoleByNameAsync("TenantAdmin");
                if (adminRole == null)
                {
                    throw new InvalidOperationException("No se encontró el rol de administrador");
                }

                // 3. Crear el usuario administrador
                var adminUser = new UserModel
                {
                    TenantId = tenant.ID,
                    Email = model.Email,
                    Username = model.Email,
                    FirstName = model.FirstName,
                    LastName = model.LastName,
                    PasswordHash = _passwordHasher.HashPassword(model.Password),
                    RoleId = adminRole.ID,
                    EmailConfirmed = true,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                await _authRepository.CreateUserAsync(adminUser);
                await _authRepository.SaveChangesAsync();

                await transaction.CommitAsync();
                return (true, null, tenant);
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
    }
}
