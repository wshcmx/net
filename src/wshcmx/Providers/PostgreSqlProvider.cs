using System.Data;
using System.Data.Common;
using Npgsql;

namespace wshcmx.Providers;

internal sealed class PostgreSqlProvider : DatabaseProviderBase
{
    private const string LoadRoutineAllowListSql = @"
SELECT n.nspname, p.proname, p.pronargs, p.pronargdefaults, pg_function_is_visible(p.oid)
FROM pg_catalog.pg_proc p
JOIN pg_catalog.pg_namespace n ON n.oid = p.pronamespace
WHERE p.prokind = 'f'";

    private HashSet<string>? _routineAllowList;

    internal override object[] ExecuteProcedure(string connectionString, string procedureName, string? serializedParameters)
    {
        Dictionary<string, object>? parameters = DeserializeParameters(serializedParameters);

        using var connection = CreateConnection(connectionString);
        using var command = CreateCommand(BuildRoutineQueryCommandText(connectionString, procedureName, parameters), connection);
        command.CommandType = CommandType.Text;

        AddParameters(command, parameters, useRoutineQueryParameterNames: true);

        DataSet ds = ExecuteDataSet(connection, command);
        return ds.Tables.Count == 0
            ? Array.Empty<KeyValuePair<string, object?>[]>()
            : ConvertTableToRows(ds.Tables[0]);
    }

    protected override DbConnection CreateConnection(string connectionString)
    {
        return new NpgsqlConnection(connectionString);
    }

    protected override DbCommand CreateCommand(string commandText, DbConnection connection)
    {
        if (connection is not NpgsqlConnection npgsqlConnection)
        {
            throw new ArgumentException("Connection must be of type NpgsqlConnection.", nameof(connection));
        }

        return new NpgsqlCommand(commandText, npgsqlConnection);
    }

    protected override DbParameter CreateParameter(string name, object? value)
    {
        return new NpgsqlParameter(name, value ?? DBNull.Value);
    }

    protected override DbDataAdapter CreateDataAdapter(DbCommand command)
    {
        return new NpgsqlDataAdapter((NpgsqlCommand)command);
    }

    internal override object[] ExecutePaginationProcedure(string connectionString, string procedureName, string serializedOptions, string serializedParameters)
    {
        GuardHelper.ThrowIfNull(serializedParameters, nameof(serializedParameters));

        Dictionary<string, object>? parameters = DeserializeParameters(serializedParameters);

        using var connection = CreateConnection(connectionString);
        using var command = CreateCommand(BuildRoutineQueryCommandText(connectionString, procedureName, parameters), connection);
        command.CommandType = CommandType.Text;

        AddParameters(command, parameters, useRoutineQueryParameterNames: true);

        DataSet ds = ExecuteDataSet(connection, command);
        int total = ds.Tables[0].Rows.Count == 0
            ? 0
            : TryParseTotalCount(ds.Tables[0].Rows[0]["_total_count"]);

        return BuildPaginationResult(ds.Tables[0], serializedOptions, total);
    }

    private string BuildRoutineQueryCommandText(string connectionString, string routineName, Dictionary<string, object>? parameters)
    {
        string validatedRoutineName = ValidateRoutineNameAgainstAllowList(connectionString, routineName, parameters?.Count ?? 0);
        return $"SELECT * FROM {validatedRoutineName}({string.Join(", ", parameters?.Keys.Select(parameterName => ValidateIdentifier(parameterName, nameof(parameterName), "parameter name")).Select(parameterName => "@" + parameterName) ?? [])})";
    }

    private static string ValidateRoutineName(string routineName)
    {
        GuardHelper.ThrowIfNull(routineName, nameof(routineName));

        string[] parts = routineName.Split('.');
        if (parts.Length == 0)
        {
            throw new ArgumentException("Routine name cannot be empty.", nameof(routineName));
        }

        Array.ForEach(parts, part => ValidateIdentifier(part, nameof(routineName), "routine name"));

        return routineName;
    }

    private string ValidateRoutineNameAgainstAllowList(string connectionString, string routineName, int parameterCount)
    {
        string validatedRoutineName = ValidateRoutineName(routineName);
        string routineKey = string.Join(".", validatedRoutineName.Split('.').Select(part => part.ToLowerInvariant())) + "|" + parameterCount;
        HashSet<string> allowList = _routineAllowList ??= LoadRoutineAllowList(connectionString);

        if (allowList.Contains(routineKey))
        {
            return validatedRoutineName;
        }

        throw new ArgumentException("Routine name is not allowed.", nameof(routineName));
    }

    private HashSet<string> LoadRoutineAllowList(string connectionString)
    {
        HashSet<string> allowList = new(StringComparer.Ordinal);

        using var connection = CreateConnection(connectionString);
        using var command = CreateCommand(LoadRoutineAllowListSql, connection);
        connection.Open();

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            string schemaName = reader.GetString(0);
            string routineName = reader.GetString(1);
            int maxArgumentCount = reader.GetInt16(2);
            int defaultArgumentCount = reader.GetInt16(3);
            bool isVisible = reader.GetBoolean(4);
            int minArgumentCount = maxArgumentCount - defaultArgumentCount;
            string qualifiedRoutineKeyPrefix = string.Join(".", (schemaName + "." + routineName).Split('.').Select(part => part.ToLowerInvariant()));
            string? visibleRoutineKeyPrefix = isVisible
                ? string.Join(".", routineName.Split('.').Select(part => part.ToLowerInvariant()))
                : null;

            for (int argumentCount = minArgumentCount; argumentCount <= maxArgumentCount; argumentCount++)
            {
                allowList.Add(qualifiedRoutineKeyPrefix + "|" + argumentCount);

                if (visibleRoutineKeyPrefix is not null)
                {
                    allowList.Add(visibleRoutineKeyPrefix + "|" + argumentCount);
                }
            }
        }

        return allowList;
    }

    private static string ValidateIdentifier(string value, string argumentName, string displayName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException($"{displayName} cannot be empty.", argumentName);
        }

        if (!(value[0] == '_' || char.IsLetter(value[0])))
        {
            throw new ArgumentException($"Invalid {displayName}.", argumentName);
        }

        value.All(c => char.IsLetterOrDigit(c) || c == '_');

        return value;
    }
}
