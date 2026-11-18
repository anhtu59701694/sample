using System.ComponentModel.DataAnnotations;

namespace Claw.Modules.Eshop.Models
{
    public class UserModel
    {
        public int Id { get; set; }

        public bool InlineEdit { get; set; }

        public bool QuickAdd { get; set; }

        [Display(Name = "User name")]
        public string Username { get; set; }

        [Display(Name = "Full name")]
        public string FullName { get; set; }

        [Display(Name = "Email")]
        public string Email { get; set; }

        public string Roles { get; set; }

        [Display(Name = "Activated")]
        public bool Active { get; set; }

        [Display(Name = "Created date")]
        public ODateTime CreatedDate { get; set; }
    }
}