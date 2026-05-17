using System.Data.Common;
using Npgsql;

namespace wshcmx.Net.Providers;

internal class PostgreSqlProvider(string connectionString) : DatabaseProviderBase<NpgsqlConnection>(connectionString)
{
    protected override DbCommand CreateTypedCommand(string commandText, NpgsqlConnection connection)
    {
        return new NpgsqlCommand(commandText, connection);
    }

    protected override DbParameter CreateParameter(string name, object? value)
    {
        return new NpgsqlParameter(name, value ?? DBNull.Value);
    }

    protected override DbDataAdapter CreateDataAdapter(DbCommand command)
    {
        return new NpgsqlDataAdapter((NpgsqlCommand)command);
    }
}
