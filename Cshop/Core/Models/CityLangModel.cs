using System.ComponentModel.DataAnnotations;

namespace Claw.Modules.Eshop.Models
{
    [CustomValidation(typeof(CityLangModel), "Validate")]
    public class CityLangModel : ILanguage
    {
        public int Id { get; set; }

        [Required(ErrorMessage = StringTable.ErrMsgRequiredField)]
        [StringLength(100, ErrorMessage = StringTable.ErrMsgLengthLimit)]
        public string Name { get; set; }

        [Required(ErrorMessage = StringTable.ErrMsgRequiredField)]
        [StringLength(10, ErrorMessage = StringTable.ErrMsgLengthLimit)]
        public string LangCode { get; set; }

        public int CityId { get; set; }
       
        #region IsDataModelEmpty

        public bool IsDataModelEmpty
        {
            get { return string.IsNullOrWhiteSpace(Name); }
        }

        #endregion

        #region Validate
        public static ValidationResult Validate(CityLangModel obj, ValidationContext cxt)
        {
            if (obj == null) return null;

            return null;
        }
        #endregion
    }
}