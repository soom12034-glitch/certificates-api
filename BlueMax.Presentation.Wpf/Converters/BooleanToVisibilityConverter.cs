using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace BlueMax.Presentation.Wpf.Converters
{
    public class BooleanToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool booleanValue)
            {
                if (parameter is string stringParameter && stringParameter.Equals("Inverse", StringComparison.OrdinalIgnoreCase))
                {
                    return booleanValue ? Visibility.Collapsed : Visibility.Visible;
                }
                return booleanValue ? Visibility.Visible : Visibility.Collapsed;
            }
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Visibility visibility)
            {
                if (parameter is string stringParameter && stringParameter.Equals("Inverse", StringComparison.OrdinalIgnoreCase))
                {
                    return visibility == Visibility.Collapsed;
                }
                return visibility == Visibility.Visible;
            }
            return false;
        }
    }
}
