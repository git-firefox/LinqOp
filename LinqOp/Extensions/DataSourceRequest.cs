using System.Text.Json.Serialization;

namespace LinqOp.Models;

public enum FilterOperator
{
    Eq,             // "eq"
    Neq,            // "neq"
    Gte,            // "gte"
    Gt,             // "gt"
    Lte,            // "lte"
    Lt,             // "lt"
    StartsWith,     // "startswith"
    EndsWith,       // "endswith"
    Contains,       // "contains"
    DoesNotContain, // "doesnotcontain"
    IsNull,         // "isnull"
    IsNotNull,      // "isnotnull"
    IsEmpty,        // "isempty"
    IsNotEmpty      // "isnotempty"
}

public enum SortDirection
{
    Asc,  // "asc"
    Desc  // "desc"
}

public class SortDescriptor
{
    public string Member { get; set; } = string.Empty;  // e.g. "Name"
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public SortDirection Dir { get; set; }
}

public class FilterDescriptor
{
    public string Member { get; set; } = string.Empty;  // e.g. "Category"
    public string Value { get; set; } = string.Empty;   // e.g. "Beverages"
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public FilterOperator Operator { get; set; } = FilterOperator.Eq;        // eq, contains, startswith, etc.
}


public class DataSourceRequest
{
    public int Skip { get; set; }
    public int Take { get; set; }
    public IList<SortDescriptor> Sorts { get; set; } = new List<SortDescriptor>();
    public IList<FilterDescriptor> Filters { get; set; } = new List<FilterDescriptor>();
}

public class DataSourceResult<TResult> where TResult : class
{
    public DataSourceResult(IEnumerable<TResult> data, int total)
    {
        Data = data;
        Total = total;
    }

    public IEnumerable<TResult> Data { get; private set; }
    public int Total { get; private set; }
}
