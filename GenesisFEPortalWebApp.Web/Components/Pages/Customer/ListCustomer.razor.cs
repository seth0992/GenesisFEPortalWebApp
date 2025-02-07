using GenesisFEPortalWebApp.Models.Entities.Core;
using GenesisFEPortalWebApp.Models.Models;
using Microsoft.AspNetCore.Components;
using Newtonsoft.Json;
using Radzen.Blazor;
using Radzen;
using Blazored.Toast.Services;
using GenesisFEPortalWebApp.Models.Entities.Catalog;

namespace GenesisFEPortalWebApp.Web.Components.Pages.Customer
{
    public partial class ListCustomer
    {
        [Inject] private ApiClient ApiClient { get; set; } = default!;
        [Inject] private NavigationManager NavigationManager { get; set; } = default!;
        [Inject] private DialogService DialogService { get; set; } = default!;
        [Inject] private NotificationService NotificationService { get; set; } = default!;
        [Inject] private IToastService ToastService { get; set; } = default!;

        private RadzenDataGrid<CustomerModel> grid = default!;
        private List<CustomerModel> customers = new();
        private List<IdentificationTypeModel> IdentificationTypes = new();
        private bool isLoading = true;

        // Filtros
        private string? searchText;
        private string? selectedIdentificationType;
        private string? selectedStatus;

        protected override async Task OnInitializedAsync()
        {
            await LoadIdentificationTypes();
            await LoadCustomers();
        }

        private async Task LoadIdentificationTypes()
        {
            try
            {
                var response = await ApiClient.GetFromJsonAsync<BaseResponseModel>(
                    "/api/Catalog/identification-types"
                );

                if (response?.Success == true)
                {
                    IdentificationTypes = JsonConvert.DeserializeObject<List<IdentificationTypeModel>>(
                        response.Data.ToString()!
                    )!;
                }
            }
            catch (Exception ex)
            {
                NotificationService.Notify(NotificationSeverity.Error,
                    "Error",
                    "No se pudieron cargar los tipos de identificación");
                Console.WriteLine($"Error cargando tipos de identificación: {ex.Message}");
            }
        }

        private async Task LoadCustomers()
        {
            try
            {
                isLoading = true;
                var response = await ApiClient.GetFromJsonAsync<BaseResponseModel>("/api/Customer");

                if (response?.Success == true)
                {
                    customers = JsonConvert.DeserializeObject<List<CustomerModel>>(
                        response.Data.ToString()!
                    )!;

                    // Aplicar filtros si existen
                    ApplyFilters();
                }
                else
                {
                    NotificationService.Notify(NotificationSeverity.Error,
                        "Error",
                        "No se pudieron cargar los clientes");
                }
            }
            catch (Exception ex)
            {
                NotificationService.Notify(NotificationSeverity.Error,
                    "Error",
                    "Ocurrió un error al cargar los clientes");
                Console.WriteLine($"Error: {ex.Message}");
            }
            finally
            {
                isLoading = false;
            }
        }

        private void ApplyFilters()
        {
            var filteredCustomers = customers.AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchText))
            {
                filteredCustomers = filteredCustomers.Where(c =>
                    c.CustomerName.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
                    c.CommercialName!.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
                    c.Identification.Contains(searchText, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrWhiteSpace(selectedIdentificationType))
            {
                filteredCustomers = filteredCustomers.Where(c =>
                    c.IdentificationTypeId == selectedIdentificationType);
            }

            if (!string.IsNullOrWhiteSpace(selectedStatus))
            {
                bool isActive = selectedStatus == "Activos";
                if (selectedStatus != "Todos")
                {
                    filteredCustomers = filteredCustomers.Where(c => c.IsActive == isActive);
                }
            }

            customers = filteredCustomers.ToList();
        }

        private async Task OnSearch(object? value = null)
        {
            await LoadCustomers();
        }

        private void ClearFilters()
        {
            searchText = null;
            selectedIdentificationType = null;
            selectedStatus = null;
            LoadCustomers();
        }

        private void NavigateToEdit(long customerId)
        {
            NavigationManager.NavigateTo($"/customer/update/{customerId}");
        }

        //private async Task ToggleCustomerStatus(CustomerModel customer)
        //{
        //    var result = await DialogService.Confirm(
        //        $"¿Está seguro que desea {(customer.IsActive ? "desactivar" : "activar")} este cliente?",
        //        $"{(customer.IsActive ? "Desactivar" : "Activar")} Cliente",
        //        new ConfirmOptions { OkButtonText = "Sí", CancelButtonText = "No" });

        //    if (result == true)
        //    {
        //        try
        //        {
        //            var response = await ApiClient.PutAsync<BaseResponseModel, object>(
        //                $"/api/Customer/{customer.ID}/toggle-status",
        //                new { });

        //            if (response?.Success == true)
        //            {
        //                ToastService.ShowSuccess(
        //                    $"Cliente {(customer.IsActive ? "desactivado" : "activado")} exitosamente");
        //                await LoadCustomers();
        //            }
        //            else
        //            {
        //                ToastService.ShowError(
        //                    response?.ErrorMessage ?? "Error al cambiar el estado del cliente");
        //            }
        //        }
        //        catch (Exception ex)
        //        {
        //            ToastService.ShowError("Ocurrió un error al procesar la solicitud");
        //            Console.WriteLine($"Error: {ex.Message}");
        //        }
        //    }
        //}

        private async Task HandleToggleStatus(CustomerModel customer)
        {
            var action = customer.IsActive ? "desactivar" : "activar";
            var confirmed = await DialogService.Confirm(
                $"¿Está seguro que desea {action} este cliente?",
                $"{(customer.IsActive ? "Desactivar" : "Activar")} Cliente",
                new ConfirmOptions { OkButtonText = "Sí", CancelButtonText = "No" });

            if (confirmed ?? false)
            {
                try
                {
                    BaseResponseModel? response;
                    if (customer.IsActive)
                    {
                        response = await ApiClient.DeleteAsync<BaseResponseModel>($"api/Customer/{customer.ID}");
                    }
                    else
                    {
                        response = await ApiClient.PatchAsync<BaseResponseModel>($"api/Customer/{customer.ID}/activate", new { });
                    }

                    if (response?.Success == true)
                    {
                        await LoadCustomers();
                        ToastService.ShowSuccess($"Cliente {(customer.IsActive ? "desactivado" : "activado")} exitosamente");
                    }
                    else
                    {
                        ToastService.ShowError(response?.ErrorMessage ?? $"Error al {action} el cliente");
                    }
                }
                catch (Exception ex)
                {
                    ToastService.ShowError($"Error al {action} el cliente");
                    Console.WriteLine($"Error: {ex.Message}");
                }
            }
        }
    }


