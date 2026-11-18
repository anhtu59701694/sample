using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Claw.Core;
using Claw.Core.DAL;
using Claw.Core.Domain;
using Claw.Modules.Eshop.Controllers.Admin;
using Claw.Modules.Eshop.Domain;
using Claw.Modules.Eshop.DAL;
using System.Web.Mvc;
using AutoMapper;
using System.Web.Helpers;
using System.Web.Script.Serialization;
using Claw.Modules.Eshop.Domain.Interfaces;
using Claw.Modules.Eshop.DAL.Interfaces;
using Claw.Modules.Eshop.Helper;

namespace Claw.Modules.Eshop.Models
{
    [CustomValidation(typeof(ProductModel), "Validate")]
    public class ProductModel : BaseModel<Domain.ProductBase, ProductLangModel>, IProduct2
    {
        #region EditQty
        public class EditQty
        {
            [Required(ErrorMessage = StringTable.ErrMsgRequiredField)]
            [Range(0, int.MaxValue, ErrorMessage = StringTable.ErrMsgMinLimit)]
            [RegularExpression(@"^\d+$", ErrorMessage = StringTable.ErrMsgRequireIntegerNumber)]
            [Display(Name = "Legg til")]
            public int? QtyAdd { get; set; }

            [Required(ErrorMessage = StringTable.ErrMsgRequiredField)]
            [Range(0, int.MaxValue, ErrorMessage = StringTable.ErrMsgMinLimit)]
            [RegularExpression(@"^\d+$", ErrorMessage = StringTable.ErrMsgRequireIntegerNumber)]
            [Display(Name = "Ta bort ")]
            public int? QtyRemove { get; set; }

            [Required(ErrorMessage = StringTable.ErrMsgRequiredField)]
            [Display(Name = "Grunn")]
            public int? Reason { get; set; }

            [Display(Name = "Buying price (excl. VAT)")]
            [Range(typeof(OMoney), "0", StringTable.MaxOMoney, ErrorMessage = StringTable.ErrMsgRangeLimit)]
            public OMoney CostPrice { get; set; }

            [StringLength(200, ErrorMessage = StringTable.ErrMsgLengthLimit)]
            [Display(Name = "Memo")]
            public string Note { get; set; }
        } 
        #endregion
       
        #region AdvSearchModel

        [CustomValidation(typeof(AdvSearchModel), "Validate")]
        public class AdvSearchModel
        {
            [StringLength(50, ErrorMessage = StringTable.ErrMsgLengthLimit)]
            [Display(Name = "Search by sku")]
            public string Sku { get; set; }

            [StringLength(50, ErrorMessage = StringTable.ErrMsgLengthLimit)]
            [Display(Name = "Search by barcode")]
            public string BarCode { get; set; }

            [StringLength(400, ErrorMessage = StringTable.ErrMsgLengthLimit)]
            [Display(Name = "Search by product name")]
            public string ProductName { get; set; }

            [StringLength(200, ErrorMessage = StringTable.ErrMsgLengthLimit)]
            [Display(Name = "Search by location")]
            public string Location { get; set; }

            [Display(Name = "Search by category")]
            public int? CategoryId { get; set; }

            [Display(Name = "Search by manufacturer")]
            public int? ManufacturerId { get; set; }

            [Display(Name = "Search by products visible/hidden")]
            public bool? VisibilityWeb { get; set; }

            [Display(Name = "Discount on quantity")]
            public bool? SelfDiscount { get; set; }

            [Display(Name = "Customer group discount product")]
            public bool? CGroupDiscount { get; set; }

            [Display(Name = "Download product")]
            public bool? Download { get; set; }

            [Display(Name = "Products have quantity only")]
            public bool? HasQtyOnly { get; set; }

            [Display(Name = "Products have no category")]
            public bool? NoCateOnly { get; set; }

            [Display(Name = "Search by products status")]
            public int? Status { get; set; }

            public bool IsSpecial { get { return Status < 0 || CategoryId < 0; } }

            public string SpecialSearchTxt<T>(HtmlHelper<T> htmlHelper, CategoryModel cate, StatusModel status)
            {
                var info = new CategoryModel.StatInfo();

                if (CategoryId < 0)
                    return string.Format("{0}: {1}",
                        htmlHelper.LocalizeName(info, m => m.ProductsSub), cate == null ? "" : cate.Name);

                if (Status < 0)
                    return string.Format(htmlHelper.LocalizeName(info, m => m.StatusStat),
                        status == null ? "" : status.Name, cate == null ? "" : cate.Name);

                return "";
            }

            #region IsDataModelEmpty
            public bool IsDataModelEmpty
            {
                get
                {
                    foreach (var prop in typeof(AdvSearchModel).GetProperties())
                    {
                        if (prop.CanWrite && !(prop.GetValue(this) ?? "").ToString().IsNullOrEmpty())
                            return false;
                    }
                    return true;
                }
            }
            #endregion

            #region Validate
            public static ValidationResult Validate(AdvSearchModel obj, ValidationContext cxt)
            {
                if (obj == null) return null;

                foreach (var prop in typeof(AdvSearchModel).GetProperties()
                    .Where(x => x.CanWrite && x.PropertyType == typeof(string)))
                {
                    var val = (string)prop.GetValue(obj);
                    if (val != null) prop.SetValue(obj, val.UrlDecode());
                }

                return null;
            }
            #endregion
        }

