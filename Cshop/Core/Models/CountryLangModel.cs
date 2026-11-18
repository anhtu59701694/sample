using System.ComponentModel.DataAnnotations;

namespace Claw.Modules.Eshop.Models
{
    [CustomValidation(typeof(CountryLangModel), "Validate")]
    public class CountryLangModel : ILanguage
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public string LangCode { get; set; }

        public int? CountryId { get; set; }

        #region IsDataModelEmpty

        public bool IsDataModelEmpty
        {
            get { return string.IsNullOrWhiteSpace(Name); }
        }

        #endregion

        #region Validate
        public static ValidationResult Validate(CountryLangModel obj, ValidationContext cxt)
        {
            if (obj == null) return null;

            return null;
        }
        #endregion
    }
}