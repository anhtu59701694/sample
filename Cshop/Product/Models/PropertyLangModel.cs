using System;
using System.ComponentModel.DataAnnotations;
using System.Web.Mvc;
using Claw.Core;
using Claw.Core.DAL;
using Claw.Modules.Eshop.DAL;

namespace Claw.Modules.Eshop.Models
{
    [CustomValidation(typeof(PropertyLangModel), "Validate")]
    public class PropertyLangModel : ILanguage2
    {
        public PropertyLangModel()
        {
            Visible = true;
        }

        public int Id { get; set; }

        [Remote("ValidateName", "Property", AdditionalFields = "Id,PropertyId", HttpMethod = "POST", ErrorMessage = StringTable.ErrMsgUniqueConstraint)] 
        [Required(ErrorMessage = StringTable.ErrMsgRequiredField)]
        [StringLength(50, ErrorMessage = StringTable.ErrMsgLengthLimit)]
        public string Name { get; set; }

        [Required(ErrorMessage = StringTable.ErrMsgRequiredField)]
        [StringLength(10, ErrorMessage = StringTable.ErrMsgLengthLimit)]
        public string LangCode { get; set; }

        [Display(Name = "Visible to this language")]
        public bool Visible { get; set; }

        public int PropertyId { get; set; }

        #region IsDataModelEmpty
        public bool IsDataModelEmpty
        {
            get
            {
                return string.IsNullOrWhiteSpace(Name);
            }
        } 
        #endregion

        #region Validate
        public static ValidationResult Validate(PropertyLangModel obj, ValidationContext cxt)
        {
            if (obj == null) return null;

            if (BaseMgr.Exists<Domain.PropertyLang>(m => m.Id, obj.Id, m => m.Property.Id != obj.PropertyId, 
                new KeyValuePair() { Key = "Name", Value = obj.Name }))
            {
                return new ValidationResult(StringTable.ErrMsgUniqueConstraint, new[] { "Name" });
            }

            return null;
        }
        #endregion
    }
}