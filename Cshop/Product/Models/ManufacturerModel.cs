using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Claw.Core.DAL;
using Claw.Modules.Eshop.DAL;
using Claw.Modules.Eshop.Domain;

namespace Claw.Modules.Eshop.Models
{
    [CustomValidation(typeof(ManufacturerModel), "Validate")]
    public class ManufacturerModel : BaseModel<Manufacturer, ManufacturerLangModel>, Domain.IDisplayOrder
    {
        public ManufacturerModel()
        {
            DisplayOrder = 1000;
            Active = true;
        }

        [Display(Name = "Website")]
        public string Url { get; set; }

        public string Logo { get; set; }

        [Display(Name = "Logo")]
        public string ImageThumb { get; set; }

        public string ImageFsUrl { get; set; }

        [Display(Name = "Visibility")]
        public bool Active { get; set; }

        [Required(ErrorMessage = StringTable.ErrMsgRequiredField)]
        [Range(0, 9999, ErrorMessage = StringTable.ErrMsgRangeLimit)]
        [Display(Name = "Sort order")]
        public int DisplayOrder { get; set; }

        #region Alias

        public string Alias
        {
            get { return GetLang(m => m.Alias); }
            set { SetLang(m => { m.Alias = value; }); }
        }

        #endregion

        #region Name
        [Display(Name = "Name")]
        [Required(ErrorMessage = StringTable.ErrMsgRequiredField)]
        [StringLength(200, ErrorMessage = StringTable.ErrMsgLengthLimit)]
        public string Name
        {
            get { return GetLang(m => m.Name); }
            set { SetLang(m => { m.Name = value; }); }
        }
        #endregion

        #region LangVisible
        public bool? LangVisible
        {
            get { return GetLang(m => m.Visible); }
        }
        #endregion  

        #region Validate
        public static ValidationResult Validate(ManufacturerModel obj, ValidationContext cxt)
        {
            if (obj == null) return null;

            if (obj.QuickAdd && BaseMgr.Exists<Domain.Manufacturer>(m => m.Id, obj.Id, null,
                new KeyValuePair() { Key = "Alias", Value = obj.Alias }))
            {
                return new ValidationResult(StringTable.ErrMsgUniqueConstraint, new[] { "Name" });
            }

            return null;
        }
        #endregion
    }
}