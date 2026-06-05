using System.Text.Json;
using System.Globalization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;
using Certificates.Api.Data;
using Certificates.Api.Models;

var builder = WebApplication.CreateBuilder(args);

// Resolve DB path and storage directory
var dbPath = Environment.GetEnvironmentVariable("DB_PATH");
if (string.IsNullOrWhiteSpace(dbPath))
{
    var storageBase = Path.Combine(AppContext.BaseDirectory, "Storage");
    Directory.CreateDirectory(storageBase);
    dbPath = Path.Combine(storageBase, "app.db");
}

var storageDir = Environment.GetEnvironmentVariable("STORAGE_DIR");
if (string.IsNullOrWhiteSpace(storageDir))
{
    storageDir = Path.Combine(AppContext.BaseDirectory, "Storage", "certificates");
}
Directory.CreateDirectory(storageDir);

builder.Services.AddDbContext<AppDbContext>(opt => opt.UseSqlite($"Data Source={dbPath}"));

var app = builder.Build();

var pathBase = Environment.GetEnvironmentVariable("PATH_BASE");

// Ensure DB is created
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.EnsureCreatedAsync();
}

// Serve PDFs as static files from /files/{id}.pdf
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(storageDir),
    RequestPath = "/files"
});

app.MapGet("/healthz", () => Results.Ok(new { status = "ok" }));
app.MapGet("/routes", () => Results.Ok(new[] { "GET /healthz", "GET /routes", "POST /v1/certificates", "POST /certificates", "GET /v1/verify/{id}", "GET /verify/{id}", "GET /files/{id}.pdf" }));

app.Lifetime.ApplicationStarted.Register(() =>
{
    _ = Task.Run(async () =>
    {
        if (!IsEnabled(Environment.GetEnvironmentVariable("AUTO_CLEAN_EXPIRED")))
            return;

        await using var scope = app.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var deleted = await CleanupExpiredCertificatesAsync(db, storageDir);
        Console.WriteLine($"Expired certificates cleanup completed. Deleted: {deleted}");
    });
});

