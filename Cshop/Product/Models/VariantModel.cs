using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Dynamic;
using System.Linq;
using System.Web.Routing;
using Claw.Core;
using Claw.Core.DAL;
using Claw.Core.Domain;
using Claw.Modules.Eshop.Controllers.Admin;
using Claw.Modules.Eshop.Domain;
using Claw.Modules.Eshop.DAL;
using System.Web.Mvc;
using AutoMapper;

namespace Claw.Modules.Eshop.Models
{
    [CustomValidation(typeof(VariantModel), "Validate")]
    public class VariantModel : BaseModel<VariantBase, VariantLangModel>
    {
        public const int MaxTotal = 81;
        public const int MaxPerAdd = 10;
        public const int MaxProps = 3;
        public static int MaxImages = 150;

        public VariantModel()
        {
            _raw = new Variant();
        }

        [Remote("ValidateSku", "Variant", AdditionalFields = "Id,ProductId", HttpMethod = "POST", ErrorMessage = StringTable.ErrMsgUniqueAliasConstraint)]
        [StringLength(50, ErrorMessage = StringTable.ErrMsgLengthLimit)]
        public string SKU { get; set; }

        [Remote("ValidateBarcode", "Variant", AdditionalFields = "Id", HttpMethod = "POST", ErrorMessage = StringTable.ErrMsgUniqueAliasConstraint)]
        [StringLength(50, ErrorMessage = StringTable.ErrMsgLengthLimit)]
        [Display(Name = "Barcode")]
        public string BarCode { get; set; }

        //[StringLength(50, ErrorMessage = StringTable.ErrMsgLengthLimit)]
        //[Display(Name = "HS customs code")]
        //public string NvitCode { get; set; }

        [Required(ErrorMessage = StringTable.ErrMsgRequiredField)]
        [Range(0, int.MaxValue, ErrorMessage = StringTable.ErrMsgMinLimit)]
        [Display(Name = "Quantity")]
        public int Quantity { get; set; }


        [Required(ErrorMessage = StringTable.ErrMsgRequiredField)]
        [Range(0, int.MaxValue, ErrorMessage = StringTable.ErrMsgMinLimit)]
        [Display(Name = "Quantity1")]
        public int Quantity1 { get; set; }


        [Required(ErrorMessage = StringTable.ErrMsgRequiredField)]
        [Range(0, int.MaxValue, ErrorMessage = StringTable.ErrMsgMinLimit)]
        [Display(Name = "Quantity2")]
        public int Quantity2 { get; set; }


        [Display(Name = "Backorder quantity")]
        public int BackOrderQty { get; set; }

        public bool? AllowBackorder { get; set; }

        [Display(Name = "in order")]
        public int? ShoppingQty { get; set; }

        public string Image { get; set; }

        [Display(Name = "Image")]
        public string ImageThumb { get; set; }

        public string ImageFsUrl { get; set; }

        [Exportable(Name = "Image")]
        public string ImageExpList { get; set; }

        /// <summary>
        /// of variant or product
        /// </summary>
        public ImageModel CombinedImage { get; set; }

        [Required(ErrorMessage = StringTable.ErrMsgRequiredField)]
        [Range(0, 9999, ErrorMessage = StringTable.ErrMsgRangeLimit)]
        [Display(Name = "Ordering")]
        public int DisplayOrder { get; set; }

        [Required(ErrorMessage = StringTable.ErrMsgRequiredField)]
        [Display(Name = "Status")]
        public int? ProductStatusId { get; set; }

        public string ProductStatusName { get; set; }

        [Required(ErrorMessage = StringTable.ErrMsgRequiredField)]
        [Range(typeof(OMoney), "0", StringTable.MaxOMoney, ErrorMessage = StringTable.ErrMsgRangeLimit)]
        [Display(Name = "Price")]
        public OMoney Price { get; set; }
                           
        [Required(ErrorMessage = StringTable.ErrMsgRequiredField)]
        [Display(Name = "Price (VAT)")]
        [Range(typeof(OMoney), "0", StringTable.MaxOMoney, ErrorMessage = StringTable.ErrMsgRangeLimit)]
        public OMoney PriceInc { get; set; }

