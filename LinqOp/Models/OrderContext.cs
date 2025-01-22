using Microsoft.EntityFrameworkCore;

namespace LinqOp.Models
{
    // DbContext
    public class OrderContext : DbContext
    {
        public OrderContext(DbContextOptions<OrderContext> options) : base(options)
        {
        }

        required public DbSet<POSOrderMaster> tblPOSOrderMasters { get; set; }
        required public DbSet<POSOrderDetail> tblPOSOrderDetails { get; set; }
        required public DbSet<VendorItem> tblVendorsItems { get; set; }

    }
}
