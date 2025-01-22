using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LinqOp.Models
{
    // Order Detail Model
    public class POSOrderDetail
    {
        [Key]
        public int OrderDetailID { get; set; }
        public int OrderID { get; set; }
        public long Itemkey { get; set; }

        // Navigation property

        [InverseProperty(nameof(POSOrderMaster.tblPOSOrderDetails))]
        [ForeignKey(nameof(OrderID))]
        public virtual POSOrderMaster POSOrderMaster { get; set; } = new POSOrderMaster();
    }
}
