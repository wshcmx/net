using System.Data;
using System.Data.Common;
using System.Text.Json;
using System.Linq.Dynamic.Core;

namespace wshcmx.Net.Providers;

internal abstract class DatabaseProviderBase<T> : IDatabaseProvider where T : DbConnection, new()
{
    private readonly string _connectionString;

    protected DatabaseProviderBase(string connectionString)
    {
        _connectionString = connectionString;
    }

    public KeyValuePair<string, object?>[][] ExecuteQuery(string commandText)
    {
        using var connection = OpenConnection();
        using var command = CreateTypedCommand(commandText, connection);
        using var reader = command.ExecuteReader();

        var rows = new List<List<KeyValuePair<string, object?>>>();

        while (reader.Read())
        {
            var row = new List<KeyValuePair<string, object?>>(reader.FieldCount);

            for (int i = 0; i < reader.FieldCount; i++)
            {
                row.Add(new(reader.GetName(i), reader.IsDBNull(i) ? null : reader.GetValue(i)));
            }

            rows.Add(row);
        }

        return rows.Select(r => r.ToArray()).ToArray();
    }

    public void ExecuteNonQuery(string commandText)
    {
        using var connection = OpenConnection();
        using var command = CreateTypedCommand(commandText, connection);
        command.ExecuteNonQuery();
    }

    public object[] ExecuteProcedure(string procedureName, string? serializedParameters)
    {
        using var connection = OpenConnection();
        using var command = CreateTypedCommand(procedureName, connection);
        command.CommandType = CommandType.StoredProcedure;

        if (serializedParameters is not null)
        {
            var parameters = JsonSerializer.Deserialize<Dictionary<string, object>>(serializedParameters);

            if (parameters is not null)
            {
                foreach (var param in parameters)
                {
                    command.Parameters.Add(CreateParameter(param.Key, param.Value?.ToString()));
                }
            }
        }

        using var adapter = CreateDataAdapter(command);
        var ds = new DataSet();
        adapter.Fill(ds);

        if (ds.Tables.Count < 1)
            throw new InvalidOperationException($"Stored procedure '{procedureName}' returned no result sets.");

        var rows = new List<List<KeyValuePair<string, object?>>>();

        foreach (DataRow row in ds.Tables[0].Rows)
        {
            var rowList = new List<KeyValuePair<string, object?>>(ds.Tables[0].Columns.Count);

            foreach (DataColumn column in ds.Tables[0].Columns)
            {
                rowList.Add(new(column.ColumnName, row[column] == DBNull.Value ? null : row[column]));
            }

            rows.Add(rowList);
        }

        return rows.Select(r => r.ToArray()).ToArray();
    }

    public object[] ExecutePaginationProcedure(string procedureName, string serializedOptions, string serializedParameters)
    {
        GuardHelper.ThrowIfNull(serializedOptions, nameof(serializedOptions));
        GuardHelper.ThrowIfNull(serializedParameters, nameof(serializedParameters));
        var options = JsonSerializer.Deserialize<Dictionary<string, object>>(serializedOptions);

        _ = int.TryParse(GuardHelper.GetDictionaryValue(options, "page")?.ToString(), out int page);
        if (page < 1) page = 1;

        _ = int.TryParse(GuardHelper.GetDictionaryValue(options, "size")?.ToString(), out int size);
        if (size < 1 || size > 400) size = 400;

        string select = GuardHelper.GetDictionaryValue(options, "select")?.ToString() ?? string.Empty;
        string orderby = GuardHelper.GetDictionaryValue(options, "orderby")?.ToString() ?? string.Empty;

        var parameters = JsonSerializer.Deserialize<Dictionary<string, object>>(serializedParameters);

        using var connection = OpenConnection();
        using var command = CreateTypedCommand(procedureName, connection);
        command.CommandType = CommandType.StoredProcedure;

        if (parameters is not null)
        {
            foreach (var param in parameters)
            {
                command.Parameters.Add(CreateParameter(param.Key, param.Value?.ToString()));
            }
        }

        using var adapter = CreateDataAdapter(command);
        var ds = new DataSet();
        adapter.Fill(ds);

        if (ds.Tables.Count < 2 || ds.Tables[1].Rows.Count == 0 || ds.Tables[1].Columns.Count == 0)
            throw new InvalidOperationException($"Stored procedure '{procedureName}' must return two result sets; the second must contain the total row count.");

        var intermediateResult = ds.Tables[0].Rows.Cast<DataRow>().AsQueryable();

        if (!string.IsNullOrEmpty(orderby))
        {
            intermediateResult = intermediateResult.OrderBy(orderby);
        }

        var columns = string.IsNullOrEmpty(select)
            ? new List<string>()
            : select.Split(',').Select(s => s.Trim()).ToList();

        bool hasSelect = columns.Count > 0;
        intermediateResult = intermediateResult.Skip((page - 1) * size).Take(size);

        var rows = new List<List<KeyValuePair<string, object?>>>(capacity: size);

        foreach (DataRow row in intermediateResult)
        {
            var rowList = new List<KeyValuePair<string, object?>>(ds.Tables[0].Columns.Count);

            foreach (DataColumn column in ds.Tables[0].Columns)
            {
                if (hasSelect && !columns.Contains(column.ColumnName)) continue;
                rowList.Add(new(column.ColumnName, row[column] == DBNull.Value ? null : row[column]));
            }

            rows.Add(rowList);
        }

        _ = int.TryParse(ds.Tables[1].Rows[0][0].ToString(), out int total);
        return new object[] { total, rows.Select(r => r.ToArray()).ToArray() };
    }

    private T OpenConnection()
    {
        var connection = new T();
        try
        {
            connection.ConnectionString = _connectionString;
            connection.Open();
        }
        catch
        {
            connection.Dispose();
            throw;
        }
        return connection;
    }

    protected abstract DbCommand CreateTypedCommand(string commandText, T connection);
    protected abstract DbParameter CreateParameter(string name, object? value);
    protected abstract DbDataAdapter CreateDataAdapter(DbCommand command);
}
