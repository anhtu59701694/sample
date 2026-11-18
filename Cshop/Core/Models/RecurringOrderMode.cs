using System.ComponentModel.DataAnnotations;

namespace Claw.Modules.Eshop.Models
{
    public enum RecurringOrderMode
    {
        None = 0,
        Global = 1,
        [Display(Name = "Specific products")]
        SpecificProducts = 2
    }
}