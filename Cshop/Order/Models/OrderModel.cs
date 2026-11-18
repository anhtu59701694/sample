using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;
using System.Web.Routing;
using System.Web.Script.Serialization;
using Claw.Core.Extensions;
using Claw.Modules.Eshop.DAL;
using Claw.Modules.Eshop.Domain;
using System.Web.Mvc;
using Claw.Modules.Eshop.Helper;
using Newtonsoft.Json;
using NPOI.OpenXmlFormats.Spreadsheet;
using Claw.Modules.Eshop.DAL.Interfaces;
using Claw.Core;
using System.Web;

namespace Claw.Modules.Eshop.Models
{
    public enum SplitPaymentType { Cash = 1, Credit = 2, Gift = 4 }

    [CustomValidation(typeof (OrderModel), "Validate")]
    public class OrderModel
    {
        public static Func<Domain.Order, string> FuncOrderStatus = null;

        #region AdvSearchModel

        [CustomValidation(typeof (AdvSearchModel), "Validate")]
        public class AdvSearchModel
        {
            [StringLength(20, ErrorMessage = StringTable.ErrMsgLengthLimit)]
            [Display(Name = "Order#/Invoice#")]
            public string RefId { get; set; }

            [StringLength(150, ErrorMessage = StringTable.ErrMsgLengthLimit)]
            public string Email { get; set; }

            [StringLength(100, ErrorMessage = StringTable.ErrMsgLengthLimit)]
            public string Name { get; set; }

            [StringLength(400, ErrorMessage = StringTable.ErrMsgLengthLimit)]
            [Display(Name = "Product")]
            public string ProductName { get; set; }

            [StringLength(50, ErrorMessage = StringTable.ErrMsgLengthLimit)]
            [Display(Name = "Serial nr")]
            public string SerialNumber { get; set; }

            [Display(Name = "Date")]
            [JsonConverter(typeof(ODateTime.JsonConverter))]
            public ODateTime FromDate { get; set; }


            [Display(Name = "From date")]
            [JsonConverter(typeof(ODateTime.JsonConverter))]
            public ODateTime DateFrom { get; set; }

            [Display(Name = "To date")]
            [JsonConverter(typeof(ODateTime.JsonConverter))]
            public ODateTime DateTo { get; set; }


            [Display(Name = "Payment")]
            public int? PaymentMethodId { get; set; }

            [Display(Name = "Shipping")]
            public int? ShippingId { get; set; }

            public byte? Status { get; set; }

            [Display(Name = "Flag custom 0")]
            public bool? FlagCustom0 { get; set; }

            [Display(Name = "Flag custom 1")]
            public bool? FlagCustom1 { get; set; }

            [Display(Name = "DropShip")]
            public bool? FlagDropShip { get; set; }

            #region IsDataModelEmpty

            public bool IsDataModelEmpty
            {
                get
                {
                    foreach (var prop in typeof (AdvSearchModel).GetProperties())
                    {
                        if (prop.CanWrite && prop.GetValue(this) != null && !prop.Name.EqualsIgCase("Status"))
                            return true;
                    }
                    return false;
                }
            }

            #endregion

            #region Validate

            public static ValidationResult Validate(AdvSearchModel obj, ValidationContext cxt)
            {
                if (obj == null) return null;

                foreach (var prop in typeof (AdvSearchModel).GetProperties()
                    .Where(x => x.CanWrite && x.PropertyType == typeof (string)))
                {
                    var val = (string) prop.GetValue(obj);
                    if (val != null) prop.SetValue(obj, val.UrlDecode());
                }

                return null;
            }

            #endregion
        }

        #endregion

        #region FOpt

        public class FOpt
        {
            [Required(ErrorMessage = StringTable.ErrMsgRequiredField)]
            [StringLength(200, ErrorMessage = StringTable.ErrMsgLengthLimit)]
            [Display(Name = "Payment method name")]
            public string PaymentMethodName { get; set; }

            [Required(ErrorMessage = StringTable.ErrMsgRequiredField)]
            [Display(Name = "Payment gateway")]
            public int? PaymentMethodGetwayId { get; set; }

            [Required(ErrorMessage = StringTable.ErrMsgRequiredField)]
            [Display(Name = "Payment tax")]
            public int PaymentMethodVatId { get; set; }

            [Required(ErrorMessage = StringTable.ErrMsgRequiredField)]
            [Display(Name = "Fee (incl. VAT)")]
            [Range(typeof (OMoney), "0", StringTable.MaxOMoney, ErrorMessage = StringTable.ErrMsgMinLimit)]
            public OMoney PaymentMethodFee { get; set; }
        }

        #endregion

        #region Raw

        protected Domain.Order _raw;

        [JsonIgnore]
        [ScriptIgnore(ApplyToOverrides = true)]
        public Domain.Order Raw
        {
            get { return null; }
            set { _raw = value; }
        }

        public Domain.Order GetRaw()
        {
            return _raw = _raw ?? new Domain.Order();
        }

