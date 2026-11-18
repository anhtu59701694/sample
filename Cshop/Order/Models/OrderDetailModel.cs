using System.Globalization;
using Claw.Modules.Eshop.DAL;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Web.Mvc;
using Claw.Modules.Eshop.Helper;
using System;
using Claw.Modules.Eshop.DAL.Interfaces;
using System.Web.Script.Serialization;
using Newtonsoft.Json;

namespace Claw.Modules.Eshop.Models
{
    [CustomValidation(typeof(OrderDetailModel), "Validate")]
    public class OrderDetailModel
    {
        private List<OrderDetailBundleModel> _orderDetailBundles;
        private List<OrderDetailGiftModel> _orderDetailGifts;

        #region Raw
        protected Domain.OrderDetail _raw;

        [JsonIgnore]
        [ScriptIgnore(ApplyToOverrides = true)]
        public Domain.OrderDetail Raw { get { return null; } set { _raw = value; } }

        public Domain.OrderDetail GetRaw() { return _raw; }
        #endregion

        public OrderDetailModel()
        {
            Delivered = false;
        }

        public int Id { get; set; }

        [Required(ErrorMessage = StringTable.ErrMsgRequiredField)]
        public int? OrderId { get; set; }

        [Exportable(false, Name = "Produktnr")]
        [Required(ErrorMessage = StringTable.ErrMsgRequiredField)]
        public int? ProductId { get; set; }

        public int? VariantId { get; set; }

        [Display(Name = "Image")]
        public ImageModel ProductImage { get; set; }

        [Exportable(false, Name = "Produktnavn")]
        [Required(ErrorMessage = StringTable.ErrMsgRequiredField)]
        [Display(Name = "Products")]
        public string ProductName { get; set; }

        [Display(Name = "SKU")]
        public string ProductSKU { get; set; }
        
        public bool ProductDeleted { get; set; }

        [Display(Name = "Status")]
        public string ProductStatusName { get { return _raw == null ? null : (_raw.Variant != null ? _raw.Variant.ProductStatus : _raw.Product.ProductStatus).GetName(); } }

        [Display(Name = "Stock qty")]
        public int? ProductQty { get { return _raw == null ? (int?)null : (_raw.Variant != null ? _raw.Variant.Quantity : _raw.Product.Quantity); } }

        public int? ProductBackQty { get { return _raw == null ? (int?)null : (_raw.Variant != null ? _raw.Variant.BackOrderQty : (_raw.Product.Id ==1 ? 0 : _raw.Product.BackOrderQty)); } }

        [Display(Name = "in order")]
        public int? ProductShopQty { get { return _raw == null ? (int?)null : (_raw.Variant != null ? _raw.Variant.ShoppingQty : (_raw.Product.Id == 1 ? 0 : _raw.Product.ShoppingQty)); } }

        [Exportable(false, Name = "Antall")]
        [Required(ErrorMessage = StringTable.ErrMsgRequiredField)]
        [Range(1, int.MaxValue, ErrorMessage = StringTable.ErrMsgMinLimit)]
        public int? Quantity { get; set; }

        public int? QuantityBak { get; set; }

        [Display(Name = "Snapshot stock qty")]
        public int? StockQty { get; set; }

        [Display(Name = "Cost price (excl.VAT)")]
        public OMoney CostPrice { get; set; }

        public bool? FakeCostPrice { get; set; }

        [Required(ErrorMessage = StringTable.ErrMsgRequiredField)]
        [Display(Name = "Price (excl.VAT)")]
        public OMoney Price { get; set; }

        public OMoney PriceBak { get; set; }

        /// <summary>Price without discount/coupon</summary>
        public OMoney PriceOrigin { get; set; }

        public OMoney PriceOriginInc { get { return PriceOrigin*(1 + Vat); } }

        public OMoney PriceOriginEx { get { return PriceOrigin == 0m || PriceOrigin.IsNull() ? (Id == 0 && OriginPriceInc > 0m ? new OMoney(OriginPriceInc / (1 + Vat)) : Price) : PriceOrigin; } }

