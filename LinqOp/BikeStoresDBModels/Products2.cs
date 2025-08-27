using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace LinqOp.BikeStoresDBModels;

[Keyless]
[Table("products2", Schema = "production")]
public partial class Products2
{
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
}
