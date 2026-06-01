using System.Management;
using Microsoft.Win32;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;

namespace BlueMax.Infrastructure;

public class LicenseService
{
    readonly LicenseStore _store;
    
    // Secret key for XTEA (128 bits = 16 bytes)
    // Using a static hardcoded key for simplicity, derived from a phrase
    private static readonly uint[] XteaKey = new uint[] 
    { 
        0x12345678, 0x9ABCDEF0, 0xDEADBEEF, 0xCAFEBABE 
    };

    // Base date for license expiration calculation (to fit in 16 bits)
    private static readonly DateTime BaseDate = new DateTime(2025, 1, 1);

    public LicenseService(LicenseStore store)
    {
        _store = store;
    }

    public string GetHardwareId()
    {
        var cpu = ReadWmiValue("SELECT ProcessorId FROM Win32_Processor", "ProcessorId");
        var baseboard = ReadWmiValue("SELECT SerialNumber FROM Win32_BaseBoard", "SerialNumber");

        var hasCpu = !string.IsNullOrWhiteSpace(cpu);
        var hasBaseboard = !string.IsNullOrWhiteSpace(baseboard);

        string source;
        if (hasCpu || hasBaseboard)
        {
            if (!hasCpu) cpu = "CPU-UNKNOWN";
            if (!hasBaseboard) baseboard = "BOARD-UNKNOWN";
            source = $"{cpu}|{baseboard}";
        }
        else
        {
            var machineGuid = ReadMachineGuid();
            source = string.IsNullOrWhiteSpace(machineGuid)
                ? "CPU-UNKNOWN|BOARD-UNKNOWN"
                : $"MGUID:{machineGuid}";
        }

        using var sha = SHA256.Create();
        var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(source));
        