        #endregion

        private FOpt _opt = new FOpt();
        private Currency _currency;

        #region OrderModel

        public OrderModel()
        {
            PaymentCost = 0;
            PaymentVat = 0;
            ShippingCost = 0;
            ShippingVat = 0;
            DiscountValue = 0;
            CouponValue = 0;
            TotalDiscount = 0;
            TotalCoupon = 0;
            OrderTotal = 0;
            VatTotal = 0;
            FinalTotal = 0;
            DepositTotal = 0;
            Type = (byte) Models.OrderType.Web;
            OrderStatus = (byte) Models.OrderStatus.New;
            IsFreeShipping = false;
            CreatedDate = DateTime.Now;
        }

        #endregion

        [JsonIgnore]
        [ScriptIgnore(ApplyToOverrides = true)]
        public FOpt Opt
        {
            get { return _opt; }
        }

        public bool InlineEdit { get; set; }

        [Exportable(false, Name = "Ordrenr.")]
        public int Id { get; set; }

        public long Key { get { return Id > 0 ? Id : (CartOrderId ?? 0); } }

        public int? CartId { get; set; }

        public int? CartIdOffered { get; set; }

        [Display(Name = "Cart")]
        public long? CartOrderId { get { return CartId == null ? null : Cart.Seed + CartId; } }

        public bool? CartNotified { get; set; }

        public string DownloadContents { get; set; }

        [StringLength(50, ErrorMessage = StringTable.ErrMsgLengthLimit)]
        //[RegularExpression("[0-9]+", ErrorMessage = StringTable.ErrMsgRequireNumber)]
        public string OrgNumber { get; set; }

        [StringLength(100, ErrorMessage = StringTable.ErrMsgLengthLimit)]
        public string CompanyName { get; set; }

        [StringLength(150, ErrorMessage = StringTable.ErrMsgLengthLimit)]
        [EmailAddress(ErrorMessage = StringTable.ErrMsgInvalidEmail)]
        public string InvoiceEmail { get; set; }

        [Required(ErrorMessage = StringTable.ErrMsgRequiredField)]
        [StringLength(100, ErrorMessage = StringTable.ErrMsgLengthLimit)]
        [Display(Name = "Name")]
        public string DeliverName { get; set; }

        //[Required(ErrorMessage = StringTable.ErrMsgRequiredField)]
        [StringLength(20, ErrorMessage = StringTable.ErrMsgLengthLimit)]
        [Display(Name = "Phone")]
        public string DeliverPhone { get; set; }

        //[Required(ErrorMessage = StringTable.ErrMsgRequiredField)]
        [StringLength(20, ErrorMessage = StringTable.ErrMsgLengthLimit)]
        [Display(Name = "Mobile")]
        public string DeliverMobile { get; set; }

        [Required(ErrorMessage = StringTable.ErrMsgRequiredField)]
        [StringLength(100, ErrorMessage = StringTable.ErrMsgLengthLimit)]
        [Display(Name = "Address")]
        public string DeliverAddress { get; set; }

        [Required(ErrorMessage = StringTable.ErrMsgRequiredField)]
        [StringLength(10, ErrorMessage = StringTable.ErrMsgLengthLimit)]
        [Display(Name = "Postnr")]
        public string DeliverPostnr { get; set; }

        [Required(ErrorMessage = StringTable.ErrMsgRequiredField)]
        [StringLength(50, ErrorMessage = StringTable.ErrMsgLengthLimit)]
        [Display(Name = "Posted")]
        public string DeliverPosted { get; set; }

        //[Required(ErrorMessage = StringTable.ErrMsgRequiredField)]
        [StringLength(50, ErrorMessage = StringTable.ErrMsgLengthLimit)]
        [Display(Name = "City")]
        public string DeliverCity { get; set; }

        [Required(ErrorMessage = StringTable.ErrMsgRequiredField)]
        [Display(Name = "Country")]
        public int? DeliverCountryId { get; set; }

        [Display(Name = "Country")]
        public string DeliverCountryCode { get; set; }

        [Display(Name = "Country")]
        public string DeliverCountryName { get; set; }

        #region DeliverInfo

        [Exportable(false, Name = "Leveransested")]
        [Display(Name = "Deliver addr")]
        public string DeliverInfo
        {
            get
            {
                return string.Format("{0} {1} {2} {3}",
                    string.IsNullOrEmpty(DeliverAddress) ? "" : DeliverAddress + ",", DeliverPostnr, DeliverPosted,
                    string.IsNullOrEmpty(DeliverCity) ? "" : "," + DeliverCity);
            }
        }

        #endregion

        [Exportable(false, Name = "Kundenavn")]
        [Required(ErrorMessage = StringTable.ErrMsgRequiredField)]
        [StringLength(100, ErrorMessage = StringTable.ErrMsgLengthLimit)]
        [Display(Name = "Name")]
        public string BillingName { get; set; }

