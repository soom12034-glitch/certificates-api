using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using BlueMax.Presentation.Wpf.ViewModels;

namespace BlueMax.Presentation.Wpf.Views;

public partial class CertificatesView : UserControl
{
    public CertificatesView()
    {
        InitializeComponent();
        Loaded += async (_, __) =>
        {
            if (DataContext is CertificatesViewModel vm)
            {
                await vm.InitializeAsync();
                return;
            }
            void handler(object? sender, DependencyPropertyChangedEventArgs e)
            {
                if (e.NewValue is CertificatesViewModel vm2)
                {
                    DataContextChanged -= handler;
                    _ = vm2.InitializeAsync();
                }
            }
            DataContextChanged += handler;
        };
    }

    void ArchiveGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is not CertificatesViewModel vm)
            return;
        var cmd = vm.OpenSelectedCertificateFileCommand;
        if (cmd == null)
            return;
        if (cmd.CanExecute(null))
            cmd.Execute(null);
    }

    void ArchiveSearchBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter)
            return;
        if (DataContext is not CertificatesViewModel vm)
            return;
        var cmd = vm.RefreshCertificatesCommand;
        if (cmd == null)
            return;
        if (cmd.CanExecute(null))
            cmd.Execute(null);
    }

    void ClientNameBox_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Down)
        {
            if (DataContext is CertificatesViewModel vm && vm.IsClientOptionsOpen)
            {
                var list = ClientNameList;
                if (list != null && list.Items.Count > 0)
                {
                    list.Focus();
                    list.SelectedIndex = 0;
                    var container = list.ItemContainerGenerator.ContainerFromIndex(0) as ListBoxItem;
                    container?.Focus();
                    e.Handled = true;
                }
            }
        }
        else if (e.Key == Key.Escape)
        {
            if (DataContext is CertificatesViewModel vm)
                vm.IsClientOptionsOpen = false;
        }
    }

    void ClientNameList_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            if (sender is ListBox list && list.SelectedItem != null)
            {
                if (DataContext is CertificatesViewModel vm)
                {
                    vm.SelectedClientOption = list.SelectedItem as string ?? string.Empty;
                }
                e.Handled = true;
                ClientNameBox.Focus();
                ClientNameBox.CaretIndex = ClientNameBox.Text.Length;
            }
        }
        else if (e.Key == Key.Escape)
        {
             if (DataContext is CertificatesViewModel vm)
                vm.IsClientOptionsOpen = false;
             ClientNameBox.Focus();
        }
    }

    static T? FindChild<T>(DependencyObject parent) where T : DependencyObject
    {
        if (parent == null) return null;
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            if (child is T typed) return typed;
            var result = FindChild<T>(child);
            if (result != null) return result;
        }
        return null;
    }
}