        #endregion

        #region StatInfo
        public class StatInfo
        {

            [Display(Name = "in stock for")]
            public int InStock { get; set; }

            [Display(Name = "variants")]
            public int Variants { get; set; }

            [Display(Name = "in order lines")]
            public int OrderLines { get; set; }
        }
        #endregion

        #region FOpt

        public class FOpt
        {
            [Required(ErrorMessage = StringTable.ErrMsgRequiredField)]
            [StringLength(200, ErrorMessage = StringTable.ErrMsgLengthLimit)]
            [Display(Name = "Manufacturer")]
            public string ManufacturerInput { get; set; }

            [Required(ErrorMessage = StringTable.ErrMsgRequiredField)]
            [StringLength(20, ErrorMessage = StringTable.ErrMsgLengthLimit)]
            [Display(Name = "Product status")]
            public string ProductStatusInput { get; set; }

            [Required(ErrorMessage = StringTable.ErrMsgRequiredField)]
            [Display(Name = "Value")]
            [Range(0, 100, ErrorMessage = StringTable.ErrMsgRangeLimit)]
            public OFloat VatInput { get; set; }

            [Required(ErrorMessage = StringTable.ErrMsgRequiredField)]
            [StringLength(50, ErrorMessage = StringTable.ErrMsgLengthLimit)]
            [Display(Name = "Variant value")]
            public string PropertyNameInput { get; set; }

            [Required(ErrorMessage = StringTable.ErrMsgRequiredField)]
            [StringLength(50, ErrorMessage = StringTable.ErrMsgLengthLimit)]
            [Display(Name = "Variant")]
            public string PropValNameInput { get; set; }
        }

        #endregion

        public static int MaxImages = 150;
        private FOpt _opt = new FOpt();
        private string _cparentIds;
        private string _categoryIds;
        private string _categoryJsons;

        public ProductModel()
        {
            Visibility = VisibilityType.Web;
            PublishedDate = DateTime.Now;
            Type = ProductType.Normal;
            SalePrice = new OMoney();
            BackDate = null;
            DisplayOrder = 1000;
            ShippingCostExtra = 0m;
            VisibleSku = true;
        }

        [ScriptIgnore(ApplyToOverrides = true)]
        public FOpt Opt { get { return _opt; } }

        #region Alias

        public string Alias
        {
            get { return GetLang(m => m.Alias); }
        }

        public void SetAlias(string value)
        {
            SetLang(m => m.Alias = value, () => new ProductLangModel() { LangCode = LanguageMgr.FocusLangCode });
        }

        #endregion

        public ProductType Type { get; set; }

        [Exportable(Name = "Product sku")]
        [Remote("ValidateSku", "Product", AdditionalFields = "Id,SupplierId", HttpMethod = "POST", ErrorMessage = StringTable.ErrMsgUniqueAliasConstraint)]
        [StringLength(50, ErrorMessage = StringTable.ErrMsgLengthLimit)]
        public string SKU { get; set; }

        [Exportable(Name = "Barcode")]
        [Remote("ValidateBarcode", "Product", AdditionalFields = "Id", HttpMethod = "POST", ErrorMessage = StringTable.ErrMsgUniqueAliasConstraint)]
        [StringLength(50, MinimumLength = 4, ErrorMessage = StringTable.ErrMsgLengthRangeLimit)]
        [Display(Name = "Barcode")]
        public string BarCode { get; set; }

        [StringLength(50, ErrorMessage = StringTable.ErrMsgLengthLimit)]
        [Display(Name = "HS customs number")] 
        public string NvitCode { get; set; }

        [StringLength(32767, ErrorMessage = StringTable.ErrMsgLengthLimit)]
        public string Materials { get; set; }

        [Display(Name = "Origin country")]
        public int? OriginCountryId { get; set; }

        [Exportable(Name = "Quantity")]
        [Required(ErrorMessage = StringTable.ErrMsgRequiredField)]
        [Range(0, int.MaxValue, ErrorMessage = StringTable.ErrMsgMinLimit)]
        [Display(Name = "Quantity in stock")]
        public int Quantity { get; set; }


        [Exportable(Name = "Quantity1")]
        [Required(ErrorMessage = StringTable.ErrMsgRequiredField)]
        [Range(0, int.MaxValue, ErrorMessage = StringTable.ErrMsgMinLimit)]
        [Display(Name = "Quantity1 in stock")]
        public int Quantity1 { get; set; }


        [Exportable(Name = "Quantity2")]
        [Required(ErrorMessage = StringTable.ErrMsgRequiredField)]
        [Range(0, int.MaxValue, ErrorMessage = StringTable.ErrMsgMinLimit)]
        [Display(Name = "Quantity2 in stock")]
        public int Quantity2 { get; set; }


        [Display(Name = "Backorder quantity")]
        public int BackOrderQty { get; set; }

        [Display(Name = "in order")]
        public int? ShoppingQty { get; set; }

        [Display(Name = "Total quantity in orders")]
        public int? QtyInOrders { get; set; }

        [Display(Name = "Orders")]
        public int? Orders { get; set; }

