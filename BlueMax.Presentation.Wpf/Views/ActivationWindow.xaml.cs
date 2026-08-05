using System;
using System.Windows;
using BlueMax.Infrastructure;
using BlueMax.Presentation.Wpf;
using BlueMax.Presentation.Wpf.ViewModels;

namespace BlueMax.Presentation.Wpf.Views
{
    public partial class ActivationWindow : Window
    {
        private readonly LicenseService _licenseService;

        public ActivationWindow()
        {
            InitializeComponent();
            var store = new LicenseStore();
            _licenseService = new LicenseService(store);
            TxtHardwareId.Text = _licenseService.GetHardwareId();

            WindowHelper.ApplyDarkTitleBar(this);
        }

        private void BtnCopyHardwareId_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(TxtHardwareId.Text)) return;

            try
            {
                Clipboard.SetText(TxtHardwareId.Text);
                BtnCopyHardwareId.Content = "✓ " + TranslationViewModel.Instance.CopyButton;
                var timer = new System.Windows.Threading.DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
                timer.Tick += (_, _) =>
                {
                    BtnCopyHardwareId.Content = TranslationViewModel.Instance.CopyButton;
                    timer.Stop();
                };
                timer.Start();
            }
            catch
            {
                TxtStatus.Text = "تعذر نسخ بصمة الجهاز.";
            }
        }

        private void BtnActivate_Click(object sender, RoutedEventArgs e)
        {
            var code = TxtActivationCode.Text.Trim();
            if (string.IsNullOrWhiteSpace(code))
            {
                TxtStatus.Text = "الرجاء إدخال رمز التفعيل.";
                return;
            }

            if (_licenseService.TryActivate(code))
            {
                MessageBox.Show("تم التفعيل بنجاح! سيتم تشغيل البرنامج الآن.", "نجاح", MessageBoxButton.OK, MessageBoxImage.Information);
                DialogResult = true;
                Close();
            }
            else
            {
                TxtStatus.Text = "رمز التفعيل غير صالح. تأكد من بصمة الجهاز أو تاريخ الانتهاء.";
            }
        }

        private void TxtActivationCode_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            var textBox = sender as System.Windows.Controls.TextBox;
            if (textBox == null) return;

            // Simple logic to format as XXXX-XXXX-XXXX-XXXX
            // Keep cursor position logic simple: put at end
            
            string raw = textBox.Text.Replace("-", "").ToUpper();
            string formatted = "";
            for (int i = 0; i < raw.Length; i++)
            {
                if (i > 0 && i % 4 == 0)
                    formatted += "-";
                formatted += raw[i];
            }

            if (textBox.Text != formatted)
            {
                textBox.Text = formatted;
                textBox.SelectionStart = formatted.Length;
            }
        }
    }
}
