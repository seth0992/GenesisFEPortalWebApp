using GenesisFEPortalWebApp.BL.Repositories;
using GenesisFEPortalWebApp.Models.Entities.Security;
using GenesisFEPortalWebApp.Models.Models.Auth;
using GenesisFEPortalWebApp.Models.Services;
using Microsoft.Extensions.Configuration;
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
        private readonly IAuthRepository _authRepository;
        private readonly IConfiguration _configuration;
        private readonly ITenantService _tenantService;
        private readonly IPasswordHasher _passwordHasher;
        private readonly ITokenService _tokenService;

        public AuthService(
            IAuthRepository authRepository,
            IConfiguration configuration,
            ITenantService tenantService,
            IPasswordHasher passwordHasher,
            ITokenService tokenService)
        {
            _authRepository = authRepository;
            _configuration = configuration;
            _tenantService = tenantService;
            _passwordHasher = passwordHasher;
            _tokenService = tokenService;
        }

        public async Task<(UserModel? User, string? Token, string? RefreshToken)> LoginAsync(LoginDto model)
        {
            var user = await _authRepository.GetUserByEmailAsync(model.Email);

            if (user == null || !_passwordHasher.VerifyPassword(model.Password, user.PasswordHash))
            {
                return (null, null, null);
            }

            // Verificar si el usuario y el tenant están activos
            if (!user.IsActive || !user.Tenant.IsActive)
            {
                return (null, null, null);
            }

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
