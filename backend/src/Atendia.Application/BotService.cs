using Atendia.Domain;
using System.Security.Cryptography;

namespace Atendia.Application;

public interface IBotService
{
    Bot Create(string tenantId, string name, string? businessDescription = null);
}

public sealed class BotService : IBotService
{
    private readonly IBotRepository _botRepository;

    public BotService(IBotRepository botRepository)
    {
        _botRepository = botRepository;
    }

    public Bot Create(string tenantId, string name, string? businessDescription = null)
    {
        if (string.IsNullOrWhiteSpace(tenantId))
        {
            throw new InvalidOperationException("El tenantId es obligatorio.");
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new InvalidOperationException("El nombre del bot es obligatorio.");
        }

        var bot = new Bot
        {
            Id = Guid.NewGuid().ToString("N"),
            TenantId = tenantId,
            PublicKey = $"atd_{Convert.ToHexString(RandomNumberGenerator.GetBytes(24)).ToLowerInvariant()}",
            Name = name,
            BusinessDescription = businessDescription,
            Tone = "amigable"
        };

        _botRepository.Add(bot);
        return bot;
    }
}
