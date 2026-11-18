using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Web.Routing;
using System.Web.Security.AntiXss;
using System.Web.UI;
using AutoMapper;
using AutoMapper.Internal;
using Claw.Core.Logs;
using Claw.Modules.Eshop.DAL;
using Claw.Modules.Eshop.Domain;
using Claw.Modules.Eshop.Helper;
using Claw.Modules.Eshop.Models;
using Claw.Modules.Eshop.Payment.Helper;
using Eshop.Payment.DIBS;
using Newtonsoft.Json;
using BasePayment = Eshop.Payments.BasePayment;
using BaseShipping = Eshop.Shipping.BaseProvider;
using Claw.Modules.Eshop.Domain.Interfaces;
using Claw.Modules.Eshop.DAL.Interfaces;
using Logistra = Eshop.Shipping.Logistra;
using Proteria = Eshop.Shipping.Proteria;
using Postnord = Eshop.Shipping.Postnord;
using Eshop.Shipping.Bring;
using Claw.Core;
using System.Web;

namespace Claw.Modules.Eshop
{
    public static class DomExtensions
    {
        #region GetOrderTotal

        public static decimal? GetOrderTotal(this Domain.Order obj, bool? manual = null, bool includeDiscount = true)
        {
            if (obj == null) return null;

            return obj.OrderDetails.Where(x => manual == null || x.Manual == manual).Sum(x => (includeDiscount ? x.FinalPrice : x.PriceInc) * x.Quantity);
        }

        #endregion

        #region IsCalcProductCoupon
        public static bool IsCalcProductCoupon(this Domain.Order obj, bool admin)
        {
            if (obj == null) return false;

            var cpCount = obj.OrderDetails.Count(p => p.CouponValue > 0m || p.DiscountValue > 0m);

            return admin ? cpCount > 0 : cpCount > 0 && obj.OrderDetails.Count > cpCount;
        }
        #endregion

        #region GetVatTotalAfterCoupon

        public static decimal? GetVatTotalAfterCoupon(this Domain.Order obj, out List<System.Tuple<decimal, decimal, decimal, decimal>> taxes, bool admin)
        {
            taxes = null;

            if (obj == null) return null;

            var calcProdCoupon = obj.IsCalcProductCoupon(admin);
            var otherVatSums = obj.OrderDetails
                .Where(x => ((x.CouponValue ?? 0) == 0 && (x.DiscountValue ?? 0) == 0) || !calcProdCoupon)
                .GroupBy(x => x.Vat, (key, lst) =>
                {
                    var lst2 = lst.ToList();
                    return new
                    {
                        Vat = key,
                        Sum = lst2.Sum(w => w.Vat*w.Price*w.Quantity),
                        SumDisc = 0m,
                        TotalIncAfterDisc = admin ? (lst2.Sum(w => w.FinalPrice * w.Quantity) ?? 0) : 0
                    };
                })
                .ToList();

            var cponVatSums = obj.OrderDetails
                .Where(x => (x.CouponValue > 0 || x.DiscountValue > 0) && calcProdCoupon)
                .GroupBy(x => x.Vat, (key, lst) =>
                {
                    var lst2 = lst.ToList();
                    return new
                    {
                        Vat = key,
                        Sum = admin ? 0 : lst2.Sum(w => w.Vat * w.Price * w.Quantity),
                        TotalDisc = (admin ? lst2.Sum(w => (w.DiscountValue + w.CouponValue) * w.Quantity) : 0) ?? 0,
                        TotalIncAfterDisc = (admin ? lst2.Sum(w => w.FinalPrice * w.Quantity) : 0) ?? 0,
                    };
                }).ToList();

            var cponSumInc = calcProdCoupon && !admin ? (obj.OrderDetails.Where(x => x.CouponValue > 0).Sum(x => x.PriceInc * x.Quantity) ?? 0) : 0;
            var couponTotal = calcProdCoupon && !admin ? (obj.OrderDetails.Sum(x => x.CouponValue * x.Quantity) ?? 0) : 0;
            if (couponTotal > 0 && !admin) couponTotal = obj.TotalCoupon;
            var ratio = admin ? 0 : (cponSumInc - couponTotal) / (cponSumInc == 0 ? 1 : cponSumInc);

            var cponVatSumFinal = admin 
                ? Math.Round(cponVatSums.Sum(x => (x.TotalIncAfterDisc * x.Vat) / (1m + x.Vat)), 2, MidpointRounding.AwayFromZero)
                : Math.Round(cponVatSums.Sum(x => x.Sum) * ratio, 2, MidpointRounding.AwayFromZero);

            if (admin)
                taxes = cponVatSums.Select(x => new
                {
                    x.Vat,
                    Sum = Math.Round((x.TotalIncAfterDisc*x.Vat)/(1m + x.Vat), 2, MidpointRounding.AwayFromZero),
                    SumDisc = Math.Round(x.TotalDisc/(1m + x.Vat), 2, MidpointRounding.AwayFromZero),
                    TotalIncAfterDisc = Math.Round(x.TotalIncAfterDisc, 2, MidpointRounding.AwayFromZero)
                })
                .Concat(otherVatSums)
                .GroupBy(x => x.Vat, (key, lst) =>
                {
                    var lst2 = lst.ToList();
                    return new System.Tuple<decimal, decimal, decimal, decimal>(key, lst2.Sum(w => w.Sum), lst2.Sum(w => w.SumDisc), lst2.Sum(w => w.TotalIncAfterDisc));
                }).ToList();
            else
                taxes = cponVatSums.Select(x => new { x.Vat, Sum = Math.Round(x.Sum * ratio, 2, MidpointRounding.AwayFromZero), SumDisc = 0m, TotalIncAfterDisc = 0m })
                         .Concat(otherVatSums)
                         .GroupBy(x => x.Vat, (key, lst) => new System.Tuple<decimal, decimal, decimal, decimal>(key, lst.Sum(w => w.Sum), 0, 0))
                         .ToList();

            return otherVatSums.Sum(x => x.Sum) + Math.Max(0, cponVatSumFinal);
        }

        #endregion

        #region GetAllVatSumAfterCoupDisc

        public static decimal? GetAllVatSumAfterCoupDisc(this Domain.Order obj, out List<System.Tuple<OFloat, OMoney>> taxes, bool inCart = false)
        {
            List<System.Tuple<OFloat, OMoney, OMoney>> taxes0;

            var ret = obj.GetAllVatSumAfterCoupDisc(false, out taxes0, inCart);

            taxes = taxes0 == null ? null : taxes0.Select(x => new System.Tuple<OFloat, OMoney>(x.Item1, x.Item2)).ToList();

            return ret;
        }

        public static decimal? GetAllVatSumAfterCoupDisc(this Domain.Order obj,
            out List<System.Tuple<OFloat, OMoney, OMoney>> taxes, bool inCart = false)
        {
            return obj.GetAllVatSumAfterCoupDisc(false, out taxes, inCart);
        }

        public static decimal? GetAllVatSumAfterCoupDisc(this Domain.Order obj, bool admin, out List<System.Tuple<OFloat, OMoney, OMoney>> taxes, bool inCart = false, bool pos = false)
        {
            taxes = new List<System.Tuple<OFloat, OMoney, OMoney>>();

            if (obj == null) return null;

            var calcProdCoupon = obj.IsCalcProductCoupon(admin);
            var couponTotal = calcProdCoupon ? (obj.OrderDetails.Sum(x => x.CouponValue * x.Quantity) ?? 0) : 0;

            couponTotal = couponTotal > 0 ? 0 : obj.TotalCoupon;

            List<System.Tuple<decimal, decimal, decimal, decimal>> prodTaxes;
            var paymentVatCost = (obj.PaymentCost * obj.PaymentVat) / (1 + obj.PaymentVat);
            var shippingVatCost = (obj.ShippingCost * obj.ShippingVat) / (1 + obj.ShippingVat);
            var vatTotalAfterCoupon = obj.GetVatTotalAfterCoupon(out prodTaxes, admin) ?? 0;
            var subTotal = obj.OrderTotal - obj.VatTotal;

            decimal? ret = 0;
            if (inCart)
            { //show at page fullcart
                ret = subTotal.ComputeVat(obj.VatTotal, obj.VatTotal, 0, 0, 0, 0, obj.TotalDiscount, 0);

                taxes = prodTaxes.Select(x => new System.Tuple<decimal, decimal>(x.Item1, subTotal.ComputeVat(
                obj.VatTotal, x.Item2, 0, 0, 0, 0, obj.TotalDiscount, 0)))
                .Where(x => x.Item1 > 0)
                .OrderBy(x => x.Item1)
                .GroupBy(x => x.Item1, (key, lst) => new System.Tuple<OFloat, OMoney, OMoney>(key, lst.Sum(w => w.Item2), 0m))
                .ToList();

            }
            else
            { //tuca   
                var subTotalInc = obj.OrderDetails.Sum(m => m.FinalPrice*m.Quantity) ?? 0;
                var subTotalAfterDisc = subTotal - (admin ? prodTaxes.Sum(x => x.Item3) : 0);
                ret = subTotalAfterDisc.ComputeVat(vatTotalAfterCoupon, vatTotalAfterCoupon, 0, 0, 0, 0, obj.TotalDiscount - obj.ItemDiscountTotal, couponTotal) 
                    + paymentVatCost + shippingVatCost;

                taxes = prodTaxes.Select(x => new System.Tuple<decimal, decimal, decimal>(
                    x.Item1, subTotalAfterDisc.ComputeVat(vatTotalAfterCoupon, x.Item2, 0, 0, 0, 0, obj.TotalDiscount - obj.ItemDiscountTotal, couponTotal),
                    x.Item4 * (subTotalInc == 0 ? 0 : (subTotalInc - obj.TotalDiscount + obj.ItemDiscountTotal - (obj.Coupon == null || obj.Coupon.DiscountType.InList(CouponType.PercentAndFreeShipping) ? 0m : obj.TotalCoupon)) / subTotalInc)))
                .Concat(new[]
                {
                    new System.Tuple<decimal, decimal, decimal>(obj.PaymentVat ?? 0, paymentVatCost ?? 0, 0m),
                    new System.Tuple<decimal, decimal, decimal>(obj.ShippingVat ?? 0, shippingVatCost ?? 0, 0m)
                })
                .Where(x => x.Item2 != 0 || (pos && x.Item3 != 0))
                .OrderBy(x => x.Item1)
                .GroupBy(x => x.Item1, (key, lst) => new System.Tuple<OFloat, OMoney, OMoney>(key, lst.Sum(w => w.Item2), lst.Sum(w => w.Item3)))
                .ToList();
            }

            if (taxes == null || taxes.Count < 1)
                taxes.Add(new System.Tuple<OFloat, OMoney, OMoney>(-1.0M, 0, 0)); //-1 order out norway

            return ret;
        }
        #endregion

        #region GetTaxStats

        public static List<System.Tuple<OFloat, OMoney, OMoney>> GetTaxStats(this Domain.Order obj, bool pos = false)
        {
            if (obj == null) return new List<System.Tuple<OFloat, OMoney, OMoney>>();

            List<System.Tuple<OFloat, OMoney, OMoney>> lst;

            obj.GetAllVatSumAfterCoupDisc(true, out lst, pos: pos);

            return lst;
        }

        #endregion

        #region GetSplitPayments

        public static List<System.Tuple<PaymentMethod,decimal>> GetSplitPayments(this Domain.Order obj, IDB db)
        {
            if (obj == null || obj.SplitPayment == 0) return new List<System.Tuple<PaymentMethod, decimal>>();
         
            return Enum.GetValues(typeof (SplitPaymentType))
            .Cast<int>().Where(m => 0 != (m & obj.SplitPayment)).Select(v =>
            {
                bool rounding;
                return new
                {
                    PaymentMethodId = v.InList(SplitPaymentType.Cash) ? 18 : v.InList(SplitPaymentType.Gift) ? 20 : v.InList(SplitPaymentType.Credit) ? 19 : 0,
                    FinalTotal = obj.FinalTotal * obj.GetSplitPaymentRate((SplitPaymentType) v, out rounding) + (rounding ? (decimal?)(obj.RoundingUp ?? 0) : 0)
                };
            })
            .Select(m => new System.Tuple<PaymentMethod, decimal>(PaymentMethodMgr.GetDom(db, m.PaymentMethodId), m.FinalTotal ?? 0)).ToList();
        } 

        #endregion

        #region GetSplitPaymentRate

        public static decimal GetSplitPaymentRate(this Domain.Order obj, SplitPaymentType type)
        {
            bool rounding;
            return obj.GetSplitPaymentRate(type, out rounding);
        }

        public static decimal GetSplitPaymentRate(this SplitPaymentType type, int splitPayment, string splitPayData,
            decimal finalTotal, decimal roundingUp)
        {
            bool rounding;
            return GetSplitPaymentRate(type, splitPayment, splitPayData, finalTotal, roundingUp, out rounding);
        }

        public static decimal GetSplitPaymentRate(this Domain.Order obj, SplitPaymentType type, out bool rounding)
        {
            return GetSplitPaymentRate(type, obj.SplitPayment, obj.SplitPayData, (obj.FinalTotal ?? 0), (decimal)(obj.RoundingUp ?? 0), out rounding);
        }

        public static decimal GetSplitPaymentRate(this SplitPaymentType type, int splitPayment, string splitPayData, decimal finalTotal,
            decimal roundingUp, out bool rounding)
        {
            rounding = false;
            const StringComparison cmpF = StringComparison.OrdinalIgnoreCase;
            
            if ((splitPayment & (int)type) == 0 || finalTotal == 0) return 0;

            var patt = "";
            var data = splitPayData ?? "";

            switch (type)
            {
                case SplitPaymentType.Cash:
                    patt = "cash:"; 
                    rounding = true;
                    break;
                case SplitPaymentType.Credit:
                    patt = "credit:";
                    rounding = data.IndexOf("cash:", cmpF) == -1 && data.IndexOf("gift:", cmpF) == -1;
                    break;
                case SplitPaymentType.Gift:
                    patt = "gift:";  
                    rounding = data.IndexOf("cash:", cmpF) == -1;
                    break;
                default:
                    return 0;
            }

            var idx = data.IndexOf(patt, cmpF) + patt.Length;
            var value = decimal.Parse(data.Substring(idx, data.IndexOf(';', idx) - idx), CultureInfo.InvariantCulture);

            return (value - (rounding ? roundingUp : 0)) / finalTotal;
        }

