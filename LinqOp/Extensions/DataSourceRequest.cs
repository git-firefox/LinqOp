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

public enum AggregateFunction
{
    Sum,
    Min,
    Max,
    Average,
    Count
}

public class AggregateDescriptor
{
    public string Member { get; set; } = string.Empty;
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public AggregateFunction Aggregate { get; set; }
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
    public IList<AggregateDescriptor> Aggregates { get; set; } = new List<AggregateDescriptor>();

}

public class DataSourceResult<TResult> where TResult : class
{
    public DataSourceResult(IList<TResult> data, int total, IDictionary<string, IDictionary<string, object>>? aggregates = null)
    {
        Data = data;
        Total = total;
        Aggregates = aggregates;
    }

    public IEnumerable<TResult> Data { get; private set; }
    public int Total { get; private set; }
    public IDictionary<string, IDictionary<string, object>>? Aggregates { get; }

}
