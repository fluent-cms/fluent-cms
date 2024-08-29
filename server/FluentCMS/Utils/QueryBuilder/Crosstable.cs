using FluentCMS.Utils.DataDefinitionExecutor;
using SqlKata;

namespace FluentCMS.Utils.QueryBuilder;

public class Crosstable
{
    public Entity CrossEntity { get; set; }
    public Entity TargetEntity { get; set; }
    public Attribute FromAttribute { get; set; }
    public Attribute TargetAttribute { get; set; }

    public ColumnDefinition[] GetColumnDefinitions()
    {
        CrossEntity.Attributes = [FromAttribute, TargetAttribute];
        CrossEntity.EnsureDefaultAttribute();
        CrossEntity.EnsureDeleted();
        return CrossEntity.ColumnDefinitions();
    }
    
    public Crosstable(Entity fromEntity, Entity targetEntity)
    {
        TargetEntity = targetEntity;
        string[] strs = [fromEntity.Name, targetEntity.Name];
        var sorted = strs.OrderBy(x => x).ToArray();
        var crossEntityName = $"{sorted[0]}_{sorted[1]}";
        CrossEntity = new Entity
        {
            TableName = crossEntityName,

        };
        FromAttribute = new Attribute
        {
            Field = $"{fromEntity.Name}_id",
            Parent = CrossEntity,
        };
        TargetAttribute = new Attribute
        {
            Field = $"{targetEntity.Name}_id",
            Parent = CrossEntity
        };
    }

    public SqlKata.Query Delete(object id, Record[] targetItems)
    {
        var vals = targetItems.Select(x => x[TargetEntity.PrimaryKey]);
        return new SqlKata.Query(CrossEntity.TableName).Where(FromAttribute.Field, id)
            .WhereIn(TargetAttribute.Field, vals).AsUpdate(["deleted"],[true]);
    }
    
    public SqlKata.Query Insert(object id, Record[] targetItems)
    {
        string[] cols = [FromAttribute.Field, TargetAttribute.Field];
        var vals = targetItems.Select(x =>
        {
            return new object[]{id, x[TargetEntity.PrimaryKey]};
        });

        return new SqlKata.Query(CrossEntity.TableName).AsInsert(cols, vals);
    }
     private SqlKata.Query Base(Attribute[] selectAttributes)
     {
          var lstFields = selectAttributes.Select(x => x.FullName()).ToList();
          lstFields.Add(FromAttribute.FullName());
          var qry = TargetEntity.Basic().Select(lstFields);
          return qry;
     }

     public SqlKata.Query Many(Attribute[] selectAttributes, bool exclude, object id)
     {
         var baseQuery = Base(selectAttributes);
         var (a, b) = (TargetEntity.PrimaryKeyAttribute.FullName(), TargetAttribute.FullName());
         if (exclude)
         {
             baseQuery.LeftJoin(CrossEntity.TableName,
                 j => j.On(a, b)
                     .Where(FromAttribute.FullName(), id)
                     .Where(CrossEntity.Fullname("deleted"), false)
             ).WhereNull(FromAttribute.FullName());
         }
         else
         {
             baseQuery.Join(CrossEntity.TableName, a, b)
                 .Where(FromAttribute.FullName(), id)
                 .Where(CrossEntity.Fullname("deleted"), false);
         }

         return baseQuery;
     }

     public SqlKata.Query Many(Attribute[] selectAttributes, object[] ids)
     {
         var baseQuery = Base(selectAttributes);
         var (a, b) = (TargetEntity.PrimaryKeyAttribute.FullName(), TargetAttribute.FullName());
         baseQuery.Join(CrossEntity.TableName, a, b)
             .WhereIn(FromAttribute.FullName(), ids)
             .Where(CrossEntity.Fullname("deleted"), false);
         return baseQuery;
     }
}