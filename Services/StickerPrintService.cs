using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing; // Added for StringAlignment
using System.Drawing.Imaging; // Added for ImageToTsplPcx
using System.Drawing.Drawing2D;
using ZXing;
using ZXing.Common;
using ZXing.Windows.Compatibility;
using BlueMax.Infrastructure;
using BlueMax.Presentation.Wpf.ViewModels;

namespace BlueMax.Presentation.Wpf.Services
{
    public static class RawPrintEngine
    {
        public static string ZplHexEncodeUtf8(string? input)
        {
            if (string.IsNullOrEmpty(input))
                return string.Empty;

            var bytes = Encoding.UTF8.GetBytes(input);
            var sb = new StringBuilder();
            foreach (var b in bytes)
            {
                sb.AppendFormat("_{0:X2}", b);
            }
            return sb.ToString();
        }
    }

    public class StickerPrintService
    {
        public async Task PrintStickerAsync(StickerDesignerViewModel viewModel, PrinterSettings printerSettings)
        {
            if (string.Equals(printerSettings.Protocol, "Windows", StringComparison.OrdinalIgnoreCase))
            {
                await PrintStickerWithWindowsDriverAsync(viewModel, printerSettings);
            }
            else if (string.Equals(printerSettings.Protocol, "TSPL", StringComparison.OrdinalIgnoreCase))
            {
                var tspl = GenerateTspl(viewModel, printerSettings);
                // TODO: Implement TSPL printing
            }
            else
            {
                var zpl = GenerateZpl(viewModel, printerSettings);
                // TODO: Implement ZPL printing
            }
        }

        async Task PrintStickerWithWindowsDriverAsync(StickerDesignerViewModel viewModel, PrinterSettings settings)
        {
            var printerName = settings.PrinterName ?? "";
            if (string.IsNullOrWhiteSpace(printerName))
                throw new InvalidOperationException("Printer name is required.");

            var widthMm = Math.Max(1, viewModel.StickerWidthMm);
            var heightMm = Math.Max(1, viewModel.StickerHeightMm);
            var paperWidth = (int)Math.Round(widthMm / 25.4 * 100);
            var paperHeight = (int)Math.Round(heightMm / 25.4 * 100);

            await Task.Run(() =>
            {
                using var doc = new System.Drawing.Printing.PrintDocument();
                doc.PrinterSettings.PrinterName = printerName;
                doc.DefaultPageSettings.PaperSize = new System.Drawing.Printing.PaperSize("Sticker", paperWidth, paperHeight);
                doc.DefaultPageSettings.Margins = new System.Drawing.Printing.Margins(0, 0, 0, 0);
                doc.PrintController = new System.Drawing.Printing.StandardPrintController();

                doc.PrintPage += (_, e) =>
                {
                    var g = e.Graphics!;
                    g.PageUnit = GraphicsUnit.Millimeter;
                    g.SmoothingMode = SmoothingMode.HighQuality;
                    g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

                    const double PixelToMm = 0.264583;
                    var bodyFontSize = ResolveBodyFontSize(viewModel);
                    foreach (var item in viewModel.StickerItems.Where(x => x.IsVisible))
                    {
                        var xMm = (float)(item.X * PixelToMm);
                        var yMm = (float)(item.Y * PixelToMm);
                        var wMm = (float)Math.Max(1, item.Width * PixelToMm);
                        var hMm = (float)Math.Max(1, item.Height * PixelToMm);

                        string content = "";
                        var useBodyFont = false;
                        switch (item.Key.ToLowerInvariant())
                        {
                            case "logo":
                                if (!string.IsNullOrWhiteSpace(viewModel.LogoPath) && System.IO.File.Exists(viewModel.LogoPath))
                                {
                                    using var img = Image.FromFile(viewModel.LogoPath);
                                    g.DrawImage(img, xMm, yMm, wMm, hMm);
                                }
                                continue;
                            case "company": content = viewModel.CompanyName; useBodyFont = true; break;
                            case "header": content = viewModel.CompanyHeader; useBodyFont = true; break;
                            case "address": content = viewModel.CompanyAddress; useBodyFont = true; break;
                            case "phone": content = string.IsNullOrWhiteSpace(viewModel.CompanyPhone) ? "" : $"Tel: {viewModel.CompanyPhone}"; useBodyFont = true; break;
                            case "qr":
                                var qrText = viewModel.FindItem("qr")?.DisplayText ?? "";
                                if (!string.IsNullOrWhiteSpace(qrText))
                                {
                                    using var qr = BuildQrBitmap(qrText);
                                    var qrScale = 0.8f;
                                    var qrW = wMm * qrScale;
                                    var qrH = hMm * qrScale;
                                    var qrX = xMm + (wMm - qrW) / 2f;
                                    var qrY = yMm + (hMm - qrH) / 2f;
                                    g.DrawImage(qr, qrX, qrY, qrW, qrH);
                                }
                                continue;
                            case "brand_label":
                            case "model_label":
                            case "serial_label":
                            case "cert_label":
                            case "date_label":
                            case "exp_label":
                                content = item.DisplayText;
                                break;
                            case "brand_value": content = viewModel.Brand; break;
                            case "model_value": content = viewModel.Model; break;
                            case "serial_value": content = viewModel.Serial; break;
                            case "cert_value": content = viewModel.CertificateNumber; break;
                            case "date_value": content = viewModel.CalDate.ToString("dd-MM-yyyy"); break;
                            case "exp_value": content = viewModel.ExpDate.ToString("dd-MM-yyyy"); break;
                            default:
                                content = item.DisplayText;
                                break;
                        }

                        if (string.IsNullOrWhiteSpace(content)) continue;

                        var effectiveFontSize = useBodyFont ? bodyFontSize : item.FontSize;
                        var fontSizePt = Math.Max(6, effectiveFontSize * 0.75);
                        var fittedSizePt = useBodyFont
                            ? FitFontSize(g, content, wMm, fontSizePt)
                            : fontSizePt;
                        using var font = new Font("Arial", (float)fittedSizePt, FontStyle.Regular);
                        using var brush = new SolidBrush(Color.Black);
                        var format = new StringFormat
                        {
                            Alignment = StringAlignment.Near,
                            LineAlignment = StringAlignment.Near,
                            FormatFlags = StringFormatFlags.DirectionRightToLeft
                        };
                        g.DrawString(content, font, brush, new RectangleF(xMm, yMm, wMm, hMm), format);
                    }
                    e.HasMorePages = false;
                };

                doc.Print();
            });
        }

