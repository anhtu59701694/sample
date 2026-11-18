using System.ComponentModel.DataAnnotations;

namespace Claw.Modules.Eshop.Models
{
    public enum PermissionMask : byte
    {
        [Display(Name = "View")] View = 1,
        [Display(Name = "Manage")] Edit = 2,
    }
}