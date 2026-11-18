using Claw.Core.DAL;
using Claw.Modules.Eshop.DAL;
using System.ComponentModel.DataAnnotations;
using System.Web.Mvc;

namespace Claw.Modules.Eshop.Models
{
    [CustomValidation(typeof(EmailTemplLangModel), "Validate")]
    public class EmailTemplLangModel : ILanguage2
    {
        public EmailTemplLangModel()
        {
            Visible = true;
        }

        public int Id { get; set; }

        [Remote("ValidateSubject", "EmailTempl", AdditionalFields = "Id,EmailTemplId", HttpMethod = "POST", ErrorMessage = StringTable.ErrMsgUniqueConstraint)]
        [Required(ErrorMessage = StringTable.ErrMsgRequiredField)]
        [StringLength(100, ErrorMessage = StringTable.ErrMsgLengthLimit)]
        [Display(Name = "Template subject")]
        public string Subject { get; set; }

        [Required(ErrorMessage = StringTable.ErrMsgRequiredField)]
        [Display(Name = "Template content")]
        public string Body { get; set; }

        [Required(ErrorMessage = StringTable.ErrMsgRequiredField)]
        [StringLength(10, ErrorMessage = StringTable.ErrMsgLengthLimit)]
        public string LangCode { get; set; }

        public bool Visible { get; set; }

        public int EmailTemplId { get; set; }

        #region IsDataModelEmpty

        public bool IsDataModelEmpty
        {
            get { return string.IsNullOrWhiteSpace(Subject) && string.IsNullOrWhiteSpace(Body); }
        }

        #endregion

        #region Validate
        public static ValidationResult Validate(EmailTemplLangModel obj, ValidationContext cxt)
        {
            //if (obj == null) return null;
            //return null;
            if (obj == null) return null;

            if (BaseMgr.Exists<Domain.EmailTemplLang>(m => m.Id, obj.Id, m => m.EmailTempl.Id != obj.EmailTemplId,
                new KeyValuePair() { Key = "Subject", Value = obj.Subject }))
            {
                return new ValidationResult(StringTable.ErrMsgUniqueConstraint, new[] { "Subject" });
            }

            return null;
        }
        #endregion
    }
}