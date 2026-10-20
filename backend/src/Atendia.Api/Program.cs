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
    var defaultOrigins = new[]
    {
        "http://localhost:8000",
        "http://127.0.0.1:8000",
        "http://localhost:5500",
        "http://127.0.0.1:5500",
        "http://localhost:3000",
        "http://127.0.0.1:3000"
    };

    options.AddPolicy("Widget", policy =>
    {
        var origins = builder.Configuration.GetSection("Widget:AllowedOrigins").Get<string[]>();
        var allowedOrigins = origins is { Length: > 0 } ? origins : defaultOrigins;

        policy.WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });

    options.AddPolicy("Frontend", policy =>
    {
        var origins = builder.Configuration.GetSection("Frontend:AllowedOrigins").Get<string[]>();
        var allowedOrigins = origins is { Length: > 0 } ? origins : defaultOrigins;

        policy.WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
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
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
    options.AddPolicy("KnowledgeRead", policy => policy.RequireRole("Admin", "Operator", "CustomerSupport"));
});

var connectionString = builder.Configuration.GetConnectionString("AtendiaDb");
if (!string.IsNullOrWhiteSpace(connectionString))
{
    builder.Services.AddDbContext<AtendiaDbContext>(options =>
        options.UseNpgsql(connectionString, npgsqlOptions =>
            npgsqlOptions
                .UseVector()
                .MigrationsAssembly("Atendia.Api")));
}

var providerSettings = builder.Configuration.GetSection("ChatProvider").Get<ChatProviderSettings>() ?? new ChatProviderSettings();
builder.Services.AddSingleton(providerSettings);
builder.Services.AddHttpClient<IEmbeddingService, ExternalEmbeddingService>(client =>
    client.Timeout = TimeSpan.FromMinutes(5));
builder.Services.AddScoped<IKnowledgeService, EfKnowledgeService>();
builder.Services.AddScoped<ITenantRepository, EfTenantRepository>();
builder.Services.AddScoped<IBotRepository, EfBotRepository>();
builder.Services.AddScoped<IBotConfigurationRepository, EfBotConfigurationRepository>();
builder.Services.AddScoped<IConversationRepository, EfConversationRepository>();
builder.Services.AddScoped<IMessageRepository, EfMessageRepository>();
builder.Services.AddScoped<ILeadRepository, EfLeadRepository>();
builder.Services.AddScoped<IUsageRepository, EfUsageRepository>();
builder.Services.AddScoped<IAuditRepository, EfAuditRepository>();
builder.Services.AddHttpClient<IChatModel, ExternalChatModel>(client =>
    client.Timeout = TimeSpan.FromMinutes(5));
var handoffEmailSettings = builder.Configuration.GetSection("HandoffEmail").Get<HandoffEmailSettings>() ?? new HandoffEmailSettings();
builder.Services.AddSingleton(handoffEmailSettings);
builder.Services.AddScoped<IHandoffEmailSender, SmtpHandoffEmailSender>();
builder.Services.AddScoped<ChatService>();
builder.Services.AddScoped<ITenantService, TenantService>();
builder.Services.AddScoped<IBotService, BotService>();

var app = builder.Build();

app.Use(async (context, next) =>
{
    try
    {
        await next();
    }
    catch (AiProviderUnavailableException exception) when (!context.Response.HasStarted)
    {
        app.Logger.LogWarning(exception, "AI provider unavailable while handling {Path}", context.Request.Path);
        context.Response.Clear();
        context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
        await context.Response.WriteAsJsonAsync(new
        {
            title = "Servicio de IA no disponible",
            message = "No se pudo completar la operación con el proveedor de IA. Inténtalo de nuevo más tarde."
        });
    }
});

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

if (app.Environment.IsDevelopment())
{
    var frontendPath = Path.GetFullPath(Path.Combine(app.Environment.ContentRootPath, "../../../frontend"));
    if (Directory.Exists(frontendPath))
    {
        app.UseStaticFiles(new StaticFileOptions
        {
            FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(frontendPath)
        });
    }
}