// Upload endpoint
app.MapPost("/v1/certificates", async (HttpRequest request, AppDbContext db) =>
{
    var configuredKey = Environment.GetEnvironmentVariable("API_KEY");
    if (string.IsNullOrWhiteSpace(configuredKey) || !request.Headers.TryGetValue("X-Api-Key", out var provided) || provided != configuredKey)
        return Results.Unauthorized();

    if (!request.HasFormContentType)
        return Results.BadRequest("Expected multipart/form-data");

    var form = await request.ReadFormAsync();
    var file = form.Files["file"]; // PDF
    if (file == null || file.Length == 0)
        return Results.BadRequest("Missing 'file'");

    var fingerprint = form.TryGetValue("fingerprint", out var fp) ? fp.ToString() : null;
    var meta = form.TryGetValue("meta", out var mj) ? mj.ToString() : null;

    var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
    if (ext != ".pdf")
        return Results.BadRequest("Only PDF is allowed");

    var cleanFingerprint = string.IsNullOrWhiteSpace(fingerprint) ? null : fingerprint.Trim();
    var entity = cleanFingerprint == null
        ? null
        : await db.Certificates.FirstOrDefaultAsync(x => x.Fingerprint == cleanFingerprint);
    var isUpdate = entity != null;
    var id = entity?.Id ?? Guid.NewGuid();
    var savedFileName = entity?.FileName;
    if (string.IsNullOrWhiteSpace(savedFileName))
        savedFileName = $"{id:N}.pdf";
    var fullPath = Path.Combine(storageDir, savedFileName);

    await using (var stream = File.Create(fullPath))
    await using (var input = file.OpenReadStream())
    {
        await input.CopyToAsync(stream);
    }

    if (entity == null)
    {
        entity = new Certificate
        {
            Id = id,
            CreatedAtUtc = DateTime.UtcNow
        };
        db.Certificates.Add(entity);
    }

    entity.Fingerprint = cleanFingerprint;
    entity.FileName = savedFileName;
    entity.OriginalFileName = file.FileName;
    entity.ContentType = "application/pdf";
    entity.FileSize = file.Length;
    entity.MetaJson = string.IsNullOrWhiteSpace(meta) ? null : meta;
    await db.SaveChangesAsync();

    var publicBase = Environment.GetEnvironmentVariable("PUBLIC_BASE_URL");
    if (string.IsNullOrWhiteSpace(publicBase))
    {
        var scheme = request.Headers.TryGetValue("X-Forwarded-Proto", out var proto) ? proto.ToString() : request.Scheme;
        var host = request.Headers.TryGetValue("X-Forwarded-Host", out var fwdHost) ? fwdHost.ToString() : request.Host.Value;
        var pb = request.HttpContext.Request.PathBase.HasValue ? request.HttpContext.Request.PathBase.Value : string.Empty;
        publicBase = $"{scheme}://{host}{pb}";
    }

    var apiPrefix = string.Equals(pathBase?.Trim('/'), "v1", StringComparison.OrdinalIgnoreCase) ? string.Empty : "/v1";
    var verifyUrl = $"{publicBase}{apiPrefix}/verify/{id:N}";
    return Results.Ok(new { id = id.ToString("N"), verifyUrl, updated = isUpdate });
});
app.MapPost("/certificates", async (HttpRequest request, AppDbContext db) =>
{
    var configuredKey = Environment.GetEnvironmentVariable("API_KEY");
    if (string.IsNullOrWhiteSpace(configuredKey) || !request.Headers.TryGetValue("X-Api-Key", out var provided) || provided != configuredKey)
        return Results.Unauthorized();

    if (!request.HasFormContentType)
        return Results.BadRequest("Expected multipart/form-data");

    var form = await request.ReadFormAsync();
    var file = form.Files["file"];
    if (file == null || file.Length == 0)
        return Results.BadRequest("Missing 'file'");

    var fingerprint = form.TryGetValue("fingerprint", out var fp) ? fp.ToString() : null;
    var meta = form.TryGetValue("meta", out var mj) ? mj.ToString() : null;

    var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
    if (ext != ".pdf")
        return Results.BadRequest("Only PDF is allowed");

    var cleanFingerprint = string.IsNullOrWhiteSpace(fingerprint) ? null : fingerprint.Trim();
    var entity = cleanFingerprint == null
        ? null
        : await db.Certificates.FirstOrDefaultAsync(x => x.Fingerprint == cleanFingerprint);
    var isUpdate = entity != null;
    var id = entity?.Id ?? Guid.NewGuid();
    var savedFileName = entity?.FileName;
    if (string.IsNullOrWhiteSpace(savedFileName))
        savedFileName = $"{id:N}.pdf";
    var fullPath = Path.Combine(storageDir, savedFileName);

    await using (var stream = File.Create(fullPath))
    await using (var input = file.OpenReadStream())
    {
        await input.CopyToAsync(stream);
    }

    if (entity == null)
    {
        entity = new Certificate
        {
            Id = id,
            CreatedAtUtc = DateTime.UtcNow
        };
        db.Certificates.Add(entity);
    }

    entity.Fingerprint = cleanFingerprint;
    entity.FileName = savedFileName;
    entity.OriginalFileName = file.FileName;
    entity.ContentType = "application/pdf";
    entity.FileSize = file.Length;
    entity.MetaJson = string.IsNullOrWhiteSpace(meta) ? null : meta;
    await db.SaveChangesAsync();

    var publicBase = Environment.GetEnvironmentVariable("PUBLIC_BASE_URL");
    if (string.IsNullOrWhiteSpace(publicBase))
    {
        var scheme = request.Headers.TryGetValue("X-Forwarded-Proto", out var proto) ? proto.ToString() : request.Scheme;
        var host = request.Headers.TryGetValue("X-Forwarded-Host", out var fwdHost) ? fwdHost.ToString() : request.Host.Value;
        var pb = request.HttpContext.Request.PathBase.HasValue ? request.HttpContext.Request.PathBase.Value : string.Empty;
        publicBase = $"{scheme}://{host}{pb}";
    }

    var verifyUrl = $"{publicBase}/verify/{id:N}";
    return Results.Ok(new { id = id.ToString("N"), verifyUrl, updated = isUpdate });
});

app.MapDelete("/v1/admin/certificates/expired", async (HttpRequest request, AppDbContext db) =>
{
    var configuredKey = Environment.GetEnvironmentVariable("API_KEY");
    if (string.IsNullOrWhiteSpace(configuredKey) || !request.Headers.TryGetValue("X-Api-Key", out var provided) || provided != configuredKey)
        return Results.Unauthorized();

    var deleted = await CleanupExpiredCertificatesAsync(db, storageDir);
    return Results.Ok(new { deleted });
});

