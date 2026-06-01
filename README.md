# BlueMax - Certificate Management System

## Overview
BlueMax is a WPF application for managing calibration certificates, work orders, customers, and financial ledger entries. It uses Entity Framework Core with SQL Server and SQLite fallback, with license activation using hardware ID-based encryption.

## Architecture

### Layers
- **BlueMax.Domain**: Domain entities (Certificate, Customer, WorkOrder, LedgerEntry, CertificateBarcodeInfo)
- **BlueMax.Infrastructure**: Data access, encryption services, backup services, document generation
- **BlueMax.Presentation.Wpf**: WPF UI with MVVM pattern

### Key Components
- **License System**: XTEA encryption with hardware ID validation
- **Access Control**: Role-based access (Admin/User) with PIN protection
- **Backup Service**: Automated daily backups at 2 AM
- **Document Generation**: Word and PDF certificate generation
- **Logging**: Structured logging with rotation and audit trails

## Security Features

### Implemented
- ✅ XTEA encryption with randomly generated keys
- ✅ DPAPI with CurrentUser scope for key protection
- ✅ Rate limiting for login attempts (5 attempts in 15 minutes)
- ✅ SQL injection prevention with identifier validation
- ✅ SQLite database encryption support
- ✅ Audit logging for security events
- ✅ Foreign key constraints with cascade restrictions

### Connection String
Default: `Server=(localdb)\MSSQLLocalDB;Database=MyAppDb;Integrated Security=True`
Note: TrustServerCertificate removed for security

## Configuration

### License Activation
- Base Date: January 1, 2025
- Hardware ID: Based on CPU ProcessorId and BaseBoard SerialNumber
- Storage: `%LocalAppData%\BlueMax\CertSystem\Settings\License.json`

### Backup Settings
- Storage: `%LocalAppData%\BlueMax\CertSystem\Settings\BackupSettings.json`
- Schedule: Daily at 2 AM
- Retention: 5 backup files

### Logging
- Location: `%LocalAppData%\BlueMax\CertSystem\Logs\`
- Files: application.log, audit.log, errors.log
- Rotation: 5 MB max size, 5 files retained

## Data Validation

All domain entities include validation attributes:
- Required fields
- String length limits
- Phone number format validation
- Numeric range validation

## Dependencies

### .NET 8.0
- Microsoft.EntityFrameworkCore.SqlServer
- Microsoft.EntityFrameworkCore.Sqlite
- DocumentFormat.OpenXml
- QuestPDF
- ZXing.Net
- Microsoft.ReportViewer.WinForms

## Build Instructions

```bash
dotnet restore
dotnet build
dotnet publish -c Release
```

## Deployment

1. Exclude Keygen folder from production builds
2. Ensure SQL Server LocalDB or SQLite is available
3. Configure connection string in App.config
4. Set up license activation for production

## Troubleshooting

### License Issues
- Check hardware ID stability
- Verify license expiration date
- Ensure activation code matches hardware

### Database Issues
- Verify SQL Server LocalDB is running
- Check connection string format
- Ensure database permissions

### Backup Issues
- Verify backup path permissions
- Check available disk space
- Review backup logs

## Version History
- v1.0: Initial release
- v1.1: Enhanced security (random keys, rate limiting, audit logging)
- v1.2: Performance improvements (caching, structured logging)
