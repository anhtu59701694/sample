using System.ComponentModel.DataAnnotations;
using System.Web.Script.Serialization;

namespace Claw.Modules.Eshop.Models
{
    public class CategoryProductModel
    {
        public int Id { get; set; }

        public int CategoryId { get; set; }

        public int? ProductId { get; set; }

        [Display(Name = "Name")]
        public string ProductName { get; set; }
   
        public int? ProductTotalSales { get; set; }

        public OMoney ProductPrice { get; set; }

        [Display(Name = "Price (incl. VAT)")]
        public OMoney ProductPriceInc { get; set; }

        [ScriptIgnore(ApplyToOverrides = true)]
        [Display(Name = "New price (incl. VAT)")]
        public OMoney ProductNewPriceInc { get; set; }

        public OMoney ProductSalePrice { get; set; }

        [Display(Name = "Sale price (incl. VAT)")]
        public OMoney ProductSalePriceInc { get; set; }

        public OMoney ProductCombinedSalePrice { get; set; }

        public OMoney ProductCombinedSalePriceInc { get; set; }

        [ScriptIgnore(ApplyToOverrides = true)]
        public OMoney ProductCostPrice { get; set; }

        [Display(Name = "Cost price (incl. VAT)")]
        public OMoney ProductCostPriceInc { get; set; }

        public OFloat ProductVat { get; set; }

        public ODateTime ProductPublishedDate { get; set; }
        public ODateTime ProductModifiedDate { get; set; }
        public ODateTime ProductCreatedDate { get; set; }

        [Display(Name = "Image")]
        public ImageModel ProductImage { get; set; }

        [ScriptIgnore(ApplyToOverrides = true)]
        [Display(Name = "SKU")]
        public string ProductSKU { get; set; }

        [Display(Name = "Sort order")]
        public int DisplayOrder { get; set; }

        public string Key { get { return ProductId.ToStringSafe(); } }

        #region IsDataModelEmpty

        [ScriptIgnore(ApplyToOverrides = true)]
        public bool IsDataModelEmpty
        {
            get { return (ProductId ?? 0) == 0; }
        }

        #endregion
    }
}