using System;
using BlueMax.Infrastructure;
using BlueMax.Presentation.Wpf.Services;

namespace BlueMax.Presentation.Wpf.ViewModels;

/// <summary>
/// Maintenance device receipt sticker designer.
/// It shares the EXACT same mechanism as the calibration certificate sticker designer
/// (canvas, layout items, logo, QR, preview, printer detection and print service), but
/// uses fully isolated storage (its own layout/template/printer-settings files) and its
/// own data fields. No variable, setting or layout is ever shared with the certificate sticker.
/// </summary>
public sealed class ReceiptStickerDesignerViewModel : StickerDesignerViewModel
{
    string _receiptNumber = "";
    string _customerName = "";
    string _customerPhone = "";
    DateTime _receiptDate = DateTime.MinValue;

    public ReceiptStickerDesignerViewModel() : base(StickerKind.Receipt)
    {
        _receiptDate = DateTime.Today;
        ApplyDataToItems();
        UpdateQrContent();
    }

    /// <summary>Isolated printer-settings file for the receipt sticker (never the certificate sticker's).</summary>
    protected override string PrinterSettingsFileName => PrinterSettingsStore.ReceiptFileName;

    public string ReceiptNumber
    {
        get => _receiptNumber;
        set
        {
            if (!SetProperty(ref _receiptNumber, StickerText.ToEnglishDigits(value ?? "")))
                return;
            UpdateItemText("receipt_number_value", _receiptNumber);
            UpdateQrContent();
        }
    }

    public string CustomerName
    {
        get => _customerName;
        set
        {
            if (!SetProperty(ref _customerName, StickerText.ToEnglishDigits(value ?? "")))
                return;
            UpdateItemText("customer_value", _customerName);
            UpdateQrContent();
        }
    }

    public string CustomerPhone
    {
        get => _customerPhone;
        set => SetProperty(ref _customerPhone, StickerText.ToEnglishDigits(value ?? ""));
    }

    public DateTime ReceiptDate
    {
        get => _receiptDate;
        set
        {
            if (!SetProperty(ref _receiptDate, value))
                return;
            if (_receiptDate > DateTime.MinValue)
                UpdateItemText("date_value", StickerText.FormatDate(_receiptDate));
            UpdateQrContent();
        }
    }

    /// <summary>
    /// Pushes the current maintenance work-order / device data onto this designer so the
    /// receipt sticker previews and prints EXACTLY the layout the user built for the
    /// receipt sticker (no certificate sticker data is ever involved).
    /// </summary>
    public void ApplyReceiptData(string receiptNumber, string customerName, string customerPhone, string brand, string model, string serial, DateTime date)
    {
        ReceiptNumber = receiptNumber;
        CustomerName = customerName;
        CustomerPhone = customerPhone;
        Brand = brand;
        Model = model;
        Serial = serial;
        ReceiptDate = date;
        ApplyDataToItems();
        UpdateQrContent();
    }

    protected override string[] StandardItemKeys => new[]
    {
        "company", "header", "line_top",
        "receipt_number_label", "receipt_number_value",
        "customer_label", "customer_value",
        "brand_label", "brand_value",
        "model_label", "model_value",
        "serial_label", "serial_value",
        "date_label", "date_value",
        "line_bottom", "address", "phone"
    };

    protected override void ApplyDataToItems()
    {
        UpdateItemText("receipt_number_value", StickerText.ToEnglishDigits(_receiptNumber));
        UpdateItemText("customer_value", StickerText.ToEnglishDigits(_customerName));
        UpdateItemText("brand_value", StickerText.ToEnglishDigits(Brand));
        UpdateItemText("model_value", StickerText.ToEnglishDigits(Model));
        UpdateItemText("serial_value", StickerText.ToEnglishDigits(Serial));
        if (_receiptDate > DateTime.MinValue)
            UpdateItemText("date_value", StickerText.FormatDate(_receiptDate));
    }

    protected override void RefreshLabelTexts()
    {
        void Apply(string key, string english)
        {
            var it = FindItem(key);
            if (it != null && (string.IsNullOrWhiteSpace(it.DisplayText) || ContainsArabic(it.DisplayText)))
                it.DisplayText = english;
        }
        Apply("receipt_number_label", StickerText.ReceiptNoLabel);
        Apply("customer_label", StickerText.CustomerLabel);
        Apply("brand_label", StickerText.BrandLabel);
        Apply("model_label", StickerText.ModelLabel);
        Apply("serial_label", StickerText.SerialLabel);
        Apply("date_label", StickerText.DateLabel);
    }