        [Exportable(true, false, Name = "Tax")]
        [Required(ErrorMessage = StringTable.ErrMsgRequiredField)]
        [Display(Name = "Tax")]
        public int? VatId { get; set; }

        [Display(Name = "Tax")]
        public OFloat Vat { get; set; }

        [Exportable(false, Name = "Tax")]
        [ScriptIgnore(ApplyToOverrides = true)]
        public int? VatExp { get { return Vat == null ? null : (int?)(Vat.Value * 100); } }

        [Display(Name = "value %")]
        public OFloat VatInput { get { return Opt.VatInput; } set { Opt.VatInput = value; } }

        [Display(Name = "Second tax")]
        public int? SecondVatId { get; set; }

        public OFloat SecondVat { get; set; }

        [Exportable(true, false, Name = "Status")]
        [Required(ErrorMessage = StringTable.ErrMsgRequiredField)]
        [Display(Name = "Stock status")]
        public int? ProductStatusId { get; set; }

        [Display(Name = "status name")]
        public string ProductStatusInput { get { return Opt.ProductStatusInput; } set { Opt.ProductStatusInput = value; } }

        [Exportable(false, Name = "Status")]
        [Display(Name = "Status")]
        public string ProductStatusName { get { return _raw.ProductStatus.GetName(); } }

        [Exportable(Name = "Price (exc VAT)")]
        [Required(ErrorMessage = StringTable.ErrMsgRequiredField)]
        [Range(typeof(OMoney), "0", StringTable.MaxOMoney, ErrorMessage = StringTable.ErrMsgRangeLimit)]
        [Display(Name = "Listing price")]
        public OMoney Price { get; set; }

        [Exportable(Name = "List price (incl. VAT)")]
        [Display(Name = "List price")]
        [Range(typeof(OMoney), "0", StringTable.MaxOMoney, ErrorMessage = StringTable.ErrMsgRangeLimit)]
        public OMoney PriceInc { get; set; }

        [Exportable(Name = "Sale price (excl. VAT)")]
        [Range(typeof(OMoney), "0", StringTable.MaxOMoney, ErrorMessage = StringTable.ErrMsgRangeLimit)]
        [Display(Name = "Sale price")]
        public OMoney SalePrice { get; set; }

        /// <summary>
        /// Price or SalePrice 
        /// </summary>
        [Display(Name = "Sale price")]
        public OMoney CombinedSalePrice { get { return _raw.CombinedSalePrice(); } }

        [Exportable(Name = "Sale price (incl. VAT)")]
        [Display(Name = "Sale price")]
        [Range(typeof(OMoney), "0", StringTable.MaxOMoney, ErrorMessage = StringTable.ErrMsgRangeLimit)]
        public OMoney SalePriceInc { get; set; }

        /// <summary>
        /// PriceInc or SalePriceInc 
        /// </summary>
        [Display(Name = "Sale price (incl. VAT)")]
        public OMoney CombinedSalePriceInc { get { return _raw.CombinedSalePriceInc(); } }

        [Display(Name = "Extra custom cost (excl. VAT)")]
        [Range(typeof(OMoney), "0", StringTable.MaxOMoney, ErrorMessage = StringTable.ErrMsgRangeLimit)]
        public OMoney TechSpecCostExtra { get; set; }

        [Exportable(Name = "Buying price (excl. VAT)")]
        [Display(Name = "Buying price")]
        [Range(typeof(OMoney), "0", StringTable.MaxOMoney, ErrorMessage = StringTable.ErrMsgRangeLimit)]
        public OMoney CostPrice { get; set; }

        [Exportable(Name = "Buying price(incl. VAT)")]
        [Range(typeof(OMoney), "0", StringTable.MaxOMoney, ErrorMessage = StringTable.ErrMsgRangeLimit)]
        public OMoney CostPriceInc { get; set; }

        [Exportable(Name = "Weight")]
        [Required(ErrorMessage = StringTable.ErrMsgRequiredField)]
        [Range(typeof(OFloat), "0", StringTable.MaxOFloat, ErrorMessage = StringTable.ErrMsgRangeLimit)]
        public OFloat Weight { get; set; }

        [Required(ErrorMessage = StringTable.ErrMsgRequiredField)]
        public byte? WUnit { get; set; }

        [Range(typeof(OMoney), "0", StringTable.MaxOMoney, ErrorMessage = StringTable.ErrMsgRangeLimit)]
        [Display(Name = "Extra shipping cost (excl. VAT)")]
        public OMoney ShippingCostExtra { get; set; }

        [Range(typeof(OMoney), "0", StringTable.MaxOMoney, ErrorMessage = StringTable.ErrMsgRangeLimit)]
        [Display(Name = "Deposit amount")]
        public OMoney DepositAmount { get; set; }

        /// <summary>Positive: %discount, Negative: %profit</summary>
        [Display(Name = "Bundle discount rate")]
        [Range(typeof(OFloat), "0", "100", ErrorMessage = StringTable.ErrMsgRangeLimit)]
        public OFloat BundleDiscountRate { get; set; }

        /// <summary>false: disc %, true: profit %</summary>
        [Display(Name = "Bundle discount type")]
        public bool BundleDiscProfit { get; set; }

        [Required(ErrorMessage = StringTable.ErrMsgRequiredField)]
        [Display(Name = "Visibility")]
        public VisibilityType Visibility { get; set; }