        public string BillingCoName { get { return Customer.SafeProp(m => m.ContactName).IfNull(BillingName); } }

        //[Required(ErrorMessage = StringTable.ErrMsgRequiredField)]
        [StringLength(20, ErrorMessage = StringTable.ErrMsgLengthLimit)]
        [Display(Name = "Phone")]
        public string BillingPhone { get; set; }

        [Required(ErrorMessage = StringTable.ErrMsgRequiredField)]
        [StringLength(20, ErrorMessage = StringTable.ErrMsgLengthLimit)]
        [Display(Name = "Mobile")]
        public string BillingMobile { get; set; }

        [Exportable(false, Name = "Adresselinje1")]
        [Required(ErrorMessage = StringTable.ErrMsgRequiredField)]
        [StringLength(100, ErrorMessage = StringTable.ErrMsgLengthLimit)]
        [Display(Name = "Address")]
        public string BillingAddress { get; set; }

        [Exportable(false, Name = "Adresselinje2")]
        public string BillingAddress2 { get; set; }

        [Exportable(false, Name = "Postnr.")]
        [Required(ErrorMessage = StringTable.ErrMsgRequiredField)]
        [StringLength(10, ErrorMessage = StringTable.ErrMsgLengthLimit)]
        [Display(Name = "Postnr")]
        public string BillingPostnr { get; set; }

        [Exportable(false, Name = "Sted")]
        [Required(ErrorMessage = StringTable.ErrMsgRequiredField)]
        [StringLength(50, ErrorMessage = StringTable.ErrMsgLengthLimit)]
        [Display(Name = "Posted")]
        public string BillingPosted { get; set; }

        //[Required(ErrorMessage = StringTable.ErrMsgRequiredField)]
        [StringLength(50, ErrorMessage = StringTable.ErrMsgLengthLimit)]
        [Display(Name = "City")]
        public string BillingCity { get; set; }

        [Required(ErrorMessage = StringTable.ErrMsgRequiredField)]
        [Display(Name = "Country")]
        public int? BillingCountryId { get; set; }

        [Display(Name = "Country")]
        public string BillingCountryCode { get; set; }

        [Display(Name = "Country")]
        public string BillingCountryName { get; set; }

        #region BillingInfo

        [Display(Name = "Billing")]
        public string BillingInfo
        {
            get
            {
                return string.Format("{0} {1} {2} {3}",
                    string.IsNullOrEmpty(BillingAddress) ? "" : BillingAddress + ",", BillingPostnr, BillingPosted,
                    string.IsNullOrEmpty(BillingCity) ? "" : "," + BillingCity);
            }
        }

        #endregion

        [Required(ErrorMessage = StringTable.ErrMsgRequiredField)]
        [Display(Name = "Payment method")]
        public int? PaymentMethodId { get; set; }

        [Required(ErrorMessage = StringTable.ErrMsgRequiredField)]
        [Display(Name = "Payment cost")]
        public OMoney PaymentCost { get; set; }

        [Required(ErrorMessage = StringTable.ErrMsgRequiredField)]
        [Display(Name = "Payment vat")]
        public OMoney PaymentVat { get; set; }

        [Exportable(false, Name = "Betalingstype (innbetalt beløp)")]
        [Display(Name = "Payment name")]
        public string PaymentName { get; set; }

        public List<System.Tuple<PaymentMethodModel, OMoney>> SplitPaymentMethods { get; set; }

        public int SplitPayment { get; set; }

        public string SplitPayData { get; set; }

        [Display(Name = "Transaction id")]
        public string TransactionId { get; set; }

        [Display(Name = "Authorization id")]
        public string AuthorizationId { get; set; }

        public string CombinedTransactionId
        {
            get { return TransactionId.IsNullOrEmpty() ? AuthorizationId : TransactionId; }
        }

        [Display(Name = "Payment id")]
        public string PaymentResultId { get; set; }

        [Display(Name = "Card number")]
        public string PaymentCardMaskedPan { get; set; }

        public bool IsKlarnaCheckoutPayment { get { return _raw != null && _raw.PaymentMethod != null && _raw.PaymentMethod.PaymentSystemName == GatewayID.KlarnaCheckout.ToString(); } }

        public bool IsKlarnaPayments { get { return _raw != null && _raw.PaymentMethod != null && _raw.PaymentMethod.PaymentSystemName == GatewayID.KlarnaPayments.ToString(); } }

        public bool IsDibsPayment { get { return _raw != null && _raw.PaymentMethod != null && _raw.PaymentMethod.PaymentSystemName == GatewayID.DIBS.ToString(); } }

        public bool IsVippsPayment { get { return _raw != null && _raw.PaymentMethod != null && _raw.PaymentMethod.PaymentSystemName == GatewayID.Vipps.ToString(); } }

        [Display(Name = "Shipping")]
        public int? ShippingId { get; set; }

        [Display(Name = "Shipping name")]
        public string ShippingName { get; set; }

