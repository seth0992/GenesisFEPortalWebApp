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
  
        public interface ISecretRepository
        {
        /// <summary>
        /// Obtiene el valor de un secreto específico para un tenant
        /// </summary>
        Task<string?> GetSecretValueAsync(string key, long tenantId);

        /// <summary>
        /// Verifica si existe un secreto específico para un tenant
        /// </summary>
        Task<bool> SecretExistsAsync(string key, long tenantId);

        /// <summary>
        /// Guarda un nuevo secreto o actualiza uno existente
        /// </summary>
        Task SaveSecretAsync(string key, string value, long tenantId, string description);

        /// <summary>
        /// Desactiva un secreto específico
        /// </summary>
        Task DeactivateSecretAsync(string key, long tenantId);

        /// <summary>
        /// Obtiene todos los secretos activos para un tenant
        /// </summary>
        Task<List<SecretModel>> GetAllSecretsForTenantAsync(long tenantId);
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

        /// <summary>
        /// Obtiene el valor de un secreto específico para un tenant.
        /// </summary>
        public async Task<string?> GetSecretValueAsync(string key, long tenantId)
        {
            try
            {
                _logger.LogInformation(
                    "Buscando secreto - Key: {Key}, TenantId: {TenantId}",
                    key, tenantId);

                // Buscamos un secreto activo que coincida con la clave y el tenant
                var secret = await _context.Secrets
                    .Where(s => s.Key == key &&
                               s.TenantId == tenantId &&
                               s.IsActive)
                    .Select(s => s.Value)
                    .FirstOrDefaultAsync();

                if (secret != null)
                {
                    _logger.LogInformation(
                        "Secreto encontrado para tenant {TenantId}",
                        tenantId);
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

        /// <summary>
        /// Verifica si existe un secreto específico para un tenant.
        /// </summary>
        public async Task<bool> SecretExistsAsync(string key, long tenantId)
        {
            try
            {
                return await _context.Secrets
                    .AnyAsync(s => s.Key == key &&
                                 s.TenantId == tenantId &&
                                 s.IsActive);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error verificando existencia de secreto - Key: {Key}, TenantId: {TenantId}",
                    key, tenantId);
                throw;
            }
        }

        /// <summary>
        /// Guarda un nuevo secreto o actualiza uno existente para un tenant específico.
        /// </summary>
        public async Task SaveSecretAsync(
            string key,
            string value,
            long tenantId,
            string description)
        {
            try
            {
                // Buscar si ya existe un secreto con esta clave para este tenant
                var existingSecret = await _context.Secrets
                    .FirstOrDefaultAsync(s => s.Key == key &&
                                            s.TenantId == tenantId);

                if (existingSecret != null)
                {
                    // Actualizar el secreto existente
                    _logger.LogInformation(
                        "Actualizando secreto existente - Key: {Key}, TenantId: {TenantId}",
                        key, tenantId);

                    existingSecret.Value = value;
                    existingSecret.Description = description;
                    existingSecret.UpdatedAt = DateTime.UtcNow;

                    _context.Secrets.Update(existingSecret);
                }
                else
                {
                    // Crear un nuevo secreto
                    _logger.LogInformation(
                        "Creando nuevo secreto - Key: {Key}, TenantId: {TenantId}",
                        key, tenantId);

                    var newSecret = new SecretModel
                    {
                        Key = key,
                        Value = value,
                        TenantId = tenantId,
                        Description = description,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    };

                    await _context.Secrets.AddAsync(newSecret);
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation(
                    "Secreto guardado exitosamente - Key: {Key}, TenantId: {TenantId}",
                    key, tenantId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error guardando secreto - Key: {Key}, TenantId: {TenantId}",
                    key, tenantId);
                throw;
            }
        }

        /// <summary>
        /// Desactiva un secreto específico para un tenant.
        /// </summary>
        public async Task DeactivateSecretAsync(string key, long tenantId)
        {
            try
            {
                var secret = await _context.Secrets
                    .FirstOrDefaultAsync(s => s.Key == key &&
                                            s.TenantId == tenantId);

                if (secret != null)
                {
                    secret.IsActive = false;
                    secret.UpdatedAt = DateTime.UtcNow;

                    await _context.SaveChangesAsync();

                    _logger.LogInformation(
                        "Secreto desactivado - Key: {Key}, TenantId: {TenantId}",
                        key, tenantId);
                }
                else
                {
                    _logger.LogWarning(
                        "No se encontró secreto para desactivar - Key: {Key}, TenantId: {TenantId}",
                        key, tenantId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error desactivando secreto - Key: {Key}, TenantId: {TenantId}",
                    key, tenantId);
                throw;
            }
        }

        /// <summary>
        /// Obtiene todos los secretos activos para un tenant específico.
        /// </summary>
        public async Task<List<SecretModel>> GetAllSecretsForTenantAsync(long tenantId)
        {
            try
            {
                return await _context.Secrets
                    .Where(s => s.TenantId == tenantId &&
                               s.IsActive)
                    .OrderBy(s => s.Key)
                    .ToListAsync();
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
