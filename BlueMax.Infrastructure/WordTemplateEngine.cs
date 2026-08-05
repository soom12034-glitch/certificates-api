using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Wp = DocumentFormat.OpenXml.Wordprocessing;
using DW = DocumentFormat.OpenXml.Drawing.Wordprocessing;
using A = DocumentFormat.OpenXml.Drawing;
using PIC = DocumentFormat.OpenXml.Drawing.Pictures;
using System.Text.RegularExpressions;

namespace BlueMax.Infrastructure;

public class WordTemplateEngine
{
    private static readonly Regex TokenRegex = new(@"\{\{\s*(?<key>[^}]+?)\s*\}\}", RegexOptions.CultureInvariant);
    public const string QrImageMarker = "__BLUEMAX_QR_IMAGE__";

    private readonly string _templatesDirectory;
    private readonly string? _db;

    public WordTemplateEngine(string templatesDirectory)
    {
        _templatesDirectory = templatesDirectory;
    }

    public WordTemplateEngine(string templatesDirectory, string db)
    {
        _templatesDirectory = templatesDirectory;
        _db = db;
    }

    public string GenerateDocument(string templateName, Dictionary<string, object> data)
    {
        var templatePath = Path.Combine(_templatesDirectory, templateName);
        return GenerateDocumentFromPath(templatePath, data);
    }

    public string GenerateDocumentFromPath(string templatePath, Dictionary<string, object> data)
    {
        if (!File.Exists(templatePath))
            throw new FileNotFoundException($"Template file not found: {templatePath}");

        var outputPath = Path.Combine(
            Path.GetTempPath(),
            $"BlueMax_{Guid.NewGuid()}.docx");

        File.Copy(templatePath, outputPath, true);

        using var doc = WordprocessingDocument.Open(outputPath, true);
        var mainPart = doc.MainDocumentPart ?? throw new InvalidOperationException("MainDocumentPart not found");
        var document = mainPart.Document ?? throw new InvalidOperationException("Document not found");
        var body = document.Body ?? throw new InvalidOperationException("Document body not found");
        var replacementState = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var qrImagePath = GetDataValue(data, "qr_image_path");

        ReplaceTokensInPart(body, data, replacementState);
        ReplaceQrMarkersInPart(body, mainPart, qrImagePath);

        foreach (var header in mainPart.HeaderParts)
        {
            if (header.Header != null)
            {
                ReplaceTokensInPart(header.Header, data, replacementState);
                ReplaceQrMarkersInPart(header.Header, mainPart, qrImagePath);
            }
        }

        foreach (var footer in mainPart.FooterParts)
        {
            if (footer.Footer != null)
            {
                ReplaceTokensInPart(footer.Footer, data, replacementState);
                ReplaceQrMarkersInPart(footer.Footer, mainPart, qrImagePath);
            }
        }

        doc.Save();

        return outputPath;
    }

    private static void ReplaceTokensInPart(OpenXmlElement partRoot, Dictionary<string, object> data, Dictionary<string, int> replacementState)
    {
        foreach (var paragraph in partRoot.Descendants<Paragraph>())
        {
            ReplaceTokensInParagraph(paragraph, data, replacementState);
        }

        foreach (var table in partRoot.Descendants<Table>())
        {
            ApplyTableTemplate(table, data);
        }
    }

    private static void ReplaceTokensInParagraph(Paragraph paragraph, Dictionary<string, object> data, Dictionary<string, int> replacementState)
    {
        foreach (var text in paragraph.Descendants<Text>())
        {
            text.Text = ReplaceTokens(text.Text, data, replacementState);
        }

        if (paragraph.InnerText.Contains("{{", StringComparison.Ordinal))
        {
            ReplaceSplitTokensInParagraph(paragraph, data, replacementState);
        }
    }

    private static void ReplaceSplitTokensInParagraph(Paragraph paragraph, Dictionary<string, object> data, Dictionary<string, int> replacementState)
    {
        var textNodes = paragraph.Descendants<Text>().ToList();
        if (textNodes.Count < 2)
            return;

        var starts = new int[textNodes.Count];
        var total = 0;
        for (var i = 0; i < textNodes.Count; i++)
        {
            starts[i] = total;
            total += textNodes[i].Text.Length;
        }

        if (total == 0)
            return;

        var combined = string.Concat(textNodes.Select(t => t.Text));
        var matches = TokenRegex.Matches(combined);
        if (matches.Count == 0)
            return;

        for (var m = matches.Count - 1; m >= 0; m--)
        {
            var match = matches[m];
            var key = match.Groups["key"].Value.Trim();
            var replacement = ResolveTokenValue(data, key, replacementState);
            var startPos = match.Index;
            var endPosExclusive = match.Index + match.Length;

            var startNode = FindNodeIndex(starts, textNodes, startPos);
            var endNode = FindNodeIndex(starts, textNodes, endPosExclusive - 1);
            if (startNode < 0 || endNode < 0)
                continue;

            var startOffset = startPos - starts[startNode];
            var endOffsetExclusive = endPosExclusive - starts[endNode];

            var startText = textNodes[startNode].Text;
            var endText = textNodes[endNode].Text;

            var prefix = startText.Substring(0, Math.Max(0, startOffset));
            var suffix = endText.Substring(Math.Min(endText.Length, Math.Max(0, endOffsetExclusive)));

            textNodes[startNode].Text = prefix + replacement + suffix;
            for (var i = startNode + 1; i <= endNode; i++)
            {
                textNodes[i].Text = string.Empty;
            }
        }
    }