        #endregion

        #region RoundPrice

        public static void RoundPrice(this OrderModel obj, IDB db, Predicate<OrderDetailModel> fFilter = null)
        {
            if (obj == null || obj.OrderDetails.SafeCount() == 0) return;

            var vatEnabled = DAL.CountryMgr.IsVatEnable(db, obj.DeliverCountryId ?? 0);
            var setting = new OMoneyMc.Setting()
            {
                Currency = CurrencyMgr.GetOrDefault(obj.Currency),
                Roundable = true,
                SkipRate = true,
            };

            foreach (var detail in obj.OrderDetails.Where(m => fFilter == null || fFilter(m)))
            {
                var vat = new Vat() { Value = vatEnabled ? detail.Vat : 0 };

                if (obj.Id == 0 && !detail.OriginPriceInc.IsNull())
                {
                    detail.OriginPriceInc = new OMoneyMc(detail.OriginPriceInc, setting).Value;
                    //detail.PriceInc = Math.Max(0, Math.Min(detail.PriceInc, detail.OriginPriceInc));
                }

                detail.PriceInc = new OMoneyMc(detail.PriceInc, setting).Value;
                detail.Price = vat.ExcVat(detail.PriceInc) ?? 0;
            }
        }

        #endregion

        #region GetVatTotal

        public static decimal? GetVatTotal(this Domain.Order obj)
        {
            if (obj == null) return null;

            return obj.OrderDetails.Sum(x => x.Vat * x.Price * x.Quantity);
        }

        #endregion

        #region GetCategory

        public static CategoryModel GetCategory(this OrderDetailModel obj)
        {
            if (obj == null || obj.GetRaw() == null) return null;

            var itm = obj.GetRaw().Product.CategoryProducts.FirstOrDefault();

            return itm == null ? null : Mapper.Map<Category, CategoryModel>(itm.Category);
        }

        #endregion

        #region GetTotalWeight
        public static decimal? GetTotalWeight(this Domain.Order obj)
        {
            if (obj == null || obj.OrderDetails.Count == 0) return null;

            var settings = SettingsBiz.GetSettings();

            return obj.OrderDetails.Sum(x => x.Quantity * x.ProductWeight.ToKg((WeightUnit)x.ProductWUnit));
        }
        public static decimal? GetTotalWeight(this OrderModel obj, IDB db)
        {
            if (obj == null || obj.OrderDetails.Count == 0) return null;

            return obj.OrderDetails.Sum(x =>
            {
                var prod = Product.DAL.ProductMgr.GetDom(db, x.ProductId ?? 0);
                var vari = Product.DAL.VariantMgr.GetDom(db, x.VariantId ?? 0);
                return prod == null ? 0 : x.Quantity * prod.WeightInKg(null, vari);
            });
        }
        #endregion

        #region ToCurrency
        public static Currency ToCurrency(this OrderModel order)
        {
            if (order == null) return null;

            var currency = CurrencyMgr.GetOrDefault(order.Currency);

            return new Currency()
            {
                Code = order.Currency ?? currency.Code,
                Token = currency.Token,
                Rate = order.ExchangeRate ?? currency.Rate,
                Active = currency.Active,
                DecimalPlaces = currency.DecimalPlaces,
                DecimalSep = currency.DecimalSep,
                ThousandSep = currency.ThousandSep,
            };
        }
        #endregion

        #region ToOMoneyMc

        public static OMoneyMc ToOMoneyMc(this decimal value, IMiniCart cart = null, bool? showUnit = null,
            bool? roundable = false, bool skipRate = false)
        {
            var setting = (cart == null ? null : cart.MultiCurrencySetting) ?? new OMoneyMc.Setting();

            if (showUnit != null) setting.ShowUnit = showUnit.Value;

            if (roundable != null) setting.Roundable = roundable.Value;

            setting.SkipRate = skipRate;

            return new OMoneyMc(value, setting);
        }

        public static OMoneyMc ToOMoneyMc(this decimal? value, IMiniCart cart = null, bool? showUnit = null,
            bool? roundable = false, bool skipRate = false)
        {
            if (value == null) return new OMoneyMc();
            else return ToOMoneyMc(value.Value, cart, showUnit, roundable, skipRate);
        }

        public static OMoneyMc ToOMoneyMc(this decimal? value, Domain.Order order)
        {
            return value.ToOMoneyMc(new OrderModel() {Currency = order.Currency});
        }

        public static OMoneyMc ToOMoneyMc(this decimal? value, OrderModel order, bool showUnit = true,
            bool skipRate = true)
        {
            if (value == null || order == null) return new OMoneyMc();

            var cxt = HttpContext.Current;
            var key = string.Format("OMoneyMc_Setting_{0}", order.Currency);

            var setting = cxt != null && cxt.Items.Contains(key) ? (OMoneyMc.Setting)cxt.Items[key]: null;
            if (setting == null)
            {
                setting = new OMoneyMc.Setting(order.GetCurrency())
                {
                    ShowInCart = true,
                    ShowOriginal = true,
                    ShowUnit = showUnit
                };
                if (cxt != null) cxt.Items[key] = setting;
            }

            if (skipRate) 
            {
                setting.Currency = setting.Currency.Clone();
                setting.Currency.Rate = 1m; 
            }

            return new OMoneyMc(value, setting);
        }

        public static OMoneyMc ToCeillingOMoneyMc(this decimal value, bool? roundable = true, bool? showUnit = true)
        {
            var setting = new OMoneyMc.Setting();
            setting.ShowUnit = showUnit.HasValue ? showUnit.Value : true;
            setting.ShowInCart = true;
            var settings = SettingsBiz.GetSettings();
            setting.Roundable = roundable.Value && settings.Display.Product.PriceRound;
            return new OMoneyMc(value, setting);
        }

        public static OMoneyMc ToOMoneyMc(this decimal value, bool? roundable = false, bool skipRate = false)
        {
            return value.ToOMoneyMc(null, null, roundable, skipRate);
        }

        public static OMoneyMc ToOMoneyMc(this decimal? value, bool? roundable = false, bool skipRate = false)
        {
            return value.ToOMoneyMc(null, null, roundable, skipRate);
        }
        #endregion

        #region ToBaseCurrencyCeilling
        public static decimal ToBaseCurrencyCeilling(this decimal value, bool? roundable = true)
        {
            var settings = SettingsBiz.GetSettings();
            bool ceilling = roundable.Value && settings.Display.Product.PriceRound;
            value = value.ToOMoneyMc(null, null, ceilling).Value.Value;
            var currency = CurrencyMgr.FocusCurrency;
            if (currency.Rate > 0)
            {
                value = value / currency.Rate;
            }
            return value;
        }
        #endregion

        #region GetShipOrderTotal

        public static decimal? GetShipOrderTotal(this Domain.Order obj, string countryCode, IDB db = null)
        {
            if (obj == null) return null;

            var vatEnabled = CountryMgr.IsVatEnable(db ?? new DefaultDb(), countryCode);

            return obj.OrderDetails.Sum(x =>
                (x.Product?.ShippingType ?? 0).InList(ShippingType.FreeShipping, ShippingType.Download) ? 0 : (vatEnabled ? (x.PriceInc ?? 0) : x.Price) * x.Quantity);
        }

        public static decimal? GetShipOrderTotal(this OrderModel obj, string countryCode, IDB db = null)
        {
            if (obj == null) return null;

            var vatEnabled = CountryMgr.IsVatEnable(db ?? new DefaultDb(), countryCode);

            return obj.OrderDetails.Sum(x =>
                (x.ProductShippingType ?? 0).InList(ShippingType.FreeShipping, ShippingType.Download) ? 0 : (vatEnabled ? (x.PriceInc ?? 0) : x.Price) * x.Quantity);
        }

        #endregion

        #region GetRawOrderTotal

        public static decimal? GetRawOrderTotal(this Domain.Order obj, string countryCode, IDB db = null, bool? manual = null)
        {
            return GetRawOrderTotal(obj, countryCode, db, manual, null);
        }

        public static decimal? GetRawOrderTotal(this Domain.Order obj, string countryCode, IDB db, bool? manual, int? orderDetailId)
        {
            if (obj == null) return null;

            if (CountryMgr.IsVatEnable(db ?? new DefaultDb(), countryCode))
                return obj.OrderDetails.Where(x => (manual == null || x.Manual == manual) && (orderDetailId == null || x.Id == orderDetailId)).Sum(x => (x.PriceInc ?? 0) * x.Quantity);

            return obj.OrderDetails.Where(x => (manual == null || x.Manual == manual) && (orderDetailId == null || x.Id == orderDetailId)).Sum(x => x.Price * x.Quantity);
        }

        public static decimal? GetRawOrderTotal(this OrderModel obj, string countryCode)
        {
            if (obj == null) return null;

            if (CountryMgr.IsVatEnable(countryCode))
                return obj.OrderDetails.Sum(x => (x.PriceInc ?? 0) * x.Quantity);

            return obj.OrderDetails.Sum(x => (x.Price ?? 0) * x.Quantity);
        }

        #endregion

        #region GetShipWeightTotal
        /// <summary>Kg</summary>
        public static decimal? GetShipWeightTotal(this Domain.Order obj)
        {
            if (obj == null) return null;

            return obj.OrderDetails.Sum(x =>
                (x.Product?.ShippingType ?? 0).InList(ShippingType.FreeShipping, ShippingType.Download)
                    ? 0 : x.ProductWeight.ToKg((WeightUnit)x.ProductWUnit) * x.Quantity);
        }

        public static decimal? GetShipWeightTotal(this OrderModel obj, IDB db)
        {
            if (obj == null) return null;

            return obj.OrderDetails.Sum(x =>
            {
                var prod = Product.DAL.ProductMgr.GetDom(db, x.ProductId ?? 0);
                if (prod == null || prod.ShippingType.InList(ShippingType.FreeShipping, ShippingType.Download)) return 0m;
                else return prod.Weight.ToKg(((WeightUnit?)prod.WUnit) ?? WeightUnit.Kg) * x.Quantity;
            });
        }

        #endregion

        #region GetShipExtraCostTotal

        public static decimal GetShipExtraCostTotal(this Domain.Order obj)
        {
            if (obj == null) return 0;

            return obj.OrderDetails.Sum(x => (x.Product?.ShippingCostExtra ?? 0)*x.Quantity);
        }

        public static decimal GetShipExtraCostTotal(this OrderModel obj, IDB db)
        {
            if (obj == null) return 0;

            return obj.OrderDetails.Sum(x =>
            {     
                var prod = Product.DAL.ProductMgr.GetDom(db, x.ProductId ?? 0);
                return prod == null ? 0 : (prod.ShippingCostExtra ?? 0)*(x.Quantity ?? 0);
            });
        } 

        #endregion

        #region GetCouponTotal

        public static decimal? GetCouponTotal(this Domain.Order obj)
        {
            if (obj == null) return null;

            return (obj.DiscountValue < 0 || obj.CouponValue >= 0 ? Math.Max(0, obj.CouponValue ?? 0) : obj.ShippingCost) +
                   obj.OrderDetails.Sum(x => Math.Max(0, x.CouponValue ?? 0) * x.Quantity);
        }

        #endregion

        #region GetDiscountTotal

        public static decimal? GetDiscountTotal(this Domain.Order obj)
        {
            if (obj == null) return null;

            return (obj.DiscountValue < 0 ? obj.ShippingCost : obj.DiscountValue) +
                   obj.OrderDetails.Sum(x => Math.Max(0, x.DiscountValue ?? 0));
        }

        public static decimal? GetDiscountTotal(this OrderModel obj)
        {
            if (obj == null) return null;

            return (obj.DiscountValue < 0 ? obj.ShippingCost : obj.DiscountValue) +
                obj.OrderDetails.Sum(x => Math.Max(0, x.DiscountValue ?? 0));
        }

        #endregion

        #region TryApplyCoupon

        public static bool TryApplyCoupon(this OrderModel obj, IDB db)
        {
            ICoupon coupon;

            if (obj == null || obj.CouponCodeShared.IsNullOrEmpty() || obj.ExchangeRate == 0m ||
                null == (coupon = Singleton<ICouponMgr>.Default.Get(obj.CouponCodeShared))) return false;

            var taxCountryCode = obj.GetTaxCountryCode();
            var exchangeRate = ((decimal?)obj.ExchangeRate) ?? 1m;
            var rawOrderTotal = (obj.GetRawOrderTotal(taxCountryCode) ?? 0) / exchangeRate;
            var discountTotal = (obj.GetDiscountTotal() ?? 0) / exchangeRate;

            var amount = coupon.Validate(db, rawOrderTotal - discountTotal, obj.Customer.Email);

            if (amount != null)
            {
                obj.CouponId = coupon.Id;
                obj.CouponCode = coupon.Code;
                obj.CouponValue = exchangeRate * amount;

                return true;
            }
            else
            {
                obj.CouponId = null;
                obj.CouponCode = null;
                obj.CouponValue = 0;
            }

            var retCode = false;
            if (obj.OrderDetails.SafeCount() != 0)
            {
                var vatEnabled = CountryMgr.IsVatEnable(string.IsNullOrEmpty(taxCountryCode) ? "no" : taxCountryCode);
                foreach (var odetail in obj.OrderDetails)
                {
                    if (null != (amount = coupon.Validate(db, taxCountryCode, odetail.ProductId ?? 0, odetail.VariantId,
                        rawOrderTotal - discountTotal, obj.Customer.Email, 1, odetail.DiscountValue, vatEnabled ? odetail.PriceInc : odetail.Price)))
                    {
                        odetail.CouponId = coupon.Id;
                        odetail.CouponCode = coupon.Code;
                        odetail.CouponValue = exchangeRate * amount;
                        retCode = true;
                    }
                    else
                    {
                        odetail.CouponId = null;
                        odetail.CouponCode = null;
                        odetail.CouponValue = 0;
                    }
                }
            }

            return retCode;
        }

        #endregion

        #region TryApplyDiscount

