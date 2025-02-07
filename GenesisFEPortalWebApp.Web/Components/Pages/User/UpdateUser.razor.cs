using Blazored.Toast.Services;
using GenesisFEPortalWebApp.Models.Entities.Security;
using GenesisFEPortalWebApp.Models.Models.User;
using GenesisFEPortalWebApp.Models.Models;
using Microsoft.AspNetCore.Components;
using Newtonsoft.Json;
using Radzen;

namespace GenesisFEPortalWebApp.Web.Components.Pages.User
{
    public partial class UpdateUser
    {
        // Parámetros e inyección de dependencias
        [Parameter] public long Id { get; set; }
        [Inject] private ApiClient ApiClient { get; set; } = default!;
        [Inject] private NavigationManager NavigationManager { get; set; } = default!;
        [Inject] private NotificationService NotificationService { get; set; } = default!;
        [Inject] private IToastService ToastService { get; set; } = default!;

        // Estado del componente
        private UserDetailDto? user;
        private UpdateUserDto model = new();
        private UpdatePasswordDto passwordModel = new();
        private string confirmPassword = string.Empty;
        private List<RoleModel> availableRoles = new();
        private bool isLoading = true;
        private bool isSaving;
        private bool isChangingPassword;

        // Inicialización del componente
        protected override async Task OnInitializedAsync()
        {
            await LoadRoles();
            await LoadUser();
        }

        // Método para cargar los roles disponibles
        private async Task LoadRoles()
        {
            try
            {
                var response = await ApiClient.GetFromJsonAsync<BaseResponseModel>(
                    "/api/User/available-roles"
                );

                if (response?.Success == true && response.Data != null)
                {
                    availableRoles = JsonConvert.DeserializeObject<List<RoleModel>>(
                        response.Data.ToString()!
                    )!;
                }
                else
                {
                    NotificationService.Notify(
                        NotificationSeverity.Error,
                        "Error",
                        "No se pudieron cargar los roles disponibles"
                    );
                }
            }
            catch (Exception ex)
            {
                NotificationService.Notify(
                    NotificationSeverity.Error,
                    "Error",
                    "Error al cargar los roles"
                );
                Console.WriteLine($"Error cargando roles: {ex.Message}");
            }
        }

        // Método para cargar los datos del usuario
        private async Task LoadUser()
        {
            try
            {
                var response = await ApiClient.GetFromJsonAsync<BaseResponseModel>(
                    $"/api/User/{Id}"
                );

                if (response?.Success == true && response.Data != null)
                {
                    user = JsonConvert.DeserializeObject<UserDetailDto>(
                        response.Data.ToString()!
                    );

                    if (user != null)
                    {
                        // Mapear datos al modelo de edición
                        model = new UpdateUserDto
                        {
                            Id = user.Id,
                            Email = user.Email,
                            FirstName = user.FirstName,
                            LastName = user.LastName,
                            RoleId = user.RoleId
                        };
                    }
                }
                else
                {
                    NotificationService.Notify(
                        NotificationSeverity.Error,
                        "Error",
                        "Usuario no encontrado"
                    );
                }
            }
            catch (Exception ex)
            {
                NotificationService.Notify(
                    NotificationSeverity.Error,
                    "Error",
                    "Error al cargar el usuario"
                );
                Console.WriteLine($"Error cargando usuario: {ex.Message}");
            }
            finally
            {
                isLoading = false;
                StateHasChanged();
            }
        }

        // Método para manejar la actualización de información general
        private async Task HandleValidSubmit()
        {
            try
            {
                isSaving = true;

                var response = await ApiClient.PutAsync<BaseResponseModel, UpdateUserDto>(
                    $"/api/User/{Id}",
                    model
                );

                if (response?.Success == true)
                {
                    NotificationService.Notify(
                        NotificationSeverity.Success,
                        "Éxito",
                        "Usuario actualizado exitosamente"
                    );

                    await Task.Delay(1500);
                    NavigationManager.NavigateTo("/users");
                }
                else
                {
                    NotificationService.Notify(
                        NotificationSeverity.Error,
                        "Error",
                        response?.ErrorMessage ?? "Error al actualizar el usuario"
                    );
                }
            }
            catch (Exception ex)
            {
                NotificationService.Notify(
                    NotificationSeverity.Error,
                    "Error",
                    "Ocurrió un error al procesar la solicitud"
                );
                Console.WriteLine($"Error: {ex.Message}");
            }
            finally
            {
                isSaving = false;
                StateHasChanged();
            }
        }

        // Método para manejar el cambio de contraseña
        private async Task HandlePasswordChange()
        {
            try
            {
                if (passwordModel.NewPassword != confirmPassword)
                {
                    NotificationService.Notify(
                        NotificationSeverity.Warning,
                        "Validación",
                        "Las contraseñas no coinciden"
                    );
                    return;
                }

                isChangingPassword = true;

                var response = await ApiClient.PutAsync<BaseResponseModel, UpdatePasswordDto>(
                    $"/api/User/{Id}/password",
                    passwordModel
                );

                if (response?.Success == true)
                {
                    NotificationService.Notify(
                        NotificationSeverity.Success,
                        "Éxito",
                        "Contraseña actualizada exitosamente"
                    );

                    // Limpiar el formulario de contraseña
                    passwordModel = new();
                    confirmPassword = string.Empty;
                }
                else
                {
                    NotificationService.Notify(
                        NotificationSeverity.Error,
                        "Error",
                        response?.ErrorMessage ?? "Error al actualizar la contraseña"
                    );
                }
            }
            catch (Exception ex)
            {
                NotificationService.Notify(
                    NotificationSeverity.Error,
                    "Error",
                    "Ocurrió un error al actualizar la contraseña"
                );
                Console.WriteLine($"Error: {ex.Message}");
            }
            finally
            {
                isChangingPassword = false;
                StateHasChanged();
            }
        }
    }
}
