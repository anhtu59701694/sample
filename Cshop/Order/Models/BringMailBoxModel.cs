using System;
using System.Linq;
using System.Web;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Claw.Modules.Eshop.Models
{
    public class BringMailBoxModel
    {
        public int OrderId { get; set; }

        [Required(ErrorMessage = Core.StringTable.ErrMsgRequiredField)]
        [Range(1, int.MaxValue, ErrorMessage = Core.StringTable.ErrMsgMinLimit)]
        [Display(Name = "Weight in gram")]
        public int WeightInGram { get; set; }

        [Display(Name = "Enable rfid")]
        public bool Rfid { get; set; }

        public string RecipientMobile { get; set; }


        [Display(Name = "Mailbox id")]
        public string MailboxId { get; set; }

        [Display(Name = "Label uri")]
        public string LabelUri { get; set; }

        [Display(Name = "Rfid label uri")]
        public string RfidLabelUri { get; set; }

        [Display(Name = "Waybill uri")]
        public string WaybillUri { get; set; }
    }
}