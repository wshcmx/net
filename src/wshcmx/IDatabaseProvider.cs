using System.Data.Common;

namespace wshcmx;

public interface IDatabaseProvider
{
    DbConnection CreateConnection(string connectionString);
    DbCommand CreateCommand(string commandText, DbConnection connection);
    DbParameter CreateParameter(string name, object? value);
    DbDataAdapter CreateDataAdapter(DbCommand command);
    string GetParameterPrefix();
}