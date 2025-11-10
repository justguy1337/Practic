using CharityManagement.Api.Models;
using CharityManagement.Api.Models.Enums;
using CharityManagement.Api.Options;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace CharityManagement.Api.Data.Seed;

public static class DataSeeder
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var provider = scope.ServiceProvider;

        var context = provider.GetRequiredService<ApplicationDbContext>();
        var passwordHasher = provider.GetRequiredService<IPasswordHasher<User>>();
        var seedSettings = provider.GetRequiredService<IOptions<SeedSettings>>().Value;

        await EnsureRolesAsync(context);

        var adminRole = await context.Roles.FirstAsync(r => r.Name == RoleNames.Administrator);

        var normalizedAdminEmail = seedSettings.AdminEmail.ToUpperInvariant();
        var admin = await context.Users
            .FirstOrDefaultAsync(x => x.NormalizedEmail == normalizedAdminEmail);

        if (admin is null)
        {
            admin = new User
            {
                Id = Guid.NewGuid(),
                CreatedAt = DateTimeOffset.UtcNow,
                JoinedAt = DateTimeOffset.UtcNow
            };

            context.Users.Add(admin);
        }

        admin.UserName = seedSettings.AdminUserName;
        admin.NormalizedUserName = seedSettings.AdminUserName.ToUpperInvariant();
        admin.Email = seedSettings.AdminEmail.ToLowerInvariant();
        admin.NormalizedEmail = normalizedAdminEmail;
        admin.FirstName = "System";
        admin.LastName = "Administrator";
        admin.RoleId = adminRole.Id;
        admin.IsActive = true;
        admin.TwoFactorEnabled = false;
        admin.TwoFactorSecret = null;
        admin.UpdatedAt = DateTimeOffset.UtcNow;

        admin.PasswordHash = passwordHasher.HashPassword(admin, seedSettings.AdminPassword);

        await context.SaveChangesAsync();

        await SeedSampleUsersAsync(context, passwordHasher);
        await SeedSampleProjectsAsync(context);
    }

    private static async Task EnsureRolesAsync(ApplicationDbContext context)
    {
        var existing = await context.Roles.ToListAsync();
        if (existing.Any())
        {
            EnsureRoleExists(existing, context, RoleNames.Administrator, "Administrators with full access");
            EnsureRoleExists(existing, context, RoleNames.Volunteer, "Project volunteers");
            await context.SaveChangesAsync();
            return;
        }

        var roles = new[]
        {
            new Role
            {
                Id = Guid.NewGuid(),
                Name = RoleNames.Administrator,
                Description = "Administrators with full access"
            },
            new Role
            {
                Id = Guid.NewGuid(),
                Name = RoleNames.Volunteer,
                Description = "Volunteers working on projects"
            }
        };

        context.Roles.AddRange(roles);
        await context.SaveChangesAsync();
    }

    private static void EnsureRoleExists(IEnumerable<Role> existing, ApplicationDbContext context, string name, string description)
    {
        if (existing.Any(r => string.Equals(r.Name, name, StringComparison.OrdinalIgnoreCase)))
        {
            return;
        }

        context.Roles.Add(new Role
        {
            Id = Guid.NewGuid(),
            Name = name,
            Description = description
        });
    }

    private static async Task SeedSampleUsersAsync(ApplicationDbContext context, IPasswordHasher<User> passwordHasher)
    {
        var volunteerRole = await context.Roles.FirstAsync(r => r.Name == RoleNames.Volunteer);

        var now = DateTimeOffset.UtcNow;

        var userSpecs = new[]
        {
            new
            {
                UserName = "volunteer1",
                Password = "Volunteer123!",
                FirstName = "����",
                LastName = "�������",
                Email = "volunteer1@charity.local",
                Phone = "+7 (999) 100-01-01"
            },
            new
            {
                UserName = "volunteer2",
                Password = "Volunteer123!",
                FirstName = "����",
                LastName = "�������",
                Email = "volunteer2@charity.local",
                Phone = "+7 (999) 100-02-02"
            },
            new
            {
                UserName = "coordinator",
                Password = "Volunteer123!",
                FirstName = "�����",
                LastName = "���������",
                Email = "coordinator@charity.local",
                Phone = "+7 (999) 100-03-03"
            }
        };

        foreach (var spec in userSpecs)
        {
            var normalizedUserName = spec.UserName.ToUpperInvariant();
            var existing = await context.Users
                .FirstOrDefaultAsync(u => u.NormalizedUserName == normalizedUserName);

            if (existing is not null)
            {
                continue;
            }

            var normalizedEmail = spec.Email.ToUpperInvariant();

            var user = new User
            {
                Id = Guid.NewGuid(),
                UserName = spec.UserName,
                NormalizedUserName = normalizedUserName,
                Email = spec.Email.ToLowerInvariant(),
                NormalizedEmail = normalizedEmail,
                FirstName = spec.FirstName,
                LastName = spec.LastName,
                PhoneNumber = spec.Phone,
                TwoFactorEnabled = false,
                TwoFactorSecret = null,
                JoinedAt = now.AddDays(-10),
                CreatedAt = now.AddDays(-10),
                UpdatedAt = now,
                RoleId = volunteerRole.Id,
                Role = volunteerRole,
                IsActive = true
            };

            user.PasswordHash = passwordHasher.HashPassword(user, spec.Password);
            context.Users.Add(user);
        }

        await context.SaveChangesAsync();
    }

    private static async Task SeedSampleProjectsAsync(ApplicationDbContext context)
    {
        var now = DateTimeOffset.UtcNow;

        var projectSpecs = new[]
        {
            new
            {
                Code = "HELP-001",
                Name = "����������� ��� ������",
                Description = "���� ������� �� ������������� ������������ � ������� ��� ������ ��������� ��������.",
                Status = ProjectStatus.Active,
                GoalAmount = 150000m,
                Start = now.AddMonths(-2),
                End = (DateTimeOffset?)now.AddMonths(2)
            },
            new
            {
                Code = "EDU-202",
                Name = "����������� ��������",
                Description = "�������������� ���������� ������� � ������������ �������������� �फ.",
                Status = ProjectStatus.Active,
                GoalAmount = 80000m,
                Start = now.AddMonths(-3),
                End = (DateTimeOffset?)now.AddMonths(1)
            }
        };

        var projectsByCode = (await context.Projects.ToListAsync())
            .ToDictionary(x => x.Code, x => x, StringComparer.OrdinalIgnoreCase);

        foreach (var spec in projectSpecs)
        {
            if (projectsByCode.TryGetValue(spec.Code, out var project))
            {
                project.Name = spec.Name;
                project.Description = spec.Description;
                project.Status = spec.Status;
                project.GoalAmount = spec.GoalAmount;
                project.StartDate = spec.Start;
                project.EndDate = spec.End;
                project.UpdatedAt = now;
                continue;
            }

            project = new Project
            {
                Id = Guid.NewGuid(),
                Code = spec.Code,
                Name = spec.Name,
                Description = spec.Description,
                Status = spec.Status,
                GoalAmount = spec.GoalAmount,
                CollectedAmount = 0,
                StartDate = spec.Start,
                EndDate = spec.End,
                CreatedAt = now,
                UpdatedAt = now
            };

            context.Projects.Add(project);
            projectsByCode[spec.Code] = project;
        }

        await context.SaveChangesAsync();

        var membershipSpecs = new[]
        {
            new { ProjectCode = "HELP-001", UserName = "admin", AssignmentRole = "�����" },
            new { ProjectCode = "HELP-001", UserName = "coordinator", AssignmentRole = "��������" },
            new { ProjectCode = "HELP-001", UserName = "volunteer1", AssignmentRole = "�������" },
            new { ProjectCode = "EDU-202", UserName = "admin", AssignmentRole = "�����" },
            new { ProjectCode = "EDU-202", UserName = "volunteer2", AssignmentRole = "�������" }
        };

        var users = await context.Users.ToListAsync();
        var usersByName = users.ToDictionary(u => u.UserName, StringComparer.OrdinalIgnoreCase);

        foreach (var spec in membershipSpecs)
        {
            if (!projectsByCode.TryGetValue(spec.ProjectCode, out var project) ||
                !usersByName.TryGetValue(spec.UserName, out var user))
            {
                continue;
            }

            var exists = await context.ProjectMembers
                .AnyAsync(pm => pm.ProjectId == project.Id && pm.UserId == user.Id);

            if (exists)
            {
                continue;
            }

            context.ProjectMembers.Add(new ProjectMember
            {
                ProjectId = project.Id,
                UserId = user.Id,
                AssignmentRole = spec.AssignmentRole,
                AssignedAt = now.AddDays(-7)
            });
        }

        await context.SaveChangesAsync();

        var donationSpecs = new[]
        {
            new
            {
                ProjectCode = "HELP-001",
                UserName = (string?)"volunteer1",
                Amount = 25000m,
                Method = DonationMethod.Online,
                DonorName = "��� \"����� ����\"",
                DonorEmail = (string?)"donor@kindhands.ru",
                DonorPhone = (string?)"+7 (900) 123-45-67",
                PaymentReference = "PAY-HELP-001"
            },
            new
            {
                ProjectCode = "HELP-001",
                UserName = (string?)null,
                Amount = 18000m,
                Method = DonationMethod.BankTransfer,
                DonorName = "�� \"�������\"",
                DonorEmail = (string?)null,
                DonorPhone = (string?)null,
                PaymentReference = "PAY-HELP-002"
            },
            new
            {
                ProjectCode = "EDU-202",
                UserName = (string?)"volunteer2",
                Amount = 45000m,
                Method = DonationMethod.Cash,
                DonorName = "�� ��������",
                DonorEmail = (string?)"support@solovyov.ru",
                DonorPhone = (string?)"+7 (921) 555-44-33",
                PaymentReference = "PAY-EDU-001"
            }
        };

        var createdDonations = new List<(Donation donation, Project project, User? user)>();

        foreach (var spec in donationSpecs)
        {
            if (!projectsByCode.TryGetValue(spec.ProjectCode, out var project))
            {
                continue;
            }

            usersByName.TryGetValue(spec.UserName ?? string.Empty, out var user);

            var donation = new Donation
            {
                Id = Guid.NewGuid(),
                ProjectId = project.Id,
                UserId = user?.Id,
                Amount = spec.Amount,
                Method = spec.Method,
                DonorName = spec.DonorName,
                DonorEmail = spec.DonorEmail,
                DonorPhone = spec.DonorPhone,
                PaymentReference = spec.PaymentReference,
                DonatedAt = now.AddDays(-3)
            };

            context.Donations.Add(donation);
            createdDonations.Add((donation, project, user));
        }

        await context.SaveChangesAsync();

        var totals = await context.Donations
            .GroupBy(d => d.ProjectId)
            .Select(g => new { ProjectId = g.Key, Total = g.Sum(d => d.Amount) })
            .ToListAsync();

        foreach (var total in totals)
        {
            var project = projectsByCode.Values.FirstOrDefault(p => p.Id == total.ProjectId);
            if (project is null)
            {
                continue;
            }

            project.CollectedAmount = total.Total;
            project.UpdatedAt = now;
        }

        await context.SaveChangesAsync();

        foreach (var (donation, project, user) in createdDonations)
        {
            var exists = await context.Notifications
                .AnyAsync(n => n.DonationId == donation.Id);

            if (exists)
            {
                continue;
            }

            var notification = new Notification
            {
                Id = Guid.NewGuid(),
                ProjectId = project.Id,
                UserId = user?.Id,
                DonationId = donation.Id,
                Channel = NotificationChannel.Email,
                Title = $"����� ������������� ��� {project.Name}",
                Message = $"��������� ������������� {donation.Amount:C} �� {donation.DonorName ?? "������"}.",
                CreatedAt = now,
                IsSent = false
            };

            context.Notifications.Add(notification);
        }

        await context.SaveChangesAsync();

        var reportSpecs = new[]
        {
            new
            {
                ProjectCode = "HELP-001",
                Title = "����� ����� �� ��������",
                Content = "������� �������� �� ������ ����� � ������� ���������. ������� ���� ����������!",
                CreatedByUser = "coordinator",
                IsPublic = true
            },
            new
            {
                ProjectCode = "EDU-202",
                Title = "������������� ����� �� ���������������� �������",
                Content = "��������� 20 ���������, ���������� 15 ���������� ���������. ������ �������� �������.",
                CreatedByUser = "admin",
                IsPublic = true
            }
        };

        foreach (var spec in reportSpecs)
        {
            if (!projectsByCode.TryGetValue(spec.ProjectCode, out var project) ||
                !usersByName.TryGetValue(spec.CreatedByUser, out var creator))
            {
                continue;
            }

            var exists = await context.Reports
                .AnyAsync(r => r.ProjectId == project.Id && r.Title == spec.Title);

            if (exists)
            {
                continue;
            }

            var report = new Report
            {
                Id = Guid.NewGuid(),
                ProjectId = project.Id,
                CreatedById = creator.Id,
                Title = spec.Title,
                Content = spec.Content,
                CreatedAt = now,
                PublishedAt = now,
                IsPublic = spec.IsPublic
            };

            context.Reports.Add(report);
        }

        await context.SaveChangesAsync();
    }
}
