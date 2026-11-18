using System.ComponentModel.DataAnnotations;
using Claw.Modules.Eshop.Domain;

namespace Claw.Modules.Eshop.Models
{
    public class TechSpecValModel : BaseExModel<TechSpecVal>
    {
        [Display(Name = "Tech spec")]
        public int? TechSpecId { get; set; }

        [Display(Name = "Tech spec")]
        public string TechSpecName { get; set; }

        public int TechSpecDOrder { get; set; }

        public bool TechSpecActive { get; set; }

        public string Value { get; set; }

        [StringLength(64000, ErrorMessage = StringTable.ErrMsgLengthLimit)]
        [Display(Name = "Note")]
        public string Description { get; set; }

        [Required(ErrorMessage = StringTable.ErrMsgRequiredField)]
        [Range(0, 9999, ErrorMessage = StringTable.ErrMsgRangeLimit)]
        [Display(Name = "Sort order")]
        public int DisplayOrder { get; set; }

        public int? Products { get; set; }

    }
}