        static Bitmap BuildQrBitmap(string content)
        {
            var writer = new BarcodeWriter
            {
                Format = BarcodeFormat.QR_CODE,
                Renderer = new BitmapRenderer(),
                Options = new EncodingOptions
                {
                    Height = 300,
                    Width = 300,
                    Margin = 0
                }
            };
            return writer.Write(content);
        }

        static double ResolveBodyFontSize(StickerDesignerViewModel viewModel)
        {
            var keys = new[]
            {
                "brand_value",
                "model_value",
                "serial_value",
                "cert_value",
                "date_value",
                "exp_value"
            };
            foreach (var key in keys)
            {
                var item = viewModel.FindItem(key);
                if (item != null && item.FontSize > 0)
                    return item.FontSize;
            }
            var fallback = viewModel.StickerItems.FirstOrDefault(x => x.IsVisible);
            return fallback?.FontSize ?? 12;
        }

        static double FitFontSize(Graphics g, string text, float maxWidthMm, double baseSizePt)
        {
            if (string.IsNullOrWhiteSpace(text))
                return baseSizePt;
            var size = baseSizePt;
            for (int i = 0; i < 10; i++)
            {
                using var f = new Font("Arial", (float)size, FontStyle.Regular);
                var format = new StringFormat(StringFormatFlags.NoWrap)
                {
                    Alignment = StringAlignment.Near,
                    LineAlignment = StringAlignment.Near,
                    FormatFlags = StringFormatFlags.DirectionRightToLeft | StringFormatFlags.NoWrap
                };
                var measured = g.MeasureString(text, f, new SizeF(maxWidthMm, 1000f), format);
                if (measured.Width <= maxWidthMm)
                    return size;
                size = Math.Max(6, size * 0.9);
            }
            return Math.Max(6, size);
        }

