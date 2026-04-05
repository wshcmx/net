using System.Data.Common;
using Microsoft.Data.SqlClient;

namespace wshcmx.Providers;

internal sealed class SqlServerProvider : DatabaseProviderBase
{

    protected override DbConnection CreateConnection(string connectionString)
    {
        return new SqlConnection(connectionString);
    }

    protected override DbCommand CreateCommand(string commandText, DbConnection connection)
    {
        if (connection is not SqlConnection sqlConnection)
        {
            throw new ArgumentException("Connection must be of type SqlConnection.", nameof(connection));
        }

        return new SqlCommand(commandText, sqlConnection);
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
