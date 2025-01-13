using GenesisFEPortalWebApp.BL.Cache.Configuration;
using GenesisFEPortalWebApp.BL.Cache.Monitors;
using GenesisFEPortalWebApp.BL.Cache;
using GenesisFEPortalWebApp.Models.Entities.Catalog;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GenesisFEPortalWebApp.BL.Repositories;

namespace GenesisFEPortalWebApp.BL.Services
{
    public interface ICatalogService
    {
        Task<List<IdentificationTypeModel>> GetIdentificationTypes();
        Task<List<ProvinceModel>> GetProvinces();
        Task<List<CantonModel>> GetCantons();
        Task<List<DistrictModel>> GetDistricts();

        //Task<List<CustomerModel>> GetCustomers();
        Task<List<CantonModel>> GetCantonsOfProvinces(int idProvince);
        Task<List<DistrictModel>> GetCantonsOfDistricts(int idCanton);

        Task UpdateIdentificationType(IdentificationTypeModel model);
        Task UpdateProvince(ProvinceModel model);
        Task UpdateCanton(CantonModel model);
        Task UpdateDistrict(DistrictModel model);
    }
    public class CatalogService : ICatalogService
    {
        private readonly ICatalogRepository _catalogRepository;
        private readonly ICacheService _cacheService;
        private readonly CatalogChangeMonitor _changeMonitor;
        private readonly CacheOptions _cacheOptions;
        private readonly ILogger<CatalogService> _logger;

        // Claves de caché como constantes privadas
        private const string CACHE_KEY_IDENTIFICATION_TYPES = "Catalog_IdentificationTypes";
        private const string CACHE_KEY_PROVINCES = "Catalog_Provinces";
        private const string CACHE_KEY_CANTONS = "Catalog_Cantons";
        private const string CACHE_KEY_CANTONS_BY_PROVINCE = "Catalog_Cantons_Province_{0}";
        private const string CACHE_KEY_DISTRICTS = "Catalog_Districts";
        private const string CACHE_KEY_DISTRICTS_BY_CANTON = "Catalog_Districts_Canton_{0}";

        public CatalogService(
            ICatalogRepository catalogRepository,
            ICacheService cacheService,
            CatalogChangeMonitor changeMonitor,
            IOptions<CacheOptions> cacheOptions,
            ILogger<CatalogService> logger)
        {
            _catalogRepository = catalogRepository;
            _cacheService = cacheService;
            _changeMonitor = changeMonitor;
            _cacheOptions = cacheOptions.Value;
            _logger = logger;
        }

        public async Task<List<CantonModel>> GetCantons()
        {
            if (!_cacheOptions.EnableCaching)
            {
                return await _catalogRepository.GetCantons();
            }

            var cachedData = _cacheService.Get<List<CantonModel>>(CACHE_KEY_CANTONS);
            if (cachedData != null)
            {
                _logger.LogDebug("Obteniendo cantones desde caché");
                return cachedData;
            }

            _logger.LogDebug("Obteniendo cantones desde base de datos");
            var data = await _catalogRepository.GetCantons();
            _cacheService.Set(CACHE_KEY_CANTONS, data, _cacheOptions.CatalogExpiration);
            return data!;
        }

        public async Task<List<DistrictModel>> GetCantonsOfDistricts(int idCanton)
        {
            if (!_cacheOptions.EnableCaching)
            {
                return await _catalogRepository.GetCantonsOfDistricts(idCanton);
            }

            var cacheKey = string.Format(CACHE_KEY_DISTRICTS_BY_CANTON, idCanton);
            var cachedData = _cacheService.Get<List<DistrictModel>>(cacheKey);
            if (cachedData != null)
            {
                _logger.LogDebug("Obteniendo distritos por cantón {CantonId} desde caché", idCanton);
                return cachedData;
            }

            _logger.LogDebug("Obteniendo distritos por cantón {CantonId} desde base de datos", idCanton);
            var data = await _catalogRepository.GetCantonsOfDistricts(idCanton);
            _cacheService.Set(cacheKey, data, _cacheOptions.CatalogExpiration);
            return data!;
        }

        public async Task<List<CantonModel>> GetCantonsOfProvinces(int idProvince)
        {
            if (!_cacheOptions.EnableCaching)
            {
                return await _catalogRepository.GetCantonsOfProvinces(idProvince);
            }

            var cacheKey = string.Format(CACHE_KEY_CANTONS_BY_PROVINCE, idProvince);
            var cachedData = _cacheService.Get<List<CantonModel>>(cacheKey);
            if (cachedData != null)
            {
                _logger.LogDebug("Obteniendo cantones por provincia {ProvinceId} desde caché", idProvince);
                return cachedData;
            }

            _logger.LogDebug("Obteniendo cantones por provincia {ProvinceId} desde base de datos", idProvince);
            var data = await _catalogRepository.GetCantonsOfProvinces(idProvince);
            _cacheService.Set(cacheKey, data, _cacheOptions.CatalogExpiration);
            return data;
        }

