using Atendia.Domain;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Pgvector.EntityFrameworkCore;

namespace Atendia.Infrastructure;

public sealed class AtendiaDbContext : IdentityDbContext<AppUser>
{
    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<Bot> Bots => Set<Bot>();
    public DbSet<BotConfiguration> BotConfigurations => Set<BotConfiguration>();
    public DbSet<KnowledgeItem> KnowledgeItems => Set<KnowledgeItem>();
    public DbSet<Conversation> Conversations => Set<Conversation>();
    public DbSet<Message> Messages => Set<Message>();
    public DbSet<Lead> Leads => Set<Lead>();
    public DbSet<UsageRecord> UsageRecords => Set<UsageRecord>();
    public DbSet<AuditEvent> AuditEvents => Set<AuditEvent>();

    public AtendiaDbContext(DbContextOptions<AtendiaDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasPostgresExtension("vector");

        modelBuilder.Entity<Tenant>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Name).IsRequired();
            entity.Property(x => x.Slug).IsRequired();
        });

        modelBuilder.Entity<Bot>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Name).IsRequired();
            entity.Property(x => x.TenantId).IsRequired();
            entity.Property(x => x.PublicKey).IsRequired();
            entity.HasIndex(x => x.PublicKey).IsUnique();
        });

        modelBuilder.Entity<KnowledgeItem>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.TenantId).IsRequired();
            entity.Property(x => x.BotId).IsRequired();
            entity.Property(x => x.Title).IsRequired();
            entity.Property(x => x.Content).IsRequired();
            entity.HasIndex(x => new { x.TenantId, x.BotId });

            if (Database.ProviderName?.Contains("Npgsql", StringComparison.Ordinal) == true)
            {
                entity.Property(x => x.Embedding).HasColumnType("vector(1536)");
            }
            else
            {
                entity.Ignore(x => x.Embedding);
            }
        });

        modelBuilder.Entity<Conversation>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.TenantId).IsRequired();
            entity.Property(x => x.BotId).IsRequired();
            entity.Property(x => x.Status).IsRequired();
            entity.HasIndex(x => new { x.TenantId, x.BotId, x.CreatedAtUtc });
        });

        modelBuilder.Entity<Message>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.ConversationId).IsRequired();
            entity.Property(x => x.TenantId).IsRequired();
            entity.Property(x => x.Sender).IsRequired();
            entity.Property(x => x.Content).IsRequired();
            entity.HasIndex(x => new { x.TenantId, x.ConversationId, x.CreatedAtUtc });
        });

        modelBuilder.Entity<Lead>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.TenantId).IsRequired();
            entity.Property(x => x.BotId).IsRequired();
            entity.Property(x => x.Name).IsRequired();
            entity.Property(x => x.Email).IsRequired();
            entity.HasIndex(x => new { x.TenantId, x.CreatedAtUtc });
        });

        modelBuilder.Entity<UsageRecord>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.TenantId).IsRequired();
            entity.Property(x => x.BotId).IsRequired();
            entity.Property(x => x.ConversationId).IsRequired();
            entity.Property(x => x.Model).IsRequired();
            entity.HasIndex(x => new { x.TenantId, x.TimestampUtc });
        });

        modelBuilder.Entity<AuditEvent>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Action).IsRequired();
            entity.Property(x => x.ResourceType).IsRequired();
            entity.HasIndex(x => new { x.TenantId, x.TimestampUtc });
            entity.HasIndex(x => new { x.Action, x.TimestampUtc });
        });
    }
}
