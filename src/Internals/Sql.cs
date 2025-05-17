using System.Data;
using System.Text.Json;

using Microsoft.Data.SqlClient;

namespace Internals;

public class Sql
{
    private readonly string _connectionString;

    public Sql(string connectionString)
    {
        _connectionString = connectionString;
    }

    public void ExecuteNonQuery(string procedureName)
    {
        ArgumentNullException.ThrowIfNull(_connectionString, nameof(_connectionString));
        using SqlConnection connection = new(_connectionString);
        using SqlCommand command = new(procedureName, connection);
        connection.Open();
        command.ExecuteNonQuery();
    }

    public (int total, IEnumerable<Dictionary<string, object>> items) ExecutePaginationProcedure(string procedureName, int page, int size, string serializedParameters)
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
            command.Parameters.AddWithValue(param.Key, param.Value.ToString());

        connection.Open();
        using var adapter = new SqlDataAdapter(command);
        var ds = new DataSet();
        adapter.Fill(ds);
        List<Dictionary<string, object>> items = new();

        foreach (DataRow row in ds.Tables[0].AsEnumerable().Skip(page * size).Take(size))
        {
            Dictionary<string, object> obj = new();

            foreach (DataColumn column in ds.Tables[0].Columns)
                obj[column.ColumnName] = row[column.ColumnName]?.ToString() ?? string.Empty;

            items.Add(obj);
        }

        _ = int.TryParse(ds.Tables[1].Rows[0][0].ToString(), out int total);
        return (total, items);
    }

    public IEnumerable<Dictionary<string, object>> ExecuteProcedure(string procedureName, string serializedParameters)
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
            command.Parameters.AddWithValue(param.Key, param.Value.ToString());

        connection.Open();
        using var adapter = new SqlDataAdapter(command);
        var ds = new DataSet();
        adapter.Fill(ds);
        List<Dictionary<string, object>> items = new();

        foreach (DataRow row in ds.Tables[0].Rows)
        {
            Dictionary<string, object> obj = new();

            foreach (DataColumn column in ds.Tables[0].Columns)
                obj[column.ColumnName] = row[column.ColumnName]?.ToString() ?? string.Empty;

            items.Add(obj);
        }

        return items;
    }
}