        public async Task<List<DistrictModel>> GetDistricts()
        {
            if (!_cacheOptions.EnableCaching)
            {
                return await _catalogRepository.GetDistricts();
            }

            var cachedData = _cacheService.Get<List<DistrictModel>>(CACHE_KEY_DISTRICTS);
            if (cachedData != null)
            {
                _logger.LogDebug("Obteniendo distritos desde caché");
                return cachedData;
            }

            _logger.LogDebug("Obteniendo distritos desde base de datos");
            var data = await _catalogRepository.GetDistricts();
            _cacheService.Set(CACHE_KEY_DISTRICTS, data, _cacheOptions.CatalogExpiration);
            return data;
        }

        public async Task<List<IdentificationTypeModel>> GetIdentificationTypes()
        {
            if (!_cacheOptions.EnableCaching)
            {
                return await _catalogRepository.GetIdentificationTypes();
            }

            var cachedData = _cacheService.Get<List<IdentificationTypeModel>>(CACHE_KEY_IDENTIFICATION_TYPES);
            if (cachedData != null)
            {
                _logger.LogDebug("Obteniendo tipos de identificación desde caché");
                return cachedData;
            }

            _logger.LogDebug("Obteniendo tipos de identificación desde base de datos");
            var data = await _catalogRepository.GetIdentificationTypes();
            _cacheService.Set(CACHE_KEY_IDENTIFICATION_TYPES, data, _cacheOptions.CatalogExpiration);
            return data!;
        }

        public async Task<List<ProvinceModel>> GetProvinces()
        {
            if (!_cacheOptions.EnableCaching)
            {
                return await _catalogRepository.GetProvinces();
            }

            var cachedData = _cacheService.Get<List<ProvinceModel>>(CACHE_KEY_PROVINCES);
            if (cachedData != null)
            {
                _logger.LogDebug("Obteniendo provincias desde caché");
                return cachedData;
            }

            _logger.LogDebug("Obteniendo provincias desde base de datos");
            var data = await _catalogRepository.GetProvinces();
            _cacheService.Set(CACHE_KEY_PROVINCES, data, _cacheOptions.CatalogExpiration);
            return data!;
        }

        /// <summary>
        /// Limpia el caché de un catálogo específico
        /// </summary>
        private void InvalidateCache(string catalogType)
        {
            _logger.LogInformation("Invalidando caché para {CatalogType}", catalogType);
            switch (catalogType.ToLower())
            {
                case "identificationtypes":
                    _cacheService.Remove(CACHE_KEY_IDENTIFICATION_TYPES);
                    break;
                case "provinces":
                    _cacheService.Remove(CACHE_KEY_PROVINCES);
                    break;
                case "cantons":
                    _cacheService.Remove(CACHE_KEY_CANTONS);
                    _cacheService.RemoveByPattern(CACHE_KEY_CANTONS_BY_PROVINCE.Replace("{0}", ""));
                    break;
                case "districts":
                    _cacheService.Remove(CACHE_KEY_DISTRICTS);
                    _cacheService.RemoveByPattern(CACHE_KEY_DISTRICTS_BY_CANTON.Replace("{0}", ""));
                    break;
                default:
                    // Limpiar todo el caché
                    foreach (var key in _cacheService.GetKeys())
                    {
                        _cacheService.Remove(key);
                    }
                    _changeMonitor.NotifyCatalogChanged();
                    break;
            }
        }

        // Métodos para actualizar catálogos
        public async Task UpdateIdentificationType(IdentificationTypeModel model)
        {
            await _catalogRepository.UpdateIdentificationType(model);
            InvalidateCache("identificationtypes");
            _changeMonitor.NotifyCatalogChanged("IdentificationType");
        }

        public async Task UpdateProvince(ProvinceModel model)
        {
            await _catalogRepository.UpdateProvince(model);
            InvalidateCache("provinces");
            _changeMonitor.NotifyCatalogChanged("Province");
        }

        public async Task UpdateCanton(CantonModel model)
        {
            await _catalogRepository.UpdateCanton(model);
            InvalidateCache("cantons");
            _changeMonitor.NotifyCatalogChanged("Canton");
        }

        public async Task UpdateDistrict(DistrictModel model)
        {
            await _catalogRepository.UpdateDistrict(model);
            InvalidateCache("districts");
            _changeMonitor.NotifyCatalogChanged("District");
        }
    }
}
