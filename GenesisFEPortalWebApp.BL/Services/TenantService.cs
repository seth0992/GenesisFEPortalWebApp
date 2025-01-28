using GenesisFEPortalWebApp.Database.Data;
using GenesisFEPortalWebApp.Models.Entities.Core;
using GenesisFEPortalWebApp.Models.Exceptions;
using GenesisFEPortalWebApp.Models.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace GenesisFEPortalWebApp.BL.Services
{
    public class TenantService : ITenantService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<TenantService> _logger;
        private readonly IDbContextFactory<AppDbContext> _contextFactory;
        private long? _currentTenantId;

        public TenantService(
            IHttpContextAccessor httpContextAccessor,
            ILogger<TenantService> logger,
            IDbContextFactory<AppDbContext> contextFactory)
        {
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
            _contextFactory = contextFactory;
        }

        public long GetCurrentTenantId()
        {
            try
            {
                // Obtener el contexto HTTP actual
                var httpContext = _httpContextAccessor.HttpContext;
                if (httpContext == null)
                {
                    _logger.LogWarning("HttpContext no disponible");
                    return 0;
                }

                // Verificar si el usuario está autenticado
                if (!httpContext.User?.Identity?.IsAuthenticated ?? true)
                {
                    _logger.LogWarning("Usuario no autenticado");
                    return 0;
                }

                // Intentar obtener el TenantId del claim
                var tenantClaim = httpContext.User.FindFirst("TenantId");
                if (tenantClaim == null)
                {
                    _logger.LogWarning("Claim TenantId no encontrado");
                    return 0;
                }

                if (!long.TryParse(tenantClaim.Value, out long tenantId))
                {
                    _logger.LogError("Error al parsear TenantId: {Value}", tenantClaim.Value);
                    return 0;
                }

                _logger.LogDebug("TenantId obtenido correctamente: {TenantId}", tenantId);
                return tenantId;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener el TenantId actual");
                return 0;
            }
        }

        public async Task<TenantModel> GetCurrentTenant()
        {
            var tenantId = GetCurrentTenantId();
            if (tenantId == 0)
                return null;

            using var context = await _contextFactory.CreateDbContextAsync();
            var tenant = await context.Tenants
                .FirstOrDefaultAsync(t => t.ID == tenantId && t.IsActive);

            if (tenant == null)
                throw new UnauthorizedAccessException("Tenant no encontrado o inactivo");

            return tenant;
        }

        public async Task<bool> TenantExists(long tenantId)
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            return await context.Tenants
                .AnyAsync(t => t.ID == tenantId && t.IsActive);
        }

        public async Task<bool> ValidateTenantAccess(long tenantId, long userId)
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            var user = await context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.ID == userId);

            if (user == null)
                return false;

            // Super Admin puede acceder a cualquier tenant
            if (user.Role.Name == "SuperAdmin")
                return true;

            // Para otros usuarios, verificar que pertenezcan al tenant
            return user.TenantId == tenantId;
        }

        public async Task EnsureValidTenant(long tenantId)
        {
            var currentTenantId = GetCurrentTenantId();

            if (currentTenantId != tenantId)
            {
                var userId = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (userId == null || !await ValidateTenantAccess(tenantId, long.Parse(userId)))
                {
                    throw new UnauthorizedAccessException("No tiene acceso a este tenant");
                }
            }

            if (!await TenantExists(tenantId))
            {
                throw new NotFoundException("Tenant", tenantId);
            }
        }
    }
}
