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
        return new NpgsqlCommand(commandText, (NpgsqlConnection)connection);
    }

    public DbParameter CreateParameter(string name, object? value)
    {
        return new NpgsqlParameter(name, value ?? DBNull.Value);
    }

    public DbDataAdapter CreateDataAdapter(DbCommand command)
    {
        return new NpgsqlDataAdapter((NpgsqlCommand)command);
    }

    public string GetParameterPrefix()
    {
        return "@";
    }
}