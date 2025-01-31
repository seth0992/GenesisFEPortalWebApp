using GenesisFEPortalWebApp.BL.Services;
using GenesisFEPortalWebApp.Models.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace GenesisFEPortalWebApp.ApiService.Controllers
{
    [Authorize] // Solo usuarios autenticados pueden acceder
    [Route("api/[controller]")]
    [ApiController]
    public class CustomerController : ControllerBase
    {
        private readonly ICustomerService _customerService;
        private readonly ILogger<CustomerController> _logger;

        public CustomerController(
            ICustomerService customerService,
            ILogger<CustomerController> logger)
        {
            _customerService = customerService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult<BaseResponseModel>> GetCustomers()
        {
            try
            {
                // Logging de claims del usuario actual
                var claims = User.Claims;
                foreach (var claim in claims)
                {
                    _logger.LogWarning($"Claim en GetCustomers - Type: {claim.Type}, Value: {claim.Value}");
                }

                // Obtener el TenantId directamente de los claims
                var tenantIdClaim = User.FindFirst("TenantId");
                if (tenantIdClaim == null)
                {
                    _logger.LogError("No se encontró el claim de TenantId");
                    return Unauthorized();
                }

                _logger.LogWarning($"TenantId del usuario: {tenantIdClaim.Value}");

                var customers = await _customerService.GetCustomersByCurrentTenantAsync();

                return Ok(new BaseResponseModel
                {
                    Success = true,
                    Data = customers
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error completo al obtener clientes");
                return StatusCode(500, new BaseResponseModel
                {
                    Success = false,
                    ErrorMessage = "Error interno del servidor"
                });
            }
        }
    }
}