app.UseCors("Frontend");
app.UseCors("Widget");
app.UseAuthentication();
app.UseAuthorization();

app.MapPost("/api/auth/register", async (
    RegisterRequest request,
    UserManager<AppUser> userManager,
    SignInManager<AppUser> signInManager,
    ITenantService tenantService,
    IAuditRepository auditRepository) =>
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
    auditRepository.Add(new AuditEvent
    {
        TenantId = tenant.Id,
        UserId = user.Id,
        Action = "auth.register",
        ResourceType = "tenant",
        ResourceId = tenant.Id,
        Succeeded = true
    });

    return Results.Ok(new { UserId = user.Id, TenantId = tenant.Id, tenant.Name });
});

app.MapPost("/api/auth/login", async (
    LoginRequest request,
    UserManager<AppUser> userManager,
    SignInManager<AppUser> signInManager,
    IAuditRepository auditRepository) =>
{
    var user = await userManager.FindByEmailAsync(request.Email.Trim());
    if (user is null || !await userManager.CheckPasswordAsync(user, request.Password))
    {
        auditRepository.Add(new AuditEvent
        {
            UserId = user?.Id,
            TenantId = user?.TenantId,
            Action = "auth.login",
            ResourceType = "user",
            ResourceId = user?.Id,
            Succeeded = false
        });
        return Results.Unauthorized();
    }

    await signInManager.SignInAsync(user, isPersistent: false);
    auditRepository.Add(new AuditEvent
    {
        TenantId = user.TenantId,
        UserId = user.Id,
        Action = "auth.login",
        ResourceType = "user",
        ResourceId = user.Id,
        Succeeded = true
    });
    return Results.Ok(new { user.Id, user.TenantId });
});

app.MapPost("/api/auth/logout", async (SignInManager<AppUser> signInManager) =>
{
    await signInManager.SignOutAsync();
    return Results.NoContent();
});

app.MapGet("/api/auth/session", (ClaimsPrincipal user) =>
{
    var tenantId = user.FindFirstValue("tenant_id");
    if (string.IsNullOrWhiteSpace(tenantId))
    {
        return Results.Unauthorized();
    }

    return Results.Ok(new
    {
        UserId = user.FindFirstValue(ClaimTypes.NameIdentifier),
        TenantId = tenantId
    });
}).RequireAuthorization();

app.MapPost("/api/chat", async (
    ChatRequest request,
    IBotRepository botRepository,
    IConversationRepository conversationRepository,
    IBotConfigurationRepository configurationRepository,
    IAuditRepository auditRepository,
    IHandoffEmailSender handoffEmailSender,
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
    var previousStatus = string.IsNullOrWhiteSpace(request.ConversationId)
        ? null
        : conversationRepository.GetById(request.ConversationId, bot.TenantId, bot.Id)?.Status;
    var response = await service.SendAsync(request, cancellationToken);

    if (string.Equals(response.Status, "HUMAN_HANDOFF", StringComparison.Ordinal) &&
        !string.Equals(previousStatus, "HUMAN_HANDOFF", StringComparison.Ordinal))
    {
        auditRepository.Add(new AuditEvent
        {
            TenantId = bot.TenantId,
            Action = "conversation.auto_handoff",
            ResourceType = "conversation",
            ResourceId = response.ConversationId,
            Succeeded = true
        });

        var configuration = configurationRepository.GetByBotId(bot.TenantId, bot.Id);
        if (string.IsNullOrWhiteSpace(configuration?.ContactEmail))
        {
            app.Logger.LogWarning(
                "Handoff email notification skipped because no contact email is configured for bot {BotId}",
                bot.Id);
        }
        else if (!handoffEmailSender.IsConfigured)
        {
            app.Logger.LogWarning(
                "Handoff email notification skipped because SMTP is not configured for conversation {ConversationId}",
                response.ConversationId);
        }
        else
        {
            try
            {
                await handoffEmailSender.SendAsync(
                    configuration.ContactEmail,
                    configuration.BusinessName ?? bot.Name,
                    bot.Name,
                    response.ConversationId!,
                    request.Message,
                    cancellationToken);
            }
            catch (HandoffEmailDeliveryException exception)
            {
                app.Logger.LogError(
                    exception,
                    "Handoff email notification failed for tenant {TenantId}, bot {BotId}, conversation {ConversationId}",
                    bot.TenantId,
                    bot.Id,
                    response.ConversationId);
            }
        }
    }

    return Results.Ok(response);
});