        [Required(ErrorMessage = StringTable.ErrMsgRequiredField)]
        [Display(Name = "Shipping cost")]
        public OMoney ShippingCost { get; set; }

        [Required(ErrorMessage = StringTable.ErrMsgRequiredField)]
        [Display(Name = "Shipping vat")]
        public OMoney ShippingVat { get; set; }

        [Display(Name = "Shipping tracking id")]
        public string ShippingTrackingId { get; set; }

        [Display(Name = "Reservation id")]
        public string ReservationId { get; set; }

        [Display(Name = "Discount")]
        public int? DiscountId { get; set; }

        public int? DiscountId2 { get; set; }

        [Required(ErrorMessage = StringTable.ErrMsgRequiredField)]
        [Display(Name = "Discount value")]
        public OMoney DiscountValue { get; set; }

        public OMoney DiscountValue2 { get; set; }

        [Display(Name = "Discount name")]
        public string DiscountName { get; set; }

        [Remote("ValidateCouponCode", "Order", AdditionalFields = "RawOrderTotal,TotalDiscount,Customer.Email", HttpMethod = "POST",
            ErrorMessage = StringTable.ErrMsgInvalidCouponCode)]
        [Display(Name = "Coupon code")]
        [StringLength(50, ErrorMessage = StringTable.ErrMsgLengthLimit)]
        [RegularExpression(@"[0-9a-zA-Z\-\%]+", ErrorMessage = StringTable.ErrMsgInvalidCodeFormat)]
        public string CouponCodeShared { get; set; }

        [Display(Name = "Coupon")]
        public int? CouponId { get; set; }

        [Required(ErrorMessage = StringTable.ErrMsgRequiredField)]
        [Display(Name = "Coupon value")]
        public OMoney CouponValue { get; set; }

        [Remote("ValidateCouponCode", "Order", AdditionalFields = "", HttpMethod = "POST",
            ErrorMessage = StringTable.ErrMsgInvalidCouponCode)]
        [Display(Name = "Coupon code")]
        [StringLength(50, ErrorMessage = StringTable.ErrMsgLengthLimit)]
        [RegularExpression(@"[0-9a-zA-Z\-\%]+", ErrorMessage = StringTable.ErrMsgInvalidCodeFormat)]
        public string CouponCode { get; set; }

        public string CouponName
        {
            get { return _raw == null || _raw.Coupon == null ? null : _raw.Coupon.Name; }
        }

        public string CouponNameShared { get; set; }

        //[Display(Name = "Giftcard total")]
        //public OMoney GiftCardTotal { get; set; }

        //public List<System.Tuple<int, OMoney>> GiftCards { get; set; }

        [Required(ErrorMessage = StringTable.ErrMsgRequiredField)]
        [Display(Name = "Order status")]
        public byte OrderStatus { get; set; }

        public byte Type { get; set; }

        [Display(Name = "Language")]
        public string LangCode { get; set; }

        [Exportable(false, Name = "Valuta")]
        [Display(Name = "Currency")]
        public string Currency { get; set; }

        public OFloat ExchangeRate { get; set; }

        #region OrderStatusTxt

        [Display(Name = "Status")]
        public string OrderStatusTxt
        {
            get
            {
                if (FuncOrderStatus != null) 
                {
                    var tmp = FuncOrderStatus(_raw);
                    if (tmp != null) return tmp;
                }

                if (!OfferExpiredDate.IsNull())
                    return OfferOrderId != null 
                        ? "offer status used".ModLocalize("Used") 
                        : (OfferExpiredDate.Value.Value.Date < DateTime.Now.Date 
                            ? "offer status expired".ModLocalize("Expired") 
                            : "offer status valid".ModLocalize("Valid"));

                const int customFlags = (int) Domain.Interfaces.OrderFlagType.Custom0 | (int) Domain.Interfaces.OrderFlagType.Custom1 | (int)Domain.Interfaces.OrderFlagType.DropShipMark;
                if (OrderStatus.InList(Models.OrderStatus.New, Models.OrderStatus.Processing, Models.OrderStatus.RestOrder) && 0 != (Flags & customFlags))
                    return Order.StringTable.OrderStatusFlagCustom
                        .Replace("{0}", typeof(OrderStatus).LocalizeEnumValue(OrderStatus))
                        .Replace("{1}", typeof(Domain.Interfaces.OrderFlagType).LocalizeEnumValues<int>(x => 0 != (x&customFlags & Flags)).Select(m => m.Text).ToString(", "));

                if (OrderStatus.InList(Models.OrderStatus.New, Models.OrderStatus.PaymentWaiting) && 0 != (Flags & (int)Domain.Interfaces.OrderFlagType.RenewPayment))
                    return Order.StringTable.OrderStatusRenewPayment.Replace("{0}", typeof(OrderStatus).LocalizeEnumValue(OrderStatus));

                if (Sent2PickupPoint)
                    return Order.StringTable.OrderStatusSent2PickupPoint.Replace("{0}", typeof(OrderStatus).LocalizeEnumValue(OrderStatus));

                return 0 == (Flags & (int)Domain.Interfaces.OrderFlagType.TilRegnskap) || !OrderStatus.InList(Models.OrderStatus.New, Models.OrderStatus.Processing, Models.OrderStatus.RestOrder)
                    ? typeof (OrderStatus).LocalizeEnumValue(OrderStatus)
                    : Order.StringTable.OrderStatusTilRegnskap.Replace("{0}", typeof (OrderStatus).LocalizeEnumValue(OrderStatus));
            }
            set
            {
                OrderStatus oVal;
                if (!value.IsNullOrEmpty() && Enum.TryParse(value, true, out oVal)) OrderStatus = (byte) oVal;
            }
        }

