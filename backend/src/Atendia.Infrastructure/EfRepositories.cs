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

    public Bot? GetById(string id, string tenantId)
    {
        return _dbContext.Bots
            .AsNoTracking()
            .FirstOrDefault(x => x.Id == id && x.TenantId == tenantId);
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

public sealed class EfBotConfigurationRepository : IBotConfigurationRepository
{
    private readonly AtendiaDbContext _dbContext;

    public EfBotConfigurationRepository(AtendiaDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public BotConfiguration? GetByBotId(string tenantId, string botId)
    {
        return _dbContext.BotConfigurations.AsNoTracking()
            .FirstOrDefault(x => x.TenantId == tenantId && x.BotId == botId);
    }

    public void Upsert(BotConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        var existing = _dbContext.BotConfigurations.FirstOrDefault(x =>
            x.TenantId == configuration.TenantId && x.BotId == configuration.BotId);

        if (existing is null)
        {
            if (string.IsNullOrWhiteSpace(configuration.Id))
            {
                configuration.Id = Guid.NewGuid().ToString("N");
            }

            _dbContext.BotConfigurations.Add(configuration);
        }
        else
        {
            configuration.Id = existing.Id;
            _dbContext.Entry(existing).CurrentValues.SetValues(configuration);
        }

        _dbContext.SaveChanges();
    }
}

public sealed class EfAuditRepository : IAuditRepository
{
    private readonly AtendiaDbContext _dbContext;

    public EfAuditRepository(AtendiaDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public void Add(AuditEvent auditEvent)
    {
        ArgumentNullException.ThrowIfNull(auditEvent);
        if (string.IsNullOrWhiteSpace(auditEvent.Id))
        {
            auditEvent.Id = Guid.NewGuid().ToString("N");
        }

        auditEvent.TimestampUtc = DateTime.UtcNow;
        _dbContext.AuditEvents.Add(auditEvent);
        _dbContext.SaveChanges();
    }

    public IReadOnlyList<AuditEvent> GetByTenantId(string tenantId)
    {
        return _dbContext.AuditEvents
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId)
            .OrderByDescending(x => x.TimestampUtc)
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

    public bool SetStatus(string id, string tenantId, string botId, string status)
    {
        var conversation = _dbContext.Conversations.FirstOrDefault(x =>
            x.Id == id && x.TenantId == tenantId && x.BotId == botId);
        if (conversation is null)
        {
            return false;
        }

        conversation.Status = status;
        _dbContext.SaveChanges();
        return true;
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

    public IReadOnlyList<Lead> GetByTenantIdAndBotId(string tenantId, string botId)
    {
        return _dbContext.Leads.AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.BotId == botId)
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
