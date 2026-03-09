using System.Data.Common;
using Microsoft.Data.SqlClient;

namespace wshcmx.Providers;

public class SqlServerProvider : IDatabaseProvider
{
    public DbConnection CreateConnection(string connectionString)
    {
        return new SqlConnection(connectionString);
    }

    public DbCommand CreateCommand(string commandText, DbConnection connection)
    {
        return new SqlCommand(commandText, (SqlConnection)connection);
    }

    public DbParameter CreateParameter(string name, object? value)
    {
        return new SqlParameter(name, value ?? DBNull.Value);
    }

    public DbDataAdapter CreateDataAdapter(DbCommand command)
    {
        return new SqlDataAdapter((SqlCommand)command);
    }

    public string GetParameterPrefix()
    {
        return "@";
    }
}