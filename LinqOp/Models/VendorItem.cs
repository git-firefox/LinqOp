using System.ComponentModel.DataAnnotations;

namespace LinqOp.Models;

// Vendor Items Model
public class VendorItem
{
    [Key]
    public long Itemkey { get; set; }
    public int VendorItemID { get; set; }
    public long StoreId { get; set; }
    public decimal UnitCost { get; set; }
    public decimal UnitRetail { get; set; }
}

public class OrderSummary
{
    public int OrderID { get; set; }
    public string? OrderNumber { get; set; }
    public DateTime Date { get; set; }
    public int Vendor { get; set; }
    public int StoreID { get; set; }
    public bool IsFinished { get; set; }
    public string? VendorName { get; set; }
    public int ProductGroup { get; set; }
    public int Category { get; set; }
    public int Manufacturer { get; set; }
    public string? ProductGroupName { get; set; }
    public string? CategoryName { get; set; }
    public string? ManufacturerName { get; set; }
    public float TotalCost { get; set; }
    public float TotalRetail { get; set; }
    public float Margin { get; set; }
    public int? OrderType { get; set; }
}

public class OrderSummaryResult
{
    public List<OrderSummary> Data { get; set; } = new List<OrderSummary>();
    public int Total { get; set; }
    public object? AggregateResults { get; set; }
    public object? Errors { get; set; }
}