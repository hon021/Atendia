using Atendia.Domain;
using System.Diagnostics;

namespace Atendia.Application;

public interface IChatModel
{
    Task<ChatResponse> GenerateAsync(ChatRequest request, CancellationToken cancellationToken);
}

public sealed class ChatContext
{
    public string TenantId { get; init; } = string.Empty;
    public string BotId { get; init; } = string.Empty;
    public BotConfiguration? BotConfiguration { get; init; }
    public IReadOnlyList<KnowledgeItem> Knowledge { get; init; } = Array.Empty<KnowledgeItem>();
    public IReadOnlyList<string> ConversationHistory { get; init; } = Array.Empty<string>();
}

public sealed class ChatService
{
    private readonly IChatModel _chatModel;
    private readonly IKnowledgeService _knowledgeService;
    private readonly IConversationRepository _conversationRepository;
    private readonly IMessageRepository _messageRepository;
    private readonly IUsageRepository _usageRepository;

    public ChatService(
        IChatModel chatModel,
        IKnowledgeService knowledgeService,
        IConversationRepository conversationRepository,
        IMessageRepository messageRepository,
        IUsageRepository usageRepository)
    {
        _chatModel = chatModel;
        _knowledgeService = knowledgeService;
        _conversationRepository = conversationRepository;
        _messageRepository = messageRepository;
        _usageRepository = usageRepository;
    }

    public async Task<ChatResponse> SendAsync(ChatRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.TenantId))
        {
            throw new InvalidOperationException("El tenant es obligatorio.");
        }

        if (string.IsNullOrWhiteSpace(request.BotId))
        {
            throw new InvalidOperationException("El bot es obligatorio.");
        }

        if (string.IsNullOrWhiteSpace(request.Message))
        {
            throw new InvalidOperationException("El mensaje no puede estar vacío.");
        }

        var conversation = GetOrCreateConversation(request);
        _messageRepository.Add(new Message
        {
            Id = Guid.NewGuid().ToString("N"),
            ConversationId = conversation.Id,
            TenantId = request.TenantId,
            Sender = "user",
            Content = request.Message
        });

        var context = BuildContext(request);
        _ = context;
        var stopwatch = Stopwatch.StartNew();
        var response = await _chatModel.GenerateAsync(request, cancellationToken);
        stopwatch.Stop();

        if (!string.Equals(response.TenantId, request.TenantId, StringComparison.Ordinal))
        {
            response.TenantId = request.TenantId;
        }

        if (!string.Equals(response.BotId, request.BotId, StringComparison.Ordinal))
        {
            response.BotId = request.BotId;
        }

        if (string.IsNullOrWhiteSpace(response.ConversationId))
        {
            response.ConversationId = conversation.Id;
        }

        _messageRepository.Add(new Message
        {
            Id = Guid.NewGuid().ToString("N"),
            ConversationId = conversation.Id,
            TenantId = request.TenantId,
            Sender = "assistant",
            Content = response.Content
        });

        _usageRepository.Add(new UsageRecord
        {
            Id = Guid.NewGuid().ToString("N"),
            TenantId = request.TenantId,
            BotId = request.BotId,
            ConversationId = conversation.Id,
            Model = string.IsNullOrWhiteSpace(response.Model) ? "unknown" : response.Model,
            InputTokens = response.InputTokens > 0 ? response.InputTokens : EstimateTokens(request.Message),
            OutputTokens = response.OutputTokens > 0 ? response.OutputTokens : EstimateTokens(response.Content),
            EstimatedCost = response.EstimatedCost,
            LatencyMs = stopwatch.ElapsedMilliseconds
        });

        return response;
    }

    private Conversation GetOrCreateConversation(ChatRequest request)
    {
        if (!string.IsNullOrWhiteSpace(request.ConversationId))
        {
            var existing = _conversationRepository.GetById(request.ConversationId, request.TenantId, request.BotId);
            if (existing is null)
            {
                throw new InvalidOperationException("La conversación no existe para el tenant y bot indicados.");
            }

            return existing;
        }

        var conversation = new Conversation
        {
            Id = Guid.NewGuid().ToString("N"),
            TenantId = request.TenantId,
            BotId = request.BotId,
            Status = "AI_HANDLING"
        };

        _conversationRepository.Add(conversation);
        return conversation;
    }

    private static int EstimateTokens(string content)
    {
        return Math.Max(1, (content.Length + 3) / 4);
    }

    private ChatContext BuildContext(ChatRequest request)
    {
        var knowledge = _knowledgeService.Search(request.TenantId, request.BotId, request.Message);

        var history = new List<string>
        {
            $"Usuario: {request.Message}"
        };

        return new ChatContext
        {
            TenantId = request.TenantId,
            BotId = request.BotId,
            BotConfiguration = new BotConfiguration
            {
                BotId = request.BotId,
                TenantId = request.TenantId,
                BusinessName = "Atendia Demo",
                Sector = "Servicios",
                SystemPrompt = "Responde solo con información conocida y ofrece derivación a humano si no sabes."
            },
            Knowledge = knowledge,
            ConversationHistory = history
        };
    }
}
