using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace Claw.Modules.Eshop.Models
{
    public class BringBookingModel
    {
        [Display(Name = "Order id")]
        public int OrderId { get; set; }

        [Required(ErrorMessage = Core.StringTable.ErrMsgRequiredField)]
        [Range(typeof(OMoney), "0", Core.StringTable.MaxOMoney, ErrorMessage = Core.StringTable.ErrMsgMinLimit)]
        public OMoney Amount { get; set; }

        public string Currency { get; set; }

        [Required(ErrorMessage = Core.StringTable.ErrMsgRequiredField)]
        [Range(1, int.MaxValue, ErrorMessage = Core.StringTable.ErrMsgMinLimit)]
        [Display(Name = "Height in cm")]
        public int HeightInCm { get; set; }

        [Required(ErrorMessage = Core.StringTable.ErrMsgRequiredField)]
        [Range(1, int.MaxValue, ErrorMessage = Core.StringTable.ErrMsgMinLimit)]
        [Display(Name = "Length in cm")]
        public int LengthInCm { get; set; }

        [Required(ErrorMessage = Core.StringTable.ErrMsgRequiredField)]
        [Range(1, int.MaxValue, ErrorMessage = Core.StringTable.ErrMsgMinLimit)]
        [Display(Name = "Width in cm")]
        public int WidthInCm { get; set; }

        [Required(ErrorMessage = Core.StringTable.ErrMsgRequiredField)]
        [Range(typeof(OFloat),"0", Core.StringTable.MaxOFloat, ErrorMessage = Core.StringTable.ErrMsgMinLimit)]
        [Display(Name = "Weight in kg")]
        public OFloat WeightInKg { get; set; }

        public string GoodsDescription { get; set; }

        public int NumberOfItems { get; set; }

        public string CustomerId { get; set; }

        [Required(ErrorMessage = Core.StringTable.ErrMsgRequiredField)]
        [Display(Name = "Product")]
        public string ProductId { get; set; }

        //[Required(ErrorMessage = Core.StringTable.ErrMsgRequiredField)]
        [Display(Name = "Pickup point")]
        public string PickupPointId { get; set; }

        [Required(ErrorMessage = Core.StringTable.ErrMsgRequiredField)]
        [Display(Name = "Cod")]
        public bool ServiceCod { get; set; }

        [Display(Name = "Bag on door")]
        public bool Service1081 { get; set; }

        [Display(Name = "Return Parcel")]
        public bool Service9650 { get; set; }

        [Display(Name = "VAS")]
        public bool Service9650A1073 { get; set; }

        [Display(Name = "Notification")]
        public bool RecipientNoti { get; set; }

        public string RecipientNotiEmail { get; set; }

        [Required(ErrorMessage = Core.StringTable.ErrMsgRequiredField)]
        [Display(Name = "Recipient mobile")]
        public string RecipientNotiMobile { get; set; }

        [Required(ErrorMessage = Core.StringTable.ErrMsgRequiredField)]
        [Display(Name = "Cod account number")]
        public string CodAccountNumber { get; set; }

        public List<BringBookingItemModel> Items { get; set; }



        [Display(Name = "Consignment#")]
        public string ConsignmentNumber { get; set; }

        [Display(Name = "Label url")]
        public string LabelUrl { get; set; }

        [Display(Name = "Tracking url")]
        public string TrackingUrl { get; set; }

        [Display(Name = "Waybill url")]
        public string WaybillUrl { get; set; }

        [Display(Name = "Package#")]
        public string PackageNumber { get; set; }

        [Display(Name = "Earliest pickup")]
        public ODateTime EarliestPickup { get; set; }

        [Display(Name = "Expected delivery")]
        public ODateTime ExpectedDelivery { get; set; }
    }
}