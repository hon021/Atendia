using Atendia.Domain;

namespace Atendia.Application;

public interface ITenantService
{
    Tenant Create(string name);
}

public sealed class TenantService : ITenantService
{
    private readonly ITenantRepository _tenantRepository;

    public TenantService(ITenantRepository tenantRepository)
    {
        _tenantRepository = tenantRepository;
    }

    public Tenant Create(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new InvalidOperationException("El nombre del tenant es obligatorio.");
        }

        var tenant = new Tenant
        {
            Id = Guid.NewGuid().ToString("N"),
            Name = name,
            Slug = name.Trim().ToLowerInvariant().Replace(" ", "-")
        };

        _tenantRepository.Add(tenant);
        return tenant;
    }
}
