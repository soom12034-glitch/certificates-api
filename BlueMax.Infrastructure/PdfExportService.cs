using System;
using System.IO;
using System.Runtime.InteropServices;
using Word = Microsoft.Office.Interop.Word;

namespace BlueMax.Infrastructure;

public sealed class PdfExportService
{
    public string ExportToPdfA(string docxPath, string outputPdfPath)
    {
        if (string.IsNullOrWhiteSpace(docxPath) || !File.Exists(docxPath))
            throw new FileNotFoundException("DOCX file not found", docxPath);

        var outputDir = Path.GetDirectoryName(outputPdfPath);
        if (!string.IsNullOrWhiteSpace(outputDir))
            Directory.CreateDirectory(outputDir);

        // Quick check to provide a friendly message if Word is not installed
        if (Type.GetTypeFromProgID("Word.Application") == null)
            throw new InvalidOperationException("Microsoft Word is required to export PDF. Please install Word, then try again.");

        Word.Application? app = null;
        Word.Document? doc = null;
        try
        {
            app = new Word.Application { Visible = false, ScreenUpdating = false, DisplayAlerts = Word.WdAlertLevel.wdAlertsNone };
            object missing = Type.Missing;
            object fileName = docxPath;
            object readOnly = true;
            object isVisible = false;

            doc = app.Documents.Open(ref fileName, ReadOnly: ref readOnly, Visible: ref isVisible);

            object pdfPathObj = outputPdfPath;
            try
            {
                doc.ExportAsFixedFormat(
                    OutputFileName: outputPdfPath,
                    ExportFormat: Word.WdExportFormat.wdExportFormatPDF,
                    OpenAfterExport: false,
                    OptimizeFor: Word.WdExportOptimizeFor.wdExportOptimizeForPrint,
                    Range: Word.WdExportRange.wdExportAllDocument,
                    From: 0, To: 0,
                    Item: Word.WdExportItem.wdExportDocumentContent,
                    IncludeDocProps: true,
                    KeepIRM: false,
                    CreateBookmarks: Word.WdExportCreateBookmarks.wdExportCreateNoBookmarks,
                    DocStructureTags: true,
                    BitmapMissingFonts: true,
                    UseISO19005_1: true // PDF/A-1b
                );
            }
            catch (COMException)
            {
                throw new InvalidOperationException("تعذر تصدير PDF عبر Microsoft Word. يرجى التأكد أن Word مُثبّت ومفعّل ثم المحاولة مرة أخرى.");
            }
            catch (FileNotFoundException ex) when ((ex.FileName ?? ex.Message).IndexOf("office, Version=", StringComparison.OrdinalIgnoreCase) >= 0
                                                   || (ex.FileName ?? ex.Message).IndexOf("Microsoft.Office.Core", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                throw new InvalidOperationException(
                    "تعذر العثور على تجميعة 'office' (Microsoft.Office.Core). لحل المشكلة، افتح إعدادات Microsoft Office > تغيير > إضافة/إزالة ميزات > Office Shared Features ثم فعّل .NET Programmability Support لبرنامج Word (Run from My Computer). بعد ذلك أعد تشغيل البرنامج ثم جرّب التصدير مرة أخرى.");
            }

            return outputPdfPath;
        }
        finally
        {
            try
            {
                if (doc != null)
                {
                    doc.Close(SaveChanges: false);
                    Marshal.FinalReleaseComObject(doc);
                }
            }
            catch { }

            try
            {
                if (app != null)
                {
                    app.Quit(SaveChanges: false);
                    Marshal.FinalReleaseComObject(app);
                }
            }
            catch { }
        }
    }
}