app.MapGet("/api/chat/{conversationId}/messages", (
    string conversationId,
    HttpRequest httpRequest,
    IBotRepository botRepository,
    IConversationRepository conversationRepository,
    IMessageRepository messageRepository) =>
{
    var botKey = httpRequest.Headers["X-Atendia-Bot-Key"].ToString();
    var bot = string.IsNullOrWhiteSpace(botKey) ? null : botRepository.GetByPublicKey(botKey);
    if (bot is null)
    {
        return Results.Unauthorized();
    }

    var conversation = conversationRepository.GetById(conversationId, bot.TenantId, bot.Id);
    if (conversation is null)
    {
        return Results.NotFound();
    }

    var agentMessages = messageRepository.GetByConversationId(conversationId, bot.TenantId)
        .Where(message => message.Sender == "agent");
    return Results.Ok(new { conversation.Status, Messages = agentMessages });
});

app.MapPost("/api/leads", (LeadCreateRequest request, IBotRepository botRepository, ILeadRepository leadRepository, IAuditRepository auditRepository) =>
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

    if (string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.Email))
    {
        return Results.BadRequest(new { message = "Name y Email son obligatorios." });
    }

    var lead = new Lead
    {
        Id = Guid.NewGuid().ToString("N"),
        TenantId = bot.TenantId,
        BotId = bot.Id,
        Name = request.Name.Trim(),
        Email = request.Email.Trim(),
        Phone = request.Phone,
        Interest = request.Interest
    };
    leadRepository.Add(lead);
    auditRepository.Add(new AuditEvent
    {
        TenantId = bot.TenantId,
        Action = "lead.create",
        ResourceType = "lead",
        ResourceId = lead.Id,
        Succeeded = true
    });
    return Results.Ok(lead);
});

app.MapPost("/api/tenants", (TenantCreateRequest request, ITenantService tenantService) =>
{
    if (string.IsNullOrWhiteSpace(request.Name))
    {
        return Results.BadRequest(new { message = "El nombre del tenant es obligatorio." });
    }

    var tenant = tenantService.Create(request.Name);
    return Results.Ok(tenant);
}).RequireAuthorization("AdminOnly");

app.MapPost("/api/bots", (BotCreateRequest request, ClaimsPrincipal user, IBotService botService, IAuditRepository auditRepository) =>
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
    auditRepository.Add(new AuditEvent
    {
        TenantId = tenantId,
        UserId = user.FindFirstValue(ClaimTypes.NameIdentifier),
        Action = "bot.create",
        ResourceType = "bot",
        ResourceId = bot.Id,
        Succeeded = true
    });
    return Results.Ok(bot);
}).RequireAuthorization("AdminOnly");

app.MapGet("/api/bots", (ClaimsPrincipal user, IBotRepository botRepository) =>
{
    var tenantId = user.FindFirstValue("tenant_id");
    if (string.IsNullOrWhiteSpace(tenantId))
    {
        return Results.Forbid();
    }

    return Results.Ok(botRepository.GetByTenantId(tenantId));
}).RequireAuthorization("KnowledgeRead");

