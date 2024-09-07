namespace FluentCMS.Cms.Models;

public sealed class Page
{
    
    public string Name { get; set; } = "";
    public string Title { get; set; } = "";
    public string Query { get; set; } = "";
    public string QueryString { get; set; } = "";
    
    public string Html { get; set; } = "";
    public string Css { get; set; } = "";

    public const string HomePage = "home";
    public const string RouterKey = "router.key";
    public static string SinglePageName(string pageName)
    {
        return pageName + ".detail";
    } 
    
    /*for grapes.js restore last configure */
    public string Components { get; set; } = "";
    public string Styles { get; set; } = "";
}