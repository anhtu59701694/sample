using System;
using System.ComponentModel.DataAnnotations;
using System.Web.Mvc;
using Claw.Core;
using Claw.Core.DAL;
using Claw.Modules.Eshop.DAL;

namespace Claw.Modules.Eshop.Models
{
    [CustomValidation(typeof(PropValLangModel), "Validate")]
    public class PropValLangModel : ILanguage2
    {
        public PropValLangModel()
        {
            Visible = true;
        }

        public int Id { get; set; }

        [Remote("ValidateName", "PropVal", AdditionalFields = "Id,PropValId,PropertyId", HttpMethod = "POST", ErrorMessage = StringTable.ErrMsgUniqueConstraint)] 
        [Required(ErrorMessage = StringTable.ErrMsgRequiredField)]
        [StringLength(50, ErrorMessage = StringTable.ErrMsgLengthLimit)]
        public string Name { get; set; }

        [Required(ErrorMessage = StringTable.ErrMsgRequiredField)]
        [StringLength(10, ErrorMessage = StringTable.ErrMsgLengthLimit)]
        public string LangCode { get; set; }

        [Display(Name = "Visible to this language")]
        public bool Visible { get; set; }

        public int PropValId { get; set; }

        public virtual int PropertyId { get; set; }

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
        public static ValidationResult Validate(PropValLangModel obj, ValidationContext cxt)
        {
            if (obj == null) return null;

            if (BaseMgr.Exists<Domain.PropValLang>(m => m.Id, obj.Id, 
                m => m.PropVal.Property.Id == obj.PropertyId && m.PropVal.Id != obj.PropValId, 
                new KeyValuePair() { Key = "Name", Value = obj.Name }))
            {
                return new ValidationResult(StringTable.ErrMsgUniqueConstraint, new[] { "Name" });
            }

            return null;
        }
        #endregion
    }
}