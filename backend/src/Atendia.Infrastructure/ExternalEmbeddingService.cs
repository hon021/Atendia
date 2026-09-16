using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Atendia.Application;

namespace Atendia.Infrastructure;

public sealed class ExternalEmbeddingService : IEmbeddingService
{
    private readonly HttpClient _httpClient;
    private readonly ChatProviderSettings _settings;

    public ExternalEmbeddingService(HttpClient httpClient, ChatProviderSettings settings)
    {
        _httpClient = httpClient;
        _settings = settings;
    }

    public async Task<float[]?> GenerateAsync(string text, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_settings.ApiKey) || _settings.ApiKey == "SET_YOUR_API_KEY")
        {
            return null;
        }

        var payload = new
        {
            model = _settings.EmbeddingModel,
            input = text
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, _settings.BaseUrl + "/embeddings");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _settings.ApiKey);
        request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));

        return document.RootElement
            .GetProperty("data")[0]
            .GetProperty("embedding")
            .EnumerateArray()
            .Select(value => value.GetSingle())
            .ToArray();
    }
}