using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using BlueMax.Infrastructure;

namespace BlueMax.KeyGen.UI;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private readonly LicenseService _licenseService;

    public MainWindow()
    {
        InitializeComponent();
        var store = new LicenseStore();
        _licenseService = new LicenseService(store);
        
        // Default to 1 year from now
        DpExpiration.SelectedDate = DateTime.Today.AddYears(1);
    }

    private void BtnPaste_Click(object sender, RoutedEventArgs e)
    {
        if (Clipboard.ContainsText())
        {
            TxtHardwareId.Text = Clipboard.GetText().Trim();
        }
    }

    private void BtnGenerate_Click(object sender, RoutedEventArgs e)
    {
        var hwid = TxtHardwareId.Text.Trim();
        if (string.IsNullOrWhiteSpace(hwid))
        {
            MessageBox.Show("الرجاء إدخال بصمة الجهاز (Hardware ID)", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        if (DpExpiration.SelectedDate == null)
        {
            MessageBox.Show("الرجاء اختيار تاريخ الانتهاء", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        try
        {
            var expiration = DpExpiration.SelectedDate.Value;
            var key = _licenseService.GenerateActivationCode(hwid, expiration);
            TxtLicenseKey.Text = key;
            TxtStatus.Text = $"تم توليد المفتاح بنجاح! (ينتهي في: {expiration:yyyy-MM-dd})";
        }
        catch (Exception ex)
        {
             MessageBox.Show($"حدث خطأ أثناء التوليد: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void BtnCopy_Click(object sender, RoutedEventArgs e)
    {
        if (!string.IsNullOrWhiteSpace(TxtLicenseKey.Text))
        {
            Clipboard.SetText(TxtLicenseKey.Text);
            TxtStatus.Text = "تم نسخ المفتاح إلى الحافظة!";
        }
    }
}