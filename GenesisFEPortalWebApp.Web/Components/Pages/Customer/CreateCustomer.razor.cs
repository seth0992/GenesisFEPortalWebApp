using Blazored.Toast.Services;
using GenesisFEPortalWebApp.Models.Entities.Catalog;
using GenesisFEPortalWebApp.Models.Entities.Core;
using GenesisFEPortalWebApp.Models.Models;
using GenesisFEPortalWebApp.Models.Models.Customer;
using GenesisFEPortalWebApp.Utilities;
using Microsoft.AspNetCore.Components;
using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;

namespace GenesisFEPortalWebApp.Web.Components.Pages.Customer
{
    public partial class CreateCustomer
    {
        [Inject]
        public required ApiClient ApiClient { get; set; }
        [Inject]
        private IToastService ToastService { get; set; } = default!;
        [Inject]
        private NavigationManager NavigationManager { get; set; } = default!;

        private CustomerModel customer = new();
        private string? identificationError;
        private int? ProvinceSelected { get; set; }
        private int? CantonSelected { get; set; }
        private List<IdentificationTypeModel> IdentificationTypes { get; set; } = new();
        private List<ProvinceModel> Provinces { get; set; } = new();
        private List<CantonModel> Cantons { get; set; } = new();
        private List<DistrictModel> Districts { get; set; } = new();

        protected override async Task OnInitializedAsync()
        {
            await LoadCatalogs();
        }

        private async Task HandleValidSubmit()
        {
            try
            {
                if (!ValidateIdentification())
                {
                    return;
                }

                var createDto = new CreateCustomerDto
                {
                    CustomerName = customer.CustomerName,
                    CommercialName = customer.CommercialName,
                    Identification = customer.Identification,
                    IdentificationTypeId = customer.IdentificationTypeId,
                    Email = customer.Email,
                    PhoneCode = customer.PhoneCode,
                    Phone = customer.Phone,
                    Address = customer.Address,
                    Neighborhood = customer.Neighborhood,
                    DistrictId = customer.DistrictId
                };

                var response = await ApiClient.PostAsync<BaseResponseModel, CreateCustomerDto>(
                    "/api/Customer",
                    createDto
                );

                if (response?.Success == true)
                {
                    ToastService.ShowSuccess("Cliente registrado exitosamente");
                    await Task.Delay(1000);
                    NavigationManager.NavigateTo("/customer");
                }
                else
                {
                    ToastService.ShowError(response?.ErrorMessage ?? "Error al registrar el cliente");
                }
            }
            catch (Exception ex)
            {
                ToastService.ShowError("Error al procesar la solicitud");
                Console.WriteLine($"Error: {ex.Message}");
            }
        }

        private bool ValidateIdentification()
        {
            if (string.IsNullOrEmpty(customer.IdentificationTypeId) ||
                string.IsNullOrEmpty(customer.Identification))
            {
                identificationError = "Tipo y número de identificación son requeridos";
                return false;
            }

            var validationResult = IdentificationValidationUtils.ValidateIdentification(
                customer.IdentificationTypeId,
                customer.Identification
            );

            identificationError = validationResult?.ErrorMessage;
            return validationResult == ValidationResult.Success;
        }

        private async Task LoadCatalogs()
        {
            try
            {
                var identificationTypesResponse = await ApiClient.GetFromJsonAsync<BaseResponseModel>(
                    "/api/Catalog/identification-types"
                );

                if (identificationTypesResponse?.Success == true)
                {
                    IdentificationTypes = JsonConvert.DeserializeObject<List<IdentificationTypeModel>>(
                        identificationTypesResponse.Data.ToString()!
                    )!;
                }

                var provincesResponse = await ApiClient.GetFromJsonAsync<BaseResponseModel>(
                    "/api/Catalog/provinces"
                );

                if (provincesResponse?.Success == true)
                {
                    Provinces = JsonConvert.DeserializeObject<List<ProvinceModel>>(
                        provincesResponse.Data.ToString()!
                    )!;
                }
            }
            catch (Exception ex)
            {
                ToastService.ShowError("Error al cargar los catálogos");
                Console.WriteLine($"Error: {ex.Message}");
            }
        }

        private async Task SearchCantonsOfProvinces()
        {
            if (ProvinceSelected == null) return;

            var response = await ApiClient.GetFromJsonAsync<BaseResponseModel>(
                $"/api/Catalog/provinces/{ProvinceSelected}/cantons"
            );

            if (response?.Success == true)
            {
                Cantons = JsonConvert.DeserializeObject<List<CantonModel>>(
                    response.Data.ToString()!
                )!;
            }
        }

        private async Task SearchDistrictsOfCanton()
        {
            if (CantonSelected == null) return;

            var response = await ApiClient.GetFromJsonAsync<BaseResponseModel>(
                $"/api/Catalog/cantons/{CantonSelected}/districts"
            );

            if (response?.Success == true)
            {
                Districts = JsonConvert.DeserializeObject<List<DistrictModel>>(
                    response.Data.ToString()!
                )!;
            }
        }
    }
}
