using GenesisFEPortalWebApp.BL.Services;
using GenesisFEPortalWebApp.Models.Entities.Core;
using GenesisFEPortalWebApp.Models.Models;
using GenesisFEPortalWebApp.Models.Models.Customer;
using GenesisFEPortalWebApp.Models.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

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

        [HttpPost]
        public async Task<ActionResult<BaseResponseModel>> CreateCustomer([FromBody] CreateCustomerDto customerDto)
        {
            try
            {
                // Delegate mapping and creation to the service
                var createdCustomer = await _customerService.CreateCustomerAsync(customerDto);

                return Ok(new BaseResponseModel
                {
                    Success = true,
                    Data = createdCustomer
                });
            }
            catch (ValidationException validationEx)
            {
                // Handle validation-specific errors
                return BadRequest(new BaseResponseModel
                {
                    Success = false,
                    ErrorMessage = validationEx.Message
                });
            }
            catch (InvalidOperationException opEx)
            {
                // Handle tenant-related errors
                return Unauthorized(new BaseResponseModel
                {
                    Success = false,
                    ErrorMessage = opEx.Message
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating customer");
                return StatusCode(500, new BaseResponseModel
                {
                    Success = false,
                    ErrorMessage = "An unexpected error occurred while creating the customer"
                });
            }
        }


        [HttpGet("{id}")]
        public async Task<ActionResult<BaseResponseModel>> GetCustomerById(long id)
        {
            try
            {
                var customer = await _customerService.GetCustomerByIdAsync(id);
                if (customer == null)
                {
                    return Ok(new BaseResponseModel
                    {
                        Success = false,
                        ErrorMessage = "Cliente no encontrado"
                    });
                }

                return Ok(new BaseResponseModel
                {
                    Success = true,
                    Data = customer
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo cliente {CustomerId}", id);
                return StatusCode(500, new BaseResponseModel
                {
                    Success = false,
                    ErrorMessage = "Error interno del servidor"
                });
            }
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<BaseResponseModel>> UpdateCustomer(long id, [FromBody] UpdateCustomerDto customerDto)
        {
            if (id != customerDto.ID)
            {
                return BadRequest(new BaseResponseModel
                {
                    Success = false,
                    ErrorMessage = "ID no coincide"
                });
            }

            try
            {
                var updatedCustomer = await _customerService.UpdateCustomerAsync(customerDto);
                return Ok(new BaseResponseModel
                {
                    Success = true,
                    Data = updatedCustomer
                });
            }
            catch (ValidationException vex)
            {
                return BadRequest(new BaseResponseModel
                {
                    Success = false,
                    ErrorMessage = vex.Message
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error actualizando cliente {CustomerId}", id);
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
                var result = await _customerService.DeleteCustomerAsync(id);
                return Ok(new BaseResponseModel
                {
                    Success = result,
                    ErrorMessage = result ? null : "No se pudo eliminar el cliente",
                    Data = result  // Es importante incluir el resultado en Data
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error eliminando cliente {CustomerId}", id);
                return StatusCode(500, new BaseResponseModel
                {
                    Success = false,
                    ErrorMessage = "Error interno del servidor"
                });
            }
        }

        [HttpPatch("{id}/activate")]
        public async Task<ActionResult<BaseResponseModel>> ActivateCustomer(long id)
        {
            try
            {
                var success = await _customerService.ActivateCustomerAsync(id);

                return Ok(new BaseResponseModel
                {
                    Success = success,
                    ErrorMessage = success ? null : "No se pudo activar el cliente",
                    Data = success
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error activando cliente {CustomerId}", id);
                return StatusCode(500, new BaseResponseModel
                {
                    Success = false,
                    ErrorMessage = "Error interno del servidor"
                });
            }
        }

    }
}

