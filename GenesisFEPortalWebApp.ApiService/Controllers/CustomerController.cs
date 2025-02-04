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

    }
}

