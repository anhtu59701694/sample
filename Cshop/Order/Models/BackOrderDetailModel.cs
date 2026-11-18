using System.ComponentModel.DataAnnotations;

namespace Claw.Modules.Eshop.Models
{
    public class BackOrderDetailModel : BaseExModel<Domain.OrderDetail>
    {
        public int? OrderId { get; set; }

        [Required(ErrorMessage = StringTable.ErrMsgRequiredField)]
        public int? OrderDetailId { get; set; }

        [Display(Name = "Product name")]
        public string ProductName { get; set; }

        public string ProductSKU { get; set; }

        [Display(Name = "Location")]
        public string ProductLocation { get; set; }
        
        public ImageModel ProductImage { get; set; }

        [Display(Name = "Quantity")]
        public int? OriginQty { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = StringTable.ErrMsgRangeLimit)]
        [Display(Name = "Rest quantity")]
        public int? Quantity { get; set; }

        public OMoney Price { get; set; }

        [Display(Name = "Price (VAT)")]
        public OMoney PriceInc { get; set; }

        public int? Iseq { get; set; }

        internal string UniqueKey { get; set; }

        public int? Key
        {
            get { return OrderDetailId; }
        }

        public bool IsDataModelEmpty
        {
            get { return (Quantity ?? 0) == 0; }
        }
    }
}