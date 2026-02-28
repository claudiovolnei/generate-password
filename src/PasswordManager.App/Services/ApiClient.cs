using System.Net.Http.Headers;
using System.Net.Http.Json;
using PasswordManager.App.Models;

namespace PasswordManager.App.Services;

public class ApiClient
{
    private readonly HttpClient _httpClient;
    private readonly AuthState _authState;

    public ApiClient(AuthState authState)
    {
        _authState = authState;
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri("http://password-manager.runasp.net")
        };
    }

    public async Task<bool> LoginAsync(LoginRequest request)
    {
        var response = await _httpClient.PostAsJsonAsync("api/auth/login", request);
        if (!response.IsSuccessStatusCode) return false;

        var payload = await response.Content.ReadFromJsonAsync<LoginResponse>();
        if (string.IsNullOrWhiteSpace(payload?.Token)) return false;

        _authState.SetToken(payload.Token);
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", payload.Token);

        return true;
    }

    public async Task<IReadOnlyCollection<PasswordEntry>> GetPasswordsAsync()
    {
        var response = await _httpClient.GetAsync("api/passwords/");
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<List<PasswordEntry>>() ?? [];
    }

    public async Task<string> GeneratePasswordAsync(GeneratePasswordRequest request)
    {
        var response = await _httpClient.PostAsJsonAsync("api/passwords/generate", request);
        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadFromJsonAsync<GeneratePasswordResponse>();
        return payload?.Password ?? string.Empty;
    }

    public async Task<PasswordEntry?> CreatePasswordAsync(CreatePasswordRequest request)
    {
        var response = await _httpClient.PostAsJsonAsync("api/passwords/", request);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<PasswordEntry>();
    }

    public async Task DeletePasswordAsync(Guid id)
    {
        var response = await _httpClient.DeleteAsync($"api/passwords/{id}");
        response.EnsureSuccessStatusCode();
    }
}
