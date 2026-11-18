using System.ComponentModel.DataAnnotations;

namespace Claw.Modules.Eshop.Models
{
    public class RecurOrderLogModel
    {
        public int Id { get; set; }

        public int? RecurringId { get; set; }

        [Display(Name = "Changes")]
        public string Content { get; set; }

        [Display(Name = "Date")]
        public ODateTime CreatedDate { get; set; }

        public string AuditInfo { get; set; }
    }
}