using System;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;
using BlueMax.Infrastructure;
using BlueMax.Presentation.Wpf.ViewModels;
using BlueMax.Presentation.Wpf.Services;
using BlueMax.Presentation.Wpf.Resources;
using System.Reflection;
using System.Collections.Generic;
using System.Windows.Controls;
using System.Windows.Media;

namespace BlueMax.Presentation.Wpf;

public partial class MainWindow : Window
{
    private bool _isDarkMode = false;
    private bool _isSidebarVisible = false;

    public MainWindow()
    {
        InitializeComponent();
        this.DataContext = new MainViewModel();
        Visibility = Visibility.Visible;
        var asm = Assembly.GetExecutingAssembly();
        var product = asm.GetCustomAttribute<AssemblyProductAttribute>()?.Product ?? "Calibration Certificates";
        var infoVer = asm.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        if (string.IsNullOrWhiteSpace(infoVer))
            infoVer = asm.GetName().Version?.ToString();
        Title = string.IsNullOrWhiteSpace(infoVer) ? product : $"{product} v{infoVer}";

        WindowHelper.ApplyDarkTitleBar(this);
        
        // Initialize language service
        LanguageService.Instance.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(LanguageService.CurrentCulture))
            {
                UpdateUIForLanguage();
            }
        };
        
        // Set initial flow direction
        this.FlowDirection = LanguageService.Instance.IsRTL ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;
    }

    private void UpdateUIForLanguage()
    {
        this.FlowDirection = LanguageService.Instance.IsRTL ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;
        
        // Force UI refresh for views
        if (DataContext is MainViewModel vm)
        {
            var currentView = vm.CurrentView;
            if (currentView != null)
            {
                vm.CurrentView = null;
                vm.CurrentView = currentView;
            }
        }
    }

    void OnNavHomeClick(object sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel vm)
        {
            vm.ShowHomeCommand.Execute(null);
            
            // Always show sidebar for Home (لوحة التحكم)
            _isSidebarVisible = true;
            var sidebarBorder = this.FindName("SidebarBorder") as Border;
            if (sidebarBorder != null)
            {
                sidebarBorder.Visibility = Visibility.Visible;
            }
            var sidebarColumn = this.FindName("SidebarColumn") as ColumnDefinition;
            if (sidebarColumn != null)
            {
                sidebarColumn.Width = new GridLength(240);
            }
        }
    }

    void OnNavCertificatesClick(object sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel vm)
        {
            vm.ShowCertificatesCommand.Execute(null);
            
            // Hide sidebar for other sections
            _isSidebarVisible = false;
            var sidebarBorder = this.FindName("SidebarBorder") as Border;
            if (sidebarBorder != null)
            {
                sidebarBorder.Visibility = Visibility.Collapsed;
            }
            var sidebarColumn = this.FindName("SidebarColumn") as ColumnDefinition;
            if (sidebarColumn != null)
            {
                sidebarColumn.Width = new GridLength(0);
            }
        }
    }

    void OnNavMaintenanceClick(object sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel vm)
        {
            vm.ShowMaintenanceCommand.Execute(null);
            
            // Hide sidebar for other sections
            _isSidebarVisible = false;
            var sidebarBorder = this.FindName("SidebarBorder") as Border;
            if (sidebarBorder != null)
            {
                sidebarBorder.Visibility = Visibility.Collapsed;
            }
            var sidebarColumn = this.FindName("SidebarColumn") as ColumnDefinition;
            if (sidebarColumn != null)
            {
                sidebarColumn.Width = new GridLength(0);
            }
        }
    }

    void OnNavClientsClick(object sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel vm)
        {
            vm.ShowClientsCommand.Execute(null);
            
            // Hide sidebar for other sections
            _isSidebarVisible = false;
            var sidebarBorder = this.FindName("SidebarBorder") as Border;
            if (sidebarBorder != null)
            {
                sidebarBorder.Visibility = Visibility.Collapsed;
            }
            var sidebarColumn = this.FindName("SidebarColumn") as ColumnDefinition;
            if (sidebarColumn != null)
            {
                sidebarColumn.Width = new GridLength(0);
            }
        }
    }

    void OnNavInventoryClick(object sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel vm)
        {
            vm.ShowInventoryCommand.Execute(null);
            
            // Hide sidebar for other sections
            _isSidebarVisible = false;
            var sidebarBorder = this.FindName("SidebarBorder") as Border;
            if (sidebarBorder != null)
            {
                sidebarBorder.Visibility = Visibility.Collapsed;
            }
            var sidebarColumn = this.FindName("SidebarColumn") as ColumnDefinition;
            if (sidebarColumn != null)
            {
                sidebarColumn.Width = new GridLength(0);
            }
        }
    }

    void OnNavDeviceHistoryClick(object sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel vm)
        {
            vm.ShowDeviceHistoryCommand.Execute(null);
            
            // Hide sidebar for other sections
            _isSidebarVisible = false;
            var sidebarBorder = this.FindName("SidebarBorder") as Border;
            if (sidebarBorder != null)
            {
                sidebarBorder.Visibility = Visibility.Collapsed;
            }
            var sidebarColumn = this.FindName("SidebarColumn") as ColumnDefinition;
            if (sidebarColumn != null)
            {
                sidebarColumn.Width = new GridLength(0);
            }
        }
    }

    void OnNavStickerClick(object sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel vm)
        {
            vm.ShowStickerDesignerCommand.Execute(null);
            
            // Hide sidebar for other sections
            _isSidebarVisible = false;
            var sidebarBorder = this.FindName("SidebarBorder") as Border;
            if (sidebarBorder != null)
            {
                sidebarBorder.Visibility = Visibility.Collapsed;
            }
            var sidebarColumn = this.FindName("SidebarColumn") as ColumnDefinition;
            if (sidebarColumn != null)
            {
                sidebarColumn.Width = new GridLength(0);
            }
        }
    }

    void OnNavRentalsClick(object sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel vm)
        {
            vm.ShowRentalsCommand.Execute(null);
            
            // Hide sidebar for other sections
            _isSidebarVisible = false;
            var sidebarBorder = this.FindName("SidebarBorder") as Border;
            if (sidebarBorder != null)
            {
                sidebarBorder.Visibility = Visibility.Collapsed;
            }
            var sidebarColumn = this.FindName("SidebarColumn") as ColumnDefinition;
            if (sidebarColumn != null)
            {
                sidebarColumn.Width = new GridLength(0);
            }
        }
    }

    void OnToggleSidebarClick(object sender, RoutedEventArgs e)
    {
        _isSidebarVisible = !_isSidebarVisible;
        var sidebarBorder = this.FindName("SidebarBorder") as Border;
        if (sidebarBorder != null)
        {
            sidebarBorder.Visibility = _isSidebarVisible ? Visibility.Visible : Visibility.Collapsed;
        }
        var sidebarColumn = this.FindName("SidebarColumn") as ColumnDefinition;
        if (sidebarColumn != null)
        {
            sidebarColumn.Width = new GridLength(_isSidebarVisible ? 240 : 0);
        }
    }

    void OnNavSettingsClick(object sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel vm)
        {
            try { App.Log("OnNavSettingsClick invoked"); } catch {}
            vm.ShowSettingsCommand.Execute(null);
            
            // Always show sidebar for Settings
            _isSidebarVisible = true;
            var sidebarBorder = this.FindName("SidebarBorder") as Border;
            if (sidebarBorder != null)
            {
                sidebarBorder.Visibility = Visibility.Visible;
            }
            var sidebarColumn = this.FindName("SidebarColumn") as ColumnDefinition;
            if (sidebarColumn != null)
            {
                sidebarColumn.Width = new GridLength(240);
            }
        }
    }

    void OnNavLoginClick(object sender, RoutedEventArgs e)
    {
        ShowLoginDialog();
    }

    void OnNavSearchClick(object sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel vm)
            vm.SearchCertificateCommand.Execute(null);
    }

    void OnHelpClick(object sender, RoutedEventArgs e)
    {
        ShowHelpDialog();
    }

    void ShowHelpDialog()
    {
        try
        {
            var helpWindow = new BlueMax.Presentation.Wpf.Views.HelpWindow
            {
                Owner = this,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };
            
            if (helpWindow.DataContext is BlueMax.Presentation.Wpf.ViewModels.HelpViewModel viewModel)
            {
                viewModel.SetOwnerWindow(helpWindow);
            }
            
            helpWindow.ShowDialog();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"خطأ في فتح نافذة المساعدة: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    void OnNotificationsClick(object sender, RoutedEventArgs e)
    {
        ShowNotificationsDialog();
    }

    void ShowNotificationsDialog()
    {
        try
        {
            var notifications = new List<string>();
            
            // Check for expired rentals
            try
            {
                using var db = BlueMax.Infrastructure.DbContextFactory.CreateDbContext();
                var expiredRentals = db.Rentals
                    .Where(r => r.EndDate < DateTime.Today && r.Status == "نشط")
                    .ToList();
                
                if (expiredRentals.Any())
                {
                    notifications.Add($"تنبيه: هناك {expiredRentals.Count} إيجار منتهي (تاريخ الانتهاء أقل من اليوم)");
                }
            }
            catch
            {
            }

            // Check for devices needing calibration
            try
            {
                using var db = BlueMax.Infrastructure.DbContextFactory.CreateDbContext();
                var devicesNeedingCalibration = db.Certificates
                    .Where(c => c.ExpiryDate < DateTime.Today.AddDays(7))
                    .ToList();
                
                if (devicesNeedingCalibration.Any())
                {
                    notifications.Add($"تنبيه: هناك {devicesNeedingCalibration.Count} شهادة ستنتهي خلال 7 أيام");
                }
            }
            catch
            {
            }

            if (!notifications.Any())
            {
                notifications.Add("لا توجد إشعارات جديدة");
            }

            var list = new ListBox
            {
                Width = 400,
                Height = 250,
                ItemsSource = notifications,
                Margin = new Thickness(0, 10, 0, 0),
                FontSize = 13,
                BorderThickness = new Thickness(1),
                BorderBrush = new SolidColorBrush(Color.FromRgb(229, 231, 235))
            };

            var close = new Button
            {
                Content = "إغلاق",
                Width = 90,
                IsCancel = true,
                Margin = new Thickness(0, 10, 0, 0),
                Background = new SolidColorBrush(Color.FromRgb(22, 160, 133)),
                Foreground = Brushes.White,
                BorderBrush = new SolidColorBrush(Color.FromRgb(22, 160, 133))
            };

            var root = new StackPanel
            {
                Margin = new Thickness(18),
                FlowDirection = FlowDirection.RightToLeft
            };
            root.Children.Add(new TextBlock { Text = "الإشعارات", FontSize = 16, FontWeight = FontWeights.Bold, Margin = new Thickness(0, 0, 0, 10) });
            root.Children.Add(list);
            root.Children.Add(close);

            var window = new Window
            {
                Title = "الإشعارات",
                Content = root,
                Width = 450,
                Height = 350,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                ResizeMode = ResizeMode.NoResize,
                WindowStyle = WindowStyle.ToolWindow,
                Owner = this,
                Topmost = true,
                FlowDirection = FlowDirection.RightToLeft,
                Background = Brushes.White
            };

            close.Click += (_, __) => window.Close();
            window.ShowDialog();
        }
        catch
        {
        }
    }

    void OnToggleThemeClick(object sender, RoutedEventArgs e)
    {
        _isDarkMode = !_isDarkMode;

        if (_isDarkMode)
        {
            // Dark Mode
            this.Resources["SurfaceBackground"] = new SolidColorBrush(Color.FromRgb(75, 85, 99));
            this.Resources["SidebarDarkBackground"] = new SolidColorBrush(Color.FromRgb(45, 27, 78));
            this.Resources["TextPrimary"] = new SolidColorBrush(Color.FromRgb(255, 255, 255));
            this.Resources["CardBackground"] = new SolidColorBrush(Color.FromRgb(55, 65, 81));
            this.Resources["BorderLight"] = new SolidColorBrush(Color.FromRgb(75, 85, 99));
        }
        else
        {
            // Light Mode
            this.Resources["SurfaceBackground"] = new SolidColorBrush(Color.FromRgb(243, 244, 246));
            this.Resources["SidebarDarkBackground"] = new SolidColorBrush(Color.FromRgb(45, 27, 78));
            this.Resources["TextPrimary"] = new SolidColorBrush(Color.FromRgb(31, 41, 55));
            this.Resources["CardBackground"] = new SolidColorBrush(Color.FromRgb(255, 255, 255));
            this.Resources["BorderLight"] = new SolidColorBrush(Color.FromRgb(229, 231, 235));
        }
    }

    void OnLanguageClick(object sender, RoutedEventArgs e)
    {
        LanguageService.Instance.ToggleLanguage();
    }

    void OnNavPrintStickerClick(object sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel vm)
            vm.PrintStickerCommand.Execute(null);
    }

    void OnNavExpiredClick(object sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel vm)
            vm.ExpiredDevicesCommand.Execute(null);
    }

    void OnNavHistoryClick(object sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel vm)
            vm.ShowDeviceHistoryCommand.Execute(null);
    }

    void OnExitClick(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void MinimizeButton_Click(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
    }

    private void MaximizeButton_Click(object sender, RoutedEventArgs e)
    {
        if (WindowState == WindowState.Maximized)
        {
            WindowState = WindowState.Normal;
        }
        else
        {
            WindowState = WindowState.Maximized;
        }
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    void ShowLoginDialog()
    {
        try
        {
            var access = BlueMax.Presentation.Wpf.App.AccessControl;
            var users = access.GetUsernames();
            var items = new List<string> { "المدير" };
            items.AddRange(users);

            var combo = new ComboBox
            {
                Width = 260,
                ItemsSource = items,
                SelectedIndex = 0,
                Margin = new Thickness(0, 6, 0, 0),
                FontSize = 14
            };

            var lastUser = access.LastUserName;
            if (!string.IsNullOrWhiteSpace(lastUser) && items.Contains(lastUser))
                combo.SelectedItem = lastUser;

            var passwordBox = new PasswordBox
            {
                Width = 260,
                Margin = new Thickness(0, 6, 0, 0),
                FontSize = 14
            };

            var ok = new Button
            {
                Content = "دخول",
                Width = 90,
                Margin = new Thickness(0, 0, 8, 0),
                IsDefault = true,
                Background = new SolidColorBrush(Color.FromRgb(22, 160, 133)),
                Foreground = Brushes.White,
                BorderBrush = new SolidColorBrush(Color.FromRgb(22, 160, 133))
            };

            var cancel = new Button
            {
                Content = "إلغاء",
                Width = 90,
                IsCancel = true
            };

            var buttons = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Left,
                Margin = new Thickness(0, 14, 0, 0)
            };
            buttons.Children.Add(ok);
            buttons.Children.Add(cancel);

            var root = new StackPanel
            {
                Margin = new Thickness(18),
                FlowDirection = FlowDirection.RightToLeft
            };
            root.Children.Add(new TextBlock { Text = "اسم المستخدم", FontSize = 13 });
            root.Children.Add(combo);
            root.Children.Add(new TextBlock { Text = "كلمة المرور", FontSize = 13, Margin = new Thickness(0, 8, 0, 0) });
            root.Children.Add(passwordBox);
            root.Children.Add(buttons);

            var window = new Window
            {
                Title = "تسجيل الدخول",
                Content = root,
                SizeToContent = SizeToContent.WidthAndHeight,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                ResizeMode = ResizeMode.NoResize,
                WindowStyle = WindowStyle.ToolWindow,
                Owner = this,
                Topmost = true,
                FlowDirection = FlowDirection.RightToLeft,
                Background = Brushes.White
            };

            ok.Click += (_, __) =>
            {
                var selected = combo.SelectedItem?.ToString() ?? "";
                var password = passwordBox.Password ?? "";
                if (string.IsNullOrWhiteSpace(selected) || string.IsNullOrWhiteSpace(password))
                    return;

                var okLogin = false;
                if (string.Equals(selected, "المدير", StringComparison.OrdinalIgnoreCase))
                    okLogin = access.TryLoginAsAdminWithPin(password);
                else
                    okLogin = access.TryLoginUser(selected, password);

                if (!okLogin)
                {
                    MessageBox.Show("بيانات الدخول غير صحيحة.", "خطأ", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                window.DialogResult = true;
                window.Close();
            };

            passwordBox.Loaded += (_, __) => passwordBox.Focus();
            window.ShowDialog();
        }
        catch
        {
        }
    }
}
