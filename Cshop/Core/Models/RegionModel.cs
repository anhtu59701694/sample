using System.ComponentModel.DataAnnotations;
using Claw.Core.DAL;
using Claw.Modules.Eshop.DAL;
using Claw.Modules.Eshop.Domain;

namespace Claw.Modules.Eshop.Models
{
    [CustomValidation(typeof(RegionModel), "Validate")]
    public class RegionModel : BaseModel<Region, RegionLangModel>
    {
        public string Code { get; set; }

        #region Name

        [Required(ErrorMessage = StringTable.ErrMsgRequiredField)]
        [StringLength(100, ErrorMessage = StringTable.ErrMsgLengthLimit)]
        public string Name
        {
            get { return GetLang(m => m.Name); }
            set { SetLang(m => { m.Name = value; }); }
        }

        #endregion

        #region Validate

        public static ValidationResult Validate(RegionModel obj, ValidationContext cxt)
        {
            if (obj == null) return null;

            if (BaseMgr.Exists<Domain.Region>(m => m.Id, obj.Id, null,
                new KeyValuePair() { Key = "Code", Value = obj.Code }))
            {
                return new ValidationResult(StringTable.ErrMsgUniqueConstraint, new[] { "Code" });
            }

            return null;
        }

        #endregion
    }
}