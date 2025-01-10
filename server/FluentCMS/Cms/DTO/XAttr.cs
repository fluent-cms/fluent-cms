using FluentCMS.Core.Descriptors;

namespace FluentCMS.Cms.DTO;

//extended attribute for admin-panel
public record XAttr(
    string Field,
    string Header ,
    DataType DataType ,
    DisplayType DisplayType ,
    bool InList ,
    bool InDetail ,
    bool IsDefault ,
    string Options ,
    XEntity? Junction = null,
    XEntity? Lookup = null,
    XEntity? Collection = null
);

public static class XAttrExtensions
{
    public static XAttr ToXAttr(this LoadedAttribute attribute)
        => new (
            Field: attribute.Field,
            Header: attribute.Header,
            DataType: attribute.DataType,
            DisplayType: attribute.DisplayType,
            InList: attribute.InList,
            InDetail: attribute.InDetail,
            IsDefault: attribute.IsDefault,
            Options: attribute.Options,
            Junction: attribute.Junction?.TargetEntity.ToXEntity(),
            Lookup: attribute.Lookup?.TargetEntity.ToXEntity(),
            Collection: attribute.Collection?.TargetEntity.ToXEntity()
        );
}