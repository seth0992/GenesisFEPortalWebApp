using Blazored.Toast.Services;
using GenesisFEPortalWebApp.Models.Entities.Catalog;
using GenesisFEPortalWebApp.Models.Entities.Core;
using GenesisFEPortalWebApp.Models.Models;
using Microsoft.AspNetCore.Components;
using Newtonsoft.Json;

namespace GenesisFEPortalWebApp.Web.Components.Pages.Customer
{
    //public partial class CreateCustomer
    //{
    //    [Inject]
    //    public required ApiClient ApiClient { get; set; }
    //    [Inject]
    //    private IToastService toastService { get; set; } = default!;
    //    [Inject]
    //    private NavigationManager NavigationManager { get; set; } = default!;
    //    public CustomerModel customer { get; set; } = new();

    //    public List<ProvinceModel> Provinces { get; set; } = new();
    //    public List<CantonModel> Cantons { get; set; } = new();
    //    public List<DistrictModel> Districts { get; set; } = new();
    //    public List<IdentificationTypeModel> IdentificationTypes { get; set; } = new();
    //    public int? ProvinceSelected { get; set; } = null;
    //    public int? CantonSelected { get; set; } = null;
    //    bool popup;

    //    protected override async Task OnInitializedAsync()
    //    {

    //        await base.OnInitializedAsync();
    //        await GetProvinces();
    //        await GetIdentificationTypes();
    //    }
    //    public async Task Submit(CustomerModel Model)
    //    {
    //        customer.ID = 1;
    //        var cu = new CustomerModel();

    //        var res = await ApiClient.PostAsync<BaseResponseModel, CustomerModel>("/api/Customer", customer);
    //        if (res != null && res.Success)
    //        {
    //            toastService.ShowSuccess("El cliente se agrego correctamente.");
    //            NavigationManager.NavigateTo("/customer");
    //        }
    //        else
    //        {

    //        }
    //    }

    //    protected async Task GetIdentificationTypes()
    //    {

    //        var res = await ApiClient.GetFromJsonAsync<BaseResponseModel>("/api/Catalog/identification-types");

    //        if (res != null && res.Success)
    //        {
    //            IdentificationTypes = JsonConvert.DeserializeObject<List<IdentificationTypeModel>>(res.Data.ToString()!)!;
    //        }

    //    }

    //    protected async Task GetProvinces()
    //    {

    //        var res = await ApiClient.GetFromJsonAsync<BaseResponseModel>("/api/Catalog/provinces");

    //        if (res != null && res.Success)
    //        {
    //            Provinces = JsonConvert.DeserializeObject<List<ProvinceModel>>(res.Data.ToString()!)!;
    //        }

    //    }

    //    protected async Task SearchCantonsOfProvinces()
    //    {

    //        if (ProvinceSelected == null)
    //        {
    //            return;
    //        }


    //        var res = await ApiClient.GetFromJsonAsync<BaseResponseModel>($"/api/Catalog/provinces/{ProvinceSelected}/cantons");

    //        if (res != null && res.Success)
    //        {
    //            Cantons = JsonConvert.DeserializeObject<List<CantonModel>>(res.Data.ToString()!)!;

    //        }
    //    }

    //    protected async Task SearchDistrictsOfCanton()
    //    {

    //        if (CantonSelected == null)
    //        {
    //            return;
    //        }


    //        var res = await ApiClient.GetFromJsonAsync<BaseResponseModel>($"/api/Catalog/cantons/{CantonSelected}/districts");

    //        if (res != null && res.Success)
    //        {
    //            Districts = JsonConvert.DeserializeObject<List<DistrictModel>>(res.Data.ToString()!)!;

    //        }
    //    }


    //}
}
