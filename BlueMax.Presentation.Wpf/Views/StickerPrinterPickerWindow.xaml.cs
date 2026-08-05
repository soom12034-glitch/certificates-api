using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using BlueMax.Presentation.Wpf.Resources;
using BlueMax.Presentation.Wpf.Services;
using BlueMax.Presentation.Wpf.ViewModels;

namespace BlueMax.Presentation.Wpf.Views;

public partial class StickerPrinterPickerWindow : Window
{
    public StickerPrinterInfo? SelectedPrinter { get; private set; }

    public StickerPrinterPickerWindow(IEnumerable<StickerPrinterInfo> printers)
    {
        InitializeComponent();
        WindowHelper.ApplyDarkTitleBar(this);
        var vm = new PickerViewModel(printers);
        vm.RequestClose += (_, _) =>
        {
            SelectedPrinter = vm.SelectedRow?.Info;
            DialogResult = vm.SelectedRow != null;
            Close();
        };
        DataContext = vm;
    }

    public sealed class PickerRow
    {
        public PickerRow(StickerPrinterInfo info) => Info = info;

        public StickerPrinterInfo Info { get; }
        public string Name => Info.Name;
        public string DriverName => Info.DriverName;
        public string PortName => Info.PortName;
        public string Protocol => Info.Protocol;
        public int Dpi => Info.Dpi;
        public bool IsThermal => Info.IsThermal;
    }

    sealed class PickerViewModel : ViewModelBase
    {
        PickerRow? _selectedRow;

        public PickerViewModel(IEnumerable<StickerPrinterInfo> printers)
        {
            foreach (var printer in printers)
                Printers.Add(new PickerRow(printer));
            SelectedRow = Printers.FirstOrDefault(r => r.IsThermal) ?? Printers.FirstOrDefault();

            ApplyCommand = new RelayCommand(_ =>
            {
                if (SelectedRow == null)
                {
                    MessageBox.Show(Translations.Get("NoPrinterSelected"),
                        Translations.Get("DetectStickerPrinter"),
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }
                RequestClose?.Invoke(this, EventArgs.Empty);
            });
            CloseCommand = new RelayCommand(_ => RequestClose?.Invoke(this, EventArgs.Empty));
        }

        public ObservableCollection<PickerRow> Printers { get; } = new();

        public PickerRow? SelectedRow
        {
            get => _selectedRow;
            set => SetProperty(ref _selectedRow, value);
        }

        public RelayCommand ApplyCommand { get; }
        public RelayCommand CloseCommand { get; }
        public event EventHandler? RequestClose;
    }
}
