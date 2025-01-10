using FormCMS.Core.Descriptors;

namespace FormCMS.Cms.DTO;

public record XEntity(
    XAttr[] Attributes,
    string Name ,
    string PrimaryKey ,
    string Title ,
    string TitleAttribute ,
    int DefaultPageSize 
);

public static class EntityDtoExtensions
{
    public static XEntity ToXEntity(this LoadedEntity entity)
        => new(
            Attributes: entity.Attributes.Select(x => x.ToXAttr()).ToArray(),
            Name: entity.Name,
            PrimaryKey: entity.PrimaryKey,
            Title: entity.Title,
            TitleAttribute: entity.TitleAttribute,
            DefaultPageSize: entity.DefaultPageSize
        );
}