using Atendia.Application;
using Atendia.Domain;
using Microsoft.EntityFrameworkCore;

namespace Atendia.Infrastructure;

public sealed class EfKnowledgeService : IKnowledgeService
{
    private readonly AtendiaDbContext _dbContext;

    public EfKnowledgeService(AtendiaDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public IReadOnlyList<KnowledgeItem> Search(string tenantId, string botId, string query)
    {
        if (string.IsNullOrWhiteSpace(tenantId))
        {
            throw new InvalidOperationException("El tenantId es obligatorio.");
        }

        if (string.IsNullOrWhiteSpace(botId))
        {
            throw new InvalidOperationException("El botId es obligatorio.");
        }

        var normalizedQuery = query?.Trim();

        var items = _dbContext.KnowledgeItems
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.BotId == botId)
            .OrderByDescending(x => x.CreatedAtUtc)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(normalizedQuery))
        {
            items = items.Where(x =>
                x.Title.Contains(normalizedQuery) ||
                x.Content.Contains(normalizedQuery));
        }

        return items.ToList();
    }

    public void Add(KnowledgeItem item)
    {
        ArgumentNullException.ThrowIfNull(item);

        if (string.IsNullOrWhiteSpace(item.TenantId))
        {
            throw new InvalidOperationException("El TenantId es obligatorio.");
        }

        if (string.IsNullOrWhiteSpace(item.BotId))
        {
            throw new InvalidOperationException("El BotId es obligatorio.");
        }

        if (string.IsNullOrWhiteSpace(item.Title))
        {
            throw new InvalidOperationException("El título es obligatorio.");
        }

        if (string.IsNullOrWhiteSpace(item.Content))
        {
            throw new InvalidOperationException("El contenido es obligatorio.");
        }

        if (string.IsNullOrWhiteSpace(item.Id))
        {
            item.Id = Guid.NewGuid().ToString("N");
        }

        item.CreatedAtUtc = DateTime.UtcNow;
        _dbContext.KnowledgeItems.Add(item);
        _dbContext.SaveChanges();
    }
}
