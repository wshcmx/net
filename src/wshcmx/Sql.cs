namespace wshcmx;

public class Sql
{
    private string? _connectionString;
    private DatabaseProviderBase? _provider;

    public void Init(string connectionString, DatabaseType databaseType = DatabaseType.SqlServer)
    {
        _connectionString = connectionString;
        _provider = DatabaseProviderFactory.CreateProvider(databaseType);
    }

    public void Init(string connectionString, int databaseTypeNumber)
    {
        Init(connectionString, (DatabaseType)databaseTypeNumber);
    }

    public KeyValuePair<string, object?>[][] ExecuteQuery(string commandText)
    {
        var provider = GuardHelper.GetRequired(_provider, nameof(_provider));
        var connectionString = GuardHelper.GetRequired(_connectionString, nameof(_connectionString));
        return provider.ExecuteQuery(connectionString, commandText);
    }

    public void ExecuteNonQuery(string commandText)
    {
        var provider = GuardHelper.GetRequired(_provider, nameof(_provider));
        var connectionString = GuardHelper.GetRequired(_connectionString, nameof(_connectionString));
        provider.ExecuteNonQuery(connectionString, commandText);
    }

    public object[] ExecutePaginationProcedure(string procedureName, string serializedOptions, string serializedParameters)
    {
        var provider = GuardHelper.GetRequired(_provider, nameof(_provider));
        var connectionString = GuardHelper.GetRequired(_connectionString, nameof(_connectionString));
        return provider.ExecutePaginationProcedure(connectionString, procedureName, serializedOptions, serializedParameters);
    }

    public object[] ExecuteProcedure(string procedureName, string? serializedParameters)
    {
        var provider = GuardHelper.GetRequired(_provider, nameof(_provider));
        var connectionString = GuardHelper.GetRequired(_connectionString, nameof(_connectionString));
        return provider.ExecuteProcedure(connectionString, procedureName, serializedParameters);
    }
}
