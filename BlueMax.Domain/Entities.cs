using System.ComponentModel.DataAnnotations;

namespace BlueMax.Domain;

/// <summary>
/// Represents a calibration certificate issued for a device.
/// Contains device information, client details, and calibration results.
/// </summary>
public class Certificate
{
    /// <summary>
    /// Unique identifier for the certificate.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// The certificate number used for tracking and verification.
    /// </summary>
    [Required(ErrorMessage = "Certificate number is required")]
    [StringLength(50, ErrorMessage = "Certificate number cannot exceed 50 characters")]
    public string CertificateNumber { get; set; } = "";

    /// <summary>
    /// Name of the client who owns the device being calibrated.
    /// </summary>
    [Required(ErrorMessage = "Client name is required")]
    [StringLength(200, ErrorMessage = "Client name cannot exceed 200 characters")]
    public string ClientName { get; set; } = "";

    /// <summary>
    /// Contact phone number for the client.
    /// </summary>
    [StringLength(50, ErrorMessage = "Phone cannot exceed 50 characters")]
    [Phone(ErrorMessage = "Invalid phone number format")]
    public string Phone { get; set; } = "";

    /// <summary>
    /// Type of device being calibrated (e.g., Total Station, Auto Level).
    /// </summary>
    [Required(ErrorMessage = "Device type is required")]
    [StringLength(100, ErrorMessage = "Device type cannot exceed 100 characters")]
    public string DeviceType { get; set; } = "";

    /// <summary>
    /// Brand/manufacturer of the device.
    /// </summary>
    [StringLength(100, ErrorMessage = "Brand cannot exceed 100 characters")]
    public string Brand { get; set; } = "";

    /// <summary>
    /// Model number of the device.
    /// </summary>
    [StringLength(100, ErrorMessage = "Model cannot exceed 100 characters")]
    public string Model { get; set; } = "";

    /// <summary>
    /// Primary serial number of the device.
    /// </summary>
    [StringLength(100, ErrorMessage = "Serial number cannot exceed 100 characters")]
    public string SerialText { get; set; } = "";

    /// <summary>
    /// Secondary serial number if applicable.
    /// </summary>
    [StringLength(100, ErrorMessage = "Serial number 2 cannot exceed 100 characters")]
    public string SerialText2 { get; set; } = "";

    /// <summary>
    /// Specification value or calibration result.
    /// </summary>
    [StringLength(500, ErrorMessage = "Spec value cannot exceed 500 characters")]
    public string SpecValue { get; set; } = "";

    /// <summary>
    /// Date when the certificate was issued.
    /// </summary>
    [Required(ErrorMessage = "Issue date is required")]
    public DateTime IssueDate { get; set; }

    /// <summary>
    /// Date when the certificate expires and recalibration is required.
    /// </summary>
    [Required(ErrorMessage = "Expiry date is required")]
    public DateTime ExpiryDate { get; set; }

    /// <summary>
    /// Name of the delegate or technician who performed the calibration.
    /// </summary>
    [StringLength(100, ErrorMessage = "Delegate name cannot exceed 100 characters")]
    public string DelegateName { get; set; } = "";

    /// <summary>
    /// Encrypted payload for verification URL generation.
    /// </summary>
    public string PayloadEnc { get; set; } = "";

    /// <summary>
    /// Reference to the template used for document generation.
    /// </summary>
    public int? TemplateId { get; set; }

    /// <summary>
    /// Timestamp when the certificate record was created.
    /// </summary>
    [Required(ErrorMessage = "Created date is required")]
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Concurrency token for optimistic locking.
    /// </summary>
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
}

/// <summary>
/// Represents a customer or client who owns devices for calibration.
/// </summary>
public class Customer
{
    /// <summary>
    /// Unique identifier for the customer.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Name of the customer or company.
    /// </summary>
    [Required(ErrorMessage = "Customer name is required")]
    [StringLength(200, ErrorMessage = "Customer name cannot exceed 200 characters")]
    public string Name { get; set; } = "";

    /// <summary>
    /// Contact phone number for the customer.
    /// </summary>
    [StringLength(50, ErrorMessage = "Phone cannot exceed 50 characters")]
    [Phone(ErrorMessage = "Invalid phone number format")]
    public string Phone { get; set; } = "";

    /// <summary>
    /// Physical address of the customer.
    /// </summary>
    [StringLength(500, ErrorMessage = "Address cannot exceed 500 characters")]
    public string Address { get; set; } = "";

    /// <summary>
    /// Additional notes about the customer.
    /// </summary>
    [StringLength(1000, ErrorMessage = "Notes cannot exceed 1000 characters")]
    public string Notes { get; set; } = "";

