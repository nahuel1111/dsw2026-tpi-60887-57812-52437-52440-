
using System;
using System.Linq;
using System.Threading.Tasks;
using Dsw2026Tpi.CrossCutting.Identity;
using Dsw2026Tpi.Data.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
namespace Dsw2026Tpi.Api.Seed
{
    public class AdminSeeder
    {
        public static async Task SeedAsync(IServiceProvider services)
        {
            using var scope = services.CreateScope();
            var provider = scope.ServiceProvider;

            var logger = provider.GetRequiredService<ILogger<AdminSeeder>>();
            var config = provider.GetRequiredService<IConfiguration>();
            var roleManager = provider.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = provider.GetRequiredService<UserManager<ApplicationUser>>();

            var adminEmail = config["Admin:Email"];
            var adminPassword = config["Admin:Password"];
            logger.LogInformation("Email admin leído: {Email}", adminEmail);

            if (!await roleManager.RoleExistsAsync(Roles.Administrator))
            {
                await roleManager.CreateAsync(new IdentityRole(Roles.Administrator));
                logger.LogInformation("Rol '{Role}' creado.", Roles.Administrator);
            }

            var existing = await userManager.FindByEmailAsync(adminEmail);

            if (existing != null)
            {
                logger.LogInformation("Admin ya existe: {Email}", adminEmail);
                return;
            }

            var adminUser = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            };

            var createResult = await userManager.CreateAsync(adminUser, adminPassword);

            if (!createResult.Succeeded)
            {
                logger.LogError(
                    "Fallo al crear admin: {Errors}",
                    string.Join(", ", createResult.Errors.Select(e => e.Description))
                );
                return;
            }

            await userManager.AddToRoleAsync(
                adminUser,
                Roles.Administrator
            );

            logger.LogInformation("Usuario admin creado: {Email}", adminEmail);
        }
    }
}
