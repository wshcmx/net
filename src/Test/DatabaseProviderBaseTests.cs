using System.Data;
using System.Text.Json;
using Test.Fakes;

namespace Test;

public class DatabaseProviderBaseTests
{
    private static DataTable BuildTable(string[] columns, params object?[][] rows)
    {
        DataTable table = new();

        foreach (string c in columns)
        {
            _ = table.Columns.Add(c, typeof(object));
        }

        foreach (object?[] r in rows)
        {
            _ = table.Rows.Add(r);
        }

        return table;
    }

    [Fact]
    public void ExecuteQuery_ReturnsRowsAndMapsDbNullToNull()
    {
        FakeProvider provider = new()
        {
            ReaderResult = BuildTable(["id", "name"], [1, "alice"], [2, DBNull.Value])
        };

        KeyValuePair<string, object?>[][] result = provider.ExecuteQuery("SELECT * FROM t");

        Assert.Equal(2, result.Length);
        Assert.Equal("id", result[0][0].Key);
        Assert.Equal(1, result[0][0].Value);
        Assert.Equal("alice", result[0][1].Value);
        Assert.Null(result[1][1].Value);
    }

    [Fact]
    public void ExecuteQuery_EmptyReader_ReturnsEmptyArray()
    {
        FakeProvider provider = new() { ReaderResult = BuildTable(["id"]) };

        KeyValuePair<string, object?>[][] result = provider.ExecuteQuery("SELECT * FROM t");

        Assert.Empty(result);
    }

    [Fact]
    public void ExecuteNonQuery_InvokesCommandExecuteNonQuery()
    {
        FakeProvider provider = new();

        provider.ExecuteNonQuery("DELETE FROM t");

        FakeDbCommand cmd = Assert.Single(provider.Commands);
        Assert.Equal("DELETE FROM t", cmd.CommandText);
        Assert.Equal(1, cmd.ExecuteNonQueryCallCount);
    }

    [Fact]
    public void ExecuteProcedure_NullParameters_DoesNotAddParameters()
    {
        DataSet ds = new();
        ds.Tables.Add(BuildTable(["id"], [42]));
        FakeProvider provider = new() { AdapterResult = ds };

        object[] result = provider.ExecuteProcedure("sp_test", null);

        FakeDbCommand cmd = Assert.Single(provider.Commands);
        Assert.Equal(CommandType.StoredProcedure, cmd.CommandType);
        Assert.Empty(cmd.Parameters);
        _ = Assert.Single(result);
    }

    [Fact]
    public void ExecuteProcedure_WithParameters_AddsThemToCommand()
    {
        DataSet ds = new();
        ds.Tables.Add(BuildTable(["id"]));
        FakeProvider provider = new() { AdapterResult = ds };
        string json = JsonSerializer.Serialize(new Dictionary<string, object> { ["name"] = "bob", ["age"] = 30 });

        _ = provider.ExecuteProcedure("sp_test", json);

        FakeDbCommand cmd = Assert.Single(provider.Commands);
        Assert.Equal(2, cmd.Parameters.Count);
        Assert.Equal("name", cmd.Parameters[0].ParameterName);
        Assert.Equal("bob", cmd.Parameters[0].Value);
        Assert.Equal("age", cmd.Parameters[1].ParameterName);
        Assert.Equal("30", cmd.Parameters[1].Value);
    }