        [Display(Name = "Price")]
        public OMoney CombinedPrice { get { return _raw.CombinedPrice(); } }

        [Range(typeof(OMoney), "0", StringTable.MaxOMoney, ErrorMessage = StringTable.ErrMsgRangeLimit)]
        [Display(Name = "Sale price")]
        public OMoney SalePrice { get; set; }

        [Display(Name = "Sale price")]
        public OMoney CombinedSalePrice { get { return _raw.CombinedSalePrice(); } }

        [Range(typeof(OMoney), "0", StringTable.MaxOMoney, ErrorMessage = StringTable.ErrMsgRangeLimit)]
        [Display(Name = "Sale price (VAT)")]
        public OMoney SalePriceInc { get; set; }

        [Display(Name = "Sale price (VAT)")]
        public OMoney CombinedSalePriceInc { get { return _raw.CombinedSalePriceInc(); } }

        [Range(typeof(OMoney), "0", StringTable.MaxOMoney, ErrorMessage = StringTable.ErrMsgRangeLimit)]
        [Display(Name = "Cost price")]
        public OMoney CostPrice { get; set; }

        [Range(typeof(OMoney), "0", StringTable.MaxOMoney, ErrorMessage = StringTable.ErrMsgRangeLimit)]
        [Display(Name = "Cost price (VAT)")]
        public OMoney CostPriceInc { get; set; }

        [Range(typeof(OFloat), "0", StringTable.MaxOFloat, ErrorMessage = StringTable.ErrMsgRangeLimit)]
        public OFloat Weight { get; set; }

        public byte? ProductWUnit { get { return _raw.Product.SafeProp(m => m.WUnit); } }

        [Display(Name = "Weight")]
        public OFloat CombinedWeight { get; set; }

        public OMoney ProdVat { get; set; }

        [Display(Name = "Visibility")]
        public bool? Active { get; set; }


        public VisibilityType ProductVisibility { get; set; }

        #region VisibleWeb
        [Exportable(Name = "Visible on website: yes/no")]
        [Display(Name = "On webshop")]
        public bool ProductVisibleWeb
        {
            get { return 0 != (ProductVisibility & VisibilityType.Web); }
        }
        #endregion

        #region ProductVisiblePos
        public bool ProductVisiblePos
        {
            get { return 0 != (ProductVisibility & VisibilityType.Pos); }
        }
        #endregion


        public bool? Deletable { get; set; }

        public int ProductId { get; set; }

        public string ProductName { get; set; }

        public int? ProductShippingType { get; set; }

        public OFloat ProductWeight { get; set; }

        [Display(Name = "Location")]
        [StringLength(200, ErrorMessage = StringTable.ErrMsgLengthLimit)]
        public string Location { get; set; }

        [Display(Name = "Back stock date")]
        public ODateTime BackDate { get; set; }

        [Display(Name = "Price check url")]
        [StringLength(150, ErrorMessage = StringTable.ErrMsgLengthLimit)]
        [Url(ErrorMessage = StringTable.ErrMsgInvalidUrl)]
        public string PriceCkUrl { get; set; }

        [Display(Name = "Price check min (incl. VAT)")]
        [Range(typeof(OMoney), "0", StringTable.MaxOMoney, ErrorMessage = StringTable.ErrMsgRangeLimit)]
        public OMoney PriceCkMinVal { get; set; }

        [Display(Name = "Email marketings")]
        public int? EmailMUsers { get; set; }

        public int? CGroupDisc3Id { get; set; }
        public decimal? CGroupDisc3Rate { get; set; }

        /// <summary>
        /// product name + propdesc
        /// </summary>
        public string VariantName { get; set; }

        public List<VariantPropValModel> Properties { get; set; }

        public List<VariantImageModel> Images { get; set; }
      
        [Display(Name = "Description")]
        public string PropDesc { get; set; }

        public int Iseq { get; set; }

        public string Key { get { return Id == 0 ? "nil_" + Iseq : Id.ToString(); } }