        public static bool TryApplyDiscount(this OrderModel obj, IDB db)
        {
            if (obj == null || obj.ExchangeRate == 0m) return false;

            var customer = CustomerMgr.Get(db, obj.Customer.Email);
            var custgroupIds = customer == null ? null : customer.CustomerCGroups.Select(x => x.Group.Id).ToList();
            var taxCountryCode = obj.GetTaxCountryCode();
            var exchangeRate = ((decimal?)obj.ExchangeRate) ?? 1m;
            var rawOrderTotal = (obj.GetRawOrderTotal(taxCountryCode) ?? 0) / exchangeRate;
            var shippingFee = obj.ShippingCost / exchangeRate;

            var lst = Singleton<IDiscountMgr>.Default.ListAppliable(db, rawOrderTotal, obj.OrderDetails.SafeCount(), null, custgroupIds);
            var itm = lst.Select(m =>
            {
                var amt = m.Validate(rawOrderTotal, obj.OrderDetails.SafeCount());
                return new { Amount = amt == -1 ? shippingFee : amt, Discount = m };
            }).Where(m => m.Discount != null).OrderByDescending(m => m.Amount).Take(1).FirstOrDefault();

            if (itm != null)
            {
                obj.DiscountId = itm.Discount.Id;
                obj.DiscountValue = exchangeRate * itm.Amount;
                return true;
            }
            else
            {
                obj.DiscountId = null;
                obj.DiscountValue = 0;
            }

            return false;
        }

        #endregion

        #region GetShippingFee

        public static decimal GetShippingFee(this OrderModel obj, IDB db, bool incExchangeRate = false)
        {
            if (obj == null) return 0;

            var taxCountryCode = obj.GetTaxCountryCode();
            var exchangeRate = ((decimal?)obj.ExchangeRate) ?? 1m;
            var rawOrderTotal = (obj.GetShipOrderTotal(taxCountryCode) ?? 0) / exchangeRate;
            var shipping = Singleton<IShippingMgr>.Default.GetDom(db, obj.ShippingId ?? 0);

            return shipping == null ? 0 : (incExchangeRate ? exchangeRate : 1) * shipping.GetFee(rawOrderTotal, obj.GetShipWeightTotal(db) ?? 0, taxCountryCode, null, obj.GetShipExtraCostTotal(db));
        }

        public static decimal GetShippingFee(this Domain.Order obj, bool incExchangeRate = false, IDB db = null)
        {
            if (obj == null) return 0;

            var taxCountryCode = obj.GetTaxCountryCode();
            var exchangeRate = obj.ExchangeRate ?? 1m;
            var rawOrderTotal = (obj.GetShipOrderTotal(taxCountryCode, db) ?? 0) / exchangeRate;
            var shipping = obj.Shipping;

            return shipping == null ? 0 : (incExchangeRate ? exchangeRate : 1) * shipping.GetFee(rawOrderTotal, obj.GetShipWeightTotal() ?? 0, taxCountryCode, db, obj.GetShipExtraCostTotal());
        }

        #endregion

        #region CanMakeRest

        public static bool CanMakeRest(this Domain.Order obj)
        {
            if (obj == null || obj.OrderStatus.InList(OrderStatus.Sent, OrderStatus.Cancelled, OrderStatus.Credit,
                OrderStatus.PaymentWaiting, OrderStatus.RestOrder)) return false;

            return obj.GetBackOrderDetails().SafeCount() != 0 && obj.GetPaymentData().SafeInvokeMethodEx<bool>("CanCapturePartial", true);
        }
        #endregion

        #region InitMandatoryInfo
        public static void InitMandatoryInfo(this OrderModel obj, IDB db = null)
        {
            if (obj == null || obj.GetRaw() == null) return;

            var raw = obj.GetRaw();

            obj.CanReturn = raw.CanReturn();
            obj.CanCancel = raw.CanCancel();
            obj.CanMakeRest = raw.CanMakeRest();
            obj.TaxStats = raw.GetTaxStats().Select(x => new System.Tuple<OFloat, OMoney, OMoney>(x.Item1, x.Item2, x.Item3)).ToList();
            obj.SplitPaymentMethods = db == null ? null : raw.GetSplitPayments(db).Select(x => new System.Tuple<PaymentMethodModel, OMoney>(Mapper.Map<PaymentMethod, PaymentMethodModel>(x.Item1), x.Item2)).ToList();
            obj.BackOrderIds = raw.GetBackOrderIds();

            obj.TotalWeight = raw.GetTotalWeight();
            obj.DownloadContents = raw.GetDownloadContents();

            if (obj.Customer != null && raw.Customer != null)
                obj.Customer.StatsInfo = raw.Customer.GetStatsInfo(db);

            if (obj.CouponCodeShared.IsNullOrEmpty())
            {
                obj.CouponCodeShared = raw.GetSharedCoupon(x => x.Code);
                obj.CouponNameShared = raw.GetSharedCoupon(x => x.Name);
            }
        }
        #endregion

        #region Validate

        public static bool Validate(this OrderDetailModel obj, IDB db, out int stockQty,
            int qtyDelta = 0, bool fillInfo = false)
        {
            stockQty = 0;

            if (obj == null) return false;
            if (qtyDelta < 0) return true;

            var product = Product.DAL.ProductMgr.GetDom(db, obj.ProductId ?? 0);
            var variant = Product.DAL.VariantMgr.GetDom(db, obj.VariantId ?? 0);
            var quantity = obj.Id == 0 ? obj.Quantity : qtyDelta;

            if (fillInfo)
            {
                obj.ProductName = product.GetName(variant);
                obj.Vat = product.Vat.Value;
            }

            if (product == null || (variant == null && obj.VariantId > 0)) return false;

            if (product.AllowBackorder) return true;

            stockQty = variant != null ? variant.Quantity : product.Quantity;

            return stockQty >= quantity;
        }

        #endregion

        #region ToEDI

        public static string ToEDI(this Domain.Order obj)
        {
            if (obj == null) return String.Empty;
            var fristName = "";
            string lastName;
            if (obj.DeliverName.IndexOf(" ", StringComparison.Ordinal) > 0)
            {
                var index = obj.DeliverName.IndexOf(" ", StringComparison.Ordinal);
                fristName = obj.DeliverName.Substring(0, index);
                lastName = obj.DeliverName.Substring(index + 1);
            }
            else
            {
                lastName = obj.DeliverName;
            }
            var strEDI = new List<string>()
            {
                string.Format("\"{0}\"",obj.Id),
                string.Format("\"{0}\"",obj.FinalTotal.RoundEx(2)),
                string.Format("\"{0}\"",obj.Customer!=null?obj.Customer.Id:0),
                string.Format("\"{0} {1}\"",fristName,lastName),
                string.Format("\"{0}\"",obj.Customer!=null?obj.Customer.Email:string.Empty),
                string.Format("\"{0}\"",obj.DeliverAddress),
                string.Format("\"{0}\"",""),
                string.Format("\"{0}\"",!string.IsNullOrEmpty(obj.DeliverMobile)?obj.DeliverMobile:obj.DeliverPhone),
                string.Format("\"{0}\"",obj.DeliverPostnr),
                string.Format("\"{0}\"",obj.DeliverPosted),
            };

            return strEDI.ToString(",");
        }

        #endregion

        #region ToProteria

        public static string ToProteria(this Domain.Order obj)
        {
            if (obj == null) return String.Empty;
            var fristName = "";
            string lastName;
            if (obj.DeliverName.IndexOf(" ", StringComparison.Ordinal) > 0)
            {
                var index = obj.DeliverName.IndexOf(" ", StringComparison.Ordinal);
                fristName = obj.DeliverName.Substring(0, index);
                lastName = obj.DeliverName.Substring(index + 1);
            }
            else
            {
                lastName = obj.DeliverName;
            }
            var strEDI = new List<string>()
            {
                string.Format("{0} ",obj.Id),
                string.Format("{0} ",obj.PaymentMethod.PaymentIsRegistered?0:obj.FinalTotal.RoundEx(2)),
                string.Format("{0} ",0),
                string.Format("{0} ",obj.Customer!=null?obj.Customer.Id:0),
                string.Format("{0} {1} ",fristName,lastName),
                string.Format("{0} ",obj.DeliverAddress),
                string.Format("{0} ",""),
                string.Format("{0} ",obj.DeliverPostnr),
                string.Format("{0} ",obj.DeliverPosted),
                string.Format("{0} ",obj.Customer!=null?obj.Customer.Email:""),
                string.Format("{0} ",obj.DeliverMobile),
                string.Format("{0} ",obj.DeliverPhone),
                string.Format("{0} ;",""),

            };

            return strEDI.ToString(";");
        }

        #endregion

        #region GetFinalPrice
        public static decimal? GetFinalPrice(this OrderDetail obj)
        {
            if (obj == null) return null;

            return Math.Max(0m, (obj.Price*(1.0m + obj.Vat) - obj.DiscountValue - obj.CouponValue) ?? 0);
        }
        #endregion

        #region GetReservationId
        public static string GetReservationId(this Domain.Order obj)
        {
            var dt = obj.CreatedDate;
            var idVal = ((uint)(obj.Id & 0xffff) ^ 0xaf2d).ToBase36();
            var yearLetter = ((dt.Year - 2010) % 36).ToBase36();
            var random = new Random();
            var randVal = random.Next(0, 35).ToBase36() + random.Next(0, 35).ToBase36() + random.Next(0, 35).ToBase36();

            return string.Format("{1}{2:x}{3}{4}{0}{5}", idVal, yearLetter,
                dt.Month, dt.Day.ToBase36(), (dt.Hour % 12).ToBase36(), randVal);
        }
        #endregion

        #region GetTaxCountryCode

        public static string GetTaxCountryCode(this Domain.Order obj)
        {
            return obj != null && obj.DeliverCountry != null ? obj.DeliverCountry.Code : string.Empty;
        }

        public static string GetTaxCountryCode(this OrderModel obj)
        {
            if (obj == null || obj.DeliverCountryId == null) return string.Empty;

            var country = CountryMgr.GetDom(new DefaultDb(), obj.DeliverCountryId ?? 0);

            return country != null ? country.Code : string.Empty;
        }

        #endregion

        #region GetCustGroups

        public static List<CustomerCGroupModel> GetCustGroups(this Customer obj, IDB db)
        {
            if (obj == null) return new List<CustomerCGroupModel>();

            var lst = obj.CustomerCGroups.ToList();

            return CustGroupMgr.GetAll(db).Select(x =>
            {
                var itm = lst.FirstOrDefault(m => m.Group.Id == x.Id);
                return new CustomerCGroupModel()
                {
                    Id = itm != null ? itm.Id : 0,
                    Selected = itm != null,
                    GroupId = x.Id,
                    GroupName = x.Name,
                    CustomerId = obj.Id,
                };
            }).ToList();
        }

        #endregion

        #region GetOrderDetails

        public static List<OrderDetailModel> GetOrderDetails(this Domain.Order obj, bool includePayShip = false)
        {
            return obj.GetOrderDetails(includePayShip, null);
        }

        public static List<OrderDetailModel> GetOrderDetails(this Domain.Order obj, bool includePayShip, IDB db)
        {
            if (obj == null) return new List<OrderDetailModel>();

            if (SettingsBiz.GetSettings().Hidden.ShoppingProduct)
            {
                db = db ?? new DefaultDb();
                ShoppingProductMgr.BindStats(db, obj.OrderDetails.Where(m => m.Variant == null).Select(m => m.Product).ToList());
                ShoppingProductMgr.BindStats(db, obj.OrderDetails.Select(m => m.Variant).Where(m => m != null).ToList());
            }

            var lst = Mapper.Map<List<OrderDetail>, List<OrderDetailModel>>(obj.OrderDetails.ToList());
            var credit = obj.OrderStatus.InList(OrderStatus.Credit);

            if (credit || includePayShip)
            {
                if (obj.PaymentCost != 0 || includePayShip)
                    lst.Add(new OrderDetailModel()
                    {
                        Id = -1,
                        OrderId = obj.Id,
                        ProductId = includePayShip ? (int?)null : -1,
                        Quantity = credit ? -1 : 1,
                        Vat = obj.PaymentVat,
                        Price = new Vat() { Value = obj.PaymentVat ?? 0 }.ExcVat(Math.Abs(obj.PaymentCost ?? 0)),
                        PriceInc = Math.Abs(obj.PaymentCost ?? 0),
                        ProductName = obj.PaymentName,
                    });

                if (obj.ShippingCost != 0 || includePayShip)
                    lst.Add(new OrderDetailModel()
                    {
                        Id = -2,
                        OrderId = obj.Id,
                        ProductId = includePayShip ? (int?)null : -2,
                        Quantity = credit ? -1 : 1,
                        Vat = obj.ShippingVat,
                        Price = new Vat() { Value = obj.ShippingVat ?? 0 }.ExcVat(Math.Abs(obj.ShippingCost ?? 0)),
                        PriceInc = Math.Abs(obj.ShippingCost ?? 0),
                        ProductName = obj.ShippingName
                    });
            }

            return lst;
        }

        #endregion

        #region GetBackOrderDetails

        public static List<BackOrderDetailModel> GetBackOrderDetails(this Domain.Order obj)
        {
            if (obj == null) return new List<BackOrderDetailModel>();

            var odDataJson = SettingsBiz.GetSettings().Hidden.OrderDetailDataJson;
            var ret = obj.OrderDetails.Select(x =>
            {
                var itm = obj.BackOrderDetails.FirstOrDefault(w => w.OrderDetail.Id == x.Id);
                return new BackOrderDetailModel()
                {
                    Raw = x,
                    UniqueKey = OrderDetailModel.GetUniqueKey(x.Product.Id, x.Variant.GetId(), x.Price, odDataJson ? x.Data : null),
                    Id = itm == null ? 0 : itm.Id,
                    Quantity = itm == null ? 0 : itm.Quantity,
                    OrderId = x.Order.Id,
                    OrderDetailId = x.Id,
                    OriginQty = x.QuantityOri,
                    Price = x.Price,
                    PriceInc = x.PriceInc,
                    ProductName = x.GetRecurInfoDesc(),
                    ProductSKU = x.Product.CombinedSKU(x.Variant),
                    ProductImage = x.Product.GetImage(x.Variant),
                    ProductLocation = x.Product.Location,
                };
            }).ToList();

            var backOrders = obj.BackOrders.Cast<Domain.Order>()
                .SelectMany(x => x.GetOrderDetails())
                .GroupBy(x => x.GetUniqueKey(obj, odDataJson: odDataJson), (key, lst) => new { Key = key, SumQty = lst.Sum(w => Math.Abs(w.Quantity ?? 0)) })
                .Select(x => new DAL.Tuple2<string, int>(x.Key, x.SumQty)).ToList();

            foreach (var itm in ret.Join(backOrders, o => o.UniqueKey, c => c.Item1,
                (o, c) => new { BackOrder = c, Origin = o }))
            {
                itm.Origin.OriginQty -= itm.BackOrder.Item2;
                itm.BackOrder.Item2 = Math.Abs(Math.Min(0, itm.Origin.OriginQty ?? 0));
                itm.Origin.OriginQty = Math.Max(0, itm.Origin.OriginQty ?? 0);
            }

            return ret.Where(x => x.OriginQty > 0).ToList();
        }

