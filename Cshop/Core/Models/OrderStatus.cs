using System.ComponentModel.DataAnnotations;

namespace Claw.Modules.Eshop.Models
{
    public enum OrderStatus : byte
    {
        [Display(Name = "Mottatt")]
        New = 1,
        [Display(Name = "Sendt")]
        Sent = 2,
        [Display(Name = "Ufullstendige")]
        PaymentWaiting = 3,
        [Display(Name = "Kansellert")]
        Cancelled = 4,
        [Display(Name = "Krediter")]
        Credit = 5, //returned order
        [Display(Name = "Restordre")]
        RestOrder = 6,
        [Display(Name = "Behandles")]
        Processing = 7,
        [Display(Name = "Venter på innbetaling")]
        PayBeforeDelivery = 8,
        //[Display(Name = "I bestilling")]InOrder = 9,
        //[Display(Name = "Hent")]Hent = 10
    }
}