using System.ComponentModel.DataAnnotations;
namespace Claw.Modules.Eshop.Models
{
    public class OrderDetailBundleModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = StringTable.ErrMsgRequiredField)]
        public int? ProductId { get; set; }

        public int? VariantId { get; set; }

        [Required(ErrorMessage = StringTable.ErrMsgRequiredField)]
        public int? OrderDetailId { get; set; }

        [Required(ErrorMessage = StringTable.ErrMsgRequiredField)]
        public int? Quantity { get; set; }

        public int? StockQty { get; set; }

        public string ProductName { get; set; }

        public string ProductSKU { get; set; }

        public string SerialNumber { get; set; }
    }
}