        #endregion

        #region GetCreditOrderDetails

        public static List<CreditOrderDetailModel> GetCreditOrderDetails(this Domain.Order obj)
        {
            if (obj == null || obj.OrderDetails.SafeCount() == 0 || obj.OrderTotal == 0)
                return new List<CreditOrderDetailModel>();

            var settings = SettingsBiz.GetSettings();
            var odDataJson = settings.Hidden.OrderDetailDataJson;
            var orderTotal = obj.GetOrderTotal(false);
            var shippingCost = obj.ShippingCost;
            var paymentCost = obj.PaymentCost;
            var giftCardTotalAfterPayShip = 0m;//obj.GiftCardTotal;

            if (giftCardTotalAfterPayShip >= paymentCost && paymentCost > 0)
            {
                giftCardTotalAfterPayShip -= paymentCost ?? 0; //paymentCost = 0;
            }
            if (giftCardTotalAfterPayShip >= shippingCost && shippingCost > 0)
            {
                giftCardTotalAfterPayShip -= shippingCost ?? 0; //shippingCost = 0;
            }

            var giftcardAmount = obj.OrderDetails.Where(m => m.Product.Id == settings.Hidden.GiftCardProductId).Sum(m => m.Quantity * (m.PriceInc ?? 0));
            var ratio = orderTotal - giftcardAmount + obj.DepositTotal == 0 ? 0 : (orderTotal - giftcardAmount + obj.DepositTotal - obj.TotalDiscount + obj.ItemDiscountTotal - (obj.Coupon != null && !obj.Coupon.DiscountType.InList(CouponType.PercentAndFreeShipping) ? obj.TotalCoupon : 0) - giftCardTotalAfterPayShip) / (orderTotal - giftcardAmount + obj.DepositTotal);

            var ret = obj.OrderDetails.Where(m => !m.Product.Deleted && m.Product.Id != settings.Hidden.GiftCardProductId && !m.Variant.SafeProp(z => z.Deleted, false)).Select(x =>
            {
                var newPriceInc = x.Manual ? (x.FinalPrice ?? 0) : Math.Round(((x.FinalPrice ?? 0) + (x.DepositAmount ?? 0)) * (ratio ?? 0), 2);
                return new CreditOrderDetailModel()
                {
                    Raw = x,
                    UniqueKey = OrderDetailModel.GetUniqueKey(x.Product.Id, x.Variant.GetId(), x.Price, odDataJson ? x.Data : null),
                    Quantity = 0,
                    OrderId = x.Order.Id,
                    OrderDetailId = x.Id,
                    OriginQty = x.Quantity,
                    Price = Math.Round(new Vat() { Value = x.Vat }.ExcVat(newPriceInc) ?? 0, 2),
                    PriceInc = newPriceInc,
                    ProductName = x.GetRecurInfoDesc(),
                    ProductSKU = x.Product.CombinedSKU(x.Variant),
                };
            }).ToList();

            if (paymentCost != 0 && !obj.PaymentName.IsNullOrEmpty())
            {
                var payPrice = new Vat() { Value = obj.PaymentVat ?? 0 }.ExcVat(paymentCost);
                ret.Add(new CreditOrderDetailModel()
                {
                    UniqueKey = OrderDetailModel.GetUniqueKey(-1, null, payPrice),
                    OrderDetailId = -1,
                    OrderId = obj.Id,
                    OriginQty = 1,
                    Quantity = 0,
                    Price = payPrice,
                    PriceInc = paymentCost,
                    ProductName = obj.PaymentName
                });
            }

            if (shippingCost != 0 && !obj.ShippingName.IsNullOrEmpty())
            {
                var shipPrice = new Vat() { Value = obj.ShippingVat ?? 0 }.ExcVat(shippingCost);
                ret.Add(new CreditOrderDetailModel()
                {
                    UniqueKey = OrderDetailModel.GetUniqueKey(-2, null, shipPrice),
                    OrderDetailId = -2,
                    OrderId = obj.Id,
                    OriginQty = 1,
                    Quantity = 0,
                    Price = shipPrice,
                    PriceInc = shippingCost,
                    ProductName = obj.ShippingName
                });
            }

            var credits = obj.OriginCredits
                .SelectMany(x => x.GetOrderDetails())
                .GroupBy(x => x.GetUniqueKey(null, true, odDataJson), (key, lst) => new { Key = key, SumQty = lst.Sum(w => Math.Abs(w.Quantity ?? 0)) })
                .Select(x => new DAL.Tuple2<string, int>(x.Key, x.SumQty)).ToList();

            foreach (var itm in ret.Join(credits, o => o.UniqueKey, c => c.Item1,
                (o, c) => new { Credit = c, Origin = o }))
            {
                itm.Origin.OriginQty -= itm.Credit.Item2;
                itm.Credit.Item2 = Math.Abs(Math.Min(0, itm.Origin.OriginQty ?? 0));
                itm.Origin.OriginQty = Math.Max(0, itm.Origin.OriginQty ?? 0);
            }

            return ret.Where(x => x.OriginQty > 0).ToList();
        }

        #endregion

        #region IsFreeShipping

        public static bool IsFreeShipping(this Domain.Order obj)
        {
            if (obj == null || (obj.Coupon == null && obj.Discount == null)) return false;

            return (obj.Coupon != null && obj.Coupon.Validate(new DefaultDb(), obj.OrderTotal, obj.Customer.Email) == -1) ||
                   (obj.Discount != null && obj.Discount.Validate(obj.OrderTotal, obj.OrderDetails.Count) == -1);
        }

        #endregion

        #region GetOrderNotes

        public static List<OrderNoteModel> GetOrderNotes(this Domain.Order obj)
        {
            if (obj == null || obj.OrderDetails.Count == 0) return new List<OrderNoteModel>();

            return Mapper.Map<List<OrderNote>, List<OrderNoteModel>>(obj.OrderNotes.ToList());
        }

        #endregion

        #region GetUniqueKey
        public static string GetUniqueKey(this OrderDetailModel obj, Domain.Order order = null, bool credit = false, bool odDataJson = false)
        {
            if (obj == null) return null;

            if (obj.ProductId < 0) return OrderDetailModel.GetUniqueKey(obj.ProductId, obj.VariantId ?? 0, obj.Price, odDataJson ? obj.Data : null);

            var raw = obj.GetRaw();

            if (raw == null) return null;

            var price = credit ? raw.PriceAdm : raw.Price;

            if (price == 0 && order != null)
                price = order.OrderDetails.Where(x => x.Product.Id == obj.ProductId && x.Variant.GetId(0) == (obj.VariantId ?? 0) && (!odDataJson || x.Data.EqualsIgCase(obj.Data)))
                    .Select(x => x.Price).FirstOrDefault();

            return OrderDetailModel.GetUniqueKey(raw.Product.Id, raw.Variant.GetId(0), price, odDataJson ? raw.Data : null);
        }
        #endregion

        #region GetPaymentLogs

        public static List<PaymentLogModel> GetPaymentLogs(this Domain.Order obj)
        {
            if (obj == null || obj.PaymentLogs.Count == 0) return new List<PaymentLogModel>();

            return Mapper.Map<List<PaymentLog>, List<PaymentLogModel>>(obj.PaymentLogs.ToList());
        }

        #endregion

        #region GetPaymentData

        public static BasePayment.PaymentData GetPaymentData(this IOrder obj, bool resolveType = true)
        {
            if (obj == null || obj.PaymentData.IsNullOrEmpty()) return null;

            var data = BasePayment.PaymentData.Deserialize(obj.PaymentData);
            
            if (!resolveType) return data;

            var ret = obj.PaymentMethod.GetPaymentData(data);

            if (ret == null) return null;

            ret.Prev = data.Serialize();
            ret.NormalizePaymentData();

            return ret;
        }

        #endregion

        #region GetPaymentCardMaskedPan
        public static string GetPaymentCardMaskedPan(this Domain.Order obj, char maskChar = 'X')
        {
            if (obj == null||obj.PaymentCardMaskedPan==null) return null;

            if (obj.PaymentCardMaskedPan.Length <= 4) return obj.PaymentCardMaskedPan;

            return obj.PaymentCardMaskedPan
                .Substring(obj.PaymentCardMaskedPan.Length - 4, 4)
                .PadLeft(obj.PaymentCardMaskedPan.Length, maskChar);
        }
        #endregion

        #region GetId
        public static int? GetId(this CustGroup obj, int? defVal = null)
        {
            if (obj == null) return defVal;
            else return obj.Id;
        }

        public static int? GetId(this Invoice obj, int? defVal = null)
        {
            if (obj == null) return defVal;
            else return obj.Id;
        }

        public static int? GetId(this Domain.Order obj, int? defVal = null)
        {
            if (obj == null) return defVal;
            else return obj.Id;
        } 
        #endregion

        #region GetName
        public static string GetName(this CustGroup obj)
        {
            if (obj == null) return string.Empty;

            return obj.Name;
        } 
        #endregion

        #region GetBackOrderIds

        public static List<int> GetBackOrderIds(this Domain.Order obj)
        {
            if (obj == null || obj.BackOrders.SafeCount() == 0) return new List<int>();

            return obj.BackOrders.Select(x => x.Id).ToList();
        }

        #endregion

        #region GetOriginCreditId
        public static int? GetOriginCreditId(this Domain.Order obj)
        {
            if (obj == null || obj.OriginCredits.SafeCount() == 0) return null;

            return obj.OriginCredits.First().Id;
        }
        #endregion

        #region GetCustomer

        public static CustomerModel GetCustomer(this Domain.Order obj)
        {
            if (obj == null || obj.Customer == null) return null;

            var ret = Mapper.Map<Customer, CustomerModel>(obj.Customer);

            ret.OrgNumber = obj.OrgNumber.IfNull(ret.OrgNumber);
            ret.CompanyName = obj.CompanyName.IfNull(ret.CompanyName);
            ret.InvoiceEmail = obj.InvoiceEmail.IfNull(ret.InvoiceEmail);

            return ret;
        }

        #endregion

        #region ToEmailTempl
        public static EmailTemplModel ToEmailTempl(this OrderStatus status, IDB db, bool statusChange = true, bool newOrder = false, bool pos = false, bool recurring = false)
        {
            EmailTemplateType? type = null;

            if (statusChange)
                switch (status)
                {
                    case OrderStatus.New:
                    case OrderStatus.PayBeforeDelivery:
                        type = newOrder ? EmailTemplateType.OrderConfirm : EmailTemplateType.OrderChangeStatus;
                        break;
                    case OrderStatus.Sent:
                        type = pos ? EmailTemplateType.OrderConfirmPosInv : EmailTemplateType.OrderSent;
                        break;
                    case OrderStatus.PaymentWaiting:
                        type = recurring ? EmailTemplateType.RecurOrderFailed : EmailTemplateType.AbandonedCheckout;
                        break;
                    case OrderStatus.Cancelled:
                        type = EmailTemplateType.OrderCancel;
                        break;
                    case OrderStatus.Credit:
                        type = EmailTemplateType.OrderRefund;
                        break;
                    case OrderStatus.Processing:
                    case OrderStatus.RestOrder:
                        type = EmailTemplateType.OrderChangeStatus;
                        break;
                }
            else type = EmailTemplateType.OrderChangeStatus;

            return type == null ? null : EmailTemplMgr.GetAll(db, (int?)type).FirstOrDefault();
        }
        #endregion

        #region ToPaymentOrder

        public static global::Eshop.Payments.BasePayment.Order ToPaymentOrder(this Domain.Order obj, bool? giftcard = null)
        {
            return ToPaymentOrder(obj, giftcard, false);
        }

