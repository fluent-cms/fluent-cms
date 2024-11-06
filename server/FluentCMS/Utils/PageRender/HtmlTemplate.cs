namespace FluentCMS.Utils.PageRender;

public sealed class HtmlTemplate(string templatePath)
{
    private readonly string _template = LoadTemplate(templatePath);

    static string LoadTemplate(string templatePath)
    {
        return File.Exists(templatePath)
            ? File.ReadAllText(templatePath)
            :"";
    }
    
    public string RenderHtml(string title,string body,  string css)
    {
        return _template.
            Replace("<!--title-->", title).
            Replace("<!--body-->", body).
            Replace("<!--css-->", css);
    }
}