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

public class BackupEncryptionTests
{
    [Fact]
    public void EncryptThenDecrypt_RoundTrips_OriginalData()
    {
        var data = System.Text.Encoding.UTF8.GetBytes("BlueMax backup content 123");

        var encrypted = BackupEncryption.EncryptData(data, "P@ssw0rd!");

        Assert.NotEqual(data, encrypted);
        var decrypted = BackupEncryption.DecryptData(encrypted, "P@ssw0rd!");

        Assert.Equal(data, decrypted);
    }

    [Fact]
    public void EncryptedData_HasV2MagicHeader()
    {
        var data = System.Text.Encoding.UTF8.GetBytes("payload");

        var encrypted = BackupEncryption.EncryptData(data, "pw");

        var magic = System.Text.Encoding.ASCII.GetString(encrypted.AsSpan(0, 11).ToArray());
        Assert.Equal("BLUEMAXENC2", magic);
    }

    [Fact]
    public void Decrypt_WithWrongPassword_Throws()
    {
        var data = System.Text.Encoding.UTF8.GetBytes("secret payload");
        var encrypted = BackupEncryption.EncryptData(data, "correct-password");

        Assert.Throws<System.Security.Cryptography.CryptographicException>(
            () => BackupEncryption.DecryptData(encrypted, "wrong-password"));
    }

    [Fact]
    public void Decrypt_CorruptedData_Throws()
    {
        var data = System.Text.Encoding.UTF8.GetBytes("secret payload");
        var encrypted = BackupEncryption.EncryptData(data, "correct-password");

        encrypted[^1] ^= 0xFF; // corrupt the last ciphertext byte

        Assert.Throws<System.Security.Cryptography.CryptographicException>(
            () => BackupEncryption.DecryptData(encrypted, "correct-password"));
    }

    [Fact]
    public void EncryptFileToFile_RoundTrips_AndIsDetectedAsEncrypted()
    {
        var dir = Path.Combine(Path.GetTempPath(), $"BlueMaxEncTest_{Guid.NewGuid():N}");
        Directory.CreateDirectory(dir);
        var source = Path.Combine(dir, "backup.sqlite");
        var dest = Path.Combine(dir, "backup.enc");
        var restored = Path.Combine(dir, "restored.sqlite");
        var content = System.Text.Encoding.UTF8.GetBytes("file content");

        try
        {
            File.WriteAllBytes(source, content);

            BackupEncryption.EncryptFileToFile(source, dest, "pw");
            Assert.True(File.Exists(dest));
            Assert.True(BackupEncryption.IsEncryptedFile(dest));
            Assert.False(BackupEncryption.IsEncryptedFile(source));

            BackupEncryption.DecryptFileToFile(dest, restored, "pw");

            Assert.Equal(content, File.ReadAllBytes(restored));
        }
        finally
        {
            if (Directory.Exists(dir))
                Directory.Delete(dir, recursive: true);
        }
    }
}

public class CalibrationDefaultsSanitizationTests
{
    static string UniqueName() => $"Test{Guid.NewGuid():N}";

    static string GetSettingsFilePath(string deviceType) => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "BlueMax",
        "CertSystem",
        "Settings",
        $"CalibrationDefaults_{deviceType}.json");

    [Fact]
    public void LoadRows_DeviceTypeWithInvalidPathChars_IsSanitizedAndRoundTrips()
    {
        var store = new CalibrationDefaultsStore();
        var deviceType = UniqueName() + ":*?\"<>|";
        var rows = new List<CalibrationDefaultsRow>
        {
            new() { Item = "Axis Error", MeasuredValue = 0.002m }
        };

        try
        {
            store.Save(deviceType, rows);

            var loaded = store.LoadRows(deviceType);

            Assert.Single(loaded);
            Assert.Equal("Axis Error", loaded[0].Item);
            Assert.Equal(0.002m, loaded[0].MeasuredValue);
        }
        finally
        {
            foreach (var dt in new[] { deviceType })
            {
                var file = GetSettingsFilePath(dt);
                if (File.Exists(file))
                    File.Delete(file);
            }
        }
    }
}

public class DocumentSequenceStoreTests
{
    [Fact]
    public void GetNextFormatted_SequentialPerKey_ReturnsD4Counters()
    {
        var key = $"TestSeq{Guid.NewGuid():N}";

        try
        {
            var first = DocumentSequenceStore.GetNextFormatted(key);
            var second = DocumentSequenceStore.GetNextFormatted(key);

            Assert.Equal("0001", first);
            Assert.Equal("0002", second);
        }
        finally
        {
            RemoveTestKey(key);
        }
    }

    [Fact]
    public void GetNextFormatted_DifferentKeys_AreIndependent()
    {
        var keyA = $"TestSeqA{Guid.NewGuid():N}";
        var keyB = $"TestSeqB{Guid.NewGuid():N}";

        try
        {
            var a1 = DocumentSequenceStore.GetNextFormatted(keyA);
            var b1 = DocumentSequenceStore.GetNextFormatted(keyB);
            var a2 = DocumentSequenceStore.GetNextFormatted(keyA);

            Assert.Equal("0001", a1);
            Assert.Equal("0001", b1);
            Assert.Equal("0002", a2);
        }
        finally
        {
            RemoveTestKey(keyA);
            RemoveTestKey(keyB);
        }
    }

    static void RemoveTestKey(string key)
    {
        try
        {
            var path = Path.Combine(AppPaths.CertificatesOutput, "sequence_counters.json");
            if (!File.Exists(path))
                return;
            var map = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, int>>(File.ReadAllText(path));
            if (map == null || !map.Remove(key))
                return;
            var temp = path + ".tmp";
            File.WriteAllText(temp, System.Text.Json.JsonSerializer.Serialize(map));
            File.Move(temp, path, overwrite: true);
        }
        catch
        {
            // best-effort cleanup
        }
    }
}
