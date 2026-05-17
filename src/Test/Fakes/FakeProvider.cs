using System.Data;
using System.Data.Common;
using wshcmx.Net.Providers;

namespace Test.Fakes;

internal sealed class FakeProvider(string connectionString = "fake") : DatabaseProviderBase<FakeDbConnection>(connectionString)
{
    public DataTable? ReaderResult { get; set; }
    public DataSet? AdapterResult { get; set; }
    public List<FakeDbCommand> Commands { get; } = [];

    protected override DbCommand CreateTypedCommand(string commandText, FakeDbConnection connection)
    {
        var cmd = new FakeDbCommand { CommandText = commandText, ReaderResult = ReaderResult };
        Commands.Add(cmd);
        return cmd;
    }

    protected override DbParameter CreateParameter(string name, object? value)
        => new FakeDbParameter { ParameterName = name, Value = value ?? DBNull.Value };

    protected override DbDataAdapter CreateDataAdapter(DbCommand command)
        => new FakeDbDataAdapter { FillResult = AdapterResult };
}
