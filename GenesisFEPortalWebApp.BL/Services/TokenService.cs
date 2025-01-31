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
    /// Interfaz para el servicio de gestión de tokens JWT
    /// </summary>
    public interface ITokenService
    {
        /// <summary>
        /// Genera un token JWT y refresh token para el usuario especificado
        /// </summary>
        Task<(string Token, string RefreshToken)> GenerateTokensAsync(UserModel user);

        /// <summary>
        /// Refresca un token existente
        /// </summary>
        Task<(string NewToken, string NewRefreshToken)?> RefreshTokenAsync(string token, string refreshToken);

        /// <summary>
        /// Revoca un token
        /// </summary>
        Task<bool> RevokeTokenAsync(string token);

        /// <summary>
        /// Valida un token JWT
        /// </summary>
        Task<bool> ValidateTokenAsync(string token);

        /// <summary>
        /// Obtiene los claims de un token
        /// </summary>
        Task<ClaimsPrincipal?> ValidateAndGetPrincipalAsync(string token);
    }


    public class TokenService : ITokenService
    {
        private readonly ISecretService _secretService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<TokenService> _logger;

        public TokenService(
            ISecretService secretService,
            IConfiguration configuration,
            ILogger<TokenService> logger)
        {
            _secretService = secretService;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<(string Token, string RefreshToken)> GenerateTokensAsync(UserModel user)
        {
            try
            {
                var jwtConfig = _configuration.GetSection("JWT");
                var key = new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(jwtConfig["Secret"]!));

                var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

                var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, user.ID.ToString()),
                new(ClaimTypes.Name, $"{user.FirstName} {user.LastName}"),
                new(ClaimTypes.Email, user.Email),
                new("TenantId", user.TenantId.ToString()),
                new(ClaimTypes.Role, user.Role.Name)
            };

                var token = new JwtSecurityToken(
                    issuer: jwtConfig["ValidIssuer"],
                    audience: jwtConfig["ValidAudience"],
                    claims: claims,
                    expires: DateTime.UtcNow.AddHours(1),
                    signingCredentials: credentials
                );

                var tokenHandler = new JwtSecurityTokenHandler();
                var refreshToken = GenerateRefreshTokenString();

                _logger.LogInformation("Token generado exitosamente para usuario {UserId}", user.ID);

                return (tokenHandler.WriteToken(token), refreshToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generando tokens para usuario {UserId}", user.ID);
                throw;
            }
        }

        public async Task<(string NewToken, string NewRefreshToken)?> RefreshTokenAsync(string token, string refreshToken)
        {
            try
            {
                var principal = await ValidateAndGetPrincipalAsync(token);
                if (principal == null)
                {
                    return null;
                }

                var user = new UserModel
                {
                    ID = long.Parse(principal.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0"),
                    Username = principal.FindFirst(ClaimTypes.Name)?.Value ?? string.Empty,
                    Email = principal.FindFirst(ClaimTypes.Email)?.Value ?? string.Empty,
                    TenantId = long.Parse(principal.FindFirst("TenantId")?.Value ?? "0"),
                    Role = new RoleModel { Name = principal.FindFirst(ClaimTypes.Role)?.Value ?? string.Empty }
                };

                return await GenerateTokensAsync(user);
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
                var principal = await ValidateAndGetPrincipalAsync(token);
                return principal != null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error revoking token");
                return false;
            }
        }

        public async Task<bool> ValidateTokenAsync(string token)
        {
            try
            {
                var principal = await ValidateAndGetPrincipalAsync(token);
                return principal != null;
            }
            catch
            {
                return false;
            }
        }

        public async Task<ClaimsPrincipal?> ValidateAndGetPrincipalAsync(string token)
        {
            try
            {
                // Extraer el TenantId del token
                var handler = new JwtSecurityTokenHandler();
                var jsonToken = handler.ReadToken(token) as JwtSecurityToken;

                // Obtener el TenantId de los claims
                var tenantIdClaim = jsonToken?.Claims
                    .FirstOrDefault(claim => claim.Type == "TenantId");

                if (tenantIdClaim == null)
                {
                    _logger.LogWarning("No se encontró el TenantId en el token");
                    return null;
                }

                long tenantId;
                if (!long.TryParse(tenantIdClaim.Value, out tenantId))
                {
                    _logger.LogWarning("No se pudo parsear el TenantId: {TenantId}", tenantIdClaim.Value);
                    return null;
                }

                // Obtener el secreto específico del tenant
                var jwtSecret = await _secretService.GetSecretValueAsync("JWT_SECRET", tenantId);

                if (string.IsNullOrEmpty(jwtSecret))
                {
                    _logger.LogError("No se encontró el secreto JWT para el tenant {TenantId}", tenantId);
                    throw new InvalidOperationException($"JWT secret not configured for tenant {tenantId}");
                }

                // Configurar parámetros de validación
                var tokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                };

                // Validar token
                var principal = handler.ValidateToken(token, tokenValidationParameters, out _);

                return principal;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al validar el token para el tenant");
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
