using System.Drawing;
using ZXing;
using ZXing.Common;
using ZXing.Windows.Compatibility;

namespace BlueMax.Infrastructure;

public static class BarcodeGenerator
{
    public static Bitmap GenerateBarcode(string text, int width = 300, int height = 100)
    {
        var writer = new BarcodeWriter
        {
            Format = BarcodeFormat.CODE_128,
            Options = new EncodingOptions
            {
                Width = width,
                Height = height,
                Margin = 2
            }
        };
        return writer.Write(text);
    }

    public static Bitmap GenerateQrCode(string text, int size = 200)
    {
        var writer = new BarcodeWriter
        {
            Format = BarcodeFormat.QR_CODE,
            Options = new EncodingOptions
            {
                Width = size,
                Height = size,
                Margin = 1
            }
        };
        return writer.Write(text);
    }
}