app.MapGet("/api/bots/{botId}/configuration", (string botId, ClaimsPrincipal user, IBotRepository botRepository, IBotConfigurationRepository configurationRepository) =>
{
    var tenantId = user.FindFirstValue("tenant_id");
    if (string.IsNullOrWhiteSpace(tenantId))
    {
        return Results.Forbid();
    }

    if (botRepository.GetById(botId, tenantId) is null)
    {
        return Results.NotFound(new { message = "El bot no existe para el tenant autenticado." });
    }

    return Results.Ok(configurationRepository.GetByBotId(tenantId, botId) ?? new BotConfiguration
    {
        Id = Guid.NewGuid().ToString("N"),
        TenantId = tenantId,
        BotId = botId
    });
}).RequireAuthorization("KnowledgeRead");

app.MapPut("/api/bots/{botId}/configuration", (string botId, BotConfigurationRequest request, ClaimsPrincipal user, IBotRepository botRepository, IBotConfigurationRepository configurationRepository, IAuditRepository auditRepository) =>
{
    var tenantId = user.FindFirstValue("tenant_id");
    if (string.IsNullOrWhiteSpace(tenantId))
    {
        return Results.Forbid();
    }

    if (botRepository.GetById(botId, tenantId) is null)
    {
        return Results.NotFound(new { message = "El bot no existe para el tenant autenticado." });
    }

    var configuration = new BotConfiguration
    {
        Id = configurationRepository.GetByBotId(tenantId, botId)?.Id ?? Guid.NewGuid().ToString("N"),
        TenantId = tenantId,
        BotId = botId,
        BusinessName = request.BusinessName,
        Sector = request.Sector,
        ContactPhone = request.ContactPhone,
        ContactEmail = request.ContactEmail,
        Website = request.Website,
        HandOffChannel = request.HandOffChannel,
        SystemPrompt = request.SystemPrompt
    };
    configurationRepository.Upsert(configuration);
    auditRepository.Add(new AuditEvent
    {
        TenantId = tenantId,
        UserId = user.FindFirstValue(ClaimTypes.NameIdentifier),
        Action = "bot.configuration.update",
        ResourceType = "bot_configuration",
        ResourceId = configuration.Id,
        Succeeded = true
    });

    return Results.Ok(configuration);
}).RequireAuthorization("AdminOnly");

app.MapPost("/api/knowledge", async (KnowledgeItem item, ClaimsPrincipal user, IBotRepository botRepository, IKnowledgeService knowledgeService, IAuditRepository auditRepository, CancellationToken cancellationToken) =>
{
    var tenantId = user.FindFirstValue("tenant_id");
    if (string.IsNullOrWhiteSpace(tenantId) || string.IsNullOrWhiteSpace(item.BotId))
    {
        return Results.BadRequest(new { message = "BotId es obligatorio." });
    }

    if (botRepository.GetById(item.BotId, tenantId) is null)
    {
        return Results.NotFound(new { message = "El bot no existe para el tenant autenticado." });
    }

    if (string.IsNullOrWhiteSpace(item.Title) || string.IsNullOrWhiteSpace(item.Content))
    {
        return Results.BadRequest(new { message = "Title y Content son obligatorios." });
    }

    item.TenantId = tenantId;
    item.Id ??= Guid.NewGuid().ToString("N");
    item.CreatedAtUtc = DateTime.UtcNow;
    await knowledgeService.AddAsync(item, cancellationToken);
    auditRepository.Add(new AuditEvent
    {
        TenantId = tenantId,
        UserId = user.FindFirstValue(ClaimTypes.NameIdentifier),
        Action = "knowledge.create",
        ResourceType = "knowledge",
        ResourceId = item.Id,
        Succeeded = true
    });

    return Results.Ok(item);
}).RequireAuthorization("AdminOnly");

