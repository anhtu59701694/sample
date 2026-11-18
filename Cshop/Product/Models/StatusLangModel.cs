using System.ComponentModel.DataAnnotations;
using Claw.Core.DAL;
using Claw.Modules.Eshop.DAL;
using System.Web.Mvc;

namespace Claw.Modules.Eshop.Models
{
    [CustomValidation(typeof(StatusLangModel), "Validate")]
    public class StatusLangModel : ILanguage2
    {
        public StatusLangModel()
        {
            Visible = true;
        }

        public int Id { get; set; }

        [Required(ErrorMessage = StringTable.ErrMsgRequiredField)]
        [StringLength(50, ErrorMessage = StringTable.ErrMsgLengthLimit)]
        [Remote("ValidateName", "Status", AdditionalFields = "Id,StatusId", HttpMethod = "POST", ErrorMessage = StringTable.ErrMsgUniqueConstraint)]

        [Display(Name = "Status name")]
        public string Name { get; set; }

        [Required(ErrorMessage = StringTable.ErrMsgRequiredField)]
        [StringLength(10, ErrorMessage = StringTable.ErrMsgLengthLimit)]
        public string LangCode { get; set; }

        public bool Visible { get; set; }

        public int StatusId { get; set; }

        #region IsDataModelEmpty
        public bool IsDataModelEmpty
        {
            get
            {
                return string.IsNullOrWhiteSpace(Name);
            }
        }
        #endregion

        #region Validate
        public static ValidationResult Validate(StatusLangModel obj, ValidationContext cxt)
        {
            if (obj == null) return null;

            if (BaseMgr.Exists<Domain.StatusLang>(m => m.Id, obj.Id, m => m.Status.Id != obj.StatusId,
                new KeyValuePair() { Key = "Name", Value = obj.Name }))
            {
                return new ValidationResult(StringTable.ErrMsgUniqueConstraint, new[] { "Name" });
            }

            return null;
        }
        #endregion
    }
}