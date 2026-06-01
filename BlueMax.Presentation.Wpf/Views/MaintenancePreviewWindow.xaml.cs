using System.Windows;
using BlueMax.Presentation.Wpf.ViewModels;

namespace BlueMax.Presentation.Wpf.Views;

public partial class MaintenancePreviewWindow : Window
{
    public MaintenancePreviewWindow(WorkOrderItem item)
    {
        InitializeComponent();
        DataContext = item;
    }
}

