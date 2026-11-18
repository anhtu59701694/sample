using System;
using System.ComponentModel.DataAnnotations;

namespace Claw.Modules.Eshop.Models
{
    public partial class ProductWaitingModel
    {
        public int Id { get; set; }

        public int? ProductId { get; set; }

        public int? VariantId { get; set; }

        [Display(Name = "Product")]
        public string ProductName { get; set; }

        [Required(ErrorMessage = StringTable.ErrMsgRequiredField)]
        [StringLength(150, ErrorMessage = StringTable.ErrMsgLengthLimit)]
        [EmailAddress(ErrorMessage = StringTable.ErrMsgInvalidEmail)]
        public string Email { get; set; }

        public byte? Status { get; set; }

        public string StatusTxt { get { return typeof (Domain.ProductWaitingStatus).LocalizeEnumValue(Status ?? 0); } }

        [Display(Name = "Created date")]
        public ODateTime CreatedDate { get; set; }
    }
}