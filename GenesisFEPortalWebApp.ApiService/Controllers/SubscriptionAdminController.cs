using GenesisFEPortalWebApp.BL.Services;
using GenesisFEPortalWebApp.Models.Models;
using GenesisFEPortalWebApp.Models.Models.Subscription;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace GenesisFEPortalWebApp.ApiService.Controllers
{
    [Route("api/[controller]")]
    [AllowAnonymous]
    [ApiController]
    public class SubscriptionAdminController : ControllerBase
    {
        private readonly ISubscriptionService _subscriptionService;
        private readonly ILogger<SubscriptionAdminController> _logger;

        public SubscriptionAdminController(
       ISubscriptionService subscriptionService,
       ILogger<SubscriptionAdminController> logger)
        {
            _subscriptionService = subscriptionService;          
            _logger = logger;
        }

        [HttpPost("register")]
        //[Authorize(Roles = "SuperAdmin")]
        public async Task<ActionResult<BaseResponseModel>> RegisterTenantWithSubscription(
            [FromBody] RegisterTenantWithSubscriptionDto model)
        {
            try
            {
                var result = await _subscriptionService.RegisterTenantWithSubscriptionAsync(model);
                return Ok(new BaseResponseModel { Success = result.Success, ErrorMessage = result.ErrorMessage });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en registro de tenant con suscripción");
                return Ok(new BaseResponseModel { Success = false, ErrorMessage = "Error procesando la solicitud" });
            }
        }

        [HttpGet("plans")]
        public async Task<ActionResult<BaseResponseModel>> GetSubscriptionPlans()
        {
            var plans = await _subscriptionService.GetActiveSubscriptionTypesAsync();
            return Ok(new BaseResponseModel { Success = true, Data = plans });
        }
    }
}