        public string GenerateTspl(StickerDesignerViewModel viewModel, PrinterSettings settings)
        {
            var dpmm = settings.DotsPerMm <= 0 ? 8 : settings.DotsPerMm;
            var widthMm = Math.Max(1, viewModel.StickerWidthMm);
            var heightMm = Math.Max(1, viewModel.StickerHeightMm);
            var widthDots = (int)Math.Round((double)widthMm * dpmm);
            var heightDots = (int)Math.Round((double)heightMm * dpmm);

            const double PixelToMm = 0.264583;
            int ToDots(double px) => (int)Math.Round(px * PixelToMm * dpmm);
            static string TsplEscape(string s) => (s ?? "").Replace("\"", "\\\"");

            var commands = new List<string>();
            // Setup label size and clear buffer
            commands.Add($"SIZE {widthMm} mm,{heightMm} mm");
            commands.Add($"GAP {Math.Max(0, settings.TsplGapMm)} mm,{Math.Max(0, settings.TsplGapOffsetMm)} mm");
            commands.Add($"DIRECTION {settings.TsplDirection}");
            if (settings.TsplReferenceX != 0 || settings.TsplReferenceY != 0)
                commands.Add($"REFERENCE {settings.TsplReferenceX},{settings.TsplReferenceY}");
            if (!string.IsNullOrWhiteSpace(settings.TsplCodepage))
                commands.Add($"CODEPAGE {settings.TsplCodepage}");
            commands.Add("CLS");

            foreach (var item in viewModel.StickerItems.Where(x => x.IsVisible))
            {
                var x = ToDots(item.X);
                var y = ToDots(item.Y);
                var h = Math.Max(10, ToDots(item.FontSize));
                
                string content = "";
                switch (item.Key.ToLowerInvariant())
                {
                    case "logo":
                        if (!string.IsNullOrWhiteSpace(viewModel.LogoPath))
                        {
                            commands.Add(ImageToTsplPcx(viewModel.LogoPath, x, y, ToDots(item.Width), ToDots(item.Height)));
                        }
                        continue;
                    case "company": content = viewModel.CompanyName; break;
                    case "header": content = viewModel.CompanyHeader; break;
                    case "address": content = viewModel.CompanyAddress; break;
                    case "phone": content = string.IsNullOrWhiteSpace(viewModel.CompanyPhone) ? "" : $"Tel: {viewModel.CompanyPhone}"; break;
                    case "qr":
                        // QRCODE x,y,ECC,cellwidth,mode,rotation,"content"
                        // cellwidth 1-10
                        var qrH = Math.Max(8, (int)Math.Round(h * 0.8));
                        int cellWidth = Math.Max(1, Math.Min(10, qrH / 20));
                        commands.Add($"QRCODE {x},{y},L,{cellWidth},A,0,\"{TsplEscape(viewModel.FindItem("qr")?.DisplayText ?? string.Empty)}\"");
                        continue;

                    case "brand_label":
                    case "model_label":
                    case "serial_label":
                    case "cert_label":
                    case "date_label":
                    case "exp_label":
                        content = item.DisplayText;
                        break;

                    case "brand_value": content = viewModel.Brand; break;
                    case "model_value": content = viewModel.Model; break;
                    case "serial_value": content = viewModel.Serial; break;
                    case "cert_value": content = viewModel.CertificateNumber; break;
                    case "date_value": content = viewModel.CalDate.ToString("dd-MM-yyyy"); break;
                    case "exp_value": content = viewModel.ExpDate.ToString("dd-MM-yyyy"); break;

                    default:
                        content = item.DisplayText;
                        break;
                }

                if (string.IsNullOrWhiteSpace(content)) continue;

                // TEXT x,y,"font",rotation,x-mul,y-mul,"content"
                // Scale based on printer resolution: base char height ~ 3mm at given dpmm
                int baseCharHeight = Math.Max(8, (int)Math.Round(dpmm * 3.0));
                int mul = Math.Max(1, Math.Min(10, (int)Math.Round(h / (double)baseCharHeight)));
                
                commands.Add($"TEXT {x},{y},\"0\",0,{mul},{mul},\"{TsplEscape(content)}\"");
            }

            commands.Add($"PRINT {Math.Max(1, settings.TsplCopies)},1");
            return string.Join("\n", commands);
        }

