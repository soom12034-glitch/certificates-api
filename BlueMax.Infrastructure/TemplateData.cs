namespace BlueMax.Infrastructure;

public class TableTemplateData
{
    public string? TableName { get; set; }
    public List<Dictionary<string, object>> Rows { get; set; } = new();
}

public class ImageTemplateData
{
    public string? ImagePath { get; set; }
    public string? BookmarkName { get; set; }
    public double Width { get; set; }
    public double Height { get; set; }
}

public class DocumentTemplateData
{
    public string? TemplateName { get; set; }
    public string? TemplatePath { get; set; }
    public Dictionary<string, object> Data { get; set; } = new();

    public DocumentTemplateData()
    {
    }

    public DocumentTemplateData(string templateName, string templatePath, Dictionary<string, object> data)
    {
        TemplateName = templateName;
        TemplatePath = templatePath;
        Data = data;
    }
}
