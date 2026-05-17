using System.Data;
using System.Text.Json;
using Test.Fakes;

namespace Test;

public class DatabaseProviderBaseTests : IDisposable
{
    public DatabaseProviderBaseTests() => FakeDbConnection.Reset();
    public void Dispose() => FakeDbConnection.Reset();

    private static DataTable BuildTable(string[] columns, params object?[][] rows)
    {
        var table = new DataTable();
        foreach (var c in columns) table.Columns.Add(c, typeof(object));
        foreach (var r in rows) table.Rows.Add(r);
        return table;
    }

    [Fact]
    public void ExecuteQuery_ReturnsRowsAndMapsDbNullToNull()
    {
        var provider = new FakeProvider
        {
            ReaderResult = BuildTable(["id", "name"], [1, "alice"], [2, DBNull.Value])
        };

        var result = provider.ExecuteQuery("SELECT * FROM t");

        Assert.Equal(2, result.Length);
        Assert.Equal("id", result[0][0].Key);
        Assert.Equal(1, result[0][0].Value);
        Assert.Equal("alice", result[0][1].Value);
        Assert.Null(result[1][1].Value);
    }

    [Fact]
    public void ExecuteQuery_EmptyReader_ReturnsEmptyArray()
    {
        var provider = new FakeProvider { ReaderResult = BuildTable(["id"]) };

        var result = provider.ExecuteQuery("SELECT * FROM t");

        Assert.Empty(result);
    }

    [Fact]
    public void ExecuteNonQuery_InvokesCommandExecuteNonQuery()
    {
        var provider = new FakeProvider();

        provider.ExecuteNonQuery("DELETE FROM t");

        var cmd = Assert.Single(provider.Commands);
        Assert.Equal("DELETE FROM t", cmd.CommandText);
        Assert.Equal(1, cmd.ExecuteNonQueryCallCount);
    }

    [Fact]
    public void ExecuteProcedure_NullParameters_DoesNotAddParameters()
    {
        var ds = new DataSet();
        ds.Tables.Add(BuildTable(["id"], [42]));
        var provider = new FakeProvider { AdapterResult = ds };

        var result = provider.ExecuteProcedure("sp_test", null);

        var cmd = Assert.Single(provider.Commands);
        Assert.Equal(CommandType.StoredProcedure, cmd.CommandType);
        Assert.Empty(cmd.Parameters);
        Assert.Single(result);
    }

    [Fact]
    public void ExecuteProcedure_WithParameters_AddsThemToCommand()
    {
        var ds = new DataSet();
        ds.Tables.Add(BuildTable(["id"]));
        var provider = new FakeProvider { AdapterResult = ds };
        var json = JsonSerializer.Serialize(new Dictionary<string, object> { ["name"] = "bob", ["age"] = 30 });

        provider.ExecuteProcedure("sp_test", json);

        var cmd = Assert.Single(provider.Commands);
        Assert.Equal(2, cmd.Parameters.Count);
        Assert.Equal("name", cmd.Parameters[0].ParameterName);
        Assert.Equal("bob", cmd.Parameters[0].Value);
        Assert.Equal("age", cmd.Parameters[1].ParameterName);
        Assert.Equal("30", cmd.Parameters[1].Value);
    }

    [Fact]
    public void ExecuteProcedure_NoTables_ThrowsInvalidOperationException()
    {
        var provider = new FakeProvider { AdapterResult = new DataSet() };

        var ex = Assert.Throws<InvalidOperationException>(() => provider.ExecuteProcedure("sp_test", null));
        Assert.Contains("sp_test", ex.Message);
    }

    [Fact]
    public void ExecutePaginationProcedure_NullSerializedOptions_ThrowsWithCorrectParamName()
    {
        var provider = new FakeProvider();

        var ex = Assert.Throws<ArgumentNullException>(() => provider.ExecutePaginationProcedure("sp", null!, "{}"));
        Assert.Equal("serializedOptions", ex.ParamName);
    }

    [Fact]
    public void ExecutePaginationProcedure_NullSerializedParameters_ThrowsWithCorrectParamName()
    {
        var provider = new FakeProvider();
        var options = JsonSerializer.Serialize(new Dictionary<string, object> { ["page"] = 1, ["size"] = 10 });

        var ex = Assert.Throws<ArgumentNullException>(() => provider.ExecutePaginationProcedure("sp", options, null!));
        Assert.Equal("serializedParameters", ex.ParamName);
    }

