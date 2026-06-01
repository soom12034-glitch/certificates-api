using Microsoft.EntityFrameworkCore;
using Certificates.Api.Models;

namespace Certificates.Api.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Certificate> Certificates => Set<Certificate>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Certificate>(e =>
            {
                e.HasKey(x => x.Id);
                e.Property(x => x.Id).ValueGeneratedNever();
                e.Property(x => x.FileName).IsRequired();
                e.Property(x => x.OriginalFileName).IsRequired();
                e.Property(x => x.ContentType).IsRequired();
                e.HasIndex(x => x.Fingerprint);
                e.HasIndex(x => x.CreatedAtUtc);
            });
        }
    }
}
