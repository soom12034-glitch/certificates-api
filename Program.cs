using System.Text.Json;
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

// Optional path base (e.g., /verify) for running behind a reverse proxy on a sub-path
var pathBase = Environment.GetEnvironmentVariable("PATH_BASE");
if (!string.IsNullOrWhiteSpace(pathBase))
{
    if (!pathBase.StartsWith("/")) pathBase = "/" + pathBase;
    app.UsePathBase(pathBase);
}

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

    var id = Guid.NewGuid();
    var savedFileName = $"{id:N}.pdf";
    var fullPath = Path.Combine(storageDir, savedFileName);

    await using (var stream = File.Create(fullPath))
    await using (var input = file.OpenReadStream())
    {
        await input.CopyToAsync(stream);
    }

    var entity = new Certificate
    {
        Id = id,
        Fingerprint = string.IsNullOrWhiteSpace(fingerprint) ? null : fingerprint.Trim(),
        FileName = savedFileName,
        OriginalFileName = file.FileName,
        ContentType = "application/pdf",
        FileSize = file.Length,
        CreatedAtUtc = DateTime.UtcNow,
        MetaJson = string.IsNullOrWhiteSpace(meta) ? null : meta
    };
    db.Certificates.Add(entity);
    await db.SaveChangesAsync();

    var publicBase = Environment.GetEnvironmentVariable("PUBLIC_BASE_URL");
    if (string.IsNullOrWhiteSpace(publicBase))
    {
        var scheme = request.Headers.TryGetValue("X-Forwarded-Proto", out var proto) ? proto.ToString() : request.Scheme;
        var host = request.Headers.TryGetValue("X-Forwarded-Host", out var fwdHost) ? fwdHost.ToString() : request.Host.Value;
        var pb = request.HttpContext.Request.PathBase.HasValue ? request.HttpContext.Request.PathBase.Value : string.Empty;
        publicBase = $"{scheme}://{host}{pb}";
    }

    var verifyUrl = $"{publicBase}/v1/verify/{id:N}";
    return Results.Ok(new { id = id.ToString("N"), verifyUrl });
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
    var html = $@"<!doctype html>
<html lang='en'><head><meta charset='utf-8'>
<meta name='viewport' content='width=device-width,initial-scale=1'>
<title>Certificate {id:N}</title>
<style>body{{font-family:-apple-system,Segoe UI,Roboto,Arial,sans-serif;margin:40px;line-height:1.6}} .box{{border:1px solid #e5e7eb;border-radius:8px;padding:16px;background:#fafafa}}</style>
</head><body>
<h1>Certificate Verification</h1>
<div class='box'>
<p><b>Status:</b> Valid</p>
<p><b>Fingerprint:</b> {System.Net.WebUtility.HtmlEncode(cert.Fingerprint ?? "-")}</p>
<p><b>Issued (UTC):</b> {cert.CreatedAtUtc:yyyy-MM-dd HH:mm}</p>
<p><a href='{fileUrl}' target='_blank' rel='noopener'>Download PDF</a></p>
</div>
</body></html>";
    return Results.Content(html, "text/html");
});

app.Run();
