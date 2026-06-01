using System.Text.Json;
using System.Xml;

namespace BlueMax.Infrastructure;

public class ReportTemplateXml
{
    public string? Name { get; set; }
    public string? Content { get; set; }
    public Dictionary<string, string> Tokens { get; set; } = new();
    public bool PrintWithBackground { get; set; }
    public List<ReportLayoutItem> LayoutItems { get; set; } = new();
    public string? LayoutTemplatePath { get; set; }

    public XmlDocument ToXmlDocument()
    {
        var doc = new XmlDocument();
        if (string.IsNullOrWhiteSpace(Content))
            return doc;
        
        try
        {
            doc.LoadXml(Content);
            return doc;
        }
        catch
        {
            return doc;
        }
    }

    public string Serialize()
    {
        return JsonSerializer.Serialize(this);
    }

    public static ReportTemplateXml? Deserialize(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<ReportTemplateXml>(json);
        }
        catch
        {
            return null;
        }
    }
}