        public string GenerateZpl(StickerDesignerViewModel viewModel, PrinterSettings settings)
        {
            var dpmm = settings.DotsPerMm <= 0 ? 8 : settings.DotsPerMm;
            var widthMm = Math.Max(1, viewModel.StickerWidthMm);
            var heightMm = Math.Max(1, viewModel.StickerHeightMm);
            var widthDots = (int)Math.Round((double)widthMm * dpmm);
            var heightDots = (int)Math.Round((double)heightMm * dpmm);

            // Designer uses 0.264583 mm per pixel (96 DPI screen pixels)
            const double PixelToMm = 0.264583;
            
            // Helper to convert Screen Pixels (from Designer) to Printer Dots
            int ToDots(double px) => (int)Math.Round(px * PixelToMm * dpmm);

            var zplCommands = new List<string>();
            zplCommands.Add("^CI28"); // UTF-8 encoding
            zplCommands.Add("^CF0,28"); // Default font

            foreach (var item in viewModel.StickerItems.Where(x => x.IsVisible))
            {
                var x = ToDots(item.X);
                var y = ToDots(item.Y);
                var h = Math.Max(10, ToDots(item.FontSize));
                var w = h; 

                string content = "";
                bool isImage = false;
                bool isBold = false;

                switch (item.Key.ToLowerInvariant())
                {
                    case "logo":
                        if (!string.IsNullOrWhiteSpace(viewModel.LogoPath))
                        {
                            // For ZPL, we use StickerImageHelper to convert the image to ZPL graphic commands
                            zplCommands.Add(StickerImageHelper.GenerateImageAsZplGraphic(viewModel.LogoPath, x, y, ToDots(item.Width), ToDots(item.Height)));
                        }
                        continue;
                    case "company":
                        content = viewModel.CompanyName;
                        isImage = true;
                        isBold = true;
                        break;
                    case "header":
                        content = viewModel.CompanyHeader;
                        isImage = true;
                        break;
                    case "address":
                        content = viewModel.CompanyAddress;
                        isImage = true;
                        break;
                    case "phone":
                        content = string.IsNullOrWhiteSpace(viewModel.CompanyPhone) ? "" : $"Tel: {viewModel.CompanyPhone}";
                        isImage = true;
                        break;
                    case "qr":
                        var qrH = Math.Max(8, (int)Math.Round(h * 0.8));
                        int scale = Math.Max(2, Math.Min(10, qrH / 20));
                        var encQr = RawPrintEngine.ZplHexEncodeUtf8(viewModel.FindItem("qr")?.DisplayText);
                        zplCommands.Add($"^FO{x},{y}^BQN,2,{scale}^FH\\^FD{encQr}^FS");
                        continue;

                    case "brand_label":
                    case "model_label":
                    case "serial_label":
                    case "cert_label":
                    case "date_label":
                    case "exp_label":
                        content = item.DisplayText;
                        break;

                    case "brand_value": content = viewModel.Brand; break;
                    case "model_value": content = viewModel.Model; break;
                    case "serial_value": content = viewModel.Serial; break;
                    case "cert_value": content = viewModel.CertificateNumber; break;
                    case "date_value": content = viewModel.CalDate.ToString("dd-MM-yyyy"); break;
                    case "exp_value": content = viewModel.ExpDate.ToString("dd-MM-yyyy"); break;
                    
                    default:
                        content = item.DisplayText;
                        break;
                }

                if (string.IsNullOrWhiteSpace(content)) continue;

                if (isImage)
                {
                     // Use StickerImageHelper with Far alignment (Left in RTL context)
                     // Pass widthDots as max width, and x as position.
                     // The helper creates a bitmap of size widthDots x heightDots.
                     // With Far alignment (Left), the text starts at 0 inside the bitmap.
                     // We place the bitmap at x,y.
                     // So text starts at x.
                     // Wait, if bitmap width is widthDots, and we place at x, and text is at 0 (Left of bitmap).
                     // Then text is at x.
                     // BUT, the bitmap extends to x + widthDots.
                     // If x > 0, x + widthDots > widthDots.
                     // The printer might clip or wrap or error if image goes out of bounds.
                     // Ideally, bitmap width should be widthDots - x.
                     // Or just enough for the text.
                     // But calculating text width requires Graphics.MeasureString.
                     // Let's try passing widthDots - x.
                     
                     int availableWidth = Math.Max(10, widthDots - x);
                     var alignment = (item.Key is "company" or "header" or "address" or "phone") ? StringAlignment.Center : StringAlignment.Far;
                     var imgZpl = StickerImageHelper.GenerateTextAsZplImage(content, availableWidth, x, y, h, (int)item.FontSize, isBold, alignment);
                     zplCommands.Add(imgZpl);
                }
                else
                {
                    // Standard ZPL Text
                    var enc = RawPrintEngine.ZplHexEncodeUtf8(content);
                    zplCommands.Add($"^FO{x},{y}^A0N,{h},{w}^FH\\^FD{enc}^FS");
                }
            }

            var body = string.Join("\n", zplCommands);
            var header = $"~SD{settings.Darkness}\n^XA^PW{widthDots}^LL{heightDots}^PR{settings.SpeedIps}\n";
            return $"{header}{body}\n^XZ";
        }

