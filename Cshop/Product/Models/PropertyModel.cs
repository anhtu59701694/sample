using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Claw.Core;
using Claw.Core.DAL;
using Claw.Modules.Eshop.Controllers.Admin;
using Claw.Modules.Eshop.Domain;
using Claw.Modules.Eshop.DAL;
using System.Web.Mvc;
using AutoMapper;

namespace Claw.Modules.Eshop.Models
{
    [CustomValidation(typeof(PropertyModel), "Validate")]
    public class PropertyModel : BaseModel<Property, PropertyLangModel>, Domain.IDisplayOrder
    {

        public PropertyModel()
        {
            DisplayOrder = 1000;
            Active = true;
        }

        [Remote("ValidateCode", "Property", AdditionalFields = "Id", HttpMethod = "POST", ErrorMessage = StringTable.ErrMsgUniqueConstraint)]
        [Required(ErrorMessage = StringTable.ErrMsgRequiredField)]
        [StringLength(50, ErrorMessage = StringTable.ErrMsgLengthLimit)]
        public string Code { get; set; }

        [Required(ErrorMessage = StringTable.ErrMsgRequiredField)]
        [Range(0, 9999, ErrorMessage = StringTable.ErrMsgRangeLimit)]
        [Display(Name = "Sort order")]
        public int DisplayOrder { get; set; }

        [Display(Name = "Created Date")]
        public ODateTime CreatedDate { get; set; }

        public DateTime? ModifiedDate { get; set; }

        [Display(Name = "Visibility")]
        public bool? Active { get; set; }

        public string Icon { get; set; }
        [Display(Name = "Variant icon")]
        public string ImageThumb { get; set; }

        public string ImageFsUrl { get; set; }


        [Display(Name = "Values")]
        public int PropValCount { get { return _raw.GetPropValCount(); } }

        [Display(Name = "Value")]
        public string PropValListTxt { get; set; }

        #region Name

        [Required(ErrorMessage = StringTable.ErrMsgRequiredField)]
        [StringLength(150, ErrorMessage = StringTable.ErrMsgLengthLimit)]
        [Display(Name = "Name")]
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

        #region GetPropVals

        public List<PropValModel> GetPropVals(bool? active = null)
        {
            return _raw.GetPropVals(active);
        }

        #endregion

        #region Validate
        public static ValidationResult Validate(PropertyModel obj, ValidationContext cxt)
        {
            if (obj == null) return null;

            if (BaseMgr.Exists<Domain.Property>(m => m.Id, obj.Id, null,
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