using System.Data;
using Microsoft.Data.SqlClient;

namespace Internals;

public class Sql : IDisposable
{
    private SqlConnection _connection;

    public Sql()
    {
    }

    public void Init(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new ArgumentException("Connection string cannot be null or empty.", nameof(connectionString));

        _connection = new SqlConnection(connectionString);
        _connection.Open();
    }

    public DataTable ExecuteProcedure(string procedureName, Dictionary<string, object>? parameters = null)
    {
        return Execute(procedureName, CommandType.StoredProcedure, parameters);
    }

    public DataTable ExecuteQuery(string queryText, Dictionary<string, object>? parameters = null)
    {
        return Execute(queryText, CommandType.Text, parameters);
    }

    private DataTable Execute(string commandText, CommandType commandType, Dictionary<string, object>? parameters)
    {
        using var cmd = new SqlCommand(commandText, _connection);
        cmd.CommandType = commandType;
        AddParameters(cmd, parameters);
        using var adapter = new SqlDataAdapter(cmd);
        var table = new DataTable();
        adapter.Fill(table);
        return table;
    }

    public int ExecuteNonQuery(string commandText, CommandType commandType, Dictionary<string, object>? parameters = null)
    {
        using var cmd = new SqlCommand(commandText, _connection);
        cmd.CommandType = commandType;
        AddParameters(cmd, parameters);
        return cmd.ExecuteNonQuery();
    }

    public object ExecuteScalar(string commandText, CommandType commandType, Dictionary<string, object>? parameters = null)
    {
        using var cmd = new SqlCommand(commandText, _connection);
        cmd.CommandType = commandType;
        AddParameters(cmd, parameters);
        return cmd.ExecuteScalar();
    }

    private static void AddParameters(SqlCommand cmd, Dictionary<string, object>? parameters)
    {
        if (parameters == null)
            return;

        foreach (var param in parameters)
        {
            cmd.Parameters.AddWithValue(param.Key, param.Value ?? DBNull.Value);
        }
    }

    public void Dispose()
    {
        if (_connection?.State == ConnectionState.Open)
            _connection.Close();

        _connection?.Dispose();
    }
}