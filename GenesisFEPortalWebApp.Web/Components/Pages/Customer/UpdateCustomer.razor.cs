using GenesisFEPortalWebApp.Models.Entities.Catalog;
using GenesisFEPortalWebApp.Models.Entities.Core;
using GenesisFEPortalWebApp.Models.Models.Customer;
using GenesisFEPortalWebApp.Models.Models;
using Microsoft.AspNetCore.Components;
using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;
using GenesisFEPortalWebApp.Utilities;

namespace GenesisFEPortalWebApp.Web.Components.Pages.Customer
{
    // GenesisFEPortalWebApp.Web/Components/Pages/Customer/UpdateCustomer.razor.cs
    public partial class UpdateCustomer
    {
        [Parameter] 
        public long Id { get; set; }

        private CustomerModel customer = new();
        private string? IdentificationError;
        private bool IsLoading = true;

        // Propiedades para manejo de ubicación
        private int? ProvinceSelected;
        private int? CantonSelected;

        // Catálogos
        private List<IdentificationTypeModel> IdentificationTypes = new();
        private List<ProvinceModel> Provinces = new();
        private List<CantonModel> Cantons = new();
        private List<DistrictModel> Districts = new();

        protected override async Task OnInitializedAsync()
        {
            try
            {
                await LoadCatalogs();
                await LoadCustomer();
            }
            catch (Exception ex)
            {
                ToastService.ShowError("Error cargando datos del cliente");
                Console.WriteLine($"Error: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task LoadCatalogs()
        {
            var identificationTypesResponse = await ApiClient
                .GetFromJsonAsync<BaseResponseModel>("/api/Catalog/identification-types");
            if (identificationTypesResponse?.Success == true)
                IdentificationTypes = JsonConvert.DeserializeObject<List<IdentificationTypeModel>>(
                    identificationTypesResponse.Data.ToString()!)!;

            var provincesResponse = await ApiClient
                .GetFromJsonAsync<BaseResponseModel>("/api/Catalog/provinces");
            if (provincesResponse?.Success == true)
                Provinces = JsonConvert.DeserializeObject<List<ProvinceModel>>(
                    provincesResponse.Data.ToString()!)!;
        }

        private async Task LoadCustomer()
        {
            var response = await ApiClient.GetFromJsonAsync<BaseResponseModel>($"/api/Customer/{Id}");
            if (response?.Success == true)
            {
                customer = JsonConvert.DeserializeObject<CustomerModel>(response.Data.ToString()!)!;

                if (customer.District?.Canton != null)
                {
                    ProvinceSelected = customer.District.Canton.ProvinceId;
                    await SearchCantonsOfProvinces();

                    CantonSelected = customer.District.CantonId;
                    await SearchDistrictsOfCanton();
                }
            }
            else
            {
                ToastService.ShowError("No se encontró el cliente");
                NavigationManager.NavigateTo("/customer");
            }
        }

        private async Task HandleSubmit()
        {
            try
            {
                // Validación de identificación
                if (!ValidateIdentification())
                {
                    ToastService.ShowError("Por favor corrija los errores de validación");
                    return;
                }

                // Creamos el DTO para la actualización
                var updateDto = new UpdateCustomerDto
                {
                    ID = customer.ID,
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

                // Realizamos la actualización
                var response = await ApiClient.PutAsync<BaseResponseModel, UpdateCustomerDto>(
                    $"/api/Customer/{Id}", updateDto);

                if (response?.Success == true)
                {
                    ToastService.ShowSuccess("Cliente actualizado exitosamente");
                    NavigationManager.NavigateTo("/customer");
                }
                else
                {
                    ToastService.ShowError(response?.ErrorMessage ?? "Error al actualizar el cliente");
                }
            }
            catch (Exception ex)
            {
                ToastService.ShowError("Error al actualizar el cliente");
                Console.WriteLine($"Error en HandleSubmit: {ex.Message}");
            }
        }

        private bool ValidateIdentification()
        {
            var validationResult = IdentificationValidationUtils.ValidateIdentification(
                customer.IdentificationTypeId,
                customer.Identification);

            IdentificationError = validationResult?.ErrorMessage;
            return validationResult == ValidationResult.Success;
        }

        // Métodos para manejo de ubicación
        private async Task SearchCantonsOfProvinces()
        {
            if (ProvinceSelected == null) return;

            var response = await ApiClient
                .GetFromJsonAsync<BaseResponseModel>($"/api/Catalog/provinces/{ProvinceSelected}/cantons");

            if (response?.Success == true)
                Cantons = JsonConvert.DeserializeObject<List<CantonModel>>(response.Data.ToString()!)!;
        }

        private async Task SearchDistrictsOfCanton()
        {
            if (CantonSelected == null) return;

            var response = await ApiClient
                .GetFromJsonAsync<BaseResponseModel>($"/api/Catalog/cantons/{CantonSelected}/districts");

            if (response?.Success == true)
                Districts = JsonConvert.DeserializeObject<List<DistrictModel>>(response.Data.ToString()!)!;
        }
    }
}
