using System.Data;
using System.Data.Common;
using System.Text.Json;
using System.Linq.Dynamic.Core;

namespace wshcmx.Net.Database.Providers;

internal abstract class DatabaseProviderBase<T>(string connectionString) : IDatabaseProvider where T : DbConnection, new()
{
    private readonly string _connectionString = connectionString;

    public KeyValuePair<string, object?>[][] ExecuteQuery(string commandText)
    {
        GuardHelper.ThrowIfNull(commandText, nameof(commandText));
        GuardHelper.ThrowIfWhiteSpace(commandText, nameof(commandText));

        using T connection = CreateConnection();
        connection.ConnectionString = _connectionString;
        connection.Open();
        using DbCommand command = CreateTypedCommand(commandText, connection);
        using DbDataReader reader = command.ExecuteReader();

        List<List<KeyValuePair<string, object?>>> rows = [];

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

    public void ExecuteNonQuery(string commandText)
    {
        GuardHelper.ThrowIfNull(commandText, nameof(commandText));
        GuardHelper.ThrowIfWhiteSpace(commandText, nameof(commandText));

        using T connection = CreateConnection();
        connection.ConnectionString = _connectionString;
        connection.Open();
        using DbCommand command = CreateTypedCommand(commandText, connection);
        _ = command.ExecuteNonQuery();
    }

    public object[] ExecuteProcedure(string procedureName, string? serializedParameters)
    {
        GuardHelper.ThrowIfNull(procedureName, nameof(procedureName));
        GuardHelper.ThrowIfWhiteSpace(procedureName, nameof(procedureName));

        Dictionary<string, object>? parameters = null;
        if (serializedParameters is not null)
        {
            parameters = JsonSerializer.Deserialize<Dictionary<string, object>>(serializedParameters);
        }

        using T connection = CreateConnection();
        connection.ConnectionString = _connectionString;
        connection.Open();
        using DbCommand command = CreateTypedCommand(procedureName, connection);
        command.CommandType = CommandType.StoredProcedure;

        if (parameters is not null)
        {
            foreach (KeyValuePair<string, object> param in parameters)
            {
                _ = command.Parameters.Add(CreateParameter(param.Key, param.Value?.ToString()));
            }
        }

        using DbDataAdapter adapter = CreateDataAdapter(command);
        DataSet ds = new();
        _ = adapter.Fill(ds);

        if (ds.Tables.Count < 1)
        {
            throw new InvalidOperationException($"Stored procedure '{procedureName}' returned no result sets.");
        }

        List<List<KeyValuePair<string, object?>>> rows = [];

        foreach (DataRow row in ds.Tables[0].Rows)
        {
            List<KeyValuePair<string, object?>> rowList = new(ds.Tables[0].Columns.Count);

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
        GuardHelper.ThrowIfNull(procedureName, nameof(procedureName));
        GuardHelper.ThrowIfWhiteSpace(procedureName, nameof(procedureName));
        GuardHelper.ThrowIfNull(serializedOptions, nameof(serializedOptions));
        GuardHelper.ThrowIfNull(serializedParameters, nameof(serializedParameters));
        Dictionary<string, object>? options = JsonSerializer.Deserialize<Dictionary<string, object>>(serializedOptions);

        _ = int.TryParse(GuardHelper.GetDictionaryValue(options, "page")?.ToString(), out int page);
        if (page < 1)
        {
            page = 1;
        }

        _ = int.TryParse(GuardHelper.GetDictionaryValue(options, "size")?.ToString(), out int size);

        if (size is < 1 or > 400)
        {
            size = 400;
        }

        string select = GuardHelper.GetDictionaryValue(options, "select")?.ToString() ?? string.Empty;
        string orderby = GuardHelper.GetDictionaryValue(options, "orderby")?.ToString() ?? string.Empty;

        Dictionary<string, object>? parameters = JsonSerializer.Deserialize<Dictionary<string, object>>(serializedParameters);

        using T connection = CreateConnection();
        connection.ConnectionString = _connectionString;
        connection.Open();
        using DbCommand command = CreateTypedCommand(procedureName, connection);
        command.CommandType = CommandType.StoredProcedure;

        if (parameters is not null)
        {
            foreach (KeyValuePair<string, object> param in parameters)
            {
                _ = command.Parameters.Add(CreateParameter(param.Key, param.Value?.ToString()));
            }
        }

        using DbDataAdapter adapter = CreateDataAdapter(command);
        DataSet ds = new();
        _ = adapter.Fill(ds);

        if (ds.Tables.Count < 2 || ds.Tables[1].Rows.Count == 0 || ds.Tables[1].Columns.Count == 0)
        {
            throw new InvalidOperationException($"Stored procedure '{procedureName}' must return two result sets; the second must contain the total row count.");
        }

        IQueryable<DataRow> intermediateResult = ds.Tables[0].Rows.Cast<DataRow>().AsQueryable();

        if (!string.IsNullOrEmpty(orderby))
        {
            intermediateResult = intermediateResult.OrderBy(orderby);
        }

        List<string> columns = string.IsNullOrEmpty(select)
            ? []
            : select.Split(',').Select(s => s.Trim()).ToList();

        bool hasSelect = columns.Count > 0;
        intermediateResult = intermediateResult.Skip((page - 1) * size).Take(size);

        List<List<KeyValuePair<string, object?>>> rows = new(capacity: size);

        foreach (DataRow row in intermediateResult)
        {
            List<KeyValuePair<string, object?>> rowList = new(ds.Tables[0].Columns.Count);

            foreach (DataColumn column in ds.Tables[0].Columns)
            {
                if (hasSelect && !columns.Contains(column.ColumnName))
                {
                    continue;
                }
                rowList.Add(new(column.ColumnName, row[column] == DBNull.Value ? null : row[column]));
            }

            rows.Add(rowList);
        }

        _ = int.TryParse(ds.Tables[1].Rows[0][0].ToString(), out int total);
        return [total, rows.Select(r => r.ToArray()).ToArray()];
    }

    protected virtual T CreateConnection()
    {
        return new T();
    }

    protected abstract DbCommand CreateTypedCommand(string commandText, T connection);
    protected abstract DbParameter CreateParameter(string name, object? value);
    protected abstract DbDataAdapter CreateDataAdapter(DbCommand command);
}