    /// <summary>
    /// Timestamp when the customer record was created.
    /// </summary>
    [Required(ErrorMessage = "Created date is required")]
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Represents a maintenance or repair work order for a device.
/// Tracks device information, customer details, and repair costs.
/// </summary>
public class WorkOrder
{
    /// <summary>
    /// Unique identifier for the work order.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Foreign key reference to the customer.
    /// </summary>
    public int? CustomerId { get; set; }

    /// <summary>
    /// Name of the customer (cached for performance).
    /// </summary>
    [Required(ErrorMessage = "Customer name is required")]
    [StringLength(200, ErrorMessage = "Customer name cannot exceed 200 characters")]
    public string CustomerName { get; set; } = "";

    /// <summary>
    /// Phone number of the customer (cached for performance).
    /// </summary>
    [StringLength(50, ErrorMessage = "Customer phone cannot exceed 50 characters")]
    [Phone(ErrorMessage = "Invalid phone number format")]
    public string CustomerPhone { get; set; } = "";

    /// <summary>
    /// Identifier to group multiple work orders (devices) received together into a single receipt.
    /// </summary>
    [StringLength(50, ErrorMessage = "Receipt group number cannot exceed 50 characters")]
    public string ReceiptGroupNumber { get; set; } = "";

    /// <summary>
    /// Type of device being repaired or maintained.
    /// </summary>
    [Required(ErrorMessage = "Device type is required")]
    [StringLength(100, ErrorMessage = "Device type cannot exceed 100 characters")]
    public string DeviceType { get; set; } = "";

    /// <summary>
    /// Brand or manufacturer of the device (e.g. Leica, Trimble, Topcon, etc.)
    /// </summary>
    [StringLength(100, ErrorMessage = "Brand cannot exceed 100 characters")]
    public string Brand { get; set; } = "";

    /// <summary>
    /// Model number of the device.
    /// </summary>
    [StringLength(100, ErrorMessage = "Model cannot exceed 100 characters")]
    public string Model { get; set; } = "";

    /// <summary>
    /// Serial number of the device.
    /// </summary>
    [StringLength(100, ErrorMessage = "Serial number cannot exceed 100 characters")]
    public string SerialNumber { get; set; } = "";

    /// <summary>
    /// Second serial number of the device (e.g. for GPS Rover).
    /// </summary>
    [StringLength(100, ErrorMessage = "Second serial number cannot exceed 100 characters")]
    public string SerialNumber2 { get; set; } = "";

    /// <summary>
    /// List of accessories included with the device.
    /// </summary>
    [StringLength(500, ErrorMessage = "Accessories cannot exceed 500 characters")]
    public string Accessories { get; set; } = "";

    /// <summary>
    /// Description of the problem or complaint reported by the customer.
    /// </summary>
    [StringLength(1000, ErrorMessage = "Complaint cannot exceed 1000 characters")]
    public string Complaint { get; set; } = "";

    /// <summary>
    /// Technical report detailing the diagnosis and repairs performed.
    /// </summary>
    [StringLength(2000, ErrorMessage = "Technical report cannot exceed 2000 characters")]
    public string TechnicalReport { get; set; } = "";

    /// <summary>
    /// Current status of the work order (e.g., Pending, In Progress, Completed).
    /// </summary>
    [Required(ErrorMessage = "Status is required")]
    [StringLength(50, ErrorMessage = "Status cannot exceed 50 characters")]
    public string Status { get; set; } = "";

    /// <summary>
    /// Cost of replacement parts used in the repair.
    /// </summary>
    [Range(0, double.MaxValue, ErrorMessage = "Parts cost must be positive")]
    public decimal PartsCost { get; set; }

    /// <summary>
    /// Labor cost for the repair work.
    /// </summary>
    [Range(0, double.MaxValue, ErrorMessage = "Labor cost must be positive")]
    public decimal LaborCost { get; set; }

    /// <summary>
    /// Total cost (parts + labor) for the work order.
    /// </summary>
    [Range(0, double.MaxValue, ErrorMessage = "Total cost must be positive")]
    public decimal TotalCost { get; set; }

    /// <summary>
    /// Date when the device was received for repair.
    /// </summary>
    [Required(ErrorMessage = "Received date is required")]
    public DateTime ReceivedDate { get; set; }

    /// <summary>
    /// Date when the work order was last updated.
    /// </summary>
    [Required(ErrorMessage = "Updated date is required")]
    public DateTime UpdatedDate { get; set; }
}

/// <summary>
/// Represents a spare part or inventory item used in maintenance.
/// </summary>
public class SparePart
{
    /// <summary>
    /// Unique identifier for the spare part.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Item code or SKU.
    /// </summary>
    [Required(ErrorMessage = "Item code is required")]
    [StringLength(50, ErrorMessage = "Item code cannot exceed 50 characters")]
    public string Code { get; set; } = "";