    private static int FindNodeIndex(int[] starts, List<Text> nodes, int position)
    {
        for (var i = 0; i < nodes.Count; i++)
        {
            var start = starts[i];
            var end = start + nodes[i].Text.Length;
            if (position >= start && position < end)
                return i;
        }

        return -1;
    }

    private static void ReplaceQrMarkersInPart(OpenXmlElement partRoot, MainDocumentPart mainPart, string qrImagePath)
    {
        if (string.IsNullOrWhiteSpace(qrImagePath) || !File.Exists(qrImagePath))
            return;

        foreach (var text in partRoot.Descendants<Text>().ToList())
        {
            var value = text.Text ?? string.Empty;
            var markerIndex = value.IndexOf(QrImageMarker, StringComparison.Ordinal);
            if (markerIndex < 0)
                continue;

            if (text.Parent is not Run originalRun)
                continue;

            var before = value.Substring(0, markerIndex);
            var after = value.Substring(markerIndex + QrImageMarker.Length);

            text.Text = before;

            var imageRun = CreateImageRun(mainPart, qrImagePath, originalRun);
            originalRun.InsertAfterSelf(imageRun);

            if (!string.IsNullOrEmpty(after))
            {
                var afterRun = (Run)originalRun.CloneNode(true);
                afterRun.RemoveAllChildren<Text>();
                afterRun.AppendChild(new Text(after) { Space = SpaceProcessingModeValues.Preserve });
                imageRun.InsertAfterSelf(afterRun);
            }
        }
    }

    private static Run CreateImageRun(MainDocumentPart mainPart, string imagePath, Run templateRun)
    {
        var imagePartType = Path.GetExtension(imagePath).Equals(".png", StringComparison.OrdinalIgnoreCase)
            ? ImagePartType.Png
            : ImagePartType.Jpeg;

        var imagePart = mainPart.AddImagePart(imagePartType);
        using (var stream = File.OpenRead(imagePath))
            imagePart.FeedData(stream);

        var inline = CreateImageElement(mainPart.GetIdOfPart(imagePart));
        var drawing = new Drawing(inline);
        var run = new Run();
        if (templateRun.RunProperties != null)
            run.RunProperties = (RunProperties)templateRun.RunProperties.CloneNode(true);
        run.AppendChild(drawing);
        return run;
    }

    private static void ApplyTableTemplate(Table table, Dictionary<string, object> data)
    {
        // Check if table has a template marker
        foreach (var row in table.Elements<TableRow>().ToList())
        {
            var firstCell = row.Elements<TableCell>().FirstOrDefault();
            if (firstCell == null) continue;

            var text = firstCell.InnerText;
            if (text.StartsWith("TEMPLATE:"))
            {
                var templateName = text.Substring("TEMPLATE:".Length).Trim();
                if (data.TryGetValue(templateName, out var value) && value is System.Collections.IEnumerable items)
                {
                    // Remove template row
                    row.Remove();

                    // Add data rows
                    foreach (var item in items)
                    {
                        if (item is Dictionary<string, object> itemData)
                        {
                            var newRow = CloneTableRow(row, itemData);
                            table.AppendChild(newRow);
                        }
                    }
                }
            }
        }
    }

    private static TableRow CloneTableRow(TableRow templateRow, Dictionary<string, object> data)
    {
        var newRow = (TableRow)templateRow.CloneNode(true);

        foreach (var cell in newRow.Elements<TableCell>())
        {
            foreach (var paragraph in cell.Elements<Paragraph>())
            {
                var text = paragraph.InnerText;
                var newText = text;

                foreach (var kvp in data)
                {
                    var token = $"{{{{{kvp.Key}}}}}";
                    newText = newText.Replace(token, kvp.Value?.ToString() ?? "");
                }

                if (newText != text)
                {
                    paragraph.RemoveAllChildren<Run>();
                    paragraph.AppendChild(new Run(new Text(newText)));
                }
            }
        }

        return newRow;
    }

    private static string ReplaceTokens(string text, Dictionary<string, object> data, Dictionary<string, int> replacementState)
    {
        return TokenRegex.Replace(text, match =>
        {
            var key = match.Groups["key"].Value.Trim();
            return ResolveTokenValue(data, key, replacementState);
        });
    }

