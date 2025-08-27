using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace LinqOp.BikeStoresDBModels;

[Table("products", Schema = "production")]
public partial class Product
{
    [Key]
    [Column("product_id")]
    public int ProductId { get; set; }

    [Column("product_name")]
    [StringLength(255)]
    [Unicode(false)]
    public string ProductName { get; set; } = null!;

    [Column("brand_id")]
    public int BrandId { get; set; }

    [Column("category_id")]
    public int CategoryId { get; set; }

    [Column("model_year")]
    public short ModelYear { get; set; }

    [Column("list_price", TypeName = "decimal(10, 2)")]
    public decimal ListPrice { get; set; }

    [ForeignKey("BrandId")]
    [InverseProperty("Products")]
    public virtual Brand Brand { get; set; } = null!;

    [ForeignKey("CategoryId")]
    [InverseProperty("Products")]
    public virtual Category Category { get; set; } = null!;

    [InverseProperty("Product")]
    public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();

    [InverseProperty("Product")]
    public virtual ICollection<Stock> Stocks { get; set; } = new List<Stock>();
}
