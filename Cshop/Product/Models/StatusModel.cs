using System.ComponentModel.DataAnnotations;
using System.Web.Mvc;
using Claw.Core.DAL;
using Claw.Modules.Eshop.DAL;
using Claw.Modules.Eshop.Domain;

namespace Claw.Modules.Eshop.Models
{
    [CustomValidation(typeof(StatusModel), "Validate")]
    public class StatusModel : BaseModel<Status, StatusLangModel>
    {
        public StatusModel()
        {
            DescInstock = true;
            ShowIcon = true;
            ContactLink = false;
        }

        [Display(Name = "Code")]
        [Remote("ValidateCode", "Status", AdditionalFields = "Id", HttpMethod = "POST", ErrorMessage = StringTable.ErrMsgUniqueConstraint)]
        [Required(ErrorMessage = StringTable.ErrMsgRequiredField)]
        [StringLength(60, ErrorMessage = StringTable.ErrMsgLengthLimit)]
        public string Code { get; set; }

        [Display(Name = "Enable buy button")]
        public bool EnableBuy { get; set; }

        //[Display(Name = "Allow buying with 0 in quantity")]
        //public bool BuyZeroQty { get; set; }

        //[Display(Name = "Auto change to this status when its quantity reaches to 0")]
        //public int? ZeroQtyStatusId { get; set; }

        public string Icon { get; set; }

        [Display(Name = "Icon")]
        public string ImageThumb { get; set; }
        public string ImageFsUrl { get; set; }

        [Display(Name = "Link to contact page")]
        public bool ContactLink { get; set; }

        [Display(Name = "Enable status icon")]
        public bool ShowIcon { get; set; }

        [Display(Name = "Descrease in stock when order is placed")]
        public bool DescInstock { get; set; }

        [Display(Name = "Show product price")]
        public bool ShowPrice { get; set; }

        [Display(Name = "Show in frontend")]
        public bool ShowFrontEnd { get; set; }

        //[Display(Name = "Send notification email to admin when its quantity reach to X")]
        //[Range(0, int.MaxValue, ErrorMessage = StringTable.ErrMsgRequireNumber)]
        //[RegularExpression(@"^(\d*)$", ErrorMessage = StringTable.ErrMsgRequireNumber)]
        //public int EmailQtyLevel { get; set; }

        //[Display(Name = "Visibility")]
        //public bool Active { get; set; }

        #region Name

        [Display(Name = "Status name")]
        [Required(ErrorMessage = StringTable.ErrMsgRequiredField)]
        [StringLength(150, ErrorMessage = StringTable.ErrMsgLengthLimit)]
        public string Name
        {
            get { return GetLang(m => m.Name); }
            set { SetLang(m => { m.Name = value; }); }
        }

        #endregion

        #region Validate
        public static ValidationResult Validate(StatusModel obj, ValidationContext cxt)
        {
            if (obj == null) return null;

            if (BaseMgr.Exists<Domain.Status>(m => m.Id, obj.Id, null,
                new KeyValuePair() { Key = "Code", Value = obj.Code }))
            {
                if (obj.QuickAdd) return new ValidationResult(StringTable.ErrMsgUniqueConstraint, new[] { "Name" });
                else obj.Code = string.Format("{0}-{1}",obj.Code,System.DateTime.Now.Second);//return new ValidationResult(StringTable.ErrMsgUniqueConstraint, new[] { "Code" });
            }

            return null;
        }
        #endregion
    }
}