        #region VisibleWeb
        [Exportable(Name = "Visible on website: yes/no")]
        [Display(Name = "On webshop")]
        public bool VisibleWeb
        {
            get { return 0 != (Visibility & VisibilityType.Web); }
            set
            {
                if (value) Visibility |= VisibilityType.Web;
                else Visibility &= ~VisibilityType.Web;
            }
        }
        #endregion

        #region VisiblePos
        [Exportable(Name = "Visible on POS: yes/no")]
        [Display(Name = "On pos")]
        public bool VisiblePos
        {
            get { return 0 != (Visibility & VisibilityType.Pos); }
            set
            {
                if (value) Visibility |= VisibilityType.Pos;
                else Visibility &= ~VisibilityType.Pos;
            }
        }
        #endregion

        [Required(ErrorMessage = StringTable.ErrMsgRequiredField)]
        public int? ShippingType { get; set; }

        [StringLength(150, ErrorMessage = StringTable.ErrMsgLengthLimit)]
        public string AttachFile { get; set; }

        public string VideoUrls { get; set; }

        [Exportable(Name = "Location")]
        [Display(Name = "Location")]
        [StringLength(200, ErrorMessage = StringTable.ErrMsgLengthLimit)]
        public string Location { get; set; }

        [Display(Name = "Allow backorder")]
        public bool AllowBackorder { get; set; }

        [Display(Name = "Enable email marketing for this product")]
        public bool EmailMarketing { get; set; }

        [Display(Name = "Visible sku")]
        public bool VisibleSku { get; set; }

        [Display(Name = "Register serial nr")]
        public bool RegSerialNumber { get; set; }
                   
        [Display(Name = "Hide bundle items")]
        public bool BundleHideSubItems { get; set; }

        [Display(Name = "Need to select all")]
        public bool BundleNeedSelectAll { get; set; }

        [Display(Name = "Only show products in stock")]
        public bool BundleShowProdInStock { get; set; }

        [Display(Name = "Fetch image/description from products")]
        public bool BundleFetchDescFromProducts { get; set; }

        [Display(Name = "Need to be controlled")]
        public bool Controlled { get; set; }

        [Display(Name = "Backorrder message")]
        public string BackorrderMsg { get; set; }

        [Display(Name = "Minimum quantity allowed in shopcart")]
        [Range(0, int.MaxValue, ErrorMessage = StringTable.ErrMsgMinLimit)]
        public int MinQuantity { get; set; }

        [Display(Name = "Notify for quantity threshold")]
        [Range(0, int.MaxValue, ErrorMessage = StringTable.ErrMsgMinLimit)]
        public int NotifyQtyThreshold { get; set; }

        /// <summary>view only</summary>
        public int Flags { get; set; }

        [Display(Name = "Accessory")]
        public bool IsAccessory { get { return (Flags & (int)ProductFlags.Accessory) != 0; } }

        [Display(Name = "A-assortment")]
        public bool IsAassortment { get; set; }

        [Display(Name = "A-assortment threshold")]
        //[Range(0, int.MaxValue, ErrorMessage = StringTable.ErrMsgMinLimit)]
        public int? AassortmentThreshold { get; set; }

        public bool NoDelete { get { return (Flags & (int)ProductFlags.NoDelete) != 0; } }

        [Display(Name = "Product image")]
        public ImageModel Image { get; set; }

        public string ImageKey { get; set; }

        [Exportable(true, false, Name = "Manufacturer")]
        [Display(Name = "Manufacturer")]
        public int? ManufacturerId { get; set; }

        [Display(Name = "manufacturer name")]
        public string ManufacturerInput { get { return Opt.ManufacturerInput; } set { Opt.ManufacturerInput = value; } }

        [Exportable(false, Name = "Manufacturer name")]
        public string ManufacturerName { get; set; }

        [Display(Name = "Supplier")]
        public int? SupplierId { get; set; }

        public string SupplierName { get; set; }

        public bool? IsBundle { get; set; }

        public int TotalSales { get; set; }

        [Display(Name = "Email marketings")]
        public int? EmailMUsers { get; set; }

        [Display(Name = "Published Date")]
        public ODateTime PublishedDate { get; set; }

        [Display(Name = "Back stock date")]
        public ODateTime BackDate { get; set; }

        [Display(Name = "Created Date")]
        public ODateTime CreatedDate { get; set; }

        [Display(Name = "Modified date")]
        public ODateTime ModifiedDate { get; set; }

        [Required(ErrorMessage = StringTable.ErrMsgRequiredField)]
        [Range(0, 99999, ErrorMessage = StringTable.ErrMsgRangeLimit)]
        [Display(Name = "Sort order")]
        public int DisplayOrder { get; set; }

        [Display(Name = "Enable recurring order")]
        public bool RecurringOrder { get; set; }

        [Display(Name = "property name")]
        public string PropertyNameInput { get { return Opt.PropertyNameInput; } set { Opt.PropertyNameInput = value; } }

        [Display(Name = "value name")]
        public string PropValNameInput { get { return Opt.PropValNameInput; } set { Opt.PropValNameInput = value; } }

        #region Name

        [Required(ErrorMessage = StringTable.ErrMsgRequiredField)]
        [StringLength(400, ErrorMessage = StringTable.ErrMsgLengthLimit)]
        [Display(Name = "Product name")]
        public string Name
        {
            get { return GetLang(m => m.Name); }
        }

