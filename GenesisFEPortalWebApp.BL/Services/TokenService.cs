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
                var jwtSecret = await _secretService.GetSecretValueAsync("JWT_SECRET", user.TenantId);
                if (string.IsNullOrEmpty(jwtSecret))
                {
                    throw new InvalidOperationException($"JWT secret not configured for tenant {user.TenantId}");
                }

                // Asegurar que la clave tenga el tamaño correcto
                var keyBytes = Convert.FromBase64String(jwtSecret);
                if (keyBytes.Length < 32) // 256 bits
                {
                    throw new InvalidOperationException("JWT secret key is too short");
                }

                var key = new byte[32];
                Buffer.BlockCopy(keyBytes, 0, key, 0, 32);

                var tokenHandler = new JwtSecurityTokenHandler();
                var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, user.ID.ToString()),
                new(ClaimTypes.Name, user.Username),
                new(ClaimTypes.Email, user.Email),
                new("TenantId", user.TenantId.ToString()),
                new(ClaimTypes.Role, user.Role.Name)
            };

                var tokenDescriptor = new SecurityTokenDescriptor
                {
                    Subject = new ClaimsIdentity(claims),
                    Expires = DateTime.UtcNow.AddHours(1),
                    SigningCredentials = new SigningCredentials(
                        new SymmetricSecurityKey(key),
                        SecurityAlgorithms.HmacSha256Signature)
                };

                var token = tokenHandler.CreateToken(tokenDescriptor);
                var refreshToken = GenerateRefreshTokenString();

                return (tokenHandler.WriteToken(token), refreshToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating tokens for user {UserId}", user.ID);
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
                var jwtSecret = await _secretService.GetSecretValueAsync("JWT_SECRET");
                if (string.IsNullOrEmpty(jwtSecret))
                {
                    throw new InvalidOperationException("JWT secret not configured");
                }

                var tokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ValidateLifetime = false
                };

                var tokenHandler = new JwtSecurityTokenHandler();
                var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out var securityToken);

                if (securityToken is not JwtSecurityToken jwtSecurityToken ||
                    !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256,
                    StringComparison.InvariantCultureIgnoreCase))
                {
                    throw new SecurityTokenException("Invalid token");
                }

                return principal;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating token");
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
