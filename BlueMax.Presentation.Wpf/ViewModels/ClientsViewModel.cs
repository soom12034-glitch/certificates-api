using System;
using System.IO;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Markup;
using System.Windows.Media;
using Microsoft.EntityFrameworkCore;
using BlueMax.Domain;
using BlueMax.Infrastructure;

namespace BlueMax.Presentation.Wpf.ViewModels;

public sealed class ClientsViewModel : ViewModelBase
{
    string _customerNameInput = "";
    string _customerPhoneInput = "";
    string _customerAddressInput = "";
    string _customerNotesInput = "";
    string _customerSearchText = "";
    CustomerItem? _selectedCustomer;

    public ClientsViewModel()
    {
        Customers = new ObservableCollection<CustomerItem>();
        WorkOrders = new ObservableCollection<WorkOrder>();
        Certificates = new ObservableCollection<Certificate>();
        
        NewCustomerCommand = new RelayCommand(_ => ClearInputs());
        SaveCustomerCommand = new RelayCommand(_ => SaveCustomer());
        DeleteCustomerCommand = new RelayCommand(_ => DeleteCustomer(), _ => SelectedCustomer != null);
        RefreshCustomersCommand = new RelayCommand(_ => RefreshCustomers());
        PrintReportCommand = new RelayCommand(_ => PrintClientReport(), _ => SelectedCustomer != null);

        try { RefreshCustomers(); } catch { }
    }

    public ObservableCollection<CustomerItem> Customers { get; }
    public ObservableCollection<WorkOrder> WorkOrders { get; }
    public ObservableCollection<Certificate> Certificates { get; }

    public CustomerItem? SelectedCustomer
    {
        get => _selectedCustomer;
        set
        {
            if (!SetProperty(ref _selectedCustomer, value))
                return;
            if (value == null)
            {
                ClearInputs();
                return;
            }
            CustomerNameInput = value.Name;
            CustomerPhoneInput = value.Phone;
            CustomerAddressInput = value.Address;
            CustomerNotesInput = value.Notes;
            DeleteCustomerCommand.RaiseCanExecuteChanged();
            PrintReportCommand.RaiseCanExecuteChanged();
            
            LoadCustomerHistory();
        }
    }

    public string CustomerNameInput { get => _customerNameInput; set => SetProperty(ref _customerNameInput, value); }
    public string CustomerPhoneInput { get => _customerPhoneInput; set => SetProperty(ref _customerPhoneInput, value); }
    public string CustomerAddressInput { get => _customerAddressInput; set => SetProperty(ref _customerAddressInput, value); }
    public string CustomerNotesInput { get => _customerNotesInput; set => SetProperty(ref _customerNotesInput, value); }
    public string CustomerSearchText { get => _customerSearchText; set => SetProperty(ref _customerSearchText, value); }

    public RelayCommand NewCustomerCommand { get; }
    public RelayCommand SaveCustomerCommand { get; }
    public RelayCommand DeleteCustomerCommand { get; }
    public RelayCommand RefreshCustomersCommand { get; }
    public RelayCommand PrintReportCommand { get; }

