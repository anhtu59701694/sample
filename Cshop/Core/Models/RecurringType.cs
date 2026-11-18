using System.ComponentModel.DataAnnotations;

namespace Claw.Modules.Eshop.Models
{
    public enum RecurringType
    {
        Daily = 1,
        Weekly = 2,
        Monthly = 3,
        Bimonthly = 5,
        Trimonthly = 6,
        Quadmonthly = 8,
        Semiyearly = 7,
        Yearly = 4
    }
}