        // Helper to convert an image to PCX format for TSPL
        private string ConvertToMonochromeAndGenerateTsplGraphic(System.Drawing.Image image, int x, int y)
        {
            Bitmap? monoBitmap = null;
            System.Drawing.Imaging.BitmapData? bmpData = null;

            try
            {
                monoBitmap = new Bitmap(image.Width, image.Height, PixelFormat.Format1bppIndexed);
                using (var g = Graphics.FromImage(monoBitmap))
                {
                    g.DrawImage(image, 0, 0, image.Width, image.Height);
                }

                bmpData = monoBitmap.LockBits(new Rectangle(0, 0, monoBitmap.Width, monoBitmap.Height), ImageLockMode.ReadOnly, PixelFormat.Format1bppIndexed);
                int stride = bmpData.Stride;
                int byteCount = stride * bmpData.Height;
                byte[] pixels = new byte[byteCount];
                System.Runtime.InteropServices.Marshal.Copy(bmpData.Scan0, pixels, 0, byteCount);

                StringBuilder hexData = new StringBuilder();
                foreach (byte b in pixels)
                {
                    hexData.Append(b.ToString("X2"));
                }

                return $"GRAPHIC {x},{y},{monoBitmap.Width},{monoBitmap.Height},0,{hexData}";
            }
            finally
            {
                if (bmpData != null)
                {
                    monoBitmap?.UnlockBits(bmpData);
                }
                monoBitmap?.Dispose();
            }
        }

        private string ImageToTsplPcx(string imagePath, int x, int y, int newWidth, int newHeight)
        {
            if (!System.IO.File.Exists(imagePath))
            {
                return "";
            }

            try
            {
                using (System.Drawing.Image originalImage = System.Drawing.Image.FromFile(imagePath))
                {
                    // Calculate dimensions to maintain aspect ratio
                    double aspectRatio = (double)originalImage.Width / originalImage.Height;
                    int finalWidth = newWidth;
                    int finalHeight = newHeight;

                    if (newWidth / (double)newHeight > aspectRatio)
                    {
                        finalWidth = (int)(newHeight * aspectRatio);
                    }
                    else
                    {
                        finalHeight = (int)(newWidth / aspectRatio);
                    }

                    using (System.Drawing.Bitmap processedBitmap = new System.Drawing.Bitmap(finalWidth, finalHeight))
                    {
                        using (var graphics = System.Drawing.Graphics.FromImage(processedBitmap))
                        {
                            graphics.DrawImage(originalImage, 0, 0, finalWidth, finalHeight);
                        }
                        return ConvertToMonochromeAndGenerateTsplGraphic(processedBitmap, x, y);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error converting image to TSPL GRAPHIC: {ex.Message}");
                return "";
            }
        }
    }
}
