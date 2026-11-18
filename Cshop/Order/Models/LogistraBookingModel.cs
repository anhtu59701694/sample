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
    /// https://sandbox.cargonizer.no [quoc@cateno.no/1234]
    /// </summary>
    public class LogistraBookingModel
    {
        public LogistraBookingModel() { PackageCount = 1; }
        public int OrderId { get; set; }

        public int TransportAgreementId { get; set; }

        [Required(ErrorMessage = StringTable.ErrMsgRequiredField)]
        public OMoney Amount { get; set; }

        [Required(ErrorMessage = StringTable.ErrMsgRequiredField)]
        public string Currency { get; set; }

        [Display(Name = "Notification sms")]
        public bool NotiSms { get; set; }

        [Display(Name = "Notification email")]
        public bool NotiEmail { get; set; }

        [Display(Name = "Etterkrav")]
        public bool ServiceCod { get; set; }

        [Display(Name = "Tollverdi")]
        public bool ServiceCustomsValue { get; set; }

        [Display(Name = "Insurance")]
        public bool ServiceInsurance { get; set; }

        [Display(Name = "Weight in kg")]
        [Required(ErrorMessage = StringTable.ErrMsgRequiredField)]
        public OFloat WeightInKg { get; set; }

        [Display(Name = "Packages")]
        public uint PackageCount { get; set; }

        public string Description { get; set; }

        [Display(Name = "Pickup point")]
        [Required(ErrorMessage = StringTable.ErrMsgRequiredField)]
        public string PickupPointId { get; set; }

        [Display(Name = "Product")]
        [Required(ErrorMessage = StringTable.ErrMsgRequiredField)]
        public string ProductId { get; set; }

        public string ProductName { get; set; }

        [Display(Name = "Shop's freight payer customer id")]
        [Required(ErrorMessage = StringTable.ErrMsgRequiredField)]
        public string FreightPayerCustomerNumber { get; set; }

        [Display(Name = "Terms of delivery")]
        public string TermsOfDelivery { get; set; }

        [Display(Name = "Consignee customer id")]
        [Required(ErrorMessage = StringTable.ErrMsgRequiredField)]
        public string ConsigneeCustomerNumber { get; set; }

        public string CarrierId { get; set; }

        [Display(Name = "Carrier")]
        public string CarrierName { get; set; }

        public List<LogistraBookingItemModel> Items { get; set; }

        [Display(Name = "Booking id")]
        public string BookingId { get; set; }

        [Display(Name = "Booking number")]
        public string BookingNumber { get; set; }

        [Display(Name = "Created at")]
        public ODateTime BookingCreatedAt { get; set; }

        [Display(Name = "Consignment pdf")]
        public string BookingConsignmentPdf { get; set; }

        [Display(Name = "Waybill pdf")]
        public string BookingWaybillPdf { get; set; }

        [Display(Name = "Tracking url")]
        public string BookingTrackingUrl { get; set; }

        [Display(Name = "Printer")]
        public string BookingPrinter { get; set; }

        public string[] GetServices(Logistra.Agreement agreement)
        {
            var isDHL = CarrierName.ToLower().IndexOf("dhl") >= 0 ? true : false;

            return new[]
            {
                NotiSms ? "postnord_notification_sms" : null, 
                NotiEmail ? "postnord_notification_email" : null, 
                ServiceCod ? "tg_etterkrav" : null, 
                ServiceCustomsValue ? "dhl_express_customs_value" : null, 
                ServiceInsurance ? (isDHL ? "dhl_express_insurance": "tg_insurance") : null
            }.Where(x => x != null).ToArray();
        }
    }
}