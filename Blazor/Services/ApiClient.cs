using System.Net.Http.Json;
using System.Text.Json;
using Shared.Models.Dtos;

namespace Blazor.Services;

public interface IApiClient
{
    Task<T?> GetAsync<T>(string endpoint);
    Task<T?> PostAsync<T>(string endpoint, object data);
    Task<T?> PutAsync<T>(string endpoint, object data);
    Task DeleteAsync(string endpoint);
    Task<IEnumerable<IncidentResponseDto>> GetAllIncidentsAsync();
    Task<LoginResponseDto?> LoginAsync(LoginDto loginDto);
    Task LogoutAsync();
}

public class ApiClient : IApiClient
{
    private readonly HttpClient _httpClient;
    private readonly JsonSerializerOptions _jsonOptions;

    public ApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
    }

    public async Task<T?> GetAsync<T>(string endpoint)
    {
        var response = await _httpClient.GetAsync(endpoint);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<T>(_jsonOptions);
    }

    public async Task<T?> PostAsync<T>(string endpoint, object data)
    {
        var response = await _httpClient.PostAsJsonAsync(endpoint, data, _jsonOptions);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<T>(_jsonOptions);
    }

    public async Task<T?> PutAsync<T>(string endpoint, object data)
    {
        var response = await _httpClient.PutAsJsonAsync(endpoint, data, _jsonOptions);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<T>(_jsonOptions);
    }

    public async Task DeleteAsync(string endpoint)
    {
        var response = await _httpClient.DeleteAsync(endpoint);
        response.EnsureSuccessStatusCode();
    }

    public async Task<IEnumerable<IncidentResponseDto>> GetAllIncidentsAsync()
    {
        return await GetAsync<IEnumerable<IncidentResponseDto>>("api/Incident") ?? Enumerable.Empty<IncidentResponseDto>();
    }

    public async Task<LoginResponseDto?> LoginAsync(LoginDto loginDto)
    {
        return await PostAsync<LoginResponseDto>("api/Auth/login", loginDto);
    }

    public async Task LogoutAsync()
    {
        await PostAsync<object>("api/Auth/logout", new { });
    }
} 