using BlueMax.Infrastructure;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace BlueMax.Presentation.Wpf.ViewModels;

public sealed class HomeViewModel : ViewModelBase
{
    private int _totalCertificates;
    private int _totalClients;
    private int _totalWorkOrders;
    private int _totalInventoryItems;
    private int _pendingWorkOrders;
    private int _lowStockItems;

    public ObservableCollection<RecentCertificate> RecentCertificates { get; } = new();
    public ObservableCollection<RecentWorkOrder> RecentWorkOrders { get; } = new();

    public int TotalCertificates
    {
        get => _totalCertificates;
        set => SetProperty(ref _totalCertificates, value);
    }

    public int TotalClients
    {
        get => _totalClients;
        set => SetProperty(ref _totalClients, value);
    }

    public int TotalWorkOrders
    {
        get => _totalWorkOrders;
        set => SetProperty(ref _totalWorkOrders, value);
    }

    public int TotalInventoryItems
    {
        get => _totalInventoryItems;
        set => SetProperty(ref _totalInventoryItems, value);
    }

    public int PendingWorkOrders
    {
        get => _pendingWorkOrders;
        set => SetProperty(ref _pendingWorkOrders, value);
    }

    public int LowStockItems
    {
        get => _lowStockItems;
        set => SetProperty(ref _lowStockItems, value);
    }

    public HomeViewModel()
    {
        try
        {
            LoadStatistics();
            LoadRecentData();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading statistics: {ex.Message}");
        }
    }

    private void LoadStatistics()
    {
        try
        {
            using var db = DbContextFactory.CreateDbContext();
            db.Database.EnsureCreated();

            TotalCertificates = db.Certificates.Count();
            TotalClients = db.Customers.Count();
            TotalWorkOrders = db.WorkOrders.Count();
            TotalInventoryItems = db.SpareParts.Count();
            PendingWorkOrders = db.WorkOrders.Count(w => w.Status == "Open" || w.Status == "In Progress");
            LowStockItems = db.SpareParts.Count(s => s.Quantity <= s.MinThreshold);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error in LoadStatistics: {ex.Message}");
        }
    }

    private void LoadRecentData()
    {
        try
        {
            using var db = DbContextFactory.CreateDbContext();
            db.Database.EnsureCreated();

            // Load recent certificates
            var recentCerts = db.Certificates
                .OrderByDescending(c => c.IssueDate)
                .Take(5)
                .Select(c => new RecentCertificate
                {
                    CertificateNumber = c.CertificateNumber,
                    ClientName = c.ClientName ?? "غير معروف",
                    DeviceType = c.DeviceType,
                    IssueDate = c.IssueDate
                })
                .ToList();

            RecentCertificates.Clear();
            foreach (var cert in recentCerts)
            {
                RecentCertificates.Add(cert);
            }

            // Load recent work orders
            var recentOrders = db.WorkOrders
                .OrderByDescending(w => w.ReceivedDate)
                .Take(5)
                .Select(w => new RecentWorkOrder
                {
                    ReceiptNumber = w.ReceiptGroupNumber ?? w.Id.ToString(),
                    CustomerName = w.CustomerName,
                    Status = w.Status,
                    ReceivedDate = w.ReceivedDate
                })
                .ToList();

            RecentWorkOrders.Clear();
            foreach (var order in recentOrders)
            {
                RecentWorkOrders.Add(order);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error in LoadRecentData: {ex.Message}");
        }
    }
}

public class RecentCertificate
{
    public string CertificateNumber { get; set; } = string.Empty;
    public string ClientName { get; set; } = string.Empty;
    public string DeviceType { get; set; } = string.Empty;
    public DateTime IssueDate { get; set; }
}

public class RecentWorkOrder
{
    public string ReceiptNumber { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime ReceivedDate { get; set; }
}
