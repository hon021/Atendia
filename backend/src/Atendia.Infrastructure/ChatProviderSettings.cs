namespace Atendia.Infrastructure;

public sealed class ChatProviderSettings
{
    public string Provider { get; set; } = "OpenRouter";
    public string ApiKey { get; set; } = string.Empty;
    public string Model { get; set; } = "openai/gpt-4o-mini";
    public string BaseUrl { get; set; } = "https://openrouter.ai/api/v1";
    public string SystemPrompt { get; set; } = "Eres un asistente de atención al cliente para PYMEs. Responde solo con información conocida y ofrece derivación a humano cuando no tengas certeza.";
}