        public static global::Eshop.Payments.BasePayment.Order ToPaymentOrder(this Domain.Order obj, bool? giftcard, bool skipGiftcardPayment)
        {
            if (obj == null) return null;

            var settings = SettingsBiz.GetSettings();
            bool ceilingPrice = settings.Display.Product.PriceRound;
            var dibsEasyInvoiceFee = obj.PaymentMethod.GetPaymentDataType() == typeof(DIBSPaymentData) && settings.Hidden.DibsEasyInvoiceFee > 0 &&
                                     ((DIBSSetting)((PaymentMethod)obj.PaymentMethod).GetSetting()).DIBSPlatform.InList(DIBSPlatform.Easy) && 
                                     Math.Abs(obj.PaymentCost ?? 0) < 0.0001m && obj.OrderStatus.InList(OrderStatus.PaymentWaiting);
            var paymentCost = dibsEasyInvoiceFee ? settings.Hidden.DibsEasyInvoiceFee : (obj.PaymentCost ?? 0);
            var shippingCost = obj.ShippingCost;
            var giftCardTotalAfterPayShip = skipGiftcardPayment ? 0 : obj.GiftCardTotal;
            var giftcardAmount = (double)obj.OrderDetails.Where(m => m.Product.Id == settings.Hidden.GiftCardProductId).Sum(m => m.Quantity * (m.PriceInc ?? 0));
            var noGiftcardAmount = obj.OrderDetails.Where(m => m.Product.Id != settings.Hidden.GiftCardProductId).Sum(m => m.Quantity * (m.PriceInc ?? 0));

            if (giftcard == true && noGiftcardAmount != 0m) shippingCost = paymentCost = 0m;

            if (obj.OrderStatus.InList(OrderStatus.Credit))
            {
                if (giftCardTotalAfterPayShip <= paymentCost && paymentCost < 0) { giftCardTotalAfterPayShip -= paymentCost; paymentCost = 0; }
                if (giftCardTotalAfterPayShip <= shippingCost && shippingCost < 0) { giftCardTotalAfterPayShip -= shippingCost ?? 0; shippingCost = 0; }
            }  

            if (giftCardTotalAfterPayShip >= paymentCost && paymentCost > 0) { giftCardTotalAfterPayShip -= paymentCost; paymentCost = 0; }
            if (giftCardTotalAfterPayShip >= shippingCost && shippingCost > 0) { giftCardTotalAfterPayShip -= shippingCost ?? 0; shippingCost = 0; }

            var isDibsD2 = obj.PaymentMethod.GetPaymentDataType() == typeof(DIBSPaymentData) && ((DIBSSetting)((PaymentMethod)obj.PaymentMethod).GetSetting()).DIBSPlatform.InList(DIBSPlatform.D2);

            return new BasePayment.Order()
            {
                OrderId = (obj.IsRecurOneTime() || obj.IsRenewRecur()) ? obj.Id : obj.GetCartOrderId(),
                CustomerId = obj.Customer.Id,
                RestOrder = obj.OriginRest != null,
                Recurring = obj.Recurring != null && obj.Recurring.Order.Id == obj.Id,
                CurrencyCode = (string.IsNullOrEmpty(obj.Currency) ? "NOK" : obj.Currency).ToUpper(),
                PaymentFee = ceilingPrice ? Math.Ceiling((double)paymentCost) : (double)paymentCost,
                PaymentTaxRate = (dibsEasyInvoiceFee ? -1 : 1) * (double)(obj.PaymentVat ?? 0),
                ShippingFee = ceilingPrice ? Math.Ceiling((double)(shippingCost ?? 0)) : (double)(shippingCost ?? 0),
                ShippingTaxRate = (double)(obj.ShippingVat ?? 0),
                CouponName = obj.CouponCode ?? obj.OrderDetails.Select(x => x.CouponCode)
                    .FirstOrDefault(x => !x.IsNullOrEmpty()),
                CouponAmount = (double)Math.Round(obj.TotalCoupon + (isDibsD2 ? giftCardTotalAfterPayShip : 0), 2, MidpointRounding.AwayFromZero),
                DiscountName = obj.DiscountName ?? obj.OrderDetails.Select(x => x.DiscountName)
                    .FirstOrDefault(x => !x.IsNullOrEmpty()),
                DiscountAmount = (double)obj.TotalDiscount,
                BillingFirstName = obj.BillingName.GetFirstName(),
                BillingLastName = obj.BillingName.GetLastName(),
                BillingAddress = obj.BillingAddress,
                BillingAddress2 = null,
                PostNumber = obj.BillingPostnr,
                Location = obj.BillingPosted,
                Email = (obj.Customer.Email ?? "").EndsWith("@invalid.local") ? null : obj.Customer.Email,
                PhoneNumber = obj.DeliverMobile,//obj.BillingPhone,
                DeliveryFirstName = obj.DeliverName.GetFirstName(),
                DeliveryLastName = obj.DeliverName.GetLastName(),
                DeliveryAddress = obj.DeliverAddress,
                DeliveryAddress2 = null,
                DeliveryPostNumber = obj.DeliverPostnr,
                DeliveryLocation = obj.DeliverPosted,
                DeliveryStateOrProvince = obj.DeliverCity,
                DeliveryCountryCode = obj.DeliverCountry.Code,
                OrganizationNo = obj.OrgNumber,
                BillingCompany = obj.CompanyName,
                OrderTotal = giftcard == true ? giftcardAmount : Math.Round((double)Math.Min(obj.FinalTotal ?? 0, (obj.FinalTotalBak ?? obj.FinalTotal) ?? 0) - (giftcard == null ? 0 : giftcardAmount), 2, MidpointRounding.AwayFromZero),
                BillingCountryCode = obj.BillingCountry.Code,
            };
        }

        #endregion

        #region ToPaymentOrderDetails

        public static List<BasePayment.OrderDetail> ToPaymentOrderDetails(this Domain.Order obj,
            Domain.OrderDetail odetail = null, bool? giftcard = null)
        {
            return ToPaymentOrderDetails(obj, odetail, giftcard, includeManual: false);
        }

        public static List<BasePayment.OrderDetail> ToPaymentOrderDetails(this Domain.Order obj,
            Domain.OrderDetail odetail, bool? giftcard, bool includeManual)
        {
            return ToPaymentOrderDetails(obj, odetail, giftcard, includeManual, false);
        }

        public static List<BasePayment.OrderDetail> ToPaymentOrderDetails(this Domain.Order obj, Domain.OrderDetail odetail, bool? giftcard, bool includeManual, bool skipGiftcardPayment)
        {
            if (obj == null || obj.OrderDetails == null) return null;
            var settings = SettingsBiz.GetSettings();
            var recurring = obj.Recurring != null && obj.Recurring.Order.Id != obj.Id;
            var credit = obj.OrderStatus.InList(OrderStatus.Credit);
            var restCancel = obj.OrderStatus.InList(OrderStatus.RestOrder) && obj.OriginRest != null;
            bool ceilingPrice = settings.Display.Product.PriceRound;
            var canUpdOrder = includeManual || (obj.OriginRest != null ? obj.OriginRest.CanUpdateOrder(true) : obj.CanUpdateOrder(true));
            var orderTotal = obj.GetOrderTotal(canUpdOrder ? (bool?) null : false);
            var orderTotal2 = obj.GetOrderTotal(canUpdOrder ? (bool?)null : false, includeDiscount: false);
            var shippingCost = obj.ShippingCost;
            var paymentCost = obj.PaymentCost;
            var giftCardTotalAfterPayShip = skipGiftcardPayment ? 0 : obj.GiftCardTotal;

            if (credit)
            {
                if (giftCardTotalAfterPayShip <= paymentCost && paymentCost < 0) { giftCardTotalAfterPayShip -= paymentCost ?? 0; paymentCost = 0; }
                if (giftCardTotalAfterPayShip <= shippingCost && shippingCost < 0) { giftCardTotalAfterPayShip -= shippingCost ?? 0; shippingCost = 0; }
            }  

            if (giftCardTotalAfterPayShip >= paymentCost && paymentCost > 0)
            {
                giftCardTotalAfterPayShip -= paymentCost ?? 0;
                paymentCost = 0;
            }

            if (giftCardTotalAfterPayShip >= shippingCost && shippingCost > 0)
            {
                giftCardTotalAfterPayShip -= shippingCost ?? 0;
                shippingCost = 0;
            }

            var giftcardAmount = giftcard == null ? 0 : obj.OrderDetails.Where(m => m.Product.Id == settings.Hidden.GiftCardProductId).Sum(m => m.Quantity * (m.PriceInc ?? 0));
            var ratioDisc  = credit
                ? (orderTotal == 0 ? 0 : (orderTotal - giftCardTotalAfterPayShip) / orderTotal) 
                : (orderTotal - giftcardAmount + obj.DepositTotal == 0 ? 0 : (orderTotal - giftcardAmount + obj.DepositTotal - obj.TotalDiscount + obj.ItemDiscountTotal - (obj.Coupon != null && !obj.Coupon.DiscountType.InList(CouponType.PercentAndFreeShipping) ? obj.TotalCoupon : 0) - giftCardTotalAfterPayShip) / (orderTotal - giftcardAmount + obj.DepositTotal));
            var ratioDisc2 = credit
                ? (orderTotal2 == 0 ? 0 : (orderTotal2 - giftCardTotalAfterPayShip) / orderTotal2) 
                : (orderTotal2 - giftcardAmount + obj.DepositTotal == 0 ? 0 : (orderTotal2 - giftcardAmount + obj.DepositTotal /*- obj.TotalDiscount + obj.ItemDiscountTotal - (obj.Coupon != null ? obj.TotalCoupon : 0)*/ - giftCardTotalAfterPayShip) / (orderTotal2 - giftcardAmount + obj.DepositTotal));

            if (odetail == null) 
                obj.OrderDetails.ToList().ForEach(p => p.PriceInc = (decimal)Math.Round((double)p.PriceInc, 2, MidpointRounding.AwayFromZero));

            var gcProductId = settings.Hidden.GiftCardProductId;
            var oDetails = odetail != null 
                ? new[] { new { Od = odetail, OriginQty = (int?)null } } 
                : (obj.BackOrders.SafeCount() != 0 /*send order having rest orders*/
                    ? obj.GetBackOrderDetails().Select(x => new {Od = x.GetRaw(), x.OriginQty /*remaining qty*/}).ToArray()
                    : obj.OrderDetails.Select(x => new { Od = x, OriginQty = (int?)null }).ToArray());

            Func<KeyValuePair<string, object>, bool> filterDataExp = z => !(z.Value ?? "").ToString().IsNullOrEmpty() && !("odetail_datajson_" + z.Key).ModLocalize().Trim().IsNullOrEmpty() && !z.Key.InList("bundlevariantids", "bundleaccessoryids");

            return oDetails
                .Where(x => !x.Od.Manual && (giftcard == true ? x.Od.Product.Id == gcProductId : giftcard == null || x.Od.Product.Id != gcProductId))
                .Concat(oDetails.Where(x => canUpdOrder && x.Od.Manual)).Select(x => new
                {
                    x.Od.Id, x.Od.Product, x.Od.Variant, x.Od.ProductName,
                    Quantity = x.OriginQty ?? x.Od.Quantity,
                    PriceInc = x.Od.PriceInc ?? 0, x.Od.Price, DepositAmount = x.Od.DepositAmount ?? 0,
                    x.Od.CouponValue, x.Od.DiscountValue, x.Od.Vat, x.Od.RecurQty, x.Od.SourceId, DataExp = (System.Collections.Generic.IDictionary<string,object>)x.Od.Data.ToExpando(),
                }).Select(x => new BasePayment.OrderDetail()
                {
                    Id = recurring || credit || restCancel || obj.CartId != null ? (x.SourceId ?? 0) : x.Id,
                    Recurring = obj.Recurring != null && x.Product.ProductRecurDiscs.Any(),
                    ProductId = x.Product.GetId() ?? 0,
                    ProductName = string.Format("{0}{1}", x.ProductName?.Replace("\\","&#92;"), x.DataExp == null || x.DataExp.Count(filterDataExp) == 0 ? "" : " [" + x.DataExp.Where(filterDataExp).Select(z => string.Format("{0}: {1}", ("odetail_datajson_" + z.Key).ModLocalize(), z.Value)).ToString(", ") + "]"),
                    VariantId = x.Variant.SafeProp(z => z.Id),
                    SKU = x.Product.SKU,
                    Quantity = restCancel ? -1 * Math.Abs(x.Quantity) : (x.Quantity),
                    PriceExcVAT = (ceilingPrice ? Math.Round(Math.Ceiling((double)(x.PriceInc + x.DepositAmount)) / (1 + (double)x.Vat), 2, MidpointRounding.AwayFromZero) : (double)x.Price) * (giftcard == true ? 1 : (double)(ratioDisc2 ?? 0)),
                    PriceIncVAT = (ceilingPrice ? Math.Ceiling((double)(x.PriceInc + x.DepositAmount)) : (double)(x.PriceInc + x.DepositAmount)) * (giftcard == true ? 1 : (double)(ratioDisc2 ?? 0)),
                    PriceIncCredit = (double)Math.Round(giftcard == true ? x.PriceInc + x.DepositAmount : ((x.PriceInc + x.DepositAmount) - (x.CouponValue ?? 0) - (x.DiscountValue ?? 0)) * (ratioDisc ?? 0), 2),
                    VATValue = (ceilingPrice ? Math.Ceiling((double)(x.PriceInc)) - Math.Round(Math.Ceiling((double)(x.PriceInc)) / (1 + (double)x.Vat), 2, MidpointRounding.AwayFromZero) : (double)(x.PriceInc - x.Price)) * (giftcard == true ? 1 : (double)(ratioDisc2 ?? 0)),
                    VATRate = (double)x.Vat,
                }).Where(m => m.Quantity != 0).ToList();
        }
       
        #endregion

        #region GetPaymentOrderAmount
        public static decimal GetPaymentOrderAmount(this Domain.Order obj, bool skipGiftcardPayment, bool? giftcard = null)
        {
            if (obj == null) return 0;

            var order = obj.ToPaymentOrder(giftcard, skipGiftcardPayment);
            var orderDetails = obj.ToPaymentOrderDetails(null, giftcard, false, skipGiftcardPayment);

            #region ODetails

            var oDetails = orderDetails
                .Select(m => new { m.ItemCode, m.ProductName, m.Quantity, m.VATRate, PriceExcVAT = m.PriceIncCredit / (1 + m.VATRate), PriceIncVAT = m.PriceIncCredit })
                .Concat(new[]
                {
                    new
                    { 
                        ItemCode = "PAYEMNT", ProductName = "Payment Fee", Quantity = order.PaymentFee >= 0 ? 1 : -1, VATRate = order.PaymentTaxRate, 
                        PriceExcVAT = Math.Abs(order.PaymentFee/(1 + order.PaymentTaxRate)), PriceIncVAT = Math.Abs(order.PaymentFee), 
                    },
                    new
                    {
                        ItemCode = "SHIPPING", ProductName = "Shipping Fee", Quantity = order.ShippingFee >= 0 ? 1 : -1, VATRate = order.ShippingTaxRate, 
                        PriceExcVAT = Math.Abs(order.ShippingFee/(1 + order.ShippingTaxRate)), PriceIncVAT = Math.Abs(order.ShippingFee),
                    }
                })
                .Select(m => new
                {
                    OD = m,
                    GrossTotal = (int)Math.Round(m.PriceIncVAT * m.Quantity * 100),
                    NetTotal = (int)Math.Round(m.PriceExcVAT * m.Quantity * 100)
                }).Where(m => m.OD.ItemCode != "PAYEMNT" || Math.Abs(m.OD.PriceIncVAT) > 0.0001).ToList();

            #endregion

            return oDetails.Count == 0 ? 0 : oDetails.Sum(m => m.GrossTotal) / 100m;
        } 
        #endregion

