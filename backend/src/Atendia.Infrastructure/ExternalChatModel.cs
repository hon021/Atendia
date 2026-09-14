using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Atendia.Application;
using Atendia.Domain;

namespace Atendia.Infrastructure;

public sealed class ExternalChatModel : IChatModel
{
    private readonly HttpClient _httpClient;
    private readonly ChatProviderSettings _settings;

    public ExternalChatModel(HttpClient httpClient, ChatProviderSettings settings)
    {
        _httpClient = httpClient;
        _settings = settings;
    }

    public async Task<ChatResponse> GenerateAsync(ChatRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_settings.ApiKey) || _settings.ApiKey == "SET_YOUR_API_KEY")
        {
            return new ChatResponse
            {
                TenantId = request.TenantId,
                BotId = request.BotId,
                Content = "El proveedor externo no está configurado aún. Configura la API key para habilitar el modelo.",
                ConversationId = request.ConversationId
            };
        }

        var payload = new
        {
            model = _settings.Model,
            messages = new[]
            {
                new { role = "system", content = _settings.SystemPrompt },
                new { role = "user", content = request.Message }
            }
        };

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, _settings.BaseUrl + "/chat/completions");
        httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _settings.ApiKey);
        httpRequest.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        httpRequest.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

        using var httpResponse = await _httpClient.SendAsync(httpRequest, cancellationToken);
        httpResponse.EnsureSuccessStatusCode();

        var json = await httpResponse.Content.ReadAsStringAsync(cancellationToken);
        using var document = JsonDocument.Parse(json);

        var content = document.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString();

        var inputTokens = 0;
        var outputTokens = 0;
        if (document.RootElement.TryGetProperty("usage", out var usage))
        {
            if (usage.TryGetProperty("prompt_tokens", out var promptTokens))
            {
                inputTokens = promptTokens.GetInt32();
            }

            if (usage.TryGetProperty("completion_tokens", out var completionTokens))
            {
                outputTokens = completionTokens.GetInt32();
            }
        }

        return new ChatResponse
        {
            TenantId = request.TenantId,
            BotId = request.BotId,
            Content = content ?? "No se pudo generar una respuesta válida.",
            ConversationId = request.ConversationId,
            Model = _settings.Model,
            InputTokens = inputTokens,
            OutputTokens = outputTokens
        };
    }
}
