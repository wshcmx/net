namespace wshcmx.Net.Providers;

internal interface IDatabaseProvider
{
    KeyValuePair<string, object?>[][] ExecuteQuery(string commandText);
    void ExecuteNonQuery(string commandText);
    object[] ExecuteProcedure(string procedureName, string? serializedParameters);
    object[] ExecutePaginationProcedure(string procedureName, string serializedOptions, string serializedParameters);
}
