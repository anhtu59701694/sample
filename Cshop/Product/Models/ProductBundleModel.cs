using System;
using System.ComponentModel.DataAnnotations;
using Claw.Modules.Eshop.DAL;
using Claw.Modules.Eshop.Domain;
using Claw.Modules.Eshop.Helper;

namespace Claw.Modules.Eshop.Models
{
    [CustomValidation(typeof(ProductBundleModel), "Validate")]
    public class ProductBundleModel
    {
        public int Id { get; set; }

        public int Iseq { get; set; }

        [Required(ErrorMessage = StringTable.ErrMsgRequiredField)]
        [Range(1, int.MaxValue, ErrorMessage = StringTable.ErrMsgMinLimit)]
        public int Quantity { get; set; }

        public int? ProductQty { get; set; }

        public bool? ProductAllowBackorder { get; set; }

        public int? BundleId { get; set; }

        public int? ProductId { get; set; }

        public int? VariantId { get; set; }

        public string ProductName { get; set; }

        public ImageModel ProductImage { get; set; }

        [Display(Name = "Description")]
        public string PropDesc { get; set; }

        [Display(Name = "SKU")]
        public string ProductSKU { get; set; }


        public OMoney ProductCostPrice { get; set; }

        public OMoney ProductCostPriceInc { get; set; }

        public OMoney ProductPrice { get; set; }

        [Display(Name = "Price (incl. VAT)")]
        public OMoney ProductPriceInc { get; set; }

        public OMoney ProductSalePrice { get; set; }

        [Display(Name = "Sale price (incl. VAT)")]
        public OMoney ProductSalePriceInc { get; set; }

        public OFloat ProductVat { get; set; }

        public int? ProductVariantNum { get; set; }

        public OFloat Weight { get; set; }

        public byte? WUnit { get; set; }

        public string Key { get { return (ProductId ?? 0) + "-" + (VariantId ?? 0); } }

        #region IsDataModelEmpty

        public bool IsDataModelEmpty
        {
            get { return (ProductId ?? 0) == 0; }
        }

        #endregion
        
        #region Validate
        public static ValidationResult Validate(ProductBundleModel obj, ValidationContext cxt)
        {
            var db = new DefaultDb();
            var quantity = QParam.GetField<int?>("Quantity");
            var product = ProductMgr.GetDom(db, obj == null ? 0 : (obj.ProductId ?? 0));

            if (obj == null || product == null || quantity < 1) return null;

            /*var bundle = BaseMgr<ProductBundleModel, ProductBundle>.Get(db, obj.Id);
            var maxQty = bundle == null
                ? product.Quantity
                : (bundle.ProductAllowBackorder == true ? 10000 : bundle.ProductQty);

            if (quantity*obj.Quantity > maxQty)
                return new ValidationResult(
                    maxQty < quantity
                        ? Product.StringTable.ErrMsgNotEnoughQuantity
                        : string.Format(StringTable.ErrMsgRangeLimit.ModErrLocalize(), "", 1, maxQty/quantity),
                    new[] {"Quantity"});*/

            return null;
        }
        #endregion

    }
}