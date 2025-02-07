using Blazored.Toast.Services;
using GenesisFEPortalWebApp.Models.Entities.Security;
using GenesisFEPortalWebApp.Models.Models.User;
using GenesisFEPortalWebApp.Models.Models;
using Microsoft.AspNetCore.Components;
using Newtonsoft.Json;
using Radzen.Blazor;
using Radzen;

namespace GenesisFEPortalWebApp.Web.Components.Pages.User
{
    public partial class ListUsers
    {
        // Inyección de dependencias
        [Inject] private ApiClient ApiClient { get; set; } = default!;
        [Inject] private NavigationManager NavigationManager { get; set; } = default!;
        [Inject] private DialogService DialogService { get; set; } = default!;
        [Inject] private NotificationService NotificationService { get; set; } = default!;
        [Inject] private IToastService ToastService { get; set; } = default!;

        // Referencias a componentes
        private RadzenDataGrid<UserListDto> grid = default!;

        // Estado del componente
        private List<UserListDto> users = new();
        private List<RoleModel> availableRoles = new();
        private bool isLoading = true;

        // Filtros
        private string? searchText;
        private long? selectedRole;
        private string? selectedStatus;

        // Inicialización del componente
        protected override async Task OnInitializedAsync()
        {
            await LoadRoles();
            await LoadUsers();
        }

        // Carga de datos
        private async Task LoadRoles()
        {
            try
            {
                var response = await ApiClient.GetFromJsonAsync<BaseResponseModel>(
                    "/api/User/available-roles"
                );

                if (response?.Success == true)
                {
                    availableRoles = JsonConvert.DeserializeObject<List<RoleModel>>(
                        response.Data.ToString()!
                    )!;
                }
            }
            catch (Exception ex)
            {
                NotificationService.Notify(NotificationSeverity.Error,
                    "Error",
                    "No se pudieron cargar los roles");
                Console.WriteLine($"Error cargando roles: {ex.Message}");
            }
        }

        private async Task LoadUsers()
        {
            try
            {
                isLoading = true;
                var response = await ApiClient.GetFromJsonAsync<BaseResponseModel>("/api/User");

                if (response?.Success == true)
                {
                    users = JsonConvert.DeserializeObject<List<UserListDto>>(
                        response.Data.ToString()!
                    )!;

                    // Aplicar filtros si existen
                    ApplyFilters();
                }
                else
                {
                    NotificationService.Notify(NotificationSeverity.Error,
                        "Error",
                        "No se pudieron cargar los usuarios");
                }
            }
            catch (Exception ex)
            {
                NotificationService.Notify(NotificationSeverity.Error,
                    "Error",
                    "Ocurrió un error al cargar los usuarios");
                Console.WriteLine($"Error: {ex.Message}");
            }
            finally
            {
                isLoading = false;
                StateHasChanged();
            }
        }

        // Manejo de filtros
        private void ApplyFilters()
        {
            var filteredUsers = users.AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchText))
            {
                filteredUsers = filteredUsers.Where(u =>
                    u.Email.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
                    u.FirstName.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
                    u.LastName.Contains(searchText, StringComparison.OrdinalIgnoreCase));
            }

            if (selectedRole.HasValue)
            {
                filteredUsers = filteredUsers.Where(u => u.RoleId == selectedRole.Value);
            }

            if (!string.IsNullOrWhiteSpace(selectedStatus))
            {
                bool isActive = selectedStatus == "Activos";
                if (selectedStatus != "Todos")
                {
                    filteredUsers = filteredUsers.Where(u => u.IsActive == isActive);
                }
            }

            users = filteredUsers.ToList();
        }

        private async Task OnSearch(object? value = null)
        {
            await LoadUsers();
        }

        private async void ClearFilters()
        {
            searchText = null;
            selectedRole = null;
            selectedStatus = null;
           await LoadUsers();
        }

        // Navegación
        private void NavigateToEdit(long userId)
        {
            NavigationManager.NavigateTo($"/users/edit/{userId}");
        }

        // Acciones de usuario
        private async Task HandleToggleStatus(UserListDto user)
        {
            var action = user.IsActive ? "desactivar" : "activar";
            var confirmed = await DialogService.Confirm(
                $"¿Está seguro que desea {action} este usuario?",
                $"{(user.IsActive ? "Desactivar" : "Activar")} Usuario",
                new ConfirmOptions
                {
                    OkButtonText = "Sí",
                    CancelButtonText = "No"
                });

            if (confirmed ?? false)
            {
                try
                {
                    var response = await ApiClient.PatchAsync<BaseResponseModel>(
                        $"api/User/{user.Id}/toggle-status",
                        new { });

                    if (response?.Success == true)
                    {
                        ToastService.ShowSuccess(
                            $"Usuario {(user.IsActive ? "desactivado" : "activado")} exitosamente");
                        await LoadUsers();
                    }
                    else
                    {
                        ToastService.ShowError(
                            response?.ErrorMessage ?? $"Error al {action} el usuario");
                    }
                }
                catch (Exception ex)
                {
                    ToastService.ShowError($"Error al {action} el usuario");
                    Console.WriteLine($"Error: {ex.Message}");
                }
            }
        }
    }
}
