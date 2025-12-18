using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WasteCollectionSystem.Data;

namespace WasteCollectionSystem.Models
{
    /// <summary>
    /// Static class for seeding database with initial data from SQL scripts.
    /// </summary>
    public static class SeedData
    {
        /// <summary>
        /// Seeds the database from SQL file or inline SQL commands.
        /// This method should be called once at application startup.
        /// </summary>
        /// <param name="context">The database context</param>
        /// <param name="logger">Logger for tracking seed operations</param>
        public static async Task SeedFromSqlAsync(ApplicationDbContext context, ILogger logger)
        {
            try
            {
                logger.LogInformation("Starting database seeding from SQL...");

                // Check if database is already seeded
                var hasData = await context.WasteRequests.AnyAsync();
                if (hasData)
                {
                    logger.LogInformation("Database already contains data. Skipping seed.");
                    return;
                }

                // Example: Seed sample data using raw SQL
                // You can replace this with your actual SQL seeding logic
                
                logger.LogInformation("Seeding sample waste types and initial data...");

                // Example SQL commands (customize based on your needs)
                // Uncomment and modify as needed:
                
                /*
                await context.Database.ExecuteSqlRawAsync(@"
                    -- Insert sample data here
                    -- Example:
                    -- INSERT INTO WasteTypes (Name, Description) VALUES ('Plastic', 'Recyclable plastic waste');
                    -- INSERT INTO WasteTypes (Name, Description) VALUES ('Paper', 'Recyclable paper waste');
                ");
                */

                // Alternative: Load from external SQL file
                // var sqlFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "seed.sql");
                // if (File.Exists(sqlFilePath))
                // {
                //     var sql = await File.ReadAllTextAsync(sqlFilePath);
                //     await context.Database.ExecuteSqlRawAsync(sql);
                //     logger.LogInformation("Successfully executed seed.sql");
                // }

                await context.SaveChangesAsync();
                logger.LogInformation("Database seeding completed successfully.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while seeding the database.");
                throw;
            }
        }
    }
}
