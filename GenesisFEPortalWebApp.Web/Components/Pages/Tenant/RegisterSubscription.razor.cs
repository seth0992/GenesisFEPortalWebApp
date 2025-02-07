using Blazored.Toast.Services;
using GenesisFEPortalWebApp.Models.Entities.Catalog;
using GenesisFEPortalWebApp.Models.Entities.Subscription;
using GenesisFEPortalWebApp.Models.Models.Subscription;
using GenesisFEPortalWebApp.Models.Models;
using Microsoft.AspNetCore.Components;
using Newtonsoft.Json;
using Radzen;
using GenesisFEPortalWebApp.Utilities;
using System.ComponentModel.DataAnnotations;

namespace GenesisFEPortalWebApp.Web.Components.Pages.Tenant
{
    public partial class RegisterSubscription
    {
        [Inject] private ApiClient ApiClient { get; set; } = default!;
        [Inject] private NavigationManager NavigationManager { get; set; } = default!;
        [Inject] private NotificationService NotificationService { get; set; } = default!;
        [Inject] private IToastService ToastService { get; set; } = default!;

        private List<SubscriptionTypeModel>? subscriptionPlans;
        private RegisterTenantWithSubscriptionDto registrationModel = new();
        private SubscriptionTypeModel? selectedPlan;
        private string? selectedIdentificationType;
        private string confirmPassword = string.Empty;
        private bool isSubmitting = false;

        // Tipos de identificación (deberías obtenerlos desde un servicio de catálogo)
        private List<IdentificationTypeModel> IdentificationTypes = new()
    {
        new IdentificationTypeModel { ID = "01", Description = "Cédula Física" },
        new IdentificationTypeModel { ID = "02", Description = "Cédula Jurídica" },
        new IdentificationTypeModel { ID = "03", Description = "DIMEX" },
        new IdentificationTypeModel { ID = "04", Description = "NITE" }
    };

        protected override async Task OnInitializedAsync()
        {
            try
            {
                // Inicializar la lista como lista vacía para evitar null
                subscriptionPlans = new List<SubscriptionTypeModel>();

                var response = await ApiClient.GetFromJsonAsync<BaseResponseModel>("api/SubscriptionAdmin/plans");

                if (response == null)
                {
                    NotificationService.Notify(
                        NotificationSeverity.Error,
                        "Error",
                        "No se recibió respuesta del servidor"
                    );
                    return;
                }

                if (response.Success && response.Data != null)
                {
                    subscriptionPlans = JsonConvert.DeserializeObject<List<SubscriptionTypeModel>>(
                        response.Data.ToString()!
                    ) ?? new List<SubscriptionTypeModel>();

                    if (!subscriptionPlans.Any())
                    {
                        NotificationService.Notify(
                            NotificationSeverity.Warning,
                            "Advertencia",
                            "No hay planes de suscripción disponibles"
                        );
                    }
                }
                else
                {
                    NotificationService.Notify(
                        NotificationSeverity.Error,
                        "Error",
                        response.ErrorMessage ?? "Error al cargar planes de suscripción"
                    );
                }
            }
            catch (Exception ex)
            {
                NotificationService.Notify(
                    NotificationSeverity.Error,
                    "Error",
                    $"Error de conexión: {ex.Message}"
                );
            }
        }
       
        private void SelectPlan(SubscriptionTypeModel plan)
        {
            selectedPlan = plan;
            registrationModel.SubscriptionTypeId = plan.ID;
        }

        private void UpdateIdentificationModel(object? value)
        {
            // Puedes agregar lógica adicional si es necesario cuando cambia el tipo de identificación
            registrationModel.IdentificationTypeId = selectedIdentificationType;
        }

        private void ValidateIdentification()
        {
            if (string.IsNullOrEmpty(selectedIdentificationType))
            {
                NotificationService.Notify(
                    NotificationSeverity.Warning,
                    "Validación",
                    "Seleccione primero el tipo de identificación"
                );
                return;
            }

            // Aquí puedes agregar validaciones específicas de identificación
            // Por ejemplo, usando la clase IdentificationValidationUtils
        }

        private List<string> GetFeatures(SubscriptionTypeModel plan)
        {
            var features = JsonConvert.DeserializeObject<Dictionary<string, object>>(plan.Features);
            var featureList = new List<string>();

            if (features != null)
            {
                featureList.Add($"Hasta {features["users"]} usuarios");
                featureList.Add($"{features["customers"]} clientes");
                if ((bool)features["support"])
                    featureList.Add("Soporte prioritario");
            }

            return featureList;
        }

        private async Task HandleSubmit()
        {
            try
            {
                isSubmitting = true;

                // Validación adicional de contraseña
                if (registrationModel.Password != confirmPassword)
                {
                    NotificationService.Notify(
                        NotificationSeverity.Warning,
                        "Validación",
                        "Las contraseñas no coinciden"
                    );
                    return;
                }

                // Realizar validación de identificación 
                var validationResult = IdentificationValidationUtils.ValidateIdentification(
                    selectedIdentificationType,
                    registrationModel.Identification
                );

                if (validationResult != ValidationResult.Success)
                {
                    NotificationService.Notify(
                        NotificationSeverity.Warning,
                        "Validación",
                        validationResult.ErrorMessage
                    );
                    return;
                }

                // Enviar solicitud de registro
                var response = await ApiClient.PostAsync<BaseResponseModel, RegisterTenantWithSubscriptionDto>(
                    "api/SubscriptionAdmin/register",
                    registrationModel
                );

                if (response?.Success == true)
                {
                    ToastService.ShowSuccess("Registro exitoso. Redirigiendo...");

                    // Pequeña pausa para mostrar el mensaje
                    await Task.Delay(1500);

                    NavigationManager.NavigateTo("/login");
                }
                else
                {
                    NotificationService.Notify(
                        NotificationSeverity.Error,
                        "Error",
                        response?.ErrorMessage ?? "Error en el registro"
                    );
                }
            }
            catch (Exception ex)
            {
                NotificationService.Notify(
                    NotificationSeverity.Error,
                    "Error",
                    $"Error inesperado: {ex.Message}"
                );
            }
            finally
            {
                isSubmitting = false;
            }
        }
    }

}
