using Microsoft.AspNetCore.Identity;

namespace Atendia.Infrastructure;

public sealed class AppUser : IdentityUser
{
    public string TenantId { get; set; } = string.Empty;
}