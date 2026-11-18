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
    [CustomValidation(typeof(PropValModel), "Validate")]
    public class PropValModel : BaseModel<PropVal, PropValLangModel>, Domain.IDisplayOrder
    {
        public class Node
        {
            public class QtyStatus
            {
                public int StatusId { get; set; }
                public int SumQty { get; set; }
                public bool EnableBuy { get; set; }
                public bool ContactLink { get; set; }
                public bool BackDate { get; set; }
            }
            public int Id { get; set; }
            public string Name { get; set; }
            public string Code { get; set; }
            public string Icon { get; set; }
            public List<Node> Children { get; set; }
            public int? VariantId { get; set; }
            public List<QtyStatus> QtyStatuses { get; set; }
            public OMoneyMc PriceOffset { get; set; }
        }

        public PropValModel()
        {
            DisplayOrder = 1000;
            Active = true;
        }

        [Remote("ValidateCode", "PropVal", AdditionalFields = "Id", HttpMethod = "POST", ErrorMessage = StringTable.ErrMsgUniqueConstraint)]
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
        [Display(Name = "Icon")]
        public string ImageThumb { get; set; }

        public string ImageFsUrl { get; set; }


        [Required(ErrorMessage = StringTable.ErrMsgRequiredField)]
        [Display(Name = "Variant")]
        public int PropertyId { get; set; }

        [Display(Name = "Variant")]
        public string PropertyName { get; set; }

        #region Name

        [Required(ErrorMessage = StringTable.ErrMsgRequiredField)]
        [StringLength(50, ErrorMessage = StringTable.ErrMsgLengthLimit)]
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
        public static ValidationResult Validate(PropValModel obj, ValidationContext cxt)
        {
            if (obj == null) return null;

            if (obj.Id != 0 && BaseMgr.Exists<Domain.PropVal>(m => m.Id, obj.Id, null,
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