        #region InitProperties
        public void InitProperties(IDB db, List<PropertyModel> properties, bool mergeable = false)
        {
            if (Properties == null) Properties = new List<VariantPropValModel>();
            _raw.GetVariantPropVals(Properties, properties, mergeable);

            if (mergeable)
            {
                Properties.Action(x =>
                {
                    var propVal = PropValMgr.GetDom(db, x.PropValId ?? 0);
                    x.Property = properties.FirstOrDefault(m => m.Id == x.PropId);
                    x.PropValName = propVal == null ? null : propVal.GetName();
                });
            }

            if (Properties.Count > 1)
                Properties = Properties.OrderBy(x => x.Property.DisplayOrder).ThenBy(x => x.PropId).ToList();
        } 
        #endregion

        #region InitImages
        public void InitImages()
        {
            Images = _raw.GetImages();
        }
        #endregion

        #region IsDataModelEmpty
        public bool IsDataModelEmpty
        {
            get
            {
                return string.IsNullOrWhiteSpace(SKU) && string.IsNullOrWhiteSpace(BarCode) &&
                       string.IsNullOrWhiteSpace(Image)
                       && ProductStatusId == null && Price == null && CostPrice == null && SalePrice == null
                       && Weight == null && Active == null;
            }
        }
        #endregion

        #region Validate

        public static ValidationResult Validate(VariantModel obj, ValidationContext cxt)
        {
            return ValidateEx(obj, cxt, null);
        }

        public static ValidationResult ValidateEx(VariantModel obj, ValidationContext cxt, ProductModel proMod)
        {
            ValidationResult ret;
            if (obj == null) return null;

            if (cxt != null && obj.SalePrice == 0) obj.SalePrice = null;

            if (cxt != null && obj.SalePriceInc == 0) obj.SalePriceInc = null;

            if (cxt != null && null != (ret = ValidatePrices(obj))) return ret;

            var lstOthers = proMod != null && proMod.Variants != null
                ? proMod.Variants.Where(x => x != null && x.Iseq != obj.Iseq).ToList()
                : new List<VariantModel>();
            var supplierId = Helper.QParam.GetField<int?>("SupplierId", false);
            var skuSupplier = Helper.SettingsBiz.GetSettings().Hidden.SkuUniqueBySupplier;

            if (!obj.SKU.IsNullOrEmpty() && (lstOthers.Any(x => x.SKU.EqualsIgCase(obj.SKU)) ||
                BaseMgr.Exists<Domain.Product>(m => m.Id, 0, m => !skuSupplier || m.Supplier.GetId().EqualsSafe(supplierId), 
                    new KeyValuePair() { Key = "SKU", Value = obj.SKU }) ||
                BaseMgr.Exists<Variant>(m => m.Id, obj.Id, m => !skuSupplier || m.Product.Supplier.GetId().EqualsSafe(supplierId), 
                    new KeyValuePair() { Key = "SKU", Value = obj.SKU })))
            {
                return new ValidationResult(StringTable.ErrMsgUniqueAliasConstraint, new[] { "SKU" });
            }

            if (!obj.BarCode.IsNullOrEmpty() && (lstOthers.Any(x => x.BarCode.EqualsIgCase(obj.BarCode)) ||
                BaseMgr.Exists<Domain.Product>(m => m.Id, 0, null, new KeyValuePair() { Key = "BarCode", Value = obj.BarCode }) ||
                BaseMgr.Exists<Variant>(m => m.Id, obj.Id, null, new KeyValuePair() { Key = "BarCode", Value = obj.BarCode })))
            {
                return new ValidationResult(StringTable.ErrMsgUniqueAliasConstraint, new[] { "BarCode" });
            }

            return null;
        } 
        #endregion

        #region ValidatePrices
        public static ValidationResult ValidatePrices(VariantModel obj)
        {
            //if (obj.SalePrice > 0 && obj.Price <= obj.SalePrice)
            //    return new ValidationResult(Product.StringTable.ErrPriceMustGreaterSalePrice, new[] { "SalePriceInc" });

            //if (obj.SalePrice > 0 && obj.CostPrice > obj.SalePrice)
            //    return new ValidationResult(StringTable.ErrCostPriceMustLessOrEqSalePrice, new[] { "SalePriceInc" });

            //if (!obj.CostPrice.IsNull() && obj.CostPrice > obj.Price)
            //    return new ValidationResult(StringTable.ErrCostPriceMustLessOrEqPrice, new[] { "PriceInc" });

            return null;
        }
        #endregion
    }
}