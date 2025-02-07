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
    /// <summary>
    /// Interfaz para el repositorio de usuarios.
    /// </summary>
    public interface IUserRepository
    {
        Task<List<UserModel>> GetUsersByTenantIdAsync(long tenantId);
        Task<UserModel?> GetUserByIdAsync(long id, long tenantId);
        Task<bool> EmailExistsInTenantAsync(string email, long tenantId, long? excludeUserId = null);
        Task<UserModel> CreateUserAsync(UserModel user);
        Task<UserModel> UpdateUserAsync(UserModel user);
        Task<bool> DeleteUserAsync(long id, long tenantId);
        Task<bool> ToggleUserStatusAsync(long id, long tenantId);
        Task<bool> UpdatePasswordAsync(long id, long tenantId, string passwordHash);
        Task<List<RoleModel>> GetAvailableRolesAsync(long tenantId);
    }

    /// <summary>
    /// Repositorio para la gestión de usuarios.
    /// </summary>
    public class UserRepository : IUserRepository
    {
        private readonly AppDbContext _context;
        private readonly ILogger<UserRepository> _logger;

        /// <summary>
        /// Constructor del repositorio de usuarios.
        /// </summary>
        /// <param name="context">Contexto de la base de datos.</param>
        /// <param name="logger">Logger para el repositorio.</param>
        public UserRepository(AppDbContext context, ILogger<UserRepository> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Obtiene una lista de usuarios por ID de tenant.
        /// </summary>
        /// <param name="tenantId">ID del tenant.</param>
        /// <returns>Lista de usuarios.</returns>
        public async Task<List<UserModel>> GetUsersByTenantIdAsync(long tenantId)
        {
            try
            {
                return await _context.Users
                    .Include(u => u.Role)  // Aseguramos que se incluya el Role
                    .Where(u => u.TenantId == tenantId)
                    .OrderBy(u => u.Email)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo usuarios del tenant {TenantId}", tenantId);
                throw;
            }
        }
        /// <summary>
        /// Obtiene un usuario por ID y ID de tenant.
        /// </summary>
        /// <param name="id">ID del usuario.</param>
        /// <param name="tenantId">ID del tenant.</param>
        /// <returns>Usuario encontrado o null.</returns>
        public async Task<UserModel?> GetUserByIdAsync(long id, long tenantId)
        {
            try
            {
                return await _context.Users
                    .Include(u => u.Role)
                    .FirstOrDefaultAsync(u => u.ID == id && u.TenantId == tenantId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo usuario {UserId} del tenant {TenantId}", id, tenantId);
                throw;
            }
        }

        /// <summary>
        /// Verifica si un email existe en un tenant específico.
        /// </summary>
        /// <param name="email">Email a verificar.</param>
        /// <param name="tenantId">ID del tenant.</param>
        /// <param name="excludeUserId">ID del usuario a excluir (opcional).</param>
        /// <returns>True si el email existe, false en caso contrario.</returns>
        public async Task<bool> EmailExistsInTenantAsync(string email, long tenantId, long? excludeUserId = null)
        {
            try
            {
                var query = _context.Users.Where(u =>
                    u.Email.ToLower() == email.ToLower() &&
                    u.TenantId == tenantId);

                if (excludeUserId.HasValue)
                {
                    query = query.Where(u => u.ID != excludeUserId.Value);
                }

                return await query.AnyAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verificando existencia de email {Email} en tenant {TenantId}", email, tenantId);
                throw;
            }
        }

        /// <summary>
        /// Crea un nuevo usuario.
        /// </summary>
        /// <param name="user">Modelo del usuario a crear.</param>
        /// <returns>Usuario creado.</returns>
        public async Task<UserModel> CreateUserAsync(UserModel user)
        {
            try
            {
                // Establecer valores por defecto
                user.CreatedAt = DateTime.UtcNow;
                user.IsActive = true;
                user.SecurityStamp = Guid.NewGuid().ToString();
                user.LastPasswordChangeDate = DateTime.UtcNow;

                await _context.Users.AddAsync(user);
                await _context.SaveChangesAsync();

                return user;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creando usuario para tenant {TenantId}", user.TenantId);
                throw;
            }
        }

        /// <summary>
        /// Actualiza un usuario existente.
        /// </summary>
        /// <param name="user">Modelo del usuario a actualizar.</param>
        /// <returns>Usuario actualizado.</returns>
        public async Task<UserModel> UpdateUserAsync(UserModel user)
        {
            try
            {
                user.UpdatedAt = DateTime.UtcNow;
                _context.Users.Update(user);
                await _context.SaveChangesAsync();

                return user;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error actualizando usuario {UserId}", user.ID);
                throw;
            }
        }

        /// <summary>
        /// Elimina un usuario de forma lógica.
        /// </summary>
        /// <param name="id">ID del usuario.</param>
        /// <param name="tenantId">ID del tenant.</param>
        /// <returns>True si la eliminación fue exitosa, false en caso contrario.</returns>
        public async Task<bool> DeleteUserAsync(long id, long tenantId)
        {
            try
            {
                var user = await GetUserByIdAsync(id, tenantId);
                if (user == null) return false;

                // Eliminación lógica
                user.IsActive = false;
                user.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error eliminando usuario {UserId}", id);
                throw;
            }
        }

        /// <summary>
        /// Cambia el estado de activación de un usuario.
        /// </summary>
        /// <param name="id">ID del usuario.</param>
        /// <param name="tenantId">ID del tenant.</param>
        /// <returns>True si el cambio fue exitoso, false en caso contrario.</returns>
        public async Task<bool> ToggleUserStatusAsync(long id, long tenantId)
        {
            try
            {
                var user = await GetUserByIdAsync(id, tenantId);
                if (user == null) return false;

                user.IsActive = !user.IsActive;
                user.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cambiando estado del usuario {UserId}", id);
                throw;
            }
        }

        /// <summary>
        /// Actualiza la contraseña de un usuario.
        /// </summary>
        /// <param name="id">ID del usuario.</param>
        /// <param name="tenantId">ID del tenant.</param>
        /// <param name="passwordHash">Nuevo hash de la contraseña.</param>
        /// <returns>True si la actualización fue exitosa, false en caso contrario.</returns>
        public async Task<bool> UpdatePasswordAsync(long id, long tenantId, string passwordHash)
        {
            try
            {
                var user = await GetUserByIdAsync(id, tenantId);
                if (user == null) return false;

                user.PasswordHash = passwordHash;
                user.LastPasswordChangeDate = DateTime.UtcNow;
                user.SecurityStamp = Guid.NewGuid().ToString();
                user.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error actualizando contraseña del usuario {UserId}", id);
                throw;
            }
        }

        /// <summary>
        /// Obtiene una lista de roles disponibles para un tenant.
        /// </summary>
        /// <param name="tenantId">ID del tenant.</param>
        /// <returns>Lista de roles disponibles.</returns>
        public async Task<List<RoleModel>> GetAvailableRolesAsync(long tenantId)
        {
            try
            {
                // Excluir roles de sistema para tenants normales
                var roles = await _context.Roles
                    //.Where(r => !r.IsSystem)
                    .OrderBy(r => r.Name)
                    .ToListAsync();

                return roles;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo roles disponibles para tenant {TenantId}", tenantId);
                throw;
            }
        }
    }
}
