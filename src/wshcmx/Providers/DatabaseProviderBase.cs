using System.Data;
using System.Data.Common;
using System.Text.Json;
using System.Linq.Dynamic.Core;

namespace wshcmx;

internal abstract class DatabaseProviderBase
{
    protected abstract DbConnection CreateConnection(string connectionString);

    protected abstract DbCommand CreateCommand(string commandText, DbConnection connection);

    protected abstract DbParameter CreateParameter(string name, object? value);

    protected abstract DbDataAdapter CreateDataAdapter(DbCommand command);

    internal KeyValuePair<string, object?>[][] ExecuteQuery(string connectionString, string commandText)
    {
        using var connection = CreateConnection(connectionString);
        using var command = CreateCommand(commandText, connection);
        connection.Open();

        using var reader = command.ExecuteReader();
        List<List<KeyValuePair<string, object?>>> rows = new();

        while (reader.Read())
        {
            List<KeyValuePair<string, object?>> row = new(reader.FieldCount);

            for (int i = 0; i < reader.FieldCount; i++)
            {
                row.Add(new(reader.GetName(i), reader.IsDBNull(i) ? null : reader.GetValue(i)));
            }

            rows.Add(row);
        }

        return rows.Select(r => r.ToArray()).ToArray();
    }

    internal void ExecuteNonQuery(string connectionString, string commandText)
    {
        using var connection = CreateConnection(connectionString);
        using var command = CreateCommand(commandText, connection);
        connection.Open();
        command.ExecuteNonQuery();
    }

    internal virtual object[] ExecuteProcedure(string connectionString, string procedureName, string? serializedParameters)
    {
        using var connection = CreateConnection(connectionString);
        using var command = CreateCommand(procedureName, connection);
        command.CommandType = CommandType.StoredProcedure;

        AddParameters(command, DeserializeParameters(serializedParameters), useRoutineQueryParameterNames: false);

        DataSet ds = ExecuteDataSet(connection, command);
        return ds.Tables.Count == 0
            ? Array.Empty<KeyValuePair<string, object?>[]>()
            : ConvertTableToRows(ds.Tables[0]);
    }

    internal virtual object[] ExecutePaginationProcedure(string connectionString, string procedureName, string serializedOptions, string serializedParameters)
    {
        GuardHelper.ThrowIfNull(serializedParameters, nameof(serializedParameters));

        Dictionary<string, object>? parameters = DeserializeParameters(serializedParameters);

        using var connection = CreateConnection(connectionString);
        using var command = CreateCommand(procedureName, connection);
        command.CommandType = CommandType.StoredProcedure;

        AddParameters(command, parameters, useRoutineQueryParameterNames: false);

        DataSet ds = ExecuteDataSet(connection, command);
        int total = TryParseTotalCount(ds.Tables[1].Rows[0][0]);

        return BuildPaginationResult(ds.Tables[0], serializedOptions, total);
    }

    protected DataSet ExecuteDataSet(DbConnection connection, DbCommand command)
    {
        connection.Open();
        using var adapter = CreateDataAdapter(command);
        DataSet ds = new();
        adapter.Fill(ds);
        return ds;
    }

    protected void AddParameters(DbCommand command, Dictionary<string, object>? parameters, bool useRoutineQueryParameterNames)
    {
        if (parameters is null)
        {
            return;
        }

        foreach (var param in parameters)
        {
            string parameterName = useRoutineQueryParameterNames
                ? "@" + param.Key
                : param.Key;

            command.Parameters.Add(CreateParameter(parameterName, param.Value is null ? DBNull.Value : param.Value.ToString()));
        }
    }

    protected static object[] BuildPaginationResult(DataTable table, string serializedOptions, int total)
    {
        PaginationOptions options = ParsePaginationOptions(serializedOptions);
        IQueryable<DataRow> intermediateResult = table.Rows.Cast<DataRow>().AsQueryable();

        if (!string.IsNullOrEmpty(options.OrderBy))
        {
            intermediateResult = intermediateResult.OrderBy(options.OrderBy);
        }

        intermediateResult = intermediateResult
            .Skip((options.Page - 1) * options.Size)
            .Take(options.Size);

        List<List<KeyValuePair<string, object?>>> rows = new(capacity: options.Size);

        foreach (DataRow row in intermediateResult)
        {
            rows.Add(ConvertRowToKeyValuePairs(row, table.Columns, options.SelectedColumns));
        }

        return [total, rows.Select(r => r.ToArray()).ToArray()];
    }

    public static KeyValuePair<string, object?>[][] ConvertTableToRows(DataTable table)
    {
        List<List<KeyValuePair<string, object?>>> rows = new();

        foreach (DataRow row in table.Rows)
        {
            rows.Add(ConvertRowToKeyValuePairs(row, table.Columns, selectedColumns: null));
        }

        return rows.Select(r => r.ToArray()).ToArray();
    }

    private static List<KeyValuePair<string, object?>> ConvertRowToKeyValuePairs(
        DataRow row,
        DataColumnCollection columns,
        HashSet<string>? selectedColumns)
    {
        List<KeyValuePair<string, object?>> rowList = new(columns.Count);

        foreach (DataColumn column in columns)
        {
            if (selectedColumns is not null && !selectedColumns.Contains(column.ColumnName))
            {
                continue;
            }

            rowList.Add(new(column.ColumnName, row[column] == DBNull.Value ? null : row[column]));
        }

        return rowList;
    }

    protected static Dictionary<string, object>? DeserializeParameters(string? serializedParameters)
    {
        return serializedParameters is null
            ? null
            : JsonSerializer.Deserialize<Dictionary<string, object>>(serializedParameters);
    }

    private static PaginationOptions ParsePaginationOptions(string serializedOptions)
    {
        Dictionary<string, object>? options = JsonSerializer.Deserialize<Dictionary<string, object>>(serializedOptions);
        _ = int.TryParse(GuardHelper.GetDictionaryValue(options, "page")?.ToString(), out int page);
        _ = int.TryParse(GuardHelper.GetDictionaryValue(options, "size")?.ToString(), out int size);

        if (page < 1)
        {
            page = 1;
        }

        if (size < 1 || size > 400)
        {
            size = 400;
        }

        string select = GuardHelper.GetDictionaryValue(options, "select")?.ToString() ?? string.Empty;
        HashSet<string>? selectedColumns = string.IsNullOrEmpty(select)
            ? null
            : new HashSet<string>(
                select.Split(',')
                    .Select(column => column.Trim())
                    .Where(column => !string.IsNullOrEmpty(column)));

        return new PaginationOptions(
            page,
            size,
            GuardHelper.GetDictionaryValue(options, "orderby")?.ToString() ?? string.Empty,
            selectedColumns);
    }

    protected static int TryParseTotalCount(object? value)
    {
        _ = int.TryParse(value?.ToString(), out int total);
        return total;
    }

    private sealed class PaginationOptions
    {
        public PaginationOptions(int page, int size, string orderBy, HashSet<string>? selectedColumns)
        {
            Page = page;
            Size = size;
            OrderBy = orderBy;
            SelectedColumns = selectedColumns;
        }

        public int Page { get; }

        public int Size { get; }

        public string OrderBy { get; }

        public HashSet<string>? SelectedColumns { get; }
    }
}
