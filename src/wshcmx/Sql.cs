using System.Data;
using System.Text.Json;
using System.Linq.Dynamic.Core;

using Microsoft.Data.SqlClient;

namespace wshcmx;

public class Sql
{
    private string? _connectionString;

    public void Init(string connectionString)
    {
        _connectionString = connectionString;
    }

    public KeyValuePair<string, object?>[][] ExecuteQuery(string commandText)
    {
        ArgumentNullException.ThrowIfNull(_connectionString, nameof(_connectionString));

        using SqlConnection connection = new(_connectionString);
        using SqlCommand command = new(commandText, connection);
        connection.Open();

        using SqlDataReader reader = command.ExecuteReader();
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

    public void ExecuteNonQuery(string commandText)
    {
        ArgumentNullException.ThrowIfNull(_connectionString, nameof(_connectionString));
        using SqlConnection connection = new(_connectionString);
        using SqlCommand command = new(commandText, connection);
        connection.Open();
        command.ExecuteNonQuery();
    }

    public object[] ExecutePaginationProcedure(string procedureName, string serializedOptions, string serializedParameters)
    {
        ArgumentNullException.ThrowIfNull(_connectionString, nameof(_connectionString));
        ArgumentNullException.ThrowIfNull(serializedParameters, nameof(serializedParameters));

        Dictionary<string, object>? options = JsonSerializer.Deserialize<Dictionary<string, object>>(serializedOptions);
        _ = int.TryParse(options?.GetValueOrDefault("page")?.ToString(), out int page);

        if (page < 1)
        {
            page = 1;
        }

        _ = int.TryParse(options?.GetValueOrDefault("size")?.ToString(), out int size);

        if (size < 1 || size > 400)
        {
            size = 400;
        }

        string select = options?.GetValueOrDefault("select")?.ToString() ?? string.Empty;
        string orderby = options?.GetValueOrDefault("orderby")?.ToString() ?? string.Empty;

        Dictionary<string, object>? parameters = JsonSerializer.Deserialize<Dictionary<string, object>>(serializedParameters);

        using SqlConnection connection = new(_connectionString);
        using SqlCommand command = new(procedureName, connection)
        {
            CommandType = CommandType.StoredProcedure
        };

        if (parameters is not null)
        {
            foreach (var param in parameters)
            {
                command.Parameters.AddWithValue(param.Key, param.Value is null ? DBNull.Value : param.Value.ToString());
            }
        }

        connection.Open();
        using SqlDataAdapter adapter = new(command);
        DataSet ds = new();
        adapter.Fill(ds);
        var intermediateResult = ds.Tables[0].AsEnumerable().AsQueryable();

        if (!string.IsNullOrEmpty(orderby))
        {
            intermediateResult = intermediateResult.OrderBy(orderby);
        }

        List<string> columns = new();

        if (!string.IsNullOrEmpty(select))
        {
            select.Split(',')
                .Select(s => s.Trim())
                .ToList()
                .ForEach(c => columns.Add(c));
        }

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
        return new object[] { total, rows.Select(r => r.ToArray()).ToArray() };
    }

    public object[] ExecuteProcedure(string procedureName, string? serializedParameters)
    {
        ArgumentNullException.ThrowIfNull(_connectionString, nameof(_connectionString));

        using SqlConnection connection = new(_connectionString);
        using SqlCommand command = new(procedureName, connection)
        {
            CommandType = CommandType.StoredProcedure
        };

        if (serializedParameters is not null)
        {
            Dictionary<string, object>? parameters = JsonSerializer.Deserialize<Dictionary<string, object>>(serializedParameters);

            if (parameters is not null)
            {
                foreach (var param in parameters)
                {
                    command.Parameters.AddWithValue(param.Key, param.Value is null ? DBNull.Value : param.Value.ToString());
                }
            }
        }

        connection.Open();
        using SqlDataAdapter adapter = new(command);
        DataSet ds = new();
        adapter.Fill(ds);
        List<List<KeyValuePair<string, object?>>> rows = new();

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
}