    /// <summary>
    /// Name or description of the spare part.
    /// </summary>
    [Required(ErrorMessage = "Item name is required")]
    [StringLength(200, ErrorMessage = "Item name cannot exceed 200 characters")]
    public string Name { get; set; } = "";

    /// <summary>
    /// Quantity available in stock.
    /// </summary>
    [Range(0, 100000, ErrorMessage = "Quantity cannot be negative")]
    public int Quantity { get; set; }

    /// <summary>
    /// Date when the item was purchased or added to inventory.
    /// </summary>
    public DateTime PurchaseDate { get; set; }

    /// <summary>
    /// Cost price of the item.
    /// </summary>
    [Range(0, double.MaxValue, ErrorMessage = "Cost price must be positive")]
    public decimal CostPrice { get; set; }

    /// <summary>
    /// Selling price of the item.
    /// </summary>
    [Range(0, double.MaxValue, ErrorMessage = "Selling price must be positive")]
    public decimal SellingPrice { get; set; }

    /// <summary>
    /// Brand or manufacturer of the part.
    /// </summary>
    [StringLength(100, ErrorMessage = "Brand cannot exceed 100 characters")]
    public string Brand { get; set; } = "";

    /// <summary>
    /// Location or shelf where the item is stored.
    /// </summary>
    [StringLength(100, ErrorMessage = "Location cannot exceed 100 characters")]
    public string Location { get; set; } = "";

    /// <summary>
    /// Minimum quantity threshold to trigger low stock warning.
    /// </summary>
    [Range(0, 1000, ErrorMessage = "Minimum threshold cannot be negative")]
    public int MinThreshold { get; set; } = 2;
}

/// <summary>
/// Information used for generating barcodes/QR codes on certificate stickers.
/// Contains essential certificate details for quick scanning and verification.
/// </summary>
public class CertificateBarcodeInfo
{
    /// <summary>
    /// Name of the company issuing the certificate.
    /// </summary>
    public string CompanyName { get; set; } = "";

    /// <summary>
    /// Brand/manufacturer of the calibrated device.
    /// </summary>
    public string DeviceBrand { get; set; } = "";

    /// <summary>
    /// Model number of the calibrated device.
    /// </summary>
    public string DeviceModel { get; set; } = "";

    /// <summary>
    /// Serial number of the calibrated device.
    /// </summary>
    public string DeviceSerialNumber { get; set; } = "";

    /// <summary>
    /// Certificate number for tracking.
    /// </summary>
    public string CertificateNumber { get; set; } = "";

    /// <summary>
    /// Date when the certificate was issued.
    /// </summary>
    public DateTime IssueDate { get; set; }

    /// <summary>
    /// Date when the certificate expires.
    /// </summary>
    public DateTime ExpiryDate { get; set; }

    /// <summary>
    /// URL for accessing the certificate online.
    /// </summary>
    public string Url { get; set; } = "";

    public string ToPayload()
    {
        string Sanitize(string s)
        {
            var v = (s ?? "").Trim();
            v = v.Replace("\r", " ").Replace("\n", " ").Replace("\t", " ");
            while (v.Contains("  "))
                v = v.Replace("  ", " ");
            return v;
        }

        var c = Sanitize(CompanyName);
        var b = Sanitize(DeviceBrand);
        var m = Sanitize(DeviceModel);
        var s = Sanitize(DeviceSerialNumber);
        var cn = Sanitize(CertificateNumber);
        var u = Sanitize(Url);

        var isDate = IssueDate.ToString("yyyy-MM-dd");
        var exDate = ExpiryDate.ToString("yyyy-MM-dd");

        var parts = new List<string>
        {
            $"COMPANY={c}",
            $"BRAND={b}",
            $"MODEL={m}",
            $"SERIAL={s}",
            $"CERT={cn}",
            $"ISSUED={isDate}",
            $"EXPIRES={exDate}"
        };
        if (!string.IsNullOrWhiteSpace(u))
            parts.Add($"URL={u}");
        return string.Join(" | ", parts);
    }
}

/// <summary>
/// Represents a rental agreement for a device.
/// Tracks device rental information, customer details, and payment status.
/// </summary>
public class Rental
{
    /// <summary>
    /// Unique identifier for the rental.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Rental number for tracking.
    /// </summary>
    [Required(ErrorMessage = "Rental number is required")]
    [StringLength(50, ErrorMessage = "Rental number cannot exceed 50 characters")]
    public string RentalNumber { get; set; } = "";

    /// <summary>
    /// Name of the customer renting the device.
    /// </summary>
    [Required(ErrorMessage = "Customer name is required")]
    [StringLength(200, ErrorMessage = "Customer name cannot exceed 200 characters")]
    public string CustomerName { get; set; } = "";

