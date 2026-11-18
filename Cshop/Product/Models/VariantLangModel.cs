using System.ComponentModel.DataAnnotations;
using Claw.Core.DAL;
using Claw.Modules.Eshop.DAL;
using System.Web.Mvc;

namespace Claw.Modules.Eshop.Models
{
    [CustomValidation(typeof(VariantLangModel), "Validate")]
    public class VariantLangModel : ILanguage
    {
        public int Id { get; set; }

        [StringLength(8000, ErrorMessage = StringTable.ErrMsgLengthLimit)]
        [Display(Name = "Description")]
        public string DescriptionV { get; set; }

        [Required(ErrorMessage = StringTable.ErrMsgRequiredField)]
        [StringLength(10, ErrorMessage = StringTable.ErrMsgLengthLimit)]
        public string LangCode { get; set; }

        public int VariantId { get; set; }

        #region IsDataModelEmpty

        public bool IsDataModelEmpty
        {
            get { return string.IsNullOrWhiteSpace(DescriptionV); }
        }

        #endregion

        #region Validate
        public static ValidationResult Validate(VariantLangModel obj, ValidationContext cxt)
        {
            if (obj == null) return null;

            return null;
        }
        #endregion
    }
}