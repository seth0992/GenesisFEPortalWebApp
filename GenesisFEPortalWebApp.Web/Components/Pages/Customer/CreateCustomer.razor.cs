using Blazored.Toast.Services;
using GenesisFEPortalWebApp.Models.Entities.Catalog;
using GenesisFEPortalWebApp.Models.Entities.Core;
using GenesisFEPortalWebApp.Models.Models;
using GenesisFEPortalWebApp.Models.Models.Customer;
using GenesisFEPortalWebApp.Utilities;
using Microsoft.AspNetCore.Components;
using Newtonsoft.Json;
using Radzen;
using System;
using System.ComponentModel.DataAnnotations;

namespace GenesisFEPortalWebApp.Web.Components.Pages.Customer
{
    public partial class CreateCustomer
    {
        [Inject] private ApiClient ApiClient { get; set; } = default!;
        [Inject] private NavigationManager NavigationManager { get; set; } = default!;
        [Inject] private NotificationService NotificationService { get; set; } = default!;
        [Inject] private IToastService ToastService { get; set; } = default!;

        private CustomerModel customer = new();
        private bool isSaving;
        private string? identificationError;

        // Propiedades para el manejo de ubicación
        private int? ProvinceSelected { get; set; }
        private int? CantonSelected { get; set; }

        // Catálogos
        private List<IdentificationTypeModel> IdentificationTypes { get; set; } = new();
        private List<ProvinceModel> Provinces { get; set; } = new();
        private List<CantonModel> Cantons { get; set; } = new();
        private List<DistrictModel> Districts { get; set; } = new();
        public string mask { get; set; } = string.Empty;
        protected override async Task OnInitializedAsync()
        {
            await LoadCatalogs();
        }

        private async Task LoadCatalogs()
        {
            try
            {
                // Cargar tipos de identificación
                var identificationTypesResponse = await ApiClient.GetFromJsonAsync<BaseResponseModel>(
                    "/api/Catalog/identification-types"
                );

                if (identificationTypesResponse?.Success == true)
                {
                    IdentificationTypes = JsonConvert.DeserializeObject<List<IdentificationTypeModel>>(
                        identificationTypesResponse.Data.ToString()!
                    )!;
                }

                // Cargar provincias
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
                NotificationService.Notify(NotificationSeverity.Error,
                    "Error",
                    "No se pudieron cargar los catálogos necesarios");
                Console.WriteLine($"Error cargando catálogos: {ex.Message}");
            }
        }

        //private string GetIdentificationMask()
        //{
        //    return customer.IdentificationTypeId switch
        //    {
        //        "01" => "0-0000-0000", // Cédula física
        //        "02" => "0-000-000000", // Cédula jurídica
        //        "03" => "000000000000", // DIMEX
        //        "04" => "0000000000", // NITE
        //        _ => ""
        //    };
        //}
        //void OnChange(string value, string name)
        //{
        //    var t = value;
        //   // console.Log($"{name} value changed to {value}");
        //}

        //private void HandleIdentificationChange(string value)
        //{
        //    customer.Identification = value.Replace("-", "");
        //    ValidateIdentification();
        //}

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
            StateHasChanged();
            return validationResult == ValidationResult.Success;
        }

        private void OnIdentificationChange(string value)
        {
            if (string.IsNullOrEmpty(customer.IdentificationTypeId))
            {
                NotificationService.Notify(NotificationSeverity.Warning,
                    "Validación",
                    "Seleccione primero el tipo de identificación");
                return;
            }

            // Eliminar caracteres no numéricos
            var numbersOnly = new string(value.Where(char.IsDigit).ToArray());

            // Aplicar formato según el tipo
            customer.Identification = customer.IdentificationTypeId switch
            {
                "01" => FormatCedulaFisica(numbersOnly),
                "02" => FormatCedulaJuridica(numbersOnly),
                "03" => numbersOnly,
                "04" => numbersOnly,
                _ => numbersOnly
            };

            ValidateIdentification();
        }

        private string FormatCedulaFisica(string numbers)
        {
            if (numbers.Length <= 1) return numbers;
            if (numbers.Length <= 5) return $"{numbers[0]}-{numbers[1..]}";
            return $"{numbers[0]}-{numbers[1..5]}-{numbers[5..]}";
        }

