using System.ComponentModel.DataAnnotations;
using Claw.Core.DAL;
using Claw.Modules.Eshop.DAL;
using Claw.Core;
using Claw.Core.Extensions;
using System.Web.Mvc;

namespace Claw.Modules.Eshop.Models
{
    [CustomValidation(typeof(SupplierModel), "Validate")]
    public class SupplierModel
    {
        public SupplierModel()
        {
            Active = true;
        }

        public int Id { get; set; }

        [Remote("ValidateName", "Supplier", AdditionalFields = "Id", HttpMethod = "POST", ErrorMessage = StringTable.ErrMsgUniqueConstraint)]
        [Display(Name = "Name")]
        [Required(ErrorMessage = StringTable.ErrMsgRequiredField)]
        public string Name { get; set; }

        [Display(Name = "Information")]
        public string Information { get; set; }

        [Display(Name = "Information")]
        public virtual string ShortInfo
        {
            get
            {
                return !string.IsNullOrEmpty(Information) ? Globals.HtmlDecode(Information).TrimLeftTextRemoveTag(250) : "";
            }
        }

        [Display(Name = "Visibility")]
        public bool Active { get; set; }

        #region Validate

        public static ValidationResult Validate(SupplierModel obj, ValidationContext cxt)
        {
            if (obj == null) return null;

            if (BaseMgr.Exists<Domain.Supplier>(m => m.Id, obj.Id, null, new KeyValuePair() { Key = "Name", Value = obj.Name }))
            {
                return new ValidationResult(StringTable.ErrMsgUniqueConstraint, new[] { "Name" });
            }

            return null;
        }

        #endregion
    }
}