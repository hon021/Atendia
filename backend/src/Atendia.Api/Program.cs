using Atendia.Application;
using Atendia.Domain;
using Atendia.Infrastructure;
using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddHttpClient();
builder.Services.AddCors(options =>
{
    options.AddPolicy("Widget", policy =>
    {
        var origins = builder.Configuration.GetSection("Widget:AllowedOrigins").Get<string[]>();
        if (origins is { Length: > 0 })
        {
            policy.WithOrigins(origins);
        }
        else
        {
            policy.AllowAnyOrigin();
        }

        policy.AllowAnyHeader().AllowAnyMethod();
    });
});
builder.Services.AddIdentityCore<AppUser>(options =>
{
    options.User.RequireUniqueEmail = true;
    options.Password.RequiredLength = 8;
    options.Password.RequireNonAlphanumeric = false;
})
    .AddSignInManager<SignInManager<AppUser>>()
    .AddEntityFrameworkStores<AtendiaDbContext>();
builder.Services.AddAuthentication(IdentityConstants.ApplicationScheme)
    .AddCookie(IdentityConstants.ApplicationScheme, options =>
    {
        options.Cookie.Name = "atendia.auth";
        options.Events.OnRedirectToLogin = context =>
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return Task.CompletedTask;
        };
        options.Events.OnRedirectToAccessDenied = context =>
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            return Task.CompletedTask;
        };
    });
builder.Services.AddAuthorization();

var connectionString = builder.Configuration.GetConnectionString("AtendiaDb");
if (!string.IsNullOrWhiteSpace(connectionString))
{
    builder.Services.AddDbContext<AtendiaDbContext>(options =>
        options.UseNpgsql(connectionString, npgsqlOptions =>
            npgsqlOptions.MigrationsAssembly("Atendia.Api")));
}

var providerSettings = builder.Configuration.GetSection("ChatProvider").Get<ChatProviderSettings>() ?? new ChatProviderSettings();
builder.Services.AddSingleton(providerSettings);
builder.Services.AddScoped<IKnowledgeService, EfKnowledgeService>();
builder.Services.AddScoped<ITenantRepository, EfTenantRepository>();
builder.Services.AddScoped<IBotRepository, EfBotRepository>();
builder.Services.AddScoped<IConversationRepository, EfConversationRepository>();
builder.Services.AddScoped<IMessageRepository, EfMessageRepository>();
builder.Services.AddScoped<ILeadRepository, EfLeadRepository>();
builder.Services.AddScoped<IUsageRepository, EfUsageRepository>();
builder.Services.AddScoped<IChatModel, ExternalChatModel>();
builder.Services.AddScoped<ChatService>();
builder.Services.AddScoped<ITenantService, TenantService>();
builder.Services.AddScoped<IBotService, BotService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseCors("Widget");
app.UseAuthentication();
app.UseAuthorization();

app.MapPost("/api/auth/register", async (
    RegisterRequest request,
    UserManager<AppUser> userManager,
    SignInManager<AppUser> signInManager,
    ITenantService tenantService) =>
{
    if (string.IsNullOrWhiteSpace(request.Name) ||
        string.IsNullOrWhiteSpace(request.Email) ||
        string.IsNullOrWhiteSpace(request.Password))
    {
        return Results.BadRequest(new { message = "Nombre, email y password son obligatorios." });
    }

    if (await userManager.FindByEmailAsync(request.Email.Trim()) is not null)
    {
        return Results.Conflict(new { message = "El email ya está registrado." });
    }

    var tenant = tenantService.Create(request.Name);
    var user = new AppUser
    {
        UserName = request.Email.Trim(),
        Email = request.Email.Trim(),
        TenantId = tenant.Id
    };

    var result = await userManager.CreateAsync(user, request.Password);
    if (!result.Succeeded)
    {
        return Results.BadRequest(new
        {
            message = "No se pudo crear el usuario.",
            errors = result.Errors.Select(error => error.Description)
        });
    }

    await userManager.AddClaimAsync(user, new Claim("tenant_id", tenant.Id));
    await userManager.AddClaimAsync(user, new Claim(ClaimTypes.Role, "Admin"));
    await signInManager.SignInAsync(user, isPersistent: false);

    return Results.Ok(new { UserId = user.Id, TenantId = tenant.Id, tenant.Name });
});

