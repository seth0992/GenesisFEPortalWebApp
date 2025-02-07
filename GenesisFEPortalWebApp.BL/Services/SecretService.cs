using GenesisFEPortalWebApp.BL.Repositories;
using GenesisFEPortalWebApp.Models.Entities.Security;
using GenesisFEPortalWebApp.Models.Services;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
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
        Task<string?> GetSecretValueAsync(string key, long tenantId);
        Task<bool> SetSecretAsync(string key, string value, long tenantId, string description = "");
        Task<bool> DeleteSecretAsync(string key, long tenantId);
        Task<List<SecretModel>> GetAllSecretsAsync(long tenantId);
    }

    // GenesisFEPortalWebApp.BL/Services/SecretService.cs

    public class SecretService : ISecretService
    {
        private readonly ISecretRepository _secretRepository;
        private readonly ILogger<SecretService> _logger;

        public SecretService(
            ISecretRepository secretRepository,
            ILogger<SecretService> logger)
        {
            _secretRepository = secretRepository;
            _logger = logger;
        }

        public async Task<string?> GetSecretValueAsync(string key, long tenantId)
        {
            try
            {
                _logger.LogInformation(
                    "Buscando secreto {Key} para tenant {TenantId}",
                    key, tenantId);

                var secretValue = await _secretRepository.GetSecretValueAsync(key, tenantId);

                if (secretValue == null)
                {
                    _logger.LogWarning(
                        "No se encontró secreto {Key} para tenant {TenantId}",
                        key, tenantId);
                    return null;
                }

                _logger.LogInformation(
                    "Secreto {Key} encontrado para tenant {TenantId}",
                    key, tenantId);

                return secretValue;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error obteniendo secreto {Key} para tenant {TenantId}",
                    key, tenantId);
                throw;
            }
        }

        public async Task<bool> SetSecretAsync(
            string key,
            string value,
            long tenantId,
            string description = "")
        {
            try
            {
                await _secretRepository.SaveSecretAsync(
                    key,
                    value,
                    tenantId,
                    description);

                _logger.LogInformation(
                    "Secreto {Key} guardado exitosamente para tenant {TenantId}",
                    key, tenantId);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error guardando secreto {Key} para tenant {TenantId}",
                    key, tenantId);
                return false;
            }
        }

        public async Task<bool> DeleteSecretAsync(string key, long tenantId)
        {
            try
            {
                await _secretRepository.DeactivateSecretAsync(key, tenantId);

                _logger.LogInformation(
                    "Secreto {Key} desactivado para tenant {TenantId}",
                    key, tenantId);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error eliminando secreto {Key} para tenant {TenantId}",
                    key, tenantId);
                return false;
            }
        }

        public async Task<List<SecretModel>> GetAllSecretsAsync(long tenantId)
        {
            try
            {
                var secrets = await _secretRepository.GetAllSecretsForTenantAsync(tenantId);

                _logger.LogInformation(
                    "Obtenidos {Count} secretos para tenant {TenantId}",
                    secrets.Count, tenantId);

                return secrets;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error obteniendo secretos para tenant {TenantId}",
                    tenantId);
                throw;
            }
        }
    }
}
