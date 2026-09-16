using Atendia.Application;
using Atendia.Domain;
using Microsoft.EntityFrameworkCore;
using Pgvector;
using Pgvector.EntityFrameworkCore;

namespace Atendia.Infrastructure;

public sealed class EfKnowledgeService : IKnowledgeService
{
    private readonly AtendiaDbContext _dbContext;
    private readonly IEmbeddingService _embeddingService;

    public EfKnowledgeService(AtendiaDbContext dbContext, IEmbeddingService embeddingService)
    {
        _dbContext = dbContext;
        _embeddingService = embeddingService;
    }

    public async Task<IReadOnlyList<KnowledgeItem>> SearchAsync(string tenantId, string botId, string query, CancellationToken cancellationToken = default)
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
        if (string.IsNullOrWhiteSpace(normalizedQuery))
        {
            return Array.Empty<KnowledgeItem>();
        }

        var items = _dbContext.KnowledgeItems
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.BotId == botId);

        var queryEmbedding = await _embeddingService.GenerateAsync(normalizedQuery, cancellationToken);
        if (queryEmbedding is not null && _dbContext.Database.ProviderName?.Contains("Npgsql", StringComparison.Ordinal) == true)
        {
            var vector = new Vector(queryEmbedding);
            return await items
                .Where(x => x.Embedding != null)
                .OrderBy(x => x.Embedding!.CosineDistance(vector))
                .Take(5)
                .ToListAsync(cancellationToken);
        }

        return await items
            .Where(x => x.Title.Contains(normalizedQuery) || x.Content.Contains(normalizedQuery))
            .OrderByDescending(x => x.CreatedAtUtc)
            .Take(5)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(KnowledgeItem item, CancellationToken cancellationToken = default)
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

        var embedding = await _embeddingService.GenerateAsync($"{item.Title}\n{item.Content}", cancellationToken);
        item.Embedding = embedding is null ? null : new Vector(embedding);
        item.CreatedAtUtc = DateTime.UtcNow;
        _dbContext.KnowledgeItems.Add(item);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public Task<KnowledgeItem?> GetByIdAsync(string tenantId, string botId, string id, CancellationToken cancellationToken = default)
    {
        return _dbContext.KnowledgeItems.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId && x.BotId == botId, cancellationToken);
    }

    public async Task<bool> UpdateAsync(string tenantId, string botId, string id, string title, string content, string sourceType, CancellationToken cancellationToken = default)
    {
        var item = await _dbContext.KnowledgeItems.FirstOrDefaultAsync(x =>
            x.Id == id && x.TenantId == tenantId && x.BotId == botId, cancellationToken);
        if (item is null)
        {
            return false;
        }

        item.Title = title;
        item.Content = content;
        item.SourceType = string.IsNullOrWhiteSpace(sourceType) ? "faq" : sourceType;
        item.Embedding = null;
        var embedding = await _embeddingService.GenerateAsync($"{item.Title}\n{item.Content}", cancellationToken);
        item.Embedding = embedding is null ? null : new Vector(embedding);
        item.CreatedAtUtc = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> DeleteAsync(string tenantId, string botId, string id, CancellationToken cancellationToken = default)
    {
        var item = await _dbContext.KnowledgeItems.FirstOrDefaultAsync(x =>
            x.Id == id && x.TenantId == tenantId && x.BotId == botId, cancellationToken);
        if (item is null)
        {
            return false;
        }

        _dbContext.KnowledgeItems.Remove(item);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }
}
