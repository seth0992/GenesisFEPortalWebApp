using GenesisFEPortalWebApp.Models.Entities.Core;
using GenesisFEPortalWebApp.Models.Models;
using Microsoft.AspNetCore.Components;
using Newtonsoft.Json;
using Radzen.Blazor;
using Radzen;
using Blazored.Toast.Services;

namespace GenesisFEPortalWebApp.Web.Components.Pages.Customer
{
    // ListCustomer.razor.cs
    public partial class ListCustomer
    {
        [Inject] public required ApiClient ApiClient { get; set; }
        [Inject] public required NavigationManager NavigationManager { get; set; }
        [Inject] public required DialogService DialogService { get; set; }
        [Inject] public required IToastService ToastService { get; set; }

        private RadzenDataGrid<CustomerModel> grid;
        private IQueryable<CustomerModel> customers;

        protected override async Task OnInitializedAsync()
        {
            await LoadCustomers();
        }

        private async Task LoadCustomers()
        {
            try
            {
                var response = await ApiClient.GetFromJsonAsync<BaseResponseModel>("/api/Customer");

                if (response?.Success == true)
                {
                    // Actualizamos la lista de clientes
                    var customerList = JsonConvert.DeserializeObject<List<CustomerModel>>(
                        response.Data.ToString()!)!;
                    customers = customerList.AsQueryable();

                    // Forzamos la actualización del grid
                    await InvokeAsync(StateHasChanged);
                }
                else
                {
                    ToastService.ShowError("Error al cargar los clientes");
                }
            }
            catch (Exception ex)
            {
                ToastService.ShowError("Error al cargar los clientes");
                Console.WriteLine($"Error en LoadCustomers: {ex.Message}");
            }
        }
        private void NavigateToRoute(string route)
        {
            NavigationManager.NavigateTo($"/customer/{route}");
        }

        private async Task HandleDelete(CustomerModel customer)
        {
            try
            {
                // Mostramos el diálogo de confirmación
                var confirmed = await DialogService.Confirm(
                    $"¿Está seguro que desea eliminar el cliente {customer.CustomerName}?",
                    "Confirmar eliminación",
                    new ConfirmOptions
                    {
                        OkButtonText = "Sí",
                        CancelButtonText = "No"
                    });

                if (confirmed ?? false)
                {
                    // Realizamos la eliminación
                    var response = await ApiClient.DeleteAsync<BaseResponseModel>($"/api/Customer/{customer.ID}");

                    if (response?.Success == true)
                    {
                        // Mostramos el mensaje de éxito
                        ToastService.ShowSuccess("Cliente eliminado exitosamente");

                        // Esperamos un momento para que se muestre el mensaje
                        await Task.Delay(500);

                        // Recargamos los datos
                        await LoadCustomers();

                        // Si estamos usando paginación, volvemos a la primera página
                        if (grid != null)
                        {
                            await grid.FirstPage();
                        }
                    }
                    else
                    {
                        ToastService.ShowError(response?.ErrorMessage ?? "Error al eliminar el cliente");
                    }
                }
            }
            catch (Exception ex)
            {
                ToastService.ShowError("Error al procesar la solicitud");
                Console.WriteLine($"Error en HandleDelete: {ex.Message}");
            }
        }
    }
}
