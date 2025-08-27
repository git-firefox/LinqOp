using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace LinqOp.BikeStoresDBModels;

[Table("orders", Schema = "sales")]
public partial class Order
{
    [Key]
    [Column("order_id")]
    public int OrderId { get; set; }

    [Column("customer_id")]
    public int? CustomerId { get; set; }

    [Column("order_status")]
    public byte OrderStatus { get; set; }

    [Column("order_date")]
    public DateOnly OrderDate { get; set; }

    [Column("required_date")]
    public DateOnly RequiredDate { get; set; }

    [Column("shipped_date")]
    public DateOnly? ShippedDate { get; set; }

    [Column("store_id")]
    public int StoreId { get; set; }

    [Column("staff_id")]
    public int StaffId { get; set; }

    [ForeignKey("CustomerId")]
    [InverseProperty("Orders")]
    public virtual Customer? Customer { get; set; }

    [InverseProperty("Order")]
    public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();

    [ForeignKey("StaffId")]
    [InverseProperty("Orders")]
    public virtual Staff Staff { get; set; } = null!;

    [ForeignKey("StoreId")]
    [InverseProperty("Orders")]
    public virtual Store Store { get; set; } = null!;
}
