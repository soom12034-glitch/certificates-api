using System.IO;
using System;

namespace BlueMax.Presentation.Wpf.ViewModels;

public sealed class MainViewModel : ViewModelBase
{
    const string HomeSection = "Home";
    const string CertificatesSection = "Certificates";
    const string ClientsSection = "Clients";
    const string MaintenanceSection = "Maintenance";
    const string InventorySection = "Inventory";
    const string DeviceHistorySection = "DeviceHistory";
    const string StickerDesignerSection = "StickerDesigner";
    const string SettingsSection = "Settings";

    readonly HomeViewModel _homeViewModel;
    readonly CertificatesViewModel _certificatesViewModel;
    readonly ClientsViewModel _clientsViewModel;
    readonly MaintenanceViewModel _maintenanceViewModel;
    readonly InventoryViewModel _inventoryViewModel;
    readonly SettingsViewModel _settingsViewModel;
    readonly DeviceHistoryViewModel _deviceHistoryViewModel;
    readonly StickerDesignerViewModel _stickerDesignerViewModel;

    ViewModelBase? _currentView;
    string _currentTitle = "الرئيسية";

    public MainViewModel()
    {
        _homeViewModel = new HomeViewModel();
        _certificatesViewModel = new CertificatesViewModel();
        _clientsViewModel = new ClientsViewModel();
        _maintenanceViewModel = new MaintenanceViewModel();
        _inventoryViewModel = new InventoryViewModel();
        _settingsViewModel = new SettingsViewModel();
        _deviceHistoryViewModel = new DeviceHistoryViewModel();
        _stickerDesignerViewModel = new StickerDesignerViewModel();

        ShowHomeCommand = new RelayCommand(_ => TrySetCurrentView(HomeSection, _homeViewModel, "الرئيسية"), _ => BlueMax.Presentation.Wpf.App.AccessControl.CanAccess(HomeSection));
        ShowCertificatesCommand = new RelayCommand(_ => TrySetCurrentView(CertificatesSection, _certificatesViewModel, "Certificates"), _ => BlueMax.Presentation.Wpf.App.AccessControl.CanAccess(CertificatesSection));
        ShowClientsCommand = new RelayCommand(_ => TrySetCurrentView(ClientsSection, _clientsViewModel, "العملاء"), _ => BlueMax.Presentation.Wpf.App.AccessControl.CanAccess(ClientsSection));
        ShowMaintenanceCommand = new RelayCommand(_ => TrySetCurrentView(MaintenanceSection, _maintenanceViewModel, "الصيانة"), _ => BlueMax.Presentation.Wpf.App.AccessControl.CanAccess(MaintenanceSection));
        ShowInventoryCommand = new RelayCommand(_ => TrySetCurrentView(InventorySection, _inventoryViewModel, "المخزون"), _ => BlueMax.Presentation.Wpf.App.AccessControl.CanAccess(InventorySection));
        ShowSettingsCommand = new RelayCommand(_ => TrySetCurrentView(SettingsSection, _settingsViewModel, "الإعدادات"));
        ShowDeviceHistoryCommand = new RelayCommand(_ => TrySetCurrentView(DeviceHistorySection, _deviceHistoryViewModel, "سجل الجهاز"), _ => BlueMax.Presentation.Wpf.App.AccessControl.CanAccess(DeviceHistorySection));
        ShowStickerDesignerCommand = new RelayCommand(_ => TrySetCurrentView(StickerDesignerSection, _stickerDesignerViewModel, "مصمم الملصق"), _ => BlueMax.Presentation.Wpf.App.AccessControl.CanAccess(StickerDesignerSection));

        SearchCertificateCommand = new RelayCommand(_ => NavigateToCertificates(QuickActionMode.Search), _ => BlueMax.Presentation.Wpf.App.AccessControl.CanAccess(CertificatesSection));
        PrintStickerCommand = new RelayCommand(_ => NavigateToCertificates(QuickActionMode.PrintSticker), _ => BlueMax.Presentation.Wpf.App.AccessControl.CanAccess(CertificatesSection));
        ExpiredDevicesCommand = new RelayCommand(_ => NavigateToCertificates(QuickActionMode.ExpiredDevices), _ => BlueMax.Presentation.Wpf.App.AccessControl.CanAccess(CertificatesSection));

        BlueMax.Presentation.Wpf.App.AccessControl.Changed += (_, __) =>
        {
            ShowHomeCommand.RaiseCanExecuteChanged();
            ShowCertificatesCommand.RaiseCanExecuteChanged();
            ShowClientsCommand.RaiseCanExecuteChanged();
            ShowMaintenanceCommand.RaiseCanExecuteChanged();
            ShowInventoryCommand.RaiseCanExecuteChanged();
            ShowDeviceHistoryCommand.RaiseCanExecuteChanged();
            ShowStickerDesignerCommand.RaiseCanExecuteChanged();
            SearchCertificateCommand.RaiseCanExecuteChanged();
            PrintStickerCommand.RaiseCanExecuteChanged();
            ExpiredDevicesCommand.RaiseCanExecuteChanged();
            OnPropertyChanged(nameof(CanAccessStickerDesigner));
        };

        SetCurrentView(_homeViewModel, "الرئيسية");
    }

    public ViewModelBase? CurrentView
    {
        get => _currentView;
        set
        {
            if (SetProperty(ref _currentView, value))
            {
                OnPropertyChanged(nameof(IsSidebarVisible));
            }
        }
    }

    public string CurrentTitle
    {
        get => _currentTitle;
        set => SetProperty(ref _currentTitle, value);
    }

    public RelayCommand ShowHomeCommand { get; }
    public RelayCommand ShowCertificatesCommand { get; }
    public RelayCommand ShowClientsCommand { get; }
    public RelayCommand ShowMaintenanceCommand { get; }
    public RelayCommand ShowInventoryCommand { get; }
    public RelayCommand ShowSettingsCommand { get; }
    public RelayCommand ShowDeviceHistoryCommand { get; }
    public RelayCommand ShowStickerDesignerCommand { get; }
    public RelayCommand SearchCertificateCommand { get; }
    public RelayCommand PrintStickerCommand { get; }
    public RelayCommand ExpiredDevicesCommand { get; }

    public bool CanAccessStickerDesigner => BlueMax.Presentation.Wpf.App.AccessControl.CanAccess(StickerDesignerSection);

    public bool IsSidebarVisible
    {
        get => _currentView is HomeViewModel;
    }

    void TrySetCurrentView(string sectionCode, ViewModelBase viewModel, string title)
    {
        if (!BlueMax.Presentation.Wpf.App.AccessControl.EnsureAccess(sectionCode, title))
            return;
        SetCurrentView(viewModel, title);
    }

    void SetCurrentView(ViewModelBase viewModel, string title)
    {
        try
        {
            BlueMax.Presentation.Wpf.App.Log($"Navigate to: {title} ({viewModel.GetType().Name})");
        }
        catch
        {
        }
        CurrentView = viewModel;
        CurrentTitle = title;
    }

    void NavigateToCertificates(QuickActionMode mode)
    {
        if (!BlueMax.Presentation.Wpf.App.AccessControl.EnsureAccess(CertificatesSection, "Certificates"))
            return;
        _certificatesViewModel.ApplyQuickAction(mode);
        SetCurrentView(_certificatesViewModel, "Certificates");
    }
}
