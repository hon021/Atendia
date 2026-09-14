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
        var knowledge = new EfKnowledgeService(db);
        var service = new ChatService(
            provider,
            knowledge,
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
    public void AddAndSearch_ReturnsRelevantKnowledge()
    {
        var options = new DbContextOptionsBuilder<AtendiaDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        using var db = new AtendiaDbContext(options);
        var knowledge = new EfKnowledgeService(db);

        knowledge.Add(new KnowledgeItem
        {
            Id = "faq-10",
            TenantId = "tenant-1",
            BotId = "bot-1",
            Title = "Políticas de devolución",
            Content = "Las devoluciones se gestionan en 7 días hábiles.",
            SourceType = "faq"
        });

        var results = knowledge.Search("tenant-1", "bot-1", "devolución");

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

    private sealed class FakeChatModel : IChatModel
    {
        public Task<ChatResponse> GenerateAsync(ChatRequest request, CancellationToken cancellationToken)
        {
            return Task.FromResult(new ChatResponse
            {
                TenantId = request.TenantId,
                BotId = request.BotId,
                Content = $"Respuesta: según la información disponible, los horarios son de lunes a viernes."
            });
        }
    }
}
