using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Text;
using System.Runtime.Versioning;

namespace BlueMax.Infrastructure
{
    [SupportedOSPlatform("windows")]
    public static class ZplImageHelper
    {
        public static string ConvertBitmapToZpl(Bitmap bitmap, int x, int y)
        {
            var binaryData = GetMonochromePixelData(bitmap);
            var hexData = BitConverter.ToString(binaryData).Replace("-", "");
            
            var totalBytes = binaryData.Length;
            var widthBytes = (bitmap.Width + 7) / 8;
            
            return $"^FO{x},{y}^GFA,{totalBytes},{totalBytes},{widthBytes},{hexData}^FS";
        }

        private static byte[] GetMonochromePixelData(Bitmap bitmap)
        {
            int width = bitmap.Width;
            int height = bitmap.Height;
            int widthBytes = (width + 7) / 8;
            byte[] data = new byte[widthBytes * height];

            // Lock bits for faster access
            BitmapData bmpData = bitmap.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            
            unsafe
            {
                byte* ptr = (byte*)bmpData.Scan0;
                for (int y = 0; y < height; y++)
                {
                    byte* row = ptr + (y * bmpData.Stride);
                    for (int x = 0; x < width; x++)
                    {
                        // 32bppArgb: B, G, R, A
                        byte b = row[x * 4];
                        byte g = row[x * 4 + 1];
                        byte r = row[x * 4 + 2];
                        
                        // Simple threshold
                        if ((r + g + b) / 3 < 128)
                        {
                            int byteIndex = y * widthBytes + (x / 8);
                            int bitIndex = 7 - (x % 8);
                            data[byteIndex] |= (byte)(1 << bitIndex);
                        }
                    }
                }
            }
            
            bitmap.UnlockBits(bmpData);
            return data;
        }

        public static string GenerateTextAsZplImage(string text, int widthDots, int x, int y, int heightDots = 40, int fontSize = 16, bool bold = true)
        {
            if (string.IsNullOrWhiteSpace(text)) return "";

            using (var bitmap = new Bitmap(widthDots, heightDots))
            using (var g = Graphics.FromImage(bitmap))
            {
                g.Clear(Color.White);
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.SingleBitPerPixelGridFit;

                using (var font = new Font("Arial", fontSize, bold ? FontStyle.Bold : FontStyle.Regular))
                using (var brush = new SolidBrush(Color.Black))
                {
                    var format = new StringFormat
                    {
                        Alignment = StringAlignment.Center,
                        LineAlignment = StringAlignment.Center,
                        FormatFlags = StringFormatFlags.DirectionRightToLeft 
                    };

                    g.DrawString(text, font, brush, new RectangleF(0, 0, widthDots, heightDots), format);
                }
                
                return ConvertBitmapToZpl(bitmap, x, y);
            }
        }
    }
}