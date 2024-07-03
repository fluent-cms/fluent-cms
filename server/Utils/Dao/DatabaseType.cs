namespace Utils.Dao;

public enum DatabaseType
{
    Int,
    Datetime,

    Text, //slow performance compare to string
    String,//has length limit 255 
    
    NA,
}
