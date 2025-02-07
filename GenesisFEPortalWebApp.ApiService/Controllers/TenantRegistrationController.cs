using GenesisFEPortalWebApp.BL.Services;
using GenesisFEPortalWebApp.Models.Models.Auth;
using GenesisFEPortalWebApp.Models.Models.Core;
using GenesisFEPortalWebApp.Models.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GenesisFEPortalWebApp.ApiService.Controllers
{
    public class TenantRegistrationController : ControllerBase
    {
        private readonly ITenantRegistrationService _tenantRegistrationService;
        private readonly IAuthService _authService;
        private readonly ILogger<TenantRegistrationController> _logger;

        public TenantRegistrationController(
            ITenantRegistrationService tenantRegistrationService,
            IAuthService authService,
            ILogger<TenantRegistrationController> logger)
        {
            _tenantRegistrationService = tenantRegistrationService;
            _authService = authService;
            _logger = logger;
        }

        [HttpPost("register")]
        [AllowAnonymous]
        public async Task<ActionResult<BaseResponseModel>> RegisterTenant([FromBody] RegisterTenantDto model)
        {
            try
            {
                var (success, errorMessage, tenant) = await _tenantRegistrationService.RegisterTenantWithAdminAsync(model);

                if (!success || tenant == null)
                {
                    return Ok(new BaseResponseModel
                    {
                        Success = false,
                        ErrorMessage = errorMessage ?? "Error al registrar el tenant"
                    });
                }

                // Realizar login automático después del registro
                var loginResult = await _authService.LoginAsync(new LoginDto
                {
                    Email = model.Email,
                    Password = model.Password
                });

                if (loginResult.User == null)
                {
                    return Ok(new BaseResponseModel
                    {
                        Success = true,
                        Data = new
                        {
                            message = "Tenant registrado exitosamente. Por favor, inicie sesión.",
                            tenantId = tenant.ID
                        }
                    });
                }

                return Ok(new BaseResponseModel
                {
                    Success = true,
                    Data = new
                    {
                        message = "Tenant registrado exitosamente",
                        tenantId = tenant.ID,
                        user = new
                        {
                            id = loginResult.User.ID,
                            email = loginResult.User.Email,
                            firstName = loginResult.User.FirstName,
                            lastName = loginResult.User.LastName
                        },
                        token = loginResult.Token,
                        refreshToken = loginResult.RefreshToken
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al registrar tenant");
                return Ok(new BaseResponseModel
                {
                    Success = false,
                    ErrorMessage = "Error al procesar la solicitud"
                });
            }
        }
    }
}