app.MapGet("/api/knowledge/{tenantId}/{botId}", async (string tenantId, string botId, ClaimsPrincipal user, IBotRepository botRepository, IKnowledgeService knowledgeService, CancellationToken cancellationToken) =>
{
    var authenticatedTenantId = user.FindFirstValue("tenant_id");
    if (string.IsNullOrWhiteSpace(authenticatedTenantId))
    {
        return Results.Forbid();
    }

    if (!string.Equals(tenantId, authenticatedTenantId, StringComparison.Ordinal) ||
        botRepository.GetById(botId, authenticatedTenantId) is null)
    {
        return Results.NotFound(new { message = "El bot no existe para el tenant autenticado." });
    }

    var items = await knowledgeService.SearchAsync(authenticatedTenantId, botId, "", cancellationToken);
    return Results.Ok(items);
}).RequireAuthorization("KnowledgeRead");

app.MapPost("/api/conversations/{conversationId}/handoff", (string conversationId, HandoffRequest request, ClaimsPrincipal user, IBotRepository botRepository, IConversationRepository conversationRepository, IAuditRepository auditRepository) =>
{
    var tenantId = user.FindFirstValue("tenant_id");
    if (string.IsNullOrWhiteSpace(tenantId) || string.IsNullOrWhiteSpace(request.BotId))
    {
        return Results.BadRequest(new { message = "BotId es obligatorio." });
    }

    if (botRepository.GetById(request.BotId, tenantId) is null)
    {
        return Results.NotFound(new { message = "El bot no existe para el tenant autenticado." });
    }

    if (!conversationRepository.SetStatus(conversationId, tenantId, request.BotId, "HUMAN_HANDOFF"))
    {
        return Results.NotFound(new { message = "La conversación no existe para el tenant y bot indicados." });
    }

    auditRepository.Add(new AuditEvent
    {
        TenantId = tenantId,
        UserId = user.FindFirstValue(ClaimTypes.NameIdentifier),
        Action = "conversation.handoff",
        ResourceType = "conversation",
        ResourceId = conversationId,
        Succeeded = true
    });
    return Results.Ok(new { ConversationId = conversationId, Status = "HUMAN_HANDOFF" });
}).RequireAuthorization("KnowledgeRead");

app.MapPost("/api/conversations/{conversationId}/resolve", (string conversationId, HandoffRequest request, ClaimsPrincipal user, IBotRepository botRepository, IConversationRepository conversationRepository, IAuditRepository auditRepository) =>
{
    var tenantId = user.FindFirstValue("tenant_id");
    if (string.IsNullOrWhiteSpace(tenantId) || string.IsNullOrWhiteSpace(request.BotId))
    {
        return Results.BadRequest(new { message = "BotId es obligatorio." });
    }

    if (botRepository.GetById(request.BotId, tenantId) is null ||
        !conversationRepository.SetStatus(conversationId, tenantId, request.BotId, "RESOLVED"))
    {
        return Results.NotFound(new { message = "La conversación no existe para el tenant y bot indicados." });
    }

    auditRepository.Add(new AuditEvent
    {
        TenantId = tenantId,
        UserId = user.FindFirstValue(ClaimTypes.NameIdentifier),
        Action = "conversation.resolve",
        ResourceType = "conversation",
        ResourceId = conversationId,
        Succeeded = true
    });
    return Results.Ok(new { ConversationId = conversationId, Status = "RESOLVED" });
}).RequireAuthorization("KnowledgeRead");

app.MapGet("/api/conversations/{botId}", (string botId, ClaimsPrincipal user, IBotRepository botRepository, IConversationRepository conversationRepository) =>
{
    var tenantId = user.FindFirstValue("tenant_id");
    if (string.IsNullOrWhiteSpace(tenantId))
    {
        return Results.Forbid();
    }

    if (botRepository.GetById(botId, tenantId) is null)
    {
        return Results.NotFound(new { message = "El bot no existe para el tenant autenticado." });
    }

    return Results.Ok(conversationRepository.GetByTenantId(tenantId, botId));
}).RequireAuthorization("KnowledgeRead");

