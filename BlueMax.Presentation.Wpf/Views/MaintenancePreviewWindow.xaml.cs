using System.Windows;
using BlueMax.Presentation.Wpf.Services;
using BlueMax.Presentation.Wpf.ViewModels;

namespace BlueMax.Presentation.Wpf.Views;

public partial class MaintenancePreviewWindow : Window
{
    public MaintenancePreviewWindow(WorkOrderItem item)
    {
        InitializeComponent();
        LanguageService.Instance.ApplyFlowDirection(this);
        DataContext = item;
    }
}

