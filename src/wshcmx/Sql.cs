using System.Data;
using System.Text.Json;
using System.Linq.Dynamic.Core;
using wshcmx.Net.Providers;

namespace wshcmx.Net;

public class Sql
{
    private IDatabaseProvider? _provider;

    public void Init(string connectionString, DatabaseType databaseType = DatabaseType.SqlServer)
    {
        _provider = DatabaseProviderFactory.CreateProvider(connectionString, databaseType);
    }

    public void Init(string connectionString, int databaseTypeNumber)
    {
        Init(connectionString, (DatabaseType)databaseTypeNumber);
    }

    public KeyValuePair<string, object?>[][] ExecuteQuery(string commandText)
    {
        return GuardHelper.GetRequired(_provider, nameof(_provider)).ExecuteQuery(commandText);
    }

    public void ExecuteNonQuery(string commandText)
    {
        GuardHelper.GetRequired(_provider, nameof(_provider)).ExecuteNonQuery(commandText);
    }

    public object[] ExecuteProcedure(string procedureName, string? serializedParameters)
    {
        return GuardHelper.GetRequired(_provider, nameof(_provider)).ExecuteProcedure(procedureName, serializedParameters);
    }

    public object[] ExecutePaginationProcedure(string procedureName, string serializedOptions, string serializedParameters)
    {
        return GuardHelper.GetRequired(_provider, nameof(_provider)).ExecutePaginationProcedure(procedureName, serializedOptions, serializedParameters);
    }
}
