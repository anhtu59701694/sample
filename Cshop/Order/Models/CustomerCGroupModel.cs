using System.ComponentModel.DataAnnotations;

namespace Claw.Modules.Eshop.Models
{
    public class CustomerCGroupModel
    {
        public int Id { get; set; }

        public int? CustomerId { get; set; }

        public int? GroupId { get; set; }

        public bool Selected { get; set; }

        [Display(Name = "Name")]
        public string GroupName { get; set; }

        public bool IsDataModelEmpty
        {
            get { return !Selected || (GroupId ?? 0) == 0; }
        }
    }
}