using System.Text.Json;
using CharityManagement.Api.Models;
using CharityManagement.Api.Models.Enums;
using CharityManagement.Api.Services.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace CharityManagement.Api.Data;

public class ApplicationDbContext : DbContext
{
    private readonly ICurrentUserService _currentUser;

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, ICurrentUserService? currentUser = null) : base(options)
    {
        _currentUser = currentUser ?? NullCurrentUserService.Instance;
    }

    public DbSet<Role> Roles => Set<Role>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<ProjectMember> ProjectMembers => Set<ProjectMember>();
    public DbSet<Donation> Donations => Set<Donation>();
    public DbSet<Report> Reports => Set<Report>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Role>(entity =>
        {
            entity.Property(x => x.Name).HasMaxLength(128);
            entity.HasIndex(x => x.Name).IsUnique();
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.Property(x => x.UserName).HasMaxLength(128);
            entity.Property(x => x.NormalizedUserName).HasMaxLength(128);
            entity.Property(x => x.Email).HasMaxLength(256);
            entity.Property(x => x.NormalizedEmail).HasMaxLength(256);
            entity.Property(x => x.PasswordHash).HasMaxLength(512);
            entity.Property(x => x.FirstName).HasMaxLength(128);
            entity.Property(x => x.LastName).HasMaxLength(128);
            entity.Property(x => x.PhoneNumber).HasMaxLength(32);
            entity.Property(x => x.TwoFactorSecret).HasMaxLength(128);

            entity.HasIndex(x => x.NormalizedUserName).IsUnique();
            entity.HasIndex(x => x.NormalizedEmail).IsUnique();

            entity.HasOne(x => x.Role)
                .WithMany(x => x.Users)
                .HasForeignKey(x => x.RoleId);
        });

        modelBuilder.Entity<Project>(entity =>
        {
            entity.HasIndex(x => x.Code).IsUnique();
            entity.Property(x => x.Name).HasMaxLength(256);
            entity.Property(x => x.Code).HasMaxLength(32);
            entity.Property(x => x.GoalAmount).HasPrecision(18, 2);
            entity.Property(x => x.CollectedAmount).HasPrecision(18, 2);
        });

        modelBuilder.Entity<ProjectMember>(entity =>
        {
            entity.HasKey(x => new { x.ProjectId, x.UserId });
            entity.Property(x => x.AssignmentRole).HasMaxLength(64);

            entity.HasOne(x => x.Project)
                .WithMany(x => x.Members)
                .HasForeignKey(x => x.ProjectId);

            entity.HasOne(x => x.User)
                .WithMany(x => x.Projects)
                .HasForeignKey(x => x.UserId);
        });

        modelBuilder.Entity<Donation>(entity =>
        {
            entity.Property(x => x.Amount).HasPrecision(18, 2);
            entity.Property(x => x.DonorName).HasMaxLength(256);
            entity.Property(x => x.DonorEmail).HasMaxLength(256);
            entity.Property(x => x.DonorPhone).HasMaxLength(32);
            entity.Property(x => x.PaymentReference).HasMaxLength(128);

            entity.HasOne(x => x.Project)
                .WithMany(x => x.Donations)
                .HasForeignKey(x => x.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(x => x.User)
                .WithMany(x => x.Donations)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(x => x.Notification)
                .WithOne(x => x.Donation)
                .HasForeignKey<Notification>(x => x.DonationId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Report>(entity =>
        {
            entity.Property(x => x.Title).HasMaxLength(256);

            entity.HasOne(x => x.Project)
                .WithMany(x => x.Reports)
                .HasForeignKey(x => x.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(x => x.CreatedBy)
                .WithMany(x => x.Reports)
                .HasForeignKey(x => x.CreatedById)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<Notification>(entity =>
        {
            entity.Property(x => x.Title).HasMaxLength(256);

            entity.HasOne(x => x.Project)
                .WithMany(x => x.Notifications)
                .HasForeignKey(x => x.ProjectId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(x => x.User)
                .WithMany(x => x.Notifications)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.SetNull);
        });
    }

    public override int SaveChanges()
    {
        PrepareEntityMetadata();
        AppendAuditEntries();
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        PrepareEntityMetadata();
        AppendAuditEntries();
        return base.SaveChangesAsync(cancellationToken);
    }

    private void PrepareEntityMetadata()
    {
        var now = DateTimeOffset.UtcNow;

        foreach (var entry in ChangeTracker.Entries()
                     .Where(e => e.State is EntityState.Added or EntityState.Modified))
        {
            if (entry.Metadata.FindProperty(nameof(Project.CreatedAt)) is not null)
            {
                if (entry.State == EntityState.Added && entry.Property(nameof(Project.CreatedAt)).CurrentValue is null)
                {
                    entry.Property(nameof(Project.CreatedAt)).CurrentValue = now;
                }
            }

            if (entry.Metadata.FindProperty(nameof(Project.UpdatedAt)) is not null)
            {
                entry.Property(nameof(Project.UpdatedAt)).CurrentValue = now;
            }
        }
    }

    private void AppendAuditEntries()
    {
        var auditableEntries = ChangeTracker.Entries()
            .Where(e => e.Entity is not AuditLog &&
                        e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
            .ToList();

        if (auditableEntries.Count == 0)
        {
            return;
        }

        var auditLogs = new List<AuditLog>();

        foreach (var entry in auditableEntries)
        {
            var changes = new Dictionary<string, object?>();

            foreach (var property in entry.Properties)
            {
                if (property.Metadata.IsPrimaryKey())
                {
                    continue;
                }

                switch (entry.State)
                {
                    case EntityState.Added:
                        changes[property.Metadata.Name] = new { New = property.CurrentValue };
                        break;
                    case EntityState.Deleted:
                        changes[property.Metadata.Name] = new { Old = property.OriginalValue };
                        break;
                    case EntityState.Modified when property.IsModified:
                        changes[property.Metadata.Name] = new
                        {
                            Old = property.OriginalValue,
                            New = property.CurrentValue
                        };
                        break;
                }
            }

            if (changes.Count == 0)
            {
                continue;
            }

            auditLogs.Add(new AuditLog
            {
                Id = Guid.NewGuid(),
                EntityName = entry.Metadata.ClrType.Name,
                EntityId = ResolvePrimaryKey(entry),
                Action = entry.State.ToString(),
                Changes = JsonSerializer.Serialize(changes),
                PerformedBy = _currentUser.UserName ?? "system",
                UserId = _currentUser.UserId,
                CreatedAt = DateTimeOffset.UtcNow
            });
        }

        if (auditLogs.Count > 0)
        {
            AuditLogs.AddRange(auditLogs);
        }
    }

    private static Guid ResolvePrimaryKey(EntityEntry entry)
    {
        var primaryKey = entry.Metadata.FindPrimaryKey();

        if (primaryKey is null || primaryKey.Properties.Count == 0)
        {
            return Guid.Empty;
        }

        if (primaryKey.Properties.Count > 1)
        {
            return Guid.Empty;
        }

        var keyProperty = primaryKey.Properties[0];
        var value = entry.Property(keyProperty.Name).CurrentValue ?? entry.Property(keyProperty.Name).OriginalValue;

        return value is Guid guid ? guid : Guid.Empty;
    }
}
