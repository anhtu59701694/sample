using System;
using System.ComponentModel.DataAnnotations;
using System.Web.Mvc;
using Claw.Core.DAL;
using Claw.Modules.Eshop.DAL;
using Claw.Modules.Eshop.Domain;
using Claw.Modules.Eshop.Helper;

namespace Claw.Modules.Eshop.Models
{
    [CustomValidation(typeof(CurrencyModel), "Validate")]
    public class CurrencyModel
    {
        public const string CurCode = "currency";
        public const string CurrencyCode = "currencycode";
        public int Id { get; set; }

        #region Raw
        protected Currency _raw;

        public Currency Raw { get { return null; } set { _raw = value; } }

        public Currency GetRaw() { return _raw; }
        #endregion

        public bool InlineEdit { get; set; }

        public bool QuickAdd { get; set; }

        [Remote("ValidateCode", "Currency", AdditionalFields = "Id", HttpMethod = "POST", ErrorMessage = StringTable.ErrMsgUniqueConstraint)]
        [Required(ErrorMessage = StringTable.ErrMsgRequiredField)]
        [StringLength(3, MinimumLength = 3, ErrorMessage = StringTable.ErrMsgLengthRangeLimit)]
        [Display(Name = "Code")]
        public string Code { get; set; }

        [Required(ErrorMessage = StringTable.ErrMsgRequiredField)]
        [Range(typeof(OFloat), "0", StringTable.MaxOFloat, ErrorMessage = StringTable.ErrMsgRangeLimit)]
        [Display(Name = "Exchange rate")]
        public OFloat Rate { get; set; }

        [Required(ErrorMessage = StringTable.ErrMsgRequiredField)]
        [StringLength(3, ErrorMessage = StringTable.ErrMsgLengthLimit)]
        [RegularExpression(@"[^0-9\s\,\.]+", ErrorMessage = StringTable.ErrMsgInvalidCurrencyToken)]
        [Display(Name = "Currency token")]
        public string Token { get; set; }

        /// <summary>true:left, false:right</summary>
        [Display(Name = "Currency location")]
        public bool Location { get; set; }

        [Required(ErrorMessage = StringTable.ErrMsgRequiredField)]
        [Display(Name = "Decimal token")]
        public string DecimalSep { get; set; }

        [Required(ErrorMessage = StringTable.ErrMsgRequiredField)]
        [Display(Name = "Thousand token")]
        public string ThousandSep { get; set; }

        [Required(ErrorMessage = StringTable.ErrMsgRequiredField)]
        [Display(Name = "Decimal places")]
        public int DecimalPlaces { get; set; }

        [Display(Name = "Visibility")]
        public bool Active { get; set; }

        [Required(ErrorMessage = StringTable.ErrMsgRequiredField)]
        [Range(0, 9999, ErrorMessage = StringTable.ErrMsgRangeLimit)]
        [Display(Name = "Sort order")]
        public int DisplayOrder { get; set; }

        [Required(ErrorMessage = StringTable.ErrMsgRequiredField)]
        [StringLength(100, ErrorMessage = StringTable.ErrMsgLengthLimit)]
        [Display(Name = "Currency name")]
        public string Name { get; set; }

        [Display(Name = "Default currency")]
        public bool Default { get; set; }

        public bool? AnyOrder { get; set; }

        #region Validate

        public static ValidationResult Validate(CurrencyModel obj, ValidationContext cxt)
        {
            if (obj == null) return null;
            
            CurrencyCodeType cVal;
            if (cxt != null && !obj.Code.IsNullOrEmpty() && !Enum.TryParse(obj.Code, true, out cVal))
                return new ValidationResult(StringTable.ErrMsgInvalidCurrencyCode, new[] {"Code"});

            if (cxt != null && obj.Rate <= 0m)
            {
                return new ValidationResult(StringTable.ErrMsgRequirePositiveNumber, new[] {"Rate"});
            }

            if (BaseMgr.Exists<Domain.Currency>(m => m.Id, obj.Id, null,
                new KeyValuePair() { Key = "Code", Value = obj.Code }))
            {
                return new ValidationResult(StringTable.ErrMsgUniqueConstraint, new[] { "Code" });
            }

            return null;
        }

        #endregion
    }
}