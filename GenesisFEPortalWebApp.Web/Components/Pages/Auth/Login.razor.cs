using GenesisFEPortalWebApp.Models.Models.Auth;
using GenesisFEPortalWebApp.Models.Models;

namespace GenesisFEPortalWebApp.Web.Components.Pages.Auth
{
    public partial class Login
    {
        private LoginDto model = new();

        private async Task OnSubmit()
        {
            try
            {
                var response = await ApiClient.PostAsync<BaseResponseModel, LoginDto>("/api/Auth/login", model);

                if (response.Success)
                {
                    // TODO: Almacenar el token en LocalStorage o similar
                    ToastService.ShowSuccess("Inicio de sesión exitoso");
                    NavigationManager.NavigateTo("/");
                }
                else
                {
                    ToastService.ShowError(response.ErrorMessage ?? "Error al iniciar sesión");
                }
            }
            catch (Exception ex)
            {
                ToastService.ShowError("Error al conectar con el servidor");
            }
        }
    }
}
