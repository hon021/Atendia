using Atendia.Application;
using Atendia.Domain;
using Microsoft.EntityFrameworkCore;

namespace Atendia.Infrastructure;

public sealed class EfTenantRepository : ITenantRepository
{
    private readonly AtendiaDbContext _dbContext;

    public EfTenantRepository(AtendiaDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public void Add(Tenant tenant)
    {
        ArgumentNullException.ThrowIfNull(tenant);
        _dbContext.Tenants.Add(tenant);
        _dbContext.SaveChanges();
    }

    public Tenant? GetById(string id)
    {
        return _dbContext.Tenants.AsNoTracking().FirstOrDefault(x => x.Id == id);
    }

    public IReadOnlyList<Tenant> GetAll()
    {
        return _dbContext.Tenants.AsNoTracking().OrderBy(x => x.Name).ToList();
    }
}

public sealed class EfBotRepository : IBotRepository
{
    private readonly AtendiaDbContext _dbContext;

    public EfBotRepository(AtendiaDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public void Add(Bot bot)
    {
        ArgumentNullException.ThrowIfNull(bot);
        _dbContext.Bots.Add(bot);
        _dbContext.SaveChanges();
    }

    public Bot? GetById(string id)
    {
        return _dbContext.Bots.AsNoTracking().FirstOrDefault(x => x.Id == id);
    }

    public Bot? GetByPublicKey(string publicKey)
    {
        return _dbContext.Bots.AsNoTracking().FirstOrDefault(x => x.PublicKey == publicKey);
    }

    public IReadOnlyList<Bot> GetByTenantId(string tenantId)
    {
        return _dbContext.Bots
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId)
            .OrderBy(x => x.Name)
            .ToList();
    }
}

public sealed class EfConversationRepository : IConversationRepository
{
    private readonly AtendiaDbContext _dbContext;

    public EfConversationRepository(AtendiaDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public void Add(Conversation conversation)
    {
        ArgumentNullException.ThrowIfNull(conversation);
        _dbContext.Conversations.Add(conversation);
        _dbContext.SaveChanges();
    }

    public Conversation? GetById(string id, string tenantId, string botId)
    {
        return _dbContext.Conversations.AsNoTracking().FirstOrDefault(x =>
            x.Id == id && x.TenantId == tenantId && x.BotId == botId);
    }

    public IReadOnlyList<Conversation> GetByTenantId(string tenantId, string botId)
    {
        return _dbContext.Conversations.AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.BotId == botId)
            .OrderByDescending(x => x.CreatedAtUtc)
            .ToList();
    }
}

public sealed class EfMessageRepository : IMessageRepository
{
    private readonly AtendiaDbContext _dbContext;

    public EfMessageRepository(AtendiaDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public void Add(Message message)
    {
        ArgumentNullException.ThrowIfNull(message);
        _dbContext.Messages.Add(message);
        _dbContext.SaveChanges();
    }

    public IReadOnlyList<Message> GetByConversationId(string conversationId, string tenantId)
    {
        return _dbContext.Messages.AsNoTracking()
            .Where(x => x.ConversationId == conversationId && x.TenantId == tenantId)
            .OrderBy(x => x.CreatedAtUtc)
            .ToList();
    }
}

public sealed class EfLeadRepository : ILeadRepository
{
    private readonly AtendiaDbContext _dbContext;

    public EfLeadRepository(AtendiaDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public void Add(Lead lead)
    {
        ArgumentNullException.ThrowIfNull(lead);
        _dbContext.Leads.Add(lead);
        _dbContext.SaveChanges();
    }

    public IReadOnlyList<Lead> GetByTenantId(string tenantId)
    {
        return _dbContext.Leads.AsNoTracking()
            .Where(x => x.TenantId == tenantId)
            .OrderByDescending(x => x.CreatedAtUtc)
            .ToList();
    }
}

public sealed class EfUsageRepository : IUsageRepository
{
    private readonly AtendiaDbContext _dbContext;

    public EfUsageRepository(AtendiaDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public void Add(UsageRecord usageRecord)
    {
        ArgumentNullException.ThrowIfNull(usageRecord);
        _dbContext.UsageRecords.Add(usageRecord);
        _dbContext.SaveChanges();
    }

    public IReadOnlyList<UsageRecord> GetByTenantId(string tenantId)
    {
        return _dbContext.UsageRecords.AsNoTracking()
            .Where(x => x.TenantId == tenantId)
            .OrderByDescending(x => x.TimestampUtc)
            .ToList();
    }
}