    public override void ApplyVariableBindings()
    {
        foreach (var item in StickerItems)
        {
            if (string.IsNullOrWhiteSpace(item.VariableBinding))
                continue;
            string value = item.VariableBinding switch
            {
                "receipt_number" => StickerText.ToEnglishDigits(_receiptNumber),
                "customer" => StickerText.ToEnglishDigits(_customerName),
                "brand" => StickerText.ToEnglishDigits(Brand),
                "model" => StickerText.ToEnglishDigits(Model),
                "serial" => StickerText.ToEnglishDigits(Serial),
                "date" => _receiptDate > DateTime.MinValue ? StickerText.FormatDate(_receiptDate) : "",
                _ => item.DisplayText
            };
            item.DisplayText = value;
        }
    }

    public override string ResolveValueKey(string itemKey)
    {
        return itemKey switch
        {
            "receipt_number_value" => StickerText.ToEnglishDigits(_receiptNumber),
            "customer_value" => StickerText.ToEnglishDigits(_customerName),
            "brand_value" => StickerText.ToEnglishDigits(Brand),
            "model_value" => StickerText.ToEnglishDigits(Model),
            "serial_value" => StickerText.ToEnglishDigits(Serial),
            "date_value" => _receiptDate > DateTime.MinValue ? StickerText.FormatDate(_receiptDate) : "",
            _ => ""
        };
    }

    public override void UpdateQrContent()
    {
        // The receipt sticker has no QR code by design. The receipt number is shown
        // in large bold text in the badge area instead (see StrictLayout).
    }

