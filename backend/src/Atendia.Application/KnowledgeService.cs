using Atendia.Domain;

namespace Atendia.Application;

public interface IKnowledgeService
{
    Task<IReadOnlyList<KnowledgeItem>> SearchAsync(string tenantId, string botId, string query, CancellationToken cancellationToken = default);
    Task<KnowledgeItem?> GetByIdAsync(string tenantId, string botId, string id, CancellationToken cancellationToken = default);
    Task AddAsync(KnowledgeItem item, CancellationToken cancellationToken = default);
    Task<bool> UpdateAsync(string tenantId, string botId, string id, string title, string content, string sourceType, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(string tenantId, string botId, string id, CancellationToken cancellationToken = default);
}

public sealed class InMemoryKnowledgeService : IKnowledgeService
{
    private readonly List<KnowledgeItem> _items = new()
    {
        new()
        {
            Id = "faq-1",
            TenantId = "tenant-1",
            BotId = "bot-1",
            Title = "Horarios",
            Content = "Los horarios de atención son de lunes a viernes de 9:00 a 18:00.",
            SourceType = "faq"
        },
        new()
        {
            Id = "faq-2",
            TenantId = "tenant-1",
            BotId = "bot-1",
            Title = "Envíos",
            Content = "Los envíos dentro de la ciudad tienen un plazo de 24 a 48 horas hábiles.",
            SourceType = "faq"
        },
        new()
        {
            Id = "faq-3",
            TenantId = "tenant-1",
            BotId = "bot-1",
            Title = "Contacto",
            Content = "Puedes comunicarte por teléfono o email para asesoría personalizada.",
            SourceType = "faq"
        }
    };

    public Task<IReadOnlyList<KnowledgeItem>> SearchAsync(string tenantId, string botId, string query, CancellationToken cancellationToken = default)
    {
        var normalizedQuery = query.Trim();

        if (string.IsNullOrWhiteSpace(normalizedQuery))
        {
            return Task.FromResult<IReadOnlyList<KnowledgeItem>>(Array.Empty<KnowledgeItem>());
        }

        IReadOnlyList<KnowledgeItem> result = _items
            .Where(item =>
                item.TenantId == tenantId &&
                item.BotId == botId &&
                (item.Title.Contains(normalizedQuery, StringComparison.OrdinalIgnoreCase) ||
                 item.Content.Contains(normalizedQuery, StringComparison.OrdinalIgnoreCase)))
            .ToList();

            return Task.FromResult(result);
    }

            public Task AddAsync(KnowledgeItem item, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(item);

        _items.Add(item);
        return Task.CompletedTask;
    }

    public Task<KnowledgeItem?> GetByIdAsync(string tenantId, string botId, string id, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_items.FirstOrDefault(item => item.Id == id && item.TenantId == tenantId && item.BotId == botId));
    }

    public Task<bool> UpdateAsync(string tenantId, string botId, string id, string title, string content, string sourceType, CancellationToken cancellationToken = default)
    {
        var item = _items.FirstOrDefault(x => x.Id == id && x.TenantId == tenantId && x.BotId == botId);
        if (item is null)
        {
            return Task.FromResult(false);
        }

        item.Title = title;
        item.Content = content;
        item.SourceType = sourceType;
        item.CreatedAtUtc = DateTime.UtcNow;
        return Task.FromResult(true);
    }

    public Task<bool> DeleteAsync(string tenantId, string botId, string id, CancellationToken cancellationToken = default)
    {
        var item = _items.FirstOrDefault(x => x.Id == id && x.TenantId == tenantId && x.BotId == botId);
        return Task.FromResult(item is not null && _items.Remove(item));
    }
}
