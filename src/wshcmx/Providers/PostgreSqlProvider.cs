using System.Collections.Concurrent;
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

    private static readonly ConcurrentDictionary<string, HashSet<string>> RoutineAllowListCache = new(StringComparer.Ordinal);

    private static bool IsIdentifierStart(char ch) => ch == '_' || char.IsLetter(ch);

    private static bool IsIdentifierPart(char ch) => ch == '_' || char.IsLetterOrDigit(ch);

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
        return $"SELECT * FROM {validatedRoutineName}({string.Join(", ", parameters?.Keys.Select(ValidateRoutineParameterName).Select(parameterName => "@" + parameterName) ?? [])})";
    }

    private static string ValidateRoutineName(string routineName)
    {
        GuardHelper.ThrowIfNull(routineName, nameof(routineName));

        string[] parts = routineName.Split('.');
        if (parts.Length == 0)
        {
            throw new ArgumentException("Routine name cannot be empty.", nameof(routineName));
        }

        foreach (string part in parts)
        {
            _ = ValidateIdentifier(part, nameof(routineName), "routine name");
        }

        return routineName;
    }

    private static string ValidateRoutineParameterName(string parameterName)
    {
        return ValidateIdentifier(parameterName, nameof(parameterName), "parameter name");
    }

    private static string ValidateRoutineNameAgainstAllowList(string connectionString, string routineName, int parameterCount)
    {
        string validatedRoutineName = ValidateRoutineName(routineName);
        string routineKey = BuildRoutineKey(validatedRoutineName, parameterCount);
        HashSet<string> allowList = RoutineAllowListCache.GetOrAdd(connectionString, LoadRoutineAllowList);

        if (allowList.Contains(routineKey))
        {
            return validatedRoutineName;
        }

        HashSet<string> refreshedAllowList = LoadRoutineAllowList(connectionString);
        RoutineAllowListCache[connectionString] = refreshedAllowList;

        if (refreshedAllowList.Contains(routineKey))
        {
            return validatedRoutineName;
        }

        throw new ArgumentException("Routine name is not allowed.", nameof(routineName));
    }

    private static HashSet<string> LoadRoutineAllowList(string connectionString)
    {
        HashSet<string> allowList = new(StringComparer.Ordinal);

        using var connection = new NpgsqlConnection(connectionString);
        using var command = new NpgsqlCommand(LoadRoutineAllowListSql, connection);
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

            for (int argumentCount = minArgumentCount; argumentCount <= maxArgumentCount; argumentCount++)
            {
                allowList.Add(BuildRoutineKey(schemaName + "." + routineName, argumentCount));

                if (isVisible)
                {
                    allowList.Add(BuildRoutineKey(routineName, argumentCount));
                }
            }
        }

        return allowList;
    }

    private static string BuildRoutineKey(string routineName, int parameterCount)
    {
        return NormalizeRoutineName(routineName) + "|" + parameterCount;
    }

    private static string NormalizeRoutineName(string routineName)
    {
        return string.Join(".", routineName.Split('.').Select(part => part.ToLowerInvariant()));
    }

    private static string ValidateIdentifier(string value, string argumentName, string displayName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException($"{displayName} cannot be empty.", argumentName);
        }

        if (!IsIdentifierStart(value[0]))
        {
            throw new ArgumentException($"Invalid {displayName}.", argumentName);
        }

        for (int i = 1; i < value.Length; i++)
        {
            if (!IsIdentifierPart(value[i]))
            {
                throw new ArgumentException($"Invalid {displayName}.", argumentName);
            }
        }

        return value;
    }
}
