using System;
using System.ComponentModel.DataAnnotations;

namespace Claw.Modules.Eshop.Models
{
    public class ShippingBookingModel
    {
        public int Id { get; set; }
        
        public int Service { get; set; }

        [Display(Name = "Order id")]
        public int? OrderId { get; set; }

        [Required(ErrorMessage = Core.StringTable.ErrMsgRequiredField)]
        [Range(typeof(OMoney), "0", Core.StringTable.MaxOMoney, ErrorMessage = Core.StringTable.ErrMsgMinLimit)]
        public OMoney Amount { get; set; }
        
        public string Currency { get; set; }

        [Required(ErrorMessage = Core.StringTable.ErrMsgRequiredField)]
        [Range(1, int.MaxValue, ErrorMessage = Core.StringTable.ErrMsgMinLimit)]
        [Display(Name = "Width in cm")]
        public int? WidthInCm { get; set; }

        [Required(ErrorMessage = Core.StringTable.ErrMsgRequiredField)]
        [Range(1, int.MaxValue, ErrorMessage = Core.StringTable.ErrMsgMinLimit)]
        [Display(Name = "Height in cm")]
        public int? HeightInCm { get; set; }

        [Required(ErrorMessage = Core.StringTable.ErrMsgRequiredField)]
        [Range(1, int.MaxValue, ErrorMessage = Core.StringTable.ErrMsgMinLimit)]
        [Display(Name = "Length in cm")]
        public int? LengthInCm { get; set; }

        [Required(ErrorMessage = Core.StringTable.ErrMsgRequiredField)]
        [Range(typeof(OFloat), "0", Core.StringTable.MaxOFloat, ErrorMessage = Core.StringTable.ErrMsgMinLimit)]
        [Display(Name = "Weight in kg")]
        public OFloat WeightInKg { get; set; }

        [Display(Name = "Goods description")]
        [StringLength(35,ErrorMessage = StringTable.ErrMsgLengthLimit)]
        public string GoodsDescription { get; set; }

        [Display(Name = "Number of items")]
        public int? NumberOfItems { get; set; }

        [Required(ErrorMessage = Core.StringTable.ErrMsgRequiredField)]
        [Display(Name = "Customer number")]
        [StringLength(50, ErrorMessage = StringTable.ErrMsgLengthLimit)]
        public string CustomerNum { get; set; }

        [Display(Name = "Customer id")]
        public int? CustomerId { get; set; }

        [Required(ErrorMessage = Core.StringTable.ErrMsgRequiredField)]
        [Display(Name = "Product")]
        public string ProductId { get; set; }

        [Display(Name = "Cod")]
        public bool? ServiceCod { get; set; }

        [Display(Name = "Enable rfid")]
        public bool? Rfid { get; set; }

        [Required(ErrorMessage = Core.StringTable.ErrMsgRequiredField)]
        [Display(Name = "Postnr")]
        [StringLength(10, ErrorMessage = StringTable.ErrMsgLengthLimit)]
        public string RecipientPostalCode { get; set; }

        [Required(ErrorMessage = Core.StringTable.ErrMsgRequiredField)]
        [Display(Name = "Posted")]
        [StringLength(50, ErrorMessage = StringTable.ErrMsgLengthLimit)]
        public string RecipientCity { get; set; }

        [Required(ErrorMessage = Core.StringTable.ErrMsgRequiredField)]
        [Display(Name = "Name")]
        [StringLength(100, ErrorMessage = StringTable.ErrMsgLengthLimit)]
        public string RecipientName { get; set; }

        [Required(ErrorMessage = Core.StringTable.ErrMsgRequiredField)]
        [Display(Name = "Address")]
        [StringLength(100, ErrorMessage = StringTable.ErrMsgLengthLimit)]
        public string RecipientAddress { get; set; }

        [Required(ErrorMessage = Core.StringTable.ErrMsgRequiredField)]
        [Display(Name = "Country")]
        public string RecipientCountryCode { get; set; }

        #region AddressInfo

        [Display(Name = "Address")]
        public string AddressInfo
        {
            get
            {
                return string.Format("{0} {1} {2}, {3}",
                    string.IsNullOrEmpty(RecipientAddress) ? "" : RecipientAddress + ",", RecipientPostalCode, RecipientCity, RecipientCountryCode);
            }
        }

        #endregion

        [Display(Name = "Notification")]
        public bool? RecipientNoti { get; set; }

        [Display(Name = "Email")]
        [StringLength(150, ErrorMessage = StringTable.ErrMsgLengthLimit)]
        [EmailAddress(ErrorMessage = StringTable.ErrMsgInvalidEmail)]
        public string RecipientNotiEmail { get; set; }

        [Required(ErrorMessage = Core.StringTable.ErrMsgRequiredField)]
        [Display(Name = "Mobile")]
        [StringLength(20, ErrorMessage = StringTable.ErrMsgLengthLimit)]
        public string RecipientNotiMobile { get; set; }

        [Required(ErrorMessage = Core.StringTable.ErrMsgRequiredField)]
        [Display(Name = "Cod account number")]
        [StringLength(50, ErrorMessage = StringTable.ErrMsgLengthLimit)]
        public string CodAccountNumber { get; set; }
        
        public string Data { get; set; }
        
        public string Result { get; set; }

        [Display(Name = "Booking id")]
        public string BookingId { get; set; }

        [Display(Name = "Label url")]
        public string LabelUrl { get; set; }

        [Display(Name = "Tracking url")]
        public string TrackingUrl { get; set; }

        [Display(Name = "Waybill url")]
        public string WaybillUrl { get; set; }

        public ODateTime CreatedDate { get; set; }
    }
}