        private string FormatCedulaJuridica(string numbers)
        {
            if (numbers.Length <= 1) return numbers;
            if (numbers.Length <= 4) return $"{numbers[0]}-{numbers[1..]}";
            return $"{numbers[0]}-{numbers[1..4]}-{numbers[4..]}";
        }
        private async Task SearchCantonsOfProvinces()
        {
            if (ProvinceSelected == null) return;

            try
            {
                var response = await ApiClient.GetFromJsonAsync<BaseResponseModel>(
                    $"/api/Catalog/provinces/{ProvinceSelected}/cantons"
                );

                if (response?.Success == true)
                {
                    Cantons = JsonConvert.DeserializeObject<List<CantonModel>>(
                        response.Data.ToString()!
                    )!;

                    // Limpiar selecciones dependientes
                    CantonSelected = null;
                    customer.DistrictId = null;
                    Districts.Clear();
                }
            }
            catch (Exception ex)
            {
                NotificationService.Notify(NotificationSeverity.Error,
                    "Error",
                    "No se pudieron cargar los cantones");
                Console.WriteLine($"Error: {ex.Message}");
            }
        }

        private async Task SearchDistrictsOfCanton()
        {
            if (CantonSelected == null) return;

            try
            {
                var response = await ApiClient.GetFromJsonAsync<BaseResponseModel>(
                    $"/api/Catalog/cantons/{CantonSelected}/districts"
                );

                if (response?.Success == true)
                {
                    Districts = JsonConvert.DeserializeObject<List<DistrictModel>>(
                        response.Data.ToString()!
                    )!;

                    // Limpiar la selección del distrito
                    customer.DistrictId = null;
                }
            }
            catch (Exception ex)
            {
                NotificationService.Notify(NotificationSeverity.Error,
                    "Error",
                    "No se pudieron cargar los distritos");
                Console.WriteLine($"Error: {ex.Message}");
            }
        }

        private async Task HandleValidSubmit()
        {
            try
            {
                // Validar la identificación antes de enviar
                if (!ValidateIdentification())
                {
                    NotificationService.Notify(NotificationSeverity.Warning,
                        "Validación",
                        "Por favor, corrija los errores de identificación antes de continuar");
                    return;
                }

                // Validaciones adicionales
                if (string.IsNullOrWhiteSpace(customer.CustomerName))
                {
                    NotificationService.Notify(NotificationSeverity.Warning,
                        "Validación",
                        "El nombre del cliente es requerido");
                    return;
                }

                isSaving = true;

                // Preparar el DTO
                var createDto = new CreateCustomerDto
                {
                    CustomerName = customer.CustomerName.Trim(),
                    CommercialName = customer.CommercialName?.Trim(),
                    Identification = customer.Identification.Trim(),
                    IdentificationTypeId = customer.IdentificationTypeId,
                    Email = customer.Email?.Trim(),
                    PhoneCode = customer.PhoneCode?.Trim(),
                    Phone = customer.Phone?.Trim(),
                    Address = customer.Address?.Trim(),
                    Neighborhood = customer.Neighborhood?.Trim(),
                    DistrictId = customer.DistrictId
                };

                // Enviar la solicitud
                var response = await ApiClient.PostAsync<BaseResponseModel, CreateCustomerDto>(
                    "/api/Customer",
                    createDto
                );

                if (response?.Success == true)
                {
                    NotificationService.Notify(NotificationSeverity.Success,
                        "Éxito",
                        "Cliente registrado exitosamente");

                    // Esperar un momento para que el usuario vea el mensaje de éxito
                    await Task.Delay(1500);
                    NavigationManager.NavigateTo("/customer");
                }
                else
                {
                    NotificationService.Notify(NotificationSeverity.Error,
                        "Error",
                        response?.ErrorMessage ?? "Error al registrar el cliente");
                }
            }
            catch (Exception ex)
            {
                NotificationService.Notify(NotificationSeverity.Error,
                    "Error",
                    "Ocurrió un error al procesar la solicitud");
                Console.WriteLine($"Error: {ex.Message}");
            }
            finally
            {
                isSaving = false;
                StateHasChanged();
            }
        }

        private void ValidateEmail()
        {
            if (!string.IsNullOrWhiteSpace(customer.Email) &&
                !System.Text.RegularExpressions.Regex.IsMatch(
                    customer.Email,
                    @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
            {
                NotificationService.Notify(NotificationSeverity.Warning,
                    "Validación",
                    "El formato del correo electrónico no es válido");
            }
        }

        private void ValidatePhoneNumber()
        {
            if (!string.IsNullOrEmpty(customer.Phone))
            {
                if (!System.Text.RegularExpressions.Regex.IsMatch(customer.Phone, @"^\d{8}$"))
                {
                    NotificationService.Notify(NotificationSeverity.Warning,
                        "Validación",
                        "El número debe contener 8 dígitos");
                    customer.Phone = customer.Phone.Length > 8
                        ? customer.Phone[..8]
                        : customer.Phone;
                }
            }
        }
    }
}
