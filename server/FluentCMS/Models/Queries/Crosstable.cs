using SqlKata;

namespace FluentCMS.Models.Queries;
using Record = IDictionary<string,object>;

public class Crosstable
{
    public Entity CrossEntity { get; set; } = null!;
    public Entity TargetEntity { get; set; } = null!;
    public Attribute FromAttribute { get; set; } = null!;
    public Attribute TargetAttribute { get; set; } = null!;


    public Query Delete(string fromId, Record[] targetItems)
    {
        var id = FromAttribute.CastToDatabaseType(fromId);
        var vals = targetItems.Select(x => x[TargetEntity.PrimaryKey]);
        return new Query(CrossEntity.TableName).Where(FromAttribute.Field, id)
            .WhereIn(TargetAttribute.Field, vals).AsUpdate(["deleted"],[true]);
    }
    
    public Query Insert(string fromId, Record[] targetItems)
    {
        var id = FromAttribute.CastToDatabaseType(fromId);
        string[] cols = [FromAttribute.Field, TargetAttribute.Field];
        var vals = targetItems.Select(x =>
        {
            return new object[]{id, x[TargetEntity.PrimaryKey]};
        });

        return new Query(CrossEntity.TableName).AsInsert(cols, vals);
    }
     private Query Base(Attribute[] selectAttributes, object id)
     {
          var lstFields = selectAttributes.Select(x => x.FullName()).ToList();
          lstFields.Add(FromAttribute.FullName());
          var (a, b) = (TargetEntity.KeyAttribute().FullName(), TargetAttribute.FullName());
          var qry = TargetEntity.Basic()
              .LeftJoin(CrossEntity.TableName, j=>j.On(a,b)
                  .Where(FromAttribute.FullName(),id).Where(CrossEntity.Fullname("deleted"), false))
              .Select(lstFields);
          return qry;
     }   
    public Query Many(Attribute[] selectAttributes, object id, bool exclude)
    {
        var baseQuery = Base(selectAttributes, id);
        if (exclude)
        {
            baseQuery.WhereNull(FromAttribute.FullName());
        }
        else
        {
            baseQuery.WhereNotNull(FromAttribute.FullName());
        }
        return baseQuery;
    }
}