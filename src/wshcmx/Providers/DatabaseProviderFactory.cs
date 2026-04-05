using wshcmx.Providers;

namespace wshcmx;

internal static class DatabaseProviderFactory
{
    public static DatabaseProviderBase CreateProvider(DatabaseType databaseType)
    {
        return databaseType switch
        {
            DatabaseType.SqlServer => new SqlServerProvider(),
            DatabaseType.PostgreSql => new PostgreSqlProvider(),
            _ => throw new NotSupportedException($"Database type {databaseType} is not supported.")
        };
    }
}
