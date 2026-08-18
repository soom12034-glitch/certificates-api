using System.ComponentModel;
using BlueMax.Presentation.Wpf.Resources;
using BlueMax.Presentation.Wpf.Services;

namespace BlueMax.Presentation.Wpf.ViewModels;

public class TranslationViewModel : INotifyPropertyChanged
{
    private static TranslationViewModel? _instance;
    public static TranslationViewModel Instance => _instance ??= new TranslationViewModel();

    public event PropertyChangedEventHandler? PropertyChanged;

    public TranslationViewModel()
    {
        LanguageService.Instance.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(LanguageService.CurrentCulture))
            {
                RefreshAll();
            }
        };
    }

    public string Home => Translations.Get("Home");
    public string Certificates => Translations.Get("Certificates");
    public string Maintenance => Translations.Get("Maintenance");
    public string Clients => Translations.Get("Clients");
    public string Inventory => Translations.Get("Inventory");
    public string DeviceHistory => Translations.Get("DeviceHistory");
    public string StickerDesigner => Translations.Get("StickerDesigner");
    public string ReceiptStickerDesigner => Translations.Get("ReceiptStickerDesigner");
    public string Settings => Translations.Get("Settings");
    public string Rentals => Translations.Get("Rentals");
    public string Dashboard => Translations.Get("Dashboard");
    public string QuickActions => Translations.Get("QuickActions");
    public string TotalCertificates => Translations.Get("TotalCertificates");
    public string TotalClients => Translations.Get("TotalClients");
    public string NewCertificate => Translations.Get("NewCertificate");
    public string NewRental => Translations.Get("NewRental");
    public string NewClient => Translations.Get("NewClient");
    public string ViewReports => Translations.Get("ViewReports");
    public string CertificatesLog => Translations.Get("CertificatesLog");
    public string TemplateSettings => Translations.Get("TemplateSettings");
    public string SearchCertificate => Translations.Get("SearchCertificate");
    public string ExpiredDevices => Translations.Get("ExpiredDevices");
    public string Statistics => Translations.Get("Statistics");
    public string MaintenanceRecords => Translations.Get("MaintenanceRecords");
    public string LowStock => Translations.Get("LowStock");
    public string LastCertificates => Translations.Get("LastCertificates");
    public string LastMaintenances => Translations.Get("LastMaintenances");
    public string AddMaintenance => Translations.Get("AddMaintenance");
    public string Back => Translations.Get("Back");
    public string Print => Translations.Get("Print");
    public string Search => Translations.Get("Search");
    public string StickerPreview => Translations.Get("StickerPreview");
    public string StickerSettings => Translations.Get("StickerSettings");
    public string HelpSystem => Translations.Get("HelpSystem");
    public string HelpPrintGuide => Translations.Get("HelpPrintGuide");
    public string HelpExportPdf => Translations.Get("HelpExportPdf");
    public string ActivationTitle => Translations.Get("ActivationTitle");
    public string ProductActivation => Translations.Get("ProductActivation");
    public string HardwareFingerprint => Translations.Get("HardwareFingerprint");
    public string ActivationCode => Translations.Get("ActivationCode");
    public string Activate => Translations.Get("Activate");
    
    // Certificates View Fields
    public string ClientData => Translations.Get("ClientData");
    public string ClientNameField => Translations.Get("ClientNameField");
    public string PhoneNumber => Translations.Get("PhoneNumber");
    public string DelegateName => Translations.Get("DelegateName");
    public string DeviceData => Translations.Get("DeviceData");
    public string DeviceType => Translations.Get("DeviceType");
    public string Brand => Translations.Get("Brand");
    public string Model => Translations.Get("Model");
    public string MainSerial => Translations.Get("MainSerial");
    public string FirstSerial => Translations.Get("FirstSerial");
    public string SecondSerial => Translations.Get("SecondSerial");
    public string Accuracy => Translations.Get("Accuracy");
    public string DeviationRate => Translations.Get("DeviationRate");
    public string CertificateData => Translations.Get("CertificateData");
    public string CertificateNumber => Translations.Get("CertificateNumber");
    public string IssueDate => Translations.Get("IssueDate");
    public string ExpiryDate => Translations.Get("ExpiryDate");
    public string TemplateType => Translations.Get("TemplateType");
    public string SaveCertificate => Translations.Get("SaveCertificate");
    public string PrintCertificateWord => Translations.Get("PrintCertificateWord");
    public string PrintCertificatePdf => Translations.Get("PrintCertificatePdf");
    public string ExportPDF => Translations.Get("ExportPDF");
    public string UploadToCloud => Translations.Get("UploadToCloud");
    public string AutoUpload => Translations.Get("AutoUpload");
    public string DownloadCloudBackup => Translations.Get("DownloadCloudBackup");
    public string UploadCloudBackup => Translations.Get("UploadCloudBackup");
    public string CloudConnectionStatus => Translations.Get("CloudConnectionStatus");
    public string OfflineStatus => Translations.Get("OfflineStatus");
    public string CloudBackupList => Translations.Get("CloudBackupList");
    public string CloudBackupUnavailable => Translations.Get("CloudBackupUnavailable");
    public string VerificationLink => Translations.Get("VerificationLink");
    public string CopyVerificationLink => Translations.Get("CopyVerificationLink");
    public string OpenVerificationLink => Translations.Get("OpenVerificationLink");
    public string PreviewCertificate => Translations.Get("PreviewCertificate");
    public string Preview => Translations.Get("Preview");
    public string PrintStickerButton => Translations.Get("PrintStickerButton");
    public string TemplateDocTypeStep => Translations.Get("TemplateDocTypeStep");
    public string TemplateChooseStep => Translations.Get("TemplateChooseStep");
    public string DocumentType => Translations.Get("DocumentType");
    public string CurrentPath => Translations.Get("CurrentPath");
    public string ChooseWordTemplate => Translations.Get("ChooseWordTemplate");
    public string SaveTemplateSetting => Translations.Get("SaveTemplateSetting");
    public string TemplateTagsDictionary => Translations.Get("TemplateTagsDictionary");
    public string DescriptionLabel => Translations.Get("DescriptionLabel");
    public string TagLabel => Translations.Get("TagLabel");
    public string SaveAsTemplate => Translations.Get("SaveAsTemplate");
    public string ContentHeaderCompanyName => Translations.Get("ContentHeaderCompanyName");
    public string ContentHeaderAddress => Translations.Get("ContentHeaderAddress");
    public string ContentHeaderPhone => Translations.Get("ContentHeaderPhone");
    public string LogoSettings => Translations.Get("LogoSettings");
    public string UseLogoText => Translations.Get("UseLogoText");
    public string PickLogo => Translations.Get("PickLogo");
    public string AddItems => Translations.Get("AddItems");
    public string AddTextItem => Translations.Get("AddTextItem");
    public string AddLineItem => Translations.Get("AddLineItem");
    public string DeleteSelectedItem => Translations.Get("DeleteSelectedItem");
    public string SelectedItemProperties => Translations.Get("SelectedItemProperties");
    public string ItemNameLabel => Translations.Get("ItemNameLabel");
    public string PositionX => Translations.Get("PositionX");
    public string PositionY => Translations.Get("PositionY");
    public string WidthLabel => Translations.Get("WidthLabel");
    public string HeightLabel => Translations.Get("HeightLabel");
    public string RotationLabel => Translations.Get("RotationLabel");
    public string FontSizeLabel => Translations.Get("FontSizeLabel");
    public string FontColorLabel => Translations.Get("FontColorLabel");
    public string FontFamilyLabel => Translations.Get("FontFamilyLabel");
    public string BoldLabel => Translations.Get("BoldLabel");
    public string ItalicLabel => Translations.Get("ItalicLabel");
    public string UnderlineLabel => Translations.Get("UnderlineLabel");
    public string AlignmentLabel => Translations.Get("AlignmentLabel");
    public string QrSizeMm => Translations.Get("QrSizeMm");
    public string ActionsTab => Translations.Get("ActionsTab");
    public string TextLabel => Translations.Get("TextLabel");
    public string BindToVariableLabel => Translations.Get("BindToVariableLabel");
    public string ZIndexLabel => Translations.Get("ZIndexLabel");
    public string ItemVisibility => Translations.Get("ItemVisibility");
    public string CurrentStatus => Translations.Get("CurrentStatus");
    public string LoginAs => Translations.Get("LoginAs");
    public string LoginAsAdmin => Translations.Get("LoginAsAdmin");
    public string LogoutAdmin => Translations.Get("LogoutAdmin");
    public string LogoutUser => Translations.Get("LogoutUser");
    public string UserManagement => Translations.Get("UserManagement");
    public string UserLabel => Translations.Get("UserLabel");
    public string AddUserButton => Translations.Get("AddUserButton");
    public string DeleteUserButton => Translations.Get("DeleteUserButton");
    public string ChangePasswordButton => Translations.Get("ChangePasswordButton");
    public string SavePermissions => Translations.Get("SavePermissions");
    public string AdminPin => Translations.Get("AdminPin");
    public string AdminPinNote => Translations.Get("AdminPinNote");
    public string ChangeAdminPin => Translations.Get("ChangeAdminPin");
    public string LicenseTab => Translations.Get("LicenseTab");
    public string LicenseInfo => Translations.Get("LicenseInfo");
    public string LicenseStatusLabel => Translations.Get("LicenseStatusLabel");
    public string LicenseExpiryLabel => Translations.Get("LicenseExpiryLabel");
    public string HardwareIdLabel => Translations.Get("HardwareIdLabel");
    public string CopyButton => Translations.Get("CopyButton");
    public string HardwareIdHint => Translations.Get("HardwareIdHint");
    public string LicenseUpdate => Translations.Get("LicenseUpdate");
    public string EnterNewKey => Translations.Get("EnterNewKey");
    public string ActivateLicense => Translations.Get("ActivateLicense");

    // Shared print/preview windows
    public string RefreshPreview => Translations.Get("RefreshPreview");
    public string PrintReceiptWindow => Translations.Get("PrintReceiptWindow");
    public string CloseReceipt => Translations.Get("CloseReceipt");
    public string UseLetterhead => Translations.Get("UseLetterhead");
    public string VerticalOffset => Translations.Get("VerticalOffset");
    public string HorizontalOffset => Translations.Get("HorizontalOffset");
    public string HeaderMargin => Translations.Get("HeaderMargin");
    public string FooterMargin => Translations.Get("FooterMargin");
    public string FrameColor => Translations.Get("FrameColor");
    public string ZoomIn => Translations.Get("ZoomIn");
    public string ZoomOut => Translations.Get("ZoomOut");
    public string FitWidth => Translations.Get("FitWidth");
    public string FitPage => Translations.Get("FitPage");
    public string DeviceReceipt => Translations.Get("DeviceReceipt");
    public string RentalDeviceReceipt => Translations.Get("RentalDeviceReceipt");
    public string Printer => Translations.Get("Printer");
    public string SelectPrinter => Translations.Get("SelectPrinter");
    public string ClientAccountStatement => Translations.Get("ClientAccountStatement");

    // Sticker/receipt preview fields
    public string MaintenanceSticker => Translations.Get("MaintenanceSticker");
    public string MaintenanceStickerSettings => Translations.Get("MaintenanceStickerSettings");
    public string MaintenanceStickerData => Translations.Get("MaintenanceStickerData");
    public string DeviceSerial => Translations.Get("DeviceSerial");
    public string RealDimensions => Translations.Get("RealDimensions");
    public string StickerWidthMm => Translations.Get("StickerWidthMm");
    public string StickerHeightMm => Translations.Get("StickerHeightMm");
    public string DefaultSize => Translations.Get("DefaultSize");
    public string StickerPrinter => Translations.Get("StickerPrinter");
    public string SelectPrinterButton => Translations.Get("SelectPrinterButton");
    public string ScaleContent => Translations.Get("ScaleContent");
    public string HorizontalOffsetSticker => Translations.Get("HorizontalOffsetSticker");
    public string VerticalOffsetValue => Translations.Get("VerticalOffsetValue");
    public string RealPreview => Translations.Get("RealPreview");
    
    // Settings View Fields
    public string GeneralReports => Translations.Get("GeneralReports");
    public string ReportSettings => Translations.Get("ReportSettings");
    public string PrintBackground => Translations.Get("PrintBackground");
    public string TopMargin => Translations.Get("TopMargin");
    public string BottomMargin => Translations.Get("BottomMargin");
    public string RightMargin => Translations.Get("RightMargin");
    public string LeftMargin => Translations.Get("LeftMargin");
    public string CompanyShort => Translations.Get("CompanyShort");
    public string CompanyNameField => Translations.Get("CompanyNameField");
    public string CompanyPhoneField => Translations.Get("CompanyPhoneField");
    public string CompanyCommercialRecord => Translations.Get("CompanyCommercialRecord");
    public string CompanyTaxNumber => Translations.Get("CompanyTaxNumber");
    public string CompanyNationalAddress => Translations.Get("CompanyNationalAddress");
    public string ContractRepresentativeName => Translations.Get("ContractRepresentativeName");
    public string ContractRepresentativeId => Translations.Get("ContractRepresentativeId");
    public string ContractRepresentativePhone => Translations.Get("ContractRepresentativePhone");
    public string CertNumberPrefix => Translations.Get("CertNumberPrefix");
    public string AppIcon => Translations.Get("AppIcon");
    public string UploadIcon => Translations.Get("UploadIcon");
    public string LibreOfficePath => Translations.Get("LibreOfficePath");
    public string Select => Translations.Get("Select");
    public string UploadHeader => Translations.Get("UploadHeader");
    public string UploadFooter => Translations.Get("UploadFooter");
    public string CompanyLogo => Translations.Get("CompanyLogo");
    public string UploadLogo => Translations.Get("UploadLogo");
    public string ShowLogoInHeader => Translations.Get("ShowLogoInHeader");
    public string SaveReportSettings => Translations.Get("SaveReportSettings");
    public string WebVerification => Translations.Get("WebVerification");
    public string BaseUrl => Translations.Get("BaseUrl");
    public string SaveButton => Translations.Get("SaveButton");
    public string Database => Translations.Get("Database");
    public string CurrentProvider => Translations.Get("CurrentProvider");
    public string ConnectionString => Translations.Get("ConnectionString");
    public string SaveDbSettings => Translations.Get("SaveDbSettings");
    public string PrintersTab => Translations.Get("PrintersTab");
    public string A4PrinterSettings => Translations.Get("A4PrinterSettings");
    public string PrinterName => Translations.Get("PrinterName");
    public string Properties => Translations.Get("Properties");
    public string Preferences => Translations.Get("Preferences");
    public string StickerPrinterSettings => Translations.Get("StickerPrinterSettings");
    public string PrinterModelProtocol => Translations.Get("PrinterModelProtocol");
    public string ConnectionType => Translations.Get("ConnectionType");
    public string Host => Translations.Get("Host");
    public string Port => Translations.Get("Port");
    public string LabelWidth => Translations.Get("LabelWidth");
    public string LabelHeight => Translations.Get("LabelHeight");
    public string Darkness => Translations.Get("Darkness");
    public string Speed => Translations.Get("Speed");
    public string DotsPerMm => Translations.Get("DotsPerMm");
    public string ThermalSafeMode => Translations.Get("ThermalSafeMode");
    public string ThermalCoolingDelay => Translations.Get("ThermalCoolingDelay");
    public string SavePrinterSettings => Translations.Get("SavePrinterSettings");
    public string AccessControlTab => Translations.Get("AccessControlTab");
    
    // Clients View Fields
    public string Name => Translations.Get("Name");
    public string ClientNotes => Translations.Get("ClientNotes");
    public string Address => Translations.Get("Address");
    public string Clear => Translations.Get("Clear");
    public string DeleteButton => Translations.Get("DeleteButton");
    public string Report => Translations.Get("Report");
    public string DeviceMaintenanceHistory => Translations.Get("DeviceMaintenanceHistory");
    public string DateField => Translations.Get("DateField");
    public string DeviceField => Translations.Get("DeviceField");
    public string Cost => Translations.Get("Cost");
    public string CalibrationCertificates => Translations.Get("CalibrationCertificates");
    public string Issue => Translations.Get("Issue");
    public string Expiry => Translations.Get("Expiry");
    public string Refresh => Translations.Get("Refresh");
    public string CompanyClientName => Translations.Get("CompanyClientName");
    public string CalibrationAlertTooltip => Translations.Get("CalibrationAlertTooltip");
    public string ClientsSearchSmartTooltip => Translations.Get("ClientsSearchSmartTooltip");
    public string RefreshCustomersListTooltip => Translations.Get("RefreshCustomersListTooltip");
    
    // Rentals View Fields (Additional)
    public string ExpiredRental => Translations.Get("ExpiredRental");
    public string CreateNewRental => Translations.Get("CreateNewRental");
    public string NewRentalForm => Translations.Get("NewRentalForm");
    public string Company => Translations.Get("Company");
    public string TaxNumberField => Translations.Get("TaxNumberField");
    public string IdNumber => Translations.Get("IdNumber");
    public string Serial2Optional => Translations.Get("Serial2Optional");
    public string StartDateField => Translations.Get("StartDateField");
    public string EndDateField => Translations.Get("EndDateField");
    public string RentalType => Translations.Get("RentalType");
    public string PriceField => Translations.Get("PriceField");
    public string PaidAmount => Translations.Get("PaidAmount");
    public string RemainingAmount => Translations.Get("RemainingAmount");
    public string StatusField => Translations.Get("StatusField");
    public string NewButton => Translations.Get("NewButton");
    public string ActionsSection => Translations.Get("ActionsSection");
    public string PrintRentalReceipt => Translations.Get("PrintRentalReceipt");
    public string CompleteRental => Translations.Get("CompleteRental");
    public string PrintReceipt => Translations.Get("PrintReceipt");
    public string SerialField => Translations.Get("SerialField");
    public string CurrentlyRentedDevices => Translations.Get("CurrentlyRentedDevices");
    public string RentalReport => Translations.Get("RentalReport");
    public string RentalStatistics => Translations.Get("RentalStatistics");
    public string TotalRentals => Translations.Get("TotalRentals");
    public string CurrentlyActive => Translations.Get("CurrentlyActive");
    public string ExpiredStatus => Translations.Get("ExpiredStatus");
    public string TotalIncome => Translations.Get("TotalIncome");
    public string DailyPrice => Translations.Get("DailyPrice");
    public string MonthlyPrice => Translations.Get("MonthlyPrice");
    public string DeviceValue => Translations.Get("DeviceValue");
    public string RentalContract => Translations.Get("RentalContract");
    public string PrintRentalContract => Translations.Get("PrintRentalContract");
    public string ExportContractWord => Translations.Get("ExportContractWord");
    public string FirstParty => Translations.Get("FirstParty");
    public string SecondParty => Translations.Get("SecondParty");
    public string RepresentativeOf => Translations.Get("RepresentativeOf");
    public string HandoverReceiptButton => Translations.Get("HandoverReceiptButton");
    public string ReceiveReceiptTitle => Translations.Get("ReceiveReceiptTitle");
    public string ReturnReceiptTitle => Translations.Get("ReturnReceiptTitle");
    public string ExpiringSoonRentals => Translations.Get("ExpiringSoonRentals");
    
    // Maintenance View Fields
    public string MaintenanceOrders => Translations.Get("MaintenanceOrders");
    public string MaintenanceOrder => Translations.Get("MaintenanceOrder");
    public string SerialNumber1 => Translations.Get("SerialNumber1");
    public string Accessories => Translations.Get("Accessories");
    public string AccessoriesField => Translations.Get("AccessoriesField");
    public string Complaint => Translations.Get("Complaint");
    public string TechnicalReport => Translations.Get("TechnicalReport");
    public string PartsCost => Translations.Get("PartsCost");
    public string LaborCost => Translations.Get("LaborCost");
    public string Total => Translations.Get("Total");
    public string ReceiptDate => Translations.Get("ReceiptDate");
    public string UpdateDate => Translations.Get("UpdateDate");
    public string AddDeviceForSameCustomer => Translations.Get("AddDeviceForSameCustomer");
    public string AddDeviceTooltip => Translations.Get("AddDeviceTooltip");
    public string Edit => Translations.Get("Edit");
    public string EditRename => Translations.Get("EditRename");
    public string OpenMenuItem => Translations.Get("Open");
    public string ShowPrice => Translations.Get("ShowPrice");
    public string ShowPriceTooltip => Translations.Get("ShowPriceTooltip");
    public string IndividualReceipt => Translations.Get("IndividualReceipt");
    public string MaintenanceReport => Translations.Get("MaintenanceReport");
    public string MaintenanceReportTooltip => Translations.Get("MaintenanceReportTooltip");
    public string PreviewTooltip => Translations.Get("PreviewTooltip");
    public string ReceiptSticker => Translations.Get("ReceiptSticker");
    public string ReceiptStickerTooltip => Translations.Get("ReceiptStickerTooltip");
    public string MaintenanceRequests => Translations.Get("MaintenanceRequests");
    public string ReceiptNumber => Translations.Get("ReceiptNumber");
    public string ReceiptStickerTab => Translations.Get("ReceiptStickerTab");
    public string PrintReceiptSticker => Translations.Get("PrintReceiptSticker");
    public string ReceiptStickerNote => Translations.Get("ReceiptStickerNote");
    public string StickerSize => Translations.Get("StickerSize");
    public string ReceiptStickerSettings => Translations.Get("ReceiptStickerSettings");
    public string PrintNow => Translations.Get("PrintNow");
    public string ReceiptStickerExtraNote => Translations.Get("ReceiptStickerExtraNote");
    public string MaintenanceReportsTab => Translations.Get("MaintenanceReportsTab");
    public string ReportsFrom => Translations.Get("ReportsFrom");
    public string ReportsTo => Translations.Get("ReportsTo");
    public string TotalOrders => Translations.Get("TotalOrders");
    public string OrdersInProgress => Translations.Get("OrdersInProgress");
    public string CompletedDelivered => Translations.Get("CompletedDelivered");
    public string TotalPartsCost => Translations.Get("TotalPartsCost");
    public string TotalLaborCost => Translations.Get("TotalLaborCost");
    public string StatusBreakdown => Translations.Get("StatusBreakdown");
    public string MonthlyBreakdown => Translations.Get("MonthlyBreakdown");
    public string ExportPdf => Translations.Get("ExportPdf");
    public string ExportExcel => Translations.Get("ExportExcel");
    public string MonthColumn => Translations.Get("MonthColumn");
    public string OrdersCount => Translations.Get("OrdersCount");
    public string TotalCostColumn => Translations.Get("TotalCostColumn");
    public string NoReportsData => Translations.Get("NoReportsData");
    
    // Inventory View Fields
    public string AddEditSparePart => Translations.Get("AddEditSparePart");
    public string PartCodeSKU => Translations.Get("PartCodeSKU");
    public string ItemNameField => Translations.Get("ItemNameField");
    public string CategoryField => Translations.Get("CategoryField");
    public string BrandField => Translations.Get("BrandField");
    public string CurrentQuantity => Translations.Get("CurrentQuantity");
    public string MinimumThreshold => Translations.Get("MinimumThreshold");
    public string CostPrice => Translations.Get("CostPrice");
    public string SellingPrice => Translations.Get("SellingPrice");
    public string PurchaseDate => Translations.Get("PurchaseDate");
    public string LowStockAlert => Translations.Get("LowStockAlert");
    public string LowStockAlertEnd => Translations.Get("LowStockAlertEnd");
    public string ShowLowStockOnly => Translations.Get("ShowLowStockOnly");
    public string SearchRefresh => Translations.Get("SearchRefresh");
    public string SearchTooltip => Translations.Get("SearchTooltip");
    public string PrintReport => Translations.Get("PrintReport");
    public string Code => Translations.Get("Code");
    public string NameField => Translations.Get("NameField");
    public string BrandColumn => Translations.Get("BrandColumn");
    
    // Device History View Fields
    public string SerialSearch => Translations.Get("SerialSearch");
    public string DeviceHistoryField => Translations.Get("DeviceHistoryField");
    public string HistoryType => Translations.Get("HistoryType");
    public string HistoryTitle => Translations.Get("HistoryTitle");
    
    // Sticker Designer View Fields
    public string PageTab => Translations.Get("PageTab");
    public string WidthMm => Translations.Get("WidthMm");
    public string PreviewScale => Translations.Get("PreviewScale");
    public string HeightMm => Translations.Get("HeightMm");
    public string GridTab => Translations.Get("GridTab");
    public string ShowPreviewGrid => Translations.Get("ShowPreviewGrid");
    public string SnapToGrid => Translations.Get("SnapToGrid");
    public string EnableMouseEditing => Translations.Get("EnableMouseEditing");
    public string GridStepMm => Translations.Get("GridStepMm");
    public string HeaderTab => Translations.Get("HeaderTab");
    public string UseLogo => Translations.Get("UseLogo");
    public string HeaderTitle => Translations.Get("HeaderTitle");
    public string StickerCompanyName => Translations.Get("StickerCompanyName");
    public string StickerAddress => Translations.Get("StickerAddress");
    public string StickerPhone => Translations.Get("StickerPhone");
    public string HeaderFontSize => Translations.Get("HeaderFontSize");
    public string ItemTab => Translations.Get("ItemTab");
    public string XPosition => Translations.Get("XPosition");
    public string YPosition => Translations.Get("YPosition");
    public string ItemFontSize => Translations.Get("ItemFontSize");
    public string ContentTab => Translations.Get("ContentTab");
    public string DeviceTypeName => Translations.Get("DeviceTypeName");
    public string StickerBrand => Translations.Get("StickerBrand");
    public string SerialNumberField => Translations.Get("SerialNumberField");
    public string StickerCalibrationDate => Translations.Get("StickerCalibrationDate");
    public string StickerExpiryDate => Translations.Get("StickerExpiryDate");
    public string QrAndActions => Translations.Get("QrAndActions");
    public string ShowQrInPreview => Translations.Get("ShowQrInPreview");
    public string SaveLayout => Translations.Get("SaveLayout");
    public string ResetLayout => Translations.Get("ResetLayout");
    public string TestPrint => Translations.Get("TestPrint");
    public string QrLabel => Translations.Get("QrLabel");
    public string GenerateAndSaveQr => Translations.Get("GenerateAndSaveQr");
    public string DetectStickerPrinter => Translations.Get("DetectStickerPrinter");
    public string DetectedPrinters => Translations.Get("DetectedPrinters");
    public string ApplySelectedPrinter => Translations.Get("ApplySelectedPrinter");
    public string PrinterDriver => Translations.Get("PrinterDriver");
    public string PrinterPort => Translations.Get("PrinterPort");
    public string PrinterDpi => Translations.Get("PrinterDpi");
    public string PrinterProtocolHeader => Translations.Get("PrinterProtocolHeader");
    public string ThermalPrinterType => Translations.Get("ThermalPrinterType");
    public string StandardPrinterType => Translations.Get("StandardPrinterType");
    public string NoPrinterSelected => Translations.Get("NoPrinterSelected");
    public string PrinterDetectedMessage => Translations.Get("PrinterDetectedMessage");
    public string ThermalHint => Translations.Get("ThermalHint");

    // Backup Settings Fields
    public string Backup => Translations.Get("Backup");
    public string BackupSettings => Translations.Get("BackupSettings");
    public string BackupPath => Translations.Get("BackupPath");
    public string DefaultBackupPath => Translations.Get("DefaultBackupPath");
    public string BackupEncryption => Translations.Get("BackupEncryption");
    public string EnableBackupEncryption => Translations.Get("EnableBackupEncryption");
    public string EncryptionPassword => Translations.Get("EncryptionPassword");
    public string GeneratePassword => Translations.Get("GeneratePassword");
    public string EncryptionPasswordNote => Translations.Get("EncryptionPasswordNote");
    public string AutoBackup => Translations.Get("AutoBackup");
    public string EnableAutoBackup => Translations.Get("EnableAutoBackup");
    public string BackupSchedule => Translations.Get("BackupSchedule");
    public string Daily => Translations.Get("Daily");
    public string Weekly => Translations.Get("Weekly");
    public string Monthly => Translations.Get("Monthly");
    public string BackupTime => Translations.Get("BackupTime");
    public string MaxBackupCount => Translations.Get("MaxBackupCount");
    public string SaveBackupSettings => Translations.Get("SaveBackupSettings");
    public string LocalBackup => Translations.Get("LocalBackup");
    public string CreateBackup => Translations.Get("CreateBackup");
    public string RestoreBackup => Translations.Get("RestoreBackup");
    public string BackupList => Translations.Get("BackupList");
    public string Delete => Translations.Get("Delete");
    public string Change => Translations.Get("Change");

    // Maintenance Preview Window Fields
    public string MaintenanceOrderPreview => Translations.Get("MaintenanceOrderPreview");
    public string ClientDataPreview => Translations.Get("ClientDataPreview");
    public string ClientNamePreview => Translations.Get("ClientNamePreview");
    public string PhonePreview => Translations.Get("PhonePreview");
    public string DeviceDataPreview => Translations.Get("DeviceDataPreview");
    public string DeviceTypePreview => Translations.Get("DeviceTypePreview");
    public string ModelPreview => Translations.Get("ModelPreview");
    public string SerialNumberPreview => Translations.Get("SerialNumberPreview");
    public string AccessoriesPreview => Translations.Get("AccessoriesPreview");
    public string StatusAndDates => Translations.Get("StatusAndDates");
    public string StatusFieldPreview => Translations.Get("StatusFieldPreview");
    public string ReceivedDatePreview => Translations.Get("ReceivedDatePreview");
    public string UpdatedDatePreview => Translations.Get("UpdatedDatePreview");
    public string TotalPreview => Translations.Get("TotalPreview");
    public string ComplaintPreview => Translations.Get("ComplaintPreview");
    public string TechnicalReportPreview => Translations.Get("TechnicalReportPreview");
    public string Close => Translations.Get("Close");


    // Central accessor for new keys without adding a named property.
    public string this[string key] => Translations.Get(key);

    // Re-notifies every translation property when the language changes.
    private void RefreshAll()
    {
        foreach (var key in Translations.Keys)
        {
            OnPropertyChanged(key);
        }
    }

    protected void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
