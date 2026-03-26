using System.Data.Common;
using Microsoft.Data.SqlClient;

namespace wshcmx.Providers;

internal class SqlServerProvider : IDatabaseProvider
{
    public DbConnection CreateConnection(string connectionString)
    {
        return new SqlConnection(connectionString);
    }

    public DbCommand CreateCommand(string commandText, DbConnection connection)
    {
        if (connection is not SqlConnection sqlConnection)
        {
            throw new ArgumentException("Connection must be of type SqlConnection.", nameof(connection));
        }
        return new SqlCommand(commandText, sqlConnection);
    }

    public DbParameter CreateParameter(string name, object? value)
    {
        return new SqlParameter(name, value ?? DBNull.Value);
    }

    public DbDataAdapter CreateDataAdapter(DbCommand command)
    {
        return new SqlDataAdapter((SqlCommand)command);
    }
}
