using System.ComponentModel.DataAnnotations;

namespace Claw.Modules.Eshop.Models
{
    [CustomValidation(typeof(RegionLangModel), "Validate")]
    public class RegionLangModel : ILanguage
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public string LangCode { get; set; }

        public int? RegionId { get; set; }

        #region IsDataModelEmpty

        public bool IsDataModelEmpty
        {
            get { return string.IsNullOrWhiteSpace(Name); }
        }

        #endregion

        #region Validate
        public static ValidationResult Validate(RegionLangModel obj, ValidationContext cxt)
        {
            if (obj == null) return null;

            return null;
        }
        #endregion
    }
}