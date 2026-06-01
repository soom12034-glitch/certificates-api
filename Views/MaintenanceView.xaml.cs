using System.Windows.Controls;
using System.Windows;
using BlueMax.Presentation.Wpf.ViewModels;

namespace BlueMax.Presentation.Wpf.Views;

public partial class MaintenanceView : UserControl
{
    public MaintenanceView()
    {
        InitializeComponent();
    }

    void OnWorkOrdersDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (DataContext is MaintenanceViewModel vm)
            vm.PreviewWorkOrderCommand.Execute(null);
    }
}