        // Use first 16 chars (8 bytes) of the hash to create a short, user-friendly ID
        // This gives 64 bits of entropy which is sufficient for this purpose
        string hex = Convert.ToHexString(bytes).Substring(0, 16);
        return FormatLicenseKey(hex);
    }

    // Generates a 16-character license key (XXXX-XXXX-XXXX-XXXX)
    public string GenerateActivationCode(string hardwareId, DateTime expirationDate)
    {
        // 1. Calculate Hardware ID Hash (32 bits)
        // Ensure hardwareId is cleaned before hashing
        uint hwHash = ComputeHardwareHash(hardwareId);

        // 2. Calculate Date Offset (16 bits)
        int days = (expirationDate - BaseDate).Days;
        if (days < 0) days = 0;
        if (days > 65535) days = 65535; // Max ~179 years from 2025
        ushort dateVal = (ushort)days;

        // 3. Calculate Checksum (16 bits)
        // Checksum of (hwHash + dateVal) to verify integrity
        ushort checksum = ComputeChecksum(hwHash, dateVal);

        // 4. Pack into 64 bits (2 uints)
        // v0 = hwHash
        // v1 = [dateVal (16 bits)] [checksum (16 bits)]
        uint v0 = hwHash;
        uint v1 = (uint)((dateVal << 16) | checksum);

        // 5. Encrypt using XTEA
        EncryptXtea(ref v0, ref v1);

        // 6. Convert to Hex string (16 chars)
        // v0 (8 chars) + v1 (8 chars)
        string hex = $"{v0:X8}{v1:X8}";

        // 7. Format as XXXX-XXXX-XXXX-XXXX
        return FormatLicenseKey(hex);
    }

    public string GenerateActivationCode(string hardwareId)
    {
        return GenerateActivationCode(hardwareId, DateTime.Today.AddYears(1));
    }

    public bool IsActivated()
    {
        var code = _store.LoadActivationCode();
        if (string.IsNullOrWhiteSpace(code))
            return false;
        return ValidateActivationCode(code);
    }

    public bool TryActivate(string activationCode)
    {
        if (!ValidateActivationCode(activationCode))
            return false;
        
        _store.SaveActivationCode(activationCode);
        return true;
    }

    public (bool IsValid, DateTime ExpirationDate) GetLicenseDetails()
    {
        try
        {
            var code = _store.LoadActivationCode();
            if (string.IsNullOrWhiteSpace(code)) return (false, DateTime.MinValue);

            var (isValid, expDate, _) = ParseAndValidate(code);
            return (isValid, expDate);
        }
        catch { }
        return (false, DateTime.MinValue);
    }

    public bool ValidateActivationCode(string activationCode)
    {
        var (isValid, _, _) = ParseAndValidate(activationCode);
        return isValid;
    }

    private (bool IsValid, DateTime ExpirationDate, string HwIdHash) ParseAndValidate(string code)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(code)) return (false, DateTime.MinValue, "");

            // Remove separators
            string cleanCode = code.Replace("-", "").Replace(" ", "").Trim();
            if (cleanCode.Length != 16) return (false, DateTime.MinValue, "");

            // Parse Hex
            if (!uint.TryParse(cleanCode.Substring(0, 8), System.Globalization.NumberStyles.HexNumber, null, out uint v0))
                return (false, DateTime.MinValue, "");
            if (!uint.TryParse(cleanCode.Substring(8, 8), System.Globalization.NumberStyles.HexNumber, null, out uint v1))
                return (false, DateTime.MinValue, "");

            // Decrypt
            DecryptXtea(ref v0, ref v1);

            // Unpack
            uint hwHash = v0;
            ushort dateVal = (ushort)(v1 >> 16);
            ushort checksum = (ushort)(v1 & 0xFFFF);

            // Verify Checksum
            if (checksum != ComputeChecksum(hwHash, dateVal))
                return (false, DateTime.MinValue, "");

            // Verify Hardware ID
            string currentHwId = GetHardwareId();
            uint currentHwHash = ComputeHardwareHash(currentHwId);
            
            if (hwHash != currentHwHash)
                return (false, DateTime.MinValue, "");

            // Verify Date
            DateTime expDate = BaseDate.AddDays(dateVal);
            if (DateTime.Today > expDate)
                return (false, expDate, "");

            return (true, expDate, "");
        }
        catch
        {
            return (false, DateTime.MinValue, "");
        }
    }

    // Helper: Compute 32-bit hash of Hardware ID string
    private uint ComputeHardwareHash(string hwId)
    {
        // Clean input: remove dashes, spaces, and convert to uppercase
        string clean = (hwId ?? "").Replace("-", "").Replace(" ", "").Trim().ToUpper();
        if (string.IsNullOrEmpty(clean)) clean = "UNKNOWN";

        using var sha = SHA256.Create();
        var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(clean));
        return BitConverter.ToUInt32(bytes, 0);
    }

    // Helper: Compute 16-bit checksum
    private ushort ComputeChecksum(uint hwHash, ushort dateVal)
    {
        // Simple mix
        uint mix = hwHash ^ dateVal ^ 0x5A5A5A5A;
        return (ushort)((mix & 0xFFFF) ^ (mix >> 16));
    }

    // Helper: Format hex string into groups
    private string FormatLicenseKey(string hex)
    {
        // AAAA-BBBB-CCCC-DDDD
        if (hex.Length != 16) return hex;
        return $"{hex.Substring(0, 4)}-{hex.Substring(4, 4)}-{hex.Substring(8, 4)}-{hex.Substring(12, 4)}";
    }

    // XTEA Encryption (64-bit block, 128-bit key)
    private static void EncryptXtea(ref uint v0, ref uint v1)
    {
        uint sum = 0;
        uint delta = 0x9E3779B9;
        for (int i = 0; i < 32; i++)
        {
            v0 += (((v1 << 4) ^ (v1 >> 5)) + v1) ^ (sum + XteaKey[sum & 3]);
            sum += delta;
            v1 += (((v0 << 4) ^ (v0 >> 5)) + v0) ^ (sum + XteaKey[(sum >> 11) & 3]);
        }
    }

    // XTEA Decryption
    private static void DecryptXtea(ref uint v0, ref uint v1)
    {
        uint delta = 0x9E3779B9;
        uint sum = delta * 32;
        for (int i = 0; i < 32; i++)
        {
            v1 -= (((v0 << 4) ^ (v0 >> 5)) + v0) ^ (sum + XteaKey[(sum >> 11) & 3]);
            sum -= delta;
            v0 -= (((v1 << 4) ^ (v1 >> 5)) + v1) ^ (sum + XteaKey[sum & 3]);
        }
    }


    static string ReadWmiValue(string query, string property)
    {
        try
        {
            // Windows only
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) return "NON-WINDOWS";

            using var searcher = new ManagementObjectSearcher(query);
            foreach (var item in searcher.Get())
            {
                var value = item[property]?.ToString();
                if (!string.IsNullOrWhiteSpace(value))
                    return value.Trim();
            }
        }
        catch
        {
        }
        return "";
    }

    static string ReadMachineGuid()
    {
        try
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return "";

            using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Cryptography", false);
            var value = key?.GetValue("MachineGuid")?.ToString();
            return value?.Trim() ?? "";
        }
        catch
        {
            return "";
        }
    }
}
