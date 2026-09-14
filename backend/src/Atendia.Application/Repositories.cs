using Atendia.Domain;

namespace Atendia.Application;

public interface ITenantRepository
{
    void Add(Tenant tenant);
    Tenant? GetById(string id);
    IReadOnlyList<Tenant> GetAll();
}

public interface IBotRepository
{
    void Add(Bot bot);
    Bot? GetById(string id);
    Bot? GetByPublicKey(string publicKey);
    IReadOnlyList<Bot> GetByTenantId(string tenantId);
}

public interface IConversationRepository
{
    void Add(Conversation conversation);
    Conversation? GetById(string id, string tenantId, string botId);
    IReadOnlyList<Conversation> GetByTenantId(string tenantId, string botId);
}

public interface IMessageRepository
{
    void Add(Message message);
    IReadOnlyList<Message> GetByConversationId(string conversationId, string tenantId);
}

public interface ILeadRepository
{
    void Add(Lead lead);
    IReadOnlyList<Lead> GetByTenantId(string tenantId);
}

public interface IUsageRepository
{
    void Add(UsageRecord usageRecord);
    IReadOnlyList<UsageRecord> GetByTenantId(string tenantId);
}
