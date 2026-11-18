using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;
using Claw.Core.Extensions;
using Claw.Modules.Eshop.DAL;
using Claw.Modules.Eshop.Domain;
using Logistra = Eshop.Shipping.Logistra;

namespace Claw.Modules.Eshop.Models
{
    public class PostnordBookingModel
    {
        public int OrderId { get; set; }

        [Required(ErrorMessage = StringTable.ErrMsgRequiredField)]
        public OMoney Amount { get; set; }

        [Required(ErrorMessage = StringTable.ErrMsgRequiredField)]
        public string Currency { get; set; }

        [Required(ErrorMessage = StringTable.ErrMsgRequiredField)]
        public string CodAccountNumber { get; set; }

        public string CountryCode { get; set; }

        [Display(Name = "Notification sms")]
        public bool NotiSms { get; set; }

        [Display(Name = "Notification email")]
        public bool NotiEmail { get; set; }

        [Display(Name = "Etterkrav")]
        public bool ServiceCod { get; set; }

        [Display(Name = "Insurance")]
        public bool ServiceInsurance { get; set; }

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

        [Display(Name = "Weight in kg")]
        [Required(ErrorMessage = StringTable.ErrMsgRequiredField)]
        public OFloat WeightInKg { get; set; }

        public string Description { get; set; }

        [Display(Name = "Pickup point")]
        //[Required(ErrorMessage = StringTable.ErrMsgRequiredField)]
        public string PickupPointId { get; set; }

        [Display(Name = "Product")]
        [Required(ErrorMessage = StringTable.ErrMsgRequiredField)]
        public string ProductId { get; set; }

        public string ProductName { get; set; }

        public List<PostnordBookingItemModel> Items { get; set; }

        [Display(Name = "Booking id")]
        public string BookingId { get; set; }

        [Display(Name = "Tracking number")]
        public string BookingNumber { get; set; }

        public string PrintId { get; set; }

        [Display(Name = "Label pdf")]
        public string BookingLabelPdf { get; set; }

        [Display(Name = "Tracking url")]
        public string BookingTrackingUrl { get; set; }
    }
}