        /// <summary>Always have value</summary>
        public OMoney PriceOriginExInc { get { return PriceOriginInc == 0m || PriceOriginInc.IsNull() ? (Id == 0 && OriginPriceInc > 0m ? OriginPriceInc : PriceInc) : PriceOriginInc; } }

        [Required(ErrorMessage = StringTable.ErrMsgRequiredField)]
        public OMoney Vat { get; set; }

        [Exportable(false, Name = "Mva-type")]
        public int VatType { get { return Vat == 0 ? 0 : 3; }}

        [Display(Name = "Inn price (inkl. VAT)")]
        public OMoney CostPriceInc { get { return new OMoney(CostPrice.IsNull() || Vat.IsNull() ? null : CostPrice.Value * (1 + Vat.Value)); } }

        [Required(ErrorMessage = StringTable.ErrMsgRequiredField)]
        [Range(typeof(OMoney), "0", StringTable.MaxOMoney, ErrorMessage = StringTable.ErrMsgRangeLimit)]
        [Display(Name = "Price (VAT)")]
        public OMoney PriceInc { get; set; }

        [Exportable(false, Name = "Enhetspris")]
        public string PriceIncExp { get { return Math.Round((decimal)PriceInc, 2).ToString(CultureInfo.InvariantCulture); } }

        public OMoney OriginPriceInc { get; set; }

        public OMoney OriginSalePriceInc { get; set; }

        public OFloat ProductWeight { get; set; }

        public OMoney ShippingCostExtra { get; set; }

        public OMoney DepositAmount { get; set; }

        public int? ProductShippingType { get; set; }

        public int? DiscountId { get; set; }

        public int? DiscountId2 { get; set; }

        public string DiscountName { get; set; }

        [Required(ErrorMessage = StringTable.ErrMsgRequiredField)]
        [Display(Name = "Discount")]
        public OMoney DiscountValue { get; set; }

        public OMoney DiscountValue2 { get; set; }

        public int? CouponId { get; set; }

        [Remote("ValidateCouponCode", "Order", AdditionalFields = "", HttpMethod = "POST", ErrorMessage = StringTable.ErrMsgInvalidCouponCode)]
        [StringLength(50, ErrorMessage = StringTable.ErrMsgLengthLimit)]
        [RegularExpression(@"[0-9a-zA-Z-]+", ErrorMessage = StringTable.ErrMsgInvalidCodeFormat)]
        [Display(Name = "Coupon code")]
        public string CouponCode { get; set; }

        [Required(ErrorMessage = StringTable.ErrMsgRequiredField)]
        [Display(Name = "Coupon")]
        public OMoney CouponValue { get; set; }

        public string CouponTotal
        {
            get { return CouponValue > 0 ? "-(" + new OMoney(CouponValue * Math.Abs(Quantity.Value)) + ")" : ""; }
        }

        public string ProductShippingTypeText
        {
            get { return (ProductShippingType == (int)ShippingType.FreeShipping ? Product.StringTable.ThisProductIsFreeShipping : (ProductShippingType == (int)ShippingType.Download ? StringTable.Digital : "")); }
        }

        [Display(Name = "Final price")]
        public OMoney FinalPrice { get { return (1.0m + Vat) * Price - DiscountValue - CouponValue; } }

        [Display(Name = "Total (excl.VAT)")]
        public OMoney Total_Exc { get { return (Price * Quantity); } }

        [Display(Name = "Total (incl.VAT)")]
        public OMoney Total_Inc
        {
            get { return PriceInc != null ? (Math.Round(PriceInc, 2, MidpointRounding.AwayFromZero) * Quantity) : 0.0m; }
        }

        public bool Delivered { get; set; }

        public int? RecurQty { get; set; }

        public int? RecurType { get; set; }

        public int? RecurEach { get; set; }

        public bool? GiftCard { get; set; }

        public bool? CreditNote { get; set; }

        public string Data { get; set; }

        public IDictionary<string, object> DataExp { get; set; }

        [Display(Name = "Tag")]
        public string Tags { get; set; }

        public bool Manual { get; set; }

        public string SerialNumber { get; set; }

        public bool? Offered { get; set; }

        public int Iseq { get; set; }

        public ulong? Timestamp { get; set; }

        public bool? AllowEdit { get; set; }

