using System.ComponentModel.DataAnnotations;

namespace Claw.Modules.Eshop.Models
{
    public class OrderNoteModel
    {
        public int Id { get; set; }

        public bool InlineEdit { get; set; }

        [Required(ErrorMessage = StringTable.ErrMsgRequiredField)]
        public int? OrderId { get; set; }

        [Required(ErrorMessage = StringTable.ErrMsgRequiredField)]
        [StringLength(200, ErrorMessage = StringTable.ErrMsgLengthLimit)]
        public string Subject { get; set; }

        [StringLength(64000, ErrorMessage = StringTable.ErrMsgLengthLimit)]
        public string Body { get; set; }

        [Display(Name = "Comments")]
        public string Message { get; set; }

        public string AuditInfo { get; set; }

        [Display(Name = "Date")]
        public ODateTime CreatedDate { get; set; }

        [Display(Name = "Date")]
        public string CreatedDateTxt { get { return CreatedDate.SafeProp(m => m.Text); } }

        //true: admin, false : display FE
        public bool Private { get; set; }

        public int Flags { get; set; }

        public byte? OrderStatus { get; set; }

        [Display(Name = "Status")]
        public string OrderStatusTxt
        {
            get { return typeof(OrderStatus).LocalizeEnumValue(OrderStatus ?? 0); }
        }

        [Display(Name = "Notice customer")]
        public bool NoticeCustomer { get { return (this.Flags == (int)OrderNoteFlags.SentEmail); } }
    }
}