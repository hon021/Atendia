namespace Atendia.Domain;

public sealed class Tenant
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}

public sealed class Bot
{
    public string Id { get; set; } = string.Empty;
    public string TenantId { get; set; } = string.Empty;
    public string PublicKey { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? BusinessDescription { get; set; }
    public string? Tone { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}

public sealed class BotConfiguration
{
    public string Id { get; set; } = string.Empty;
    public string BotId { get; set; } = string.Empty;
    public string TenantId { get; set; } = string.Empty;
    public string? BusinessName { get; set; }
    public string? Sector { get; set; }
    public string? ContactPhone { get; set; }
    public string? ContactEmail { get; set; }
    public string? Website { get; set; }
    public string? HandOffChannel { get; set; }
    public string? SystemPrompt { get; set; }
}

public sealed class KnowledgeItem
{
    public string Id { get; set; } = string.Empty;
    public string TenantId { get; set; } = string.Empty;
    public string BotId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string SourceType { get; set; } = "faq";
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}

public sealed class Conversation
{
    public string Id { get; set; } = string.Empty;
    public string TenantId { get; set; } = string.Empty;
    public string BotId { get; set; } = string.Empty;
    public string Status { get; set; } = "OPEN";
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}

public sealed class Message
{
    public string Id { get; set; } = string.Empty;
    public string ConversationId { get; set; } = string.Empty;
    public string TenantId { get; set; } = string.Empty;
    public string Sender { get; set; } = "user";
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}

public sealed class Lead
{
    public string Id { get; set; } = string.Empty;
    public string TenantId { get; set; } = string.Empty;
    public string BotId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Interest { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}

public sealed class UsageRecord
{
    public string Id { get; set; } = string.Empty;
    public string TenantId { get; set; } = string.Empty;
    public string BotId { get; set; } = string.Empty;
    public string ConversationId { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public int InputTokens { get; set; }
    public int OutputTokens { get; set; }
    public int TotalTokens => InputTokens + OutputTokens;
    public decimal EstimatedCost { get; set; }
    public long LatencyMs { get; set; }
    public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;
}

public sealed class ChatRequest
{
    public string TenantId { get; set; } = string.Empty;
    public string BotId { get; set; } = string.Empty;
    public string BotKey { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? ConversationId { get; set; }
}

public sealed class ChatResponse
{
    public string TenantId { get; set; } = string.Empty;
    public string BotId { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string? ConversationId { get; set; }
    public string? Model { get; set; }
    public int InputTokens { get; set; }
    public int OutputTokens { get; set; }
    public decimal EstimatedCost { get; set; }
}
