using System;
using System.Globalization;
using System.IO;
using System.Windows.Data;
using System.Windows.Media.Imaging;
using ZXing;
using ZXing.Common;
using ZXing.Windows.Compatibility;
using System.Drawing;
using System.Drawing.Imaging;
using System.Diagnostics;
using System.Windows;

namespace BlueMax.Presentation.Wpf.Converters;

public class QrCodeConverter : IValueConverter
{
#nullable disable
    static readonly object _lock = new();
    static readonly System.Collections.Generic.Dictionary<string, BitmapImage> _cache =
        new(System.StringComparer.Ordinal);

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value == null || (value is string s && string.IsNullOrWhiteSpace(s)))
            return (object)null;
            
        string content = (value.ToString() ?? "").Trim();
        Debug.WriteLine($"QrCodeConverter: Converting content: {content}");

        lock (_lock)
        {
            if (_cache.TryGetValue(content, out var cached))
                return cached;
        }

        try
        {
            // Use Windows compatibility barcode writer to ensure a Bitmap renderer is present
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
            
            // Generate bitmap
            using (var bitmap = writer.Write(content))
            {
                using (var stream = new MemoryStream())
                {
                    bitmap.Save(stream, ImageFormat.Png);
                    stream.Position = 0;
                    
                    var image = new BitmapImage();
                    image.BeginInit();
                    image.CacheOption = BitmapCacheOption.OnLoad;
                    image.StreamSource = stream;
                    image.EndInit();
                    image.Freeze(); // Important for cross-thread access if needed
                    Debug.WriteLine($"QrCodeConverter: Successfully converted content to image.");
                    lock (_lock)
                    {
                        _cache[content] = image;
                    }
                    return image; 
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"QrCodeConverter: Error converting content '{content}': {ex}");
            return (object)null;
        }
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return System.Windows.Data.Binding.DoNothing;
    }
#nullable enable
}
