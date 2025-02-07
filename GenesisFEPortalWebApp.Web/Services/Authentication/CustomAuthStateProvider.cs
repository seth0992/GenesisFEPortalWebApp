using GenesisFEPortalWebApp.Models.Models.Auth;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace GenesisFEPortalWebApp.Web.Services.Authentication
{
    public class CustomAuthStateProvider : AuthenticationStateProvider
    {
        private readonly ProtectedLocalStorage _localStorage;
        private NavigationManager _navigationManager { get; set; } = default!;
        public CustomAuthStateProvider(ProtectedLocalStorage localStorage , NavigationManager navigationManager)
        {
            _localStorage = localStorage;
            _navigationManager = navigationManager;
        }
        public override async Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            try
            {
                var sessionModel = await _localStorage.GetAsync<LoginResponseModel>("sessionState");

                if (!sessionModel.Success || sessionModel.Value == null)
                {
                    return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
                }

                // Verificar expiración del token
                if (sessionModel.Value.TokenExpired < DateTimeOffset.UtcNow.ToUnixTimeSeconds())
                {
                    await _localStorage.DeleteAsync("sessionState");
                    _navigationManager.NavigateTo("/login", true);
                    return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
                }

                var identity = GetClaimsIdentity(sessionModel.Value.Token);
                return new AuthenticationState(new ClaimsPrincipal(identity));
            }
            catch
            {
                return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
            }
        }

        public async Task MarkUserAsAuthenticated(LoginResponseModel model)
        {
            await _localStorage.SetAsync("sessionState", model);
            var identity = GetClaimsIdentity(model.Token);

            // Agregar claims adicionales si es necesario
            if (!identity.HasClaim(c => c.Type == ClaimTypes.Name))
            {
                identity.AddClaim(new Claim(ClaimTypes.Name,
                    $"{model.User.FirstName} {model.User.LastName}"));
            }
            if (!identity.HasClaim(c => c.Type == ClaimTypes.Email))
            {
                identity.AddClaim(new Claim(ClaimTypes.Email, model.User.Email));
            }

            var user = new ClaimsPrincipal(identity);
            NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(user)));
        }

        private ClaimsIdentity GetClaimsIdentity(string token)
        {
            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(token);
            return new ClaimsIdentity(jwtToken.Claims, "jwt");
        }

        public async Task MarkUserAsLoggedOut()
        {
            await _localStorage.DeleteAsync("sessionState");
            var identity = new ClaimsIdentity();
            var user = new ClaimsPrincipal(identity);
            NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(user)));
        }
    }
}
