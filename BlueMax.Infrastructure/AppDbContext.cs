using Microsoft.EntityFrameworkCore;
using BlueMax.Domain;

namespace BlueMax.Infrastructure;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public static string GetLocalDbPath()
    {
        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "BlueMax",
            "CertSystem",
            "Data",
            "local.db");
    }

    public DbSet<Certificate> Certificates => Set<Certificate>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<WorkOrder> WorkOrders => Set<WorkOrder>();
    public DbSet<SparePart> SpareParts => Set<SparePart>();
    public DbSet<Rental> Rentals => Set<Rental>();

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            var sqlConnectionString = SecureConnectionStringStore.LoadConnectionString()
                ?? System.Configuration.ConfigurationManager.ConnectionStrings["MyDb"]?.ConnectionString;
            ConfigureProvider(optionsBuilder, sqlConnectionString);
        }
    }

    internal static void ConfigureProvider(DbContextOptionsBuilder optionsBuilder, string? sqlConnectionString)
    {
        if (!string.IsNullOrWhiteSpace(sqlConnectionString))
        {
            if (!CanUseSqlServer(sqlConnectionString))
                throw new InvalidOperationException(
                    "تم تكوين SQL Server لكن لا يمكن الوصول إلى الخادم. تحقق من الاتصال، أو أزل إعدادات SQL Server لاستخدام قاعدة البيانات المحلية.");
            optionsBuilder.UseSqlServer(sqlConnectionString);
            return;
        }

        var localDbPath = GetLocalDbPath();

        Directory.CreateDirectory(Path.GetDirectoryName(localDbPath)!);
        var localCs = $"Data Source={localDbPath}";
        optionsBuilder.UseSqlite(localCs);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var isSqlite = Database.ProviderName != null &&
                       Database.ProviderName.Contains("Sqlite", StringComparison.OrdinalIgnoreCase);
        if (isSqlite)
        {
            modelBuilder.Entity<Certificate>()
                        .Property(c => c.RowVersion)
                        .IsConcurrencyToken()
                        .ValueGeneratedNever();
        }
        else
        {
            modelBuilder.Entity<Certificate>()
                        .Property(c => c.RowVersion)
                        .IsRowVersion();
        }
        modelBuilder.Entity<Certificate>().HasIndex(c => c.ClientName);
        modelBuilder.Entity<Certificate>().HasIndex(c => c.Phone);
        modelBuilder.Entity<Certificate>().HasIndex(c => c.DeviceType);
        modelBuilder.Entity<Certificate>().HasIndex(c => c.Brand);
        modelBuilder.Entity<Certificate>().HasIndex(c => c.SerialText);
        modelBuilder.Entity<Certificate>().HasIndex(c => c.IssueDate);

        modelBuilder.Entity<Customer>().HasIndex(c => c.Name);
        modelBuilder.Entity<Customer>().HasIndex(c => c.Phone);
        modelBuilder.Entity<Customer>().HasIndex(c => c.CreatedAt);

        modelBuilder.Entity<WorkOrder>().HasIndex(w => w.Status);
        modelBuilder.Entity<WorkOrder>().HasIndex(w => w.ReceivedDate);
        modelBuilder.Entity<WorkOrder>().HasIndex(w => w.CustomerName);
        modelBuilder.Entity<WorkOrder>().HasIndex(w => w.SerialNumber);
        // Add foreign key constraint for WorkOrder -> Customer
        modelBuilder.Entity<WorkOrder>()
            .HasOne<Customer>()
            .WithMany()
            .HasForeignKey(w => w.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<SparePart>().HasIndex(s => s.Code).IsUnique();
        modelBuilder.Entity<SparePart>().HasIndex(s => s.Name);
        modelBuilder.Entity<SparePart>().HasIndex(s => s.Brand);

        modelBuilder.Entity<Rental>().HasIndex(r => r.RentalNumber).IsUnique();
        modelBuilder.Entity<Rental>().HasIndex(r => r.CustomerName);
        modelBuilder.Entity<Rental>().HasIndex(r => r.Phone);
        modelBuilder.Entity<Rental>().HasIndex(r => r.DeviceType);
        modelBuilder.Entity<Rental>().HasIndex(r => r.Brand);
        modelBuilder.Entity<Rental>().HasIndex(r => r.Serial);
        modelBuilder.Entity<Rental>().HasIndex(r => r.StartDate);
        modelBuilder.Entity<Rental>().HasIndex(r => r.EndDate);
        modelBuilder.Entity<Rental>().HasIndex(r => r.Status);
    }

    static bool CanUseSqlServer(string connectionString)
    {
        try
        {
            var builder = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(connectionString)
            {
                ConnectTimeout = 3
            };
            using var connection = new Microsoft.Data.SqlClient.SqlConnection(builder.ConnectionString);
            connection.Open();
            return true;
        }
        catch
        {
            return false;
        }
    }
}
