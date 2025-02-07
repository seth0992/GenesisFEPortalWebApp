using GenesisFEPortalWebApp.Database.Data;
using GenesisFEPortalWebApp.Models.Entities.Catalog;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GenesisFEPortalWebApp.BL.Repositories
{
    public interface ICatalogRepository
    {
        Task<List<IdentificationTypeModel>> GetIdentificationTypes();
        Task<List<ProvinceModel>> GetProvinces();
        Task<List<CantonModel>> GetCantons();
        Task<List<DistrictModel>> GetDistricts();

        Task<List<CantonModel>> GetCantonsOfProvinces(int idProvince);
        Task<List<DistrictModel>> GetCantonsOfDistricts(int idCanton);

        Task UpdateIdentificationType(IdentificationTypeModel model);
        Task UpdateProvince(ProvinceModel model);
        Task UpdateCanton(CantonModel model);
        Task UpdateDistrict(DistrictModel model);
    }
    public class CatalogRepository(AppDbContext AppDbContext) : ICatalogRepository
    {


        public Task<List<IdentificationTypeModel>> GetIdentificationTypes()
        {
            return AppDbContext.IdentificationTypes.ToListAsync();
        }

        public Task<List<ProvinceModel>> GetProvinces()
        {
            return AppDbContext.Provinces.ToListAsync();
        }
        public Task<List<CantonModel>> GetCantons()
        {
            return AppDbContext.Cantons.Include(c => c.Province).ToListAsync();
        }

        public Task<List<CantonModel>> GetCantonsOfProvinces(int idProvince)
        {
            return AppDbContext.Cantons.Where(x => x.ProvinceId == idProvince).ToListAsync();
        }
        public Task<List<DistrictModel>> GetDistricts()
        {
            return AppDbContext.Districts.Include(d => d.Region).Include(d => d.Canton).ToListAsync();
        }

        public Task<List<DistrictModel>> GetCantonsOfDistricts(int idCanton)
        {
            return AppDbContext.Districts.Where(x => x.CantonId == idCanton).ToListAsync();
        }

        public async Task UpdateIdentificationType(IdentificationTypeModel model)
        {
            AppDbContext.IdentificationTypes.Update(model);
            await AppDbContext.SaveChangesAsync();
        }

        public async Task UpdateProvince(ProvinceModel model)
        {
            AppDbContext.Provinces.Update(model);
            await AppDbContext.SaveChangesAsync();
        }

        public async Task UpdateCanton(CantonModel model)
        {
            AppDbContext.Cantons.Update(model);
            await AppDbContext.SaveChangesAsync();
        }

        public async Task UpdateDistrict(DistrictModel model)
        {
            AppDbContext.Districts.Update(model);
            await AppDbContext.SaveChangesAsync();
        }
    }
}
