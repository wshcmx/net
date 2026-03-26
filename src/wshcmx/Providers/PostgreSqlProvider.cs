using System.Data.Common;
using Npgsql;

namespace wshcmx.Providers;

public class PostgreSqlProvider : IDatabaseProvider
{
    public DbConnection CreateConnection(string connectionString)
    {
        return new NpgsqlConnection(connectionString);
    }

    public DbCommand CreateCommand(string commandText, DbConnection connection)
    {
        if (connection is not NpgsqlConnection npgsqlConnection)
        {
            throw new ArgumentException("Connection must be of type NpgsqlConnection.", nameof(connection));
        }
        return new NpgsqlCommand(commandText, npgsqlConnection);
    }

    public DbParameter CreateParameter(string name, object? value)
    {
        return new NpgsqlParameter(name, value ?? DBNull.Value);
    }

    public DbDataAdapter CreateDataAdapter(DbCommand command)
    {
        return new NpgsqlDataAdapter((NpgsqlCommand)command);
    }
}
