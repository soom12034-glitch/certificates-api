using System.Windows;
using BlueMax.Presentation.Wpf;
using BlueMax.Presentation.Wpf.ViewModels;

namespace BlueMax.Presentation.Wpf.Views;

public partial class StickerPreviewWindow : Window
{
    public StickerPreviewWindow(StickerDesignerViewModel viewModel)
    {
        InitializeComponent();
        WindowHelper.ApplyDarkTitleBar(this);
        DataContext = viewModel;
    }
}
