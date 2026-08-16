using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace BlueMax.Presentation.Wpf;

public static class WindowHelper
{
    public static void ApplyDarkTitleBar(Window window)
    {
        if (window == null) return;

        // Keep the active language direction (RTL/LTR) consistent on this window.
        Services.LanguageService.Instance.ApplyFlowDirection(window);

        // Apply immediately if loaded, otherwise wait for load
        if (window.IsLoaded)
        {
            ApplyDarkTitleBarInternal(window);
        }
        else
        {
            window.Loaded += (s, e) => ApplyDarkTitleBarInternal(window);
        }
    }

    private static void ApplyDarkTitleBarInternal(Window window)
    {
        var windowHandle = new WindowInteropHelper(window).Handle;
        if (windowHandle == IntPtr.Zero) return;

        // Try to enable immersive dark mode (works on Win10/11)
        int useImmersiveDarkMode = 1;
        DwmSetWindowAttribute(windowHandle, DWMWA_USE_IMMERSIVE_DARK_MODE, ref useImmersiveDarkMode, sizeof(int));

        // Try to set specific caption color for Win11 (HeaderBackground: #23334D -> 0x004D3323)
        // DWM COLORREF is 0x00BBGGRR
        int captionColor = 0x004D3323; 
        DwmSetWindowAttribute(windowHandle, DWMWA_CAPTION_COLOR, ref captionColor, sizeof(int));

        // Set text color to white (0x00FFFFFF)
        int textColor = 0x00FFFFFF;
        DwmSetWindowAttribute(windowHandle, DWMWA_TEXT_COLOR, ref textColor, sizeof(int));
    }

    [DllImport("dwmapi.dll", PreserveSig = true)]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

    private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;
    private const int DWMWA_CAPTION_COLOR = 35;
    private const int DWMWA_TEXT_COLOR = 36;
}
