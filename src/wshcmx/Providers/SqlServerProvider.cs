using System.Data.Common;
using Microsoft.Data.SqlClient;

namespace wshcmx.Net.Providers;

internal class SqlServerProvider(string connectionString) : DatabaseProviderBase<SqlConnection>(connectionString)
{

    protected override DbCommand CreateTypedCommand(string commandText, SqlConnection connection)
    {
        return new SqlCommand(commandText, connection);
    }

    protected override DbParameter CreateParameter(string name, object? value)
    {
        return new SqlParameter(name, value ?? DBNull.Value);
    }

    protected override DbDataAdapter CreateDataAdapter(DbCommand command)
    {
        return new SqlDataAdapter((SqlCommand)command);
    }
}
