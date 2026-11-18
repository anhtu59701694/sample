using System.ComponentModel.DataAnnotations;
using Claw.Core.DAL;
using Claw.Modules.Eshop.DAL;
using Claw.Modules.Eshop.Domain;
using System;
using System.Web.Mvc;

namespace Claw.Modules.Eshop.Models
{
    [CustomValidation(typeof(EmailTemplModel), "Validate")]
    public class EmailTemplModel : BaseModel<EmailTempl, EmailTemplLangModel>
    {
        [Required(ErrorMessage = StringTable.ErrMsgRequiredField)]
        [StringLength(50, ErrorMessage = StringTable.ErrMsgLengthLimit)]
        public string Code { get; set; }

        [Required(ErrorMessage = StringTable.ErrMsgRequiredField)]
        [StringLength(100, ErrorMessage = StringTable.ErrMsgLengthLimit)]
        [Display(Name = "Template name")]
        public string Name { get; set; }

        [Remote("ValidateType", "EmailTempl", AdditionalFields = "Id", HttpMethod = "POST", ErrorMessage = StringTable.ErrMsgUniqueConstraint)]
        [Required(ErrorMessage = StringTable.ErrMsgRequiredField)]
        public int? Type { get; set; }

        [StringLength(150, ErrorMessage = StringTable.ErrMsgLengthLimit)]
        [Display(Name = "Upload attachment")]
        public string AttachFile { get; set; }

        [EmailAddress(ErrorMessage = StringTable.ErrMsgInvalidEmail)]
        public string From { get; set; }

        [Display(Name = "Reply to")]
        [EmailAddress(ErrorMessage = StringTable.ErrMsgInvalidEmail)]
        public string ReplyTo { get; set; }

        public bool HasFromReplyTo()
        {
            return !From.IsNullOrEmpty() || !ReplyTo.IsNullOrEmpty();
        }

        #region TypeTxt
        [Display(Name = "Template description")]
        public string TypeTxt
        {
            get { return typeof(EmailTemplateType).LocalizeEnumValue(Type ?? 0); }
        }
        #endregion

        #region Subject
        public string Subject
        {
            get { return GetLang(m => m.Subject); }
            set { SetLang(m => { m.Subject = value; }); }
        } 
        #endregion

        #region Body
        public string Body
        {
            get { return GetLang(m => m.Body); }
            set { SetLang(m => { m.Body = value; }); }
        }
        #endregion

        #region Validate

        public static ValidationResult Validate(EmailTemplModel obj, ValidationContext cxt)
        {
            if (obj == null) return null;

            if (cxt != null && !Enum.IsDefined(typeof(EmailTemplateType), obj.Type))
                return new ValidationResult(StringTable.ErrMsgRequiredField, new[] { "Type" });

            if (!obj.Code.IsNullOrEmpty() && BaseMgr.Exists<Domain.EmailTempl>(m => m.Id, obj.Id, null,
                new KeyValuePair() { Key = "Name", Value = obj.Name }))
            {
                return new ValidationResult(StringTable.ErrMsgUniqueConstraint, new[] { "Name" });
            }

            if (!obj.Code.IsNullOrEmpty() && BaseMgr.Exists<Domain.EmailTempl>(m => m.Id, obj.Id, null,
                new KeyValuePair() { Key = "Code", Value = obj.Code }))
            {
                return new ValidationResult(StringTable.ErrMsgUniqueConstraint, new[] { "Code" });
            }

            if (!obj.TypeTxt.IsNullOrEmpty() && BaseMgr.Exists<Domain.EmailTempl>(m => m.Id, obj.Id, null,
                new KeyValuePair() { Key = "Type", Value = obj.Type }))
            {
                return new ValidationResult(StringTable.ErrMsgUniqueConstraint, new[] { "Type" });
            }

            return null;
        }

        #endregion
    }
}