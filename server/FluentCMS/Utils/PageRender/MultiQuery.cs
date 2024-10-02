using Microsoft.Extensions.Primitives;

namespace FluentCMS.Utils.PageRender;

public record MultiQuery(string Query, Dictionary<string,StringValues> Qs, int Offset, int Limit);