        public void SetName(string value)
        {
            SetLang(m => { m.Name = value; }, () => new ProductLangModel() { LangCode = LanguageMgr.FocusLangCode });
        }

        #endregion

        #region LangVisible

        public bool? LangVisible
        {
            get { return GetLang(m => m.Visible); }
        }

        #endregion

        //public bool AnyChild { get { return _raw.AnyChild(); } }
        public int VariantCount { get; set; }

        [Exportable(Name = "Image")]
        public string ImageExpList { get; set; }

        [Exportable(Name = "Category")]
        public string CategoryPath { get; set; }

        public bool? AnyBundleRef { get; set; }

        [Display(Name = "Do not show price")]
        public bool HidePrice { get; set; }

        [Display(Name = "Show price for logged in users only")]
        public bool Price4LoginOnly { get; set; }

        [Display(Name = "Enable")]
        public bool SelfDiscActive { get; set; }

        [Display(Name = "Minimum quantity")]
        [Range(2, int.MaxValue, ErrorMessage = Core.StringTable.ErrMsgMinLimit)]
        [Required(ErrorMessage = StringTable.ErrMsgRequiredField)]
        public int? SelfDiscMinQty { get; set; }

        /// <summary>true: amount, false: pecent</summary>
        [Display(Name = "Type")]
        public bool SelfDiscType { get; set; }

        /// <summary>positive: amount, negative: pecent between 0.0 and 1.0</summary>
        [Display(Name = "Value")]
        [Range(typeof(OMoney), "1", Core.StringTable.MaxOMoney, ErrorMessage = Core.StringTable.ErrMsgMinLimit)]
        [Required(ErrorMessage = StringTable.ErrMsgRequiredField)]
        public OMoney SelfDiscValue { get; set; }

        [Display(Name = "Url")]
        [Required(ErrorMessage = StringTable.ErrMsgRequiredField)]
        [StringLength(150, ErrorMessage = StringTable.ErrMsgLengthLimit)]
        [Url(ErrorMessage = StringTable.ErrMsgInvalidUrl)]
        public string PriceCkUrl { get; set; }

        [Display(Name = "Enable")]
        public bool PriceCkActive { get; set; }

        [Display(Name = "Apply variants")]
        public bool PriceCkVariant { get; set; }

        [Display(Name = "Min value (incl. VAT)")]
        [Range(typeof(OMoney), "0", StringTable.MaxOMoney, ErrorMessage = StringTable.ErrMsgRangeLimit)]
        public OMoney PriceCkMinVal { get; set; }

        [Display(Name = "Last run")]
        public ODateTime PriceCkTimestamp { get; set; }

        [Display(Name = "From date")]
        public ODateTime PriceCkDateFrom { get; set; }

        [Display(Name = "To date")]
        public ODateTime PriceCkDateTo { get; set; }

        [Display(Name = "Skjul produkt ved 0-på lager")]
        public bool HideZeroQtyProd { get; set; }
        
        public List<ImageModel> Images { get; set; }

        public List<VariantModel> Variants { get; set; }

        public List<ProductBundleModel> Bundlings { get; set; }

        public List<ProductAccessoryModel> Accessories { get; set; }

        public List<ProductExtraInformationModel> ExtraInformation { get; set; }

        [Display(Name = "Tech specs")]
        public List<ProductTechSValModel> TechSVals { get; set; }

        [Display(Name = "Recurring discounts")]
        public List<ProductRecurDiscModel> RecurringDiscounts { get; set; }

        [Display(Name = "Customer groups")]
        public List<Price4LoginCustGroupModel> Price4LoginCustGroups { get; set; }

        [Display(Name = "Customer group discounts")]
        public List<ProductCGroupDiscModel> CGroupDiscounts { get; set; }

        [Display(Name = "Serial numbers")]
        public List<ProductSerialModel> SerialNumbers { get; set; }

        public string GetCategoryAlias(Language lang = null) { return _raw.GetCategoryAlias(lang); }

        public List<ImageModel> GetImages(Language lang = null) { return _raw.GetImages(); }

        public string  GetImageThumbFrist(Language lang = null) { return _raw.GetImageThumbFrist; }
                
        #region CParentIds
        public string CParentIds
        {
            get { return _cparentIds; }
        }
        #endregion

        #region CategoryIds
        public string CategoryIds
        {
            get { return _categoryIds; }
            set { _categoryIds = value; }
        }
        #endregion

        #region CategoryJsons
        public string CategoryJsons
        {
            get { return _categoryJsons; }
            set { _categoryJsons = value; }
        }
        #endregion

        #region InitRelationIds

        private class IRelID
        {
            public int Count = 0;
        }

        public void InitRelationIds()
        {
            InitRelationIds(out _cparentIds, out _categoryIds, out _categoryJsons);
        }

        public void InitRelationIds(IDB db, string cateIds)
        {
            var itemCates = (cateIds ?? "").Trim().Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(x => BaseMgr.Get<Category>(int.Parse(x)))
                .ToList();

            InitRelationIds(itemCates, out _cparentIds, out _categoryIds, out _categoryJsons);
        }