app.MapGet("/api/conversations/{botId}/{conversationId}/messages", (string botId, string conversationId, ClaimsPrincipal user, IBotRepository botRepository, IConversationRepository conversationRepository, IMessageRepository messageRepository) =>
{
    var tenantId = user.FindFirstValue("tenant_id");
    if (string.IsNullOrWhiteSpace(tenantId))
    {
        return Results.Forbid();
    }

    if (botRepository.GetById(botId, tenantId) is null ||
        conversationRepository.GetById(conversationId, tenantId, botId) is null)
    {
        return Results.NotFound(new { message = "La conversación no existe para el tenant autenticado." });
    }

    return Results.Ok(messageRepository.GetByConversationId(conversationId, tenantId));
}).RequireAuthorization("KnowledgeRead");

app.MapPost("/api/conversations/{conversationId}/messages", (
    string conversationId,
    AgentReplyRequest request,
    ClaimsPrincipal user,
    IBotRepository botRepository,
    IConversationRepository conversationRepository,
    IMessageRepository messageRepository,
    IAuditRepository auditRepository) =>
{
    var tenantId = user.FindFirstValue("tenant_id");
    if (string.IsNullOrWhiteSpace(tenantId))
    {
        return Results.Forbid();
    }

    if (string.IsNullOrWhiteSpace(request.BotId) || string.IsNullOrWhiteSpace(request.Content) || request.Content.Length > 4000)
    {
        return Results.BadRequest(new { message = "BotId y Content son obligatorios; Content admite hasta 4000 caracteres." });
    }

    if (botRepository.GetById(request.BotId, tenantId) is null)
    {
        return Results.NotFound(new { message = "El bot no existe para el tenant autenticado." });
    }

    var conversation = conversationRepository.GetById(conversationId, tenantId, request.BotId);
    if (conversation is null)
    {
        return Results.NotFound(new { message = "La conversación no existe para el tenant autenticado." });
    }

    if (!string.Equals(conversation.Status, "HUMAN_HANDOFF", StringComparison.Ordinal))
    {
        return Results.Conflict(new { message = "La conversación debe estar escalada para responder como agente." });
    }

    var message = new Message
    {
        Id = Guid.NewGuid().ToString("N"),
        ConversationId = conversationId,
        TenantId = tenantId,
        Sender = "agent",
        Content = request.Content.Trim()
    };
    messageRepository.Add(message);
    auditRepository.Add(new AuditEvent
    {
        TenantId = tenantId,
        UserId = user.FindFirstValue(ClaimTypes.NameIdentifier),
        Action = "conversation.agent_reply",
        ResourceType = "conversation",
        ResourceId = conversationId,
        Succeeded = true
    });

    return Results.Ok(message);
}).RequireAuthorization("KnowledgeRead");

app.MapGet("/api/leads/{botId}", (string botId, ClaimsPrincipal user, IBotRepository botRepository, ILeadRepository leadRepository) =>
{
    var tenantId = user.FindFirstValue("tenant_id");
    if (string.IsNullOrWhiteSpace(tenantId))
    {
        return Results.Forbid();
    }

    if (botRepository.GetById(botId, tenantId) is null)
    {
        return Results.NotFound(new { message = "El bot no existe para el tenant autenticado." });
    }

    return Results.Ok(leadRepository.GetByTenantIdAndBotId(tenantId, botId));
}).RequireAuthorization("KnowledgeRead");

