using GenesisFEPortalWebApp.BL.Services;
using GenesisFEPortalWebApp.Models.Entities.Core;
using GenesisFEPortalWebApp.Models.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Runtime.Intrinsics.X86;
using System.Security.Claims;

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


        [HttpGet("{id}")]
        public async Task<ActionResult<BaseResponseModel>> GetCustomer(long id)
        {
            try
            {
                // Logging de claims del usuario actual
                var claims = User.Claims;
                foreach (var claim in claims)
                {
                    _logger.LogWarning($"Claim en GetCustomer - Type: {claim.Type}, Value: {claim.Value}");
                }

                var tenantIdClaim = User.FindFirst("TenantId");
                if (tenantIdClaim == null)
                {
                    _logger.LogError("No se encontró el claim de TenantId");
                    return Unauthorized();
                }

                _logger.LogWarning($"TenantId del usuario: {tenantIdClaim.Value}");
                var customer = await _customerService.GetCustomerByIdAsync(id);

                if (customer == null)
                {
                    _logger.LogWarning($"Cliente no encontrado - ID: {id}, TenantId: {tenantIdClaim.Value}");
                    return NotFound(new BaseResponseModel
                    {
                        Success = false,
                        ErrorMessage = "Cliente no encontrado"
                    });
                }

                return Ok(new BaseResponseModel { Success = true, Data = customer });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener cliente por ID");
                return StatusCode(500, new BaseResponseModel
                {
                    Success = false,
                    ErrorMessage = "Error interno del servidor"
                });
            }
        }

        [HttpPost]
        public async Task<ActionResult<BaseResponseModel>> CreateCustomer(CustomerModel customer)
        {
            try
            {
                // Logging de claims del usuario actual
                var claims = User.Claims;
                foreach (var claim in claims)
                {
                    _logger.LogWarning($"Claim en CreateCustomer - Type: {claim.Type}, Value: {claim.Value}");
                }

                var tenantIdClaim = User.FindFirst("TenantId");
                if (tenantIdClaim == null)
                {
                    _logger.LogError("No se encontró el claim de TenantId");
                    return Unauthorized();
                }

                _logger.LogWarning($"TenantId del usuario: {tenantIdClaim.Value}");
                var (success, errorMessage, createdCustomer) = await _customerService.CreateCustomerAsync(customer);

                if (!success)
                {
                    _logger.LogWarning($"Error al crear cliente: {errorMessage}");
                    return BadRequest(new BaseResponseModel
                    {
                        Success = false,
                        ErrorMessage = errorMessage
                    });
                }

                _logger.LogInformation($"Cliente creado exitosamente - ID: {createdCustomer?.ID}");
                return Ok(new BaseResponseModel { Success = true, Data = createdCustomer });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear cliente");
                return StatusCode(500, new BaseResponseModel
                {
                    Success = false,
                    ErrorMessage = "Error interno del servidor"
                });
            }
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<BaseResponseModel>> UpdateCustomer(long id, CustomerModel customer)
        {
            try
            {
                // Logging de claims del usuario actual
                var claims = User.Claims;
                foreach (var claim in claims)
                {
                    _logger.LogWarning($"Claim en UpdateCustomer - Type: {claim.Type}, Value: {claim.Value}");
                }

                var tenantIdClaim = User.FindFirst("TenantId");
                if (tenantIdClaim == null)
                {
                    _logger.LogError("No se encontró el claim de TenantId");
                    return Unauthorized();
                }

                if (id != customer.ID)
                {
                    _logger.LogWarning($"ID no coincide - URL: {id}, Body: {customer.ID}");
                    return BadRequest(new BaseResponseModel
                    {
                        Success = false,
                        ErrorMessage = "ID no coincide"
                    });
                }

                _logger.LogWarning($"TenantId del usuario: {tenantIdClaim.Value}");
                var (success, errorMessage, updatedCustomer) = await _customerService.UpdateCustomerAsync(customer);

                if (!success)
                {
                    _logger.LogWarning($"Error al actualizar cliente: {errorMessage}");
                    return BadRequest(new BaseResponseModel
                    {
                        Success = false,
                        ErrorMessage = errorMessage
                    });
                }

                _logger.LogInformation($"Cliente actualizado exitosamente - ID: {updatedCustomer?.ID}");
                return Ok(new BaseResponseModel { Success = true, Data = updatedCustomer });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al actualizar cliente");
                return StatusCode(500, new BaseResponseModel
                {
                    Success = false,
                    ErrorMessage = "Error interno del servidor"
                });
            }
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult<BaseResponseModel>> DeleteCustomer(long id)
        {
            try
            {
                // Logging de claims del usuario actual
                var claims = User.Claims;
                foreach (var claim in claims)
                {
                    _logger.LogWarning($"Claim en DeleteCustomer - Type: {claim.Type}, Value: {claim.Value}");
                }

                var tenantIdClaim = User.FindFirst("TenantId");
                if (tenantIdClaim == null)
                {
                    _logger.LogError("No se encontró el claim de TenantId");
                    return Unauthorized();
                }

                _logger.LogWarning($"TenantId del usuario: {tenantIdClaim.Value}");
                var (success, errorMessage) = await _customerService.DeleteCustomerAsync(id);

                if (!success)
                {
                    _logger.LogWarning($"Error al eliminar cliente: {errorMessage}");
                    return BadRequest(new BaseResponseModel
                    {
                        Success = false,
                        ErrorMessage = errorMessage
                    });
                }

                _logger.LogInformation($"Cliente eliminado exitosamente - ID: {id}");
                return Ok(new BaseResponseModel { Success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al eliminar cliente");
                return StatusCode(500, new BaseResponseModel
                {
                    Success = false,
                    ErrorMessage = "Error interno del servidor"
                });
            }
        }

    }
}

