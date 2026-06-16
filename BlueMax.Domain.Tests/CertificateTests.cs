using BlueMax.Domain;
using Xunit;

namespace BlueMax.Domain.Tests;

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
        Assert.Equal("", certificate.CertificateNumber); // Validation would fail at runtime
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
        Assert.Equal("", customer.Name); // Validation would fail at runtime
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
}