    public override void StrictLayout()
    {
        var mm2px = 1.0 / PixelToMm;
        var left = 2 * mm2px;
        var right = 2 * mm2px;
        var top = 2.0 * mm2px;
        var bottom = 1.0 * mm2px;
        var contentWidth = Math.Max(0.0, CanvasWidthPx - left - right);
        var bodyFont = Math.Max(9.0, BodyFontSize);
        var titleFont = Math.Max(bodyFont * 1.2, HeaderFontSize);

        var company = EnsureItem("company");
        var header = EnsureItem("header");
        var logo = EnsureItem("logo");
        var lineTop = FindItem("line_top") ?? EnsureItem("line_top");
        var lineBottom = FindItem("line_bottom") ?? EnsureItem("line_bottom");
        var address = EnsureItem("address");
        var phone = EnsureItem("phone");

        var rows = new[]
        {
            ("customer_label", "customer_value"),
            ("brand_label", "brand_value"),
            ("model_label", "model_value"),
            ("serial_label", "serial_value"),
            ("date_label", "date_value")
        };

        void SetLabel(string key, string text)
        {
            var it = FindItem(key) ?? EnsureItem(key);
            it.DisplayText = text;
        }

        // Company name (top, prominent) + abbreviation right under it
        company.IsVisible = true;
        company.DisplayText = CompanyName ?? "";
        company.FontSize = titleFont;
        company.X = left;
        company.Y = top;
        company.Width = contentWidth;
        company.Height = Math.Max(16.0, titleFont * 1.4);

        // Logo beside the company name if available
        var hasLogo = UseLogo && !string.IsNullOrWhiteSpace(LogoPath);
        UpdateItemVisibility("logo", hasLogo);
        if (hasLogo)
        {
            var logoW = Math.Min(16 * mm2px, contentWidth * 0.25);
            var logoH = Math.Min(company.Height, CanvasHeightPx * 0.22);
            logo.Width = logoW;
            logo.Height = logoH;
            logo.X = left;
            logo.Y = company.Y + Math.Max(0, (company.Height - logoH) / 2);
            company.X = left + logoW + (1 * mm2px);
            company.Width = Math.Max(0, contentWidth - logoW - (1 * mm2px));
        }

        // Header / abbreviation under the company name
        header.IsVisible = true;
        header.DisplayText = CompanyHeader ?? "";
        header.FontSize = Math.Max(10.0, bodyFont * 0.85);
        header.X = left;
        header.Y = company.Y + company.Height;
        header.Width = contentWidth;
        header.Height = Math.Max(13.0, header.FontSize * 1.3);

        // Top line
        lineTop.X = left;
        lineTop.Y = header.Y + header.Height + (0.5 * mm2px);
        lineTop.Width = contentWidth;
        lineTop.Height = Math.Max(1.0, 0.5 * mm2px);
        lineTop.IsVisible = true;

        // Right area: receipt number badge (large bold text, in place of a QR code)
        var badgeW = Math.Min(15 * mm2px, Math.Max(12 * mm2px, contentWidth * 0.30));
        var badgeRight = CanvasWidthPx - right - (1 * mm2px);
        var badgeX = badgeRight - badgeW;
        var textAreaWidth = Math.Max(0.0, badgeX - left - (1 * mm2px));
        var labelColWidth = Math.Max(18 * mm2px, textAreaWidth * 0.42);
        var valueColX = left + labelColWidth + (0.5 * mm2px);
        var valueColWidth = Math.Max(0.0, textAreaWidth - labelColWidth - (1 * mm2px));

        // English labels (sticker data prints in English for thermal printers)
        SetLabel("customer_label", StickerText.CustomerLabel);
        SetLabel("brand_label", StickerText.BrandLabel);
        SetLabel("model_label", StickerText.ModelLabel);
        SetLabel("serial_label", StickerText.SerialLabel);
        SetLabel("date_label", StickerText.DateLabel);

        // Values from the isolated receipt data
        ApplyDataToItems();

        // Distribute the rows vertically between the top line and the footer so the
        // layout always fits the configured sticker size.
        var rowStart = lineTop.Y + lineTop.Height + (1.5 * mm2px);
        var footerHeight = (2.5 * mm2px) + (2.0 * mm2px) + (2.5 * mm2px);
        var rowsEnd = Math.Max(rowStart, CanvasHeightPx - bottom - footerHeight);
        var bodyH = Math.Max(10.0, Math.Min(4.2 * mm2px, (rowsEnd - rowStart) / rows.Length));

        var y = rowStart;
        foreach (var (lblKey, valKey) in rows)
        {
            var lbl = FindItem(lblKey) ?? EnsureItem(lblKey);
            var val = FindItem(valKey) ?? EnsureItem(valKey);

            lbl.X = left + (1 * mm2px);
            lbl.Y = y;
            lbl.Width = labelColWidth;
            lbl.Height = bodyH;
            lbl.FontSize = bodyFont;

            val.X = valueColX;
            val.Y = y;
            val.Width = valueColWidth;
            val.Height = bodyH;
            val.FontSize = bodyFont;

            UpdateItemVisibility(lblKey, true);
            UpdateItemVisibility(valKey, true);

            y += bodyH + (0.6 * mm2px);
        }

        // Receipt number badge on the right, vertically centered with the rows
        var numFont = Math.Max(15.0, Math.Min(bodyFont * 1.9, titleFont * 1.3));
        var badgeLabelH = Math.Max(8.0, bodyFont * 0.85);
        var badgeValueH = Math.Max(12.0, numFont * 1.25);
        var badgeH = badgeLabelH + badgeValueH;
        var badgeTop = rowStart + Math.Max(0, (y - rowStart - badgeH) / 2);

        var badgeLabel = FindItem("receipt_number_label") ?? EnsureItem("receipt_number_label");
        badgeLabel.DisplayText = StickerText.ReceiptNoLabel;
        badgeLabel.X = badgeX;
        badgeLabel.Y = badgeTop;
        badgeLabel.Width = badgeW;
        badgeLabel.Height = badgeLabelH;
        badgeLabel.FontSize = Math.Max(8.0, bodyFont * 0.8);
        badgeLabel.TextAlignment = "Center";
        badgeLabel.IsBold = true;
        UpdateItemVisibility("receipt_number_label", true);

        var badgeValue = FindItem("receipt_number_value") ?? EnsureItem("receipt_number_value");
        badgeValue.X = badgeX;
        badgeValue.Y = badgeTop + badgeLabelH;
        badgeValue.Width = badgeW;
        badgeValue.Height = badgeValueH;
        badgeValue.FontSize = numFont;
        badgeValue.TextAlignment = "Center";
        badgeValue.IsBold = true;
        badgeValue.DisplayText = StickerText.ToEnglishDigits(_receiptNumber);
        UpdateItemVisibility("receipt_number_value", true);

        // Bottom line
        lineBottom.X = left;
        lineBottom.Y = y + (0.5 * mm2px);
        lineBottom.Width = contentWidth;
        lineBottom.Height = Math.Max(1.0, 0.5 * mm2px);
        lineBottom.IsVisible = true;

        // Footer
        address.DisplayText = CompanyAddress ?? "";
        phone.DisplayText = string.IsNullOrWhiteSpace(CompanyPhone)
            ? ""
            : $"{StickerText.PhoneLabel} {StickerText.ToEnglishDigits(CompanyPhone)}";

        address.X = 0;
        address.Width = CanvasWidthPx;
        address.Height = bodyH * 0.9;
        address.FontSize = bodyFont * 0.85;
        address.Y = lineBottom.Y + (2 * mm2px);

        phone.X = 0;
        phone.Width = CanvasWidthPx;
        phone.Height = bodyH * 0.9;
        phone.FontSize = bodyFont * 0.8;
        phone.Y = address.Y + (bodyH * 1.6);

        ClampAllItems();
        OnPropertyChanged(nameof(StickerItems));
    }

    /// <summary>
    /// The receipt sticker may never contain certificate data, device type rows or a QR
    /// code, even when an old (pre-fix) receipt layout was already saved to disk.
    /// </summary>
    protected override bool SanitizeLayout()
    {
        var forbidden = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "cert_label", "cert_value",
            "cal_label", "cal_value",
            "exp_label", "exp_value",
            "device_type_label", "device_type_value",
            "qr"
        };

        bool removed = false;
        for (int i = StickerItems.Count - 1; i >= 0; i--)
        {
            if (forbidden.Contains(StickerItems[i].Key))
            {
                StickerItems.RemoveAt(i);
                removed = true;
            }
        }
        return removed;
    }
}
