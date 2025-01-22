using System.ComponentModel.DataAnnotations;

namespace LinqOp.Models
{
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
}
