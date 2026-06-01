using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using System.Configuration;

namespace BlueMax.Infrastructure;

public static class DbContextFactory
{
    private static DbContextOptions<AppDbContext>? _options;

    public static AppDbContext CreateDbContext()
    {
        if (_options == null)
        {
            var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();

            var sqlConnectionString = SecureConnectionStringStore.LoadConnectionString()
                ?? ConfigurationManager.ConnectionStrings["MyDb"]?.ConnectionString;

            if (!string.IsNullOrWhiteSpace(sqlConnectionString) && CanUseSqlServer(sqlConnectionString))
            {
                optionsBuilder.UseSqlServer(sqlConnectionString);
            }
            else
            {
                var localDbPath = AppDbContext.GetLocalDbPath();
                Directory.CreateDirectory(Path.GetDirectoryName(localDbPath)!);
                var localCs = $"Data Source={localDbPath}";
                optionsBuilder.UseSqlite(localCs);
            }

            _options = optionsBuilder.Options;
        }
        return new AppDbContext(_options);
    }

    static bool CanUseSqlServer(string connectionString)
    {
        try
        {
            var builder = new SqlConnectionStringBuilder(connectionString);
            builder.ConnectTimeout = 3; // 3 seconds timeout for check
            using var connection = new SqlConnection(builder.ConnectionString);
            connection.Open();
            return true;
        }
        catch
        {
            return false;
        }
    }
}
