using GenesisFEPortalWebApp.Models.Models.Auth;
using GenesisFEPortalWebApp.Models.Models;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;

namespace GenesisFEPortalWebApp.Web.Services
{
    public interface IAuthService
    {
        Task<BaseResponseModel> LoginAsync(LoginDto loginModel);
        Task LogoutAsync();
        Task<bool> IsUserAuthenticated();
    }

    public class AuthService : IAuthService
    {
        private readonly ApiClient _apiClient;
        private readonly ProtectedLocalStorage _localStorage;
        private readonly CustomAuthStateProvider _authStateProvider;

        public AuthService(
            ApiClient apiClient,
            ProtectedLocalStorage localStorage,
            AuthenticationStateProvider authStateProvider)
        {
            _apiClient = apiClient;
            _localStorage = localStorage;
            _authStateProvider = (CustomAuthStateProvider)authStateProvider;
        }

        public async Task<BaseResponseModel> LoginAsync(LoginDto loginModel)
        {
            var response = await _apiClient.PostAsync<BaseResponseModel, LoginDto>("/api/auth/login", loginModel);

            if (response.Success && response.Data != null)
            {
                var authData = System.Text.Json.JsonSerializer.Deserialize<AuthResponseData>(
                    response.Data.ToString()!);

                var userSession = new UserSession
                {
                    Email = authData.User.Email,
                    Role = authData.User.RoleName,
                    TenantId = Convert.ToInt64(authData.User.TenantName),
                    Token = authData.Token,
                    RefreshToken = authData.RefreshToken
                };

                await _authStateProvider.UpdateAuthenticationState(userSession);
            }

            return response;
        }

        public async Task LogoutAsync()
        {
            await _authStateProvider.UpdateAuthenticationState(null);
        }

        public async Task<bool> IsUserAuthenticated()
        {
            var state = await _authStateProvider.GetAuthenticationStateAsync();
            return state.User.Identity?.IsAuthenticated ?? false;
        }
    }
}
