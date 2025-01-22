using GenesisFEPortalWebApp.Models.Models;
using GenesisFEPortalWebApp.Models.Models.Auth;
using GenesisFEPortalWebApp.Web.Services.Authentication;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using Newtonsoft.Json;
using System.Net.Http.Headers;

namespace GenesisFEPortalWebApp.Web;


public class ApiClient(HttpClient httpClient,
    ProtectedLocalStorage localStorage,
    NavigationManager navigationManager,
    AuthenticationStateProvider authStateProvider)
{

    public async Task SetAuthorizationHeader()
    {
        var session = await localStorage.GetAsync<LoginResponseModel>("sessionState");
        if (session.Value != null)
        {
            if (session.Value.TokenExpired < DateTimeOffset.UtcNow.ToUnixTimeSeconds())
            {
                var newToken = await RefreshToken(session.Value.RefreshToken);
                if (newToken != null)
                {
                    await ((CustomAuthStateProvider)authStateProvider)
                        .MarkUserAsAuthenticated(newToken);
                }
                else
                {
                    await ((CustomAuthStateProvider)authStateProvider).MarkUserAsLoggedOut();
                    navigationManager.NavigateTo("/login");
                }
            }

            httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", session.Value.Token);
        }
    }

    private async Task<LoginResponseModel?> RefreshToken(string refreshToken)
    {
        return await httpClient.GetFromJsonAsync<LoginResponseModel>(
            $"/api/auth/refreshToken?token={refreshToken}");
    }

    public async Task<T> GetFromJsonAsync<T>(string path)
    {
        await SetAuthorizationHeader();
        var result = await httpClient.GetFromJsonAsync<T>(path);
        return result ?? throw new InvalidOperationException("Received null response from the server.");
    }

    public async Task<T1> PostAsync<T1, T2>(string path, T2 postModel)
    {
        await SetAuthorizationHeader();
        try
        {
            var response = await httpClient.PostAsJsonAsync(path, postModel);
            if (response != null && response.IsSuccessStatusCode)
            {
                var jsonString = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Raw response: {jsonString}"); // Para debugging

                // Primero deserializamos a un objeto dinámico para manejar la estructura anidada
                var baseResponse = JsonConvert.DeserializeObject<BaseResponseModel>(jsonString);

                if (baseResponse != null && baseResponse.Success)
                {
                    // Convertimos el objeto data a JSON y luego al tipo deseado
                    string dataJson = JsonConvert.SerializeObject(baseResponse.Data);
                    return JsonConvert.DeserializeObject<T1>(dataJson)!;
                }
                else
                {
                    throw new ApplicationException(baseResponse?.ErrorMessage ?? "Error desconocido en la respuesta");
                }
            }

            throw new HttpRequestException($"Error en la respuesta HTTP: {response?.StatusCode}");
        }
        catch (JsonSerializationException ex)
        {
            Console.WriteLine($"Error de serialización: {ex.Message}");
            throw;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error general: {ex.Message}");
            throw new HttpRequestException($"Error en la petición a {path}: {ex.Message}", ex);
        }
    }

    public async Task<T1> PutAsync<T1, T2>(string path, T2 postModel)
    {
        await SetAuthorizationHeader();
        var response = await httpClient.PutAsJsonAsync(path, postModel);
        if (response != null && response.IsSuccessStatusCode)
        {
            return JsonConvert.DeserializeObject<T1>(await response.Content.ReadAsStringAsync()!)!;
        }
        return default!;
    }

    public async Task<T> DeleteAsync<T>(string path)
    {
        await SetAuthorizationHeader();
        var result = await httpClient.DeleteFromJsonAsync<T>(path);
        return result ?? throw new InvalidOperationException("Received null response from the server.");
    }
}



