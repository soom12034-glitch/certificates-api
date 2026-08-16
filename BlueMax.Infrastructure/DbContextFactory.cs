using Microsoft.EntityFrameworkCore;

namespace BlueMax.Infrastructure;

public static class DbContextFactory
{
    public static AppDbContext CreateDbContext()
    {
        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();

        var sqlConnectionString = SecureConnectionStringStore.LoadConnectionString()
            ?? System.Configuration.ConfigurationManager.ConnectionStrings["MyDb"]?.ConnectionString;

        AppDbContext.ConfigureProvider(optionsBuilder, sqlConnectionString);

        return new AppDbContext(optionsBuilder.Options);
    }
}
