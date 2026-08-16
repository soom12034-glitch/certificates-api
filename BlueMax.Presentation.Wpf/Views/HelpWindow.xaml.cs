using System.Windows;
using BlueMax.Presentation.Wpf.Services;

namespace BlueMax.Presentation.Wpf.Views
{
    public partial class HelpWindow : Window
    {
        public HelpWindow()
        {
            InitializeComponent();
            LanguageService.Instance.ApplyFlowDirection(this);
        }
    }
}
