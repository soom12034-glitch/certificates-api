using BlueMax.Domain;
using QuestPDF.Infrastructure;
using QuestPDF.Fluent;
using System.Drawing.Imaging;
using System.Configuration;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using ZXing;
using ZXing.Common;
using ZXing.QrCode;
using ZXing.QrCode.Internal;
using ZXing.Windows.Compatibility;
using System.Text.Json;

namespace BlueMax.Infrastructure;

public class CertificateDocumentService
{
    private readonly AppDbContext _db;
    private readonly WordTemplateEngine _wordEngine;

    public CertificateDocumentService(
        AppDbContext db,
        WordTemplateEngine wordEngine)
    {
        _db = db;
        _wordEngine = wordEngine;
    }

    public async Task<string> GenerateCertificateDocxAndPdfByIdAsync(int certificateId, string templateName)
    {
        var cert = await _db.Certificates.FindAsync(certificateId);
        if (cert == null)
            throw new InvalidOperationException($"Certificate with ID {certificateId} not found");

        var data = BuildCertificateTokenData(cert, LoadReportSettings(), LoadVerificationBaseUrl());

        var docxPath = await Task.Run(() => _wordEngine.GenerateDocument(templateName, data));
        var pdfPath = Path.ChangeExtension(docxPath, ".pdf");

        await Task.Run(() =>
        {
            Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(595, 842);
                    page.Margin(2, Unit.Centimetre);
                    page.DefaultTextStyle(x => x.FontSize(12));

                    page.Header().Element(c => c.AlignCenter().Text("Certificate of Calibration").Bold().FontSize(20));
                    page.Content().Element(c => GeneratePdfContent(c, cert));
                    page.Footer().AlignCenter().Text(x =>
                    {
                        x.Span("Page ");
                        x.CurrentPageNumber();
                    });
                });
            }).GeneratePdf(pdfPath);
        });

        return docxPath;
    }

    private static Dictionary<string, object> BuildCertificateTokenData(Certificate cert, ReportDesignerSettings reportSettings, string verificationBaseUrl)
    {
        var qrPayload = BuildCertificateQrPayload(cert, reportSettings, verificationBaseUrl);
        var qrImagePath = TryBuildQrImagePath(qrPayload);
        var isGps = string.Equals(cert.DeviceType, "GPS", StringComparison.OrdinalIgnoreCase);
        var baseSerial = cert.SerialText ?? "";
        var roverSerial = cert.SerialText2 ?? "";
        var serialForGenericToken = isGps && !string.IsNullOrWhiteSpace(roverSerial)
            ? string.Join(" / ", new[] { baseSerial, roverSerial }.Where(x => !string.IsNullOrWhiteSpace(x)))
            : baseSerial;

        var data = new Dictionary<string, object>
        {
            ["CertificateNumber"] = cert.CertificateNumber,
            ["cert_no"] = cert.CertificateNumber,
            ["ClientName"] = cert.ClientName,
            ["client_name"] = cert.ClientName,
            ["Phone"] = cert.Phone ?? "",
            ["phone"] = cert.Phone ?? "",
            ["DeviceType"] = cert.DeviceType,
            ["device_type"] = cert.DeviceType,
            ["Brand"] = cert.Brand ?? "",
            ["brand"] = cert.Brand ?? "",
            ["Model"] = cert.Model ?? "",
            ["model"] = cert.Model ?? "",
            ["SerialText"] = baseSerial,
            ["serial"] = serialForGenericToken,
            ["sn"] = baseSerial,
            ["serial_base"] = baseSerial,
            ["sn_base"] = baseSerial,
            ["SerialText2"] = roverSerial,
            ["serial2"] = roverSerial,
            ["sn2"] = roverSerial,
            ["sn_rover"] = roverSerial,
            ["rover_serial"] = roverSerial,
            ["SpecValue"] = cert.SpecValue ?? "",
            ["spec_value"] = cert.SpecValue ?? "",
            ["spec"] = cert.SpecValue ?? "",
            ["deviation_rate"] = cert.SpecValue ?? "",
            ["auto_level_spec"] = cert.SpecValue ?? "",
            ["DelegateName"] = cert.DelegateName ?? "",
            ["delegate_name"] = cert.DelegateName ?? "",
            ["IssueDate"] = cert.IssueDate.ToString("yyyy-MM-dd"),
            ["issue_date"] = cert.IssueDate.ToString("yyyy-MM-dd"),
            ["date"] = cert.IssueDate.ToString("yyyy-MM-dd"),
            ["ExpiryDate"] = cert.ExpiryDate.ToString("yyyy-MM-dd"),
            ["expiry_date"] = cert.ExpiryDate.ToString("yyyy-MM-dd"),
            ["valid_until"] = cert.ExpiryDate.ToString("yyyy-MM-dd"),
            ["qr_image_path"] = qrImagePath,
            ["qr_code_img"] = string.IsNullOrWhiteSpace(qrImagePath) ? "" : WordTemplateEngine.QrImageMarker,
            ["company_logo_img"] = ""
        };

        TryWriteCertificateTrace(cert, data, qrPayload, qrImagePath);
        return data;
    }

    private static void TryWriteCertificateTrace(Certificate cert, Dictionary<string, object> data, string qrPayload, string qrImagePath)
    {
        try
        {
            var logDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "BlueMax", "CertSystem", "Logs");
            Directory.CreateDirectory(logDir);
            var logPath = Path.Combine(logDir, "certificate-print-trace.log");

            var line = string.Join(" | ",
                DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                $"cert_id={cert.Id}",
                $"cert_no={cert.CertificateNumber}",
                $"device_type={cert.DeviceType}",
                $"sn={GetDataValue(data, "sn")}",
                $"sn2={GetDataValue(data, "sn2")}",
                $"spec_value={GetDataValue(data, "spec_value")}",
                $"valid_until={GetDataValue(data, "valid_until")}",
                $"qr_path_exists={(!string.IsNullOrWhiteSpace(qrImagePath) && File.Exists(qrImagePath)).ToString().ToLowerInvariant()}",
                $"qr_payload={qrPayload.Replace(Environment.NewLine, " || ")}");

            File.AppendAllText(logPath, line + Environment.NewLine);
        }
        catch
        {
        }
    }

    private static string GetDataValue(Dictionary<string, object> data, string key)
    {
        foreach (var kv in data)
        {
            if (string.Equals(kv.Key, key, StringComparison.OrdinalIgnoreCase))
                return kv.Value?.ToString() ?? string.Empty;
        }

        return string.Empty;
    }

    private static string TryBuildQrImagePath(string payload)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(payload))
                return string.Empty;

            var options = new QrCodeEncodingOptions
            {
                Width = 600,
                Height = 600,
                Margin = 4,
                CharacterSet = "UTF-8",
                ErrorCorrection = ErrorCorrectionLevel.M
            };

            var writer = new BarcodeWriter
            {
                Format = BarcodeFormat.QR_CODE,
                Renderer = new BitmapRenderer(),
                Options = options
            };

            using var bitmap = writer.Write(payload);
            var outputPath = Path.Combine(Path.GetTempPath(), $"BlueMax_QR_{Guid.NewGuid():N}.png");
            bitmap.Save(outputPath, System.Drawing.Imaging.ImageFormat.Png);
            return outputPath;
        }
        catch
        {
            return string.Empty;
        }
    }

    private static void GeneratePdfContent(IContainer container, Certificate cert)
    {
        container.Column(column =>
        {
            column.Item().Text($"Certificate Number: {cert.CertificateNumber}").Bold();
            column.Item().Text($"Client: {cert.ClientName}");
            column.Item().Text($"Phone: {cert.Phone ?? "N/A"}");
            column.Item().Text($"Device Type: {cert.DeviceType}");
            column.Item().Text($"Brand: {cert.Brand ?? "N/A"}");
            column.Item().Text($"Model: {cert.Model ?? "N/A"}");
            column.Item().Text($"Serial: {cert.SerialText ?? "N/A"}");
            column.Item().Text($"Issue Date: {cert.IssueDate:yyyy-MM-dd}");
            column.Item().Text($"Expiry Date: {cert.ExpiryDate:yyyy-MM-dd}");
        });
    }

    public async Task<string> GenerateCertificateDocxAndPdfByIdFromPathAsync(int certificateId, string templatePath)
    {
        var cert = await _db.Certificates.FindAsync(certificateId);
        if (cert == null)
            throw new InvalidOperationException($"Certificate with ID {certificateId} not found");

        var data = BuildCertificateTokenData(cert, LoadReportSettings(), LoadVerificationBaseUrl());

        var docxPath = await Task.Run(() => _wordEngine.GenerateDocumentFromPath(templatePath, data));
        var pdfPath = Path.ChangeExtension(docxPath, ".pdf");

        await Task.Run(() =>
        {
            if (!TryConvertDocxToPdfWithWord(docxPath, pdfPath))
                GenerateFallbackPdf(pdfPath, cert);
        });

        return docxPath;
    }

    private static bool TryConvertDocxToPdfWithWord(string docxPath, string pdfPath)
    {
        dynamic? app = null;
        dynamic? document = null;
        try
        {
            if (string.IsNullOrWhiteSpace(docxPath) || !File.Exists(docxPath))
                return false;

            Directory.CreateDirectory(Path.GetDirectoryName(pdfPath) ?? AppContext.BaseDirectory);
            var wordType = Type.GetTypeFromProgID("Word.Application");
            if (wordType == null)
                return false;

            app = Activator.CreateInstance(wordType);
            if (app == null)
                return false;

            app.Visible = false;
            app.DisplayAlerts = 0;
            try { app.AutomationSecurity = 3; } catch { }
            document = app.Documents.Open(
                FileName: docxPath,
                ConfirmConversions: false,
                ReadOnly: true,
                AddToRecentFiles: false,
                PasswordDocument: "",
                PasswordTemplate: "",
                Revert: false,
                WritePasswordDocument: "",
                WritePasswordTemplate: "",
                Format: 0,
                Encoding: 65001,
                Visible: false,
                OpenAndRepair: true,
                NoEncodingDialog: true);
            document.ExportAsFixedFormat(
                pdfPath,
                17,
                OpenAfterExport: false,
                OptimizeFor: 0,
                Range: 0,
                Item: 0,
                IncludeDocProps: true,
                KeepIRM: true,
                CreateBookmarks: 0,
                DocStructureTags: true,
                BitmapMissingFonts: true,
                UseISO19005_1: true);

            return File.Exists(pdfPath) && new FileInfo(pdfPath).Length > 0;
        }
        catch
        {
            return false;
        }
        finally
        {
            try { document?.Close(false); } catch { }
            try { app?.Quit(false); } catch { }
        }
    }

    private static void GenerateFallbackPdf(string pdfPath, Certificate cert)
    {
        Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(595, 842);
                page.Margin(2, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontSize(12));

                page.Header().Element(c => c.AlignCenter().Text("Certificate of Calibration").Bold().FontSize(20));
                page.Content().Element(c => GeneratePdfContent(c, cert));
                page.Footer().AlignCenter().Text(x =>
                {
                    x.Span("Page ");
                    x.CurrentPageNumber();
                });
            });
        }).GeneratePdf(pdfPath);
    }

    private static string BuildCertificateQrPayload(Certificate cert, ReportDesignerSettings reportSettings, string verificationBaseUrl)
    {
        var companyName = (reportSettings.CompanyName ?? string.Empty).Trim();
        var companyHeader = (reportSettings.CompanyHeader ?? string.Empty).Trim();

        var issueDate = cert.IssueDate == default ? DateTime.Today : cert.IssueDate;
        var expiryDate = cert.ExpiryDate == default ? issueDate.AddMonths(6) : cert.ExpiryDate;

        var brand = (cert.Brand ?? string.Empty).Trim();
        var model = (cert.Model ?? string.Empty).Trim();
        var serial = (cert.SerialText ?? string.Empty).Trim();
        var serial2 = (cert.SerialText2 ?? string.Empty).Trim();
        var specValue = (cert.SpecValue ?? string.Empty).Trim();
        var certNo = (cert.CertificateNumber ?? string.Empty).Trim();

        var baseUrl = string.IsNullOrWhiteSpace(verificationBaseUrl)
            ? "https://example.com/certificates"
            : verificationBaseUrl.Trim();

        // Prefer verifyUrl saved locally (after cloud upload). Fallback to baseUrl/certNo.
        var certUrl = TryLoadVerifyUrlLocal(certNo);
        if (string.IsNullOrWhiteSpace(certUrl))
            certUrl = BuildCertificateUrl(baseUrl, certNo);

        var lines = new List<string>();
        if (!string.IsNullOrWhiteSpace(companyName))
            lines.Add(companyName);
        if (!string.IsNullOrWhiteSpace(companyHeader))
            lines.Add(companyHeader);

        lines.Add($"CAL:{issueDate:yyyy-MM-dd}");
        lines.Add($"EXP:{expiryDate:yyyy-MM-dd}");
        lines.Add($"DEV:{brand} {model}".Trim());
        lines.Add($"SN:{serial}");
        if (!string.IsNullOrWhiteSpace(serial2))
            lines.Add($"SN2:{serial2}");
        if (!string.IsNullOrWhiteSpace(specValue))
            lines.Add($"SPEC:{specValue}");
        lines.Add($"URL:{certUrl}");

        return string.Join(Environment.NewLine, lines.Where(x => !string.IsNullOrWhiteSpace(x)));
    }

    private static string TryLoadVerifyUrlLocal(string certificateNumber)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(certificateNumber))
                return string.Empty;
            var dir = Path.Combine(AppContext.BaseDirectory, "Certificates_Output");
            var mapPath = Path.Combine(dir, "verify_urls.json");
            if (!File.Exists(mapPath))
                return string.Empty;
            var json = File.ReadAllText(mapPath);
            var dict = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
            if (dict != null && dict.TryGetValue(certificateNumber, out var url))
                return url ?? string.Empty;
            return string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }

    private static string BuildCertificateUrl(string baseUrl, string certNo)
    {
        var cleanBase = (baseUrl ?? string.Empty).Trim();
        var cleanNo = (certNo ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(cleanNo))
            return cleanBase;

        cleanBase = cleanBase.TrimEnd('/');
        return $"{cleanBase}/{Uri.EscapeDataString(cleanNo)}";
    }

    private static ReportDesignerSettings LoadReportSettings()
    {
        try
        {
            return new ReportDesignerSettingsStore().Load();
        }
        catch
        {
            return new ReportDesignerSettings();
        }
    }

    private static string LoadVerificationBaseUrl()
    {
        try
        {
            return ConfigurationManager.AppSettings["VerificationBaseUrl"] ?? string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }

    public async Task<string> GenerateWorkOrderReportDocxAndPdfFromPathAsync(int workOrderId, string templatePath)
    {
        var workOrder = await _db.WorkOrders.FindAsync(workOrderId);
        if (workOrder == null)
            throw new InvalidOperationException($"WorkOrder with ID {workOrderId} not found");

        var documentNumber = workOrder.Id.ToString("D4");
        var data = BuildWorkOrderTokenData(workOrder, documentNumber, "");

        var docxPath = await Task.Run(() => _wordEngine.GenerateDocumentFromPath(templatePath, data));
        var pdfPath = Path.ChangeExtension(docxPath, ".pdf");

        await Task.Run(() =>
        {
            Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(595, 842);
                    page.Margin(2, Unit.Centimetre);
                    page.DefaultTextStyle(x => x.FontSize(12));

                    page.Header().Element(c => c.AlignCenter().Text("Work Order Report").Bold().FontSize(20));
                    page.Content().Element(c => c.Column(column =>
                    {
                        column.Item().Text($"Report Number: {documentNumber}").Bold();
                        column.Item().Text($"Work Order Number: {workOrder.Id}");
                        column.Item().Text($"Device Type: {workOrder.DeviceType}");
                        column.Item().Text($"Brand: {workOrder.Brand}");
                        column.Item().Text($"Model: {workOrder.Model}");
                        column.Item().Text($"Serial Number: {workOrder.SerialNumber}");
                        column.Item().Text($"Issue Date: {workOrder.ReceivedDate:yyyy-MM-dd}");
                        column.Item().Text($"Status: {workOrder.Status}");
                    }));
                    page.Footer().AlignCenter().Text(x =>
                    {
                        x.Span("Page ");
                        x.CurrentPageNumber();
                    });
                });
            }).GeneratePdf(pdfPath);
        });

        return docxPath;
    }

    public async Task<string> GenerateWorkOrderReportDocxAndPdfFromPathAsync(int workOrderId, string reportType, string templatePath)
    {
        var workOrder = await _db.WorkOrders.FindAsync(workOrderId);
        if (workOrder == null)
            throw new InvalidOperationException($"WorkOrder with ID {workOrderId} not found");

        var normalizedType = (reportType ?? "").Trim();
        var numberTokenName = normalizedType.Equals("Price Quotation", StringComparison.OrdinalIgnoreCase)
            ? "QuotationNumber"
            : normalizedType.Equals("Maintenance Report", StringComparison.OrdinalIgnoreCase)
                ? "MaintenanceReportNumber"
                : "DocumentNumber";

        var documentNumber = workOrder.Id.ToString("D4");
        var data = BuildWorkOrderTokenData(workOrder, documentNumber, normalizedType);
        data[numberTokenName] = documentNumber;

        var docxPath = await Task.Run(() => _wordEngine.GenerateDocumentFromPath(templatePath, data));
        var pdfPath = Path.ChangeExtension(docxPath, ".pdf");

        await Task.Run(() =>
        {
            Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(595, 842);
                    page.Margin(2, Unit.Centimetre);
                    page.DefaultTextStyle(x => x.FontSize(12));

                    page.Header().Element(c => c.AlignCenter().Text(normalizedType).Bold().FontSize(20));
                    page.Content().Element(c => c.Column(column =>
                    {
                        column.Item().Text($"Document Number: {documentNumber}").Bold();
                        column.Item().Text($"Work Order Number: {workOrder.Id}");
                        column.Item().Text($"Device Type: {workOrder.DeviceType}");
                        column.Item().Text($"Brand: {workOrder.Brand}");
                        column.Item().Text($"Model: {workOrder.Model}");
                        column.Item().Text($"Serial Number: {workOrder.SerialNumber}");
                        column.Item().Text($"Issue Date: {workOrder.ReceivedDate:yyyy-MM-dd}");
                        column.Item().Text($"Status: {workOrder.Status}");
                    }));
                    page.Footer().AlignCenter().Text(x =>
                    {
                        x.Span("Page ");
                        x.CurrentPageNumber();
                    });
                });
            }).GeneratePdf(pdfPath);
        });

        return docxPath;
    }

    private static Dictionary<string, object> BuildWorkOrderTokenData(WorkOrder workOrder, string documentNumber, string reportType)
    {
        return new Dictionary<string, object>
        {
            ["WorkOrderNumber"] = documentNumber,
            ["DocumentNumber"] = documentNumber,
            ["MaintenanceReportNumber"] = documentNumber,
            ["QuotationNumber"] = documentNumber,
            ["work_order_no"] = documentNumber,
            ["document_no"] = documentNumber,

            ["CustomerName"] = workOrder.CustomerName ?? "",
            ["customer_name"] = workOrder.CustomerName ?? "",
            ["Phone"] = workOrder.CustomerPhone ?? "",
            ["phone"] = workOrder.CustomerPhone ?? "",

            ["DeviceType"] = workOrder.DeviceType ?? "",
            ["device_type"] = workOrder.DeviceType ?? "",
            ["Brand"] = workOrder.Brand ?? "",
            ["brand"] = workOrder.Brand ?? "",
            ["Model"] = workOrder.Model ?? "",
            ["model"] = workOrder.Model ?? "",
            ["SerialNumber"] = workOrder.SerialNumber ?? "",
            ["serial"] = workOrder.SerialNumber ?? "",

            ["Accessories"] = workOrder.Accessories ?? "",
            ["accessories"] = workOrder.Accessories ?? "",
            ["Complaint"] = workOrder.Complaint ?? "",
            ["complaint"] = workOrder.Complaint ?? "",
            ["TechnicalReport"] = workOrder.TechnicalReport ?? "",
            ["technical_report"] = workOrder.TechnicalReport ?? "",
            ["Status"] = workOrder.Status ?? "",
            ["status"] = workOrder.Status ?? "",

            ["PartsCost"] = workOrder.PartsCost.ToString("0.##"),
            ["parts_cost"] = workOrder.PartsCost.ToString("0.##"),
            ["LaborCost"] = workOrder.LaborCost.ToString("0.##"),
            ["labor_cost"] = workOrder.LaborCost.ToString("0.##"),
            ["TotalCost"] = workOrder.TotalCost.ToString("0.##"),
            ["total_cost"] = workOrder.TotalCost.ToString("0.##"),

            ["IssueDate"] = workOrder.ReceivedDate.ToString("yyyy-MM-dd"),
            ["issue_date"] = workOrder.ReceivedDate.ToString("yyyy-MM-dd"),
            ["received_date"] = workOrder.ReceivedDate.ToString("yyyy-MM-dd"),
            ["updated_date"] = workOrder.UpdatedDate.ToString("yyyy-MM-dd"),
            ["report_type"] = reportType,
            ["company_logo_img"] = "",
            ["qr_code_img"] = ""
        };
    }

    public async Task<string> GenerateRentalReceiptDocxAndPdfFromPathAsync(int rentalId, string templateName, string templatePath)
    {
        var rental = await _db.Rentals.FindAsync(rentalId);
        if (rental == null)
            throw new InvalidOperationException($"Rental with ID {rentalId} not found");

        var data = BuildRentalTokenData(rental);

        var docxPath = await Task.Run(() => _wordEngine.GenerateDocumentFromPath(templatePath, data));
        var pdfPath = Path.ChangeExtension(docxPath, ".pdf");

        await Task.Run(() =>
        {
            Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(595, 842);
                    page.Margin(2, Unit.Centimetre);
                    page.DefaultTextStyle(x => x.FontSize(12));

                    page.Header().Element(c => c.AlignCenter().Text("إيصال استلام جهاز إيجار").Bold().FontSize(20));
                    page.Content().Element(c => GenerateRentalPdfContent(c, rental));
                    page.Footer().AlignCenter().Text(x =>
                    {
                        x.Span("Page ");
                        x.CurrentPageNumber();
                    });
                });
            }).GeneratePdf(pdfPath);
        });

        return docxPath;
    }

    private static void GenerateRentalPdfContent(IContainer container, Rental rental)
    {
        container.Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.ConstantColumn(150);
                columns.RelativeColumn();
            });

            table.Cell().Element(c => c.Padding(5)).Text("رقم الإيصال:").Bold();
            table.Cell().Element(c => c.Padding(5)).Text(rental.RentalNumber);
            
            table.Cell().Element(c => c.Padding(5)).Text("اسم العميل:").Bold();
            table.Cell().Element(c => c.Padding(5)).Text(rental.CustomerName);
            
            table.Cell().Element(c => c.Padding(5)).Text("الشركة:").Bold();
            table.Cell().Element(c => c.Padding(5)).Text(rental.Company ?? "-");
            
            table.Cell().Element(c => c.Padding(5)).Text("رقم الهاتف:").Bold();
            table.Cell().Element(c => c.Padding(5)).Text(rental.Phone ?? "-");
            
            table.Cell().Element(c => c.Padding(5)).Text("نوع الجهاز:").Bold();
            table.Cell().Element(c => c.Padding(5)).Text(rental.DeviceType);
            
            table.Cell().Element(c => c.Padding(5)).Text("الماركة:").Bold();
            table.Cell().Element(c => c.Padding(5)).Text(rental.Brand ?? "-");
            
            table.Cell().Element(c => c.Padding(5)).Text("الموديل:").Bold();
            table.Cell().Element(c => c.Padding(5)).Text(rental.Model ?? "-");
            
            table.Cell().Element(c => c.Padding(5)).Text("السيريال:").Bold();
            table.Cell().Element(c => c.Padding(5)).Text(rental.Serial);
            
            table.Cell().Element(c => c.Padding(5)).Text("تاريخ البدء:").Bold();
            table.Cell().Element(c => c.Padding(5)).Text(rental.StartDate.ToString("yyyy-MM-dd"));
            
            table.Cell().Element(c => c.Padding(5)).Text("تاريخ الانتهاء:").Bold();
            table.Cell().Element(c => c.Padding(5)).Text(rental.EndDate.ToString("yyyy-MM-dd"));
            
            table.Cell().Element(c => c.Padding(5)).Text("نوع الإيجار:").Bold();
            table.Cell().Element(c => c.Padding(5)).Text(rental.RentalType);
            
            table.Cell().Element(c => c.Padding(5)).Text("السعر:").Bold();
            table.Cell().Element(c => c.Padding(5)).Text(rental.Price.ToString("0.##"));
            
            table.Cell().Element(c => c.Padding(5)).Text("المبلغ المدفوع:").Bold();
            table.Cell().Element(c => c.Padding(5)).Text(rental.PaidAmount.ToString("0.##"));
            
            table.Cell().Element(c => c.Padding(5)).Text("المبلغ المتبقي:").Bold();
            table.Cell().Element(c => c.Padding(5)).Text(rental.RemainingAmount.ToString("0.##"));
            
            table.Cell().Element(c => c.Padding(5)).Text("الحالة:").Bold();
            table.Cell().Element(c => c.Padding(5)).Text(rental.Status);
            
            table.Cell().Element(c => c.Padding(5)).Text("ملاحظات:").Bold();
            table.Cell().Element(c => c.Padding(5)).Text(rental.Notes ?? "-");
        });
    }

    private static Dictionary<string, object> BuildRentalTokenData(Rental rental)
    {
        return new Dictionary<string, object>
        {
            ["RentalNumber"] = rental.RentalNumber,
            ["rental_number"] = rental.RentalNumber,
            ["ReceiptNumber"] = rental.RentalNumber,
            ["receipt_number"] = rental.RentalNumber,

            ["CustomerName"] = rental.CustomerName,
            ["customer_name"] = rental.CustomerName,
            ["ClientName"] = rental.CustomerName,
            ["client_name"] = rental.CustomerName,

            ["Company"] = rental.Company ?? "",
            ["company"] = rental.Company ?? "",

            ["Phone"] = rental.Phone ?? "",
            ["phone"] = rental.Phone ?? "",

            ["DeviceType"] = rental.DeviceType,
            ["device_type"] = rental.DeviceType,

            ["Brand"] = rental.Brand ?? "",
            ["brand"] = rental.Brand ?? "",

            ["Model"] = rental.Model ?? "",
            ["model"] = rental.Model ?? "",

            ["Serial"] = rental.Serial,
            ["serial"] = rental.Serial,
            ["SerialNumber"] = rental.Serial,
            ["serial_number"] = rental.Serial,

            ["Serial2"] = rental.Serial2 ?? "",
            ["serial2"] = rental.Serial2 ?? "",
            ["SerialNumber2"] = rental.Serial2 ?? "",
            ["serial_number_2"] = rental.Serial2 ?? "",

            ["StartDate"] = rental.StartDate.ToString("yyyy-MM-dd"),
            ["start_date"] = rental.StartDate.ToString("yyyy-MM-dd"),

            ["EndDate"] = rental.EndDate.ToString("yyyy-MM-dd"),
            ["end_date"] = rental.EndDate.ToString("yyyy-MM-dd"),

            ["RentalType"] = rental.RentalType,
            ["rental_type"] = rental.RentalType,

            ["Price"] = rental.Price.ToString("0.##"),
            ["price"] = rental.Price.ToString("0.##"),

            ["PaidAmount"] = rental.PaidAmount.ToString("0.##"),
            ["paid_amount"] = rental.PaidAmount.ToString("0.##"),

            ["RemainingAmount"] = rental.RemainingAmount.ToString("0.##"),
            ["remaining_amount"] = rental.RemainingAmount.ToString("0.##"),

            ["Status"] = rental.Status,
            ["status"] = rental.Status,

            ["Notes"] = rental.Notes ?? "",
            ["notes"] = rental.Notes ?? "",

            ["IssueDate"] = rental.CreatedAt.ToString("yyyy-MM-dd"),
            ["issue_date"] = rental.CreatedAt.ToString("yyyy-MM-dd"),
            ["created_date"] = rental.CreatedAt.ToString("yyyy-MM-dd"),

            ["company_logo_img"] = "",
            ["qr_code_img"] = ""
        };
    }
}