        void InitRelationIds(out string parentIds, out string categoryIds, out string categoryJsons)
        {
            var itemCates = _raw == null ? null : _raw.CategoryProducts.Select(x => x.Category).ToList();
            InitRelationIds(itemCates, out parentIds, out categoryIds, out categoryJsons);
        }

        public static void InitRelationIds(List<Category> itemCates, out string parentIds, out string categoryIds, out string categoryJsons)
        {
            if (itemCates == null) { parentIds = categoryIds = categoryJsons = ""; }
            else
            {
                var dicCate = new Dictionary<int, IRelID>();
                var quePar = new Queue<Category>(itemCates.Select(x => x.Parent).Where(x => x != null));

                while (quePar.Count != 0)
                {
                    var cate = quePar.Dequeue(); IRelID dicVal;
                    if (!dicCate.TryGetValue(cate.Id, out dicVal))
                        dicCate.Add(cate.Id, dicVal = new IRelID());

                    dicVal.Count++;

                    if (cate.Parent != null) quePar.Enqueue(cate.Parent);
                }

                parentIds = dicCate
                    .Select(x => string.Format("{{Id:{0},Count:{1}}}", x.Key, x.Value.Count))
                    .ToString(",");

                categoryIds = itemCates.Select(x => x.Id).ToString<int>(",");
                categoryJsons = Json.Encode(itemCates.Select(x => new { x.Id, Name = x.GetName(), HierOrder = x.HierOrder(false) }));
            }
        }

        #endregion

        [Display(Name = "Inventory")]
        public StatInfo Information { get; set; }

        #region ActiveLang
        [ScriptIgnore(ApplyToOverrides = true)]
        public ProductLangModel ActiveLang
        {
            get
            {
                return GetLangOrAddNew(LanguageMgr.FocusLangCode,
                    () => new ProductLangModel() { LangCode = LanguageMgr.FocusLangCode, ProductId = Id });
            }
        }
        #endregion

        #region InitImages
        public void InitImages()
        {
            Images = _raw.GetImages();
        }
        #endregion
        #region InitVariants

        public void InitVariants(IDB db, bool mergeable = false, bool deletable = false)
        {
            if (mergeable)
            {
                var tmpPath = ModuleUrl.TempFolder.ResolveClientUrl();
                Variants = Variants ?? new List<VariantModel>();
                foreach (var itm in Variants)
                {
                    var status = StatusMgr.GetDom(db, itm.ProductStatusId ?? 0);
                    if (status != null) itm.ProductStatusName = status.GetName();
                    if (itm.ImageFsUrl != null && itm.ImageFsUrl.StartsWith(tmpPath))
                    {
                        itm.ImageThumb = ModuleUrl.TempThumbPath(itm.Image).ResolveClientUrl();
                    }
                    else
                    {
                        itm.ImageThumb = ModuleUrl.ImageThumbPath(itm.Image).ResolveClientUrl();
                        itm.ImageFsUrl = ModuleUrl.ImagePath(itm.Image).ResolveClientUrl();
                    }
                }
            }
            else Variants = _raw.GetVariants();

            Variants.Action((m, i) => m.Iseq = i);

            if (deletable && Variants.Count > 0 && Id != 0)
            {
                var bundleVars = (_raw.BundlingRefs ?? new ProductBundle[0]).Where(x => x.Variant != null)
                    .Select(x => x.Variant.Id).ToList();
                var lst = Singleton<IOrderMgr>.Default.CountVariant(db, Id, Variants.Select(x => x.Id).ToArray(),
                    (byte)OrderStatus.New, (byte)OrderStatus.Processing, (byte)OrderStatus.PayBeforeDelivery, (byte)OrderStatus.RestOrder);
                Variants.Action(m => m.Deletable = !bundleVars.Contains(m.Id) && (lst.Count == 0 || lst.All(k => k.Item1 != m.Id || k.Item2 == 0)) 
                    && m.GetRaw().SafeProp(z => z.BongerProducts).SafeCount() == 0);
            }
        }

