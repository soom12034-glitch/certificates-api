using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace BlueMax.Presentation.Wpf.Converters;

/// <summary>
/// Shows the empty state (Visible) only when the collection is empty and the view model is not busy.
/// Inputs: [0] = collection Count (int), [1] = IsBusy (bool).
/// </summary>
public sealed class EmptyStateVisibilityConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        int count = 0;
        if (values.Length > 0 && values[0] is int c)
        {
            count = c;
        }
        else if (values.Length > 0 && !ReferenceEquals(values[0], DependencyProperty.UnsetValue))
        {
            try
            {
                count = System.Convert.ToInt32(values[0], CultureInfo.InvariantCulture);
            }
            catch
            {
                count = 0;
            }
        }

        bool isBusy = values.Length > 1 && values[1] is bool busy && busy;

        return !isBusy && count == 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
