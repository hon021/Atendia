using Atendia.Application;
using Atendia.Domain;
using Atendia.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Atendia.Tests;

public class ChatServiceTests
{
    [Fact]
    public async Task SendAsync_ReturnsProviderResponse()
    {
        var provider = new FakeChatModel();
        var options = new DbContextOptionsBuilder<AtendiaDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        using var db = new AtendiaDbContext(options);
        var knowledge = new EfKnowledgeService(db, new ExternalEmbeddingService(new HttpClient(), new ChatProviderSettings()));
        var service = new ChatService(
            provider,
            knowledge,
            new EfBotConfigurationRepository(db),
            new EfConversationRepository(db),
            new EfMessageRepository(db),
            new EfUsageRepository(db));

        var request = new ChatRequest
        {
            TenantId = "tenant-1",
            BotId = "bot-1",
            Message = "¿Cuáles son sus horarios?"
        };

        var result = await service.SendAsync(request, CancellationToken.None);

        Assert.Equal("tenant-1", result.TenantId);
        Assert.Equal("bot-1", result.BotId);
        Assert.Contains("horarios", result.Content, StringComparison.OrdinalIgnoreCase);
        Assert.False(string.IsNullOrWhiteSpace(result.ConversationId));
        Assert.Single(db.Conversations);
        Assert.Equal(2, db.Messages.Count());
        Assert.Single(db.UsageRecords);
    }

    [Fact]
    public async Task SendAsync_DoesNotInvokeModelAfterHumanHandoff()
    {
        var provider = new CountingChatModel();
        var options = new DbContextOptionsBuilder<AtendiaDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        using var db = new AtendiaDbContext(options);
        db.Conversations.Add(new Conversation
        {
            Id = "conversation-handoff",
            TenantId = "tenant-1",
            BotId = "bot-1",
            Status = "HUMAN_HANDOFF"
        });
        db.SaveChanges();
        var service = new ChatService(
            provider,
            new EfKnowledgeService(db, new ExternalEmbeddingService(new HttpClient(), new ChatProviderSettings())),
            new EfBotConfigurationRepository(db),
            new EfConversationRepository(db),
            new EfMessageRepository(db),
            new EfUsageRepository(db));

        var result = await service.SendAsync(new ChatRequest
        {
            TenantId = "tenant-1",
            BotId = "bot-1",
            ConversationId = "conversation-handoff",
            Message = "Necesito ayuda humana"
        }, CancellationToken.None);

        Assert.Equal("HUMAN_HANDOFF", result.Status);
        Assert.Equal(0, provider.Calls);
    }