    [Fact]
    public void ExecutePaginationProcedure_NoTables_ThrowsInvalidOperationException()
    {
        var provider = new FakeProvider { AdapterResult = new DataSet() };
        var options = JsonSerializer.Serialize(new Dictionary<string, object> { ["page"] = 1, ["size"] = 10 });

        var ex = Assert.Throws<InvalidOperationException>(() =>
            provider.ExecutePaginationProcedure("sp_pag", options, "{}"));
        Assert.Contains("sp_pag", ex.Message);
    }

    [Fact]
    public void ExecutePaginationProcedure_OnlyOneTable_ThrowsInvalidOperationException()
    {
        var ds = new DataSet();
        ds.Tables.Add(BuildTable(["id"], [1], [2]));
        var provider = new FakeProvider { AdapterResult = ds };
        var options = JsonSerializer.Serialize(new Dictionary<string, object> { ["page"] = 1, ["size"] = 10 });

        var ex = Assert.Throws<InvalidOperationException>(() =>
            provider.ExecutePaginationProcedure("sp_pag", options, "{}"));
        Assert.Contains("two result sets", ex.Message);
    }

    [Fact]
    public void ExecutePaginationProcedure_SecondTableWithoutTotal_ThrowsInvalidOperationException()
    {
        var ds = new DataSet();
        ds.Tables.Add(BuildTable(["id"], [1], [2]));
        ds.Tables.Add(BuildTable(["total"]));
        var provider = new FakeProvider { AdapterResult = ds };
        var options = JsonSerializer.Serialize(new Dictionary<string, object> { ["page"] = 1, ["size"] = 10 });

        var ex = Assert.Throws<InvalidOperationException>(() =>
            provider.ExecutePaginationProcedure("sp_pag", options, "{}"));
        Assert.Contains("total row count", ex.Message);
    }

    [Fact]
    public void ExecutePaginationProcedure_ReturnsTotalAndPagedRows()
    {
        var ds = new DataSet();
        ds.Tables.Add(BuildTable(["id", "name"], [1, "a"], [2, "b"], [3, "c"]));
        var totals = BuildTable(["total"], [42]);
        ds.Tables.Add(totals);
        var provider = new FakeProvider { AdapterResult = ds };
        var options = JsonSerializer.Serialize(new Dictionary<string, object> { ["page"] = 1, ["size"] = 10 });

        var result = provider.ExecutePaginationProcedure("sp", options, "{}");

        Assert.Equal(2, result.Length);
        Assert.Equal(42, result[0]);
        var rows = Assert.IsType<KeyValuePair<string, object?>[][]>(result[1]);
        Assert.Equal(3, rows.Length);
    }

    [Fact]
    public void ExecutePaginationProcedure_AppliesPagination()
    {
        var ds = new DataSet();
        ds.Tables.Add(BuildTable(["id"], [1], [2], [3], [4], [5]));
        ds.Tables.Add(BuildTable(["total"], [5]));
        var provider = new FakeProvider { AdapterResult = ds };
        var options = JsonSerializer.Serialize(new Dictionary<string, object> { ["page"] = 2, ["size"] = 2, ["orderby"] = "it[\"id\"]" });

        var result = provider.ExecutePaginationProcedure("sp", options, "{}");

        var rows = Assert.IsType<KeyValuePair<string, object?>[][]>(result[1]);
        Assert.Equal(2, rows.Length);
        Assert.Equal(3, rows[0][0].Value);
        Assert.Equal(4, rows[1][0].Value);
    }

    [Fact]
    public void ExecutePaginationProcedure_AppliesOrderbyAscending()
    {
        var ds = new DataSet();
        ds.Tables.Add(BuildTable(["id"], [3], [1], [2]));
        ds.Tables.Add(BuildTable(["total"], [3]));
        var provider = new FakeProvider { AdapterResult = ds };
        var options = JsonSerializer.Serialize(new Dictionary<string, object>
        {
            ["page"] = 1,
            ["size"] = 10,
            ["orderby"] = "it[\"id\"]"
        });

        var result = provider.ExecutePaginationProcedure("sp", options, "{}");

        var rows = Assert.IsType<KeyValuePair<string, object?>[][]>(result[1]);
        Assert.Equal(3, rows.Length);
        Assert.Equal(1, rows[0][0].Value);
        Assert.Equal(2, rows[1][0].Value);
        Assert.Equal(3, rows[2][0].Value);
    }

