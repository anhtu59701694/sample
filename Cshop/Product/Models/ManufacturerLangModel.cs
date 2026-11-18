using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Claw.Core.DAL;
using Claw.Modules.Eshop.DAL;
using Claw.Modules.Eshop.Helper;

namespace Claw.Modules.Eshop.Models
{
    [CustomValidation(typeof(ManufacturerLangModel), "Validate")]
    public class ManufacturerLangModel : ILanguage2
    {
        public ManufacturerLangModel()
        {
            Visible = true;
        }

        public int Id { get; set; }

        [Display(Name = "URL name")]
        [Remote("ValidateLangAlias", "Manufacturer", AdditionalFields = "Id,ManufacturerId", HttpMethod = "POST", ErrorMessage = StringTable.ErrMsgUniqueAliasConstraint)]
        [Required(ErrorMessage = StringTable.ErrMsgRequiredField)]
        [RegularExpression(StringTable.RegexAlias, ErrorMessage = StringTable.ErrMsgInvalidAlias)]
        [StringLength(200, ErrorMessage = StringTable.ErrMsgLengthLimit)]
        public string Alias { get; set; }

        [Remote("ValidateName", "Manufacturer", AdditionalFields = "Id,ManufacturerId", HttpMethod = "POST", ErrorMessage = StringTable.ErrMsgUniqueConstraint)]
        [Required(ErrorMessage = StringTable.ErrMsgRequiredField)]
        [StringLength(200, ErrorMessage = StringTable.ErrMsgLengthLimit)]
        [Display(Name = "Name")]
        public string Name { get; set; }

        [StringLength(64000, ErrorMessage = StringTable.ErrMsgLengthLimit)]
        [Display(Name = "Information")]
        public string Description { get; set; }

        [Display(Name = "Page title")]
        [StringLength(70, ErrorMessage = StringTable.ErrMsgLengthLimit)]
        public string PageTitle { get; set; }

        [Display(Name = "Meta description")]
        [StringLength(160, ErrorMessage = StringTable.ErrMsgLengthLimit)]
        public string MetaDesc { get; set; }

        [Required(ErrorMessage = StringTable.ErrMsgRequiredField)]
        [StringLength(10, ErrorMessage = StringTable.ErrMsgLengthLimit)]
        public string LangCode { get; set; }

        [Display(Name = "Visible to this language")]
        public bool Visible { get; set; }

        public int ManufacturerId { get; set; }

        #region IsDataModelEmpty
        public bool IsDataModelEmpty
        {
            get
            {
                return string.IsNullOrWhiteSpace(Name) && string.IsNullOrWhiteSpace(Alias)
                       && string.IsNullOrWhiteSpace(Description) &&
                       string.IsNullOrWhiteSpace(PageTitle) && string.IsNullOrWhiteSpace(MetaDesc);
            }
        }
        #endregion

        #region Validate
        public static ValidationResult Validate(ManufacturerLangModel obj, ValidationContext cxt)
        {
            if (obj == null) return null;

            if (!obj.Alias.IsNullOrEmpty() && !QParam.GetField<bool>("QuickAdd", false) && 
                BaseMgr.Exists<Domain.ManufacturerLang>(m => m.Id, obj.Id, m => m.Manufacturer.Id != obj.ManufacturerId,
                new KeyValuePair() { Key = "Alias", Value = obj.Alias }))
            {
                return new ValidationResult(StringTable.ErrMsgUniqueAliasConstraint, new[] { "Alias" });
            }

            if (!obj.Name.IsNullOrEmpty() && BaseMgr.Exists<Domain.ManufacturerLang>(m => m.Id, obj.Id, m => m.Manufacturer.Id != obj.ManufacturerId,
                new KeyValuePair() { Key = "Name", Value = obj.Name }))
            {
                return new ValidationResult(StringTable.ErrMsgUniqueConstraint, new[] { "Name" });
            }

            return null;
        }
        #endregion
    }
}