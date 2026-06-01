using System.Security.Cryptography;

namespace BlueMax.Infrastructure;

public static class AesKeyStore
{
    private const string AesKeyPath = "AesKey";

    public static (byte[] Key, byte[] IV) GetOrCreateAesKey()
    {
        var settingsDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "BlueMax",
            "CertSystem",
            "Settings");

        Directory.CreateDirectory(settingsDir);

        var keyPath = Path.Combine(settingsDir, AesKeyPath);

        if (File.Exists(keyPath))
        {
            var encryptedBytes = File.ReadAllBytes(keyPath);
            var decryptedBytes = ProtectedData.Unprotect(encryptedBytes, null, DataProtectionScope.CurrentUser);
            return ParseKeyAndIv(decryptedBytes);
        }

        // Generate new key
        using var aes = Aes.Create();
        aes.GenerateKey();
        aes.GenerateIV();

        var combined = CombineKeyAndIv(aes.Key, aes.IV);
        var encrypted = ProtectedData.Protect(combined, null, DataProtectionScope.CurrentUser);
        File.WriteAllBytes(keyPath, encrypted);

        return (aes.Key, aes.IV);
    }

    private static byte[] CombineKeyAndIv(byte[] key, byte[] iv)
    {
        var combined = new byte[key.Length + iv.Length];
        Buffer.BlockCopy(key, 0, combined, 0, key.Length);
        Buffer.BlockCopy(iv, 0, combined, key.Length, iv.Length);
        return combined;
    }

    private static (byte[] Key, byte[] IV) ParseKeyAndIv(byte[] data)
    {
        var key = new byte[32];
        var iv = new byte[16];
        Buffer.BlockCopy(data, 0, key, 0, 32);
        Buffer.BlockCopy(data, 32, iv, 0, 16);
        return (key, iv);
    }
}
