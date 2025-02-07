using GenesisFEPortalWebApp.BL.Repositories;
using GenesisFEPortalWebApp.Models.Entities.Security;
using GenesisFEPortalWebApp.Models.Models.User;
using GenesisFEPortalWebApp.Models.Services;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GenesisFEPortalWebApp.BL.Services
{
    /// <summary>
    /// Interfaz para el servicio de usuarios.
    /// </summary>
    public interface IUserService
    {
        Task<List<UserListDto>> GetUsersAsync();
        Task<UserDetailDto?> GetUserByIdAsync(long id);
        Task<(bool Success, string? ErrorMessage)> CreateUserAsync(CreateUserDto model);
        Task<(bool Success, string? ErrorMessage)> UpdateUserAsync(UpdateUserDto model);
        Task<(bool Success, string? ErrorMessage)> UpdatePasswordAsync(long userId, string newPassword);
        Task<bool> DeleteUserAsync(long id);
        Task<bool> ToggleUserStatusAsync(long id);
        Task<List<RoleModel>> GetAvailableRolesAsync();
    }

    /// <summary>
    /// Servicio para la gestión de usuarios.
    /// </summary>
    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepository;
        private readonly ITenantService _tenantService;
        private readonly IPasswordHasher _passwordHasher;
        private readonly ILogger<UserService> _logger;

        /// <summary>
        /// Constructor del servicio de usuarios.
        /// </summary>
        public UserService(
            IUserRepository userRepository,
            ITenantService tenantService,
            IPasswordHasher passwordHasher,
            ILogger<UserService> logger)
        {
            _userRepository = userRepository;
            _tenantService = tenantService;
            _passwordHasher = passwordHasher;
            _logger = logger;
        }

        /// <summary>
        /// Obtiene la lista de usuarios del tenant actual.
        /// </summary>
        public async Task<List<UserListDto>> GetUsersAsync()
        {
            var tenantId = _tenantService.GetCurrentTenantId();
            var users = await _userRepository.GetUsersByTenantIdAsync(tenantId);

            return users.Select(u => new UserListDto
            {
                Id = u.ID,
                Email = u.Email,
                Username = u.Username,
                FirstName = u.FirstName ?? string.Empty,
                LastName = u.LastName ?? string.Empty,
                RoleId = u.RoleId,  // Agregamos el mapeo del RoleId
                RoleName = u.Role.Name,
                IsActive = u.IsActive,
                CreatedAt = u.CreatedAt,
                LastLoginDate = u.LastLoginDate
            }).ToList();
        }

        /// <summary>
        /// Obtiene un usuario por su ID.
        /// </summary>
        public async Task<UserDetailDto?> GetUserByIdAsync(long id)
        {
            var tenantId = _tenantService.GetCurrentTenantId();
            var user = await _userRepository.GetUserByIdAsync(id, tenantId);

            if (user == null) return null;

            return new UserDetailDto
            {
                Id = user.ID,
                Email = user.Email,
                Username = user.Username,
                FirstName = user.FirstName ?? string.Empty,
                LastName = user.LastName ?? string.Empty,
                RoleId = user.RoleId,
                RoleName = user.Role?.Name ?? string.Empty,
                IsActive = user.IsActive,
                CreatedAt = user.CreatedAt,
                LastLoginDate = user.LastLoginDate
            };
        }

        /// <summary>
        /// Crea un nuevo usuario.
        /// </summary>
        public async Task<(bool Success, string? ErrorMessage)> CreateUserAsync(CreateUserDto model)
        {
            try
            {
                var tenantId = _tenantService.GetCurrentTenantId();

                // Validar email único en el tenant
                if (await _userRepository.EmailExistsInTenantAsync(model.Email, tenantId))
                {
                    return (false, "El email ya está registrado en este tenant");
                }

                // Crear el usuario
                var user = new UserModel
                {
                    Email = model.Email.Trim(),
                    Username = model.Email.Trim(),
                    FirstName = model.FirstName.Trim(),
                    LastName = model.LastName.Trim(),
                    PasswordHash = _passwordHasher.HashPassword(model.Password),
                    RoleId = model.RoleId,
                    TenantId = tenantId,
                    EmailConfirmed = true
                };

                await _userRepository.CreateUserAsync(user);
                return (true, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creando usuario");
                return (false, "Error interno del servidor");
            }
        }

        /// <summary>
        /// Actualiza un usuario existente.
        /// </summary>
        public async Task<(bool Success, string? ErrorMessage)> UpdateUserAsync(UpdateUserDto model)
        {
            try
            {
                var tenantId = _tenantService.GetCurrentTenantId();

                // Verificar si el usuario existe
                var user = await _userRepository.GetUserByIdAsync(model.Id, tenantId);
                if (user == null)
                {
                    return (false, "Usuario no encontrado");
                }

                // Validar email único
                if (await _userRepository.EmailExistsInTenantAsync(model.Email, tenantId, model.Id))
                {
                    return (false, "El email ya está en uso por otro usuario");
                }

                // Actualizar datos
                user.FirstName = model.FirstName.Trim();
                user.LastName = model.LastName.Trim();
                user.Email = model.Email.Trim();
                user.Username = model.Email.Trim();
                user.RoleId = model.RoleId;

                // Actualizar contraseña si se proporcionó una nueva
                if (!string.IsNullOrEmpty(model.Password))
                {
                    user.PasswordHash = _passwordHasher.HashPassword(model.Password);
                    user.LastPasswordChangeDate = DateTime.UtcNow;
                    user.SecurityStamp = Guid.NewGuid().ToString();
                }

                await _userRepository.UpdateUserAsync(user);
                return (true, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error actualizando usuario {UserId}", model.Id);
                return (false, "Error interno del servidor");
            }
        }

        /// <summary>
        /// Actualiza la contraseña de un usuario.
        /// </summary>
        public async Task<(bool Success, string? ErrorMessage)> UpdatePasswordAsync(long userId, string newPassword)
        {
            try
            {
                var tenantId = _tenantService.GetCurrentTenantId();
                var passwordHash = _passwordHasher.HashPassword(newPassword);

                var success = await _userRepository.UpdatePasswordAsync(userId, tenantId, passwordHash);
                return success ? (true, null) : (false, "Usuario no encontrado");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error actualizando contraseña del usuario {UserId}", userId);
                return (false, "Error interno del servidor");
            }
        }

        /// <summary>
        /// Elimina un usuario por su ID.
        /// </summary>
        public async Task<bool> DeleteUserAsync(long id)
        {
            var tenantId = _tenantService.GetCurrentTenantId();
            return await _userRepository.DeleteUserAsync(id, tenantId);
        }

        /// <summary>
        /// Alterna el estado de activación de un usuario.
        /// </summary>
        public async Task<bool> ToggleUserStatusAsync(long id)
        {
            var tenantId = _tenantService.GetCurrentTenantId();
            return await _userRepository.ToggleUserStatusAsync(id, tenantId);
        }

        /// <summary>
        /// Obtiene la lista de roles disponibles para el tenant actual.
        /// </summary>
        public async Task<List<RoleModel>> GetAvailableRolesAsync()
        {
            var tenantId = _tenantService.GetCurrentTenantId();
            return await _userRepository.GetAvailableRolesAsync(tenantId);
        }
    }
}
