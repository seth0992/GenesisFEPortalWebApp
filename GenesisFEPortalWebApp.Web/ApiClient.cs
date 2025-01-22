using Newtonsoft.Json;

namespace GenesisFEPortalWebApp.Web;


public class ApiClient(HttpClient httpClient)
{
    public Task<T> GetFromJsonAsync<T>(string path)
    {
        return httpClient.GetFromJsonAsync<T>(path)!;
    }

    public async Task<T1> PostAsync<T1, T2>(string path, T2 postModel)
    {
        try
        {
            var response = await httpClient.PostAsJsonAsync(path, postModel);
            if (response != null && response.IsSuccessStatusCode)
            {
                return JsonConvert.DeserializeObject<T1>(await response.Content.ReadAsStringAsync())!;
            }
        }
        catch (Exception ex)
        {
            // Log the error or handle it
            throw new HttpRequestException($"Error while posting to {path}: {ex.Message}", ex);
        }
        return default!;
    }

    public async Task<T1> PutAsync<T1, T2>(string path, T2 postModel)
    {
        var response = await httpClient.PutAsJsonAsync(path, postModel);
        if (response != null && response.IsSuccessStatusCode)
        {
            return JsonConvert.DeserializeObject<T1>(await response.Content.ReadAsStringAsync()!)!;
        }
        return default!;
    }

    public Task<T> DeleteAsync<T>(string path)
    {
        return httpClient.DeleteFromJsonAsync<T>(path)!;
    }
}