    /// <summary>
    /// Company name of the customer.
    /// </summary>
    [StringLength(200, ErrorMessage = "Company name cannot exceed 200 characters")]
    public string Company { get; set; } = "";

    /// <summary>
    /// Contact phone number for the customer.
    /// </summary>
    [StringLength(50, ErrorMessage = "Phone cannot exceed 50 characters")]
    [Phone(ErrorMessage = "Invalid phone number format")]
    public string Phone { get; set; } = "";

    /// <summary>
    /// Tax number (VAT/TIN) of the customer.
    /// </summary>
    [StringLength(50, ErrorMessage = "Tax number cannot exceed 50 characters")]
    public string TaxNumber { get; set; } = "";

    /// <summary>
    /// ID number or residence permit number of the customer.
    /// </summary>
    [StringLength(50, ErrorMessage = "ID number cannot exceed 50 characters")]
    public string IdNumber { get; set; } = "";

    /// <summary>
    /// Type of device being rented (e.g., Total Station, GPS, Auto Level, Scanner).
    /// </summary>
    [Required(ErrorMessage = "Device type is required")]
    [StringLength(100, ErrorMessage = "Device type cannot exceed 100 characters")]
    public string DeviceType { get; set; } = "";

    /// <summary>
    /// Brand/manufacturer of the device.
    /// </summary>
    [StringLength(100, ErrorMessage = "Brand cannot exceed 100 characters")]
    public string Brand { get; set; } = "";

    /// <summary>
    /// Model number of the device.
    /// </summary>
    [StringLength(100, ErrorMessage = "Model cannot exceed 100 characters")]
    public string Model { get; set; } = "";

    /// <summary>
    /// Serial number of the device.
    /// </summary>
    [StringLength(100, ErrorMessage = "Serial number cannot exceed 100 characters")]
    public string Serial { get; set; } = "";

    /// <summary>
    /// Second serial number (for GPS devices with base and rover).
    /// </summary>
    [StringLength(100, ErrorMessage = "Serial number 2 cannot exceed 100 characters")]
    public string Serial2 { get; set; } = "";

    /// <summary>
    /// Date when the rental period starts.
    /// </summary>
    [Required(ErrorMessage = "Start date is required")]
    public DateTime StartDate { get; set; }

    /// <summary>
    /// Date when the rental period ends.
    /// </summary>
    [Required(ErrorMessage = "End date is required")]
    public DateTime EndDate { get; set; }

    /// <summary>
    /// Type of rental (daily or monthly).
    /// </summary>
    [Required(ErrorMessage = "Rental type is required")]
    [StringLength(20, ErrorMessage = "Rental type cannot exceed 20 characters")]
    public string RentalType { get; set; } = "Daily";

    /// <summary>
    /// Daily rental rate used for automatic price calculation.
    /// </summary>
    [Range(0, double.MaxValue, ErrorMessage = "Daily price must be positive")]
    public decimal DailyPrice { get; set; }

    /// <summary>
    /// Monthly rental rate used for automatic price calculation.
    /// </summary>
    [Range(0, double.MaxValue, ErrorMessage = "Monthly price must be positive")]
    public decimal MonthlyPrice { get; set; }

    /// <summary>
    /// Value of the rented device in Saudi Riyals (used as the compensation
    /// base in the rental contract in case of loss or damage).
    /// </summary>
    [Range(0, double.MaxValue, ErrorMessage = "Device value must be positive")]
    public decimal DeviceValue { get; set; }

    /// <summary>
    /// Total price for the rental.
    /// </summary>
    [Range(0, double.MaxValue, ErrorMessage = "Price must be positive")]
    public decimal Price { get; set; }

    /// <summary>
    /// Amount paid by the customer.
    /// </summary>
    [Range(0, double.MaxValue, ErrorMessage = "Paid amount must be positive")]
    public decimal PaidAmount { get; set; }

    /// <summary>
    /// Remaining amount to be paid.
    /// </summary>
    [Range(0, double.MaxValue, ErrorMessage = "Remaining amount must be positive")]
    public decimal RemainingAmount { get; set; }

    /// <summary>
    /// Status of the rental (Active, Completed, Returned).
    /// </summary>
    [Required(ErrorMessage = "Status is required")]
    [StringLength(20, ErrorMessage = "Status cannot exceed 20 characters")]
    public string Status { get; set; } = "Active";

    /// <summary>
    /// Additional notes about the rental.
    /// </summary>
    [StringLength(1000, ErrorMessage = "Notes cannot exceed 1000 characters")]
    public string Notes { get; set; } = "";

    /// <summary>
    /// Timestamp when the rental record was created.
    /// </summary>
    [Required(ErrorMessage = "Created date is required")]
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Concurrency token for optimistic locking.
    /// </summary>
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
}
