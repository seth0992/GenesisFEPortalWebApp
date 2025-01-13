using GenesisFEPortalWebApp.BL.Services;
using GenesisFEPortalWebApp.Models.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace GenesisFEPortalWebApp.ApiService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CatalogController : ControllerBase
    {
        private readonly ICatalogService _catalogService;

        public CatalogController(ICatalogService catalogService)
        {
            _catalogService = catalogService;
        }

        [HttpGet("identification-types")]

        public async Task<ActionResult<BaseResponseModel>> GetIdentificationType()
        {

            try
            {
                var customers = await _catalogService.GetIdentificationTypes();
                return Ok(new BaseResponseModel { Success = true, Data = customers });
            }
            catch (Exception ex)
            {
                return Ok(new BaseResponseModel { Success = false, ErrorMessage = ex.Message });
            }

        }


        [HttpGet("provinces")]
        public async Task<ActionResult<BaseResponseModel>> GetProvinces()
        {

            try
            {
                var customers = await _catalogService.GetProvinces();
                return Ok(new BaseResponseModel { Success = true, Data = customers });
            }
            catch (Exception ex)
            {
                return Ok(new BaseResponseModel { Success = false, ErrorMessage = ex.Message });
            }

        }

        [HttpGet("cantons")]
        public async Task<ActionResult<BaseResponseModel>> GetCantons()
        {

            try
            {
                var customers = await _catalogService.GetCantons();
                return Ok(new BaseResponseModel { Success = true, Data = customers });
            }
            catch (Exception ex)
            {
                return Ok(new BaseResponseModel { Success = false, ErrorMessage = ex.Message });
            }

        }

        [HttpGet("districts")]
        public async Task<ActionResult<BaseResponseModel>> GetDistricts()
        {

            try
            {
                var customers = await _catalogService.GetDistricts();
                return Ok(new BaseResponseModel { Success = true, Data = customers });
            }
            catch (Exception ex)
            {
                return Ok(new BaseResponseModel { Success = false, ErrorMessage = ex.Message });
            }

        }

        [HttpGet("provinces/{idProvince}/cantons")]

        public async Task<ActionResult<BaseResponseModel>> GetCantonsOfProvince(int idProvince)
        {
            var product = await _catalogService.GetCantonsOfProvinces(idProvince);
            if (product == null)
            {
                return Ok(new BaseResponseModel { Success = false, ErrorMessage = "Product not found" });
            }
            return Ok(new BaseResponseModel { Success = true, Data = product });
        }

        [HttpGet("cantons/{idCanton}/districts")]

        public async Task<ActionResult<BaseResponseModel>> GetDistritsOfCantons(int idCanton)
        {
            var product = await _catalogService.GetCantonsOfDistricts(idCanton);
            if (product == null)
            {
                return Ok(new BaseResponseModel { Success = false, ErrorMessage = "Product not found" });
            }
            return Ok(new BaseResponseModel { Success = true, Data = product });
        }
    }

}