        #endregion

        public bool Sent2PickupPoint
        {
            get { return OrderStatus.InList(Models.OrderStatus.Processing) && 0 != (Flags & (int) Domain.Interfaces.OrderFlagType.Sent2PickupPoint); }
        }

        public string Token { get; set; }
        public int? PaymentRetryCount { get; set; }
        public int? PaymentRetryStage { get; set; }
        public ODateTime PaymentRetryTimestamp { get; set; }
        public bool? VatEnabled { get; set; }

        public int? ItemTotal { get; set; }

        /// <summary>include vat</summary>
        [Display(Name = "Order total")]
        public OMoney OrderTotal { get; set; }

        public OMoney OrderTotalExc
        {
            get { return OrderTotal - VatTotal; }
        }

        public OMoney OrderTotalBak { get; set; }

        public OMoney VatTotalBak { get; set; }

        public OMoney FinalTotalBak { get; set; }

        public OMoney OrderTotalExcAfterDiscount
        {
            get { return ((decimal?) (OrderTotalExc - TotalCoupon - TotalDiscount)).NormalizePriceInc(); }
        }

        [Display(Name = "Vat total")]
        public OMoney VatTotal { get; set; }

        public OMoney AllVatSum
        {
            get
            {
                //if (VatEnabled.HasValue && !VatEnabled.Value) return 0;
                //var vatSumAfterCoup = _raw.GetAllVatSumAfterCoupDisc();
                //return vatSumAfterCoup < 0 ? 0 : vatSumAfterCoup;
                return 0;
            }
        }

        public List<System.Tuple<OFloat, OMoney, OMoney>> TaxStats { get; set; }

        [Display(Name = "Coupon value")]
        public OMoney TotalCoupon { get; set; }

        public string TotalCouponTxt
        {
            get
            {
                return CouponValue >= 0 || (_raw != null && _raw.Coupon != null && _raw.Coupon.DiscountType.InList(CouponType.PercentAndFreeShipping))
                        ? ("- " + TotalCoupon.Value.ToOMoneyMc(this)) 
                        : Order.StringTable.FreeshippingOrder;
            }
        }

        public bool ShowTotalCoupon {  get { return CouponValue != 0 || TotalCoupon != 0; } }

        [Display(Name = "Discount value")]
        public OMoney TotalDiscount { get; set; }

        public string TotalDiscountTxt
        {
            get
            {
                return DiscountValue < 0 && !IsPosOrder
                    ? Order.StringTable.FreeshippingOrder
                    : ((TotalDiscount > 0 ? "- " : "") +
                       ((decimal?) Math.Abs(TotalDiscount.Value ?? 0)).ToOMoneyMc(this));
            }
        }

        public OMoney ItemDiscountTotal { get; set; }

        public OMoney GiftCardTotalPos { get; set; }
        
        [Display(Name = "Giftcard total")]
        public OMoney GiftCardTotal { get; set; }

        public string GiftCardData { get; set; }

        public OMoney GiftCardSales { get; set; }

        [Display(Name = "Sette inn kontanter")]
        public OMoney DepositTotal { get; set; }

        public List<GiftCardOrder> GetGiftCards()
        {
            return GiftCardData.IsNullOrEmpty() ? new List<GiftCardOrder>() : JsonConvert.DeserializeObject<List<GiftCardOrder>>(GiftCardData);
        }

        [Display(Name = "Total")]
        public OMoney FinalTotal { get; set; }

        [Display(Name = "Total")]
        public OMoneyMc FinalTotalMc
        {
            get { return ((decimal?) FinalTotal).ToOMoneyMc(this); }
        }

        [Exportable(false, Name = "Innbetalt beløp")]
        public string FinalTotalExp
        {
            get { return Math.Round((decimal) FinalTotal, 2).ToString(CultureInfo.InvariantCulture); }
        }

        /// <summary>sum of (quantity*PriceInc) = OrderTotal</summary>
        public OMoney RawOrderTotal { get; set; }

        public OMoney ShipOrderTotal { get; set; }

        /// <summary>kg</summary>
        public OFloat ShipWeightTotal { get; set; }

        [JsonConverter(typeof(ODateTime.JsonConverter))]   
        [Display(Name = "Ordered date")]
        public ODateTime CreatedDate { get; set; }

