using System;
using System.ComponentModel.DataAnnotations;
using System.Web.Mvc;
using Claw.Core;
using Claw.Core.DAL;
using Claw.Modules.Eshop.DAL;

namespace Claw.Modules.Eshop.Models
{
    [CustomValidation(typeof(ProductLangModel), "Validate")]
    public class ProductLangModel : ILanguage2
    {
        public ProductLangModel()
        {
            Visible = true;
        }

        public int Id { get; set; }

        [Exportable(Name = "Url name")]
        [Remote("ValidateLangAlias", "Product", AdditionalFields = "Id,ProductId", HttpMethod = "POST", ErrorMessage = StringTable.ErrMsgUniqueAliasConstraint)]
        [Required(ErrorMessage = StringTable.ErrMsgRequiredField)]
        [RegularExpression(StringTable.RegexAlias, ErrorMessage = StringTable.ErrMsgInvalidAlias)]
        [StringLength(400, ErrorMessage = StringTable.ErrMsgLengthLimit)]
        [Display(Name = "URL name")]
        public string Alias { get; set; }

        [Exportable(Name = "Name")]
        //[Remote("ValidateName", "Product", AdditionalFields = "Id,ProductId", HttpMethod = "POST", ErrorMessage = StringTable.ErrMsgUniqueConstraint)] 
        [Required(ErrorMessage = StringTable.ErrMsgRequiredField)]
        [StringLength(400, ErrorMessage = StringTable.ErrMsgLengthLimit)]
        [Display(Name = "Product name")]
        public string Name { get; set; }

        [Exportable(Name = "Tag")]
        [StringLength(8000, ErrorMessage = StringTable.ErrMsgLengthLimit)]
        [Display(Name = "Product tag names")] 
        public string Tags { get; set; }

        [Exportable(Name = "Description")]
        [StringLength(1000000, ErrorMessage = StringTable.ErrMsgLengthLimit)]
        [Display(Name = "Information")] 
        public string Description { get; set; }

        [Exportable(Name = "Summary info")]
        [StringLength(32767, ErrorMessage = StringTable.ErrMsgLengthLimit)]
        [Display(Name = "Extra information")]
        public string ExtraInfo { get; set; }

        [StringLength(1000000, ErrorMessage = StringTable.ErrMsgLengthLimit)]
        [Display(Name = "Extra information 2")]
        public string ExtraInfo2 { get; set; }

        [StringLength(1000000, ErrorMessage = StringTable.ErrMsgLengthLimit)]
        [Display(Name = "Extra information 3")]
        public string ExtraInfo3 { get; set; }

        [StringLength(1000000, ErrorMessage = StringTable.ErrMsgLengthLimit)]
        [Display(Name = "Extra information 4")]
        public string ExtraInfo4 { get; set; }

        [Exportable(Name = "Page title")]
        [Display(Name = "Page title")]
        [StringLength(70, ErrorMessage = StringTable.ErrMsgLengthLimit)]
        public string PageTitle { get; set; }

        [Exportable(Name = "Meta description")]
        [Display(Name = "Meta description")]
        [StringLength(500, ErrorMessage = StringTable.ErrMsgLengthLimit)]
        public string MetaDesc { get; set; }

        [Required(ErrorMessage = StringTable.ErrMsgRequiredField)]
        [StringLength(10, ErrorMessage = StringTable.ErrMsgLengthLimit)]
        public string LangCode { get; set; }

        [Display(Name = "Visible to this language")]
        public bool Visible { get; set; }

        public int ProductId { get; set; }

        #region IsDataModelEmpty
        public bool IsDataModelEmpty
        {
            get
            {
                return string.IsNullOrWhiteSpace(Name) && string.IsNullOrWhiteSpace(Alias) &&
                       string.IsNullOrWhiteSpace(Description) && string.IsNullOrWhiteSpace(ExtraInfo) &&
                       string.IsNullOrWhiteSpace(PageTitle) && string.IsNullOrWhiteSpace(MetaDesc);
            }
        } 
        #endregion

        #region Validate
        public static ValidationResult Validate(ProductLangModel obj, ValidationContext cxt)
        {
            if (obj == null) return null;

            if (!obj.Alias.IsNullOrEmpty() &&
                BaseMgr.Exists<Domain.ProductLang>(m => m.Id, obj.Id, m => m.Product.Id != obj.ProductId,
                new KeyValuePair() { Key = "Alias", Value = obj.Alias }))
            {
                return new ValidationResult(StringTable.ErrMsgUniqueAliasConstraint, new[] { "Alias" });
            }

            //if (!obj.Name.IsNullOrEmpty()  && !Helper.SettingsBiz.GetSettings().Hidden.CateProdNameDuplicated && BaseMgr.Exists<Domain.ProductLang>(m => m.Id, obj.Id, m => m.Product.Id != obj.ProductId, 
            //    new KeyValuePair() { Key = "Name", Value = obj.Name }))
            //{
            //    return new ValidationResult(StringTable.ErrMsgUniqueConstraint, new[] { "Name" });
            //}

            return null;
        }
        #endregion
    }
}