using DesfudenciFy.Application.Abstractions;
using DesfudenciFy.Domain.Entities;
using DesfudenciFy.Domain.Enums;
using DesfudenciFy.Infrastructure.Auth;
using DesfudenciFy.Infrastructure.Persistence;
using DesfudenciFy.Infrastructure.Storage;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace DesfudenciFy.Infrastructure.DependencyInjection;

public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

        services.AddScoped<IAppDbContext, AppDbContextAdapter>();
        services.AddSingleton<IPasswordHasher, BcryptPasswordHasher>();
        services.AddSingleton<IJwtTokenService, JwtTokenService>();
        services.AddSingleton<IFileStorage, LocalFileStorage>();

        var jwtKey = configuration["Jwt:Key"] ?? throw new InvalidOperationException("Jwt:Key is required");
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = configuration["Jwt:Issuer"],
                    ValidAudience = configuration["Jwt:Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
                };
            });

        services.AddAuthorization();
        return services;
    }

    public static async Task MigrateAndSeedAsync(this IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();

        await db.Database.MigrateAsync();

        if (!await db.Users.AnyAsync())
        {
            var email = configuration["Seed:AdminEmail"] ?? "admin@desfudencify.local";
            var password = configuration["Seed:AdminPassword"] ?? "Admin@12345";
            var fullName = configuration["Seed:AdminFullName"] ?? "Administrator";

            db.Users.Add(new User
            {
                Email = email.Trim().ToLowerInvariant(),
                FullName = fullName,
                PasswordHash = hasher.Hash(password),
                Role = UserRole.Admin,
                IsActive = true
            });
            await db.SaveChangesAsync();
        }

        if (!await db.InvestmentTypes.AnyAsync())
        {
            db.InvestmentTypes.AddRange(
                new InvestmentType { Name = "LCI" },
                new InvestmentType { Name = "LCA" },
                new InvestmentType { Name = "CDB" },
                new InvestmentType { Name = "Tesouro SELIC" },
                new InvestmentType { Name = "FII" });
            await db.SaveChangesAsync();
        }

        if (!await db.IncomeTypes.AnyAsync())
        {
            db.IncomeTypes.AddRange(
                new IncomeType { Name = "Salário" },
                new IncomeType { Name = "Vale Refeição" },
                new IncomeType { Name = "Vale Alimentação" },
                new IncomeType { Name = "Aluguel" },
                new IncomeType { Name = "Renda extra" });
            await db.SaveChangesAsync();
        }
        else
        {
            // Ensure default catalog names exist even if migration inserted only "Renda extra" for backfill.
            var existingNames = await db.IncomeTypes.Select(t => t.Name).ToListAsync();
            var defaults = new[] { "Salário", "Vale Refeição", "Vale Alimentação", "Aluguel", "Renda extra" };
            foreach (var name in defaults.Where(n => !existingNames.Contains(n)))
            {
                db.IncomeTypes.Add(new IncomeType { Name = name });
            }

            if (db.ChangeTracker.HasChanges())
            {
                await db.SaveChangesAsync();
            }
        }

        var expenseTypeDefaults = new[] { "Leilão", "Material", "Serviços", "Documentação" };
        if (!await db.PropertyExpenseTypes.AnyAsync())
        {
            db.PropertyExpenseTypes.AddRange(expenseTypeDefaults.Select(name => new PropertyExpenseType { Name = name }));
            await db.SaveChangesAsync();
        }
        else
        {
            var existingNames = await db.PropertyExpenseTypes.Select(t => t.Name).ToListAsync();
            foreach (var name in expenseTypeDefaults.Where(n => !existingNames.Contains(n)))
            {
                db.PropertyExpenseTypes.Add(new PropertyExpenseType { Name = name });
            }

            if (db.ChangeTracker.HasChanges())
            {
                await db.SaveChangesAsync();
            }
        }

        var vehicleExpenseTypeDefaults = new[] { "Documentação", "Impostos", "Revisão", "Reparos" };
        if (!await db.VehicleExpenseTypes.AnyAsync())
        {
            db.VehicleExpenseTypes.AddRange(vehicleExpenseTypeDefaults.Select(name => new VehicleExpenseType { Name = name }));
            await db.SaveChangesAsync();
        }
        else
        {
            var existingVehicleNames = await db.VehicleExpenseTypes.Select(t => t.Name).ToListAsync();
            foreach (var name in vehicleExpenseTypeDefaults.Where(n => !existingVehicleNames.Contains(n)))
            {
                db.VehicleExpenseTypes.Add(new VehicleExpenseType { Name = name });
            }

            if (db.ChangeTracker.HasChanges())
            {
                await db.SaveChangesAsync();
            }
        }
    }
}
