using GenesisFEPortalWebApp.Database.Data;
using GenesisFEPortalWebApp.Models.Entities.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GenesisFEPortalWebApp.BL.Repositories
{
    /// <summary>
    /// Interfaz para el repositorio de secretos
    /// </summary>
    public interface ISecretRepository
    {
        /// <summary>
        /// Obtiene un secreto específico por clave y tenant
        /// </summary>
        Task<SecretModel?> GetSecretAsync(string key, long tenantId, long? userId = null);

        /// <summary>
        /// Obtiene todos los secretos de un tenant
        /// </summary>
        Task<List<SecretModel>> GetSecretsForTenantAsync(long tenantId);

        /// <summary>
        /// Obtiene todos los secretos de un usuario específico
        /// </summary>
        Task<List<SecretModel>> GetSecretsForUserAsync(long tenantId, long userId);

        /// <summary>
        /// Guarda un secreto nuevo o actualiza uno existente
        /// </summary>
        Task<SecretModel> SaveSecretAsync(SecretModel secret);

        /// <summary>
        /// Elimina lógicamente un secreto
        /// </summary>
        Task<bool> DeleteSecretAsync(long id, long tenantId);
    }

    /// <summary>
    /// Implementación del repositorio de secretos
    /// </summary>
    public class SecretRepository : ISecretRepository
    {
        private readonly AppDbContext _context;
        private readonly ILogger<SecretRepository> _logger;

        public SecretRepository(
            AppDbContext context,
            ILogger<SecretRepository> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<SecretModel?> GetSecretAsync(string key, long tenantId, long? userId = null)
        {
            try
            {
                _logger.LogInformation(
                    "Buscando secreto - Key: {Key}, TenantId: {TenantId}",
                    key, tenantId);

                // Construimos la consulta base
                var query = _context.Secrets
                    .Where(s => s.Key == key &&
                               s.TenantId == tenantId &&
                               s.UserId == userId &&
                               s.IsActive);

                // Verificamos la expiración si existe
                query = query.Where(s => !s.ExpirationDate.HasValue ||
                                       s.ExpirationDate > DateTime.UtcNow);

                var secret = await query.FirstOrDefaultAsync();

                if (secret != null)
                {
                    _logger.LogInformation("Secreto encontrado para tenant {TenantId}", tenantId);

                    // Log detallado del secreto encontrado
                    _logger.LogDebug("Detalles del secreto encontrado: " +
                        "ID: {ID}, " +
                        "TenantId: {TenantId}, " +
                        "IsEncrypted: {IsEncrypted}, " +
                        "ValueLength: {ValueLength}",
                        secret.ID,
                        secret.TenantId,
                        secret.IsEncrypted,
                        secret.EncryptedValue?.Length ?? 0);
                }
                else
                {
                    _logger.LogWarning(
                        "No se encontró secreto para Key: {Key}, TenantId: {TenantId}",
                        key, tenantId);
                }

                return secret;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error buscando secreto - Key: {Key}, TenantId: {TenantId}",
                    key, tenantId);
                throw;
            }
        }

        public async Task<List<SecretModel>> GetSecretsForTenantAsync(long tenantId)
        {
            try
            {
                return await _context.Secrets
                    .Where(s =>
                        s.TenantId == tenantId &&
                        s.UserId == null &&
                        s.IsActive &&
                        (!s.ExpirationDate.HasValue || s.ExpirationDate > DateTime.UtcNow))
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting secrets for tenant {TenantId}", tenantId);
                throw;
            }
        }

        public async Task<List<SecretModel>> GetSecretsForUserAsync(long tenantId, long userId)
        {
            try
            {
                return await _context.Secrets
                    .Where(s =>
                        s.TenantId == tenantId &&
                        s.UserId == userId &&
                        s.IsActive &&
                        (!s.ExpirationDate.HasValue || s.ExpirationDate > DateTime.UtcNow))
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting secrets for user {UserId}", userId);
                throw;
            }
        }

        public async Task<SecretModel> SaveSecretAsync(SecretModel secret)
        {
            try
            {
                var existing = await _context.Secrets
                    .FirstOrDefaultAsync(s =>
                        s.Key == secret.Key &&
                        s.TenantId == secret.TenantId &&
                        s.UserId == secret.UserId);

                if (existing != null)
                {
                    existing.EncryptedValue = secret.EncryptedValue;
                    existing.Description = secret.Description;
                    existing.ExpirationDate = secret.ExpirationDate;
                    existing.IsEncrypted = secret.IsEncrypted;
                    existing.UpdatedAt = DateTime.UtcNow;
                    _context.Secrets.Update(existing);
                }
                else
                {
                    await _context.Secrets.AddAsync(secret);
                }

                await _context.SaveChangesAsync();
                return existing ?? secret;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving secret {Key}", secret.Key);
                throw;
            }
        }

        public async Task<bool> DeleteSecretAsync(long id, long tenantId)
        {
            try
            {
                var secret = await _context.Secrets
                    .FirstOrDefaultAsync(s => s.ID == id && s.TenantId == tenantId);

                if (secret == null) return false;

                secret.IsActive = false;
                secret.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting secret {Id}", id);
                throw;
            }
        }
    }
}
