using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FluentCMSApi.models;

public class Schema:ModelBase
{
    [Column(TypeName = "VARCHAR")]
    [StringLength(250)]
    public string Name { get; set; } = "";
    [Column(TypeName = "VARCHAR")]
    [StringLength(250)]
    public string Type { get; set; } = "";
    public string Value { get; set; } = "";
}