using Claw.Modules.Eshop.Domain;
using System.ComponentModel.DataAnnotations;

namespace Claw.Modules.Eshop.Models
{
    public class ProductBundleGroupModel : BaseExModel<ProductBundleGroup>
    {
        public string Name { get; set; }

        [Display(Name = "Min price")]
        public OMoney MinPrice { get; set; }

        public int? Accessories { get; set; }

        public int? ProductId { get; set; }

        [Display(Name = "Show variant")]
        public bool ShowVariant { get; set; }
    }
}