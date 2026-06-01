using System.Security.Cryptography;
using System.Text;

namespace BlueMax.Infrastructure;

public static class BackupEncryption
{
    private const int KeySize = 256; // 256-bit key
    private const int BlockSize = 128; // 128-bit block
    private const int SaltSize = 16; // 16 bytes salt
    private const int IvSize = 16; // 16 bytes IV
    private const int Iterations = 10000; // PBKDF2 iterations

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

        // Derive key and IV from password
        using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, Iterations, HashAlgorithmName.SHA256);
        var key = pbkdf2.GetBytes(KeySize / 8);
        var iv = pbkdf2.GetBytes(IvSize / 8);

        // Encrypt data
        using var aes = Aes.Create();
        aes.Key = key;
        aes.IV = iv;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;

        using var encryptor = aes.CreateEncryptor();
        var encryptedData = encryptor.TransformFinalBlock(data, 0, data.Length);

        // Combine salt + IV + encrypted data
        var result = new byte[SaltSize + IvSize + encryptedData.Length];
        Buffer.BlockCopy(salt, 0, result, 0, SaltSize);
        Buffer.BlockCopy(iv, 0, result, SaltSize, IvSize);
        Buffer.BlockCopy(encryptedData, 0, result, SaltSize + IvSize, encryptedData.Length);

        return result;
    }

    public static byte[] DecryptData(byte[] encryptedData, string password)
    {
        if (encryptedData.Length < SaltSize + IvSize)
            throw new CryptographicException("Invalid encrypted data length");

        // Extract salt and IV
        var salt = new byte[SaltSize];
        var iv = new byte[IvSize];
        var data = new byte[encryptedData.Length - SaltSize - IvSize];

        Buffer.BlockCopy(encryptedData, 0, salt, 0, SaltSize);
        Buffer.BlockCopy(encryptedData, SaltSize, iv, 0, IvSize);
        Buffer.BlockCopy(encryptedData, SaltSize + IvSize, data, 0, data.Length);

        // Derive key and IV from password
        using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, Iterations, HashAlgorithmName.SHA256);
        var key = pbkdf2.GetBytes(KeySize / 8);

        // Decrypt data
        using var aes = Aes.Create();
        aes.Key = key;
        aes.IV = iv;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;

        using var decryptor = aes.CreateDecryptor();
        return decryptor.TransformFinalBlock(data, 0, data.Length);
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
        var random = new Random();
        var password = new char[32];
        
        for (int i = 0; i < password.Length; i++)
        {
            password[i] = chars[random.Next(chars.Length)];
        }
        
        return new string(password);
    }
}
