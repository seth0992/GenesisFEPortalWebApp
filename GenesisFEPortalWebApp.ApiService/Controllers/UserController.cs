using GenesisFEPortalWebApp.BL.Services;
using GenesisFEPortalWebApp.Models.Models.User;
using GenesisFEPortalWebApp.Models.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace GenesisFEPortalWebApp.ApiService.Controllers
{
    [Authorize(Roles = "TenantAdmin")] // Solo los administradores del tenant pueden gestionar usuarios
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly ILogger<UserController> _logger;

        public UserController(
            IUserService userService,
            ILogger<UserController> logger)
        {
            _userService = userService;
            _logger = logger;
        }

        /// <summary>
        /// Obtiene la lista de usuarios del tenant actual
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<BaseResponseModel>> GetUsers()
        {
            try
            {
                _logger.LogInformation("Obteniendo lista de usuarios");
                var users = await _userService.GetUsersAsync();

                return Ok(new BaseResponseModel
                {
                    Success = true,
                    Data = users
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo usuarios");
                return Ok(new BaseResponseModel
                {
                    Success = false,
                    ErrorMessage = "Error al obtener los usuarios"
                });
            }
        }

        /// <summary>
        /// Obtiene un usuario específico por su ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<BaseResponseModel>> GetUserById(long id)
        {
            try
            {
                _logger.LogInformation("Obteniendo usuario {UserId}", id);
                var userDetail = await _userService.GetUserByIdAsync(id);

                if (userDetail == null)
                {
                    return Ok(new BaseResponseModel
                    {
                        Success = false,
                        ErrorMessage = "Usuario no encontrado"
                    });
                }

                return Ok(new BaseResponseModel
                {
                    Success = true,
                    Data = userDetail
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo usuario {UserId}", id);
                return Ok(new BaseResponseModel
                {
                    Success = false,
                    ErrorMessage = "Error al obtener el usuario"
                });
            }
        }
        /// <summary>
        /// Crea un nuevo usuario en el tenant actual
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<BaseResponseModel>> CreateUser([FromBody] CreateUserDto model)
        {
            try
            {
                _logger.LogInformation("Creando nuevo usuario con email {Email}", model.Email);
                var (success, errorMessage) = await _userService.CreateUserAsync(model);

                if (!success)
                {
                    return Ok(new BaseResponseModel
                    {
                        Success = false,
                        ErrorMessage = errorMessage ?? "Error al crear el usuario"
                    });
                }

                return Ok(new BaseResponseModel
                {
                    Success = true,
                    Data = "Usuario creado exitosamente"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creando usuario");
                return Ok(new BaseResponseModel
                {
                    Success = false,
                    ErrorMessage = "Error interno del servidor"
                });
            }
        }

        /// <summary>
        /// Actualiza un usuario existente
        /// </summary>
        [HttpPut("{id}")]
        public async Task<ActionResult<BaseResponseModel>> UpdateUser(long id, [FromBody] UpdateUserDto model)
        {
            try
            {
                if (id != model.Id)
                {
                    return Ok(new BaseResponseModel
                    {
                        Success = false,
                        ErrorMessage = "ID no coincide"
                    });
                }

                _logger.LogInformation("Actualizando usuario {UserId}", id);
                var (success, errorMessage) = await _userService.UpdateUserAsync(model);

                if (!success)
                {
                    return Ok(new BaseResponseModel
                    {
                        Success = false,
                        ErrorMessage = errorMessage ?? "Error al actualizar el usuario"
                    });
                }

                return Ok(new BaseResponseModel
                {
                    Success = true,
                    Data = "Usuario actualizado exitosamente"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error actualizando usuario {UserId}", id);
                return Ok(new BaseResponseModel
                {
                    Success = false,
                    ErrorMessage = "Error interno del servidor"
                });
            }
        }

        /// <summary>
        /// Actualiza la contraseña de un usuario
        /// </summary>
        [HttpPut("{id}/password")]
        public async Task<ActionResult<BaseResponseModel>> UpdatePassword(long id, [FromBody] UpdatePasswordDto model)
        {
            try
            {
                _logger.LogInformation("Actualizando contraseña del usuario {UserId}", id);
                var (success, errorMessage) = await _userService.UpdatePasswordAsync(id, model.NewPassword);

                if (!success)
                {
                    return Ok(new BaseResponseModel
                    {
                        Success = false,
                        ErrorMessage = errorMessage ?? "Error al actualizar la contraseña"
                    });
                }

                return Ok(new BaseResponseModel
                {
                    Success = true,
                    Data = "Contraseña actualizada exitosamente"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error actualizando contraseña del usuario {UserId}", id);
                return Ok(new BaseResponseModel
                {
                    Success = false,
                    ErrorMessage = "Error interno del servidor"
                });
            }
        }

        /// <summary>
        /// Elimina (desactiva) un usuario
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<ActionResult<BaseResponseModel>> DeleteUser(long id)
        {
            try
            {
                _logger.LogInformation("Eliminando usuario {UserId}", id);
                var success = await _userService.DeleteUserAsync(id);

                if (!success)
                {
                    return Ok(new BaseResponseModel
                    {
                        Success = false,
                        ErrorMessage = "Usuario no encontrado"
                    });
                }

                return Ok(new BaseResponseModel
                {
                    Success = true,
                    Data = "Usuario eliminado exitosamente"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error eliminando usuario {UserId}", id);
                return Ok(new BaseResponseModel
                {
                    Success = false,
                    ErrorMessage = "Error interno del servidor"
                });
            }
        }

        /// <summary>
        /// Activa o desactiva un usuario
        /// </summary>
        [HttpPatch("{id}/toggle-status")]
        public async Task<ActionResult<BaseResponseModel>> ToggleUserStatus(long id)
        {
            try
            {
                _logger.LogInformation("Cambiando estado del usuario {UserId}", id);
                var success = await _userService.ToggleUserStatusAsync(id);

                if (!success)
                {
                    return Ok(new BaseResponseModel
                    {
                        Success = false,
                        ErrorMessage = "Usuario no encontrado"
                    });
                }

                return Ok(new BaseResponseModel
                {
                    Success = true,
                    Data = "Estado del usuario actualizado exitosamente"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cambiando estado del usuario {UserId}", id);
                return Ok(new BaseResponseModel
                {
                    Success = false,
                    ErrorMessage = "Error interno del servidor"
                });
            }
        }

        /// <summary>
        /// Obtiene los roles disponibles para asignar a usuarios
        /// </summary>
        [HttpGet("available-roles")]
        public async Task<ActionResult<BaseResponseModel>> GetAvailableRoles()
        {
            try
            {
                _logger.LogInformation("Obteniendo roles disponibles");
                var roles = await _userService.GetAvailableRolesAsync();

                return Ok(new BaseResponseModel
                {
                    Success = true,
                    Data = roles
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo roles disponibles");
                return Ok(new BaseResponseModel
                {
                    Success = false,
                    ErrorMessage = "Error al obtener los roles"
                });
            }
        }
    }
}