        public string Key { get { return string.Format("{0}_{1}_{2}", ProductId ?? 0, VariantId ?? 0, Id > 0 || ( ProductId ?? 0) != 1 ? (ulong)Id : (Timestamp ?? 0)); } }

        public static string GetUniqueKey(int? productId, int? variantId, decimal? price, string data = null)
        {
            return string.Format("{0}-{1}-{2}{3}", productId ?? 0, variantId ?? 0, new OMoney(price ?? 0), data.IsNullOrEmpty() ? "" : "-:::-" + data);
        }

        public List<System.Tuple<int, string, string>> OrderGiftCards { get; set; }

        #region OrderDetailBundles
        public List<OrderDetailBundleModel> OrderDetailBundles
        {
            get
            {
                if (_orderDetailBundles == null) _orderDetailBundles = new List<OrderDetailBundleModel>();
                return _orderDetailBundles;
            }
            set { _orderDetailBundles = value; }
        }
        #endregion

        #region OrderDetailGifts

        public List<OrderDetailGiftModel> OrderDetailGifts
        {
            get
            {
                if (_orderDetailGifts == null) _orderDetailGifts = new List<OrderDetailGiftModel>();
                return _orderDetailGifts;
            }
            set { _orderDetailGifts = value; }
        }

        #endregion

        #region IsDataModelEmpty

        public bool IsDataModelEmpty
        {
            get { return (ProductId ?? 0) == 0; }
        }

        #endregion

        #region Validate
        public static ValidationResult Validate(OrderDetailModel obj, ValidationContext cxt)
        {
            if (obj == null) return null;

            if (obj.CouponCode.IsNullOrEmpty()) obj.CouponId = null;
            else
            {
                var coupon = Singleton<ICouponMgr>.Default.Get(obj.CouponCode);
                if (coupon != null)
                {
                    obj.CouponId = coupon.Id;
                    var email = QParam.GetField<string>("Customer.Email");
                    var rawOrderTotal = QParam.GetField<OMoney>("RawOrderTotal");
                    var totalDiscount = QParam.GetField<OMoney>("TotalDiscount"); 
                    if (cxt != null && obj.Id == 0 &&
                        coupon.Validate(new DefaultDb(), null, obj.ProductId ?? 0, obj.VariantId, rawOrderTotal - totalDiscount, email) == null)
                        return new ValidationResult(Order.StringTable.CouponContextError, new[] { "CouponCode" });
                }
                else
                    return new ValidationResult(StringTable.ErrMsgInvalidCouponCode.ModErrLocalize(),
                        new[] { "CouponCode" });
            }

            return null;
        }
        #endregion
    }

    #region OrderDetailNilModel
    public class OrderDetailNilModel
    {
        [Exportable(false, Name = "Rabatt")]
        public string DiscountExp { get; set; }

        [Exportable(false, Name = "Abo-enhetsperiode")]
        public string SubUnitPricePeriod { get; set; }

        [Exportable(false, Name = "Abo-enhetsperiode-enhet")]
        public string SubUnitPricePeriodUnit { get; set; }

        [Exportable(false, Name = "Abo-faktureringsperiode")]
        public string SubInvoicePeriod { get; set; }

        [Exportable(false, Name = "Abo-fakturerigsperiode-enhet")]
        public string SubInvoicePeriodType { get; set; }

        [Exportable(false, Name = "Abo-forskudd/etterskudd")]
        public string SubInvoicingPeriodCount { get; set; }

        [Exportable(false, Name = "Abo-forskudd-etterskudd-enhet")]
        public string SubInvoicingPeriodUnit { get; set; }

        [Exportable(false, Name = "Abo-startdato")]
        public string SubStartDate { get; set; }

        [Exportable(false, Name = "Beskrivelse")]
        public string ODetailDesc { get; set; }

        [Exportable(false, Name = "Avdelingsnr")]
        public int? DepartmentId { get; set; }

        [Exportable(false, Name = "Avdelingsnavn")]
        public string DepartmentName { get; set; }

        [Exportable(false, Name = "Prosjektnr")]
        public string ProjectId { get; set; }

        [Exportable(false, Name = "Prosjektnavn")]
        public string ProjectName { get; set; }
    } 
    #endregion
}