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
        var isOllama = string.Equals(_settings.Provider, "Ollama", StringComparison.OrdinalIgnoreCase);
        if (!isOllama && (string.IsNullOrWhiteSpace(_settings.ApiKey) || _settings.ApiKey == "SET_YOUR_API_KEY"))
        {
            return null;
        }

        var payload = new Dictionary<string, object>
        {
            ["model"] = _settings.EmbeddingModel,
            ["input"] = text
        };
        if (_settings.EmbeddingDimensions is int dimensions)
        {
            payload["dimensions"] = dimensions;
        }

        using var request = new HttpRequestMessage(HttpMethod.Post, _settings.BaseUrl.TrimEnd('/') + "/embeddings");
        if (!isOllama)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _settings.ApiKey);
        }
        request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));

        var embedding = document.RootElement
            .GetProperty("data")[0]
            .GetProperty("embedding")
            .EnumerateArray()
            .Select(value => value.GetSingle())
            .ToArray();
        if (_settings.EmbeddingDimensions is int expectedDimensions && embedding.Length != expectedDimensions)
        {
            throw new InvalidOperationException($"El proveedor devolvió {embedding.Length} dimensiones; se esperaban {expectedDimensions}.");
        }

        return embedding;
    }
}