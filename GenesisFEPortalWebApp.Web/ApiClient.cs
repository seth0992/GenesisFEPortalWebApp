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

            if (path.Contains("/api/auth/login"))
            {
                return (T1)(object)await ProcessLoginResponse(response);
            }

            if (response != null && response.IsSuccessStatusCode)
            {
                var jsonString = await response.Content.ReadAsStringAsync();
                var baseResponse = JsonConvert.DeserializeObject<BaseResponseModel>(jsonString);

                if (baseResponse != null && baseResponse.Success)
                {
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

    private async Task<LoginResponseModel> ProcessLoginResponse(HttpResponseMessage response)
    {
        try
        {
            if (!response.IsSuccessStatusCode)
            {
                throw new ApplicationException($"Error de servidor: {response.StatusCode}");
            }

            var jsonString = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"Raw response from login: {jsonString}"); // Para debugging

            var baseResponse = JsonConvert.DeserializeObject<BaseResponseModel>(jsonString);

            if (baseResponse == null)
            {
                throw new ApplicationException("La respuesta del servidor está vacía");
            }

            if (!baseResponse.Success)
            {
                throw new ApplicationException(baseResponse.ErrorMessage);
            }

            // Convertir el objeto Data a LoginResponseModel
            var dataJson = JsonConvert.SerializeObject(baseResponse.Data);
            var loginResponse = JsonConvert.DeserializeObject<LoginResponseModel>(dataJson);

            if (loginResponse == null)
            {
                throw new ApplicationException("Error al procesar la respuesta de login");
            }

            // Validar que tenemos toda la información necesaria
            if (string.IsNullOrEmpty(loginResponse.Token) ||
                string.IsNullOrEmpty(loginResponse.RefreshToken) ||
                loginResponse.User == null)
            {
                throw new ApplicationException("Respuesta de login incompleta");
            }

            return loginResponse;
        }
        catch (JsonSerializationException ex)
        {
            Console.WriteLine($"Error de deserialización: {ex.Message}");
            throw new ApplicationException("Error al procesar la respuesta del servidor", ex);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error general en ProcessLoginResponse: {ex.Message}");
            throw new ApplicationException("Error en el proceso de login", ex);
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



