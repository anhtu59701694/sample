using System;
using System.ComponentModel.DataAnnotations;
using Claw.Modules.Eshop;

namespace Claw.Modules.Eshop.Models
{
    public class ProductCGroupDiscModel
    {
        [Display(Name = "Group")]
        [Required(ErrorMessage = StringTable.ErrMsgRequiredField)]
        public int? GroupId { get; set; }

        public bool DirDiscount { get; set; }
        public string VariantDesc { get; set; }

        public bool Active { get; set; }

        public bool Type { get; set; }

        [Display(Name = "Discount")]
        [Required(ErrorMessage = StringTable.ErrMsgRequiredField)]
        [Range(typeof(OMoney), "0", StringTable.MaxOMoney, ErrorMessage = StringTable.ErrMsgRangeLimit)]
        public OMoney Rate { get; set; }

        #region IsDataModelEmpty

        public bool IsDataModelEmpty
        {
            get { return !Active || Rate.IsNull() || Rate <= 0 || GroupId == null; }
        }

        #endregion
    }
}