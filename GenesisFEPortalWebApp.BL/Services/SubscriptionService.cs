using GenesisFEPortalWebApp.BL.Repositories;
using GenesisFEPortalWebApp.Models.Entities.Core;
using GenesisFEPortalWebApp.Models.Entities.Security;
using GenesisFEPortalWebApp.Models.Entities.Subscription;
using GenesisFEPortalWebApp.Models.Models.Subscription;
using GenesisFEPortalWebApp.Models.Services;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace GenesisFEPortalWebApp.BL.Services
{
    public interface ISubscriptionService
    {

        Task<SubscriptionTypeModel?> GetSubscriptionTypeAsync(long id);
        Task<List<SubscriptionTypeModel>> GetActiveSubscriptionTypesAsync();
        Task<(bool Success, string? ErrorMessage)> UpdateTenantSubscriptionAsync(
            long tenantId,
            long subscriptionTypeId,
            string paymentMethod,
            string transactionId);
        Task<(bool Success, string? ErrorMessage)> ProcessSubscriptionRenewalAsync(long tenantId);
        Task<(bool Success, string? ErrorMessage)> CancelSubscriptionAsync(long tenantId);
        Task ProcessExpiredTrialsAsync();
        Task ProcessSubscriptionRenewalsAsync();
        Task<bool> ValidateSubscriptionFeaturesAsync(long tenantId, string feature);

        Task<(bool Success, string? ErrorMessage)> RegisterTenantWithSubscriptionAsync(RegisterTenantWithSubscriptionDto model);
    }

    public class SubscriptionService : ISubscriptionService
    {
        private readonly ISubscriptionRepository _subscriptionRepository;
        private readonly IAuthRepository _authRepository;
        private readonly IPasswordHasher _passwordHasher;
        private readonly ILogger<SubscriptionService> _logger;

        public SubscriptionService(
            ISubscriptionRepository subscriptionRepository,
            IAuthRepository authRepository,
            IPasswordHasher passwordHasher,
            ILogger<SubscriptionService> logger)
        {
            _subscriptionRepository = subscriptionRepository;
            _authRepository = authRepository;
            _passwordHasher = passwordHasher;
            _logger = logger;
        }

        public async Task<(bool Success, string? ErrorMessage)> RegisterTenantWithSubscriptionAsync(
      RegisterTenantWithSubscriptionDto model)
        {
            var subscription = await _subscriptionRepository.GetByIdAsync(model.SubscriptionTypeId);
            if (subscription == null)
                return (false, "Plan de suscripción no válido");

            var tenant = CreateTenantModel(model, subscription);
            var adminUser = await CreateAdminUserAsync(model);
            if (adminUser == null)
                return (false, "Error creando usuario administrador");

            var history = CreateSubscriptionHistory(tenant, subscription, model.TransactionId);

            var result = await _subscriptionRepository.RegisterTenantWithSubscriptionAsync(
                tenant, adminUser, history);

            return (result.Success, result.ErrorMessage);
        }

        private TenantModel CreateTenantModel(RegisterTenantWithSubscriptionDto model, SubscriptionTypeModel subscription)
        {
            return new TenantModel
            {
                Name = model.CompanyName,
                Identification = model.Identification,
                Email = model.Email,
                IsActive = true,
                SubscriptionTypeId = subscription.ID,
                SubscriptionStartDate = DateTime.UtcNow,
                SubscriptionEndDate = DateTime.UtcNow.AddDays(subscription.DurationInDays),
                SubscriptionAmount = subscription.Price,
                PaymentMethod = model.PaymentMethod,
                PaymentStatus = "Active",
                IsTrialPeriod = false
            };
        }

        private async Task<UserModel?> CreateAdminUserAsync(RegisterTenantWithSubscriptionDto model)
        {
            var adminRole = await _authRepository.GetRoleByNameAsync("TenantAdmin");
            if (adminRole == null) return null;

            return new UserModel
            {
                Email = model.Email,
                Username = model.Email,
                FirstName = model.FirstName,
                LastName = model.LastName,
                PasswordHash = _passwordHasher.HashPassword(model.Password),
                RoleId = adminRole.ID,
                EmailConfirmed = true,
                IsActive = true
            };
        }

        private SubscriptionHistoryModel CreateSubscriptionHistory(
            TenantModel tenant,
            SubscriptionTypeModel subscription,
            string? transactionId)
        {
            return new SubscriptionHistoryModel
            {
                SubscriptionTypeId = subscription.ID,
                StartDate = tenant.SubscriptionStartDate!.Value,
                EndDate = tenant.SubscriptionEndDate!.Value,
                Amount = subscription.Price,
                PaymentStatus = "Active",
                TransactionId = transactionId
            };
        }
        public async Task<SubscriptionTypeModel?> GetSubscriptionTypeAsync(long id)
        {
            return await _subscriptionRepository.GetByIdAsync(id);
        }

        public async Task<List<SubscriptionTypeModel>> GetActiveSubscriptionTypesAsync()
        {
            return await _subscriptionRepository.GetActiveAsync();
        }

        public async Task<(bool Success, string? ErrorMessage)> UpdateTenantSubscriptionAsync(
            long tenantId,
            long subscriptionTypeId,
            string paymentMethod,
            string transactionId)
        {
            try
            {
                var tenant = await _subscriptionRepository.GetTenantWithSubscriptionAsync(tenantId);
                if (tenant == null)
                    return (false, "Tenant no encontrado");

                var newSubscription = await _subscriptionRepository.GetByIdAsync(subscriptionTypeId);
                if (newSubscription == null)
                    return (false, "Tipo de suscripción no válido");

                // Crear historial
                var history = new SubscriptionHistoryModel
                {
                    TenantId = tenantId,
                    SubscriptionTypeId = subscriptionTypeId,
                    StartDate = DateTime.UtcNow,
                    EndDate = DateTime.UtcNow.AddDays(newSubscription.DurationInDays),
                    Amount = newSubscription.Price,
                    TransactionId = transactionId,
                    PaymentStatus = "Completed",
                    CreatedAt = DateTime.UtcNow
                };

                await _subscriptionRepository.CreateHistoryAsync(history);

                // Actualizar tenant
                tenant.SubscriptionTypeId = subscriptionTypeId;
                tenant.SubscriptionStartDate = history.StartDate;
                tenant.SubscriptionEndDate = history.EndDate;
                tenant.SubscriptionAmount = history.Amount;
                tenant.PaymentMethod = paymentMethod;
                tenant.PaymentStatus = "Active";
                tenant.IsTrialPeriod = false;
                tenant.UpdatedAt = DateTime.UtcNow;

                await _subscriptionRepository.UpdateTenantSubscriptionAsync(tenant);

                return (true, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error actualizando suscripción del tenant {TenantId}", tenantId);
                return (false, "Error procesando la actualización de suscripción");
            }
        }

        public async Task ProcessSubscriptionRenewalsAsync()
        {
            try
            {
                var expiringTenants = await _subscriptionRepository.GetExpiringSubscriptionsAsync(7);
                foreach (var tenant in expiringTenants)
                {
                    // Implementar lógica de renovación automática
                    // Notificar al tenant
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error procesando renovaciones de suscripción");
            }
        }

        public async Task ProcessExpiredTrialsAsync()
        {
            try
            {
                var expiredTrials = await _subscriptionRepository.GetExpiredTrialsAsync();
                foreach (var tenant in expiredTrials)
                {
                    // Implementar lógica de expiración de prueba
                    // Notificar al tenant
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error procesando trials expirados");
            }
        }

        public async Task<bool> ValidateSubscriptionFeaturesAsync(long tenantId, string feature)
        {
            var tenant = await _subscriptionRepository.GetTenantWithSubscriptionAsync(tenantId);
            if (tenant?.SubscriptionType == null)
                return false;

            var features = JsonSerializer.Deserialize<Dictionary<string, bool>>(
                tenant.SubscriptionType.Features);

            return features?.GetValueOrDefault(feature, false) ?? false;
        }

        public async Task<(bool Success, string? ErrorMessage)> CancelSubscriptionAsync(long tenantId)
        {
            try
            {
                var tenant = await _subscriptionRepository.GetTenantWithSubscriptionAsync(tenantId);
                if (tenant == null)
                    return (false, "Tenant no encontrado");

                // Crear registro histórico de cancelación
                var history = new SubscriptionHistoryModel
                {
                    TenantId = tenantId,
                    SubscriptionTypeId = tenant.SubscriptionTypeId!.Value,
                    StartDate = tenant.SubscriptionStartDate!.Value,
                    EndDate = DateTime.UtcNow,
                    Amount = tenant.SubscriptionAmount ?? 0,
                    PaymentStatus = "Cancelled",
                    CreatedAt = DateTime.UtcNow
                };

                await _subscriptionRepository.CreateHistoryAsync(history);

                // Actualizar estado del tenant
                tenant.PaymentStatus = "Cancelled";
                tenant.UpdatedAt = DateTime.UtcNow;
                await _subscriptionRepository.UpdateTenantSubscriptionAsync(tenant);

                return (true, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelando suscripción del tenant {TenantId}", tenantId);
                return (false, "Error procesando la cancelación de suscripción");
            }
        }

        public async Task<(bool Success, string? ErrorMessage)> ProcessSubscriptionRenewalAsync(long tenantId)
        {
            try
            {
                var tenant = await _subscriptionRepository.GetTenantWithSubscriptionAsync(tenantId);
                if (tenant == null)
                    return (false, "Tenant no encontrado");

                var subscription = tenant.SubscriptionType;
                if (subscription == null)
                    return (false, "Tipo de suscripción no encontrado");

                // Crear nuevo período de suscripción
                var history = new SubscriptionHistoryModel
                {
                    TenantId = tenantId,
                    SubscriptionTypeId = subscription.ID,
                    StartDate = tenant.SubscriptionEndDate ?? DateTime.UtcNow,
                    EndDate = (tenant.SubscriptionEndDate ?? DateTime.UtcNow).AddDays(subscription.DurationInDays),
                    Amount = subscription.Price,
                    PaymentStatus = "Renewed",
                    CreatedAt = DateTime.UtcNow
                };

                await _subscriptionRepository.CreateHistoryAsync(history);

                // Actualizar tenant
                tenant.SubscriptionEndDate = history.EndDate;
                tenant.SubscriptionAmount = history.Amount;
                tenant.UpdatedAt = DateTime.UtcNow;
                tenant.PaymentStatus = "Active";

                await _subscriptionRepository.UpdateTenantSubscriptionAsync(tenant);

                return (true, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error renovando suscripción del tenant {TenantId}", tenantId);
                return (false, "Error procesando la renovación de suscripción");
            }
        }
    }
}
