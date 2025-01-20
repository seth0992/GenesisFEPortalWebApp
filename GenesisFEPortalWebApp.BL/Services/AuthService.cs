using GenesisFEPortalWebApp.BL.Repositories;
using GenesisFEPortalWebApp.Models.Entities.Security;
using GenesisFEPortalWebApp.Models.Exceptions;
using GenesisFEPortalWebApp.Models.Models.Auth;
using GenesisFEPortalWebApp.Models.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace GenesisFEPortalWebApp.BL.Services
{
    public interface IAuthService
    {
        Task<(UserModel? User, string? Token, string? RefreshToken)> LoginAsync(LoginDto model);
        Task<(bool Success, string? ErrorMessage)> RegisterUserAsync(RegisterUserDto model);
        Task<(string? Token, string? RefreshToken)> RefreshTokenAsync(string token, string refreshToken);
        Task<bool> RevokeTokenAsync(string token);
    }


    public class AuthService : IAuthService
    {
        private const int MaxFailedAttempts = 5;
        private readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);

        private readonly IAuthRepository _authRepository;
        private readonly IConfiguration _configuration;
        private readonly ITenantService _tenantService;
        private readonly IPasswordHasher _passwordHasher;
        private readonly ITokenService _tokenService;
        private readonly IAuthAuditLogger _authAuditLogger;
        private readonly ILogger<AuthService> _logger;

        public AuthService(
            IAuthRepository authRepository,
            IConfiguration configuration,
            ITenantService tenantService,
            IPasswordHasher passwordHasher,
            ITokenService tokenService,
            IAuthAuditLogger authAuditLogger,
            ILogger<AuthService> logger)
        {
            _authRepository = authRepository;
            _configuration = configuration;
            _tenantService = tenantService;
            _passwordHasher = passwordHasher;
            _tokenService = tokenService;
            _authAuditLogger = authAuditLogger;
            _logger = logger;
        }


        public async Task<(UserModel? User, string? Token, string? RefreshToken)> LoginAsync(LoginDto model)
        {
            try
            {
                var user = await _authRepository.GetUserByEmailAsync(model.Email);

                if (user == null || !user.IsActive || !user.Tenant.IsActive)
                {
                    await _authAuditLogger.LogLoginAttempt(model.Email, false, "Usuario inactivo o no encontrado");
                    return (null, null, null);
                }

                if (IsAccountLocked(user))
                {
                    await _authAuditLogger.LogLoginAttempt(model.Email, false, "Cuenta bloqueada");
                    throw new AccountLockedException("La cuenta está temporalmente bloqueada por múltiples intentos fallidos");
                }

                if (!_passwordHasher.VerifyPassword(model.Password, user.PasswordHash))
                {
                    await HandleFailedLogin(user);
                    return (null, null, null);
                }

                await HandleSuccessfulLogin(user);
                return await GenerateAuthTokens(user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error durante el proceso de login para {Email}", model.Email);
                throw;
            }
        }

        private bool IsAccountLocked(UserModel user)
        {
            return user.LockoutEnd.HasValue && user.LockoutEnd > DateTime.UtcNow;
        }

        private async Task HandleFailedLogin(UserModel user)
        {
            user.AccessFailedCount++;
            if (user.AccessFailedCount >= MaxFailedAttempts)
            {
                user.LockoutEnd = DateTime.UtcNow.Add(LockoutDuration);
                await _authAuditLogger.LogLoginAttempt(user.Email, false, "Cuenta bloqueada por múltiples intentos");
                _logger.LogWarning("Cuenta bloqueada por múltiples intentos fallidos: {Email}", user.Email);
            }
            await _authRepository.SaveChangesAsync();
        }

        private async Task HandleSuccessfulLogin(UserModel user)
        {
            user.AccessFailedCount = 0;
            user.LockoutEnd = null;
            user.LastLoginDate = DateTime.UtcNow;
            user.LastSuccessfulLogin = DateTime.UtcNow;
            user.SecurityStamp = Guid.NewGuid().ToString();

            await _authRepository.SaveChangesAsync();
            await _authAuditLogger.LogLoginAttempt(user.Email, true, "Login exitoso");
        }

        private async Task<(UserModel User, string Token, string RefreshToken)> GenerateAuthTokens(UserModel user)
        {
            var token = _tokenService.GenerateJwtToken(user);
            var refreshToken = _tokenService.GenerateRefreshToken();

            var refreshTokenEntity = new RefreshTokenModel
            {
                UserId = user.ID,
                Token = refreshToken,
                ExpiryDate = DateTime.UtcNow.AddDays(7),
                CreatedAt = DateTime.UtcNow
            };

            await _authRepository.CreateRefreshTokenAsync(refreshTokenEntity);
            await _authRepository.SaveChangesAsync();

            return (user, token, refreshToken);
        }
               
        public async Task<(bool Success, string? ErrorMessage)> RegisterUserAsync(RegisterUserDto model)
        {
            if (await _authRepository.EmailExistsAsync(model.Email))
            {
                return (false, "El email ya está registrado");
            }

            var defaultRole = await _authRepository.GetRoleByNameAsync("User");
            if (defaultRole == null)
            {
                return (false, "Error al asignar el rol");
            }

            var user = new UserModel
            {
                Email = model.Email,
                Username = model.Email,
                FirstName = model.FirstName,
                LastName = model.LastName,
                PasswordHash = _passwordHasher.HashPassword(model.Password),
                RoleId = defaultRole.ID,
                TenantId = _tenantService.GetCurrentTenantId(),
                EmailConfirmed = true,
                IsActive = true
            };

            await _authRepository.CreateUserAsync(user);
            await _authRepository.SaveChangesAsync();

            return (true, null);
        }

        public async Task<(string? Token, string? RefreshToken)> RefreshTokenAsync(string token, string refreshToken)
        {
            var principal = _tokenService.GetPrincipalFromExpiredToken(token);
            if (principal == null)
            {
                return (null, null);
            }

            var userId = long.Parse(principal.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var storedRefreshToken = await _authRepository.GetRefreshTokenAsync(userId, refreshToken);

            if (storedRefreshToken == null || storedRefreshToken.ExpiryDate < DateTime.UtcNow)
            {
                return (null, null);
            }

            var user = await _authRepository.GetUserByEmailAsync(principal.FindFirst(ClaimTypes.Email)?.Value ?? string.Empty);
            if (user == null)
            {
                return (null, null);
            }

            var newToken = _tokenService.GenerateJwtToken(user);
            var newRefreshToken = _tokenService.GenerateRefreshToken();

            // Revocar el refresh token anterior y crear uno nuevo
            storedRefreshToken.RevokedAt = DateTime.UtcNow;
            await _authRepository.UpdateRefreshTokenAsync(storedRefreshToken);

            var refreshTokenEntity = new RefreshTokenModel
            {
                UserId = userId,
                Token = newRefreshToken,
                ExpiryDate = DateTime.UtcNow.AddDays(7),
                CreatedAt = DateTime.UtcNow
            };

            await _authRepository.CreateRefreshTokenAsync(refreshTokenEntity);
            await _authRepository.SaveChangesAsync();

            return (newToken, newRefreshToken);
        }

        public async Task<bool> RevokeTokenAsync(string token)
        {
            var principal = _tokenService.GetPrincipalFromExpiredToken(token);
            if (principal == null)
            {
                return false;
            }

            var userId = long.Parse(principal.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var refreshTokens = await _authRepository.GetActiveRefreshTokensByUserIdAsync(userId);

            foreach (var refreshToken in refreshTokens)
            {
                refreshToken.RevokedAt = DateTime.UtcNow;
                await _authRepository.UpdateRefreshTokenAsync(refreshToken);
            }

            await _authRepository.SaveChangesAsync();
            return true;
        }
    }
}
