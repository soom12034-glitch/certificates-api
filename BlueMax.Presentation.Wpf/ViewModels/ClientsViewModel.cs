using System;
using System.IO;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Markup;
using System.Windows.Media;
using Microsoft.EntityFrameworkCore;
using BlueMax.Domain;
using BlueMax.Infrastructure;
using BlueMax.Presentation.Wpf.Services;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using System.Diagnostics;

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
            MessageBox.Show(T("ClientNameRequired"), T("Error"), MessageBoxButton.OK, MessageBoxImage.Warning);
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

    async void RefreshCustomers()
    {
        if (IsBusy) return;
        SetBusy(T("LoadingData"));
        try
        {
            var items = await Task.Run(() =>
            {
                using var db = CreateDbContext();
                db.Database.EnsureCreated();
                var query = db.Customers.AsNoTracking();
                if (!string.IsNullOrWhiteSpace(CustomerSearchText))
                {
                    var text = CustomerSearchText.Trim();
                    query = query.Where(c => c.Name.Contains(text) || c.Phone.Contains(text));
                }

                var list = query
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

                foreach (var item in list)
                {
                    var hasAlert = activeCertificates.Any(c => SameCustomer(c.ClientName, c.Phone, item.Name, item.Phone));
                    item.HasCalibrationAlert = hasAlert;
                }

                return list;
            });

            Customers.Clear();
            foreach (var item in items)
                Customers.Add(item);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"RefreshCustomers failed: {ex}");
        }
        finally
        {
            SetIdle();
        }
    }

    void LoadCustomerHistory()
    {
        if (SelectedCustomer == null) return;

        try
        {
            using var db = CreateDbContext();
            
            // Load Work Orders (linked by CustomerId first, legacy unlinked rows by name/phone)
            var wos = db.WorkOrders.AsNoTracking()
                        .Where(w => w.CustomerId == SelectedCustomer.Id || w.CustomerName == SelectedCustomer.Name)
                        .OrderByDescending(w => w.ReceivedDate)
                        .ToList()
                        .Where(w => w.CustomerId == SelectedCustomer.Id ||
                                    (w.CustomerId == null && SameCustomer(w.CustomerName, w.CustomerPhone, SelectedCustomer.Name, SelectedCustomer.Phone)))
                        .ToList();
            WorkOrders.Clear();
            foreach (var w in wos) WorkOrders.Add(w);

            // Load Certificates (pre-filter by name/phone, then match precisely in memory
            // so records that only share a first name or phone are not mixed together)
            var certs = db.Certificates.AsNoTracking()
                          .Where(c => c.ClientName == SelectedCustomer.Name || c.Phone == SelectedCustomer.Phone)
                          .OrderByDescending(c => c.IssueDate)
                          .ToList()
                          .Where(c => SameCustomer(c.ClientName, c.Phone, SelectedCustomer.Name, SelectedCustomer.Phone))
                          .ToList();
            Certificates.Clear();
            foreach (var c in certs) Certificates.Add(c);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"LoadCustomerHistory failed: {ex}");
        }
    }

    // A certificate/work order belongs to the selected customer only when the names
    // match AND the phones match whenever both are known. If either side has no phone
    // we fall back to name-only so nothing is lost, but two different clients that
    // merely share a name are never mixed together.
    static bool SameCustomer(string certName, string certPhone, string customerName, string customerPhone)
    {
        var n1 = (certName ?? "").Trim();
        var p1 = (certPhone ?? "").Trim();
        var n2 = (customerName ?? "").Trim();
        var p2 = (customerPhone ?? "").Trim();
        if (!string.Equals(n1, n2, StringComparison.OrdinalIgnoreCase))
            return false;
        if (string.IsNullOrWhiteSpace(p1) || string.IsNullOrWhiteSpace(p2))
            return true;
        return string.Equals(p1, p2, StringComparison.OrdinalIgnoreCase);
    }

    async void PrintClientReport()
    {
        if (SelectedCustomer == null) return;
        SetBusy(T("ExportPdf"));
        try
        {
            var pdfPath = await Task.Run(() => BuildClientStatementPdf());
            if (!string.IsNullOrWhiteSpace(pdfPath) && File.Exists(pdfPath))
                Process.Start(new ProcessStartInfo(pdfPath) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Report Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            SetIdle();
        }
    }

    string BuildClientStatementPdf()
    {
        var dir = AppPaths.ReportsOutput;
        var pdfPath = Path.Combine(dir, "ClientStatement_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".pdf");

        QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;
        QuestPdfFonts.EnsureCairoRegistered();

        var report = new ReportDesignerSettingsStore().Load();
        var companyName = report.CompanyName?.Trim() ?? "";
        var companyHeader = report.CompanyHeader?.Trim() ?? "";
        var companyAddress = report.CompanyAddress?.Trim() ?? "";
        var companyPhone = report.CompanyPhone?.Trim() ?? "";
        var hasLogo = report.ShowLogo && !string.IsNullOrWhiteSpace(report.LogoPath) && File.Exists(report.LogoPath);
        var reportNumber = "CST-" + DateTime.Now.ToString("yyyyMMddHHmmss", System.Globalization.CultureInfo.InvariantCulture);
        var generatedOn = DateTime.Now.ToString("yyyy-MM-dd HH:mm", System.Globalization.CultureInfo.InvariantCulture);

        var customer = SelectedCustomer;
        var customerName = customer?.Name ?? "";
        var customerPhone = customer?.Phone ?? "";
        var customerAddress = customer?.Address ?? "";
        var totalMaintenanceCost = WorkOrders.Sum(w => w.TotalCost);

        Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(595, 842);
                page.Margin(1.5f, QuestPDF.Infrastructure.Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontFamily("Cairo").FontSize(9).FontColor("#1F2937"));

                page.Header().Element(h => h.Column(letterhead =>
                {
                    letterhead.Item().Row(row =>
                    {
                        if (hasLogo)
                        {
                            row.ConstantItem(92).Element(logo =>
                            {
                                try
                                {
                                    logo.Width(92).Height(64).AlignLeft().AlignTop().Image(report.LogoPath!);
                                }
                                catch
                                {
                                }
                            });
                        }
                        row.RelativeItem().PaddingTop(2).Column(info =>
                        {
                            info.Spacing(2);
                            if (companyName.Length > 0)
                                info.Item().AlignRight().Text(companyName).DirectionFromRightToLeft().Bold().FontSize(17).FontColor("#0F3A5F");
                            if (companyHeader.Length > 0)
                                info.Item().AlignRight().Text(companyHeader).DirectionFromRightToLeft().FontSize(11).FontColor("#374151");
                            var contact = new System.Collections.Generic.List<(string Label, string Value)>();
                            if (companyAddress.Length > 0)
                                contact.Add(("", companyAddress));
                            if (companyPhone.Length > 0)
                                contact.Add((T("PhoneNumber"), companyPhone));
                            if (contact.Count > 0)
                                info.Item().Element(c => BuildHeaderContactLine(c, contact));
                        });
                    });
                    letterhead.Item().PaddingTop(4).Height(3).Background("#0F3A5F");
                    letterhead.Item().PaddingTop(1).Height(1).Background("#D1D5DB");
                }));

                page.Content().Element(content =>
                {
                    content.Column(col =>
                    {
                        col.Spacing(6);

                        col.Item().PaddingTop(2).AlignCenter().Text(T("ClientAccountStatement")).DirectionFromRightToLeft().Bold().FontSize(16).FontColor("#0F3A5F");
                        col.Item().AlignCenter().Text(customerName).DirectionFromRightToLeft().Bold().FontSize(12).FontColor("#374151");
                        col.Item().Row(metaRow =>
                        {
                            metaRow.RelativeItem();
                            metaRow.AutoItem().Text(generatedOn).DirectionFromLeftToRight().FontSize(8).FontColor("#6B7280");
                            metaRow.AutoItem().Text(T("ReportsGeneratedOn") + ": ").DirectionFromRightToLeft().FontSize(8).FontColor("#6B7280");
                            metaRow.AutoItem().Text("    |    ").FontSize(8).FontColor("#6B7280");
                            metaRow.AutoItem().Text(reportNumber).DirectionFromLeftToRight().FontSize(8).FontColor("#6B7280");
                            metaRow.AutoItem().Text(T("ReportNumberLabel") + ": ").DirectionFromRightToLeft().FontSize(8).FontColor("#6B7280");
                        });

                        col.Item().PaddingTop(4).Row(meta =>
                        {
                            meta.Spacing(6);
                            meta.RelativeItem().Element(c => BuildClientMetaBox(c, T("ClientNameLabel"), customerName));
                            meta.RelativeItem().Element(c => BuildClientMetaBox(c, T("PhoneNumber"), customerPhone));
                        });
                        col.Item().PaddingTop(4).Row(meta2 =>
                        {
                            meta2.Spacing(6);
                            meta2.RelativeItem().Element(c => BuildClientMetaBox(c, T("Address"), customerAddress));
                            meta2.RelativeItem().Element(c => BuildClientMetaBox(c, T("ReportDate"), DateTime.Now.ToString("yyyy-MM-dd")));
                        });

                        col.Item().PaddingTop(4).Row(summary =>
                        {
                            summary.Spacing(6);
                            summary.RelativeItem().Element(c => BuildClientSummaryCard(c, T("MaintenanceOrdersCount"), WorkOrders.Count.ToString(System.Globalization.CultureInfo.InvariantCulture)));
                            summary.RelativeItem().Element(c => BuildClientSummaryCard(c, T("TotalMaintenanceCost"), totalMaintenanceCost.ToString("N2", System.Globalization.CultureInfo.InvariantCulture)));
                            summary.RelativeItem().Element(c => BuildClientSummaryCard(c, T("CertificatesCount"), Certificates.Count.ToString(System.Globalization.CultureInfo.InvariantCulture)));
                        });

                        col.Item().PaddingTop(4).Text(T("MaintenanceHistory")).DirectionFromRightToLeft().Bold().FontSize(11).FontColor("#0F3A5F");
                        col.Item().Table(BuildClientWorkOrdersTable);

                        col.Item().PaddingTop(4).Text(T("CertificatesHistory")).DirectionFromRightToLeft().Bold().FontSize(11).FontColor("#0F3A5F");
                        col.Item().Table(BuildClientCertificatesTable);

                        col.Item().PaddingTop(8).EnsureSpace(100).Row(signatures =>
                        {
                            signatures.Spacing(14);
                            signatures.RelativeItem().Element(c => BuildClientSignatureBox(c, T("SignatureCustomer")));
                            signatures.RelativeItem().Element(c => BuildClientSignatureBox(c, T("SignatureDepartmentManager")));
                        });
                    });
                });

                page.Footer().Element(f => f.Column(footerCol =>
                {
                    footerCol.Item().LineHorizontal(0.5f).LineColor("#CBD5E1");
                    footerCol.Item().PaddingTop(3).Row(footerRow =>
                    {
                        footerRow.RelativeItem().AlignLeft().Text(x => SpanOf(x, companyName).FontSize(8).FontColor("#6B7280"));
                        footerRow.RelativeItem().AlignRight().DefaultTextStyle(s => s.FontSize(8).FontColor("#6B7280")).Text(x =>
                        {
                            x.Span(T("PageLabel") + " ").DirectionFromRightToLeft();
                            x.CurrentPageNumber();
                        });
                    });
                }));
            });
        }).GeneratePdf(pdfPath);

        return pdfPath;
    }

    void BuildClientMetaBox(QuestPDF.Infrastructure.IContainer container, string label, string value)
    {
        container.Border(0.75f).BorderColor("#CBD5E1").Background("#F8FAFC").Padding(6).Column(c =>
        {
            c.Item().Text(label).DirectionFromRightToLeft().FontSize(8).FontColor("#374151");
            c.Item().PaddingTop(2).Text(x => SpanOf(x, value).FontSize(10).Bold().FontColor("#0F3A5F"));
        });
    }

    void BuildClientSummaryCard(QuestPDF.Infrastructure.IContainer container, string label, string value)
    {
        container.Border(0.75f).BorderColor("#CBD5E1").Background("#F8FAFC").Padding(6).Column(c =>
        {
            c.Item().AlignCenter().Text(label).DirectionFromRightToLeft().FontSize(8).FontColor("#374151");
            c.Item().AlignCenter().PaddingTop(2).Text(value).FontSize(12).Bold().FontColor("#0F3A5F");
        });
    }

    void BuildClientTableHeaderCell(QuestPDF.Infrastructure.IContainer container, string text)
    {
        container.Border(0.5f).BorderColor("#0F3A5F").Background("#0F3A5F").Padding(4).AlignCenter().Text(text).DirectionFromRightToLeft().Bold().FontSize(9).FontColor("#FFFFFF");
    }

    void BuildClientWorkOrdersTable(QuestPDF.Fluent.TableDescriptor table)
    {
        table.ColumnsDefinition(c =>
        {
            c.RelativeColumn(1.2f);
            c.RelativeColumn(2);
            c.RelativeColumn(3);
            c.RelativeColumn(1.2f);
        });
        table.Header(h =>
        {
            BuildClientTableHeaderCell(h.Cell(), T("DateColumn"));
            BuildClientTableHeaderCell(h.Cell(), T("Device"));
            BuildClientTableHeaderCell(h.Cell(), T("Complaint"));
            BuildClientTableHeaderCell(h.Cell(), T("TotalCostColumn"));
        });
        foreach (var w in WorkOrders)
        {
            var deviceText = string.Join(" ", new[] { w.DeviceType, w.Brand, w.Model }.Where(x => !string.IsNullOrWhiteSpace(x)));
            table.Cell().Border(0.5f).BorderColor("#E5E7EB").Padding(2).AlignCenter().Text(w.ReceivedDate.ToString("yyyy-MM-dd")).FontSize(9);
            table.Cell().Border(0.5f).BorderColor("#E5E7EB").Padding(2).Text(deviceText).DirectionFromRightToLeft().FontSize(9);
            table.Cell().Border(0.5f).BorderColor("#E5E7EB").Padding(2).Text(w.Complaint).DirectionFromRightToLeft().FontSize(9);
            table.Cell().Border(0.5f).BorderColor("#E5E7EB").Padding(2).AlignRight().Text(w.TotalCost.ToString("N2", System.Globalization.CultureInfo.InvariantCulture)).FontSize(9);
        }
        table.Cell().Border(0.5f).BorderColor("#0F3A5F").Background("#E2E8F0").Padding(2).Text(T("Total")).DirectionFromRightToLeft().Bold().FontSize(9).FontColor("#0F3A5F");
        table.Cell().Border(0.5f).BorderColor("#0F3A5F").Background("#E2E8F0").Padding(2).Text("");
        table.Cell().Border(0.5f).BorderColor("#0F3A5F").Background("#E2E8F0").Padding(2).Text("");
        table.Cell().Border(0.5f).BorderColor("#0F3A5F").Background("#E2E8F0").Padding(2).AlignRight().Text(WorkOrders.Sum(w => w.TotalCost).ToString("N2", System.Globalization.CultureInfo.InvariantCulture)).Bold().FontSize(9).FontColor("#0F3A5F");
    }

    void BuildClientCertificatesTable(QuestPDF.Fluent.TableDescriptor table)
    {
        table.ColumnsDefinition(c =>
        {
            c.RelativeColumn(1.3f);
            c.RelativeColumn(2.2f);
            c.RelativeColumn(1.2f);
            c.RelativeColumn(1.2f);
        });
        table.Header(h =>
        {
            BuildClientTableHeaderCell(h.Cell(), T("CertificateNumber"));
            BuildClientTableHeaderCell(h.Cell(), T("DeviceSerial"));
            BuildClientTableHeaderCell(h.Cell(), T("IssueDate"));
            BuildClientTableHeaderCell(h.Cell(), T("ExpiryDate"));
        });
        foreach (var c in Certificates)
        {
            var deviceText = string.Join(" ", new[] { c.DeviceType, c.Brand, c.Model, c.SerialText }.Where(x => !string.IsNullOrWhiteSpace(x)));
            table.Cell().Border(0.5f).BorderColor("#E5E7EB").Padding(2).AlignCenter().Text(c.CertificateNumber).DirectionFromRightToLeft().FontSize(9);
            table.Cell().Border(0.5f).BorderColor("#E5E7EB").Padding(2).Text(deviceText).DirectionFromRightToLeft().FontSize(9);
            table.Cell().Border(0.5f).BorderColor("#E5E7EB").Padding(2).AlignCenter().Text(c.IssueDate.ToString("yyyy-MM-dd")).FontSize(9);
            table.Cell().Border(0.5f).BorderColor("#E5E7EB").Padding(2).AlignCenter().Text(c.ExpiryDate.ToString("yyyy-MM-dd")).FontSize(9);
        }
    }

    void BuildClientSignatureBox(QuestPDF.Infrastructure.IContainer container, string title)
    {
        container.Border(0.75f).BorderColor("#9CA3AF").Padding(8).Column(c =>
        {
            c.Item().AlignCenter().Text(title).DirectionFromRightToLeft().Bold().FontSize(11).FontColor("#0F3A5F");
            c.Item().PaddingTop(16).AlignCenter().Text(T("NameLabel") + ":  ........................").DirectionFromRightToLeft().FontSize(9).FontColor("#374151");
            c.Item().PaddingTop(12).AlignCenter().Text(T("SignatureLabel") + ":  ........................").DirectionFromRightToLeft().FontSize(9).FontColor("#374151");
        });
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