app.MapPost("/api/auth/login", async (
    LoginRequest request,
    UserManager<AppUser> userManager,
    SignInManager<AppUser> signInManager) =>
{
    var user = await userManager.FindByEmailAsync(request.Email.Trim());
    if (user is null || !await userManager.CheckPasswordAsync(user, request.Password))
    {
        return Results.Unauthorized();
    }

    await signInManager.SignInAsync(user, isPersistent: false);
    return Results.Ok(new { user.Id, user.TenantId });
});

app.MapPost("/api/auth/logout", async (SignInManager<AppUser> signInManager) =>
{
    await signInManager.SignOutAsync();
    return Results.NoContent();
});

app.MapPost("/api/chat", async (
    ChatRequest request,
    IBotRepository botRepository,
    ChatService service,
    CancellationToken cancellationToken) =>
{
    if (string.IsNullOrWhiteSpace(request.BotKey))
    {
        return Results.Unauthorized();
    }

    var bot = botRepository.GetByPublicKey(request.BotKey);
    if (bot is null)
    {
        return Results.Unauthorized();
    }

    request.TenantId = bot.TenantId;
    request.BotId = bot.Id;
    var response = await service.SendAsync(request, cancellationToken);
    return Results.Ok(response);
});

app.MapPost("/api/tenants", (TenantCreateRequest request, ITenantService tenantService) =>
{
    if (string.IsNullOrWhiteSpace(request.Name))
    {
        return Results.BadRequest(new { message = "El nombre del tenant es obligatorio." });
    }

    var tenant = tenantService.Create(request.Name);
    return Results.Ok(tenant);
}).RequireAuthorization();

app.MapPost("/api/bots", (BotCreateRequest request, ClaimsPrincipal user, IBotService botService) =>
{
    var tenantId = user.FindFirstValue("tenant_id");
    if (string.IsNullOrWhiteSpace(tenantId))
    {
        return Results.Forbid();
    }

    if (string.IsNullOrWhiteSpace(request.Name))
    {
        return Results.BadRequest(new { message = "El nombre del bot es obligatorio." });
    }

    var bot = botService.Create(tenantId, request.Name, request.BusinessDescription);
    return Results.Ok(bot);
}).RequireAuthorization();

app.MapPost("/api/knowledge", (KnowledgeItem item, ClaimsPrincipal user, IKnowledgeService knowledgeService) =>
{
    var tenantId = user.FindFirstValue("tenant_id");
    if (string.IsNullOrWhiteSpace(tenantId) || string.IsNullOrWhiteSpace(item.BotId))
    {
        return Results.BadRequest(new { message = "BotId es obligatorio." });
    }

    if (string.IsNullOrWhiteSpace(item.Title) || string.IsNullOrWhiteSpace(item.Content))
    {
        return Results.BadRequest(new { message = "Title y Content son obligatorios." });
    }

    item.TenantId = tenantId;
    item.Id ??= Guid.NewGuid().ToString("N");
    item.CreatedAtUtc = DateTime.UtcNow;
    knowledgeService.Add(item);

    return Results.Ok(item);
}).RequireAuthorization();

app.MapGet("/api/knowledge/{tenantId}/{botId}", (string tenantId, string botId, ClaimsPrincipal user, IKnowledgeService knowledgeService) =>
{
    var authenticatedTenantId = user.FindFirstValue("tenant_id");
    if (string.IsNullOrWhiteSpace(authenticatedTenantId))
    {
        return Results.Forbid();
    }

    var items = knowledgeService.Search(authenticatedTenantId, botId, "");
    return Results.Ok(items);
}).RequireAuthorization();

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

app.Run();

public sealed record TenantCreateRequest(string Name);
public sealed record BotCreateRequest(string TenantId, string Name, string? BusinessDescription = null);
public sealed record RegisterRequest(string Name, string Email, string Password);
public sealed record LoginRequest(string Email, string Password);