    private static string ResolveTokenValue(Dictionary<string, object> data, string key, Dictionary<string, int> replacementState)
    {
        var count = replacementState.TryGetValue(key, out var existing) ? existing : 0;
        replacementState[key] = count + 1;

        if (IsPrimarySerialAlias(key))
        {
            var groupKey = "__gps_primary_serial_group__";
            var groupCount = replacementState.TryGetValue(groupKey, out var g) ? g : 0;
            replacementState[groupKey] = groupCount + 1;

            var baseSerial = GetDataValue(data, "sn");
            if (string.IsNullOrWhiteSpace(baseSerial))
                baseSerial = GetDataValue(data, "serial");

            var rover = GetDataValue(data, "sn_rover");
            if (string.IsNullOrWhiteSpace(rover))
                rover = GetDataValue(data, "serial2");

            if (groupCount > 0 && !string.IsNullOrWhiteSpace(rover))
                return rover;

            if (!string.IsNullOrWhiteSpace(baseSerial))
                return baseSerial;

            if (!string.IsNullOrWhiteSpace(rover))
                return rover;

            return string.Empty;
        }

        return GetDataValue(data, key);
    }

    private static bool IsPrimarySerialAlias(string key)
    {
        return string.Equals(key, "sn", StringComparison.OrdinalIgnoreCase)
            || string.Equals(key, "serial", StringComparison.OrdinalIgnoreCase)
            || string.Equals(key, "serial_base", StringComparison.OrdinalIgnoreCase)
            || string.Equals(key, "sn_base", StringComparison.OrdinalIgnoreCase);
    }

    private static string GetDataValue(Dictionary<string, object> data, string key)
    {
        foreach (var kvp in data)
        {
            if (string.Equals(kvp.Key, key, StringComparison.OrdinalIgnoreCase))
                return kvp.Value?.ToString() ?? string.Empty;
        }

        return string.Empty;
    }

    public void InsertImage(string templatePath, string outputPath, string imagePath, string bookmarkName)
    {
        if (!File.Exists(templatePath))
            throw new FileNotFoundException($"Template file not found: {templatePath}");

        File.Copy(templatePath, outputPath, true);

        using var doc = WordprocessingDocument.Open(outputPath, true);
        var mainPart = doc.MainDocumentPart ?? throw new InvalidOperationException("MainDocumentPart not found");
        var document = mainPart.Document ?? throw new InvalidOperationException("Document not found");
        var body = document.Body ?? throw new InvalidOperationException("Document body not found");

        var bookmark = body.Descendants<BookmarkStart>()
            .FirstOrDefault(b => b.Name == bookmarkName);

        if (bookmark == null)
            throw new InvalidOperationException($"Bookmark '{bookmarkName}' not found");

        var imagePart = mainPart.AddImagePart(DetectImagePartType(imagePath));
        using var stream = File.OpenRead(imagePath);
        imagePart.FeedData(stream);

        var imageElement = CreateImageElement(mainPart.GetIdOfPart(imagePart));

        var paragraph = new Paragraph(imageElement);
        var parent = bookmark.Parent;
        if (parent != null)
        {
            parent.InsertAfter(paragraph, bookmark);
        }

        doc.Save();
    }

    private static DocumentFormat.OpenXml.Packaging.PartTypeInfo DetectImagePartType(string imagePath)
    {
        var ext = Path.GetExtension(imagePath).ToLowerInvariant();
        return ext switch
        {
            ".png" => ImagePartType.Png,
            ".gif" => ImagePartType.Gif,
            ".bmp" => ImagePartType.Bmp,
            ".tiff" or ".tif" => ImagePartType.Tiff,
            ".emf" => ImagePartType.Emf,
            _ => ImagePartType.Jpeg
        };
    }

    private static DW.Inline CreateImageElement(string relationshipId)
    {
        var img = new DW.Inline
        {
            DistanceFromTop = 0,
            DistanceFromBottom = 0,
            DistanceFromLeft = 0,
            DistanceFromRight = 0,
            EditId = "50D07946"
        };

        const long qrSizeEmu = 1380000L;
        var inlineExtent = new DW.Extent { Cx = qrSizeEmu, Cy = qrSizeEmu };
        img.Append(inlineExtent);

        var effectExtent = new DW.EffectExtent { LeftEdge = 0L, TopEdge = 0L, RightEdge = 0L, BottomEdge = 0L };
        img.Append(effectExtent);

        var docProperties = new DW.DocProperties
        {
            Id = 1,
            Name = "Picture"
        };
        img.Append(docProperties);

        // NonVisualGraphicFrameProperties is not available in this version, skipping

        var graphic = new A.Graphic(
            new A.GraphicData(
                new PIC.Picture(
                    new PIC.NonVisualPictureProperties(
                        new PIC.NonVisualDrawingProperties
                        {
                            Id = 0,
                            Name = "New Bitmap Image.jpg"
                        },
                        new PIC.NonVisualPictureDrawingProperties()),
                    new PIC.BlipFill(
                        new A.Blip { Embed = relationshipId },
                        new A.Stretch(
                            new A.FillRectangle())),
                    new PIC.ShapeProperties(
                        new A.Transform2D(
                            new A.Offset { X = 0, Y = 0 },
                            new A.Extents { Cx = qrSizeEmu, Cy = qrSizeEmu }),
                        new A.PresetGeometry { Preset = A.ShapeTypeValues.Rectangle }))
                ) { Uri = "http://schemas.openxmlformats.org/drawingml/2006/picture" });

        img.Append(graphic);

        return img;
    }
}
