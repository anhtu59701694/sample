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
    /// <summary>
    /// https://web.proteria.com [otto@cateno.com/JaFi2019?]
    /// </summary>
    public class ProteriaBookingModel
    {
        public int OrderId { get; set; }

        [Required(ErrorMessage = Core.StringTable.ErrMsgRequiredField)]
        [Display(Name = "Cod account number")]
        public string CodAccountNumber { get; set; }

        [Required(ErrorMessage = StringTable.ErrMsgRequiredField)]
        public OMoney Amount { get; set; }

        [Display(Name = "Notification sms")]
        public bool NotiSms { get; set; }

        [Display(Name = "Notification email")]
        public bool NotiEmail { get; set; }

        [Display(Name = "Etterkrav")]
        public bool ServiceCod { get; set; }

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

        [Display(Name = "Package type")]
        [Required(ErrorMessage = StringTable.ErrMsgRequiredField)]
        public string ProductId { get; set; }

        public string ProductName { get; set; }

        [Display(Name = "Carrier")]
        public string CarrierName { get; set; }

        [Display(Name = "Shipment id")]
        public string BookingShipmentId { get; set; }

        [Display(Name = "Tracking number")]
        public string BookingTrackingNumber { get; set; }

        [Display(Name = "Created at")]
        public ODateTime BookingCreatedAt { get; set; }

        [Display(Name = "Label pdf")]
        public string BookingLabelPdf { get; set; }

        [Display(Name = "Tracking url")]
        public string BookingTrackingUrl { get; set; }

        [Required(ErrorMessage = Core.StringTable.ErrMsgRequiredField)]
        [Display(Name = "Printer")]
        public string BookingPrinter { get; set; }
    }
}