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
        private readonly ITenantRepository _tenantRepository;
        private readonly ITenantService _tenantService;
        private readonly IPasswordHasher _passwordHasher;
        private readonly ITokenService _tokenService;
        private readonly IAuthAuditLogger _authAuditLogger;
        private readonly ILogger<AuthService> _logger;

        public AuthService(
            IAuthRepository authRepository,
            ITenantRepository tenantRepository,
            ITenantService tenantService,
            IPasswordHasher passwordHasher,
            ITokenService tokenService,
            IAuthAuditLogger authAuditLogger,
            ILogger<AuthService> logger)
        {
            _authRepository = authRepository;
            _tenantRepository = tenantRepository;
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

                if (await _authRepository.IsUserLockedOutAsync(user.ID))
                {
                    await _authAuditLogger.LogLoginAttempt(model.Email, false, "Cuenta bloqueada");
                    throw new AccountLockedException("La cuenta está temporalmente bloqueada por múltiples intentos fallidos");
                }

                if (!_passwordHasher.VerifyPassword(model.Password, user.PasswordHash))
                {
                    await HandleFailedLogin(user.ID);
                    await _authAuditLogger.LogLoginAttempt(model.Email, false, "Contraseña incorrecta");
                    return (null, null, null);
                }

                await HandleSuccessfulLogin(user.ID);
                var (token, refreshToken) = await GenerateAuthTokens(user);

                return (user, token, refreshToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error durante el proceso de login para {Email}", model.Email);
                throw;
            }
        }

        public async Task<(bool Success, string? ErrorMessage)> RegisterUserAsync(RegisterUserDto model)
        {
            try
            {
                var currentTenantId = _tenantService.GetCurrentTenantId();
                if (currentTenantId == 0)
                {
                    return (false, "No hay un tenant válido en la sesión actual");
                }

                var tenant = await _tenantRepository.GetByIdAsync(currentTenantId);
                if (tenant == null || !tenant.IsActive)
                {
                    return (false, "El tenant no está activo o no existe");
                }

                if (await _authRepository.EmailExistsInTenantAsync(model.Email, currentTenantId))
                {
                    return (false, "El email ya está registrado en este tenant");
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
                    TenantId = currentTenantId,
                    EmailConfirmed = true,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    LastPasswordChangeDate = DateTime.UtcNow,
                    SecurityStamp = Guid.NewGuid().ToString()
                };

                await _authRepository.CreateUserAsync(user);
                await _authRepository.SaveChangesAsync();

                await _authAuditLogger.LogLoginAttempt(
                    user.Email,
                    true,
                    $"Usuario registrado en tenant {tenant.Name}");

                return (true, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error registrando usuario {Email}", model.Email);
                return (false, "Error interno del servidor");
            }
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

            var user = await _authRepository.GetUserByEmailAsync(
                principal.FindFirst(ClaimTypes.Email)?.Value ?? string.Empty);

            if (user == null)
            {
                return (null, null);
            }

            var (newToken, newRefreshToken) = await GenerateAuthTokens(user);
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
            await _authRepository.RevokeAllActiveRefreshTokensAsync(userId);

            return true;
        }

        private async Task HandleFailedLogin(long userId)
        {
            await _authRepository.IncrementAccessFailedCountAsync(userId);
            var failedCount = await _authRepository.GetAccessFailedCountAsync(userId);

            if (failedCount >= MaxFailedAttempts)
            {
                await _authRepository.UpdateUserLockoutAsync(userId, DateTime.UtcNow.Add(LockoutDuration));
                _logger.LogWarning("Usuario bloqueado por múltiples intentos fallidos: {UserId}", userId);
            }
        }

        private async Task HandleSuccessfulLogin(long userId)
        {
            await _authRepository.ResetAccessFailedCountAsync(userId);
            await _authRepository.UpdateUserLockoutAsync(userId, null);
            await _authRepository.UpdateUserLastLoginAsync(userId, DateTime.UtcNow);
            await _authRepository.UpdateUserSecurityStampAsync(userId, Guid.NewGuid().ToString());
        }

        private async Task<(string Token, string RefreshToken)> GenerateAuthTokens(UserModel user)
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

            return (token, refreshToken);
        }
    }
}
}
