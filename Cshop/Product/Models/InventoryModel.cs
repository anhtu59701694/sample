using System.ComponentModel.DataAnnotations;

namespace Claw.Modules.Eshop.Models
{
    public enum InventoryType
    {
        [Display(Name = "Inventory report")] Checkpoint = 0,
        [Display(Name = "Inventory report with changes")] Update = 1
    }

    public class InventoryModel
    {
        public int Id { get; set; }

        public int Type { get; set; }

        public string TypeTxt { get { return typeof (InventoryType).LocalizeEnumValue(Type); } }

        public int UserId { get; set; }

        [Display(Name = "User name")]
        public string UserName { get; set; }

        [Display(Name = "Created date")]
        public ODateTime CreatedDate { get; set; }
    }
}