using GenesisFEPortalWebApp.BL.Repositories;
using GenesisFEPortalWebApp.Models.Entities.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace GenesisFEPortalWebApp.BL.Services
{

    /// <summary>
    /// Define las operaciones para la gestión de tokens JWT en un contexto multi-tenant.
    /// Esta interfaz maneja la generación, validación y renovación de tokens de acceso.
    /// </summary>
    public interface ITokenService
    {
        /// <summary>
        /// Genera un nuevo par de tokens (access token y refresh token) para un usuario.
        /// El token generado incluirá información específica del tenant al que pertenece el usuario.
        /// </summary>
        /// <param name="user">El usuario para el cual se generarán los tokens</param>
        /// <returns>Una tupla conteniendo el token de acceso y el token de refresco</returns>
        Task<(string Token, string RefreshToken)> GenerateTokensAsync(UserModel user);

        /// <summary>
        /// Refresca un token existente, generando un nuevo par de tokens si el token actual
        /// y el refresh token son válidos. El nuevo token mantiene la información del tenant.
        /// </summary>
        /// <param name="token">El token de acceso actual</param>
        /// <param name="refreshToken">El token de refresco actual</param>
        /// <returns>Un nuevo par de tokens si la validación es exitosa, null en caso contrario</returns>
        Task<(string? Token, string? RefreshToken)?> RefreshTokenAsync(string token, string refreshToken);

        /// <summary>
        /// Revoca un token existente, invalidándolo para futuro uso.
        /// Esta operación debe considerar el contexto del tenant.
        /// </summary>
        /// <param name="token">El token a revocar</param>
        /// <returns>true si el token fue revocado exitosamente, false en caso contrario</returns>
        Task<bool> RevokeTokenAsync(string token);

        /// <summary>
        /// Valida un token JWT y retorna el ClaimsPrincipal si es válido.
        /// La validación considera el secreto específico del tenant.
        /// </summary>
        /// <param name="token">El token a validar</param>
        /// <returns>El ClaimsPrincipal si el token es válido, null en caso contrario</returns>
        Task<ClaimsPrincipal?> ValidateTokenAsync(string token);
    }

    public class TokenService : ITokenService
    {
        private readonly ISecretRepository _secretRepository;
        private readonly IConfiguration _configuration;
        private readonly ILogger<TokenService> _logger;

        public TokenService(
            ISecretRepository secretRepository,
            IConfiguration configuration,
            ILogger<TokenService> logger)
        {
            _secretRepository = secretRepository;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<(string Token, string RefreshToken)> GenerateTokensAsync(UserModel user)
        {
            try
            {
                // Intentar obtener el secreto específico del tenant
                var jwtSecret = await _secretRepository.GetSecretValueAsync("JWT_SECRET", user.TenantId);

                // Si no hay secreto específico, usar el secreto global
                if (string.IsNullOrEmpty(jwtSecret))
                {
                    _logger.LogWarning(
                        "No se encontró secreto específico para tenant {TenantId}, usando secreto global",
                        user.TenantId);
                    jwtSecret = _configuration["JWT:Secret"];
                }

                if (string.IsNullOrEmpty(jwtSecret))
                {
                    throw new InvalidOperationException("No se encontró un secreto JWT válido");
                }

                var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret));
                var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

                var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, user.ID.ToString()),
                new(ClaimTypes.Name, $"{user.FirstName} {user.LastName}"),
                new(ClaimTypes.Email, user.Email),
                new("TenantId", user.TenantId.ToString()),
                new("TenantName", user.Tenant.Name),
                new(ClaimTypes.Role, user.Role.Name)
            };

                var token = new JwtSecurityToken(
                    issuer: _configuration["JWT:ValidIssuer"],
                    audience: _configuration["JWT:ValidAudience"],
                    claims: claims,
                    expires: DateTime.UtcNow.AddHours(1),
                    signingCredentials: credentials
                );

                var tokenHandler = new JwtSecurityTokenHandler();
                var refreshToken = GenerateRefreshTokenString();

                _logger.LogInformation(
                    "Token generado exitosamente para usuario {UserId} del tenant {TenantId}",
                    user.ID, user.TenantId);

                return (tokenHandler.WriteToken(token), refreshToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error generando tokens para usuario {UserId} del tenant {TenantId}",
                    user.ID, user.TenantId);
                throw;
            }
        }

        public async Task<(string? Token, string? RefreshToken)?> RefreshTokenAsync(string token, string refreshToken)
        {
            try
            {
                var principal = await ValidateTokenAsync(token);
                if (principal == null) return null;

                // Obtener el TenantId del token actual
                var tenantIdClaim = principal.Claims.FirstOrDefault(c => c.Type == "TenantId");
                if (tenantIdClaim == null || !long.TryParse(tenantIdClaim.Value, out var tenantId))
                {
                    _logger.LogError("No se encontró TenantId en el token");
                    return null;
                }

                // Regenerar token con el mismo tenant
                var claims = principal.Claims.ToList();

                // Obtener el secreto específico del tenant
                var jwtSecret = await _secretRepository.GetSecretValueAsync("JWT_SECRET", tenantId)
                    ?? _configuration["JWT:Secret"];

                if (string.IsNullOrEmpty(jwtSecret))
                {
                    _logger.LogError("No se encontró secreto JWT válido para tenant {TenantId}", tenantId);
                    return null;
                }

                var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret));
                var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

                var newToken = new JwtSecurityToken(
                    issuer: _configuration["JWT:ValidIssuer"],
                    audience: _configuration["JWT:ValidAudience"],
                    claims: claims,
                    expires: DateTime.UtcNow.AddHours(1),
                    signingCredentials: credentials
                );

                var tokenHandler = new JwtSecurityTokenHandler();
                var newRefreshToken = GenerateRefreshTokenString();

                return (tokenHandler.WriteToken(newToken), newRefreshToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error refreshing token");
                return null;
            }
        }

        public async Task<bool> RevokeTokenAsync(string token)
        {
            try
            {
                var principal = await ValidateTokenAsync(token);
                return principal != null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error revoking token");
                return false;
            }
        }

        public async Task<ClaimsPrincipal?> ValidateTokenAsync(string token)
        {
            try
            {
                var handler = new JwtSecurityTokenHandler();
                var jwtToken = handler.ReadJwtToken(token);

                // Obtener el TenantId del token
                var tenantIdClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == "TenantId");
                if (tenantIdClaim == null || !long.TryParse(tenantIdClaim.Value, out var tenantId))
                {
                    _logger.LogError("TenantId no encontrado en el token");
                    return null;
                }

                // Obtener el secreto específico del tenant
                var jwtSecret = await _secretRepository.GetSecretValueAsync("JWT_SECRET", tenantId)
                    ?? _configuration["JWT:Secret"];

                if (string.IsNullOrEmpty(jwtSecret))
                {
                    _logger.LogError("No se encontró secreto JWT válido para tenant {TenantId}", tenantId);
                    return null;
                }

                var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret));
                var validationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = key,
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidIssuer = _configuration["JWT:ValidIssuer"],
                    ValidAudience = _configuration["JWT:ValidAudience"],
                    ClockSkew = TimeSpan.Zero
                };

                try
                {
                    var principal = handler.ValidateToken(token, validationParameters, out _);
                    return principal;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error validando token para tenant {TenantId}", tenantId);
                    return null;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error procesando token");
                return null;
            }
        }

        private static string GenerateRefreshTokenString()
        {
            var randomNumber = new byte[32];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomNumber);
            return Convert.ToBase64String(randomNumber);
        }
    }
}
