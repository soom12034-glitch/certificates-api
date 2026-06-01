using Microsoft.EntityFrameworkCore;

namespace BlueMax.Infrastructure;

public static class SqliteSchemaHelper
{
    public static void EnsureSchema(AppDbContext db)
    {
        if (db.Database.ProviderName == null || !db.Database.ProviderName.Contains("Sqlite", StringComparison.OrdinalIgnoreCase))
            return;

        try
        {
            var conn = db.Database.GetDbConnection();
            conn.Open();

            // Ensure Certificates table
            EnsureCertificatesSchema(conn);

            // Ensure Customers table
            EnsureCustomersSchema(conn);

            // Ensure WorkOrders table
            EnsureWorkOrdersSchema(conn);

            // Ensure LedgerEntries table
            EnsureLedgerEntriesSchema(conn);
        }
        catch (Exception ex)
        {
            LogService.LogException(ex);
        }
    }

    private static void EnsureCertificatesSchema(System.Data.Common.DbConnection conn)
    {
        try
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                SELECT name FROM sqlite_master 
                WHERE type='table' AND name='Certificates'";
            var result = cmd.ExecuteScalar();
            
            if (result == null)
            {
                cmd.CommandText = @"
                    CREATE TABLE Certificates (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        CertificateNumber TEXT NOT NULL,
                        ClientName TEXT NOT NULL,
                        Phone TEXT,
                        DeviceType TEXT NOT NULL,
                        Brand TEXT,
                        Model TEXT,
                        SerialText TEXT,
                        SerialText2 TEXT,
                        SpecValue TEXT,
                        DelegateName TEXT,
                        PayloadEnc TEXT,
                        TemplateId INTEGER,
                        IssueDate TEXT NOT NULL,
                        ExpiryDate TEXT NOT NULL,
                        CreatedAt TEXT NOT NULL,
                        RowVersion BLOB
                    )";
                cmd.ExecuteNonQuery();

                // Normalize NULL to empty string for text columns
                var textColumnsToNormalize = new[]
                {
                    "CertificateNumber",
                    "ClientName",
                    "Phone",
                    "DeviceType",
                    "Brand",
                    "Model",
                    "SerialText",
                    "SerialText2",
                    "SpecValue",
                    "DelegateName",
                    "PayloadEnc"
                };

                foreach (var col in textColumnsToNormalize)
                {
                    using var cmd2 = conn.CreateCommand();
                    // Use parameterized approach where possible
                    // Column names cannot be parameterized in SQL, but these are controlled by code, not user input
                    cmd2.CommandText = $"UPDATE Certificates SET [{col}] = '' WHERE [{col}] IS NULL;";
                    cmd2.ExecuteNonQuery();
                }
            }
        }
        catch (Exception ex)
        {
            LogService.LogException(ex);
        }
    }

    private static void EnsureCustomersSchema(System.Data.Common.DbConnection conn)
    {
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            SELECT name FROM sqlite_master 
            WHERE type='table' AND name='Customers'";
        var result = cmd.ExecuteScalar();
        
        if (result == null)
        {
            cmd.CommandText = @"
                CREATE TABLE Customers (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Name TEXT NOT NULL,
                    Phone TEXT,
                    Address TEXT,
                    Notes TEXT,
                    CreatedAt TEXT NOT NULL
                )";
            cmd.ExecuteNonQuery();
        }
    }

    private static void EnsureWorkOrdersSchema(System.Data.Common.DbConnection conn)
    {
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            SELECT name FROM sqlite_master 
            WHERE type='table' AND name='WorkOrders'";
        var result = cmd.ExecuteScalar();
        
        if (result == null)
        {
            cmd.CommandText = @"
                CREATE TABLE WorkOrders (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    CustomerId INTEGER,
                    CustomerName TEXT NOT NULL,
                    CustomerPhone TEXT,
                    DeviceType TEXT NOT NULL,
                    Model TEXT,
                    SerialNumber TEXT,
                    Accessories TEXT,
                    Complaint TEXT,
                    TechnicalReport TEXT,
                    Status TEXT NOT NULL,
                    PartsCost REAL NOT NULL DEFAULT 0,
                    LaborCost REAL NOT NULL DEFAULT 0,
                    TotalCost REAL NOT NULL DEFAULT 0,
                    ReceivedDate TEXT NOT NULL,
                    UpdatedDate TEXT NOT NULL
                )";
            cmd.ExecuteNonQuery();
        }
    }

    private static void EnsureLedgerEntriesSchema(System.Data.Common.DbConnection conn)
    {
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            SELECT name FROM sqlite_master 
            WHERE type='table' AND name='LedgerEntries'";
        var result = cmd.ExecuteScalar();
        
        if (result == null)
        {
            cmd.CommandText = @"
                CREATE TABLE LedgerEntries (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    EntryDate TEXT NOT NULL,
                    Type TEXT NOT NULL,
                    Description TEXT,
                    Amount REAL NOT NULL DEFAULT 0,
                    WorkOrderId INTEGER,
                    ReferenceNumber TEXT
                )";
            cmd.ExecuteNonQuery();
        }
    }
}
