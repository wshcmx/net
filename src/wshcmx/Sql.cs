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
        {
            command.Parameters.AddWithValue(param.Key, param.Value is null ? DBNull.Value : param.Value.ToString());
        }

        connection.Open();
        using SqlDataAdapter adapter = new(command);
        DataSet ds = new();
        adapter.Fill(ds, page * size, size, ds.Tables[0].TableName);
        List<List<KeyValuePair<string, object?>>> rows = new(capacity: size);

        foreach (DataRow row in ds.Tables[0].Rows)
        {
            List<KeyValuePair<string, object?>> rowList = new(ds.Tables[0].Columns.Count);

            foreach (DataColumn column in ds.Tables[0].Columns)
            {
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
