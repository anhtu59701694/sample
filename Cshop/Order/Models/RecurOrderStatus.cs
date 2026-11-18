using System.ComponentModel.DataAnnotations;

namespace Claw.Modules.Eshop.Models
{
    public enum RecurOrderStatus : byte
    {
        Pending = 0, 
        Active = 1, 
        [Display(Name = "Canceled")]
        Inactive = 2,
        Deleted = 3
    }
}