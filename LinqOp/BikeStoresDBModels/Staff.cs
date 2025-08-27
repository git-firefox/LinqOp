using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace LinqOp.BikeStoresDBModels;

[Table("staffs", Schema = "sales")]
[Index("Email", Name = "UQ__staffs__AB6E6164AB06B234", IsUnique = true)]
public partial class Staff
{
    [Key]
    [Column("staff_id")]
    public int StaffId { get; set; }

    [Column("first_name")]
    [StringLength(50)]
    [Unicode(false)]
    public string FirstName { get; set; } = null!;

    [Column("last_name")]
    [StringLength(50)]
    [Unicode(false)]
    public string LastName { get; set; } = null!;

    [Column("email")]
    [StringLength(255)]
    [Unicode(false)]
    public string Email { get; set; } = null!;

    [Column("phone")]
    [StringLength(25)]
    [Unicode(false)]
    public string? Phone { get; set; }

    [Column("active")]
    public byte Active { get; set; }

    [Column("store_id")]
    public int StoreId { get; set; }

    [Column("manager_id")]
    public int? ManagerId { get; set; }

    [InverseProperty("Manager")]
    public virtual ICollection<Staff> InverseManager { get; set; } = new List<Staff>();

    [ForeignKey("ManagerId")]
    [InverseProperty("InverseManager")]
    public virtual Staff? Manager { get; set; }

    [InverseProperty("Staff")]
    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();

    [ForeignKey("StoreId")]
    [InverseProperty("Staff")]
    public virtual Store Store { get; set; } = null!;
}