        #endregion
        #region InitBundlings
        public void InitBundlings(IDB db, bool mergeable = false)
        {
            if (mergeable)
            {
                foreach (var itm in Bundlings)
                {
                    var prod = ProductMgr.GetDom(db, itm.ProductId ?? 0);
                    if (prod == null) continue;
                    var vari = VariantMgr.GetDom(db, itm.VariantId ?? 0);
                    itm.ProductName = prod.GetName();
                    itm.ProductImage = prod.GetImage(vari);
                    itm.PropDesc = vari.GetPropDesc();
                    itm.ProductVariantNum = prod.VariantNum;
                }
            }
            else Bundlings = _raw.GetBundlings();

            Bundlings.Action((x, i) => x.Iseq = i);
        }
        #endregion
        #region InitAccessories
        public void InitAccessories(IDB db, bool mergeable = false)
        {
            if (mergeable)
            {
                foreach (var itm in Accessories)
                {
                    var prod = ProductMgr.GetDom(db, itm.AccessoryId ?? 0);
                    if (prod != null)
                    {
                        itm.AccessoryName = prod.GetName();
                        itm.AccessoryImage = prod.GetImage(null);
                        itm.AccessoryPrice = prod.Price;
                        itm.AccessoryPriceInc = prod.PriceInc;
                        itm.AccessorySalePrice = prod.SalePrice;
                        itm.AccessorySalePriceInc = prod.SalePriceInc;
                    }

                    var group = ProductBundleGroupMgr.GetDom(db, itm.BundleGroupId ?? 0);
                    if (group != null) itm.BundleGroupName = group.Name;
                }
            }
            else Accessories = Mapper.Map<List<ProductAccessory>, List<ProductAccessoryModel>>(_raw.GetAccessories(true));
        }
        #endregion
        #region InitExtraInformation
        public void InitExtraInformation()
        {
            ExtraInformation = _raw.GetProductExtraInformation();
        }
        #endregion
        #region InitTechSVals
        public void InitTechSVals(IDB db, bool mergeable = false)
        {
            if (mergeable)
            {
                foreach (var itm in TechSVals)
                {
                    var tsVal = TechSpecValMgr.GetDom(db, itm.TechSpecValId ?? 0);
                    if (tsVal == null) continue;
                    itm.TechSpecId = tsVal.TechSpec.Id;
                    itm.TechSpecName = tsVal.TechSpec.Name;
                    itm.TechSpecActive = tsVal.TechSpec.Active;
                    itm.TechSpecValId = tsVal.Id;
                    itm.TechSpecValName = tsVal.Value;
                }
            }
            else TechSVals = _raw.GetTechSVals(db);
        }
        #endregion
        #region InitRecurringDiscounts
        public void InitRecurringDiscounts(IDB db)
        {
            RecurringDiscounts = _raw.GetRecurringDiscounts(db);
        }
        #endregion
        #region InitPrice4LoginCustGroups
        public void InitPrice4LoginCustGroups(IDB db)
        {
            Price4LoginCustGroups = Singleton<ICustGroupMgr>.Default.GetAll(db).Select(m => new Price4LoginCustGroupModel()
            {
                CustGroupId = m.Id,
                CustGroupName = m.Name,
                Selected = m.Price4Login
            }).ToList();
        }
        #endregion
        #region InitCGroupDiscounts
        public void InitCGroupDiscounts(IDB db)
        {
            CGroupDiscounts = new[]
            {
                new { Id = _raw.CGroupDisc0Id, Rate = _raw.CGroupDisc0Rate, DirDisc = false, Desc = "" }, 
                new { Id = _raw.CGroupDisc1Id, Rate = _raw.CGroupDisc1Rate, DirDisc = false, Desc = "" }, 
                new { Id = _raw.CGroupDisc2Id, Rate = _raw.CGroupDisc2Rate, DirDisc = false, Desc = "" },
                new { Id = _raw.CGroupDisc4Id, Rate = _raw.CGroupDisc4Rate, DirDisc = false, Desc = "" },
               _raw.CGroupDisc3Id == null ? null : new { Id = _raw.CGroupDisc3Id, Rate = _raw.CGroupDisc3Rate, DirDisc = true, Desc = "" }
            }.Concat(_raw.Variants.Where(m => !m.Deleted && m.CGroupDisc3Id != null).Select(m => m.CGroupDisc3Id == null ? null : new
            { 
                Id = m.CGroupDisc3Id, Rate = m.CGroupDisc3Rate, DirDisc = true, Desc = m.GetPropDesc() 
            }))
            .Where(m => m != null)
            .Select(m => new ProductCGroupDiscModel()
            {
                GroupId = m.Id,
                Rate = m.Rate * (m.Rate >= 0 ? 1 : -100),
                Type = m.Rate >= 0,
                Active = m.Id != null && (m.Rate ?? 0) != 0,
                DirDiscount = m.DirDisc,
                VariantDesc = m.Desc
            }).ToList();
        }
        #endregion
        #region InitSerialNumbers
        public void InitSerialNumbers(IDB db)
        {
            SerialNumbers = _raw == null || _raw.ProductSerials == null
                ? new List<ProductSerialModel>()
                : _raw.ProductSerials.Select(m => new ProductSerialModel()
                {
                    Id = m.Id,
                    SerialNumber = m.SerialNumber,
                    Sold = m.Sold
                }).ToList().Action((x, idx) => x.Iseq = idx);
        }
        #endregion

