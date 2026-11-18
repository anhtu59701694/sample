using System.ComponentModel.DataAnnotations;

namespace Claw.Modules.Eshop.Models
{
    public class LogistraBookingItemModel
    {
        public int OrderId { get; set; }

        [Required(ErrorMessage = StringTable.ErrMsgRequiredField)]
        public OMoney Amount { get; set; }

        [Required(ErrorMessage = StringTable.ErrMsgRequiredField)]
        public string Currency { get; set; }

        [Display(Name = "Etterkrav")]
        public bool ServiceCod { get; set; }

        [Display(Name = "Weight in kg")]
        [Required(ErrorMessage = StringTable.ErrMsgRequiredField)]
        public OFloat WeightInKg { get; set; }

        public string Description { get; set; }

        public string PickupPointId { get; set; }


        public string BookingTrackingUrl { get; set; }

        public string BookingId { get; set; }

        [Display(Name = "Booking number")]
        public string BookingNumber { get; set; }

        [Display(Name = "Consignment pdf")]
        public string BookingConsignmentPdf { get; set; }

        [Display(Name = "Message")]
        public string ErrMsg { get; set; }
    }
}