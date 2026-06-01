using System.Windows;
using BlueMax.Presentation.Wpf;
using BlueMax.Presentation.Wpf.ViewModels;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using System.Windows.Controls;

namespace BlueMax.Presentation.Wpf.Views;

public partial class StickerPreviewWindow : Window
{
    public StickerPreviewWindow(byte[] pngBytes)
    {
        InitializeComponent();
        WindowHelper.ApplyDarkTitleBar(this);
    }

    public StickerPreviewWindow(StickerDesignerViewModel viewModel)
    {
        InitializeComponent();
        WindowHelper.ApplyDarkTitleBar(this);
        DataContext = viewModel;
    }

    public StickerPreviewWindow(
        string companyName,
        string companyHeader,
        string address,
        string deviceName,
        string brand,
        string serialNumber,
        DateTime calDate,
        DateTime expDate,
        string qrText,
        bool showQr = true)
    {
        InitializeComponent();
        WindowHelper.ApplyDarkTitleBar(this);
        
        var vm = new StickerDesignerViewModel();
        vm.CompanyName = companyName;
        vm.CompanyHeader = companyHeader;
        vm.CompanyAddress = address;
        vm.Brand = brand;
        vm.Model = deviceName;
        vm.Serial = serialNumber;
        vm.CalDate = calDate;
        vm.ExpDate = expDate;
        vm.UpdateQrContent();
        
        DataContext = vm;
    }

    // Remove unused methods
    private void Window_Loaded(object sender, RoutedEventArgs e) { }
}