        #region Validate
        public static ValidationResult Validate(ProductModel obj, ValidationContext cxt)
        {
            ValidationResult ret;
            if (obj == null) return null;

            if (cxt != null && obj.SalePrice == 0) obj.SalePrice = null;

            if (cxt != null && obj.SalePriceInc == 0) obj.SalePriceInc = null;

            if (cxt != null && !Enum.IsDefined(typeof(ShippingType), obj.ShippingType ?? 0))
                return new ValidationResult(StringTable.ErrMsgRequiredField, new[] { "ShippingType" });

            if (cxt != null && null != (ret = ValidatePrices(obj))) return ret;

            var skuSupplier = SettingsBiz.GetSettings().Hidden.SkuUniqueBySupplier;
            if (!obj.SKU.IsNullOrEmpty() && (
                (obj.Variants != null && obj.Variants.Any(x => x != null && x.SKU.EqualsIgCase(obj.SKU))) ||
                BaseMgr.Exists<Domain.Product>(m => m.Id, obj.Id, m => !skuSupplier || m.Supplier.GetId().EqualsSafe(obj.SupplierId), 
                    new KeyValuePair() { Key = "SKU", Value = obj.SKU }) ||
                BaseMgr.Exists<Variant>(m => m.Id, 0, m => !skuSupplier || m.Product.Supplier.GetId().EqualsSafe(obj.SupplierId), 
                    new KeyValuePair() { Key = "SKU", Value = obj.SKU })))
            {
                return new ValidationResult(StringTable.ErrMsgUniqueAliasConstraint, new[] { "SKU" });
            }

            if (!obj.BarCode.IsNullOrEmpty() && (
                (obj.Variants != null && obj.Variants.Any(x => x != null && x.BarCode.EqualsIgCase(obj.BarCode))) ||
                BaseMgr.Exists<Domain.Product>(m => m.Id, obj.Id, null, new KeyValuePair() { Key = "BarCode", Value = obj.BarCode }) ||
                BaseMgr.Exists<Variant>(m => m.Id, 0, null, new KeyValuePair() { Key = "BarCode", Value = obj.BarCode })))
            {
                return new ValidationResult(StringTable.ErrMsgUniqueAliasConstraint, new[] { "BarCode" });
            }

            if (cxt != null && obj.VisibleWeb && (obj.PublishedDate == null || obj.PublishedDate.Value == null))
                return new ValidationResult(StringTable.ErrMsgRequiredField, new[] { "PublishedDate.Value" });

            if (obj.Variants != null)
            {
                obj.Variants.Action((x, i) => x.Iseq = i);
                foreach (var variant in obj.Variants)
                {
                    var iseq = variant.Iseq;
                    if (null != (ret = VariantModel.ValidateEx(variant, null, obj)))
                        return new ValidationResult(StringTable.ErrMsgUniqueAliasConstraint,
                            ret.MemberNames.Select(x => "Variants[" + iseq + "]." + x));
                }
            }

            return null;
        }
        #endregion

        #region ValidatePrices

        public static ValidationResult ValidatePrices(ProductModel obj)
        {
            if (obj.SalePrice > 0 && obj.Price <= obj.SalePrice)
                return new ValidationResult(Product.StringTable.ErrPriceMustGreaterSalePrice, new[] { "SalePriceInc" });

            //if (obj.SalePrice > 0 && obj.CostPrice > obj.SalePrice)
            //    return new ValidationResult(StringTable.ErrCostPriceMustLessOrEqSalePrice, new[] { "SalePriceInc" });

            //if (!obj.CostPrice.IsNull() && obj.CostPrice > obj.Price)
            //    return new ValidationResult(StringTable.ErrCostPriceMustLessOrEqPrice, new[] { "PriceInc" });

            var settings = SettingsBiz.GetSettings();
            var minPrice = 0;// settings.Display.Others.PriceResolution;

            if (obj.Price < minPrice)
                return new ValidationResult(string.Format(Core.StringTable.ErrMsgMinLimit.ModErrLocalize(), null, new OFloat(minPrice)), new[] { "Price" });

            if (obj.PriceInc < minPrice)
                return new ValidationResult(string.Format(Core.StringTable.ErrMsgMinLimit.ModErrLocalize(), null, new OFloat(minPrice)), new[] { "PriceInc" });

            if (obj.CostPrice < minPrice)
                return new ValidationResult(string.Format(Core.StringTable.ErrMsgMinLimit.ModErrLocalize(), null, new OFloat(minPrice)), new[] { "CostPrice" });

            if (obj.CostPriceInc < minPrice)
                return new ValidationResult(string.Format(Core.StringTable.ErrMsgMinLimit.ModErrLocalize(), null, new OFloat(minPrice)), new[] { "CostPriceInc" });

            return null;
        }

        public static ValidationResult ValidatePrices(IDB db, Domain.ProductBase product)
        {
            ValidationResult ret;
            if (product == null) return null;
            var obj = Mapper.Map<Domain.ProductBase, ProductModel>(new Domain.Product(product));

            if (null != (ret = ValidatePrices(obj))) return ret;

            obj.InitVariants(db);

            foreach (var vari in obj.Variants)
            {
                if (null != (ret = VariantModel.ValidatePrices(vari)))
                    return new ValidationResult("[" + vari.PropDesc + "] " + ret.ErrorMessage, ret.MemberNames);
            }

            return null;
        }

        #endregion

        #region ValidateVariantDelete

        public bool ValidateVariantDelete(IDB db, ModelStateDictionary modelState)
        {
            var obj = ProductMgr.GetDom(db, Id);

            if (obj == null || Variants == null) return true;

            var qDel = obj.Variants.Where(x => !x.Deleted && Variants.All(w => w.Id != x.Id)).ToList();

            foreach (var vari in qDel)
            {
                var lst = Singleton<IOrderMgr>.Default.GetDependOrders(db, 5, vari.Product.Id, vari.Id,
                    (byte)OrderStatus.New, (byte)OrderStatus.Processing, (byte)OrderStatus.PayBeforeDelivery, (byte)OrderStatus.RestOrder);
                if (lst.Count > 0)
                {
                    var errMsg = string.Format(Product.StringTable.VariantDeleteFailedMsg, vari.GetPropDesc(), lst.Take(3).ToString(", ") + (lst.Count > 3 ? " ..." : ""));
                    modelState.AddModelError("", errMsg);
                    return false;
                }
            }

            return true;
        }

        #endregion
    }
}