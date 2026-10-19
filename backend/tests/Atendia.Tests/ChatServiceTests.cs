using Atendia.Application;
using Atendia.Domain;
using Atendia.Infrastructure;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Text;
using System.Text.Json;

namespace Atendia.Tests;

public class ChatServiceTests
{
    [Fact]
    public async Task OllamaEmbedding_UsesRequestedDimensionsWithoutApiKey()
    {
        var calls = 0;
        using var client = new HttpClient(new StubHttpHandler(async request =>
        {
            calls++;
            Assert.Equal("http://localhost:11434/v1/embeddings", request.RequestUri!.ToString());
            Assert.Null(request.Headers.Authorization);
            using var payload = JsonDocument.Parse(await request.Content!.ReadAsStringAsync());
            Assert.Equal(1536, payload.RootElement.GetProperty("dimensions").GetInt32());
            Assert.Equal("qwen3-embedding:4b", payload.RootElement.GetProperty("model").GetString());
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonSerializer.Serialize(new
                {
                    data = new[] { new { embedding = new float[1536] } }
                }), Encoding.UTF8, "application/json")
            };
        }));
        var service = new ExternalEmbeddingService(client, new ChatProviderSettings
        {
            Provider = "Ollama", BaseUrl = "http://localhost:11434/v1/",
            EmbeddingModel = "qwen3-embedding:4b", EmbeddingDimensions = 1536
        });

        var embedding = await service.GenerateAsync("Horario del negocio");

        Assert.Equal(1, calls);
        Assert.NotNull(embedding);
        Assert.Equal(1536, embedding.Length);
    }

    [Fact]
    public async Task OllamaChat_WorksWithoutApiKey()
    {
        var calls = 0;
        using var client = new HttpClient(new StubHttpHandler(async request =>
        {
            calls++;
            Assert.Equal("http://localhost:11434/v1/chat/completions", request.RequestUri!.ToString());
            Assert.Null(request.Headers.Authorization);
            using var payload = JsonDocument.Parse(await request.Content!.ReadAsStringAsync());
            Assert.Equal("qwen2.5:3b", payload.RootElement.GetProperty("model").GetString());
            Assert.Equal(512, payload.RootElement.GetProperty("max_tokens").GetInt32());
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"choices\":[{\"message\":{\"content\":\"Abrimos a las 9.\"}}],\"usage\":{\"prompt_tokens\":20,\"completion_tokens\":8}}", Encoding.UTF8, "application/json")
            };
        }));
        var model = new ExternalChatModel(client, new ChatProviderSettings
        {
            Provider = "Ollama", BaseUrl = "http://localhost:11434/v1/", Model = "qwen2.5:3b"
        });

        var response = await model.GenerateAsync(new ChatRequest
        {
            TenantId = "tenant-1", BotId = "bot-1", Message = "Horario?"
        }, new ChatContext(), CancellationToken.None);

        Assert.Equal(1, calls);
        Assert.Equal("Abrimos a las 9.", response.Content);
        Assert.Equal(20, response.InputTokens);
        Assert.Equal(8, response.OutputTokens);
    }

    [Fact]
    public async Task OllamaEmbedding_RejectsUnexpectedDimensions()
    {
        using var client = new HttpClient(new StubHttpHandler(_ => Task.FromResult(
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"data\":[{\"embedding\":[0.1,0.2,0.3]}]}", Encoding.UTF8, "application/json")
            })));
        var service = new ExternalEmbeddingService(client, new ChatProviderSettings
        {
            Provider = "Ollama", BaseUrl = "http://localhost:11434/v1",
            EmbeddingModel = "qwen3-embedding:4b", EmbeddingDimensions = 1536
        });

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.GenerateAsync("Horario"));
    }

    [Fact]
    public async Task ExternalProviders_WithoutApiKeyDoNotSendRequests()
    {
        using var client = new HttpClient(new StubHttpHandler(_ =>
            throw new InvalidOperationException("No request should be sent without an API key.")));
        var settings = new ChatProviderSettings();
        var embedding = await new ExternalEmbeddingService(client, settings).GenerateAsync("Horario");
        var response = await new ExternalChatModel(client, settings).GenerateAsync(
            new ChatRequest { TenantId = "tenant-1", BotId = "bot-1", Message = "Horario?" },
            new ChatContext(), CancellationToken.None);

        Assert.Null(embedding);
        Assert.Contains("API key", response.Content);
    }

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
    public async Task SendAsync_ReopensResolvedConversationWhenCustomerReplies()
    {
        var provider = new FakeChatModel();
        var options = new DbContextOptionsBuilder<AtendiaDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        using var db = new AtendiaDbContext(options);
        db.Conversations.Add(new Conversation
        {
            Id = "conversation-resolved",
            TenantId = "tenant-1",
            BotId = "bot-1",
            Status = "RESOLVED"
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
            ConversationId = "conversation-resolved",
            Message = "Tengo otra pregunta"
        }, CancellationToken.None);

        Assert.Equal("AI_HANDLING", result.Status);
        Assert.Equal("AI_HANDLING", db.Conversations.Single().Status);
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
    public async Task SearchAsync_EmptyQueryListsAllKnowledgeForTenantAndBot()
    {
        var options = new DbContextOptionsBuilder<AtendiaDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        using var db = new AtendiaDbContext(options);
        for (var index = 0; index < 7; index++)
        {
            db.KnowledgeItems.Add(new KnowledgeItem
            {
                Id = $"faq-{index}", TenantId = "tenant-1", BotId = "bot-1",
                Title = $"FAQ {index}", Content = "Contenido"
            });
        }
        db.KnowledgeItems.Add(new KnowledgeItem
        {
            Id = "other-tenant", TenantId = "tenant-2", BotId = "bot-1",
            Title = "Otro tenant", Content = "Privado"
        });
        db.KnowledgeItems.Add(new KnowledgeItem
        {
            Id = "other-bot", TenantId = "tenant-1", BotId = "bot-2",
            Title = "Otro bot", Content = "Privado"
        });
        db.SaveChanges();
        var knowledge = new EfKnowledgeService(db,
            new ExternalEmbeddingService(new HttpClient(), new ChatProviderSettings()));

        var results = await knowledge.SearchAsync("tenant-1", "bot-1", "");

        Assert.Equal(7, results.Count);
        Assert.All(results, item =>
        {
            Assert.Equal("tenant-1", item.TenantId);
            Assert.Equal("bot-1", item.BotId);
        });
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

    private sealed class StubHttpHandler(Func<HttpRequestMessage, Task<HttpResponseMessage>> handle) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return handle(request);
        }
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
