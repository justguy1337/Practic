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

        var adminRole = await context.Roles.FirstAsync(r =>
            r.NormalizedName == RoleNames.Administrator.ToUpperInvariant());

        var adminExists = await context.Users
            .AnyAsync(x => x.RoleId == adminRole.Id && x.IsActive);

        if (!adminExists)
        {
            var admin = new User
            {
                Id = Guid.NewGuid(),
                UserName = seedSettings.AdminUserName,
                NormalizedUserName = seedSettings.AdminUserName.ToUpperInvariant(),
                Email = seedSettings.AdminEmail.ToLowerInvariant(),
                NormalizedEmail = seedSettings.AdminEmail.ToUpperInvariant(),
                FirstName = "System",
                LastName = "Administrator",
                JoinedAt = DateTimeOffset.UtcNow,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow,
                RoleId = adminRole.Id,
                IsActive = true,
                TwoFactorEnabled = false
            };

            admin.PasswordHash = passwordHasher.HashPassword(admin, seedSettings.AdminPassword);
            context.Users.Add(admin);
            await context.SaveChangesAsync();
        }

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
                NormalizedName = RoleNames.Administrator.ToUpperInvariant(),
                Description = "Administrators with full access"
            },
            new Role
            {
                Id = Guid.NewGuid(),
                Name = RoleNames.Volunteer,
                NormalizedName = RoleNames.Volunteer.ToUpperInvariant(),
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
            NormalizedName = name.ToUpperInvariant(),
            Description = description
        });
    }

    private static async Task SeedSampleUsersAsync(ApplicationDbContext context, IPasswordHasher<User> passwordHasher)
    {
        var volunteerRole = await context.Roles.FirstAsync(r =>
            r.NormalizedName == RoleNames.Volunteer.ToUpperInvariant());

        var now = DateTimeOffset.UtcNow;

        var userSpecs = new[]
        {
            new
            {
                UserName = "volunteer1",
                Password = "Volunteer123!",
                FirstName = "Анна",
                LastName = "Петрова",
                Email = "volunteer1@charity.local",
                Phone = "+7 (999) 100-01-01"
            },
            new
            {
                UserName = "volunteer2",
                Password = "Volunteer123!",
                FirstName = "Иван",
                LastName = "Семенов",
                Email = "volunteer2@charity.local",
                Phone = "+7 (999) 100-02-02"
            },
            new
            {
                UserName = "coordinator",
                Password = "Volunteer123!",
                FirstName = "Мария",
                LastName = "Кузнецова",
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
                Name = "Оборудование для приюта",
                Description = "Сбор средств на медицинское оборудование и корма для приюта бездомных животных.",
                Status = ProjectStatus.Active,
                GoalAmount = 150000m,
                Start = now.AddMonths(-2),
                End = (DateTimeOffset?)now.AddMonths(2)
            },
            new
            {
                Code = "EDU-202",
                Name = "Образовательные наборы для школ",
                Description = "Обеспечение сельских школ учебными материалами и ноутбуками.",
                Status = ProjectStatus.Completed,
                GoalAmount = 200000m,
                Start = now.AddMonths(-6),
                End = (DateTimeOffset?)now.AddMonths(-1)
            }
        };

        var projectsByCode = new Dictionary<string, Project>(StringComparer.OrdinalIgnoreCase);

        foreach (var spec in projectSpecs)
        {
            var project = await context.Projects.FirstOrDefaultAsync(p => p.Code == spec.Code);
            if (project is null)
            {
                project = new Project
                {
                    Id = Guid.NewGuid(),
                    Code = spec.Code,
                    Name = spec.Name,
                    Description = spec.Description,
                    GoalAmount = spec.GoalAmount,
                    CollectedAmount = 0,
                    StartDate = spec.Start,
                    EndDate = spec.End,
                    Status = spec.Status,
                    IsArchived = false,
                    CreatedAt = now,
                    UpdatedAt = now
                };
                context.Projects.Add(project);
            }

            projectsByCode[spec.Code] = project!;
        }

        await context.SaveChangesAsync();

        var users = await context.Users.ToListAsync();
        var usersByName = users.ToDictionary(u => u.UserName, StringComparer.OrdinalIgnoreCase);

        var membershipSpecs = new[]
        {
            new { ProjectCode = "HELP-001", UserName = "admin", AssignmentRole = "Куратор" },
            new { ProjectCode = "HELP-001", UserName = "coordinator", AssignmentRole = "Координатор" },
            new { ProjectCode = "HELP-001", UserName = "volunteer1", AssignmentRole = "Волонтер" },
            new { ProjectCode = "EDU-202", UserName = "admin", AssignmentRole = "Куратор" },
            new { ProjectCode = "EDU-202", UserName = "volunteer2", AssignmentRole = "Волонтер" }
        };

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
                DonorName = "ООО \"Добрые руки\"",
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
                DonorName = "АО \"Соседи\"",
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
                DonorName = "ИП Соловьев",
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

            var existingDonation = await context.Donations
                .FirstOrDefaultAsync(d => d.PaymentReference == spec.PaymentReference);

            if (existingDonation is not null)
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
                Title = $"Новое пожертвование для {project.Name}",
                Message = $"Поступило пожертвование {donation.Amount:C} от {donation.DonorName ?? "Аноним"}.",
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
                Title = "Итоги сбора за сентябрь",
                Content = "Собрали средства на оплату корма и закупку переносок. Спасибо всем участникам!",
                CreatedByUser = "coordinator",
                IsPublic = true
            },
            new
            {
                ProjectCode = "EDU-202",
                Title = "Окончательный отчёт по образовательному проекту",
                Content = "Закуплено 20 ноутбуков, отправлено 15 комплектов учебников. Проект завершён успешно.",
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
