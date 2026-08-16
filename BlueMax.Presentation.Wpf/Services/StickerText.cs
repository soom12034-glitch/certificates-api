using System;
using System.Globalization;
using System.Text;

namespace BlueMax.Presentation.Wpf.Services;

/// <summary>
/// English-only text for data printed on stickers.
/// Only company name, company abbreviation and address may be Arabic/English;
/// every other sticker field stays English and all numbers use Western digits.
/// </summary>
public static class StickerText
{
    public const string BrandLabel = "Brand:";
    public const string ModelLabel = "Model:";
    public const string SerialLabel = "Serial No.:";
    public const string CalDateLabel = "Cal. Date:";
    public const string ValidUntilLabel = "Valid Until:";
    public const string PhoneLabel = "Tel:";

    // Receipt sticker labels (maintenance device receipt sticker only).
    public const string ReceiptNoLabel = "Receipt No.:";
    public const string CustomerLabel = "Customer:";
    public const string DateLabel = "Date:";

    public static string FormatDate(DateTime value)
    {
        return value.ToString("dd-MM-yyyy", CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Converts Arabic-Indic (٠-٩), Persian (۰-۹) and Urdu (۰-۹) digits to
    /// English digits (0-9) so all numbers render in English.
    /// </summary>
    public static string ToEnglishDigits(string? text)
    {
        if (string.IsNullOrEmpty(text)) return text ?? "";
        StringBuilder? sb = null;
        for (int i = 0; i < text.Length; i++)
        {
            char c = text[i];
            char? replacement = c switch
            {
                '\u0660' => '0', '\u0661' => '1', '\u0662' => '2', '\u0663' => '3', '\u0664' => '4',
                '\u0665' => '5', '\u0666' => '6', '\u0667' => '7', '\u0668' => '8', '\u0669' => '9',
                '\u06F0' => '0', '\u06F1' => '1', '\u06F2' => '2', '\u06F3' => '3', '\u06F4' => '4',
                '\u06F5' => '5', '\u06F6' => '6', '\u06F7' => '7', '\u06F8' => '8', '\u06F9' => '9',
                _ => null
            };
            if (replacement.HasValue)
            {
                sb ??= new StringBuilder(text.Length);
                sb.Append(text, 0, i);
                sb.Append(replacement.Value);
                break;
            }
        }
        if (sb == null) return text;
        for (int i = sb.Length; i < text.Length; i++)
        {
            char c = text[i];
            sb.Append(c switch
            {
                '\u0660' => '0', '\u0661' => '1', '\u0662' => '2', '\u0663' => '3', '\u0664' => '4',
                '\u0665' => '5', '\u0666' => '6', '\u0667' => '7', '\u0668' => '8', '\u0669' => '9',
                '\u06F0' => '0', '\u06F1' => '1', '\u06F2' => '2', '\u06F3' => '3', '\u06F4' => '4',
                '\u06F5' => '5', '\u06F6' => '6', '\u06F7' => '7', '\u06F8' => '8', '\u06F9' => '9',
                _ => c
            });
        }
        return sb.ToString();
    }
}