        [Exportable(false, Name = "Dato")]
        public string CreatedDateExp
        {
            get { return CreatedDate.IsNull() ? "" : CreatedDate.Value.Value.ToString("yyyy-MM-dd"); }
        }

        [JsonConverter(typeof(ODateTime.JsonConverter))]
        public ODateTime ModifiedDate { get; set; }

        [JsonConverter(typeof(ODateTime.JsonConverter))]
        public ODateTime ReportDate { get; set; }

        [Exportable(false, Name = "Forfallsdato")]
        public string DueDate
        {
            get { return DeliveredDate.IsNull() ? "" : DeliveredDate.Value.Value.AddDays(14).ToString("yyyy-MM-dd"); }
        }

        [Display(Name = "Delivered date")]
        [JsonConverter(typeof(ODateTime.JsonConverter))]
        public ODateTime DeliveredDate { get; set; }

        [Exportable(false, Name = "Leveransedato")]
        public string DeliveredDateExp
        {
            get { return DeliveredDate.IsNull() ? CreatedDateExp : DeliveredDate.Value.Value.ToString("yyyy-MM-dd"); }
        }

        [Display(Name = "Expected delivery date")]
        [JsonConverter(typeof(ODateTime.JsonConverter))]
        public ODateTime ExpectedDelivery { get; set; }

        [Exportable(false, Name = "Kommentar")]
        [StringLength(64000, ErrorMessage = StringTable.ErrMsgLengthLimit)]
        public string Comments { get; set; }

        [Display(Name = "Send email to customer")]
        public bool? SendNotifyEmail { get; set; }

        [Display(Name = "User ip")]
        public string UserIp { get; set; }

        [Display(Name = "User host")]
        public string UserHost { get; set; }

        [Display(Name = "User agent")]
        public string UserAgent { get; set; }

        [Exportable(false, Name = "Fakturanr.")]
        [Display(Name = "Invoice id")]
        public int? InvoiceId { get; set; }

        [Exportable(false, Name = "Fakturadato")]
        public string InvoiceDate
        {
            get { return DeliveredDate.IsNull() ? "" : DeliveredDate.Value.Value.ToString("yyyy-MM-dd"); }
        }

        [Display(Name = "Credited")]
        public int? Credited { get; set; }

        [Display(Name = " Original credited")]
        public int? OriginalCredited { get; set; }

        [Display(Name = "Backorder id")]
        public List<int> BackOrderIds { get; set; }

        [Display(Name = "Original backorder id")]
        public int? OriginalBackOrderId { get; set; }

        [StringLength(64000, ErrorMessage = StringTable.ErrMsgLengthLimit)]
        public string ExtraInfo1 { get; set; }

        [StringLength(64000, ErrorMessage = StringTable.ErrMsgLengthLimit)]
        public string ExtraInfo2 { get; set; }

        [StringLength(64000, ErrorMessage = StringTable.ErrMsgLengthLimit)]
        public string ExtraInfo3 { get; set; }

        [StringLength(64000, ErrorMessage = StringTable.ErrMsgLengthLimit)]
        public string ExtraInfo4 { get; set; }

        [Exportable(false, Name = "KID")]
        public string Kid { get; set; }

        public int? PackerId { get; set; }

        [Display(Name = "Packer name")]
        public string PackerName { get; set; }

        [Display(Name = "Packing start")]
        [JsonConverter(typeof(ODateTime.JsonConverter))]
        public ODateTime PackingStart { get; set; }

        [Display(Name = "Packing finish")]
        [JsonConverter(typeof(ODateTime.JsonConverter))]
        public ODateTime PackingEnd { get; set; }

        [Required(ErrorMessage = StringTable.ErrMsgRequiredField)]
        public CustomerModel Customer { get; set; }

        [Display(Name = "Recurring order")]
        public RecurOrderModel Recurring { get; set; }

        public string DataJson { get; set; }

        public bool? IsFreeShipping { get; set; }

        public bool? CanCancel { get; set; }

        public bool? CanReturn { get; set; }

        public bool? CanMakeRest { get; set; }

        [Exportable(false, Name = "Attn-fornavn")]
        public string AttnFirstName { get; set; }

        [Exportable(false, Name = "Attn-etternavn")]
        public string AttnLastName { get; set; }

        [Exportable(false, Name = "Ref.nummer")]
        public string RefNum { get; set; }

        /// <summary> kg </summary>
        [Display(Name = "Total weight")]
        public OFloat TotalWeight { get; set; }

        public OMoney PaymentCostExc
        {
            get { return this.PaymentCost/(this.PaymentVat + 1); }
        }

        public OMoney ShippingCostExc
        {
            get { return this.ShippingCost/(this.ShippingVat + 1); }
        }

        public bool IsPosOrder { get; set; }

        public int Flags { get; set; }
     
        [Display(Name = "Rounding up")]
        public OMoney RoundingUp { get; set; }

