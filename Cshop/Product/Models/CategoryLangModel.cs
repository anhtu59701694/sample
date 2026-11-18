using System;
using System.ComponentModel.DataAnnotations;
using System.Web.Mvc;
using Claw.Core;
using Claw.Core.DAL;
using Claw.Modules.Eshop.DAL;

namespace Claw.Modules.Eshop.Models
{
    [CustomValidation(typeof(CategoryLangModel), "Validate")]
    public class CategoryLangModel : ILanguage2
    {
        public CategoryLangModel()
        {
            Visible = true;
        }

        public int Id { get; set; }

        [Remote("ValidateLangAlias", "Category", AdditionalFields = "Id,CategoryId", HttpMethod = "POST", ErrorMessage = StringTable.ErrMsgUniqueAliasConstraint)]
        [Required(ErrorMessage = StringTable.ErrMsgRequiredField)]
        [RegularExpression(StringTable.RegexAlias, ErrorMessage = StringTable.ErrMsgInvalidAlias)]
        [StringLength(400, ErrorMessage = StringTable.ErrMsgLengthLimit)]
        [Display(Name = "URL name")]
        public string Alias { get; set; }

        [Remote("ValidateName", "Category", AdditionalFields = "Id,CategoryId,ParentId", HttpMethod = "POST", ErrorMessage = StringTable.ErrMsgUniqueConstraint)] 
        [Required(ErrorMessage = StringTable.ErrMsgRequiredField)]
        [StringLength(150, ErrorMessage = StringTable.ErrMsgLengthLimit)]
        [Display(Name = "Category name")]
        public string Name { get; set; }

        [StringLength(32767, ErrorMessage = StringTable.ErrMsgLengthLimit)]
        public string Description { get; set; }

        [Display(Name = "Page title")]
        [StringLength(70, ErrorMessage = StringTable.ErrMsgLengthLimit)]
        public string PageTitle { get; set; }

        [Display(Name = "Meta description")]
        [StringLength(500, ErrorMessage = StringTable.ErrMsgLengthLimit)]
        public string MetaDesc { get; set; }

        [Required(ErrorMessage = StringTable.ErrMsgRequiredField)]
        [StringLength(10, ErrorMessage = StringTable.ErrMsgLengthLimit)]
        public string LangCode { get; set; }

        [Display(Name = "Visible to this language")]
        public bool Visible { get; set; }

        public int CategoryId { get; set; }

        public int? ParentId { get; set; }

        #region IsDataModelEmpty
        public bool IsDataModelEmpty
        {
            get
            {
                return string.IsNullOrWhiteSpace(Name) && string.IsNullOrWhiteSpace(Alias) &&
                       string.IsNullOrWhiteSpace(Description) &&
                       string.IsNullOrWhiteSpace(PageTitle) && string.IsNullOrWhiteSpace(MetaDesc);
            }
        } 
        #endregion

        #region Validate
        public static ValidationResult Validate(CategoryLangModel obj, ValidationContext cxt)
        {
            if (obj == null) return null;

            if (!obj.Alias.IsNullOrEmpty() && BaseMgr.Exists<Domain.CategoryLang>(m => m.Id, obj.Id,
                m => m.Category.Id != obj.CategoryId,
                new KeyValuePair() {Key = "Alias", Value = obj.Alias}))
            {
                return new ValidationResult(StringTable.ErrMsgUniqueAliasConstraint, new[] {"Alias"});
            }

            if (!obj.Name.IsNullOrEmpty() && !Helper.SettingsBiz.GetSettings().Hidden.CateProdNameDuplicated && BaseMgr.Exists<Domain.CategoryLang>(m => m.Id, obj.Id,
                m => m.Category.Id != obj.CategoryId && m.Category.Parent.GetId(0) == (obj.ParentId ?? 0), 
                new KeyValuePair() { Key = "Name", Value = obj.Name }))
            {
                return new ValidationResult(StringTable.ErrMsgUniqueConstraint, new[] { "Name" });
            }

            return null;
        }
        #endregion
    }
}