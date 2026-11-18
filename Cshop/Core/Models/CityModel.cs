using System.ComponentModel.DataAnnotations;
using Claw.Core.DAL;
using Claw.Modules.Eshop.DAL;
using Claw.Modules.Eshop.Domain;

namespace Claw.Modules.Eshop.Models
{
    [CustomValidation(typeof(CityModel), "Validate")]
    public class CityModel : BaseModel<City, CityLangModel>
    {
        [Required(ErrorMessage = StringTable.ErrMsgRequiredField)]
        [StringLength(100, ErrorMessage = StringTable.ErrMsgLengthLimit)]
        public string Code { get; set; }

        public int? CountryId { get; set; }
        
        public string CountryName { get; set; }

        #region Name

        [Required(ErrorMessage = StringTable.ErrMsgRequiredField)]
        [StringLength(100, ErrorMessage = StringTable.ErrMsgLengthLimit)]
        public string Name
        {
            get { return GetLang(m => m.Name, true); }
            set { SetLang(m => { m.Name = value; }); }
        }

        #endregion

        #region Validate

        public static ValidationResult Validate(CityModel obj, ValidationContext cxt)
        {
            if (obj == null) return null;

            if (BaseMgr.Exists<Domain.City>(m => m.Id, obj.Id, null,
                new KeyValuePair() { Key = "Code", Value = obj.Code }))
            {
                if (obj.QuickAdd) return new ValidationResult(StringTable.ErrMsgUniqueConstraint, new[] { "Name" });
                else return new ValidationResult(StringTable.ErrMsgUniqueConstraint, new[] { "Code" });
            }

            return null;
        }

        #endregion
    }
}