        [Display(Name = "Expired date")]
        [Required(ErrorMessage = StringTable.ErrMsgRequiredField)]
        [JsonConverter(typeof(ODateTime.JsonConverter))]
        public ODateTime OfferExpiredDate { get; set; }

        public string OfferToken { get; set; }

        [Display(Name = "Message")]
        [StringLength(4000, ErrorMessage = StringTable.ErrMsgLengthLimit)]
        public string OfferMsg { get; set; }

        [Display(Name = "SMS message")]
        [Required(ErrorMessage = StringTable.ErrMsgRequiredField)]
        public string OfferSmsMsg { get; set; }

        [Display(Name = "Send sms")]
        public bool? OfferSendSms { get; set; }
        public bool? OfferEmailSent { get; set; }

        [Display(Name = "Order id")]
        public int? OfferOrderId { get; set; }

        [Display(Name = "Last view")]
        public ODateTime OfferLastView { get; set; }

        [Display(Name = "Read")]
        public ODateTime OfferEmailView { get; set; }

        [Display(Name = "Hide discount per product")]
        public bool? OfferHideDiscPerProd { get; set; }

        [Display(Name = "Discount rate")]
        [Range(typeof(OFloat), "0", "100", ErrorMessage = StringTable.ErrMsgRangeLimit)]
        public OFloat CustomDiscountRate { get; set; }

        [Display(Name = "Profit per product")]
        public bool? OfferProfitPerProd { get; set; }

        public bool? RecurringTokenOnly { get; set; }

        public string RecurringReturnUrl { get; set; }

        public List<BackOrderDetailModel> BackOrderDetails { get; set; }

        public List<OrderDetailModel> OrderDetails { get; set; }

        public List<OrderNoteModel> OrderNotes { get; set; }

        public List<PaymentLogModel> PaymentLogs { get; set; }

        #region GetCurrency

        public Currency GetCurrency()
        {
            if (Id == 0) return null;

            return _currency = (_currency ?? this.ToCurrency());
        }

        #endregion

        #region InitOrderDetails

        public void InitOrderDetails(IDB db, bool mergeable = false, bool includePayShip = false)
        {
            InitOrderDetails(db, mergeable, includePayShip, false);
        }

        public void InitOrderDetails(IDB db, bool mergeable, bool includePayShip, bool copyAllowEdit)
        {
            var allowEditDic = copyAllowEdit && OrderDetails != null ? OrderDetails.Where(m => m.Id != 0 && m.AllowEdit == true).ToDictionary(m => m.Id, m => m) : null;

            if (mergeable)
            {
                foreach (var itm in OrderDetails)
                {
                    var prod = Product.DAL.ProductMgr.GetDom(db, itm.ProductId ?? 0);
                    if (prod == null) continue;
                    var vari = Product.DAL.VariantMgr.GetDom(db, itm.VariantId ?? 0);
                    itm.ProductImage = prod.GetImage(vari);

                    foreach (var gift in itm.OrderDetailGifts)
                    {
                        gift.Init(db, gift.ProductId ?? 0, gift.VariantId, gift.Quantity ?? 0);
                    }
                }
            }
            else OrderDetails = _raw.GetOrderDetails(includePayShip, db);
            
            var settings = SettingsBiz.GetSettings();
                
            OrderDetails.Action((m, i) =>
            {
                m.Iseq = i;
                m.OrderDetailGifts.Action((mk, k) => mk.Iseq = k);

                if (!m.Data.IsNullOrEmpty() && SettingsBiz.GetSettings().Hidden.OrderDetailDataJson)
                {
                    m.DataExp = m.Data.ToExpando();   

                    if (m.ProductId == settings.Hidden.GiftCardProductId && m.DataExp != null && m.DataExp.Keys.Any(z => z.EqualsIgCase("Amount")))
                    {
                        m.DataExp.Keys
                            .Where(z => z.EqualsIgCase("Amount")).ToList()
                            .Action(z => m.DataExp.Remove(z));
                         
                        m.OrderGiftCards =  m.GetRaw()
                                .GiftCards.Cast<Domain.Interfaces.IGiftCard>()
                                .Select(z => new System.Tuple<int, string, string>(z.Id, z.Code, (m.DataExp.SafeGet("Message") ?? "").ToString())).ToList();
                    }
                }

                if (m.Id != 0 && Type.InList(OrderType.Pos) && m.GetRaw().GC2reditNote != null && m.OrderGiftCards.SafeCount() == 0)
                    m.OrderGiftCards = m.GetRaw()
                        .GiftCards.Cast<Domain.Interfaces.IGiftCard>()
                        .Select(z => new System.Tuple<int, string, string>(z.Id, z.Code, null)).ToList();

                if (allowEditDic != null && allowEditDic.ContainsKey(m.Id)) m.AllowEdit = true;
            });
        }

        #endregion

        #region InitOrderNotes

        public void InitOrderNotes()
        {
            OrderNotes = _raw.GetOrderNotes();
        }

        #endregion

        #region InitPaymentLogs

