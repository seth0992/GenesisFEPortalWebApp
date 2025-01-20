using GenesisFEPortalWebApp.BL.Services;
using GenesisFEPortalWebApp.Models.Models.Auth;
using GenesisFEPortalWebApp.Models.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace GenesisFEPortalWebApp.ApiService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(IAuthService authService, ILogger<AuthController> logger)
        {
            _authService = authService;
            _logger = logger;
        }

        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<ActionResult<BaseResponseModel>> Login([FromBody] LoginDto model)
        {
            try
            {
                var (user, token, refreshToken) = await _authService.LoginAsync(model);

                if (user == null || token == null)
                {
                    return Ok(new BaseResponseModel
                    {
                        Success = false,
                        ErrorMessage = "Credenciales inválidas"
                    });
                }

                var response = new BaseResponseModel
                {
                    Success = true,
                    Data = new
                    {
                        user = new UserDto
                        {
                            Id = user.ID,
                            Email = user.Email,
                            Username = user.Username,
                            FirstName = user.FirstName,
                            LastName = user.LastName,
                            RoleName = user.Role.Name,
                            TenantName = user.Tenant.Name
                        },
                        token,
                        refreshToken
                    }
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en login");
                return Ok(new BaseResponseModel
                {
                    Success = false,
                    ErrorMessage = "Error al procesar la solicitud"
                });
            }
        }

        [HttpPost("register")]
        [AllowAnonymous]
        public async Task<ActionResult<BaseResponseModel>> Register([FromBody] RegisterUserDto model)
        {
            try
            {
                var (success, errorMessage) = await _authService.RegisterUserAsync(model);

                if (!success)
                {
                    return Ok(new BaseResponseModel
                    {
                        Success = false,
                        ErrorMessage = errorMessage ?? "Error al registrar el usuario"
                    });
                }

                return Ok(new BaseResponseModel
                {
                    Success = true,
                    Data = "Usuario registrado exitosamente"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en registro de usuario");
                return Ok(new BaseResponseModel
                {
                    Success = false,
                    ErrorMessage = "Error al procesar la solicitud"
                });
            }
        }

        [HttpPost("refresh-token")]
        public async Task<ActionResult<BaseResponseModel>> RefreshToken([FromBody] RefreshTokenRequest request)
        {
            try
            {
                var (token, refreshToken) = await _authService.RefreshTokenAsync(
                    request.Token,
                    request.RefreshToken);

                if (token == null)
                {
                    return Ok(new BaseResponseModel
                    {
                        Success = false,
                        ErrorMessage = "Token inválido o expirado"
                    });
                }

                return Ok(new BaseResponseModel
                {
                    Success = true,
                    Data = new { token, refreshToken }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en refresh token");
                return Ok(new BaseResponseModel
                {
                    Success = false,
                    ErrorMessage = "Error al procesar la solicitud"
                });
            }
        }

        [HttpPost("revoke")]
        [Authorize]
        public async Task<ActionResult<BaseResponseModel>> RevokeToken([FromBody] RevokeTokenRequest request)
        {
            try
            {
                var success = await _authService.RevokeTokenAsync(request.Token);

                if (!success)
                {
                    return Ok(new BaseResponseModel
                    {
                        Success = false,
                        ErrorMessage = "Token inválido o expirado"
                    });
                }

                return Ok(new BaseResponseModel
                {
                    Success = true,
                    Data = "Token revocado exitosamente"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en revoke token");
                return Ok(new BaseResponseModel
                {
                    Success = false,
                    ErrorMessage = "Error al procesar la solicitud"
                });
            }
        }
    }
}
