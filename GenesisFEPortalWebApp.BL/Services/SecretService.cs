using GenesisFEPortalWebApp.BL.Repositories;
using GenesisFEPortalWebApp.Models.Entities.Security;
using GenesisFEPortalWebApp.Models.Services;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GenesisFEPortalWebApp.BL.Services
{
    /// <summary>
    /// Interfaz del servicio de gestión de secretos
    /// </summary>
    public interface ISecretService
    {
        /// <summary>
        /// Obtiene el valor de un secreto usando el tenant del contexto actual
        /// </summary>
        Task<string?> GetSecretValueAsync(string key);

        /// <summary>
        /// Obtiene el valor de un secreto para un tenant específico
        /// </summary>
        Task<string?> GetSecretValueAsync(string key, long tenantId);

        /// <summary>
        /// Guarda un secreto
        /// </summary>
        Task<bool> SetSecretAsync(
            string key,
            string value,
            long tenantId,
            string description = "",
            DateTime? expirationDate = null);

        /// <summary>
        /// Guarda un secreto específico de usuario
        /// </summary>
        Task<bool> SetUserSecretAsync(
            string key,
            string value,
            long userId,
            string description = "",
            DateTime? expirationDate = null);

        /// <summary>
        /// Elimina un secreto
        /// </summary>
        Task<bool> DeleteSecretAsync(long id);
    }

    /// <summary>
    /// Implementación del servicio de gestión de secretos
    /// </summary>
    public class SecretService : ISecretService
    {
        private readonly ISecretRepository _secretRepository;
        private readonly IEncryptionService _encryptionService;
        private readonly ITenantService _tenantService;
        private readonly ILogger<SecretService> _logger;

        public SecretService(
            ISecretRepository secretRepository,
            IEncryptionService encryptionService,
            ITenantService tenantService,
            ILogger<SecretService> logger)
        {
            _secretRepository = secretRepository;
            _encryptionService = encryptionService;
            _tenantService = tenantService;
            _logger = logger;
        }

        /// <summary>
        /// Obtiene el valor de un secreto usando el tenant del contexto actual
        /// </summary>
        public async Task<string?> GetSecretValueAsync(string key)
        {
            var tenantId = _tenantService.GetCurrentTenantId();
            return await GetSecretValueAsync(key, tenantId);
        }

        /// <summary>
        /// Obtiene el valor de un secreto para un tenant específico
        /// </summary>
        public async Task<string?> GetSecretValueAsync(string key, long tenantId)
        {
            try
            {
                _logger.LogDebug("Obteniendo secreto {Key} para tenant {TenantId}", key, tenantId);

                var secret = await _secretRepository.GetSecretAsync(key, tenantId);
                if (secret == null)
                {
                    _logger.LogWarning("Secreto {Key} no encontrado para tenant {TenantId}", key, tenantId);
                    return null;
                }

                return secret.IsEncrypted
                    ? _encryptionService.Decrypt(secret.EncryptedValue, key)
                    : secret.EncryptedValue;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving secret {Key} for tenant {TenantId}", key, tenantId);
                throw;
            }
        }

        public async Task<string?> GetUserSecretValueAsync(string key, long userId)
        {
            try
            {
                var tenantId = _tenantService.GetCurrentTenantId();
                var secret = await _secretRepository.GetSecretAsync(key, tenantId, userId);

                if (secret == null) return null;

                return secret.IsEncrypted
                    ? _encryptionService.Decrypt(secret.EncryptedValue, key)
                    : secret.EncryptedValue;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving user secret {Key} for user {UserId}", key, userId);
                return null;
            }
        }

        public async Task<bool> SetSecretAsync(
        string key,
        string value,
        long tenantId,
        string description = "",
        DateTime? expirationDate = null)
        {
            try
            {
                var encryptedValue = _encryptionService.Encrypt(value, key);

                var secret = new SecretModel
                {
                    TenantId = tenantId,
                    Key = key,
                    EncryptedValue = encryptedValue,
                    Description = description,
                    ExpirationDate = expirationDate,
                    IsEncrypted = true,
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true
                };

                await _secretRepository.SaveSecretAsync(secret);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving secret {Key} for tenant {TenantId}", key, tenantId);
                return false;
            }
        }

        public async Task<bool> SetUserSecretAsync(string key, string value, long userId, string description = "", DateTime? expirationDate = null)
        {
            try
            {
                var tenantId = _tenantService.GetCurrentTenantId();
                var encryptedValue = _encryptionService.Encrypt(value, key);

                var secret = new SecretModel
                {
                    TenantId = tenantId,
                    UserId = userId,
                    Key = key,
                    EncryptedValue = encryptedValue,
                    Description = description,
                    ExpirationDate = expirationDate,
                    IsEncrypted = true,
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true
                };

                await _secretRepository.SaveSecretAsync(secret);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving user secret {Key} for user {UserId}", key, userId);
                return false;
            }
        }

        public async Task<bool> DeleteSecretAsync(long id)
        {
            try
            {
                var tenantId = _tenantService.GetCurrentTenantId();
                return await _secretRepository.DeleteSecretAsync(id, tenantId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting secret {Id}", id);
                return false;
            }
        }
    }
}