        public void InitPaymentLogs()
        {
            PaymentLogs = _raw.GetPaymentLogs();
        }

        #endregion

        #region InitBackOrderDetails

        public void InitBackOrderDetails()
        {
            BackOrderDetails = _raw.GetBackOrderDetails();

            BackOrderDetails.Action((x, i) => x.Iseq = i);
        }

        #endregion

        #region Validate

        public static ValidationResult Validate(OrderModel obj, ValidationContext cxt)
        {
            if (obj == null || (cxt != null && cxt.ObjectType == typeof (RecurOrderModel))) return null;

            if (cxt != null) obj.Customer.Fill(obj);

            var db = new DefaultDb();

            if (cxt != null && obj.Id == 0 && obj.DiscountId != null)
            {
                var discount = Singleton<IDiscountMgr>.Default.GetDom(db, obj.DiscountId ?? 0);
                if (discount != null &&
                    0 == DiscountMgr.ListAppliable(db, obj.RawOrderTotal, obj.ItemTotal ?? 0, discount.Id).Count)
                {
                    obj.DiscountId = null;
                }
            }

            if (obj.Id == 0 && cxt == null &&obj.Customer != null && !obj.CouponCodeShared.IsNullOrEmpty())
            {
                var email = obj.Customer.Email;
                var coupon = Singleton<ICouponMgr>.Default.Get(obj.CouponCodeShared);

                if (coupon == null) return new ValidationResult(Core.StringTable.ErrMsgInvalidCouponCode.ModErrLocalize(), new[] { "CouponCode" });

                var orderTotal = obj.RawOrderTotal - obj.TotalDiscount;
                var validOrder = coupon.Validate(db, orderTotal, email);
                var validDetails = (obj.OrderDetails ?? new List<OrderDetailModel>())
                    .Select(x => coupon.Validate(db, null, x.ProductId ?? 0, x.VariantId, orderTotal, email))
                    .ToList();

                if (validOrder == null && validDetails.All(x => x == null))
                    return new ValidationResult(Order.StringTable.CouponContextError, new[] { "CouponCode" });
            }

            if (obj.CouponCode.IsNullOrEmpty()) obj.CouponId = null;
            else
            {
                var coupon = Singleton<ICouponMgr>.Default.Get(obj.CouponCode);
                if (coupon != null)
                {
                    obj.CouponId = coupon.Id;
                    if (cxt != null && obj.Id == 0 && coupon.Validate(db, obj.RawOrderTotal - obj.TotalDiscount, obj.Customer.Email) == null)
                        return new ValidationResult(Order.StringTable.CouponContextError, new[] {"CouponCode"});
                }
                else
                    return new ValidationResult(StringTable.ErrMsgInvalidCouponCode.ModErrLocalize(),
                        new[] {"CouponCode"});
            }

            if (cxt != null && obj.OrderDetails != null)
            {
                var errCoupon = obj.OrderDetails
                    .Where(x => x.CouponId != null)
                    .GroupBy(x => x.CouponId)
                    .Where(x => x.Count() > 1)
                    .Select(x => new
                    {
                        Coupon = Singleton<ICouponMgr>.Default.GetDom(db, x.Key ?? 0),
                        Count = x.Count(),
                        Item = x.First()
                    })
                    .Where(x => ((Coupon) x.Coupon).LimitUsesPerCust >= 0 || ((Coupon) x.Coupon).LimitUsesTotal >= 0)
                    .FirstOrDefault(
                        x =>
                            x.Coupon.Validate(db, obj.DeliverCountryCode, x.Item.ProductId ?? 0, x.Item.VariantId,
                                obj.RawOrderTotal, obj.Customer.Email, x.Count) == null);

                if (errCoupon != null)
                    return new ValidationResult(
                        string.Format(Discount.StringTable.ErrCouponLimitPerCust, errCoupon.Coupon.Code,
                            ((Coupon) errCoupon.Coupon).LimitUsesPerCust), new[] {""});
            }

            return null;
        }

        #endregion

        public byte ExtraService { get; set; }
        public string BringInfo { get; set; }

        public int? ShippingService { get; set; }

        public string ShippingServiceText
        {
            get { return typeof (ShippingServiceType).LocalizeEnumValue(ShippingService ?? 0); }
        }

        public int? PackingService { get; set; }

        public string PackingServiceText
        {
            get { return typeof(PackingServiceType).LocalizeEnumValue(PackingService ?? 0); }
        }

        public string ShippingPickupPoint { get; set; }

        [Display(Name = "Pick-up point")]
        public string ShippingPickupPointFormated
        {
            get { return ShippingPickupPoint.FormatShippingPickupPoint((ShippingServiceType)(ShippingService ?? 0), shipping: _raw.SafeProp(m => m.Shipping)); }
        }

        public string ShippingTrackingUrl { get; set; }

        public string BringBookingData { get; set; }
        public string BringMailBoxData { get; set; }       
        
        public string ProductName { get; set; }
    }
}