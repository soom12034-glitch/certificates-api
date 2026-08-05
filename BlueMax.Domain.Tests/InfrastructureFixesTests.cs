using BlueMax.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace BlueMax.Domain.Tests;

public class ReportTemplateStoreTests
{
    static string UniqueName() => $"Test-{Guid.NewGuid():N}";

    static string GetDeviceDirPath(string deviceType) => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "BlueMax",
        "CertSystem",
        "Templates",
        deviceType);

    static void Cleanup(string deviceType)
    {
        var dir = GetDeviceDirPath(deviceType);
        if (Directory.Exists(dir))
            Directory.Delete(dir, recursive: true);
    }

    [Fact]
    public void SaveThenLoad_DeviceTypeTemplate_RoundTrips()
    {
        var store = new ReportTemplateStore();
        var deviceType = UniqueName();
        var templateName = UniqueName();
        var template = new ReportTemplateXml
        {
            Name = templateName,
            Content = "<root><value>1</value></root>",
            PrintWithBackground = true
        };

        try
        {
            store.Save(deviceType, templateName, template);

            var loaded = store.Load(deviceType, templateName);

            Assert.NotNull(loaded);
            Assert.Equal(templateName, loaded!.Name);
            Assert.Equal(template.Content, loaded.Content);
            Assert.True(loaded.PrintWithBackground);
        }
        finally
        {
            Cleanup(deviceType);
        }
    }

    [Fact]
    public void Load_DeviceTypeTemplate_IsCaseInsensitive()
    {
        var store = new ReportTemplateStore();
        var deviceType = UniqueName();
        var templateName = UniqueName();
        var template = new ReportTemplateXml { Name = templateName, Content = "<root/>" };

        try
        {
            store.Save(deviceType, templateName, template);

            var loaded = store.Load(deviceType, templateName.ToUpperInvariant());

            Assert.NotNull(loaded);
            Assert.Equal(templateName, loaded!.Name);
        }
        finally
        {
            Cleanup(deviceType);
        }
    }

    [Fact]
    public void Load_MissingTemplate_ReturnsNull()
    {
        var store = new ReportTemplateStore();
        var deviceType = UniqueName();

        try
        {
            var loaded = store.Load(deviceType, "DoesNotExist");
            Assert.Null(loaded);
        }
        finally
        {
            Cleanup(deviceType);
        }
    }

    [Fact]
    public void ListTemplates_IncludesSavedTemplate()
    {
        var store = new ReportTemplateStore();
        var deviceType = UniqueName();
        var templateName = UniqueName();
        var template = new ReportTemplateXml { Name = templateName, Content = "<root/>" };

        try
        {
            store.Save(deviceType, templateName, template);

            var names = store.ListTemplates(deviceType);

            Assert.Contains(names, n => string.Equals(n, templateName, StringComparison.OrdinalIgnoreCase));
        }
        finally
        {
            Cleanup(deviceType);
        }
    }

    [Fact]
    public void FindTemplateId_IsStableAndCaseInsensitive()
    {
        var store = new ReportTemplateStore();
        var deviceType = UniqueName();
        var templateName = UniqueName();
        var template = new ReportTemplateXml { Name = templateName, Content = "<root/>" };

        try
        {
            store.Save(deviceType, templateName, template);

            var id1 = store.FindTemplateId(deviceType, templateName);
            var id2 = store.FindTemplateId(deviceType, templateName.ToUpperInvariant());

            Assert.NotNull(id1);
            Assert.NotNull(id2);
            Assert.Equal(id1, id2);
            Assert.True(id1 > 0);
        }
        finally
        {
            Cleanup(deviceType);
        }
    }

    [Fact]
    public void FindTemplateId_MissingTemplate_ReturnsNull()
    {
        var store = new ReportTemplateStore();
        var deviceType = UniqueName();

        try
        {
            var id = store.FindTemplateId(deviceType, "DoesNotExist");
            Assert.Null(id);
        }
        finally
        {
            Cleanup(deviceType);
        }
    }
}

public class CalibrationDefaultsStoreTests
{
    static string UniqueName() => $"Test-{Guid.NewGuid():N}";

    static string GetSettingsFilePath(string deviceType) => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "BlueMax",
        "CertSystem",
        "Settings",
        $"CalibrationDefaults_{deviceType}.json");

    [Fact]
    public void LoadRows_SavedPerDeviceType_RoundTrips()
    {
        var store = new CalibrationDefaultsStore();
        var deviceType = UniqueName();
        var rows = new List<CalibrationDefaultsRow>
        {
            new() { Item = "Horizontal Angle", MeasuredValue = 0.0005m },
            new() { Item = "Vertical Angle", MeasuredValue = null }
        };

        try
        {
            store.Save(deviceType, rows);

            var loaded = store.LoadRows(deviceType);

            Assert.Equal(2, loaded.Count);
            Assert.Equal("Horizontal Angle", loaded[0].Item);
            Assert.Equal(0.0005m, loaded[0].MeasuredValue);
            Assert.Equal("Vertical Angle", loaded[1].Item);
            Assert.Null(loaded[1].MeasuredValue);
        }
        finally
        {
            var file = GetSettingsFilePath(deviceType);
            if (File.Exists(file))
                File.Delete(file);
        }
    }

    [Fact]
    public void LoadRows_DeviceTypes_AreIsolated()
    {
        var store = new CalibrationDefaultsStore();
        var deviceTypeA = UniqueName();
        var deviceTypeB = UniqueName();
        var rows = new List<CalibrationDefaultsRow>
        {
            new() { Item = "Bubble Level", MeasuredValue = 0.001m }
        };

        try
        {
            store.Save(deviceTypeA, rows);

            var loadedA = store.LoadRows(deviceTypeA);
            var loadedB = store.LoadRows(deviceTypeB);

            Assert.Single(loadedA);
            Assert.Empty(loadedB);
        }
        finally
        {
            foreach (var dt in new[] { deviceTypeA, deviceTypeB })
            {
                var file = GetSettingsFilePath(dt);
                if (File.Exists(file))
                    File.Delete(file);
            }
        }
    }

    [Fact]
    public void LoadRows_NoFile_ReturnsEmpty()
    {
        var store = new CalibrationDefaultsStore();
        var loaded = store.LoadRows(UniqueName());
        Assert.Empty(loaded);
    }
}

public class AppDbContextTests
{
    [Fact]
    public void ConfigureProvider_UnreachableSqlServer_ThrowsInsteadOfSilentFallback()
    {
        var optionsBuilder = new DbContextOptionsBuilder();
        var sql = "Server=localhost,1;Database=BlueMax;Integrated Security=True;Connect Timeout=3";

        var ex = Assert.Throws<InvalidOperationException>(() => AppDbContext.ConfigureProvider(optionsBuilder, sql));

        Assert.Contains("SQL Server", ex.Message);
    }

    [Fact]
    public void ConfigureProvider_NoSqlConnection_UsesSqlite()
    {
        var optionsBuilder = new DbContextOptionsBuilder();

        AppDbContext.ConfigureProvider(optionsBuilder, null);

        Assert.Contains(optionsBuilder.Options.Extensions,
            e => e.GetType().Name.Contains("Sqlite", StringComparison.OrdinalIgnoreCase));
    }
}
