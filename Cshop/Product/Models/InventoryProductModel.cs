using System.ComponentModel.DataAnnotations;

namespace Claw.Modules.Eshop.Models
{
    public class InventoryProductModel
    {
        public int Id { get; set; }

        public int? InventoryId { get; set; }

        [Display(Name = "Product id")]
        public int? ProductId { get; set; }

        [Display(Name = "Variant id")]
        public int? VariantId { get; set; }

        [Display(Name = "Variant sku")]
        public string VariantSku { get; set; }

        [Display(Name = "Variant name")]
        public string VariantName { get; set; }

        [Display(Name = "Product sku")]
        public string ProductSku { get; set; }

        [Display(Name = "Product name")]
        public string ProductName { get; set; }

        [Display(Name = "Cost price")]
        public OMoney CostPrice { get; set; }

        public int Quantity { get; set; }
        
        public OMoney Total { get { return CostPrice*Quantity; }}

        [Display(Name = "Old quantity")]
        public int? OldQuantity { get; set; }

        [Display(Name = "New quantity")]
        public int NewQuantity { get { return Quantity; } }
        
        [Display(Name = "Different quantity")]
        public int? DiffQuantity { get { return Quantity - OldQuantity; } }

        [Display(Name = "Product status")]
        public string ProductStatusTxt { get; set; }

        public byte Visibility { get; set; }

        [Display(Name = "On webshop")]
        public bool VisibleWeb
        {
            get { return 0 != (Visibility & (byte) VisibilityType.Web); }
        }

        [Display(Name = "On pos")]
        public bool VisiblePos
        {
            get { return 0 != (Visibility & (byte) VisibilityType.Pos); }
        }

        public string Location { get; set; }

        [Display(Name = "Old location")]
        public string OldLocation { get; set; }

        [Display(Name = "New location")]
        public string NewLocation { get { return Location; } }
    }
}