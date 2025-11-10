using System.IO.Compression;
using System.Text.Json;
using CharityManagement.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace CharityManagement.Api.Services.Export;

public class DataExportService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    public DataExportService(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<byte[]> CreateExportAsync(CancellationToken cancellationToken = default)
    {
        var snapshot = await BuildSnapshotAsync(cancellationToken);

        using var memoryStream = new MemoryStream();
        using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
        {
            var jsonEntry = archive.CreateEntry("charity-export.json", CompressionLevel.Optimal);
            await using (var jsonStream = jsonEntry.Open())
            {
                await JsonSerializer.SerializeAsync(jsonStream, snapshot, _jsonOptions, cancellationToken);
            }

            var readmeEntry = archive.CreateEntry("readme.txt", CompressionLevel.Optimal);
            await using var writer = new StreamWriter(readmeEntry.Open());
            await writer.WriteLineAsync($"Charity Management export generated at {snapshot.GeneratedAt:O}");
            await writer.WriteLineAsync("Included entities: Projects, Users, Roles, Donations, Reports, Notifications, AuditLogs.");
        }

        return memoryStream.ToArray();
    }

    private async Task<dynamic> BuildSnapshotAsync(CancellationToken cancellationToken)
    {
        var projects = await _dbContext.Projects
            .AsNoTracking()
            .Select(p => new
            {
                p.Id,
                p.Code,
                p.Name,
                p.Description,
                p.Status,
                p.GoalAmount,
                p.CollectedAmount,
                p.StartDate,
                p.EndDate,
                p.IsArchived,
                Members = p.Members.Select(m => new
                {
                    m.UserId,
                    m.AssignmentRole,
                    m.AssignedAt
                }).ToList(),
                Donations = p.Donations.Select(d => new
                {
                    d.Id,
                    d.Amount,
                    d.Method,
                    d.DonorName,
                    d.DonorEmail,
                    d.DonorPhone,
                    d.PaymentReference,
                    d.DonatedAt,
                    d.UserId
                }).ToList(),
                Reports = p.Reports.Select(r => new
                {
                    r.Id,
                    r.Title,
                    r.Content,
                    r.CreatedAt,
                    r.PublishedAt,
                    r.CreatedById,
                    r.IsPublic
                }).ToList(),
                Notifications = p.Notifications.Select(n => new
                {
                    n.Id,
                    n.Channel,
                    n.Title,
                    n.Message,
                    n.IsSent,
                    n.CreatedAt,
                    n.SentAt,
                    n.UserId,
                    n.DonationId
                }).ToList()
            })
            .ToListAsync(cancellationToken);

        var users = await _dbContext.Users
            .AsNoTracking()
            .Select(u => new
            {
                u.Id,
                u.UserName,
                u.NormalizedUserName,
                u.Email,
                u.NormalizedEmail,
                u.FirstName,
                u.LastName,
                u.PhoneNumber,
                u.TwoFactorEnabled,
                u.IsActive,
                u.JoinedAt,
                u.RoleId
            })
            .ToListAsync(cancellationToken);

        var roles = await _dbContext.Roles
            .AsNoTracking()
            .Select(r => new
            {
                r.Id,
                r.Name,
                r.Description
            })
            .ToListAsync(cancellationToken);

        var auditLogs = await _dbContext.AuditLogs
            .AsNoTracking()
            .OrderByDescending(x => x.CreatedAt)
            .Take(1000)
            .Select(x => new
            {
                x.Id,
                x.EntityName,
                x.EntityId,
                x.Action,
                x.Changes,
                x.PerformedBy,
                x.UserId,
                x.CreatedAt
            })
            .ToListAsync(cancellationToken);

        return new
        {
            GeneratedAt = DateTimeOffset.UtcNow,
            Projects = projects,
            Users = users,
            Roles = roles,
            AuditLogs = auditLogs
        };
    }
}
