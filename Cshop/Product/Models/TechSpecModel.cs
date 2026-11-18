using System.ComponentModel.DataAnnotations;
using Claw.Modules.Eshop.Domain;

namespace Claw.Modules.Eshop.Models
{
    [CustomValidation(typeof(TechSpecModel), "Validate")]
    public class TechSpecModel : BaseExModel<TechSpec>, Domain.IDisplayOrder
    {
        public TechSpecModel()
        {
            DisplayOrder = 1000;
        }

        [Required(ErrorMessage = StringTable.ErrMsgRequiredField)]
        [StringLength(200, ErrorMessage = StringTable.ErrMsgLengthLimit)]
        public string Name { get; set; }

        public string Icon { get; set; }

        [Display(Name = "Icon")]
        public string IconThumb { get; set; }

        public string IconFsUrl { get; set; }

        [Display(Name = "Visibility")]
        public bool Active { get; set; }

        public bool Featured { get; set; }

        [Required(ErrorMessage = StringTable.ErrMsgRequiredField)]
        [Range(0, 9999, ErrorMessage = StringTable.ErrMsgRangeLimit)]
        [Display(Name = "Sort order")]
        public int DisplayOrder { get; set; }

        [StringLength(64000, ErrorMessage = StringTable.ErrMsgLengthLimit)]
        [Display(Name = "Note")]
        public string Description { get; set; }

        public int? Values { get; set; }

        #region Validate
        public static ValidationResult Validate(TechSpecModel obj, ValidationContext cxt)
        {
            if (obj == null) return null;

            return null;
        }
        #endregion
    }
}