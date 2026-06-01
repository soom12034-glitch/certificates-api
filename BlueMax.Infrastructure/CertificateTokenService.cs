using System.Security.Cryptography;
using System.Text;

namespace BlueMax.Infrastructure;

public class CertificateTokenService
{
    private readonly string _secretKey;
    private readonly string _baseUrl;
    private readonly Dictionary<string, string> _cache = new();
    private readonly object _cacheLock = new();
    private const int CacheExpirationMinutes = 30;

    public CertificateTokenService(string secretKey, string baseUrl)
    {
        _secretKey = secretKey;
        _baseUrl = baseUrl.TrimEnd('/');
    }

    public string GenerateVerificationUrl(string certificateNumber, DateTime issueDate, DateTime expiryDate)
    {
        var cacheKey = $"{certificateNumber}_{issueDate:yyyyMMdd}_{expiryDate:yyyyMMdd}";
        
        // Check cache first
        lock (_cacheLock)
        {
            if (_cache.TryGetValue(cacheKey, out var cachedUrl))
                return cachedUrl;
        }

        var payload = BuildPayload(certificateNumber, issueDate, expiryDate);
        var signature = ComputeSignature(payload);
        var token = BuildToken(payload, signature);
        var urlEncodedToken = Uri.EscapeDataString(token);
        var url = $"{_baseUrl}/cert/verify?t={urlEncodedToken}";

        // Cache the result
        lock (_cacheLock)
        {
            _cache[cacheKey] = url;
        }

        return url;
    }

    private static string BuildPayload(string certificateNumber, DateTime issueDate, DateTime expiryDate)
    {
        var cn = certificateNumber.Trim();
        var issue = issueDate.ToString("yyyyMMdd");
        var expiry = expiryDate.ToString("yyyyMMdd");
        return $"{cn}|{issue}|{expiry}";
    }

    private string ComputeSignature(string payload)
    {
        var keyBytes = Encoding.UTF8.GetBytes(_secretKey);
        using var hmac = new HMACSHA256(keyBytes);
        var bytes = Encoding.UTF8.GetBytes(payload);
        var hash = hmac.ComputeHash(bytes);
        var sb = new StringBuilder(hash.Length * 2);
        foreach (var b in hash)
            sb.Append(b.ToString("x2"));
        return sb.ToString();
    }

    private static string BuildToken(string payload, string signature)
    {
        var combined = $"{payload}|{signature}";
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(combined));
    }
}