    [Fact]
    public void ExecuteProcedure_NoTables_ThrowsInvalidOperationException()
    {
        FakeProvider provider = new() { AdapterResult = new DataSet() };

        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() => provider.ExecuteProcedure("sp_test", null));
        Assert.Contains("sp_test", ex.Message);
    }

    [Fact]
    public void ExecutePaginationProcedure_NullSerializedOptions_ThrowsWithCorrectParamName()
    {
        FakeProvider provider = new();

        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() => provider.ExecutePaginationProcedure("sp", null!, "{}"));
        Assert.Equal("serializedOptions", ex.ParamName);
    }

    [Fact]
    public void ExecutePaginationProcedure_NullSerializedParameters_ThrowsWithCorrectParamName()
    {
        FakeProvider provider = new();
        string options = JsonSerializer.Serialize(new Dictionary<string, object> { ["page"] = 1, ["size"] = 10 });

        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() => provider.ExecutePaginationProcedure("sp", options, null!));
        Assert.Equal("serializedParameters", ex.ParamName);
    }

    [Fact]
    public void ExecutePaginationProcedure_NoTables_ThrowsInvalidOperationException()
    {
        FakeProvider provider = new() { AdapterResult = new DataSet() };
        string options = JsonSerializer.Serialize(new Dictionary<string, object> { ["page"] = 1, ["size"] = 10 });

        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() =>
            provider.ExecutePaginationProcedure("sp_pag", options, "{}"));
        Assert.Contains("sp_pag", ex.Message);
    }

    [Fact]
    public void ExecutePaginationProcedure_OnlyOneTable_ThrowsInvalidOperationException()
    {
        DataSet ds = new();
        ds.Tables.Add(BuildTable(["id"], [1], [2]));
        FakeProvider provider = new() { AdapterResult = ds };
        string options = JsonSerializer.Serialize(new Dictionary<string, object> { ["page"] = 1, ["size"] = 10 });

        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() =>
            provider.ExecutePaginationProcedure("sp_pag", options, "{}"));
        Assert.Contains("two result sets", ex.Message);
    }

    [Fact]
    public void ExecutePaginationProcedure_SecondTableWithoutTotal_ThrowsInvalidOperationException()
    {
        DataSet ds = new();
        ds.Tables.Add(BuildTable(["id"], [1], [2]));
        ds.Tables.Add(BuildTable(["total"]));
        FakeProvider provider = new() { AdapterResult = ds };
        string options = JsonSerializer.Serialize(new Dictionary<string, object> { ["page"] = 1, ["size"] = 10 });

        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() =>
            provider.ExecutePaginationProcedure("sp_pag", options, "{}"));
        Assert.Contains("total row count", ex.Message);
    }

    [Fact]
    public void ExecutePaginationProcedure_ReturnsTotalAndPagedRows()
    {
        DataSet ds = new();
        ds.Tables.Add(BuildTable(["id", "name"], [1, "a"], [2, "b"], [3, "c"]));
        DataTable totals = BuildTable(["total"], [42]);
        ds.Tables.Add(totals);
        FakeProvider provider = new() { AdapterResult = ds };
        string options = JsonSerializer.Serialize(new Dictionary<string, object> { ["page"] = 1, ["size"] = 10 });

        object[] result = provider.ExecutePaginationProcedure("sp", options, "{}");

        Assert.Equal(2, result.Length);
        Assert.Equal(42, result[0]);
        KeyValuePair<string, object?>[][] rows = Assert.IsType<KeyValuePair<string, object?>[][]>(result[1]);
        Assert.Equal(3, rows.Length);
    }

    [Fact]
    public void ExecutePaginationProcedure_AppliesPagination()
    {
        DataSet ds = new();
        ds.Tables.Add(BuildTable(["id"], [1], [2], [3], [4], [5]));
        ds.Tables.Add(BuildTable(["total"], [5]));
        FakeProvider provider = new() { AdapterResult = ds };
        string options = JsonSerializer.Serialize(new Dictionary<string, object> { ["page"] = 2, ["size"] = 2, ["orderby"] = "it[\"id\"]" });

        object[] result = provider.ExecutePaginationProcedure("sp", options, "{}");

        KeyValuePair<string, object?>[][] rows = Assert.IsType<KeyValuePair<string, object?>[][]>(result[1]);
        Assert.Equal(2, rows.Length);
        Assert.Equal(3, rows[0][0].Value);
        Assert.Equal(4, rows[1][0].Value);
    }

    [Fact]
    public void ExecutePaginationProcedure_AppliesOrderbyAscending()
    {
        DataSet ds = new();
        ds.Tables.Add(BuildTable(["id"], [3], [1], [2]));
        ds.Tables.Add(BuildTable(["total"], [3]));
        FakeProvider provider = new() { AdapterResult = ds };
        string options = JsonSerializer.Serialize(new Dictionary<string, object>
        {
            ["page"] = 1,
            ["size"] = 10,
            ["orderby"] = "it[\"id\"]"
        });

        object[] result = provider.ExecutePaginationProcedure("sp", options, "{}");

        KeyValuePair<string, object?>[][] rows = Assert.IsType<KeyValuePair<string, object?>[][]>(result[1]);
        Assert.Equal(3, rows.Length);
        Assert.Equal(1, rows[0][0].Value);
        Assert.Equal(2, rows[1][0].Value);
        Assert.Equal(3, rows[2][0].Value);
    }

    [Fact]
    public void ExecutePaginationProcedure_AppliesOrderbyDescending()
    {
        DataSet ds = new();
        ds.Tables.Add(BuildTable(["id"], [1], [3], [2]));
        ds.Tables.Add(BuildTable(["total"], [3]));
        FakeProvider provider = new() { AdapterResult = ds };
        string options = JsonSerializer.Serialize(new Dictionary<string, object>
        {
            ["page"] = 1,
            ["size"] = 10,
            ["orderby"] = "it[\"id\"] desc"
        });

        object[] result = provider.ExecutePaginationProcedure("sp", options, "{}");

        KeyValuePair<string, object?>[][] rows = Assert.IsType<KeyValuePair<string, object?>[][]>(result[1]);
        Assert.Equal(3, rows[0][0].Value);
        Assert.Equal(2, rows[1][0].Value);
        Assert.Equal(1, rows[2][0].Value);
    }

    [Fact]
    public void ExecuteQuery_NullCommandText_ThrowsArgumentNullException()
    {
        FakeProvider provider = new();
        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() => provider.ExecuteQuery(null!));
        Assert.Equal("commandText", ex.ParamName);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void ExecuteQuery_WhitespaceCommandText_ThrowsArgumentException(string commandText)
    {
        FakeProvider provider = new();
        ArgumentException ex = Assert.Throws<ArgumentException>(() => provider.ExecuteQuery(commandText));
        Assert.Equal("commandText", ex.ParamName);
    }

    [Fact]
    public void ExecuteNonQuery_NullCommandText_ThrowsArgumentNullException()
    {
        FakeProvider provider = new();
        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() => provider.ExecuteNonQuery(null!));
        Assert.Equal("commandText", ex.ParamName);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void ExecuteNonQuery_WhitespaceCommandText_ThrowsArgumentException(string commandText)
    {
        FakeProvider provider = new();
        ArgumentException ex = Assert.Throws<ArgumentException>(() => provider.ExecuteNonQuery(commandText));
        Assert.Equal("commandText", ex.ParamName);
    }

    [Fact]
    public void ExecuteProcedure_NullProcedureName_ThrowsArgumentNullException()
    {
        FakeProvider provider = new();
        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() => provider.ExecuteProcedure(null!, null));
        Assert.Equal("procedureName", ex.ParamName);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void ExecuteProcedure_WhitespaceProcedureName_ThrowsArgumentException(string procedureName)
    {
        FakeProvider provider = new();
        ArgumentException ex = Assert.Throws<ArgumentException>(() => provider.ExecuteProcedure(procedureName, null));
        Assert.Equal("procedureName", ex.ParamName);
    }

    [Fact]
    public void ExecutePaginationProcedure_NullProcedureName_ThrowsArgumentNullException()
    {
        FakeProvider provider = new();
        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() => provider.ExecutePaginationProcedure(null!, "{}", "{}"));
        Assert.Equal("procedureName", ex.ParamName);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void ExecutePaginationProcedure_WhitespaceProcedureName_ThrowsArgumentException(string procedureName)
    {
        FakeProvider provider = new();
        ArgumentException ex = Assert.Throws<ArgumentException>(() => provider.ExecutePaginationProcedure(procedureName, "{}", "{}"));
        Assert.Equal("procedureName", ex.ParamName);
    }

    [Fact]
    public void ExecuteProcedure_MalformedJson_DoesNotOpenConnection()
    {
        FakeProvider provider = new();

        _ = Assert.ThrowsAny<Exception>(() => provider.ExecuteProcedure("sp", "not-json"));

        Assert.Empty(provider.CreatedConnections);
    }

    [Fact]
    public void ExecutePaginationProcedure_AppliesSelectFilter()
    {
        DataSet ds = new();
        ds.Tables.Add(BuildTable(["id", "name", "age"], [1, "a", 10]));
        ds.Tables.Add(BuildTable(["total"], [1]));
        FakeProvider provider = new() { AdapterResult = ds };
        string options = JsonSerializer.Serialize(new Dictionary<string, object>
        {
            ["page"] = 1,
            ["size"] = 10,
            ["select"] = "id,age"
        });

        object[] result = provider.ExecutePaginationProcedure("sp", options, "{}");

        KeyValuePair<string, object?>[][] rows = Assert.IsType<KeyValuePair<string, object?>[][]>(result[1]);
        KeyValuePair<string, object?>[] row = Assert.Single(rows);
        Assert.Equal(2, row.Length);
        Assert.Equal("id", row[0].Key);
        Assert.Equal("age", row[1].Key);
    }

    [Fact]
    public void Connection_WhenOpenThrows_IsDisposed()
    {
        FakeProvider provider = new() { OpenShouldThrow = true };

        _ = Assert.Throws<InvalidOperationException>(() => provider.ExecuteNonQuery("noop"));

        FakeDbConnection conn = Assert.Single(provider.CreatedConnections);
        Assert.True(conn.WasDisposed);
        Assert.False(conn.WasOpened);
    }

    [Fact]
    public void Connection_WhenOpenSucceeds_IsOpenedAndDisposed()
    {
        FakeProvider provider = new();

        provider.ExecuteNonQuery("noop");

        FakeDbConnection conn = Assert.Single(provider.CreatedConnections);
        Assert.True(conn.WasOpened);
        Assert.True(conn.WasDisposed);
    }
}
