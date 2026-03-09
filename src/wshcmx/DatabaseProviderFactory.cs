using wshcmx.Providers;

namespace wshcmx;

public static class DatabaseProviderFactory
{
    public static IDatabaseProvider CreateProvider(DatabaseType databaseType)
    {
        return databaseType switch
        {
            DatabaseType.SqlServer => new SqlServerProvider(),
            DatabaseType.PostgreSql => new PostgreSqlProvider(),
            _ => throw new NotSupportedException($"Database type {databaseType} is not supported.")
        };
    }
}