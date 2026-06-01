using System.Windows.Controls;
using System.Windows;
using BlueMax.Presentation.Wpf.ViewModels;

namespace BlueMax.Presentation.Wpf.Views;

public partial class SettingsView : UserControl
{
    public SettingsView()
    {
        InitializeComponent();
    }

    void OnLoginPasswordChanged(object sender, RoutedEventArgs e)
    {
        if (DataContext is SettingsViewModel vm && sender is PasswordBox pb)
            vm.LoginPasswordInput = pb.Password ?? "";
    }
}
