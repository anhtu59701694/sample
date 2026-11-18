using System.ComponentModel.DataAnnotations;

namespace Claw.Modules.Eshop.Models
{
    public class CreditOrderDetailModel : BaseExModel<Domain.OrderDetail>
    {
        public int? OrderId { get; set; }

        /// <summary>
        /// -1:paymentcost,-2:shippingcost
        /// </summary>
        [Required(ErrorMessage = StringTable.ErrMsgRequiredField)]
        public int? OrderDetailId { get; set; }

        [Display(Name = "SKU")]
        public string ProductSKU { get; set; }

        [Display(Name = "Name")]
        public string ProductName { get; set; }

        [Display(Name = "Quantity")]
        public int? OriginQty { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = StringTable.ErrMsgRangeLimit)]
        [Display(Name = "Return")]
        public int? Quantity { get; set; }

        public bool? Add2Store { get; set; }

        public OMoney Price { get; set; }

        [Display(Name = "Price (VAT)")]
        public OMoney PriceInc { get; set; }

        [Display(Name = "Total return (VAT)")]
        public OMoney TotalInc { get; set; }

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

        public string GetUniqueKey(bool useOriPrice = true, bool odDataJson = false)
        {
            return useOriPrice || _raw == null || _raw.Id == 0
                ? UniqueKey
                : OrderDetailModel.GetUniqueKey(_raw.Product.Id, _raw.Variant.GetId(), PriceInc, odDataJson ? _raw.Data : null);
        }
    }
}