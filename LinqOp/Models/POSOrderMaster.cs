using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LinqOp.Models
{
    public class POSOrderMaster
    {
        [Key]
        public int OrderID { get; set; }
        public string? OrderNumber { get; set; }
        public DateTime Date { get; set; }
        public int Vendor { get; set; }
        public long StoreID { get; set; }
        public bool IsFinished { get; set; }

        // Navigation property

        [InverseProperty(nameof(POSOrderDetail.POSOrderMaster))]
        public virtual ICollection<POSOrderDetail> tblPOSOrderDetails { get; set; } = [];
    }
}
