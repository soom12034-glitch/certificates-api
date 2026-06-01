namespace Certificates.Api.Models
{
    public class Certificate
    {
        public Guid Id { get; set; }
        public string? Fingerprint { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string OriginalFileName { get; set; } = string.Empty;
        public string ContentType { get; set; } = "application/pdf";
        public long FileSize { get; set; }
        public DateTime CreatedAtUtc { get; set; }
        public string? MetaJson { get; set; }
    }
}
