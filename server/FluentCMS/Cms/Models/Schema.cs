namespace FluentCMS.Cms.Models;

public record Schema(string Name, string Type, Settings Settings, int Id = 0, string CreatedBy ="");