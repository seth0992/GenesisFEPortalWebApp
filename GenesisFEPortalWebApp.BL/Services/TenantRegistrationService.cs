using GenesisFEPortalWebApp.BL.Repositories;
using GenesisFEPortalWebApp.Database.Data;
using GenesisFEPortalWebApp.Models.Entities.Core;
using GenesisFEPortalWebApp.Models.Entities.Security;
using GenesisFEPortalWebApp.Models.Entities.Subscription;
using GenesisFEPortalWebApp.Models.Models.Core;
using Microsoft.Extensions.Logging;
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
        private readonly ISecretService _secretService;
        private readonly IEncryptionService _encryptionService;
        private readonly AppDbContext _context;
        private readonly ILogger<TenantRegistrationService> _logger;
        private readonly ISubscriptionRepository _subscriptionRepository;

        public TenantRegistrationService(
            ISubscriptionRepository subscriptionRepository,
              ITenantRepository tenantRepository,
              IAuthRepository authRepository,
              IPasswordHasher passwordHasher,
              ISecretService secretService,
              IEncryptionService encryptionService,
              AppDbContext context,
              ILogger<TenantRegistrationService> logger)
        {
            _subscriptionRepository = subscriptionRepository;
            _tenantRepository = tenantRepository;
            _authRepository = authRepository;
            _passwordHasher = passwordHasher;
            _secretService = secretService;
            _encryptionService = encryptionService;
            _context = context;
            _logger = logger;
        }


        public async Task<(bool Success, string? ErrorMessage, TenantModel? Tenant)> RegisterTenantWithAdminAsync(RegisterTenantDto model)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
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

                // 2. Generar y guardar el secreto JWT para el nuevo tenant
                var jwtSecret = _encryptionService.GenerateSecureKey();
                await _secretService.SetSecretAsync(
                    "JWT_SECRET",
                    jwtSecret,
                    tenant.ID,
                    "JWT signing key for authentication");

                // 3. Obtener el rol de TenantAdmin
                var adminRole = await _authRepository.GetRoleByNameAsync("TenantAdmin");
                if (adminRole == null)
                {
                    throw new InvalidOperationException("No se encontró el rol de administrador");
                }

                // 4. Crear el usuario administrador
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
                    CreatedAt = DateTime.UtcNow,
                    LastPasswordChangeDate = DateTime.UtcNow,
                    SecurityStamp = Guid.NewGuid().ToString()
                };

                await _authRepository.CreateUserAsync(adminUser);
                await _authRepository.SaveChangesAsync();

                await transaction.CommitAsync();
                return (true, null, tenant);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error al registrar tenant");
                return (false, "Error al registrar el tenant", null);
            }
        }

        public async Task<(bool Success, string? ErrorMessage, TenantModel? Tenant)> RegisterTenantWithTrialAsync(
        RegisterTenantDto model)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Validaciones básicas
                if (await _tenantRepository.IdentificationExistsAsync(model.Identification))
                    return (false, "Ya existe un tenant con esta identificación", null);

                // Obtener suscripción trial
                var trialSubscription = await _subscriptionRepository.GetByIdAsync(1); // ID de suscripción trial
                if (trialSubscription == null)
                    return (false, "Error configurando suscripción de prueba", null);

                // Crear tenant
                var tenant = new TenantModel
                {
                    Name = model.CompanyName,
                    Identification = model.Identification,
                    Email = model.Email,
                    IsActive = true,
                    SubscriptionTypeId = trialSubscription.ID,
                    SubscriptionStartDate = DateTime.UtcNow,
                    SubscriptionEndDate = DateTime.UtcNow.AddDays(30),
                    IsTrialPeriod = true,
                    CreatedAt = DateTime.UtcNow
                };

                tenant = await _tenantRepository.CreateTenantAsync(tenant);

                // Crear admin user
                var adminRole = await _authRepository.GetRoleByNameAsync("TenantAdmin");
                if (adminRole == null)
                    return (false, "Error configurando rol de administrador", null);

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

                // Crear historial de suscripción
                var subscriptionHistory = new SubscriptionHistoryModel
                {
                    TenantId = tenant.ID,
                    SubscriptionTypeId = trialSubscription.ID,
                    StartDate = tenant.SubscriptionStartDate.Value,
                    EndDate = tenant.SubscriptionEndDate.Value,
                    Amount = 0,
                    PaymentStatus = "Trial",
                    CreatedAt = DateTime.UtcNow
                };

                await _subscriptionRepository.CreateHistoryAsync(subscriptionHistory);

                await transaction.CommitAsync();
                return (true, null, tenant);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error en registro de tenant");
                return (false, "Error al procesar el registro", null);
            }
        }

    }
}