app.MapPut("/api/knowledge/{botId}/{knowledgeId}", async (string botId, string knowledgeId, KnowledgeUpdateRequest request, ClaimsPrincipal user, IBotRepository botRepository, IKnowledgeService knowledgeService, IAuditRepository auditRepository, CancellationToken cancellationToken) =>
{
    var tenantId = user.FindFirstValue("tenant_id");
    if (string.IsNullOrWhiteSpace(tenantId))
    {
        return Results.Forbid();
    }

    if (botRepository.GetById(botId, tenantId) is null)
    {
        return Results.NotFound(new { message = "El bot no existe para el tenant autenticado." });
    }

    if (string.IsNullOrWhiteSpace(request.Title) || string.IsNullOrWhiteSpace(request.Content))
    {
        return Results.BadRequest(new { message = "Title y Content son obligatorios." });
    }

    var updated = await knowledgeService.UpdateAsync(tenantId, botId, knowledgeId, request.Title, request.Content, request.SourceType ?? "faq", cancellationToken);
    if (!updated)
    {
        return Results.NotFound(new { message = "El artículo no existe para el tenant y bot indicados." });
    }

    auditRepository.Add(new AuditEvent
    {
        TenantId = tenantId,
        UserId = user.FindFirstValue(ClaimTypes.NameIdentifier),
        Action = "knowledge.update",
        ResourceType = "knowledge",
        ResourceId = knowledgeId,
        Succeeded = true
    });
    return Results.NoContent();
}).RequireAuthorization("AdminOnly");

app.MapDelete("/api/knowledge/{botId}/{knowledgeId}", async (string botId, string knowledgeId, ClaimsPrincipal user, IBotRepository botRepository, IKnowledgeService knowledgeService, IAuditRepository auditRepository, CancellationToken cancellationToken) =>
{
    var tenantId = user.FindFirstValue("tenant_id");
    if (string.IsNullOrWhiteSpace(tenantId))
    {
        return Results.Forbid();
    }

    if (botRepository.GetById(botId, tenantId) is null)
    {
        return Results.NotFound(new { message = "El bot no existe para el tenant autenticado." });
    }

    var deleted = await knowledgeService.DeleteAsync(tenantId, botId, knowledgeId, cancellationToken);
    if (!deleted)
    {
        return Results.NotFound(new { message = "El artículo no existe para el tenant y bot indicados." });
    }

    auditRepository.Add(new AuditEvent
    {
        TenantId = tenantId,
        UserId = user.FindFirstValue(ClaimTypes.NameIdentifier),
        Action = "knowledge.delete",
        ResourceType = "knowledge",
        ResourceId = knowledgeId,
        Succeeded = true
    });
    return Results.NoContent();
}).RequireAuthorization("AdminOnly");

app.MapGet("/health", () => Results.Ok(new { status = "live" }));
app.MapGet("/health/ready", async (
    IServiceScopeFactory scopeFactory,
    IConfiguration configuration,
    CancellationToken cancellationToken) =>
{
    if (string.IsNullOrWhiteSpace(configuration.GetConnectionString("AtendiaDb")))
    {
        return Results.Json(new { status = "not_ready", dependency = "database" }, statusCode: StatusCodes.Status503ServiceUnavailable);
    }

    try
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AtendiaDbContext>();
        if (!await dbContext.Database.CanConnectAsync(cancellationToken))
        {
            return Results.Json(new { status = "not_ready", dependency = "database" }, statusCode: StatusCodes.Status503ServiceUnavailable);
        }
    }
    catch (System.Data.Common.DbException exception)
    {
        app.Logger.LogWarning(exception, "Database readiness check failed");
        return Results.Json(new { status = "not_ready", dependency = "database" }, statusCode: StatusCodes.Status503ServiceUnavailable);
    }

    return Results.Ok(new { status = "ready" });
});

app.Run();

public sealed record TenantCreateRequest(string Name);
public sealed record BotCreateRequest(string TenantId, string Name, string? BusinessDescription = null);
public sealed record BotConfigurationRequest(string? BusinessName, string? Sector, string? ContactPhone, string? ContactEmail, string? Website, string? HandOffChannel, string? SystemPrompt);
public sealed record KnowledgeUpdateRequest(string Title, string Content, string? SourceType = "faq");
public sealed record LeadCreateRequest(string BotKey, string Name, string Email, string? Phone = null, string? Interest = null);
public sealed record HandoffRequest(string BotId);
public sealed record AgentReplyRequest(string BotId, string Content);
public sealed record RegisterRequest(string Name, string Email, string Password);
public sealed record LoginRequest(string Email, string Password);