    [Fact]
    public async Task AddAndSearch_ReturnsRelevantKnowledge()
    {
        var options = new DbContextOptionsBuilder<AtendiaDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        using var db = new AtendiaDbContext(options);
        var knowledge = new EfKnowledgeService(db, new ExternalEmbeddingService(new HttpClient(), new ChatProviderSettings()));

        await knowledge.AddAsync(new KnowledgeItem
        {
            Id = "faq-10",
            TenantId = "tenant-1",
            BotId = "bot-1",
            Title = "Políticas de devolución",
            Content = "Las devoluciones se gestionan en 7 días hábiles.",
            SourceType = "faq"
        });

        var results = await knowledge.SearchAsync("tenant-1", "bot-1", "devolución");

        Assert.NotEmpty(results);
        Assert.Contains(results, item => item.Title.Contains("Políticas", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void CreateTenant_AssignsTenantIdAndName()
    {
        var options = new DbContextOptionsBuilder<AtendiaDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        using var db = new AtendiaDbContext(options);
        var service = new TenantService(new EfTenantRepository(db));

        var tenant = service.Create("Mi Negocio");

        Assert.False(string.IsNullOrWhiteSpace(tenant.Id));
        Assert.Equal("Mi Negocio", tenant.Name);
    }

    [Fact]
    public void CreateBot_AssignsUniquePublicKey()
    {
        var options = new DbContextOptionsBuilder<AtendiaDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        using var db = new AtendiaDbContext(options);
        var service = new BotService(new EfBotRepository(db));

        var bot = service.Create("tenant-1", "Bot de soporte");
        var storedBot = new EfBotRepository(db).GetByPublicKey(bot.PublicKey);

        Assert.StartsWith("atd_", bot.PublicKey);
        Assert.NotNull(storedBot);
        Assert.Equal(bot.Id, storedBot.Id);
        Assert.Equal("tenant-1", storedBot.TenantId);
    }

    [Fact]
    public void GetBotByTenant_DoesNotReturnAnotherTenantsBot()
    {
        var options = new DbContextOptionsBuilder<AtendiaDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        using var db = new AtendiaDbContext(options);
        var service = new BotService(new EfBotRepository(db));
        var bot = service.Create("tenant-1", "Bot privado");
        var repository = new EfBotRepository(db);

        Assert.NotNull(repository.GetById(bot.Id, "tenant-1"));
        Assert.Null(repository.GetById(bot.Id, "tenant-2"));
    }

    [Fact]
    public void AuditRepository_PersistsTenantScopedEvents()
    {
        var options = new DbContextOptionsBuilder<AtendiaDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        using var db = new AtendiaDbContext(options);
        var repository = new EfAuditRepository(db);

        repository.Add(new AuditEvent
        {
            TenantId = "tenant-1",
            UserId = "user-1",
            Action = "bot.create",
            ResourceType = "bot",
            ResourceId = "bot-1",
            Succeeded = true
        });
        repository.Add(new AuditEvent
        {
            TenantId = "tenant-2",
            UserId = "user-2",
            Action = "bot.create",
            ResourceType = "bot",
            ResourceId = "bot-2",
            Succeeded = true
        });

        var events = repository.GetByTenantId("tenant-1");

        var auditEvent = Assert.Single(events);
        Assert.Equal("bot-1", auditEvent.ResourceId);
        Assert.True(auditEvent.Succeeded);
    }

    [Fact]
    public async Task KnowledgeItem_CanBeUpdatedAndDeletedWithinTenant()
    {
        var options = new DbContextOptionsBuilder<AtendiaDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        using var db = new AtendiaDbContext(options);
        var knowledge = new EfKnowledgeService(db, new ExternalEmbeddingService(new HttpClient(), new ChatProviderSettings()));
        await knowledge.AddAsync(new KnowledgeItem
        {
            Id = "faq-edit-1",
            TenantId = "tenant-1",
            BotId = "bot-1",
            Title = "Horario",
            Content = "De 9 a 17.",
            SourceType = "faq"
        });

        Assert.True(await knowledge.UpdateAsync("tenant-1", "bot-1", "faq-edit-1", "Horario actualizado", "De 9 a 18.", "faq"));
        Assert.NotNull(await knowledge.GetByIdAsync("tenant-1", "bot-1", "faq-edit-1"));
        Assert.False(await knowledge.UpdateAsync("tenant-2", "bot-1", "faq-edit-1", "Intruso", "No debería actualizar.", "faq"));
        Assert.True(await knowledge.DeleteAsync("tenant-1", "bot-1", "faq-edit-1"));
        Assert.Null(await knowledge.GetByIdAsync("tenant-1", "bot-1", "faq-edit-1"));
    }

    [Fact]
    public void BotConfigurationRepository_UpsertsPerTenantAndBot()
    {
        var options = new DbContextOptionsBuilder<AtendiaDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        using var db = new AtendiaDbContext(options);
        var repository = new EfBotConfigurationRepository(db);
        repository.Upsert(new BotConfiguration
        {
            TenantId = "tenant-1",
            BotId = "bot-1",
            BusinessName = "Primera versión"
        });
        repository.Upsert(new BotConfiguration
        {
            TenantId = "tenant-1",
            BotId = "bot-1",
            BusinessName = "Segunda versión"
        });

        var configuration = repository.GetByBotId("tenant-1", "bot-1");
        Assert.NotNull(configuration);
        Assert.Equal("Segunda versión", configuration.BusinessName);
        Assert.Null(repository.GetByBotId("tenant-2", "bot-1"));
    }

    [Fact]
    public void LeadRepository_FiltersByTenantAndBot()
    {
        var options = new DbContextOptionsBuilder<AtendiaDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        using var db = new AtendiaDbContext(options);
        var repository = new EfLeadRepository(db);
        repository.Add(new Lead { Id = "lead-1", TenantId = "tenant-1", BotId = "bot-1", Name = "Ana", Email = "ana@example.com" });
        repository.Add(new Lead { Id = "lead-2", TenantId = "tenant-1", BotId = "bot-2", Name = "Luis", Email = "luis@example.com" });

        var leads = repository.GetByTenantIdAndBotId("tenant-1", "bot-1");

        var lead = Assert.Single(leads);
        Assert.Equal("lead-1", lead.Id);
    }

    private sealed class FakeChatModel : IChatModel
    {
        public Task<ChatResponse> GenerateAsync(ChatRequest request, ChatContext context, CancellationToken cancellationToken)
        {
            return Task.FromResult(new ChatResponse
            {
                TenantId = request.TenantId,
                BotId = request.BotId,
                Content = $"Respuesta: según la información disponible, los horarios son de lunes a viernes."
            });
        }
    }

    private sealed class CountingChatModel : IChatModel
    {
        public int Calls { get; private set; }

        public Task<ChatResponse> GenerateAsync(ChatRequest request, ChatContext context, CancellationToken cancellationToken)
        {
            Calls++;
            return Task.FromResult(new ChatResponse { TenantId = request.TenantId, BotId = request.BotId, Content = "unexpected" });
        }
    }
}
