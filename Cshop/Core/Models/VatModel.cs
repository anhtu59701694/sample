using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;
using System.Web.Script.Serialization;
using Claw.Core.DAL;
using Claw.Modules.Eshop.DAL;
using Claw.Modules.Eshop.Domain;

namespace Claw.Modules.Eshop.Models
{
    [CustomValidation(typeof(VatModel), "Validate")]
    public class VatModel
    {
        #region Raw
        protected Vat _raw;

        public Vat Raw { get { return null; } set { _raw = value; } }

        public Vat GetRaw() { return _raw; } 
        #endregion

        public VatModel()
        {
            Active = true;
        }

        public int Id { get; set; }

        public bool InlineEdit { get; set; }

        public bool QuickAdd { get; set; }

        [StringLength(100, ErrorMessage = StringTable.ErrMsgLengthLimit)]
        [Required(ErrorMessage = StringTable.ErrMsgRequiredField)]
        [Display(Name = "Tax name")]
        public string Name { get; set; }

        [Required(ErrorMessage = StringTable.ErrMsgRequiredField)]
        public OFloat Value { get; set; }

        #region ValTxt

        [Required(ErrorMessage = StringTable.ErrMsgRequiredField)]
        [Range(typeof(OFloat), "0", "100", ErrorMessage = StringTable.ErrMsgRangeLimit)]
        [Display(Name = "Value")]
        public string ValTxt
        {
            get { return Value == null ? "" : new OFloat(Value.Value * 100) + " %"; }
            set
            {
                if (string.IsNullOrEmpty(value)) Value = null;
                else
                {
                    int idx = value.IndexOf('%');
                    var tmp = OFloat.TryParse(value.Substring(0, idx == -1 ? value.Length : idx).Trim());
                    Value = tmp.IsNull() ? null : new OFloat(tmp / 100.0);
                }
            }
        }

        #endregion

        [Display(Name = "Take-way service tax")]
        public bool TakeAway { get; set; }

        [Display(Name = "Visibility")]
        public bool Active { get; set; }

        [Display(Name = "Default tax")]
        public bool Default { get; set; }

        public bool? AnyDepend { get; set; }

        #region Validate
        public static ValidationResult Validate(VatModel obj, ValidationContext cxt)
        {
            if (obj == null) return null;

            if (obj.Value == null)
                return new ValidationResult(StringTable.ErrMsgRequiredField, new[] { "Value" });

            if (obj.Value != null && BaseMgr.Exists<Domain.Vat>(m => m.Id, obj.Id, null,
                new KeyValuePair() { Key = "Value", Value = (decimal)obj.Value }))
            {
                return new ValidationResult(StringTable.ErrMsgUniqueConstraint, new[] { "Value" });
            }

            if (BaseMgr.Exists<Domain.Vat>(m => m.Id, obj.Id, null,
                new KeyValuePair() { Key = "Name", Value = obj.Name }))
            {
                return new ValidationResult(StringTable.ErrMsgUniqueConstraint, new[] { "Name" });
            }

            return null;
        }
        #endregion
    }
}