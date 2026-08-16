using System.Security.Cryptography;
using System.Text;

namespace BlueMax.Infrastructure;

public static class BackupEncryption
{
    private const int KeySize = 256; // 256-bit key
    private const int BlockSize = 128; // 128-bit block
    private const int SaltSize = 16; // 16 bytes salt
    private const int IvSize = 16; // 16 bytes IV
    private const int TagSize = 32; // HMAC-SHA256 tag
    private const int LegacyIterations = 10000; // PBKDF2 iterations used by v1 files
    private const int Iterations = 100000; // PBKDF2 iterations for new files
    private const int DerivedKeyBytes = 64; // 32 encryption + 32 MAC

    private static readonly byte[] MagicHeader = Encoding.ASCII.GetBytes("BLUEMAXENC2");
    private static readonly int V2HeaderSize = MagicHeader.Length + SaltSize + IvSize + TagSize;

    public static byte[] EncryptFile(string filePath, string password)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException("File not found", filePath);

        var fileBytes = File.ReadAllBytes(filePath);
        return EncryptData(fileBytes, password);
    }

    public static void EncryptFileToFile(string sourcePath, string destPath, string password)
    {
        var encryptedData = EncryptFile(sourcePath, password);
        File.WriteAllBytes(destPath, encryptedData);
    }

    public static byte[] DecryptFile(string filePath, string password)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException("File not found", filePath);

        var encryptedBytes = File.ReadAllBytes(filePath);
        return DecryptData(encryptedBytes, password);
    }

    public static void DecryptFileToFile(string sourcePath, string destPath, string password)
    {
        var decryptedData = DecryptFile(sourcePath, password);
        File.WriteAllBytes(destPath, decryptedData);
    }

    public static byte[] EncryptData(byte[] data, string password)
    {
        // Generate salt
        var salt = new byte[SaltSize];
        RandomNumberGenerator.Fill(salt);

        var iv = new byte[IvSize];
        RandomNumberGenerator.Fill(iv);

        // Derive encryption and MAC keys
        using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, Iterations, HashAlgorithmName.SHA256);
        var derived = pbkdf2.GetBytes(DerivedKeyBytes);
        var key = derived.AsSpan(0, KeySize / 8).ToArray();
        var macKey = derived.AsSpan(KeySize / 8).ToArray();

        // Encrypt data
        byte[] ciphertext;
        using (var aes = Aes.Create())
        {
            aes.Key = key;
            aes.IV = iv;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            using var encryptor = aes.CreateEncryptor();
            ciphertext = encryptor.TransformFinalBlock(data, 0, data.Length);
        }

        // Compute MAC over salt + iv + ciphertext (encrypt-then-MAC)
        using (var hmac = new HMACSHA256(macKey))
        {
            var macInput = new byte[SaltSize + IvSize + ciphertext.Length];
            Buffer.BlockCopy(salt, 0, macInput, 0, SaltSize);
            Buffer.BlockCopy(iv, 0, macInput, SaltSize, IvSize);
            Buffer.BlockCopy(ciphertext, 0, macInput, SaltSize + IvSize, ciphertext.Length);
            var tag = hmac.ComputeHash(macInput);

            // magic + salt + iv + tag + ciphertext
            var result = new byte[V2HeaderSize + ciphertext.Length];
            Buffer.BlockCopy(MagicHeader, 0, result, 0, MagicHeader.Length);
            Buffer.BlockCopy(salt, 0, result, MagicHeader.Length, SaltSize);
            Buffer.BlockCopy(iv, 0, result, MagicHeader.Length + SaltSize, IvSize);
            Buffer.BlockCopy(tag, 0, result, MagicHeader.Length + SaltSize + IvSize, TagSize);
            Buffer.BlockCopy(ciphertext, 0, result, V2HeaderSize, ciphertext.Length);

            CryptographicOperations.ZeroMemory(derived);
            return result;
        }
    }

    public static byte[] DecryptData(byte[] encryptedData, string password)
    {
        if (encryptedData.Length >= V2HeaderSize &&
            encryptedData.AsSpan(0, MagicHeader.Length).SequenceEqual(MagicHeader))
        {
            return DecryptDataV2(encryptedData, password);
        }

        return DecryptDataV1(encryptedData, password);
    }

    private static byte[] DecryptDataV2(byte[] encryptedData, string password)
    {
        if (encryptedData.Length <= V2HeaderSize)
            throw new CryptographicException("Invalid encrypted data length");

        var salt = new byte[SaltSize];
        var iv = new byte[IvSize];
        var tag = new byte[TagSize];
        var data = new byte[encryptedData.Length - V2HeaderSize];

        Buffer.BlockCopy(encryptedData, MagicHeader.Length, salt, 0, SaltSize);
        Buffer.BlockCopy(encryptedData, MagicHeader.Length + SaltSize, iv, 0, IvSize);
        Buffer.BlockCopy(encryptedData, MagicHeader.Length + SaltSize + IvSize, tag, 0, TagSize);
        Buffer.BlockCopy(encryptedData, V2HeaderSize, data, 0, data.Length);

        using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, Iterations, HashAlgorithmName.SHA256);
        var derived = pbkdf2.GetBytes(DerivedKeyBytes);
        var key = derived.AsSpan(0, KeySize / 8).ToArray();
        var macKey = derived.AsSpan(KeySize / 8).ToArray();

        // Verify MAC first (constant-time comparison)
        byte[] expectedTag;
        using (var hmac = new HMACSHA256(macKey))
        {
            var macInput = new byte[SaltSize + IvSize + data.Length];
            Buffer.BlockCopy(salt, 0, macInput, 0, SaltSize);
            Buffer.BlockCopy(iv, 0, macInput, SaltSize, IvSize);
            Buffer.BlockCopy(data, 0, macInput, SaltSize + IvSize, data.Length);
            expectedTag = hmac.ComputeHash(macInput);
        }

        CryptographicOperations.ZeroMemory(derived);

        if (!FixedTimeEquals(expectedTag, tag))
            throw new CryptographicException("Incorrect password or corrupted backup file.");

        // Decrypt data
        using (var aes = Aes.Create())
        {
            aes.Key = key;
            aes.IV = iv;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            using var decryptor = aes.CreateDecryptor();
            return decryptor.TransformFinalBlock(data, 0, data.Length);
        }
    }

    private static byte[] DecryptDataV1(byte[] encryptedData, string password)
    {
        if (encryptedData.Length < SaltSize + IvSize)
            throw new CryptographicException("Invalid encrypted data length");

        // Legacy v1 format: salt + IV + ciphertext (PBKDF2 with LegacyIterations, no MAC)
        var salt = new byte[SaltSize];
        var iv = new byte[IvSize];
        var data = new byte[encryptedData.Length - SaltSize - IvSize];

        Buffer.BlockCopy(encryptedData, 0, salt, 0, SaltSize);
        Buffer.BlockCopy(encryptedData, SaltSize, iv, 0, IvSize);
        Buffer.BlockCopy(encryptedData, SaltSize + IvSize, data, 0, data.Length);

        using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, LegacyIterations, HashAlgorithmName.SHA256);
        var key = pbkdf2.GetBytes(KeySize / 8);

        using var aes = Aes.Create();
        aes.Key = key;
        aes.IV = iv;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;

        using var decryptor = aes.CreateDecryptor();
        return decryptor.TransformFinalBlock(data, 0, data.Length);
    }

    private static bool FixedTimeEquals(byte[] a, byte[] b)
    {
        if (a.Length != b.Length)
            return false;
        var diff = 0;
        for (var i = 0; i < a.Length; i++)
            diff |= a[i] ^ b[i];
        return diff == 0;
    }

    public static bool IsEncryptedFile(string filePath)
    {
        if (!File.Exists(filePath))
            return false;

        var fileInfo = new FileInfo(filePath);
        // Encrypted files should be larger than the original due to salt + IV
        // Check if file extension is .enc
        return fileInfo.Extension.Equals(".enc", StringComparison.OrdinalIgnoreCase);
    }

    public static string GenerateEncryptionPassword()
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%^&*";
        var password = new char[32];
        for (int i = 0; i < password.Length; i++)
        {
            password[i] = chars[RandomNumberGenerator.GetInt32(chars.Length)];
        }
        return new string(password);
    }
}
