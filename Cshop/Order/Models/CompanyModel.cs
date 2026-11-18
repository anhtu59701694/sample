using System.ComponentModel.DataAnnotations;

namespace Claw.Modules.Eshop.Models
{
    public class CompanyModel : BaseExModel<Domain.Company>
    {
        public string Name { get; set; }

        [Display(Name = "Org#")]
        public string OrgNumber { get; set; }

        public string Data { get; set; }

        public int? OwnerId { get; set; }

        [Display(Name = "Created date")]
        public ODateTime CreatedDate { get; set; }

        [Display(Name = "Users")]
        public int? UserNum { get; set; }
    }
}