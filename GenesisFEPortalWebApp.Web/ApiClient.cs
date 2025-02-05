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
        try
        {
            var sessionResult = await localStorage.GetAsync<LoginResponseModel>("sessionState");
            var session = sessionResult.Success ? sessionResult.Value : null;

            if (session == null || string.IsNullOrEmpty(session.Token))
            {
                throw new Exception("No session found");
            }

            // Agregar el token a los headers
            httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", session.Token);
        }
        catch (Exception)
        {
            await ((CustomAuthStateProvider)authStateProvider).MarkUserAsLoggedOut();
            navigationManager.NavigateTo("/login");
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

    /// <summary>
    /// Realiza una petición POST asíncrona
    /// </summary>
    /// <typeparam name="T1">Tipo de retorno esperado. Puede ser:
    /// - BaseResponseModel: Retorna la respuesta completa
    /// - bool: Retorna el valor de Success
    /// - otro tipo: Intenta deserializar desde Data
    /// </typeparam>
    /// <typeparam name="T2">Tipo del modelo a enviar</typeparam>
    public async Task<T1> PostAsync<T1, T2>(string path, T2 postModel)
    {
        await SetAuthorizationHeader();
        try
        {
            // Realizamos la petición POST
            var response = await httpClient.PostAsJsonAsync(path, postModel);

            // Caso especial para el login
            if (path.Contains("/api/auth/login"))
            {
                return (T1)(object)await ProcessLoginResponse(response);
            }

            if (response != null && response.IsSuccessStatusCode)
            {
                // Leemos el contenido de la respuesta
                var jsonString = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Respuesta del servidor: {jsonString}"); // Para diagnóstico

                // Deserializamos a BaseResponseModel
                var baseResponse = JsonConvert.DeserializeObject<BaseResponseModel>(jsonString);

                if (baseResponse != null)
                {
                    // Si T1 es BaseResponseModel, devolvemos directamente
                    if (typeof(T1) == typeof(BaseResponseModel))
                    {
                        return (T1)(object)baseResponse;
                    }

                    // Para otros tipos, intentamos deserializar el Data
                    if (baseResponse.Success)
                    {
                        try
                        {
                            // Si Data es null, y T1 es un tipo por valor (como bool),
                            // devolvemos el valor Success
                            if (baseResponse.Data == null && typeof(T1).IsValueType)
                            {
                                return (T1)(object)baseResponse.Success;
                            }

                            // En otro caso, intentamos deserializar Data
                            string dataJson = JsonConvert.SerializeObject(baseResponse.Data);
                            var result = JsonConvert.DeserializeObject<T1>(dataJson);
                            return result!;
                        }
                        catch (JsonSerializationException ex)
                        {
                            Console.WriteLine($"Error deserializando Data: {ex.Message}");
                            // Si falla la deserialización y T1 es bool, devolvemos Success
                            if (typeof(T1) == typeof(bool))
                            {
                                return (T1)(object)baseResponse.Success;
                            }
                            throw;
                        }
                    }
                    throw new ApplicationException(baseResponse.ErrorMessage ?? "Error desconocido en la respuesta");
                }
                throw new ApplicationException("Respuesta nula del servidor");
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



