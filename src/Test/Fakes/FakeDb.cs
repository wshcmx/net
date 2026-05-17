using System.Collections;
using System.Data;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;

namespace Test.Fakes;

internal sealed class FakeDbConnection : DbConnection
{
    private static readonly AsyncLocal<List<FakeDbConnection>?> _instances = new();
    private static readonly AsyncLocal<bool> _openShouldThrow = new();

    public static List<FakeDbConnection> Instances
    {
        get
        {
            _instances.Value ??= [];
            return _instances.Value;
        }
    }

    public static bool OpenShouldThrow
    {
        get => _openShouldThrow.Value;
        set => _openShouldThrow.Value = value;
    }

    public static void Reset()
    {
        _instances.Value = null;
        _openShouldThrow.Value = false;
    }

    public FakeDbConnection()
    {
        Instances.Add(this);
    }

    public bool WasDisposed { get; private set; }
    public bool WasOpened { get; private set; }

    private ConnectionState _state = ConnectionState.Closed;
    public override ConnectionState State => _state;

    [AllowNull]
    public override string ConnectionString { get; set; } = string.Empty;
    public override string Database => string.Empty;
    public override string DataSource => string.Empty;
    public override string ServerVersion => string.Empty;

    public override void Open()
    {
        if (OpenShouldThrow)
            throw new InvalidOperationException("Simulated Open failure");
        WasOpened = true;
        _state = ConnectionState.Open;
    }

    public override void Close() => _state = ConnectionState.Closed;

    public override void ChangeDatabase(string databaseName) { }

    protected override DbCommand CreateDbCommand() => new FakeDbCommand();

    protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel)
        => throw new NotSupportedException();

    protected override void Dispose(bool disposing)
    {
        WasDisposed = true;
        base.Dispose(disposing);
    }
}

internal sealed class FakeDbCommand : DbCommand
{
    private readonly FakeDbParameterCollection _parameters = new();

    public DataTable? ReaderResult { get; set; }
    public int NonQueryResult { get; set; }
    public int ExecuteNonQueryCallCount { get; private set; }

    [AllowNull]
    public override string CommandText { get; set; } = string.Empty;
    public override int CommandTimeout { get; set; }
    public override CommandType CommandType { get; set; }
    public override bool DesignTimeVisible { get; set; }
    public override UpdateRowSource UpdatedRowSource { get; set; }

    protected override DbConnection? DbConnection { get; set; }
    protected override DbParameterCollection DbParameterCollection => _parameters;
    protected override DbTransaction? DbTransaction { get; set; }

    public override void Cancel() { }

    public override int ExecuteNonQuery()
    {
        ExecuteNonQueryCallCount++;
        return NonQueryResult;
    }

    public override object? ExecuteScalar() => null;
    public override void Prepare() { }

    protected override DbParameter CreateDbParameter() => new FakeDbParameter();

    protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior)
        => (ReaderResult ?? new DataTable()).CreateDataReader();
}

internal sealed class FakeDbParameter : DbParameter
{
    public override DbType DbType { get; set; }
    public override ParameterDirection Direction { get; set; }
    public override bool IsNullable { get; set; }
    [AllowNull]
    public override string ParameterName { get; set; } = string.Empty;
    public override int Size { get; set; }
    [AllowNull]
    public override string SourceColumn { get; set; } = string.Empty;
    public override bool SourceColumnNullMapping { get; set; }
    public override object? Value { get; set; }

    public override void ResetDbType() { }
}

internal sealed class FakeDbParameterCollection : DbParameterCollection
{
    private readonly List<DbParameter> _items = [];

    public override int Count => _items.Count;
    public override object SyncRoot => ((ICollection)_items).SyncRoot;

    public override int Add(object? value) { _items.Add((DbParameter)value!); return _items.Count - 1; }
    public override void AddRange(Array values) { foreach (var v in values) _items.Add((DbParameter)v); }
    public override void Clear() => _items.Clear();
    public override bool Contains(object? value) => value is DbParameter p && _items.Contains(p);
    public override bool Contains(string value) => _items.Exists(p => p.ParameterName == value);
    public override void CopyTo(Array array, int index) => ((ICollection)_items).CopyTo(array, index);
    public override IEnumerator GetEnumerator() => _items.GetEnumerator();
    public override int IndexOf(object? value) => value is DbParameter p ? _items.IndexOf(p) : -1;
    public override int IndexOf(string parameterName) => _items.FindIndex(p => p.ParameterName == parameterName);
    public override void Insert(int index, object? value) => _items.Insert(index, (DbParameter)value!);
    public override void Remove(object? value) { if (value is DbParameter p) _items.Remove(p); }
    public override void RemoveAt(int index) => _items.RemoveAt(index);
    public override void RemoveAt(string parameterName) { var i = IndexOf(parameterName); if (i >= 0) _items.RemoveAt(i); }

    protected override DbParameter GetParameter(int index) => _items[index];
    protected override DbParameter GetParameter(string parameterName) => _items[IndexOf(parameterName)];
    protected override void SetParameter(int index, DbParameter value) => _items[index] = value;
    protected override void SetParameter(string parameterName, DbParameter value) => _items[IndexOf(parameterName)] = value;
}

internal sealed class FakeDbDataAdapter : DbDataAdapter
{
    public DataSet? FillResult { get; set; }

    public override int Fill(DataSet dataSet)
    {
        if (FillResult is null) return 0;
        foreach (DataTable t in FillResult.Tables) dataSet.Tables.Add(t.Copy());
        return FillResult.Tables.Count > 0 ? FillResult.Tables[0].Rows.Count : 0;
    }
}
