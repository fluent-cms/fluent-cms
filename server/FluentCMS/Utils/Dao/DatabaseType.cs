namespace FluentCMS.Utils.Dao;

public enum DatabaseType
{
    Int,
    BigInt,
    SmallInt,
    TinyInt,
    
    Decimal,
    Float,

    Date,
    Time,
    Datetime,

    Text, // include char, varchar, nchar, nvarchar
    Binary,
    Json,
    
    NA, // not available, for the sub table and sub list
}