        #region GetRestOrderRatio
        public static decimal GetRestOrderRatio(this Domain.Order obj, int? orderDetailId = null, IDB db = null)
        {
            if (obj == null) return 0;

            var origin = (Domain.Order)obj.OriginRest ?? obj;
            var taxCountryCode = obj.GetTaxCountryCode();
            var orderTotalOri = obj.GetRawOrderTotal(taxCountryCode, db, false, orderDetailId) ?? 0;
            var originOrderTotalOri = origin.GetRawOrderTotal(taxCountryCode, db, false) ?? 0;

            return originOrderTotalOri == 0 ? 0 : orderTotalOri / originOrderTotalOri;
        } 
        #endregion

        #region GetGiftCardTotal
        /// <param name="orderDetailId">-1: payment, -2:shipping</param>
        public static decimal GetGiftCardTotal(this Domain.Order obj, int? orderDetailId = null, IDB db = null)
        {
            if (obj == null || (obj.GiftCardTotal == 0 && obj.OriginRest == null)) return 0;

            if (!obj.Type.InList(OrderType.Web) || (orderDetailId == null && obj.OriginRest == null && obj.BackOrders.SafeCount() == 0)) 
                return obj.GiftCardTotal;

            if (orderDetailId == -1)
                return obj.GiftCardTotal >= obj.PaymentCost ? (obj.PaymentCost ?? 0) : 0;

            if (orderDetailId == -2)
                return obj.GiftCardTotal >= (obj.PaymentCost + obj.ShippingCost) ? (obj.ShippingCost ?? 0) : 0;

            if (obj.OriginRest != null)
            {
                var origin = (Domain.Order) obj.OriginRest;
                var giftCardTotal = origin.GiftCardTotal;

                if (giftCardTotal >= origin.PaymentCost && origin.PaymentCost > 0)
                    giftCardTotal -= origin.PaymentCost ?? 0;

                if (giftCardTotal >= origin.ShippingCost && origin.ShippingCost > 0)
                    giftCardTotal -= origin.ShippingCost ?? 0;

                return Math.Round(giftCardTotal * obj.GetRestOrderRatio(orderDetailId, db), 2);
            }

            if (orderDetailId != null)
            {
                var giftCardTotal = obj.GiftCardTotal;

                if (giftCardTotal >= obj.PaymentCost && obj.PaymentCost > 0)
                    giftCardTotal -= obj.PaymentCost ?? 0;

                if (giftCardTotal >= obj.ShippingCost && obj.ShippingCost > 0)
                    giftCardTotal -= obj.ShippingCost ?? 0;

                return Math.Round(giftCardTotal * obj.GetRestOrderRatio(orderDetailId, db), 2);
            }

            return obj.GiftCardTotal;
        } 
        #endregion

        #region GetPaymentSystemName
        public static string GetPaymentSystemName(this Domain.Order obj)
        {
            if (obj == null || obj.PaymentMethod == null) return null;

            return obj.PaymentMethod.PaymentSystemName;
        }
        #endregion

        #region IsMoneyReturnable

        public static bool IsMoneyReturnable(this Domain.Order obj)
        {
            if (obj == null) return false;

            if (!obj.PaymentMethod.IsOnlinePayment) return true;
            else if (!obj.OrderStatus.InList(OrderStatus.Sent) && !obj.ValidPaymentData(m => m.AutoCapture)) return false;

            var processor = obj.PaymentMethod.GetProcessor(null, obj);
            var data = BasePayment.PaymentData.Deserialize(obj.PaymentData);

            return processor != null && data != null && processor.CanReturn && data.IsCaptured;
        }

        #endregion

        #region ValidPaymentData

        public static bool ValidPaymentData(this Domain.Order obj, Predicate<BasePayment.PaymentData> predicate)
        {
            if (obj == null || obj.PaymentData.IsNullOrEmpty()) return false;

            var data = BasePayment.PaymentData.Deserialize(obj.PaymentData);

            return data != null && predicate(data);
        }

        #endregion

        #region CanReturn
        public static bool CanReturn(this Domain.Order obj)
        {
            if (obj == null || !obj.OrderStatus.InList(OrderStatus.Sent)) return false;

            var settings = SettingsBiz.GetSettings();

            if (obj.OrderDetails.All(m => m.Product.Id == settings.Hidden.GiftCardProductId)) 
                return false;

            if (obj.OriginRest != null)
            {
                if (obj.PaymentMethod.IsOnlinePayment)
                {
                    if (obj.PaymentMethod.GetProcessor(null, obj).SafeProp(m => m.CanReturn))
                        if (!obj.ValidPaymentData(m => m.IsCaptured) && 
                            obj.OriginRest.ValidPaymentData(m => m.Version == 0 && !m.IsCaptured)) return false;
                }
            }

            if (obj.OriginCredits.Count == 0) return true;

            return obj.GetCreditOrderDetails().Count != 0;
        }
        #endregion

        #region CanCancel
        public static bool CanCancel(this Domain.Order obj)
        {
            if (obj == null || obj.PaymentMethod == null) return false;

            if (obj.OrderStatus.InList(OrderStatus.RestOrder))
            {
                if (!obj.PaymentMethod.IsOnlinePayment) return true;

                var processor = obj.OriginRest.PaymentMethod.GetProcessor();

                if (processor.CanReturn && !processor.CanPartialCapture &&
                    !obj.OriginRest.OrderStatus.InList(OrderStatus.Sent) &&
                    obj.OriginRest.ValidPaymentData(m => !m.AutoCapture)) return false;

                if (obj.OriginRest.ValidPaymentData(m => m.Version == 0) &&
                    processor.CanReturn && ((!obj.OriginRest.OrderStatus.InList(OrderStatus.Sent) && !obj.OriginRest.ValidPaymentData(m => m.AutoCapture)) || !obj.OriginRest.IsMoneyReturnable())) return false; 
                
                return true;
            }

            if (obj.PaymentMethod.IsOnlinePayment && obj.BackOrders.SafeCount() != 0)
            {
                var processor = obj.PaymentMethod.GetProcessor();

                if (processor.CanReturn && (!processor.CanPartialCapture || obj.ValidPaymentData(m => m.Version == 0)) && 
                    obj.BackOrders.Any(x => !x.OrderStatus.InList(OrderStatus.RestOrder, OrderStatus.Cancelled))) return false;

                if (processor.CanPartialCapture && obj.ValidPaymentData(m => m.Version > 0) && 
                    obj.BackOrders.Any(x => x.OrderStatus.InList(OrderStatus.RestOrder))) return false;
            }

            if (obj.OrderStatus.InList(OrderStatus.PaymentWaiting)) return true;

            if (!obj.OrderStatus.InList(OrderStatus.New, OrderStatus.Processing)) return false;

            if (obj.PaymentMethod.IsOnlinePayment && obj.FinalTotal > 0)
            {
                if (obj.AuthorizationId.IsNullOrEmpty()) return false;

                var data = BasePayment.PaymentData.Deserialize(obj.PaymentData);

                return data != null && data.IsAuthorized && !data.IsCanceled && !data.IsCaptured;
            }
            else return true;
        }
        #endregion

        #region CanSend
        public static bool CanSend(this Domain.Order obj, out bool needCapture, out string errMsg)
        {
            errMsg = null;
            needCapture = false;

            if (obj == null || obj.PaymentMethod == null || !obj.OrderStatus.InList(OrderStatus.New, OrderStatus.RestOrder, OrderStatus.Processing, OrderStatus.PayBeforeDelivery))
                return false;

            if (obj.OrderStatus.InList(OrderStatus.New, OrderStatus.Processing, OrderStatus.RestOrder) && 
                obj.PaymentMethod.IsOnlinePayment && obj.FinalTotal > 0 && obj.IsValidOnlinePaymentAmount())
            {
                var data = BasePayment.PaymentData.Deserialize(obj.PaymentData);

                if (/*data == null || !data.IsAuthorized ||*/ data != null && data.IsCanceled) return false;

                if (obj.OrderStatus.InList(OrderStatus.New, OrderStatus.Processing)) needCapture = data != null && !data.IsCaptured;
                else if (obj.OriginRest != null && (data == null || !data.IsCaptured))
                {
                    var origin = obj.OriginRest;
                    var oriData = BasePayment.PaymentData.Deserialize(origin.PaymentData);
                    var processor = origin.PaymentMethod.GetProcessor();
                    needCapture = processor != null && processor.CanPartialCapture && oriData != null && oriData.Version > 0 && !oriData.AutoCapture;
                }

                if (needCapture && Math.Abs(obj.ToPaymentOrder().OrderTotal) < 0.01) needCapture = false;

                return true;
            }
            return true;
        }
        #endregion

        #region IsValidOnlinePaymentAmount

        public static bool IsValidOnlinePaymentAmount(this Domain.Order obj)
        {
            var canUpdateOrder = obj != null && obj.OriginRest != null
                ? obj.OriginRest.CanUpdateOrder(true)
                : obj.CanUpdateOrder(true);

            return obj != null && obj.OrderDetails != null &&
                   obj.OrderDetails.Any(m => (canUpdateOrder || !m.Manual) && 0m != m.Quantity * (m.PriceInc - m.CouponValue - m.DiscountValue));
        } 

        #endregion

        #region CaptureGiftcardAmount
        public static bool CaptureGiftcardAmount(this Domain.Order order, Controllers.Admin.BaseController controller, ref bool sendMailYet)
        {
            if (order == null) return false;

            var needSendMail = false;
            var settings = SettingsBiz.GetSettings();

            if (order.OrderDetails.Count == 0 || order.OrderDetails.Any(m => m.Product.Id != settings.Hidden.GiftCardProductId && !m.ProductShippingType.InList(ShippingType.Download) && m.Price > 0)) return true;

            string errMsg;
            var anyGiftcard = order.OrderDetails.Any(m => m.Product.Id == settings.Hidden.GiftCardProductId);
            var anyDownload = order.OrderDetails.Any(m => m.ProductShippingType.InList(ShippingType.Download) && !m.Product.AttachFile.IsNullOrEmpty());

            PaymentHelper.Capture(controller, order, out errMsg, anyGiftcard, (data, error) =>
            {
                var body = error.IsNullOrEmpty()
                    ? string.Format(Order.StringTable.OrderCaptureOkLogTempl, data.TransactionId, order.GetPaymentSystemName())
                    : string.Format(Order.StringTable.ErrMsgOrderCaptureFailed, error);

                OrderNoteMgr.SaveorUpdate(controller, new OrderNoteModel()
                {
                    Subject = Order.StringTable.CapturedPayment + string.Format(" [{0}]", new[] { anyGiftcard ? Order.StringTable.Giftcard : null, anyDownload ? StringTable.Digital : null }.Where(m => m != null).ToString(", ")),
                    Body = body,
                    OrderId = order.Id,
                    OrderStatus = order.OrderStatus,
                    Private = true
                }, null, controller.Request);

                if (error.IsNullOrEmpty())
                {
                    /*if (order.OrderDetails.Any(m => m.Product.Id != settings.Hidden.GiftCardProductId && !m.ProductShippingType.InList(ShippingType.Download) && m.Price > 0))
                        data.SafeInvokeMethod<bool>("ResetCaptureData");
                    else*/
                    {
                        if (order.OrderStatus.InList(OrderStatus.New))
                        {
                            order.OrderStatus = (byte) OrderStatus.Sent;
                            needSendMail = true;

                            var invoice = new InvoiceModel()
                            {
                                OrderId = order.Id,
                                Data = controller.RenderToString("InvoiceHtml", new { order.Id }, true, "PosPdf", order.LangCode, new RouteCollection(){ controller.RouteData.Route })
                            };

                            InvoiceMgr.SaveorUpdate(controller, invoice);

                            order.Invoice = InvoiceMgr.GetDom(controller, invoice.Id);
                        }
                    }
                }
            });

            if (needSendMail) sendMailYet = false;

            return true;
        } 
        #endregion

        #region GetSharedCoupon

        public static TProp GetSharedCoupon<TProp>(this Domain.Order obj, Func<ICoupon, TProp> selector)
            where TProp : class
        {
            if (obj == null) return null;

            if (obj.Coupon != null) return selector(obj.Coupon);

            var itm = obj.OrderDetails.FirstOrDefault(x => x.Coupon != null);

            return itm == null ? null : selector(itm.Coupon);
        }

        #endregion

