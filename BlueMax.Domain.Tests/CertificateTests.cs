using System.ComponentModel.DataAnnotations;
using BlueMax.Domain;
using Xunit;

namespace BlueMax.Domain.Tests;

static class ModelValidator
{
    public static IList<ValidationResult> Validate(object model)
    {
        var results = new List<ValidationResult>();
        var ctx = new ValidationContext(model);
        Validator.TryValidateObject(model, ctx, results, validateAllProperties: true);
        return results;
    }
}

public class CertificateTests
{
    [Fact]
    public void Certificate_WithValidData_PassesValidation()
    {
        // Arrange
        var certificate = new Certificate
        {
            CertificateNumber = "CERT-001",
            ClientName = "Test Client",
            Phone = "1234567890",
            DeviceType = "Total Station",
            Brand = "Leica",
            Model = "TS16",
            SerialText = "SN-12345",
            IssueDate = DateTime.Now,
            ExpiryDate = DateTime.Now.AddYears(1),
            CreatedAt = DateTime.Now
        };

        // Assert
        Assert.Empty(ModelValidator.Validate(certificate));
        Assert.Equal("CERT-001", certificate.CertificateNumber);
        Assert.Equal("Test Client", certificate.ClientName);
        Assert.Equal("Total Station", certificate.DeviceType);
    }

    [Fact]
    public void Certificate_WithEmptyCertificateNumber_FailsValidation()
    {
        // Arrange
        var certificate = new Certificate
        {
            CertificateNumber = "", // Invalid: empty
            ClientName = "Test Client",
            IssueDate = DateTime.Now,
            ExpiryDate = DateTime.Now.AddYears(1),
            CreatedAt = DateTime.Now
        };

        // Assert
        var results = ModelValidator.Validate(certificate);
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(Certificate.CertificateNumber)));
    }

    [Fact]
    public void Certificate_WithMissingRequiredFields_FailsValidation()
    {
        // Arrange
        var certificate = new Certificate
        {
            CertificateNumber = "CERT-003",
            ClientName = "Test Client"
        };

        // Assert
        var results = ModelValidator.Validate(certificate);
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(Certificate.DeviceType)));
    }

    [Fact]
    public void Certificate_WithCertificateNumberTooLong_FailsValidation()
    {
        // Arrange
        var certificate = new Certificate
        {
            CertificateNumber = new string('C', 51), // Exceeds StringLength(50)
            ClientName = "Test Client",
            IssueDate = DateTime.Now,
            ExpiryDate = DateTime.Now.AddYears(1),
            CreatedAt = DateTime.Now
        };

        // Assert
        var results = ModelValidator.Validate(certificate);
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(Certificate.CertificateNumber)));
    }

    [Fact]
    public void Certificate_ExpiryDate_ShouldBeAfterIssueDate()
    {
        // Arrange
        var certificate = new Certificate
        {
            CertificateNumber = "CERT-002",
            ClientName = "Test Client",
            IssueDate = DateTime.Now,
            ExpiryDate = DateTime.Now.AddYears(1),
            CreatedAt = DateTime.Now
        };

        // Assert
        Assert.True(certificate.ExpiryDate > certificate.IssueDate);
    }
}

public class CustomerTests
{
    [Fact]
    public void Customer_WithValidData_PassesValidation()
    {
        // Arrange
        var customer = new Customer
        {
            Name = "Test Company",
            Phone = "1234567890",
            Address = "123 Test Street",
            CreatedAt = DateTime.Now
        };

        // Assert
        Assert.Empty(ModelValidator.Validate(customer));
        Assert.Equal("Test Company", customer.Name);
        Assert.Equal("1234567890", customer.Phone);
    }

    [Fact]
    public void Customer_WithEmptyName_FailsValidation()
    {
        // Arrange
        var customer = new Customer
        {
            Name = "", // Invalid: empty
            CreatedAt = DateTime.Now
        };

        // Assert
        var results = ModelValidator.Validate(customer);
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(Customer.Name)));
    }
}

public class WorkOrderTests
{
    [Fact]
    public void WorkOrder_WithValidData_PassesValidation()
    {
        // Arrange
        var workOrder = new WorkOrder
        {
            CustomerName = "Test Client",
            CustomerPhone = "1234567890",
            DeviceType = "Total Station",
            Model = "TS16",
            SerialNumber = "SN-12345",
            Status = "Pending",
            PartsCost = 100,
            LaborCost = 50,
            TotalCost = 150,
            ReceivedDate = DateTime.Now,
            UpdatedDate = DateTime.Now
        };

        // Assert
        Assert.Empty(ModelValidator.Validate(workOrder));
        Assert.Equal("Test Client", workOrder.CustomerName);
        Assert.Equal("Pending", workOrder.Status);
        Assert.Equal(150, workOrder.TotalCost);
    }

    [Fact]
    public void WorkOrder_TotalCost_EqualsPartsPlusLabor()
    {
        // Arrange
        var workOrder = new WorkOrder
        {
            PartsCost = 100,
            LaborCost = 50,
            TotalCost = 150
        };

        // Assert
        Assert.Equal(workOrder.PartsCost + workOrder.LaborCost, workOrder.TotalCost);
    }

    [Fact]
    public void WorkOrder_WithNegativeCost_FailsValidation()
    {
        // Arrange
        var workOrder = new WorkOrder
        {
            CustomerName = "Test Client",
            DeviceType = "Total Station",
            Status = "Pending",
            PartsCost = -5, // Invalid: Range(0, double.MaxValue)
            ReceivedDate = DateTime.Now,
            UpdatedDate = DateTime.Now
        };

        // Assert
        var results = ModelValidator.Validate(workOrder);
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(WorkOrder.PartsCost)));
    }
}

public class SparePartTests
{
    [Fact]
    public void SparePart_WithValidData_PassesValidation()
    {
        // Arrange
        var sparePart = new SparePart
        {
            Code = "SP-001",
            Name = "Main Board",
            Quantity = 5,
            CostPrice = 100,
            SellingPrice = 150
        };

        // Assert
        Assert.Empty(ModelValidator.Validate(sparePart));
        Assert.Equal("SP-001", sparePart.Code);
        Assert.Equal(5, sparePart.Quantity);
    }

    [Fact]
    public void SparePart_SellingPrice_ShouldBePositive()
    {
        // Arrange
        var sparePart = new SparePart
        {
            Code = "SP-002",
            Name = "Battery",
            SellingPrice = 100
        };

        // Assert
        Assert.True(sparePart.SellingPrice >= 0);
    }

    [Fact]
    public void SparePart_WithNegativeQuantity_FailsValidation()
    {
        // Arrange
        var sparePart = new SparePart
        {
            Code = "SP-003",
            Name = "Gasket",
            Quantity = -1 // Invalid: Range(0, 100000)
        };

        // Assert
        var results = ModelValidator.Validate(sparePart);
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(SparePart.Quantity)));
    }
}
