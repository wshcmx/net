using wshcmx.Net.Providers;

namespace wshcmx.Net;

internal static class DatabaseProviderFactory
{
    public static IDatabaseProvider CreateProvider(string connectionString, DatabaseType databaseType)
    {
        return databaseType switch
        {
            DatabaseType.SqlServer => new SqlServerProvider(connectionString),
            DatabaseType.PostgreSql => new PostgreSqlProvider(connectionString),
            _ => throw new NotSupportedException($"Database type {databaseType} is not supported.")
        };
    }
}
