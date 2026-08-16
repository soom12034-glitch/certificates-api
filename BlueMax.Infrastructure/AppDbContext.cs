using Microsoft.EntityFrameworkCore;
using BlueMax.Domain;
using System.Data;
using System.Data.Common;

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

    // Brings an existing local SQLite database in line with the current model without
    // losing data. Idempotent: safe to call at startup and before every save. Does
    // nothing when a SQL Server provider is configured.
    public void EnsureSchemaCompatible()
    {
        if (Database.ProviderName?.Contains("Sqlite", StringComparison.OrdinalIgnoreCase) != true)
            return;

        try
        {
            var connection = Database.GetDbConnection();
            var wasClosed = connection.State != ConnectionState.Open;
            if (wasClosed)
                connection.Open();

            try
            {
                // WorkOrders no longer has QuotationNumber/InvoiceNumber in the model.
                // Databases created by an older schema still declare them NOT NULL, which
                // makes every insert fail with "NOT NULL constraint failed: ...".
                DropColumnsIfExist(connection, "WorkOrders", new[] { "QuotationNumber", "InvoiceNumber" });

                // The Rentals table was added to the model after existing databases were
                // created; EnsureCreated() does not add tables to an existing database.
                if (!TableExists(connection, "Rentals"))
                {
                    CreateRentalsTable(connection);
                }
                else
                {
                    AddColumnIfMissing(connection, "Rentals", "DailyPrice", "TEXT NOT NULL DEFAULT '0'");
                    AddColumnIfMissing(connection, "Rentals", "MonthlyPrice", "TEXT NOT NULL DEFAULT '0'");
                    AddColumnIfMissing(connection, "Rentals", "DeviceValue", "TEXT NOT NULL DEFAULT '0'");
                }
            }
            finally
            {
                if (wasClosed)
                    connection.Close();
            }
        }
        catch (Exception ex)
        {
            LogService.LogException(ex);
        }
    }

    static bool TableExists(DbConnection connection, string tableName)
    {
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name = '" + tableName + "'";
        return Convert.ToInt32(command.ExecuteScalar()) > 0;
    }

    static HashSet<string> GetColumnNames(DbConnection connection, string tableName)
    {
        var columns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        using var command = connection.CreateCommand();
        command.CommandText = "PRAGMA table_info(" + tableName + ")";
        using var reader = command.ExecuteReader();
        while (reader.Read())
            columns.Add(Convert.ToString(reader["name"]) ?? "");
        return columns;
    }

    static void DropColumnsIfExist(DbConnection connection, string tableName, IEnumerable<string> columnNames)
    {
        if (!TableExists(connection, tableName))
            return;
        var existing = GetColumnNames(connection, tableName);
        foreach (var column in columnNames)
        {
            if (!existing.Contains(column))
                continue;
            using var command = connection.CreateCommand();
            command.CommandText = "ALTER TABLE " + tableName + " DROP COLUMN " + column;
            command.ExecuteNonQuery();
        }
    }

    static void AddColumnIfMissing(DbConnection connection, string tableName, string columnName, string columnDefinition)
    {
        if (!TableExists(connection, tableName))
            return;
        var existing = GetColumnNames(connection, tableName);
        if (existing.Contains(columnName))
            return;
        using var command = connection.CreateCommand();
        command.CommandText = $"ALTER TABLE {tableName} ADD COLUMN {columnName} {columnDefinition}";
        command.ExecuteNonQuery();
    }

    static void CreateRentalsTable(DbConnection connection)
    {
        const string sql = @"CREATE TABLE ""Rentals"" (
    ""Id"" INTEGER NOT NULL CONSTRAINT ""PK_Rentals"" PRIMARY KEY AUTOINCREMENT,
    ""RentalNumber"" TEXT NOT NULL,
    ""CustomerName"" TEXT NOT NULL,
    ""Company"" TEXT NOT NULL,
    ""Phone"" TEXT NOT NULL,
    ""TaxNumber"" TEXT NOT NULL,
    ""IdNumber"" TEXT NOT NULL,
    ""DeviceType"" TEXT NOT NULL,
    ""Brand"" TEXT NOT NULL,
    ""Model"" TEXT NOT NULL,
    ""Serial"" TEXT NOT NULL,
    ""Serial2"" TEXT NOT NULL,
    ""StartDate"" TEXT NOT NULL,
    ""EndDate"" TEXT NOT NULL,
    ""RentalType"" TEXT NOT NULL,
    ""DailyPrice"" TEXT NOT NULL,
    ""MonthlyPrice"" TEXT NOT NULL,
    ""DeviceValue"" TEXT NOT NULL,
    ""Price"" TEXT NOT NULL,
    ""PaidAmount"" TEXT NOT NULL,
    ""RemainingAmount"" TEXT NOT NULL,
    ""Status"" TEXT NOT NULL,
    ""Notes"" TEXT NOT NULL,
    ""CreatedAt"" TEXT NOT NULL,
    ""RowVersion"" BLOB NOT NULL
);
CREATE UNIQUE INDEX ""IX_Rentals_RentalNumber"" ON ""Rentals"" (""RentalNumber"");
CREATE INDEX ""IX_Rentals_CustomerName"" ON ""Rentals"" (""CustomerName"");
CREATE INDEX ""IX_Rentals_Phone"" ON ""Rentals"" (""Phone"");
CREATE INDEX ""IX_Rentals_DeviceType"" ON ""Rentals"" (""DeviceType"");
CREATE INDEX ""IX_Rentals_Brand"" ON ""Rentals"" (""Brand"");
CREATE INDEX ""IX_Rentals_Serial"" ON ""Rentals"" (""Serial"");
CREATE INDEX ""IX_Rentals_StartDate"" ON ""Rentals"" (""StartDate"");
CREATE INDEX ""IX_Rentals_EndDate"" ON ""Rentals"" (""EndDate"");
CREATE INDEX ""IX_Rentals_Status"" ON ""Rentals"" (""Status"");";
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.ExecuteNonQuery();
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
