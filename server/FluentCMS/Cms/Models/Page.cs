namespace FluentCMS.Cms.Models;

public sealed record Page(
    string Name,
    string Title,
    string? Query,
    string Html,
    string Css,
    /*for grapes.js restore last configure */
    string Components,
    string Styles);

public static class PageConstants
{
    public const string HomePage = "home";
}