// Public verification page
app.MapGet("/v1/verify/{id}", async (Guid id, AppDbContext db, HttpContext http) =>
{
    var cert = await db.Certificates.FindAsync(id);
    if (cert == null) return Results.NotFound("Certificate not found");

    var baseUrl = Environment.GetEnvironmentVariable("PUBLIC_BASE_URL");
    if (string.IsNullOrWhiteSpace(baseUrl))
    {
        var scheme = http.Request.Headers.TryGetValue("X-Forwarded-Proto", out var proto) ? proto.ToString() : http.Request.Scheme;
        var host = http.Request.Headers.TryGetValue("X-Forwarded-Host", out var fwdHost) ? fwdHost.ToString() : http.Request.Host.Value;
        var pb = http.Request.PathBase.HasValue ? http.Request.PathBase.Value : string.Empty;
        baseUrl = $"{scheme}://{host}{pb}";
    }

    var fileUrl = $"{baseUrl}/files/{id:N}.pdf";
    var expiryDate = TryReadExpiryDate(cert.MetaJson);
    var today = DateTime.UtcNow.Date;
    var remainingDays = expiryDate.HasValue ? (expiryDate.Value.Date - today).Days : (int?)null;
    var isExpired = remainingDays.HasValue && remainingDays.Value < 0;
    var statusText = !expiryDate.HasValue
        ? "Valid"
        : isExpired
            ? "Expired / منتهية"
            : "Valid / سارية";
    var statusClass = isExpired ? "expired" : "valid";
    var remainingText = !expiryDate.HasValue
        ? "Not available"
        : isExpired
            ? $"Expired {Math.Abs(remainingDays!.Value)} day(s) ago / منتهية منذ {Math.Abs(remainingDays.Value)} يوم"
            : $"{remainingDays!.Value} day(s) remaining / متبقي {remainingDays.Value} يوم";
    var expiryText = expiryDate.HasValue ? expiryDate.Value.ToString("yyyy-MM-dd") : "-";
    var html = $@"<!doctype html>
<html lang='en'><head><meta charset='utf-8'>
<meta name='viewport' content='width=device-width,initial-scale=1'>
<title>Certificate {id:N}</title>
<style>body{{font-family:-apple-system,Segoe UI,Roboto,Arial,sans-serif;margin:40px;line-height:1.6}} .box{{border:1px solid #e5e7eb;border-radius:8px;padding:16px;background:#fafafa}} .status{{display:inline-block;padding:6px 12px;border-radius:999px;font-weight:700}} .valid{{color:#166534;background:#dcfce7;border:1px solid #86efac}} .expired{{color:#991b1b;background:#fee2e2;border:1px solid #fca5a5}} .expired-line{{margin-top:14px;padding:12px;border:2px solid #dc2626;color:#991b1b;background:#fef2f2;font-weight:700;border-radius:8px}}</style>
</head><body>
<h1>Certificate Verification</h1>
<div class='box'>
<p><b>Status:</b> <span class='status {statusClass}'>{statusText}</span></p>
<p><b>Remaining validity:</b> {remainingText}</p>
<p><b>Expiry date:</b> {expiryText}</p>
<p><b>Fingerprint:</b> {System.Net.WebUtility.HtmlEncode(cert.Fingerprint ?? "-")}</p>
<p><b>Issued (UTC):</b> {cert.CreatedAtUtc:yyyy-MM-dd HH:mm}</p>
<p><a href='{fileUrl}' target='_blank' rel='noopener'>Download PDF</a></p>
{(isExpired ? "<div class='expired-line'>This certificate is expired / هذه الشهادة منتهية الصلاحية</div>" : "")}
</div>
</body></html>";
    return Results.Content(html, "text/html");
});
app.MapGet("/verify/{id}", async (Guid id, AppDbContext db, HttpContext http) =>
{
    var cert = await db.Certificates.FindAsync(id);
    if (cert == null) return Results.NotFound("Certificate not found");

    var baseUrl = Environment.GetEnvironmentVariable("PUBLIC_BASE_URL");
    if (string.IsNullOrWhiteSpace(baseUrl))
    {
        var scheme = http.Request.Headers.TryGetValue("X-Forwarded-Proto", out var proto) ? proto.ToString() : http.Request.Scheme;
        var host = http.Request.Headers.TryGetValue("X-Forwarded-Host", out var fwdHost) ? fwdHost.ToString() : http.Request.Host.Value;
        var pb = http.Request.PathBase.HasValue ? http.Request.PathBase.Value : string.Empty;
        baseUrl = $"{scheme}://{host}{pb}";
    }

    var fileUrl = $"{baseUrl}/files/{id:N}.pdf";
    var expiryDate = TryReadExpiryDate(cert.MetaJson);
    var today = DateTime.UtcNow.Date;
    var remainingDays = expiryDate.HasValue ? (expiryDate.Value.Date - today).Days : (int?)null;
    var isExpired = remainingDays.HasValue && remainingDays.Value < 0;
    var statusText = !expiryDate.HasValue
        ? "Valid"
        : isExpired
            ? "Expired / منتهية"
            : "Valid / سارية";
    var statusClass = isExpired ? "expired" : "valid";
    var remainingText = !expiryDate.HasValue
        ? "Not available"
        : isExpired
            ? $"Expired {Math.Abs(remainingDays!.Value)} day(s) ago / منتهية منذ {Math.Abs(remainingDays.Value)} يوم"
            : $"{remainingDays!.Value} day(s) remaining / متبقي {remainingDays.Value} يوم";
    var expiryText = expiryDate.HasValue ? expiryDate.Value.ToString("yyyy-MM-dd") : "-";
    var html = $@"<!doctype html>
<html lang='en'><head><meta charset='utf-8'>
<meta name='viewport' content='width=device-width,initial-scale=1'>
<title>Certificate {id:N}</title>
<style>body{{font-family:-apple-system,Segoe UI,Roboto,Arial,sans-serif;margin:40px;line-height:1.6}} .box{{border:1px solid #e5e7eb;border-radius:8px;padding:16px;background:#fafafa}} .status{{display:inline-block;padding:6px 12px;border-radius:999px;font-weight:700}} .valid{{color:#166534;background:#dcfce7;border:1px solid #86efac}} .expired{{color:#991b1b;background:#fee2e2;border:1px solid #fca5a5}} .expired-line{{margin-top:14px;padding:12px;border:2px solid #dc2626;color:#991b1b;background:#fef2f2;font-weight:700;border-radius:8px}}</style>
</head><body>
<h1>Certificate Verification</h1>
<div class='box'>
<p><b>Status:</b> <span class='status {statusClass}'>{statusText}</span></p>
<p><b>Remaining validity:</b> {remainingText}</p>
<p><b>Expiry date:</b> {expiryText}</p>
<p><b>Fingerprint:</b> {System.Net.WebUtility.HtmlEncode(cert.Fingerprint ?? "-")}</p>
<p><b>Issued (UTC):</b> {cert.CreatedAtUtc:yyyy-MM-dd HH:mm}</p>
<p><a href='{fileUrl}' target='_blank' rel='noopener'>Download PDF</a></p>
{(isExpired ? "<div class='expired-line'>This certificate is expired / هذه الشهادة منتهية الصلاحية</div>" : "")}
</div>
</body></html>";
    return Results.Content(html, "text/html");
});

app.Run();

static bool IsEnabled(string? value)
{
    return string.Equals(value, "true", StringComparison.OrdinalIgnoreCase)
        || string.Equals(value, "1", StringComparison.OrdinalIgnoreCase)
        || string.Equals(value, "yes", StringComparison.OrdinalIgnoreCase);
}

static int GetDeleteExpiredAfterDays()
{
    var raw = Environment.GetEnvironmentVariable("DELETE_EXPIRED_AFTER_DAYS");
    return int.TryParse(raw, out var days) && days >= 0 ? days : 365;
}

static DateTime? TryReadExpiryDate(string? metaJson)
{
    if (string.IsNullOrWhiteSpace(metaJson))
        return null;

    try
    {
        using var doc = JsonDocument.Parse(metaJson);
        if (!doc.RootElement.TryGetProperty("expiryDate", out var expiryElement))
            return null;

        var value = expiryElement.GetString();
        if (string.IsNullOrWhiteSpace(value))
            return null;

        if (DateTime.TryParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var exact))
            return exact.Date;

        if (DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var parsed))
            return parsed.Date;
    }
    catch
    {
    }

    return null;
}

static async Task<int> CleanupExpiredCertificatesAsync(AppDbContext db, string storageDir)
{
    var cutoff = DateTime.UtcNow.Date.AddDays(-GetDeleteExpiredAfterDays());
    var certificates = await db.Certificates.ToListAsync();
    var expired = certificates
        .Where(x =>
        {
            var expiryDate = TryReadExpiryDate(x.MetaJson);
            return expiryDate.HasValue && expiryDate.Value.Date <= cutoff;
        })
        .ToList();

    foreach (var cert in expired)
    {
        try
        {
            var fullPath = Path.Combine(storageDir, cert.FileName);
            if (File.Exists(fullPath))
                File.Delete(fullPath);
        }
        catch
        {
        }
    }

    db.Certificates.RemoveRange(expired);
    await db.SaveChangesAsync();
    return expired.Count;
}
