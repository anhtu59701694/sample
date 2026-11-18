using System.ComponentModel.DataAnnotations;

namespace Claw.Modules.Eshop.Models
{
    public class ProductAccessoryModel
    {
        public int Id { get; set; }

        public int? ProductId { get; set; }

        public int? BundleGroupId { get; set; }

        [Display(Name = "Group")]
        public string BundleGroupName { get; set; }

        public int? AccessoryId { get; set; }

        [Display(Name = "Name")]
        public string AccessoryName { get; set; }

        [Display(Name = "Price")]
        public OMoney AccessoryPrice { get; set; }

        [Display(Name = "Price (incl. VAT)")]
        public OMoney AccessoryPriceInc { get; set; }

        public OMoney AccessorySalePrice { get; set; }

        [Display(Name = "Sale price (incl. VAT)")]
        public OMoney AccessorySalePriceInc { get; set; }

        [Display(Name = "Image")]
        public ImageModel AccessoryImage { get; set; }

        public int? AccessoryVariantNum { get; set; }

        public int? AcceVariantId { get; set; }

        public string Key { get { return string.Format("{0}-{1}", AccessoryId, AcceVariantId ?? 0); } }

        #region IsDataModelEmpty

        public bool IsDataModelEmpty
        {
            get { return (AccessoryId ?? 0) == 0; }
        }

        #endregion
    }
}