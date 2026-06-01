namespace BlueMax.Infrastructure;

public static class DatabaseSecurity
{
    public static void EnsureSqliteFileEncrypted(string localDbPath)
    {
        if (!File.Exists(localDbPath))
            return;

        try
        {
            // Check if file is already encrypted by attempting to read it
            var fileInfo = new FileInfo(localDbPath);
            if ((fileInfo.Attributes & FileAttributes.Encrypted) == 0)
            {
                File.Encrypt(localDbPath);
                LogService.LogAudit("DATABASE_ENCRYPTED", $"SQLite database encrypted: {localDbPath}");
            }
        }
        catch (Exception ex)
        {
            LogService.LogException(ex);
        }
    }

    public static string CreateEncryptedConnectionString(string dbPath, string password)
    {
        // Use SQLCipher-style encryption for SQLite
        return $"Data Source={dbPath};Password={password};";
    }

    public static string GenerateDatabasePassword()
    {
        // Generate a strong random password for database encryption
        using var rng = System.Security.Cryptography.RandomNumberGenerator.Create();
        var bytes = new byte[32];
        rng.GetBytes(bytes);
        return Convert.ToBase64String(bytes);
    }
}
