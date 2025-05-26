using System.Data;
using System.Text.Json;

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
        var rows = new List<List<KeyValuePair<string, object?>>>(capacity: 16);
        while (reader.Read())
        {
            var row = new List<KeyValuePair<string, object?>>(reader.FieldCount);
            for (int i = 0; i < reader.FieldCount; i++)
                row.Add(new KeyValuePair<string, object?>(reader.GetName(i), reader.IsDBNull(i) ? null : reader.GetValue(i)));
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

    public object[] ExecutePaginationProcedure(string procedureName, int page, int size, string serializedParameters)
    {
        ArgumentNullException.ThrowIfNull(_connectionString, nameof(_connectionString));
        ArgumentNullException.ThrowIfNull(serializedParameters, nameof(serializedParameters));
        ArgumentNullException.ThrowIfNull(page, nameof(page));
        ArgumentNullException.ThrowIfNull(size, nameof(size));

        Dictionary<string, object>? parameters = JsonSerializer.Deserialize<Dictionary<string, object>>(serializedParameters);

        ArgumentNullException.ThrowIfNull(parameters, nameof(parameters));

        using SqlConnection connection = new(_connectionString);
        using SqlCommand command = new(procedureName, connection)
        {
            CommandType = CommandType.StoredProcedure
        };

        foreach (var param in parameters)
            command.Parameters.AddWithValue(param.Key, param.Value is null ? DBNull.Value : param.Value.ToString());

        connection.Open();
        using var adapter = new SqlDataAdapter(command);
        var ds = new DataSet();
        adapter.Fill(ds);
        var rows = new List<List<KeyValuePair<string, object?>>>(capacity: size);

        foreach (DataRow row in ds.Tables[0].AsEnumerable().Skip(page * size).Take(size))
        {
            var rowList = new List<KeyValuePair<string, object?>>(ds.Tables[0].Columns.Count);
            foreach (DataColumn column in ds.Tables[0].Columns)
                rowList.Add(new KeyValuePair<string, object?>(column.ColumnName,
                    row[column] == DBNull.Value ? null : row[column]));
            rows.Add(rowList);
        }

        _ = int.TryParse(ds.Tables[1].Rows[0][0].ToString(), out int total);
        var items = rows.Select(r => r.ToArray()).ToArray();
        return new object[] { total, items };
    }

    public object[] ExecuteProcedure(string procedureName, string serializedParameters)
    {
        ArgumentNullException.ThrowIfNull(_connectionString, nameof(_connectionString));
        ArgumentNullException.ThrowIfNull(serializedParameters, nameof(serializedParameters));
        Dictionary<string, object>? parameters = JsonSerializer.Deserialize<Dictionary<string, object>>(serializedParameters);
        ArgumentNullException.ThrowIfNull(parameters, nameof(parameters));

        using SqlConnection connection = new(_connectionString);
        using SqlCommand command = new(procedureName, connection)
        {
            CommandType = CommandType.StoredProcedure
        };

        foreach (var param in parameters)
            command.Parameters.AddWithValue(param.Key, param.Value is null ? DBNull.Value : param.Value.ToString());

        connection.Open();
        using var adapter = new SqlDataAdapter(command);
        var ds = new DataSet();
        adapter.Fill(ds);
        var rows = new List<List<KeyValuePair<string, object?>>>();

        foreach (DataRow row in ds.Tables[0].AsEnumerable())
        {
            var rowList = new List<KeyValuePair<string, object?>>(ds.Tables[0].Columns.Count);
            foreach (DataColumn column in ds.Tables[0].Columns)
                rowList.Add(new KeyValuePair<string, object?>(column.ColumnName,
                    row[column] == DBNull.Value ? null : row[column]));
            rows.Add(rowList);
        }

        return rows.Select(r => r.ToArray()).ToArray();
    }
}
