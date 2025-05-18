using Datex.Global.refs.xhttp;

using SqlInternal = Internals.Sql;

namespace wshcmx;

public class Sql
{
    private SqlInternal? sqlInstance;

    public void Init(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new ArgumentException("Connection string cannot be null or empty.", nameof(connectionString));

        sqlInstance = new SqlInternal(connectionString);
    }

    public void ExecuteNonQuery(string procedureName)
    {
        ArgumentNullException.ThrowIfNull(sqlInstance, nameof(sqlInstance));
        sqlInstance.ExecuteNonQuery(procedureName);
    }

    public JsObject ExecutePaginationProcedure(string procedureName, int page, int size, string serializedParameters)
    {
        ArgumentNullException.ThrowIfNull(sqlInstance, nameof(sqlInstance));
        var (total, items) = sqlInstance.ExecutePaginationProcedure(procedureName, page, size, serializedParameters);
        JsObject result = new();
        result["total"] = total;
        result["page"] = page;
        result["size"] = size;
        List<JsObject> convertedItems = new();

        foreach (var item in items)
        {
            JsObject jsItem = new();
            foreach (var kvp in item)
                jsItem[kvp.Key] = kvp.Value?.ToString() ?? string.Empty;
            convertedItems.Add(jsItem);
        }

        result["items"] = convertedItems.ToArray();
        return result;
    }

    public IEnumerable<JsObject> ExecuteProcedure(string procedureName, string serializedParameters)
    {
        ArgumentNullException.ThrowIfNull(sqlInstance, nameof(sqlInstance));
        IEnumerable<Dictionary<string, object>> items = sqlInstance.ExecuteProcedure(procedureName, serializedParameters);
        List<JsObject> convertedItems = new();

        foreach (var item in items)
        {
            JsObject jsItem = new();

            foreach (var kvp in item)
                jsItem[kvp.Key] = kvp.Value?.ToString() ?? string.Empty;

            convertedItems.Add(jsItem);
        }

        return convertedItems.ToArray();
    }
}
