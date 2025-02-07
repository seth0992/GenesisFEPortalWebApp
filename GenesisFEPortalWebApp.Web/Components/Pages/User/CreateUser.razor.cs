using Blazored.Toast.Services;
using GenesisFEPortalWebApp.Models.Entities.Security;
using GenesisFEPortalWebApp.Models.Models.User;
using GenesisFEPortalWebApp.Models.Models;
using Microsoft.AspNetCore.Components;
using Newtonsoft.Json;
using Radzen;

namespace GenesisFEPortalWebApp.Web.Components.Pages.User
{
    public partial class CreateUser
    {
        [Inject] private ApiClient ApiClient { get; set; } = default!;
        [Inject] private NavigationManager NavigationManager { get; set; } = default!;
        [Inject] private NotificationService NotificationService { get; set; } = default!;
        [Inject] private IToastService ToastService { get; set; } = default!;

        private CreateUserDto model = new();
        private string confirmPassword = string.Empty;
        private List<RoleModel> availableRoles = new();
        private bool isSaving;

        protected override async Task OnInitializedAsync()
        {
            await LoadRoles();
        }

        private async Task LoadRoles()
        {
            try
            {
                // Inicializamos la lista como vacía
                availableRoles = new List<RoleModel>();

                var response = await ApiClient.GetFromJsonAsync<BaseResponseModel>(
                    "/api/User/available-roles"
                );

                if (response?.Success == true && response.Data != null)
                {
                    var serializedData = response.Data.ToString();
                    Console.WriteLine($"Datos serializados: {serializedData}");

                    availableRoles = JsonConvert.DeserializeObject<List<RoleModel>>(
                        serializedData!,
                        new JsonSerializerSettings
                        {
                            Error = (sender, args) =>
                            {
                                Console.WriteLine($"Error de deserialización: {args.ErrorContext.Error.Message}");
                                args.ErrorContext.Handled = true;
                            }
                        }
                    ) ?? new List<RoleModel>();

                    Console.WriteLine($"Roles cargados: {availableRoles.Count}");
                    foreach (var role in availableRoles)
                    {
                        Console.WriteLine($"Role - ID: {role.ID}, Name: {role.Name}");
                    }
                }
                else
                {
                    NotificationService.Notify(
                        NotificationSeverity.Error,
                        "Error",
                        "No se pudieron cargar los roles disponibles"
                    );
                    Console.WriteLine($"Error en la respuesta: {response?.ErrorMessage}");
                }
            }
            catch (Exception ex)
            {
                NotificationService.Notify(
                    NotificationSeverity.Error,
                    "Error",
                    "Error al cargar los roles"
                );
                Console.WriteLine($"Excepción: {ex.Message}");
                Console.WriteLine($"StackTrace: {ex.StackTrace}");
            }
            finally
            {
                StateHasChanged();
            }
        }
        private async Task HandleValidSubmit()
        {
            try
            {
                if (model.Password != confirmPassword)
                {
                    NotificationService.Notify(NotificationSeverity.Warning,
                        "Validación",
                        "Las contraseñas no coinciden");
                    return;
                }

                isSaving = true;

                var response = await ApiClient.PostAsync<BaseResponseModel, CreateUserDto>(
                    "/api/User",
                    model
                );

                if (response?.Success == true)
                {
                    NotificationService.Notify(NotificationSeverity.Success,
                        "Éxito",
                        "Usuario creado exitosamente");

                    await Task.Delay(1500); // Esperar para que se vea la notificación
                    NavigationManager.NavigateTo("/users");
                }
                else
                {
                    NotificationService.Notify(NotificationSeverity.Error,
                        "Error",
                        response?.ErrorMessage ?? "Error al crear el usuario");
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
    }
}
