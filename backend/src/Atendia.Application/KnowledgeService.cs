using Atendia.Domain;

namespace Atendia.Application;

public interface IKnowledgeService
{
    IReadOnlyList<KnowledgeItem> Search(string tenantId, string botId, string query);
    void Add(KnowledgeItem item);
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

    public IReadOnlyList<KnowledgeItem> Search(string tenantId, string botId, string query)
    {
        var normalizedQuery = query.Trim();

        if (string.IsNullOrWhiteSpace(normalizedQuery))
        {
            return Array.Empty<KnowledgeItem>();
        }

        return _items
            .Where(item =>
                item.TenantId == tenantId &&
                item.BotId == botId &&
                (item.Title.Contains(normalizedQuery, StringComparison.OrdinalIgnoreCase) ||
                 item.Content.Contains(normalizedQuery, StringComparison.OrdinalIgnoreCase)))
            .ToList();
    }

    public void Add(KnowledgeItem item)
    {
        ArgumentNullException.ThrowIfNull(item);

        _items.Add(item);
    }
}
