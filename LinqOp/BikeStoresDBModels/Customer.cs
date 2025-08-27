using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace LinqOp.BikeStoresDBModels;

[Table("customers", Schema = "sales")]
public partial class Customer
{
    [Key]
    [Column("customer_id")]
    public int CustomerId { get; set; }

    [Column("first_name")]
    [StringLength(255)]
    [Unicode(false)]
    public string FirstName { get; set; } = null!;

    [Column("last_name")]
    [StringLength(255)]
    [Unicode(false)]
    public string LastName { get; set; } = null!;

    [Column("phone")]
    [StringLength(25)]
    [Unicode(false)]
    public string? Phone { get; set; }

    [Column("email")]
    [StringLength(255)]
    [Unicode(false)]
    public string Email { get; set; } = null!;

    [Column("street")]
    [StringLength(255)]
    [Unicode(false)]
    public string? Street { get; set; }

    [Column("city")]
    [StringLength(50)]
    [Unicode(false)]
    public string? City { get; set; }

    [Column("state")]
    [StringLength(25)]
    [Unicode(false)]
    public string? State { get; set; }

    [Column("zip_code")]
    [StringLength(5)]
    [Unicode(false)]
    public string? ZipCode { get; set; }

    [InverseProperty("Customer")]
    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();
}
