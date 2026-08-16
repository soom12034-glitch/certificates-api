using System;
using System.ComponentModel;
using System.Globalization;
using System.Windows;
using BlueMax.Presentation.Wpf.Resources;

namespace BlueMax.Presentation.Wpf.Services;

public class LanguageService : INotifyPropertyChanged
{
    private static LanguageService? _instance;
    public static LanguageService Instance => _instance ??= new LanguageService();

    private CultureInfo _currentCulture;

    public event PropertyChangedEventHandler? PropertyChanged;

    private LanguageService()
    {
        _currentCulture = CultureInfo.GetCultureInfo("ar-SA"); // Default to Arabic
    }

    public CultureInfo CurrentCulture
    {
        get => _currentCulture;
        set
        {
            if (_currentCulture != value)
            {
                _currentCulture = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CurrentCulture)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsRTL)));
                UpdateFlowDirection();
            }
        }
    }

    public bool IsRTL => _currentCulture.TwoLetterISOLanguageName == "ar";

    public void SetLanguage(string languageCode)
    {
        CurrentCulture = CultureInfo.GetCultureInfo(languageCode);
    }

    public void ToggleLanguage()
    {
        if (IsRTL)
        {
            SetLanguage("en-US");
        }
        else
        {
            SetLanguage("ar-SA");
        }
    }

    private void UpdateFlowDirection()
    {
        if (Application.Current == null) return;
        foreach (Window window in Application.Current.Windows)
        {
            ApplyFlowDirection(window);
        }
    }

    // Applies the active language direction (RTL/LTR) to a window.
    // Called when a child window is created so direction stays consistent
    // across all windows when the language is toggled.
    public void ApplyFlowDirection(Window window)
    {
        if (window == null) return;
        window.FlowDirection = IsRTL ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;
    }

    public string GetString(string key)
    {
        return Translations.Get(key);
    }
}