        #region HandleNewEvent
        public static void HandleNewEvent(this Domain.Order order, IDB db, BasePayment.PaymentData data,bool sendMailYet)
        {
            var body = string.Format(Order.StringTable.CallbackLogPayment, data != null ? data.TransactionId : string.Empty, order.GetPaymentSystemName(), DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss"));
            
            OrderNoteMgr.SaveorUpdate(db, new OrderNoteModel()
            {
                Subject = Order.StringTable.CallbackPayment,
                Body = body,
                OrderId = order.Id,
                OrderStatus = order.OrderStatus,
                Private = true
            });

            if (!sendMailYet)
            {
                #region SendEmail Online

                var modelOrder = OrderMgr.Get(new DefaultDb(), order.Id);
                modelOrder.InitOrderDetails(new DefaultDb());
                modelOrder.InitMandatoryInfo();
                Singleton<ISendEmailMgr>.Default.Send((System.Web.Mvc.Controller)db, modelOrder, modelOrder.OrderStatus, modelOrder.Comments, true);

                #endregion
            }
        } 
        #endregion

        #region GetNextRunning

        public static DateTime? GetNextRunning(this RecurOrder obj)
        {
            return obj.GetNextRunning(null, null, null);
        }

        public static DateTime? GetNextRunning(this RecurOrder obj, DateTime? fromDate, int? recurring, int? recurEach)
        {
            if (obj == null || obj.ToDate < DateTime.Now.Date) return null;

            var fromdate2 = (fromDate ?? obj.FromDate).Add(obj.CreatedDate - obj.CreatedDate.Date);
            var stime = obj.LastRunTime == null || obj.LastRunTime < fromdate2 ? fromdate2 : obj.LastRunTime.Value;
            var recurEach2 = recurEach ?? obj.RecurEach;

            if ((obj.LastRunTime ?? obj.CreatedDate).Date < fromdate2.Date) return fromdate2;

            switch ((RecurringType)(recurring ?? obj.Recurring))
            {
                case RecurringType.Daily:
                    return stime.AddDays(recurEach2);
                case RecurringType.Weekly:
                    return stime.AddDays(7 * recurEach2);
                case RecurringType.Monthly:
                    return stime.AddMonths(recurEach2);
                case RecurringType.Bimonthly:
                    return stime.AddMonths(2 * recurEach2);
                case RecurringType.Trimonthly:
                    return stime.AddMonths(3 * recurEach2);
                case RecurringType.Quadmonthly:
                    return stime.AddMonths(4 * recurEach2);
                case RecurringType.Semiyearly:
                    return stime.AddMonths(6 * recurEach2);
                case RecurringType.Yearly:
                    return stime.AddYears(recurEach2);
                default:
                    throw new InvalidOperationException("RecurOrder.NextRunning");
            }
        }

        #endregion

        #region GetProdRecurDiscountRate


        public static System.Tuple<decimal, ProductRecurDisc> GetProdRecurDiscountRate(this OrderDetail obj)
        {
            return obj.GetProdRecurDiscountRate(false);
        }

        public static System.Tuple<decimal, ProductRecurDisc> GetProdRecurDiscountRate(this OrderDetail obj, bool useSrc)
        {
            if (obj == null || obj.RecurType == null || (obj.RecurEach ?? 0) < 1)
                return new System.Tuple<decimal, ProductRecurDisc>(0, null);

            var ret = obj.Product.ProductRecurDiscs.FirstOrDefault(x => x.Type == ((useSrc ? obj.SrcRecurType : null) ?? obj.RecurType) && x.RecurEach == ((useSrc ? obj.SrcRecurEach : null) ?? obj.RecurEach));

            return ret == null
                ? new System.Tuple<decimal, ProductRecurDisc>(0, null)
                : new System.Tuple<decimal, ProductRecurDisc>(ret.Value, ret);
        }

        #endregion

        #region GetRecurInfoDesc

        public static string GetRecurInfoDesc(this OrderDetail obj)
        {
            return obj.GetRecurInfoDesc(true);
        }

        public static string GetRecurInfoDesc(this OrderDetail obj, bool useSrc)
        {
            if (obj == null || obj.RecurType == null || (obj.RecurEach ?? 0) < 1)
                return obj == null ? null : obj.ProductName;

            var discVal = obj.GetProdRecurDiscountRate(true).Item1* 100;
            var recurEach = (useSrc ? obj.SrcRecurEach : null) ?? obj.RecurEach;

            return string.Format(StringTable.RecurInfoDesc((RecurringType)((useSrc ? obj.SrcRecurType : null) ?? (obj.RecurType ?? 0)), discVal > 0, recurEach ?? 0), obj.ProductName,
                recurEach, (double)discVal);
        }

        #endregion

        #region IsProdRecurring

        public static bool IsProdRecurring(this Domain.Order obj, Predicate<OrderDetail> fCond = null)
        {
            if (obj == null || obj.OrderDetails.SafeCount() == 0) return false;

            return obj.OrderDetails.Any(x => x.RecurType != null && x.RecurEach > 0 && (fCond == null || fCond(x)));
        }

        public static bool IsProdRecurring(this OrderModel obj)
        {
            if (obj == null || obj.OrderDetails.SafeCount() == 0) return false;

            return obj.OrderDetails.Any(x => x.RecurType != null && x.RecurEach > 0);
        }

        #endregion

        #region GetProdRecurDiscount

        public static decimal GetProdRecurDiscount(this Domain.Order obj)
        {
            if (obj == null || obj.OrderDetails.SafeCount() == 0) return 0;

            return obj.OrderDetails.Sum(x => x.GetProdRecurDiscountRate().Item1*x.Quantity*(x.PriceInc ?? 0));
        }

        #endregion

        #region FormatShippingPickupPoint

        public static string FormatShippingPickupPoint(this string json, ShippingServiceType shippingService, bool useAlter = true, IShipping shipping = null)
        {
            if (json.IsNullOrEmpty()) return null;

            try
            {
                switch (shippingService)
                {
                    case ShippingServiceType.Bring:
                    {
                        var addr = json.JsonDeserializeSafe<PickupPoint>();
                        if (addr != null && !addr.Id.IsNullOrEmpty()) 
                            return string.Format("{0}, {1}, {3} {4}, {5} ",
                                addr.Name, addr.Address, null, addr.City, addr.PostalCode, addr.CountryCode);

                        if (!useAlter) return null;

                        var addr2 = json.JsonDeserializeSafe<Logistra.ServicePartner>();

                        if (addr2 != null && !addr2.Number.IsNullOrEmpty())
                            return string.Format("PostNord: {0}, {1}, {3} {4}, {5} ",
                                addr2.Name, addr2.Address1, addr2.Address2, addr2.City, addr2.Postcode, addr2.Country);

                        var addr3 = json.JsonDeserializeSafe<Postnord.Data.PickupPoint>();

                        return addr3 == null || addr3.servicePointId.IsNullOrEmpty() ? null : string.Format("{0}, {1} {2}, {3} {4}, {5} ",
                                addr3.name, addr3.deliveryAddress.SafeProp(z => z.streetName), addr3.deliveryAddress.SafeProp(z => z.streetNumber), addr3.deliveryAddress.SafeProp(z => z.city), addr3.deliveryAddress.SafeProp(z => z.postalCode), addr3.deliveryAddress.SafeProp(z => z.countryCode));

                    }
                    case ShippingServiceType.Logistra:
                    {
                        var addr = json.JsonDeserializeSafe<Logistra.ServicePartner>();

                        if (addr != null && !addr.Number.IsNullOrEmpty()) 
                            return string.Format("{0}, {1}, {3} {4}, {5} ",
                                addr.Name, addr.Address1, addr.Address2, addr.City, addr.Postcode, addr.Country);

                        if (!useAlter) return null;

                        var addr2 = json.JsonDeserializeSafe<PickupPoint>();

                        if (addr2 != null && !addr2.Id.IsNullOrEmpty())
                            return string.Format("Bring: {0}, {1}, {3} {4}, {5} ",
                                addr2.Name, addr2.Address, null, addr2.City, addr2.PostalCode, addr2.CountryCode);

                        var addr3 = json.JsonDeserializeSafe<Postnord.Data.PickupPoint>();

                        return addr3 == null || addr3.servicePointId.IsNullOrEmpty() ? null : string.Format("{0}, {1} {2}, {3} {4}, {5} ",
                                addr3.name, addr3.deliveryAddress.SafeProp(z => z.streetName), addr3.deliveryAddress.SafeProp(z => z.streetNumber), addr3.deliveryAddress.SafeProp(z => z.city), addr3.deliveryAddress.SafeProp(z => z.postalCode), addr3.deliveryAddress.SafeProp(z => z.countryCode));

                    }
                    case ShippingServiceType.Postnord:
                    {
                        var addr = json.JsonDeserializeSafe<Postnord.Data.PickupPoint>();

                        if (addr != null && !addr.servicePointId.IsNullOrEmpty())
                            return string.Format("{0}, {1} {2}, {3} {4}, {5} ",
                                addr.name, addr.deliveryAddress.SafeProp(z => z.streetName), addr.deliveryAddress.SafeProp(z => z.streetNumber), addr.deliveryAddress.SafeProp(z => z.city), addr.deliveryAddress.SafeProp(z => z.postalCode), addr.deliveryAddress.SafeProp(z => z.countryCode));

                        if (!useAlter) return null;

                        var addr2 = json.JsonDeserializeSafe<PickupPoint>();

                        if (addr2 != null && !addr2.Id.IsNullOrEmpty())
                            return string.Format("Bring: {0}, {1}, {3} {4}, {5} ",
                                addr2.Name, addr2.Address, null, addr2.City, addr2.PostalCode, addr2.CountryCode);

                        var addr3 = json.JsonDeserializeSafe<Logistra.ServicePartner>();

                        return addr3 == null || addr3.Number.IsNullOrEmpty() ? null : string.Format("PostNord: {0}, {1}, {3} {4}, {5} ",
                                addr3.Name, addr3.Address1, addr3.Address2, addr3.City, addr3.Postcode, addr3.Country);
                    }
                    default:
                        if (shipping != null && shipping.ServiceProvider.InList(ShippingService.StorePickup))
                        {
                            var addr = json.JsonDeserializeSafe<ShippingAddress>();
                            return addr == null ? null : string.Format("{0}, {1}, {3} {4}{5} ",
                                addr.Name, addr.Address, null, addr.Posted, addr.Postnr, addr.Country == null ? "" : ", " + addr.Country.Code);
                        }
                        return null;
                }
            }
            catch (Exception err)
            {
                LogHelper.Log4net(new Exception(string.Format("Err: {0}, Data: {1}", err.Message, json), err), ClawExceptionType.UnknownError, "");
                return null;
            }
        } 
        
        #endregion

        #region FixShippingPickupPoint
        public static string FixShippingPickupPoint(this string json, ShippingServiceType? shippingService)
        {
            if (shippingService == null || json.IsNullOrEmpty() || json.IndexOf('{') == -1) return null;

            switch (shippingService.Value)
            {
                case ShippingServiceType.Bring:
                {
                    var addr = json.JsonDeserializeSafe<PickupPoint>();
                    if (addr != null && !addr.Id.IsNullOrEmpty()) return json;

                    var addr2 = json.JsonDeserializeSafe<Logistra.ServicePartner>();
                    if (addr2 != null && !addr2.Number.IsNullOrEmpty() && !addr2.Postcode.IsNullOrEmpty() && !addr2.Country.IsNullOrEmpty())
                    {
                        if (!addr2.Address1.IsNullOrEmpty())
                            addr = BringShippingMgr.GetPickupPoints(addr2.Country, addr2.Postcode, addr2.Address1).FirstOrDefault();

                        addr = addr ?? BringShippingMgr.GetPickupPoints(addr2.Country, addr2.Postcode).FirstOrDefault();
                        return addr == null ? null : JsonConvert.SerializeObject(addr);
                    }

                    var addr3 = json.JsonDeserializeSafe<Postnord.Data.PickupPoint>();
                    if (addr3 != null && !addr3.servicePointId.IsNullOrEmpty() && addr3.deliveryAddress != null && !addr3.deliveryAddress.postalCode.IsNullOrEmpty() && !addr3.deliveryAddress.countryCode.IsNullOrEmpty())
                    {
                        if (!addr3.deliveryAddress.streetName.IsNullOrEmpty())
                            addr = BringShippingMgr.GetPickupPoints(addr3.deliveryAddress.countryCode, addr3.deliveryAddress.postalCode, string.Format("{0} {1}", addr3.deliveryAddress.streetName, addr3.deliveryAddress.streetNumber).Trim()).FirstOrDefault();

                        addr = addr ?? BringShippingMgr.GetPickupPoints(addr3.deliveryAddress.countryCode, addr3.deliveryAddress.postalCode).FirstOrDefault();
                        return addr == null ? null : JsonConvert.SerializeObject(addr);
                    }

                    return null;
                }
                case ShippingServiceType.Logistra:
                {
                    var addr = json.JsonDeserializeSafe<Logistra.ServicePartner>();
                    return addr != null && !addr.Number.IsNullOrEmpty() ? json : null;
                }
                case ShippingServiceType.Postnord:
                {
                    var addr = json.JsonDeserializeSafe<Postnord.Data.PickupPoint>();
                    return addr != null && !addr.servicePointId.IsNullOrEmpty() ? json : null;
                }
                default:
                    return null;
            }
        }
        #endregion

        #region GetShippingInfo
        public static System.Tuple<string, bool> GetShippingInfo(this Domain.Order obj)
        {
            if (obj == null || (obj.BringBookingData.IsNullOrEmpty() && obj.ShippingData.IsNullOrEmpty())) return null;

            if (!obj.BringBookingData.IsNullOrEmpty())
                return new System.Tuple<string, bool>("Bring", !obj.ShippingPickupPoint.IsNullOrEmpty());

            switch (obj.ShippingService)
            {
                case (int)ShippingServiceType.Bring:
                    return null;
                case (int)ShippingServiceType.Logistra:
                    {
                        var data = obj.ShippingData.IsNullOrEmpty() ? null : JsonConvert.DeserializeObject<Logistra.BookingResult>(obj.ShippingData);

                        return data == null || data.CarrierName.IsNullOrEmpty()
                            ? null
                            : new System.Tuple<string, bool>(data.CarrierName, !obj.ShippingPickupPoint.IsNullOrEmpty());
                    }
                case (int)ShippingServiceType.Postnord:
                    return new System.Tuple<string, bool>("Postnord", !obj.ShippingPickupPoint.IsNullOrEmpty());
                default:
                    return null;
            }
        }
        #endregion

        #region GetShippingTrackingUrl
        public static string GetShippingTrackingUrl(this Domain.Order obj)
        {
            if (obj == null || (obj.ShippingTrackingId.IsNullOrEmpty() && 0 == (obj.Flags & (int)OrderFlagType.DropShipMark))) 
                return null;

            string trackingUrl = null;
            
            if(obj.ShippingService != null)
                switch ((ShippingServiceType)obj.ShippingService)
                {
                    case ShippingServiceType.Bring:
                        var dataBring = obj.BringBookingData.IsNullOrEmpty() ? null : JsonConvert.DeserializeObject<BaseShipping.BookingResult>(obj.BringBookingData);
                        trackingUrl = dataBring.SafeProp(m => m.TrackingUrl);

                        if (!trackingUrl.IsNullOrEmpty()) return trackingUrl;
                        break;
                    case ShippingServiceType.Logistra:
                        var dataLogistra = obj.ShippingData.IsNullOrEmpty() ? null : JsonConvert.DeserializeObject<Logistra.BookingResult>(obj.ShippingData);
                        trackingUrl = dataLogistra.SafeProp(m => m.TrackingUrl);

                        if (!trackingUrl.IsNullOrEmpty()) return trackingUrl;
                        break;
                    case ShippingServiceType.Proteria:
                        var dataProteria = obj.ShippingData.IsNullOrEmpty() ? null : JsonConvert.DeserializeObject<Proteria.BookingResult>(obj.ShippingData);
                        trackingUrl = dataProteria.SafeProp(m => m.TrackingUrl);

                        if (!trackingUrl.IsNullOrEmpty()) return trackingUrl;
                        break;
                    case ShippingServiceType.PorterBuddy:
                         var dataBuddy = obj.ShippingData.IsNullOrEmpty() ? null : JsonConvert.DeserializeAnonymousType(obj.ShippingData, new { orderId = "", orderStatus = "", userInformation = "" });
                         trackingUrl = dataBuddy.SafeProp(m => m.userInformation);

                         if (!trackingUrl.IsNullOrEmpty()) return trackingUrl;
                         break;
                    case ShippingServiceType.Postnord:
                         var dataPostnord = obj.ShippingData.IsNullOrEmpty() ? null : JsonConvert.DeserializeObject<Postnord.BookingResult>(obj.ShippingData);
                         trackingUrl = dataPostnord.SafeProp(m => m.TrackingUrl);

                         if (!trackingUrl.IsNullOrEmpty()) return trackingUrl;
                         break;
                }

            if (trackingUrl.IsNullOrEmpty())
            {
                if (0 != (obj.Flags & (int)OrderFlagType.DropShipMark)) 
                {
                    var dropship = Order.StringTable.DropShip;
                    var note = obj?.OrderNotes?.FirstOrDefault(m => m.Subject == dropship && m.Body?.IndexOf("https://") >= 0);
                    if (note != null) {
                        var idx = note.Body.LastIndexOf("https://");
                        trackingUrl = note.Body.Substring(idx)?.Trim();
                    }

                    if (trackingUrl?.Length > 0 || obj.ShippingTrackingId.IsNullOrEmpty()) 
                        return trackingUrl;
                }

                var trackUrl = (obj.ShippingService == null || (obj.ShippingService ?? 0).InList(ShippingServiceType.Bring))
                    ? "https://sporing.posten.no/sporing/{0}"
                    : "http://www.tollpost.no/wwwappl/send_foresp.epl?p_ref_type=send_nr&p_ref={0}";

                return string.Format(trackUrl, obj.ShippingTrackingId);               
            }

            return trackingUrl;
        }
        #endregion

        #region ToList

        public static List<RecurOrderModel> ToList(this RecurOrderModel obj)
        {
            if (obj == null) return new List<RecurOrderModel>();

            var ret = new List<RecurOrderModel>() {obj};

            while (obj.NextRecurOrder != null)
            {
                ret.Add(obj.NextRecurOrder);
                obj = obj.NextRecurOrder;
            }

            return ret;
        }

        public static List<RecurOrder> ToList(this RecurOrder obj)
        {
            if (obj == null) return new List<RecurOrder>();

            var ret = new List<RecurOrder>() { obj };

            while (obj.NextRecurOrder != null)
            {
                ret.Add(obj.NextRecurOrder);
                obj = obj.NextRecurOrder;
            }

            return ret;
        }

        #endregion

        #region GetStatsInfo
        public static CustomerModel.StatsType GetStatsInfo(this Domain.Customer obj, IDB db = null)
        {
            if (obj == null) return null;

            if (obj.StatsInfo.IsNullOrEmpty())
            {
                var stats = OrderMgr.GetCustomerTotalSales(db = db ?? new DefaultDb(), obj.Id);

                var ret = new CustomerModel.StatsType()
                {
                    Orders = stats.SafeProp(m => m.Item1), 
                    TotalSales = stats.SafeProp(m => m.Item2)
                };

                obj.StatsInfo = JsonConvert.SerializeObject(ret, new OMoney.JsonConverter());

                if (db is SeparatedDb)
                {
                    if(Debugger.IsAttached)Debugger.Break();
                }
                else obj.Update(db.DataService);

                return ret;
            }

            return JsonConvert.DeserializeObject<CustomerModel.StatsType>(obj.StatsInfo, new OMoney.JsonConverter());
        }
        #endregion

        #region IsShowPrice
        public static bool IsShowPrice(this Domain.Customer obj)
        {
            if (obj == null || obj.CustomerCGroups == null) return false;

            var settings = SettingsBiz.GetSettings();

            if (!settings.Hidden.Price4LoginOnly || settings.Hidden.Price4LoginAnyGroup || obj.CustomerCGroups.Any(m => m.Group.Price4Login)) return true;

            return CustGroupMgr.GetAll(new DefaultDb()).Count == 0;
        }
        #endregion

        #region GetStockQty

        public static int GetStockQty(this OrderDetailBundle obj, int delta)
        {
            if (obj == null || obj.Product == null) return 0;

            return obj.Variant == null
                ? (obj.Product.Quantity > 0 ? obj.Product.Quantity : -1 * Math.Min(Math.Abs(delta), obj.Product.BackOrderQty))
                : (obj.Variant.Quantity > 0 ? obj.Variant.Quantity : -1 * Math.Min(Math.Abs(delta), obj.Variant.BackOrderQty));
        }

        public static int GetStockQty(this OrderDetail obj, int delta)
        {
            if (obj == null || obj.Product == null) return 0;

            if (!obj.Product.Type.InList(ProductType.Bundle))
                return obj.Variant == null
                    ? (obj.Product.Quantity > 0 ? obj.Product.Quantity : -1 * Math.Min(Math.Abs(delta), obj.Product.BackOrderQty))
                    : (obj.Variant.Quantity > 0 ? obj.Variant.Quantity : -1 * Math.Min(Math.Abs(delta), obj.Variant.BackOrderQty));
                    
            var lst = obj.OrderDetailBundles;
            var qty = lst.Count == 0 ? 0 : lst.Min(m => m.Quantity == 0 ? 0 : (m.Variant != null ? m.Variant.Quantity : m.Product.Quantity) / m.Quantity);
            var backQty = lst.Count == 0 ? 0 : lst.Max(m => m.Quantity == 0 ? 0 : (m.Variant != null ? m.Variant.BackOrderQty : m.Product.BackOrderQty) / m.Quantity);

            return qty > 0 ? qty : -1*Math.Min(Math.Abs(delta), backQty);
        }
        #endregion

        #region GetDownloadContents

        public static string GetDownloadContents(this Domain.Order order)
        {
            var lst = order == null || System.Web.HttpContext.Current == null ? null : order.OrderDetails.Where(m => m.ProductShippingType.InList(ShippingType.Download)).ToList();

            return lst.SafeCount() == 0 ? null : ("<p>" + lst
                        .Select(z => new { Od = z, Url = Claw.Core.ClawContext.Current.CurrentSite.SiteUrl + ModuleUrl.FrontUrl("Order", "Download", new RouteValueDictionary(new { orderId = order.Id, z.Id, token = order.ReservationId })) })
                        .Select(z => string.Format("<span>{1}</span>: <a href=\"{0}\">{0}</a>", z.Url, z.Od.ProductName))
                        .ToString("</p><p>")
                    + "</p>");

        }

        #endregion

        #region GetSerialQty

        public static List<System.Tuple<ProductBase, VariantBase, int, int, string>> GetSerialQty(this OrderDetail obj) 
        {
            return obj.GetSerialQtyRaw().Where(m => m.Item3 > 0).ToList();
        }
        private static List<System.Tuple<ProductBase, VariantBase, int, int, string>> GetSerialQtyRaw(this OrderDetail obj)
        {
            if (obj == null) return new List<System.Tuple<ProductBase, VariantBase, int, int, string>>();

            if (obj.Product.Type.InList(ProductType.Bundle))
                return obj.Product.ProductBundles
                    .Where(m => !m.Product.Deleted && 0 != (m.Product.Flags & (int)ProductFlags.SerialNumber))
                    .Select(m => new System.Tuple<ProductBase, VariantBase, int, int, string>(m.Product, m.Variant, m.Quantity * obj.Quantity, obj.Id, obj.SerialNumber))
                    .Concat(obj.Product.Accessories
                        .Where(m => m.BundleGroup != null && 0 != (m.Accessory.Flags & (int)ProductFlags.SerialNumber) && obj.OrderDetailBundles.Any(z => z.Product.Id == m.Accessory.Id && (m.AcceVariant == null || m.AcceVariant.Id == z.Variant.GetId(0))))
                        .Select(m => new System.Tuple<ProductBase, VariantBase, int, int, string>(m.Accessory, null, obj.Quantity, obj.Id, obj.SerialNumber))).ToList();

            return new[] { new System.Tuple<ProductBase, VariantBase, int, int, string>(obj.Product, obj.Variant, obj.Quantity, obj.Id, obj.SerialNumber) }.ToList();
        }
        #endregion
        
        #region GetConfirmQty

        public static List<System.Tuple<ProductBase, VariantBase, int, int, string>> GetConfirmQty(this OrderDetail obj)
        {
            return obj.GetConfirmQtyRaw().Where(m => m.Item3 > 0).ToList();
        }
        private static List<System.Tuple<ProductBase, VariantBase, int, int, string>> GetConfirmQtyRaw(this OrderDetail obj)
        {
            if (obj == null) return new List<System.Tuple<ProductBase, VariantBase, int, int, string>>();

            if (obj.Product.Type.InList(ProductType.Bundle))
                return obj.OrderDetailBundles.Select(m => new System.Tuple<ProductBase, VariantBase, int, int, string>(m.Product, m.Variant, obj.Quantity * m.Quantity, obj.Id, m.Product.CombinedBarCode(m.Variant))).ToList();
            
            return new[] { new System.Tuple<ProductBase, VariantBase, int, int, string>(obj.Product, obj.Variant, obj.Quantity, obj.Id, obj.Product.CombinedBarCode(obj.Variant)) }.ToList();
        }
        #endregion

        #region IsRecurOneTime

        public static bool IsRecurOneTime(this long? orderId, IDB db = null)
        {
            if (orderId == null || orderId > Cart.Seed) return false;

            var order = OrderMgr.GetDom(db ?? new DefaultDb(), (int)(orderId ?? 0));

            return order != null && order.IsRecurOneTime();
        }

        public static bool IsRecurOneTime(this Domain.Order order)
        {
            if (order == null || order.Id == 0) return false;

            if (SettingsBiz.GetSettings().Hidden.RecurOneTimeNoOrderCheck)
                return order.OrderStatus.InList(OrderStatus.PaymentWaiting) || (order.OrderStatus.InList(OrderStatus.New) && 0 != (order.Flags & (int)OrderFlagType.RenewPayment) /*&& order.IsDibsEasy()*/);

            return order.Recurring != null && order.Recurring.Order.Id != order.Id &&
                   (order.OrderStatus.InList(OrderStatus.PaymentWaiting) ||
                    (order.OrderStatus.InList(OrderStatus.New) && 0 != (order.Flags & (int) OrderFlagType.RenewPayment) /*&& order.IsDibsEasy()*/));
        }

        #endregion

        #region IsDibsEasy
        public static bool IsDibsEasy(this Domain.Order order)
        {
            if (order == null || order.PaymentMethod == null) return false;

            return ((DIBSSetting)((PaymentMethod)order.PaymentMethod).GetSetting())
                .SafeProp(m => m.DIBSPlatform).InList(DIBSPlatform.Easy);
        } 
        #endregion

        #region GetOfferDiscount
        
        public static decimal GetOfferDiscount(this Domain.Order order)
        {
            if (order == null || 0 == ((int) OrderFlagType.OfferHideDiscPerProd & order.Flags)
                || order.OrderDetails.Count(m => m.Offered == true) == 0) return 0;

            return order.OrderDetails.Where(m => /*m.Offered == true && */ m.PriceOrigin != null)
                .Sum(m => m.Quantity*m.Product.IncVat(m.PriceOrigin.Value - m.Price) ?? 0);
        }

        public static decimal GetOfferDiscount(this OrderModel order)
        {
            if (order == null || !(order.OfferHideDiscPerProd ?? false)
                || order.OrderDetails.Count == 0) return 0;

            return order.OrderDetails.Where(m => m.Id == 0 ? m.PriceOriginExInc != null : m.PriceOriginInc != null)
                .Sum(m => m.Quantity * ((m.Id == 0 ? m.PriceOriginExInc.Value : m.PriceOriginInc.Value) - m.PriceInc) ?? 0);
        }

        #endregion

        #region GetSveaCoName
        public static string GetSveaCoName(this string name)
        {
            if (name.IsNullOrEmpty()) return name;

            var coTempl = string.Format("name_CO_Addr".ModLocalize(" (C/O: {0})"), "").Trim().Trim('(', ')', '[', ']').Trim();
            var idxB = name.IndexOf(coTempl, StringComparison.OrdinalIgnoreCase);

            if (idxB == -1) return null;

            var idxE = name.IndexOfAny(new[] { ')', ']' }, idxB);

            return idxE == -1
                ? name.Substring(idxB + coTempl.Length).Trim()
                 : name.Substring(idxB + coTempl.Length, idxE - idxB - coTempl.Length).Trim();
        }
        #endregion

        #region GetNvitItems
        public static List<Logistra.PackageItem> GetNvitItems(this Domain.Order order)
        {
            if (order == null) return new List<Logistra.PackageItem>();

            return order.OrderDetails
                .SelectMany(m => m.Product.Type.InList(ProductType.Bundle)
                    ? m.OrderDetailBundles.Select(z => new { m.Id, z.Product, z.Variant, z.ProductName, z.ProductSKU, z.Quantity, Vat = z.Product.Vat.Value, PriceInc = z.Product.CombinedSalePriceInc(z.Variant) })
                    : new[] { new { m.Id, m.Product, m.Variant, m.ProductName, m.ProductSKU, m.Quantity, m.Vat, m.PriceInc } }
                )
                .Select(m => new Logistra.PackageItem()
                {
                    NvitCode = m.Product?.CombinedNvitCode(m.Variant),
                    Quantity = m.Quantity,
                    Description = m.ProductName,
                    Currency = order.Currency,
                    Amount = ((double?)m.PriceInc ?? 0),
                    WeightInKg = (double?)m.Product?.WeightInKg(null, m.Variant) ?? 0,
                    OriginCountry = m.Product?.OriginCountry?.Code,
                }).Where(m => m.NvitCode?.Length > 0).ToList();
        }
        #endregion
    }
}