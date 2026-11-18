using System.ComponentModel.DataAnnotations;

namespace Claw.Modules.Eshop.Models
{
    public class BringBookingItemModel
    {
        public int OrderId { get; set; }

        [Required(ErrorMessage = Core.StringTable.ErrMsgRequiredField)]
        [Range(typeof(OMoney), "0", Core.StringTable.MaxOMoney, ErrorMessage = Core.StringTable.ErrMsgMinLimit)]
        public OMoney Amount { get; set; }

        public string Currency { get; set; }

        [Required(ErrorMessage = Core.StringTable.ErrMsgRequiredField)]
        [Range(typeof(OFloat), "0", Core.StringTable.MaxOFloat, ErrorMessage = Core.StringTable.ErrMsgMinLimit)]
        [Display(Name = "Weight in kg")]
        public OFloat WeightInKg { get; set; }

        public string GoodsDescription { get; set; }

        public int NumberOfItems { get; set; }

        //[Required(ErrorMessage = Core.StringTable.ErrMsgRequiredField)]
        [Display(Name = "Pickup point")]
        public string PickupPointId { get; set; }

        [Required(ErrorMessage = Core.StringTable.ErrMsgRequiredField)]
        [Display(Name = "Cod")]
        public bool ServiceCod { get; set; }

        public string RecipientNotiEmail { get; set; }

        [Required(ErrorMessage = Core.StringTable.ErrMsgRequiredField)]
        [Display(Name = "Recipient mobile")]
        public string RecipientNotiMobile { get; set; }

        public string ConsignmentNumber { get; set; }

        public string LabelUrl { get; set; }

        [Display(Name = "Message")]
        public string ErrMsg { get; set; }

    }
}