    [Fact]
    public void ExecutePaginationProcedure_AppliesOrderbyDescending()
    {
        var ds = new DataSet();
        ds.Tables.Add(BuildTable(["id"], [1], [3], [2]));
        ds.Tables.Add(BuildTable(["total"], [3]));
        var provider = new FakeProvider { AdapterResult = ds };
        var options = JsonSerializer.Serialize(new Dictionary<string, object>
        {
            ["page"] = 1,
            ["size"] = 10,
            ["orderby"] = "it[\"id\"] desc"
        });

        var result = provider.ExecutePaginationProcedure("sp", options, "{}");

        var rows = Assert.IsType<KeyValuePair<string, object?>[][]>(result[1]);
        Assert.Equal(3, rows[0][0].Value);
        Assert.Equal(2, rows[1][0].Value);
        Assert.Equal(1, rows[2][0].Value);
    }

    [Fact]
    public void ExecuteQuery_NullCommandText_ThrowsArgumentNullException()
    {
        var provider = new FakeProvider();
        var ex = Assert.Throws<ArgumentNullException>(() => provider.ExecuteQuery(null!));
        Assert.Equal("commandText", ex.ParamName);
    }

    [Fact]
    public void ExecuteNonQuery_NullCommandText_ThrowsArgumentNullException()
    {
        var provider = new FakeProvider();
        var ex = Assert.Throws<ArgumentNullException>(() => provider.ExecuteNonQuery(null!));
        Assert.Equal("commandText", ex.ParamName);
    }

    [Fact]
    public void ExecuteProcedure_NullProcedureName_ThrowsArgumentNullException()
    {
        var provider = new FakeProvider();
        var ex = Assert.Throws<ArgumentNullException>(() => provider.ExecuteProcedure(null!, null));
        Assert.Equal("procedureName", ex.ParamName);
    }

    [Fact]
    public void ExecutePaginationProcedure_NullProcedureName_ThrowsArgumentNullException()
    {
        var provider = new FakeProvider();
        var ex = Assert.Throws<ArgumentNullException>(() => provider.ExecutePaginationProcedure(null!, "{}", "{}"));
        Assert.Equal("procedureName", ex.ParamName);
    }

    [Fact]
    public void ExecuteProcedure_MalformedJson_DoesNotOpenConnection()
    {
        var provider = new FakeProvider();

        Assert.ThrowsAny<Exception>(() => provider.ExecuteProcedure("sp", "not-json"));

        Assert.Empty(FakeDbConnection.Instances);
    }

    [Fact]
    public void ExecutePaginationProcedure_AppliesSelectFilter()
    {
        var ds = new DataSet();
        ds.Tables.Add(BuildTable(["id", "name", "age"], [1, "a", 10]));
        ds.Tables.Add(BuildTable(["total"], [1]));
        var provider = new FakeProvider { AdapterResult = ds };
        var options = JsonSerializer.Serialize(new Dictionary<string, object>
        {
            ["page"] = 1,
            ["size"] = 10,
            ["select"] = "id,age"
        });

        var result = provider.ExecutePaginationProcedure("sp", options, "{}");

        var rows = Assert.IsType<KeyValuePair<string, object?>[][]>(result[1]);
        var row = Assert.Single(rows);
        Assert.Equal(2, row.Length);
        Assert.Equal("id", row[0].Key);
        Assert.Equal("age", row[1].Key);
    }

    [Fact]
    public void Connection_WhenOpenThrows_IsDisposed()
    {
        FakeDbConnection.OpenShouldThrow = true;
        var provider = new FakeProvider();

        Assert.Throws<InvalidOperationException>(() => provider.ExecuteNonQuery("noop"));

        var conn = Assert.Single(FakeDbConnection.Instances);
        Assert.True(conn.WasDisposed);
        Assert.False(conn.WasOpened);
    }

    [Fact]
    public void Connection_WhenOpenSucceeds_IsOpenedAndDisposed()
    {
        var provider = new FakeProvider();

        provider.ExecuteNonQuery("noop");

        var conn = Assert.Single(FakeDbConnection.Instances);
        Assert.True(conn.WasOpened);
        Assert.True(conn.WasDisposed);
    }
}