    void SaveCustomer()
    {
        if (string.IsNullOrWhiteSpace(CustomerNameInput))
        {
            MessageBox.Show("اسم العميل مطلوب", "خطأ", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        using var db = CreateDbContext();
        db.Database.EnsureCreated();

        if (SelectedCustomer != null)
        {
            var entity = db.Customers.FirstOrDefault(c => c.Id == SelectedCustomer.Id);
            if (entity != null)
            {
                entity.Name = CustomerNameInput;
                entity.Phone = CustomerPhoneInput;
                entity.Address = CustomerAddressInput;
                entity.Notes = CustomerNotesInput;
                db.SaveChanges();
                RefreshCustomers();
            }
            return;
        }

        var customer = new Customer
        {
            CreatedAt = DateTime.Now,
            Name = CustomerNameInput,
            Phone = CustomerPhoneInput,
            Address = CustomerAddressInput,
            Notes = CustomerNotesInput
        };
        db.Customers.Add(customer);
        db.SaveChanges();
        RefreshCustomers();
        ClearInputs();
    }

    void DeleteCustomer()
    {
        if (SelectedCustomer == null)
            return;
        using var db = CreateDbContext();
        db.Database.EnsureCreated();
        var entity = db.Customers.FirstOrDefault(c => c.Id == SelectedCustomer.Id);
        if (entity != null)
        {
            db.Customers.Remove(entity);
            db.SaveChanges();
        }
        SelectedCustomer = null;
        RefreshCustomers();
    }

    void ClearInputs()
    {
        CustomerNameInput = "";
        CustomerPhoneInput = "";
        CustomerAddressInput = "";
        CustomerNotesInput = "";
        SelectedCustomer = null;
        WorkOrders.Clear();
        Certificates.Clear();
        DeleteCustomerCommand.RaiseCanExecuteChanged();
        PrintReportCommand.RaiseCanExecuteChanged();
    }

    void RefreshCustomers()
    {
        try
        {
            using var db = CreateDbContext();
            db.Database.EnsureCreated();
            var query = db.Customers.AsNoTracking();
            if (!string.IsNullOrWhiteSpace(CustomerSearchText))
            {
                var text = CustomerSearchText.Trim();
                query = query.Where(c => c.Name.Contains(text) || c.Phone.Contains(text));
            }

            var items = query
                .OrderByDescending(c => c.CreatedAt)
                .Take(300)
                .Select(c => new CustomerItem
                {
                    Id = c.Id,
                    Name = c.Name,
                    Phone = c.Phone,
                    Address = c.Address,
                    Notes = c.Notes
                })
                .ToList();

            // Calculate alert for each customer
            var cutoff = DateTime.Today.AddDays(30);
            var activeCertificates = db.Certificates.AsNoTracking().Where(c => c.ExpiryDate <= cutoff).ToList();

            foreach (var item in items)
            {
                var hasAlert = activeCertificates.Any(c => c.ClientName == item.Name || c.Phone == item.Phone);
                item.HasCalibrationAlert = hasAlert;
            }

            Customers.Clear();
            foreach (var item in items)
                Customers.Add(item);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"RefreshCustomers failed: {ex}");
        }
    }

    void LoadCustomerHistory()
    {
        if (SelectedCustomer == null) return;

        try
        {
            using var db = CreateDbContext();
            
            // Load Work Orders
            var wos = db.WorkOrders.AsNoTracking()
                        .Where(w => w.CustomerId == SelectedCustomer.Id || w.CustomerName == SelectedCustomer.Name)
                        .OrderByDescending(w => w.ReceivedDate)
                        .ToList();
            WorkOrders.Clear();
            foreach (var w in wos) WorkOrders.Add(w);

            // Load Certificates
            var certs = db.Certificates.AsNoTracking()
                          .Where(c => c.ClientName == SelectedCustomer.Name || c.Phone == SelectedCustomer.Phone)
                          .OrderByDescending(c => c.IssueDate)
                          .ToList();
            Certificates.Clear();
            foreach (var c in certs) Certificates.Add(c);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"LoadCustomerHistory failed: {ex}");
        }
    }

    void PrintClientReport()
    {
        if (SelectedCustomer == null) return;
        try
        {
            var win = new BlueMax.Presentation.Wpf.Views.ClientReportWindow(
                SelectedCustomer, 
                WorkOrders.ToList(), 
                Certificates.ToList())
            {
                Owner = System.Windows.Application.Current?.MainWindow
            };
            win.ShowDialog();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Report Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    static AppDbContext CreateDbContext()
    {
        return DbContextFactory.CreateDbContext();
    }
}

public sealed class CustomerItem : ViewModelBase
{
    int _id;
    string _name = "";
    string _phone = "";
    string _address = "";
    string _notes = "";
    bool _hasCalibrationAlert;

    public int Id
    {
        get => _id;
        set => SetProperty(ref _id, value);
    }

    public string Name
    {
        get => _name;
        set => SetProperty(ref _name, value);
    }

    public string Phone
    {
        get => _phone;
        set => SetProperty(ref _phone, value);
    }

    public string Address
    {
        get => _address;
        set => SetProperty(ref _address, value);
    }

    public string Notes
    {
        get => _notes;
        set => SetProperty(ref _notes, value);
    }

    public bool HasCalibrationAlert
    {
        get => _hasCalibrationAlert;
        set => SetProperty(ref _hasCalibrationAlert, value);
    }
}
