using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using BlueMax.Infrastructure;
using BlueMax.Domain;
 
namespace BlueMax.Presentation.Wpf.ViewModels;
 
public sealed class DeviceHistoryViewModel : ViewModelBase
{
    string _serialQuery = "";
    public DeviceHistoryViewModel()
    {
        Timeline = new ObservableCollection<DeviceHistoryItem>();
        SearchHistoryCommand = new RelayCommand(_ => SearchHistory());
    }
 
    public ObservableCollection<DeviceHistoryItem> Timeline { get; }
    public string SerialQuery
    {
        get => _serialQuery;
        set => SetProperty(ref _serialQuery, NormalizeToEnglishDigits(value));
    }
 
    public RelayCommand SearchHistoryCommand { get; }
 
    void SearchHistory()
    {
        var q = (SerialQuery ?? "").Trim();
        Timeline.Clear();
        if (string.IsNullOrWhiteSpace(q))
            return;
 
        using var db = CreateDbContext();
        db.Database.EnsureCreated();
 
        var certs = db.Certificates
            .AsNoTracking()
            .Where(c => c.SerialText.Contains(q))
            .Select(c => new DeviceHistoryItem
            {
                Date = c.IssueDate,
                Type = "Calibration",
                Title = string.IsNullOrWhiteSpace(c.CertificateNumber) ? "Certificate" : $"Certificate {c.CertificateNumber}",
                Notes = $"{c.DeviceType} {c.Brand} {c.Model} • {c.ClientName}",
                Reference = $"CERT:{c.Id}"
            })
            .ToList();
 
        var works = db.WorkOrders
            .AsNoTracking()
            .Where(w => w.SerialNumber.Contains(q))
            .Select(w => new
            {
                Item = new DeviceHistoryItem
                {
                    Date = w.ReceivedDate,
                    Type = "Maintenance",
                    Title = $"WorkOrder #{w.Id} - {w.Status}",
                    Notes = $"{w.DeviceType} {w.Model} • {w.CustomerName}",
                    Reference = $"WO:{w.Id}"
                },
                UpdateDate = w.UpdatedDate
            })
            .ToList();
 
        foreach (var w in works)
        {
            Timeline.Add(w.Item);
            if (w.UpdateDate != default && w.UpdateDate.Date != w.Item.Date.Date)
            {
                Timeline.Add(new DeviceHistoryItem
                {
                    Date = w.UpdateDate,
                    Type = "Maintenance Update",
                    Title = $"WorkOrder #{w.Item.Reference?.Split(':').Last()} - Updated",
                    Notes = w.Item.Notes,
                    Reference = w.Item.Reference ?? string.Empty
                });
            }
        }
 
        foreach (var c in certs)
            Timeline.Add(c);
 
        var sorted = Timeline.OrderByDescending(t => t.Date).ToList();
        Timeline.Clear();
        foreach (var t in sorted)
            Timeline.Add(t);
    }
 
    static AppDbContext CreateDbContext()
    {
        return DbContextFactory.CreateDbContext();
    }

    static string NormalizeToEnglishDigits(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return value ?? "";

        var chars = (value ?? "").ToCharArray();
        for (var i = 0; i < chars.Length; i++)
        {
            var ch = chars[i];
            if (ch >= '\u0660' && ch <= '\u0669')
                chars[i] = (char)('0' + (ch - '\u0660'));
            else if (ch >= '\u06F0' && ch <= '\u06F9')
                chars[i] = (char)('0' + (ch - '\u06F0'));
        }

        return new string(chars);
    }
}
 
public sealed class DeviceHistoryItem : ViewModelBase
{
    DateTime _date;
    string _type = "";
    string _title = "";
    string _notes = "";
    string _reference = "";
 
    public DateTime Date
    {
        get => _date;
        set
        {
            if (!SetProperty(ref _date, value))
                return;
            OnPropertyChanged(nameof(DateDisplay));
        }
    }

    public string DateDisplay => Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
 
    public string Type
    {
        get => _type;
        set => SetProperty(ref _type, value);
    }
 
    public string Title
    {
        get => _title;
        set => SetProperty(ref _title, value);
    }
 
    public string Notes
    {
        get => _notes;
        set => SetProperty(ref _notes, value);
    }
 
    public string Reference
    {
        get => _reference;
        set => SetProperty(ref _reference, value);
    }
}
