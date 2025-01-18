using FormCMS.Core.Descriptors;

namespace FormCMS.Cms.DTO;

public record XEntity(
    XAttr[] Attributes,
    string Name ,
    string DisplayName ,
    
    string PrimaryKey ,
    string LabelAttributeName,
    int DefaultPageSize 
);

public static class EntityDtoExtensions
{
    public static XEntity ToXEntity(this LoadedEntity entity)
        => new(
            Attributes: entity.Attributes.Select(x => x.ToXAttr()).ToArray(),
            Name: entity.Name,
            PrimaryKey: entity.PrimaryKey,
            DisplayName: entity.DisplayName,
            LabelAttributeName: entity.LabelAttributeName,
            DefaultPageSize: entity.DefaultPageSize
        );
}