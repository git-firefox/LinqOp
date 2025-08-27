using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace LinqOp.BikeStoresDBModels;

[Table("stores", Schema = "sales")]
public partial class Store
{
    [Key]
    [Column("store_id")]
    public int StoreId { get; set; }

    [Column("store_name")]
    [StringLength(255)]
    [Unicode(false)]
    public string StoreName { get; set; } = null!;

    [Column("phone")]
    [StringLength(25)]
    [Unicode(false)]
    public string? Phone { get; set; }

    [Column("email")]
    [StringLength(255)]
    [Unicode(false)]
    public string? Email { get; set; }

    [Column("street")]
    [StringLength(255)]
    [Unicode(false)]
    public string? Street { get; set; }

    [Column("city")]
    [StringLength(255)]
    [Unicode(false)]
    public string? City { get; set; }

    [Column("state")]
    [StringLength(10)]
    [Unicode(false)]
    public string? State { get; set; }

    [Column("zip_code")]
    [StringLength(5)]
    [Unicode(false)]
    public string? ZipCode { get; set; }

    [InverseProperty("Store")]
    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();

    [InverseProperty("Store")]
    public virtual ICollection<Staff> Staff { get; set; } = new List<Staff>();

    [InverseProperty("Store")]
    public virtual ICollection<Stock> Stocks { get; set; } = new List<Stock>();
}
