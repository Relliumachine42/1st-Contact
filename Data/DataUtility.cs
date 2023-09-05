using ContactPro.Models;
using ContactPro.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace ContactPro.Data
{
    public static class DataUtility
    {
        public static string GetConnectionString(IConfiguration configuration)
        {
            string? connectionString = configuration.GetConnectionString("DefaultConnection");
            string? databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL");

            return string.IsNullOrEmpty(databaseUrl) ? connectionString! : BuildConnectionString(databaseUrl);
        }
        private static string BuildConnectionString(string databaseUrl)

        {
            var databaseUri = new Uri(databaseUrl);
            var userInfo = databaseUri.UserInfo.Split(':');
            var builder = new NpgsqlConnectionStringBuilder()
            {
                Host = databaseUri.Host,
                Port = databaseUri.Port,
                Username = userInfo[0],
                Password = userInfo[1],
                Database = databaseUri.LocalPath.TrimStart('/'),
                SslMode = SslMode.Require,
                TrustServerCertificate = true
            };
            return builder.ToString();
        }
        public static async Task ManageDataAsync(IServiceProvider svcProvider)
        {
            //obtain necessary services based on the IServiceProvider parameter
            var dbContextSvc = svcProvider.GetRequiredService<ApplicationDbContext>();
            var userManagerSvc = svcProvider.GetRequiredService<UserManager<AppUser>>();
            var configurationSvc = svcProvider.GetRequiredService<IConfiguration>();
            //align database by checking migrations
            await dbContextSvc.Database.MigrateAsync();
            //seed demo User(s)
            await SeedDemoUsersAsync(userManagerSvc, configurationSvc);

        }
        private static async Task SeedDemoUsersAsync(UserManager<AppUser> userManager, IConfiguration configuration)
        {
            string? demoLoginEmail = configuration["DemoLoginEmail"] ?? Environment.GetEnvironmentVariable("DemoLoginEmail");
            string? demoLoginPassword = configuration["DemoLoginPassword"] ?? Environment.GetEnvironmentVariable("DemoLoginPassword");

            AppUser demoUser = new AppUser()
            {
                UserName = demoLoginEmail,
                Email = demoLoginEmail,
                FirstName = "Demo",
                LastName = "User",
                EmailConfirmed = true
            };

            try
            {
                AppUser? appUser = await userManager.FindByEmailAsync(demoLoginEmail!);
                if (appUser == null)
                {
                    await userManager.CreateAsync(demoUser, demoLoginPassword!);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("******** ERROR ********");
                Console.WriteLine("Error Seeding Demo Login User.");
                Console.WriteLine(ex.Message);
                Console.WriteLine("*****************");

                throw;
            }
        }
    }
}