    // ListCustomer.razor.cs
    //public partial class ListCustomer
    //{
    //    [Inject] public required ApiClient ApiClient { get; set; }
    //    [Inject] public required NavigationManager NavigationManager { get; set; }
    //    [Inject] public required DialogService DialogService { get; set; }
    //    [Inject] public required IToastService ToastService { get; set; }

    //    private RadzenDataGrid<CustomerModel> grid;
    //    private IQueryable<CustomerModel> customers;

    //    protected override async Task OnInitializedAsync()
    //    {
    //        await LoadCustomers();
    //    }

    //    private async Task LoadCustomers()
    //    {
    //        try
    //        {
    //            var response = await ApiClient.GetFromJsonAsync<BaseResponseModel>("/api/Customer");

    //            if (response?.Success == true)
    //            {
    //                // Actualizamos la lista de clientes
    //                var customerList = JsonConvert.DeserializeObject<List<CustomerModel>>(
    //                    response.Data.ToString()!)!;
    //                customers = customerList.AsQueryable();

    //                // Forzamos la actualización del grid
    //                await InvokeAsync(StateHasChanged);
    //            }
    //            else
    //            {
    //                ToastService.ShowError("Error al cargar los clientes");
    //            }
    //        }
    //        catch (Exception ex)
    //        {
    //            ToastService.ShowError("Error al cargar los clientes");
    //            Console.WriteLine($"Error en LoadCustomers: {ex.Message}");
    //        }
    //    }
    //    private void NavigateToRoute(string route)
    //    {
    //        NavigationManager.NavigateTo($"/customer/{route}");
    //    }

    //    private async Task HandleDelete(CustomerModel customer)
    //    {
    //        try
    //        {
    //            // Mostramos el diálogo de confirmación
    //            var confirmed = await DialogService.Confirm(
    //                $"¿Está seguro que desea eliminar el cliente {customer.CustomerName}?",
    //                "Confirmar eliminación",
    //                new ConfirmOptions
    //                {
    //                    OkButtonText = "Sí",
    //                    CancelButtonText = "No"
    //                });

    //            if (confirmed ?? false)
    //            {
    //                // Realizamos la eliminación
    //                var response = await ApiClient.DeleteAsync<BaseResponseModel>($"/api/Customer/{customer.ID}");

    //                if (response?.Success == true)
    //                {
    //                    // Mostramos el mensaje de éxito
    //                    ToastService.ShowSuccess("Cliente eliminado exitosamente");

    //                    // Esperamos un momento para que se muestre el mensaje
    //                    await Task.Delay(500);

    //                    // Recargamos los datos
    //                    await LoadCustomers();

    //                    // Si estamos usando paginación, volvemos a la primera página
    //                    if (grid != null)
    //                    {
    //                        await grid.FirstPage();
    //                    }
    //                }
    //                else
    //                {
    //                    ToastService.ShowError(response?.ErrorMessage ?? "Error al eliminar el cliente");
    //                }
    //            }
    //        }
    //        catch (Exception ex)
    //        {
    //            ToastService.ShowError("Error al procesar la solicitud");
    //            Console.WriteLine($"Error en HandleDelete: {ex.Message}");
    //        }
    //    }
    //}
}
