using GenesisFEPortalWebApp.Database.Data;
using GenesisFEPortalWebApp.Models.Entities.Security;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GenesisFEPortalWebApp.BL.Services
{
    public interface IAuthAuditLogger
    {
        Task LogLoginAttempt(string email, bool success, string details);
    }

    public class AuthAuditLogger : IAuthAuditLogger
    {
        private readonly ILogger<AuthAuditLogger> _logger;
        private readonly AppDbContext _dbContext;

        public AuthAuditLogger(
            ILogger<AuthAuditLogger> logger,
            AppDbContext dbContext)
        {
            _logger = logger;
            _dbContext = dbContext;
        }

        public async Task LogLoginAttempt(string email, bool success, string details)
        {
            try
            {
                // Crear registro de auditoría
                var auditLog = new SecurityLogModel
                {
                    EventType = "LOGIN_ATTEMPT",
                    Email = email,
                    Success = success,
                    Details = details,
                    CreatedAt = DateTime.UtcNow,
                    IpAddress = "", // Puedes agregarlo como parámetro si lo necesitas
                };

                // Guardar en base de datos
                _dbContext.SecurityLogs.Add(auditLog);
                await _dbContext.SaveChangesAsync();

                // Logging
                if (success)
                {
                    _logger.LogInformation("Login exitoso para {Email}: {Details}", email, details);
                }
                else
                {
                    _logger.LogWarning("Intento de login fallido para {Email}: {Details}", email, details);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al registrar intento de login para {Email}", email);
                throw;
            }
        }
    }
}
