using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Web;
using AutoMapper;
using Claw.Controls.Modules;
using Claw.Core;
using Claw.Modules.Eshop.Helper;
using Claw.Modules.Eshop.Payment.Helper;
using Claw.Modules.Eshop.Product.DAL;
using Eshop.Payments.KlarnaCheckout;
using Eshop.Payments.Vipps;
using Kendo.Mvc.Infrastructure;
using Lucene.Net.Search;
using Newtonsoft.Json.Serialization;
using NHibernate;
using System.Web.UI.WebControls;
using Claw.Core.DAL;
using Claw.Modules.Eshop.Controllers.Admin;
using Claw.Modules.Eshop.Domain;
using Claw.Modules.Eshop.Models;
using NHibernate.Linq;
using NHibernate.Linq.Functions;
using NPOI.SS.Formula.Functions;
using Claw.Modules.Eshop.DAL.Interfaces;
using Claw.Modules.Eshop.Domain.Interfaces;
using Newtonsoft.Json;
using Claw.Core.Logs;
using Quartz.Util;
using System.Linq.Expressions;
using System.IO;
using System.Web.Hosting;

namespace Claw.Modules.Eshop.DAL
{
    public interface IOrderMgr2 : IDalMgr
    {
        void ClearReportCache(IDB db, ReportType type, DateTime date, bool useTransaction = true);
        decimal GetLiveShippingCost(IShipping shipping, Domain.Order order);
    }

    public class OrderMgr : BaseMgr<OrderModel, Domain.Order>, IOrderMgr
    {
        public static volatile Func<OrderModel, Domain.Order, ISession, bool> FuncSaveorUpdate = null;
        public static volatile Action<Domain.Order, IDB> FuncPaymentDone = null;
        public static volatile Func<Domain.Order, IDB, Domain.Order[]> FuncPaymentDone2 = null;
        public static Func<IDB, string, string, List<System.Tuple<string, string>>, System.Tuple<string, string>> FuncHsqlList = null;

        static OrderMgr() { Init(typeof(OrderMgr)); }

        #region GetAll

        /// <summary>Cpos</summary>
        public static List<OrderModel> GetAll(IDB db, byte? orderStatus, string email,
            int pageIndex, int pageSize, out int totalRows, SortDirection sortDirection = SortDirection.Descending,
            string sortField = "Id", string name = null, string refId = null, string productName = null,
            DateTime? dateFrom = null, DateTime? dateTo = null, int? paymentId = null, int? shippingId = null,
            bool? backorder = null, int? backproductId = null, int? customerid = null, int? recurringId = null,
            bool? shippingService = null, bool pendingMailbox = false, bool bringShipping = false, int? type = null,
            bool noRowcount = false, string odTag = null)
        {
            return GetAllEx(db, orderStatus, email,
                pageIndex, pageSize, out totalRows, sortDirection, sortField, name, refId, productName,
                dateFrom, dateTo, paymentId, shippingId, backorder, backproductId, customerid, recurringId,
                shippingService, pendingMailbox, bringShipping, type, noRowcount, odTag, userKeyword: null, datafields: null);
        }

        public static List<OrderModel> GetAllEx(IDB db, byte? orderStatus, string email,
            int pageIndex, int pageSize, out int totalRows, SortDirection sortDirection = SortDirection.Descending,
            string sortField = "Id", string name = null, string refId = null, string productName = null,
            DateTime? dateFrom = null, DateTime? dateTo = null, int? paymentId = null, int? shippingId = null,
            bool? backorder = null, int? backproductId = null, int? customerid = null, int? recurringId = null,
            bool? shippingService = null, bool pendingMailbox = false, bool bringShipping = false, int? type = null,
            bool noRowcount = false, string odTag = null, bool? tilRegnskap = null, string userKeyword = null, string[] datafields = null,
            int? companyId = null, bool porterbuddy = false, bool? flagcustom0 = null, bool? flagcustom1 = null, string serialNumber = null,
            bool? renewPayment = null, bool? sent2PickupPoint = null, bool? dropShip = null)
        {
            var args = new List<System.Tuple<string, string>>();                                              
            var sortByProdName = sortField == "ProductName";
            var distinct = !string.IsNullOrEmpty(userKeyword) || !string.IsNullOrEmpty(productName) || !string.IsNullOrEmpty(serialNumber) || backorder != null || backproductId != null || sortByProdName || (customerid > 0 && !odTag.IsNullOrEmpty());
            var hSql = string.Format(@"
            select {1} ord.Id {2} from {0} as ord ", typeof(Domain.Order).FullName, "", sortByProdName ? ", min(odetail.ProductName)" : ", '' ");

            if (distinct) hSql += " join ord.OrderDetails as odetail ";

            if (backorder != null)
            {
                hSql += @"  join odetail.Product as prod 
                            left join odetail.Variant as vari 
                            join prod.ProductStatus as prodStatus 
                            left join vari.ProductStatus as variStatus ";
                orderStatus = null;
            }

            if (customerid > 0 && !odTag.IsNullOrEmpty())
            {
                hSql += " join ord.OrderDetailTags as odtag ";
                if (companyId != null) hSql += " join odtag.Customer as custTag ";
            }

            hSql += " where 1 = 1 ";

            if (porterbuddy) hSql += string.Format(@" and ord.ShippingService = {0} ", ShippingServiceType.PorterBuddy.Val());

            if (backproductId != null)
            {
                var backprod = ProductMgr.GetDom(db, backproductId ?? 0);
                hSql += string.Format(@" and odetail.Product.Id in ({0}) 
                and ord.OrderStatus in ({1},{2},{3},{4}) ", new[] { backproductId ?? 0 }
                    .Concat(backprod == null ? new int[0] : backprod.BundlingRefs.Select(m => m.Bundle.Id).ToArray())
                    .ToString(","), OrderStatus.New.Val(), OrderStatus.Processing.Val(), OrderStatus.RestOrder.Val(), OrderStatus.PayBeforeDelivery.Val());
            }
            else
                if (orderStatus != null) hSql += " and ord.OrderStatus = " + orderStatus;

            if (customerid != null)
                if (companyId != null) hSql += string.Format(" and (ord.Company.Id = {0} or ord.Customer.Id = {1})", companyId, customerid);
                else hSql += " and ord.Customer.Id = " + customerid;

            if (type != null) hSql += " and ord.Type = " + type;

            if (!string.IsNullOrEmpty(refId))
                if (Regex.IsMatch(refId, @"^\d+$"))
                {
                    if (long.Parse(refId) > Cart.Seed) hSql += " and ord.CartId = " + (long.Parse(refId) - Cart.Seed);
                    else hSql += string.Format(" and (ord.ReservationId = '{0}' or ord.Id = {0} or ord.Invoice.Id = {0})", refId);
                }
                else hSql += " and ord.ReservationId = '" + refId + "'";

            if (!string.IsNullOrEmpty(userKeyword))
            {
                hSql += " and (odetail.ProductName like :userKeyword or odetail.ProductSKU like :userKeyword ";

                if (userKeyword.TryParseInt() != null) hSql += " or ord.Id = " + userKeyword;

                if (datafields.SafeCount() != 0)
                    hSql += " or " + datafields.Select((f, i) =>
                    {
                        if (f.IsNullOrEmpty()) return null;
                        args.Add(new System.Tuple<string, string>(
                            "userKeyword" + i,
                            "%\"" + f.TrimEnd('$') + "\":\"%" + userKeyword + (i == datafields.Length - 1 || f.EndsWith("$") ? "%\"%" : "%\",\"" + datafields[i + 1] + "\"%"
                        )));
                        return " odetail.Data like :userKeyword" + i;
                    }).Where(m => m != null).ToString(" or ");

                hSql += ")";

                args.Add(new System.Tuple<string, string>("userKeyword", "%" + userKeyword + "%"));
            }

            if (!string.IsNullOrEmpty(productName))
            {
                //var prodList = ProductMgr.Find(db, productName);
                //hSql += " and odetail.Product.Id in (" + (prodList.Count == 0 ? "0" : prodList.Select(x => x.Id).ToString(",")) + ")";
                hSql += " and odetail.ProductName like :productname ";
                args.Add(new System.Tuple<string, string>("productname", "%" + productName + "%"));
            }

            if (!string.IsNullOrEmpty(serialNumber))
            {
                hSql += " and odetail.SerialNumber like :serialNumber ";
                args.Add(new System.Tuple<string, string>("serialNumber", "%" + serialNumber + "%"));
            }

            if (!string.IsNullOrEmpty(email)) hSql += " and ord.Customer.Email like '" + TranslateKWord(email) + "%'";

            if (!string.IsNullOrEmpty(name))
            {
                hSql += " and (ord.Customer.ContactName like :name or ord.Customer.CompanyName like :name or ord.Customer.DeliverName like :name or ord.Customer.BillingName like :name)";
                args.Add(new System.Tuple<string, string>("name", "%" + name + "%"));
            }

            if (dateFrom != null) hSql += string.Format(" and ord.CreatedDate >= '{0:yyyy-MM-dd HH:mm:ss}'", dateFrom);
            if (dateTo != null) hSql += string.Format(" and ord.CreatedDate < '{0:yyyy-MM-dd HH:mm:ss}'", dateTo);
            if (paymentId != null) hSql += " and ord.PaymentMethod.Id = " + paymentId;
            if (shippingId != null) hSql += " and ord.Shipping.Id = " + shippingId;
            
            if (backorder == true) 
                hSql += string.Format(@" and (
                    (ord.OrderStatus in ({0},{2},{3}) and ((vari is null and prodStatus.DescInstock = 1 and isnull(odetail.StockQty,-1*prod.BackOrderQty) < 0) or (vari is not null and variStatus.DescInstock = 1 and isnull(odetail.StockQty,-1*vari.BackOrderQty) < 0))) 
                    or ord.OrderStatus = {1})", OrderStatus.New.Val(), OrderStatus.RestOrder.Val(), OrderStatus.Processing.Val(), OrderStatus.PayBeforeDelivery.Val());

            if (backorder == false)
                hSql += string.Format(@" and ord.OrderStatus in ({0},{1},{2},{3}) ", OrderStatus.New.Val(), OrderStatus.PayBeforeDelivery.Val(), OrderStatus.RestOrder.Val(), OrderStatus.Processing.Val());

            if (recurringId != null)
            {
                var dRecur = RecurOrderMgr.GetDom(db, recurringId ?? 0);
                hSql += string.Format(" and (ord.Recurring.Id = {0} or ord.Id = {1})", recurringId, dRecur == null ? 0 : dRecur.Order.Id);
            }
            if (shippingService != null)
                hSql += string.Format(" and ord.OrderStatus in ({1},{2},{3},{4},{5}) and ord.ShippingService is {0} null and ord.ShippingData is not null", (shippingService.Value ? "not" : ""),
                    OrderStatus.New.Val(), OrderStatus.PayBeforeDelivery.Val(), OrderStatus.Sent.Val(), OrderStatus.RestOrder.Val(), OrderStatus.Processing.Val());

            if (pendingMailbox) hSql += " and ord.BringBookingStatus = 1 ";
            if (bringShipping) hSql += " and ord.BringBookingStatus in (2,3) ";

            if (customerid > 0 && !odTag.IsNullOrEmpty())
            {
                if (companyId != null) hSql += string.Format(" and odtag.Tag like :odTag and (custTag.Company.Id = {0} or odtag.Customer.Id = {1})", companyId, customerid);
                else hSql += " and odtag.Tag like :odTag and odtag.Customer.Id = " + customerid;
                args.Add(new System.Tuple<string, string>("odTag", odTag));
            }

            if (tilRegnskap != null)
            {  
                hSql += string.Format(" and ord.OrderStatus in ({0}, {1}, {2}) ", OrderStatus.New.Val(), OrderStatus.Processing.Val(), OrderStatus.RestOrder.Val());
                hSql += string.Format(" and 0 {0} (isnull(cast(ord.ExtraInfo3 as int), 0) & {1}) ", tilRegnskap == true ? "<>" : "=", OrderFlagType.TilRegnskap.Val());
            }

            if (flagcustom0 != null || flagcustom1 != null)
            {
                hSql += string.Format(" and ord.OrderStatus in ({0}, {1}, {2}) ", OrderStatus.New.Val(), OrderStatus.Processing.Val(), OrderStatus.RestOrder.Val());
                if (flagcustom0 != null)
                    hSql += string.Format(" and 0 {0} (isnull(cast(ord.ExtraInfo3 as int), 0) & {1}) ", flagcustom0 == true ? "<>" : "=", OrderFlagType.Custom0.Val());
                if (flagcustom1 != null)
                    hSql += string.Format(" and 0 {0} (isnull(cast(ord.ExtraInfo3 as int), 0) & {1}) ", flagcustom1 == true ? "<>" : "=", OrderFlagType.Custom1.Val());
            }

            if (dropShip != null)
                hSql += dropShip.Value
                    ? string.Format(" and ord.FlagsEx >= {0} and (ord.FlagsEx & {0}) = {0} ", OrderFlagType.DropShipMark.Val())
                    : string.Format(" and (ord.FlagsEx is null or (ord.FlagsEx & {0}) = 0) ", OrderFlagType.DropShipMark.Val());


            if (renewPayment != null)
                hSql += string.Format(" and ord.OrderStatus in ({0}, {1}) and 0 {2} (isnull(cast(ord.ExtraInfo3 as int), 0) & {3}) ", OrderStatus.New.Val(), OrderStatus.PaymentWaiting.Val(), renewPayment == true ? "<>" : "=", OrderFlagType.RenewPayment.Val());

            if (sent2PickupPoint != null)
                hSql += string.Format(" and ord.OrderStatus in ({0}) and 0 {1} (isnull(cast(ord.ExtraInfo3 as int), 0) & {2}) ", OrderStatus.Processing.Val(), sent2PickupPoint == true ? "<>" : "=", OrderFlagType.Sent2PickupPoint.Val());
            
            var hSqlCount = hSql.Substring(hSql.IndexOf(" from ", StringComparison.OrdinalIgnoreCase));

            if (distinct) hSql += " group by ord.Id ";

            if (backorder == false)
                hSql += string.Format(@" having count(*) = sum(case when (vari is null and (prodStatus.DescInstock = 0 or isnull(odetail.StockQty,-1*prod.BackOrderQty) >= 0)) or (vari is not null and (variStatus.DescInstock = 0 or isnull(odetail.StockQty,-1*vari.BackOrderQty) >= 0)) then 1 else 0 end) ");

            #region Sorting

            switch (sortField)
            {
                case "CreatedDate.Text":
                    sortField = "CreatedDate";
                    break;
            }

            hSql += string.Format(" order by {0} {1}", sortByProdName ? "min(odetail.ProductName)" : (distinct && sortField.InList("ReportDate", "DeliveredDate", "CreatedDate") ? "min(ord."+ sortField + ")" : "ord." + sortField), GetDir(sortDirection));

            #endregion

            if (FuncHsqlList != null)
            {
                var tmp = FuncHsqlList(db, hSql, hSqlCount, args);
                hSql = tmp.Item1;
                hSqlCount = tmp.Item2;
            }

            var ids = GetList<object[]>(db, hSql, pageIndex, pageSize, out totalRows, noRowcount ? null : "select count(" + (distinct ? "distinct" : "") + " ord) " + hSqlCount, args.ToArray())
                .Select(m => new { Id = (int)m[0], ProductName = (string)m[1] }).ToList(); ;

            var ret = GetList<Domain.Order>(db, ids.Select(m => m.Id).ToList(), sortDirection, sortByProdName ? "Id" : sortField, "ord");
            
            var ret2 = Mapper.Map<List<Domain.Order>, List<OrderModel>>(ret);

            if (sortByProdName)
                ret2 = ids.Select((m, i) => new {m.Id, m.ProductName, Idx = i})
                        .Join(ret2, x => x.Id, y => y.Id, (x, y) => new { x.Idx, Order = y, x.ProductName })
                        .OrderBy(m => m.Idx).Action(m => m.Order.ProductName = m.ProductName)
                        .Select(m => m.Order).ToList();

            return ret2;
        }

        public static List<OrderModel> GetOrderByEmail(IDB db, string email)
        {
            int total;
            return GetAll(db, null, email, 1, Int32.MaxValue, out total,
                SortDirection.Ascending, "CreatedDate", null);
        }

        public static List<OrderModel> GetOrderByRefId(IDB db, string refId)
        {
            int total;
            return GetAll(db, null, null, 1, Int32.MaxValue, out total,
                SortDirection.Ascending, "CreatedDate", null, refId);
        }
        #endregion

        #region SaveorUpdate.Raw
        public static Domain.Order SaveorUpdate(IDB db, OrderModel model, out Customer customer, out Domain.Order backorder, out List<RecurOrder> deletedRecurrings,
            List<Coupon> couponClearLst, List<Domain.Discount> discountClearLst, bool admin = false,
            bool refresh = false, bool overridePayCost = false, bool overrideShipCost = false, bool editCustomer = false,
            List<Action<ISession>> fActs = null, bool editCompany = false, bool editDetail = false)
        {
            return SaveorUpdate(db, model, false, out customer, out backorder, out deletedRecurrings, couponClearLst, discountClearLst,
                admin, refresh, overridePayCost, overrideShipCost, editCustomer, fActs, editCompany, editDetail);
        }
        public static Domain.Order SaveorUpdate(IDB db, OrderModel model, bool noUpdate, out Customer customer, out Domain.Order backorder, out List<RecurOrder> deletedRecurrings,
             List<Coupon> couponClearLst, List<Domain.Discount> discountClearLst, bool admin = false,
            bool refresh = false, bool overridePayCost = false, bool overrideShipCost = false, bool editCustomer = false,
            List<Action<ISession>> fActs = null, bool editCompany = false, bool editDetail = false)
        { 
            customer = null;
            backorder = null;
            deletedRecurrings = null;

            var settings = SettingsBiz.GetSettings();

            if (model == null || model.ExchangeRate <= 0m || (model.Id == 0 && !refresh && model.OrderDetails.SafeCount() == 0) ||
                (model.Type.InList(OrderType.Pos) && !model.OrderStatus.InList(OrderStatus.Sent, OrderStatus.PaymentWaiting, OrderStatus.Credit)) ||
                (model.Type.InList(OrderType.Web) && model.Recurring.SafeProp(m => m.OrderId ?? 0) != model.Id && !model.OrderStatus.InList(OrderStatus.PaymentWaiting, OrderStatus.New, OrderStatus.Processing, OrderStatus.RestOrder, OrderStatus.PayBeforeDelivery))) //rilk
                return null;

            //if (!model.GiftCardTotal.IsNull() && (model.GiftCards.SafeCount() == 0 || model.GiftCardTotal != model.GiftCards.Sum(x => x.Item2))) return null;

            if (!settings.Hidden.RecurringOrder && model.Recurring != null) return null;

            if (model.Id == 0 && model.Recurring != null && model.OrderDetails.All(m => (m.RecurQty ?? 0) == 0)) return null;

            #region Variables

            customer = CustomerMgr.ToDom(db, model.Customer, null, false, editCustomer);
            var obj = GetDom(db, model.Id) ?? new Domain.Order()
            {
                CreatedDate = DateTime.Now,
                DiscountValue = 0,
                CouponValue = 0,
                PaymentCost = 0,
                PaymentVat = 0,
                ShippingCost = 0,
                ShippingVat = 0,
                VatTotalOri = 0,
                VatTotalRes = 0,
                OrderTotalEx = 0,
                OrderTotalRes = 0,
                TotalCouponRes = 0,
                TotalDiscountRes = 0,
                OrderTotalOri = 0,
                PaymentCostAdm = 0,
                ShippingCostAdm = 0,
                VatTotalAdmOri = 0,
                OrderTotalAdmOri = 0,
                TotalCouponAdm = 0,
                TotalDiscountAdm = 0,
                RoundingUp = 0,
                PackerId = null,
                PackerName = null,
                PackingStart = null,
                PackingEnd = null,
            };

            var bkOrderTotal = obj.OrderTotalAdm;
            var bkItemTotal = obj.GetOrderDetails().Count;
            var bkCoupon = obj.Coupon;
            var bkDiscount = obj.Discount;
            var restOrder = model.OrderStatus == (byte)OrderStatus.RestOrder;
            var fcalc = model.OrderStatus.InList(OrderStatus.PaymentWaiting) && model.OrderDetails != null;
            var exchangeRate = ((decimal?)model.ExchangeRate) ?? 1m;

            #endregion

            if (model.Recurring.SafeProp(m => m.OrderId ?? 0) != model.Id && obj.OrderStatus.InList(OrderStatus.Credit, OrderStatus.Cancelled, OrderStatus.Sent)) //OrderStatus rilk
                return null;
            
            #region Order

            #region Basic

            if (editCompany && obj.Id != 0)
            {
                obj.OrgNumber = model.OrgNumber;
                obj.CompanyName = model.CompanyName;
                obj.InvoiceEmail = model.InvoiceEmail;
            }
            else
            {
                obj.OrgNumber = (model.Customer.Individual == true ? null : model.Customer.OrgNumber) ?? "";
                obj.CompanyName = (model.Customer.Individual == true ? null : model.Customer.CompanyName) ?? "";
                obj.InvoiceEmail = (model.Customer.Individual == true ? null : model.Customer.InvoiceEmail) ?? "";
            }

            obj.BillingName = model.BillingName;
            obj.BillingPhone = model.BillingPhone;
            obj.BillingMobile = model.BillingMobile;
            obj.BillingAddress = model.BillingAddress;
            obj.BillingPostnr = model.BillingPostnr;
            obj.BillingPosted = model.BillingPosted;
            obj.BillingCity = model.BillingCity;
            obj.DeliverName = model.DeliverName;
            obj.DeliverPhone = model.DeliverPhone;
            obj.DeliverMobile = model.DeliverMobile;
            obj.DeliverAddress = model.DeliverAddress;
            obj.DeliverPostnr = model.DeliverPostnr;
            obj.DeliverPosted = model.DeliverPosted;
            obj.DeliverCity = model.DeliverCity;
            obj.ShippingTrackingId = model.ShippingTrackingId;
            obj.ReservationId = model.Id == 0 ? null : obj.ReservationId;
            obj.TransactionId = model.TransactionId;
            obj.OrderStatus = model.OrderStatus;
            obj.Comments = model.Comments;
            obj.LangCode = model.LangCode;
            obj.Currency = model.Currency;
            obj.ExchangeRate = model.ExchangeRate;
            obj.UserIp = model.UserIp;
            obj.UserHost = model.UserHost;
            obj.UserAgent = model.UserAgent;
            obj.Credited = model.Credited;
            obj.ExtraInfo1 = model.ExtraInfo1;
            obj.ExtraInfo2 = model.ExtraInfo2;
            obj.ExtraInfo3 = model.ExtraInfo3;
            obj.ExtraInfo4 = model.ExtraInfo4;
            obj.DeliveredDate = model.Id == 0 ? null : model.DeliveredDate;
            obj.ExpectedDelivery = model.ExpectedDelivery;
            obj.ModifiedDate = DateTime.Now;
            //Bring
            obj.ExtraService = model.ExtraService;
            obj.BringInfo = model.BringInfo;
            obj.ShippingPickupPoint = model.ShippingPickupPoint;
            obj.ShippingService = model.ShippingService;
            obj.CartId = model.CartId;
            obj.Flags = model.Flags;  
            obj.GiftCardTotal = model.GiftCardTotal;
            obj.GiftCardData = model.GiftCardData;

            obj.OfferHideDiscPerProd = model.OfferHideDiscPerProd;
            obj.OfferExpiredDate = model.OfferExpiredDate;
            obj.OfferToken = model.OfferToken;
            obj.OfferMsg = model.OfferMsg;
            obj.OfferSmsMsg = model.OfferSmsMsg;
            obj.OfferSendSms = model.OfferSendSms;
            obj.OfferEmailSent = model.OfferEmailSent;
            obj.CustomDiscountRate = model.CustomDiscountRate;
            obj.OfferProfitPerProd = model.OfferProfitPerProd;
            obj.RecurringTokenOnly = model.RecurringTokenOnly;
            obj.RecurringReturnUrl = model.RecurringReturnUrl;
            #endregion

            if (obj.OrderStatus.InList(OrderStatus.RestOrder)) return obj;

            #region FE Summary

            obj.TotalCouponOri = model.TotalCoupon + obj.TotalCouponRes;
            obj.TotalDiscountOri = model.TotalDiscount + obj.TotalDiscountRes;
            obj.PaymentCost = model.PaymentCost;
            obj.ShippingCost = model.ShippingCost;
            obj.OrderTotalOri = model.OrderTotal + obj.OrderTotalRes;
            obj.VatTotalOri = model.VatTotal + obj.VatTotalRes;

            obj.FinalTotal = model.FinalTotal;

            #endregion

            obj.VatEnabled = CountryMgr.IsVatEnable(db, model.DeliverCountryId ?? 0);
            obj.Invoice = InvoiceMgr.GetDom(db, model.InvoiceId ?? 0);
            obj.OriginRest = GetDom(db, model.OriginalBackOrderId ?? 0);
            obj.DeliverCountry = CountryMgr.GetDom(db, model.DeliverCountryId ?? 0);
            obj.BillingCountry = CountryMgr.GetDom(db, model.BillingCountryId ?? 0);

            customer.OrderNum = null; customer.StatsInfo = null;

            var taxCountryCode = obj.GetTaxCountryCode(); 
            var vatEnabled = CountryMgr.IsVatEnable(string.IsNullOrEmpty(taxCountryCode) ? "no" : taxCountryCode);                

            #region Shipping

            if (!restOrder && (fcalc || obj.Shipping.GetId(0) != (model.ShippingId ?? 0)))
            {
                if (null != (obj.Shipping = Singleton<IShippingMgr>.Default.GetDom(db, model.ShippingId ?? 0)))
                {
                    obj.ShippingName = obj.Shipping.GetName();
                    obj.ShippingVat = obj.VatEnabled ? obj.Shipping.Vat.Value : 0;
                    obj.ShippingCostAdm = overrideShipCost || model.DiscountValue2 < 0 ? (decimal?)model.ShippingCost : (exchangeRate * model.GetShippingFee(db));

                    var shipping = (Domain.Shipping)obj.Shipping;
                    obj.ShippingService = obj.ShippingService ??
                        (shipping.ServiceProvider.InList(ShippingService.LogistraPostNord, ShippingService.LogistraBring) ? (int)ShippingServiceType.Logistra : (shipping.ServiceProvider.InList(ShippingService.PostNord) ? (int)ShippingServiceType.Postnord : (int?)null));

                    if (obj.ShippingService == null && !obj.ShippingPickupPoint.IsNullOrEmpty() && shipping.ServiceProvider.InList(ShippingService.Bring))
                        obj.ShippingService = (int) ShippingServiceType.Bring;

                    shipping.ClearStatistics(null);
                }
                else
                {
                    obj.ShippingName = null;
                    obj.ShippingVat = null;
                }
            }

            #endregion

            #region PaymentMethod

            if (!restOrder && (fcalc || obj.PaymentMethod.GetId(0) != (model.PaymentMethodId ?? 0)))
            {
                if (null != (obj.PaymentMethod = Singleton<IPaymentMethodMgr>.Default.GetDom(db, model.PaymentMethodId ?? 0)))
                {
                    obj.PaymentName = obj.PaymentMethod.GetName();
                    obj.PaymentCostAdm = overridePayCost ? (decimal?)model.PaymentCost : (obj.PaymentMethod.GetFee(db, taxCountryCode, (model.GetRawOrderTotal(taxCountryCode) ?? 0) - obj.TotalCoupon - obj.TotalDiscount - obj.GiftCardTotal + obj.DepositTotal) * exchangeRate);
                    obj.PaymentVat = obj.VatEnabled ? obj.PaymentMethod.TaxRate.Value : 0;

                    ((PaymentMethod) obj.PaymentMethod).ClearStatistics(null);
                }
                else
                {
                    obj.PaymentName = null;
                    obj.PaymentCostAdm = 0;
                    obj.PaymentVat = null;
                }
            }

            #endregion

            #endregion

            #region Discount

            if (obj.Id == 0 || fcalc || obj.OrderTotalAdm != bkOrderTotal || obj.OrderDetails.Count != bkItemTotal ||
                obj.Discount.GetId(0) != (model.DiscountId ?? 0))
            {
                if (null != (obj.Discount = DiscountMgr.GetDom(db, model.DiscountId ?? 0)))
                {
                    obj.DiscountName = obj.Discount.GetName();
                    obj.DiscountValue = exchangeRate * obj.Discount.Validate((model.GetRawOrderTotal(taxCountryCode) ?? 0) / exchangeRate, model.OrderDetails.Count) ?? 0;
                }
                else
                {
                    obj.DiscountName = null;
                    obj.DiscountValue = 0;
                }
            }

            if (bkDiscount.GetId(0) != obj.Discount.GetId(0))
                discountClearLst.AddRange(new[] { bkDiscount, obj.Discount });

            obj.TotalDiscountAdm = obj.GetDiscountTotal() ?? 0;

            #endregion

            #region OrderDetails

            if (model.OrderDetails != null && !restOrder)
            {
                foreach (var item in model.OrderDetails)
                {
                    #region Basic

                    int mid = item.Id, productid = item.ProductId ?? 0, variantid = item.VariantId ?? 0;
                    var timestamp = item.Timestamp;
                    OrderDetail ditm;

                    if (obj.OrderStatus.InList(OrderStatus.PaymentWaiting) && !model.Type.InList(OrderType.Pos) && !model.IsProdRecurring() && model.OrderDetails.All(m => m.Data.IsNullOrEmpty()))
                        ditm = obj.OrderDetails.FirstOrDefault(x => x.Product?.Id == productid && x.Variant.GetId(0) == variantid && (productid != 1 || (x.Id > 0 && x.Id == mid) || (x.Timestamp > 0 && x.Timestamp == timestamp)));
                    else ditm = (mid == 0 ? null : obj.OrderDetails.FirstOrDefault(x => x.Id == mid));

                    var oldQty = ditm == null ? 0 : ditm.QuantityOri;
                    if (ditm == null)
                        obj.OrderDetails.Add(ditm = new OrderDetail()
                        {
                            Order = obj,
                            Product = Product.DAL.ProductMgr.GetDom(db, item.ProductId ?? 0),
                            Variant = Product.DAL.VariantMgr.GetDom(db, item.VariantId ?? 0),
                            DiscountValue = 0,
                            CouponValue = 0,
                            SerialNumber = item.SerialNumber,
                        });

                    item.Id = ditm.Id;

                    ditm.Timestamp = item.Timestamp;
                    ditm.Vat = (obj.VatEnabled ? (editDetail && item.ProductId == 1 && item.Vat >= 0 ? (decimal?)item.Vat : ditm.Product.GetVat()) : null) ?? 0;
                    ditm.QuantityOri = item.Quantity ?? 0;
                    
                    var vat = new Vat() { Value = obj.VatEnabled ? ditm.Vat : 0 };
                    if (ditm.Id == 0 && !admin)
                    {
                        ditm.Price = (item.OriginPriceInc.IsNull() || editDetail ? (decimal?)item.Price : vat.ExcVat(item.OriginPriceInc)) ?? 0;
                        ditm.PriceInc = item.OriginPriceInc.IsNull() || editDetail ? item.PriceInc : item.OriginPriceInc;
                        ditm.PriceAdm = item.ProductId == 1 ? (decimal?)item.Price : (exchangeRate * ditm.Product?.CombinedSalePrice(ditm.Variant) ?? 0);
                        ditm.DiscountValue = (item.OriginPriceInc.IsNull() || editDetail ? 0 : item.OriginPriceInc - item.PriceInc)/* + item.DiscountValue2*/;
                        ditm.DiscountValue2 = item.DiscountValue2;
                        //ditm.Discount2 = DiscountMgr.GetDom(db, item.DiscountId2 ?? 0);
                        //ditm.Discount = ditm.Discount ?? ditm.Discount2;
                        obj.ItemDiscountTotal += ditm.QuantityOri*(ditm.DiscountValue ?? 0);
                        obj.TotalDiscountAdm += ditm.QuantityOri*(ditm.DiscountValue ?? 0);
                    }
                    else
                    {
                        ditm.Price = vat.ExcVat(item.PriceInc) ?? 0;
                        ditm.PriceInc = item.PriceInc;
                    }

                    if (ditm.Id == 0 || ditm.PriceOrigin == null)
                        ditm.PriceOrigin = (ditm.Variant == null ? ditm.Product?.Price : ditm.Variant?.Price) * (settings.Hidden.OrderPriceOriginExchangeRate ? exchangeRate : 1m);

                    ditm.RecurQty = item.RecurQty;
                    ditm.RecurType = item.RecurType;
                    ditm.RecurEach = item.RecurEach;
                    ditm.Data = item.Data;
                    ditm.Offered = item.Offered;
                    ditm.CostPrice = ditm.Product?.CombinedCostPrice(ditm.Variant);
                    ditm.Delivered = model.Id != 0 && item.Delivered;
                    ditm.ProductName = (editDetail ? item.ProductName : null).IfNull(ditm.Product.GetName(ditm.Variant));
                    ditm.ProductSKU = ditm.Product?.CombinedSKU(ditm.Variant);
                    ditm.ProductShippingType = ditm.Product?.ShippingType ?? (int)ShippingType.RequireShipping;
                    ditm.ProductWeight = ditm.Product?.WeightInKg(settings, ditm.Variant) ?? 0;
                    ditm.ShippingCostExtra = ditm.Product?.ShippingCostExtra;
                    ditm.DepositAmount = ditm.Product?.DepositAmount;
                    ditm.ProductWUnit = (Byte)WeightUnit.Kg;
                    if (ditm.Product != null)
                    {
                        ditm.Product.OrderDetailNum = null;
                        ditm.Product.TotalSales = null;
                    }

                    #region Pos
                    if (model.Type.InList(OrderType.Pos))
                    {
                        if (item.ProductId == 1)
                        {
                            ditm.PriceAdm = ditm.Price;
                            ditm.CostPrice = null;
                            ditm.Vat = item.Vat;
                        }
                        ditm.ProductName = item.ProductName ?? ditm.ProductName;
                        ditm.DiscountValue = item.DiscountValue;
                        ditm.CategoryId = ditm.Product.CategoryProducts.FirstOrDefault(x => 0 != (x.Category.Visibility & 2)).SafeProp(x => x.Category).GetId();
                        if (item.GiftCard == true && fActs != null)
                        {
                            var itmMod = item;
                            var cEmail = customer.Email;
                            fActs.Add((sess) =>
                            {
                                ditm.CategoryId = 1;
                                for (var i = 0; i < itmMod.Quantity; i++)
                                {
                                    var gcRet = (IGiftCard)Singleton<IGiftCardMgr>.Default.Create(db, itmMod.PriceInc, "Gavekort_Tilgodelapp".ModLocalize("Gavekort/Tilgodelapp", false),
                                        cEmail, obj.DeliverMobile, DateTime.Now.AddYears(1), itmMod.CreditNote, ditm, db.GetSession());

                                    if (gcRet != null) obj.GiftCardSales += gcRet.Amount;
                                }

                                sess.Update(obj);
                            });

                            ditm.GC2reditNote = itmMod.CreditNote ?? false;
                        }
                    }
                    #endregion
                    else if (settings.Hidden.ProductControll && settings.Hidden.OrderControllFlag != 0 && 0 != ((ditm.Product?.Flags ?? 0) & (int)ProductFlags.Controlled))
                        obj.Flags |= settings.Hidden.OrderControllFlag;

                    #endregion

                    #region Discount

                    //var discountChanged = ditm.Discount.GetId(0) != (item.DiscountId ?? 0);
                    //if (fcalc || discountChanged)
                    //{
                    //    if (discountChanged) discountClearLst.Add(ditm.Discount);

                    //    if (null != (ditm.Discount = DiscountMgr.GetDom(db, item.DiscountId ?? 0)) &&
                    //        DiscountMgr.ListAppliable(db, item.ProductId ?? 0, item.VariantId,
                    //            item.Quantity ?? 0, ditm.Discount.Id).Count != 0)
                    //    {
                    //        ditm.DiscountName = ditm.Discount.GetName();
                    //    }
                    //    else { ditm.DiscountName = null; ditm.Discount = null; }

                    //    ditm.DiscountValue = 0;
                    //    if (discountChanged) discountClearLst.Add(ditm.Discount);
                    //}

                    #endregion

                    #region Coupon

                    var couponchanged = ditm.Coupon.GetId(0) != (item.CouponId ?? 0);
                    if (fcalc || couponchanged)
                    {
                        if (couponchanged) couponClearLst.Add(ditm.Coupon);

                        if (null != (ditm.Coupon = CouponMgr.GetDom(db, item.CouponId ?? 0)))
                        {
                            ditm.CouponCode = ditm.Coupon.Code;
                        }
                        else { ditm.CouponCode = null; ditm.CouponValue = 0; }

                        if (couponchanged) couponClearLst.Add(ditm.Coupon);
                    }

                    #endregion

                    #region OrderDetailBundles

                    if (item.OrderDetailBundles != null && ditm.Product != null)
                    {
                        var dataExp = (IDictionary<string, object>)item.Data.ToExpando();
                        var isBundle = ditm.Product.Type.InList(ProductType.Bundle);
                        var bundlevariants = isBundle && dataExp != null
                            ? ((string)dataExp.SafeGet("bundlevariantids") ?? "").Split(',').Select(m => VariantMgr.GetDom(db, m.TryParseInt() ?? 0)).Where(m => m != null).ToList()
                            : new List<Variant>();
                        var bundleAccessoryIds = isBundle && dataExp != null
                            ? ((string)dataExp.SafeGet("bundleaccessoryids") ?? "").Split(',').Where(m => !m.IsNullOrEmpty()).Select(m => m.IndexOf('-') == -1 ? new[] { int.Parse(m) } : m.Split('-').Select(int.Parse).ToArray()).ToList()
                            : new List<int[]>();
                        var odSerials = (ditm.SerialNumber ?? "").Split(new[] { ',', ';' }).Select(z => z.Trim()).Where(z => !z.IsNullOrEmpty()).ToList();

                        foreach (var bundle in ditm.Product.ProductBundles/*.Where(m => m.Product.ValidToShow())*/)
                        {
                            int bmProdId = bundle.Product.Id, bmVarId = bundle.Variant.GetId(0) ?? 0;
                            var bditm = ditm.OrderDetailBundles
                                .FirstOrDefault(x => x.Product.Id == bmProdId && x.Variant.GetId(0) == bmVarId);
                            if (bditm == null)
                                ditm.OrderDetailBundles.Add(bditm = new OrderDetailBundle()
                                {
                                    OrderDetail = ditm,
                                    Product = bundle.Product,
                                    Variant = bundle.Variant ?? bundlevariants.FirstOrDefault(m => m.Product.Id == bmProdId),
                                });

                            bditm.Quantity = bundle.Quantity;
                            bditm.ProductName = bditm.Product.GetName(bditm.Variant);
                            bditm.ProductSKU = bundle.Variant != null ? bundle.Variant.SKU : bundle.Product.SKU;
                            bditm.SerialNumber = odSerials.Where(z => bditm.Product.ProductSerials.Any(w => w.SerialNumber.EqualsIgCase(z))).ToString(", ").IfNull(bditm.SerialNumber);
                        }

                        ditm.OrderDetailBundles.RemoveAll(x => ditm.Product.ProductBundles.
                            All(w => w.Product.Id != x.Product.Id || (w.Variant ?? bundlevariants.FirstOrDefault(m => m.Product.Id == w.Product.Id)).GetId(0) != x.Variant.GetId(0)));

                        bundleAccessoryIds.Join(ditm.Product.Accessories, o => o[0], i => i.Accessory.Id, (o, i) => new { o, i })
                            .Where(m => m.i.BundleGroup != null && (m.i.AcceVariant == null || (m.o.Length > 1 && m.i.AcceVariant.Id == m.o[1])))
                            .Action(m =>
                            {
                                var bditm = new OrderDetailBundle()
                                {
                                    Quantity = 1,
                                    OrderDetail = ditm,
                                    Product = m.i.Accessory,
                                    Variant = m.o.Length == 1 ? null : VariantMgr.GetDom(db, m.o[1]),
                                };

                                bditm.ProductName = bditm.Product.GetName(bditm.Variant);
                                bditm.ProductSKU = bditm.Variant != null ? bditm.Variant.SKU : bditm.Product.SKU;
                                bditm.SerialNumber = odSerials.Where(z => bditm.Product.ProductSerials.Any(w => w.SerialNumber.EqualsIgCase(z))).ToString(", ").IfNull(bditm.SerialNumber);

                                ditm.OrderDetailBundles.Add(bditm);
                            });

                        if (bundlevariants.SafeCount() != 0 && ditm.OrderDetailBundles.Count != 0 && ditm.Id == 0)
                        {
                            ditm.CostPrice = ditm.OrderDetailBundles.Sum(m => m.Quantity * m.Product.CombinedCostPrice(m.Variant));
                            ditm.PriceOrigin = ditm.OrderDetailBundles.Sum(m => m.Quantity * m.Product.CombinedPrice(m.Variant));
                        }
                    }

                    if(settings.Hidden.ProductControll && settings.Hidden.OrderControllFlag != 0 &&
                        ditm.OrderDetailBundles.Select(m => m.Product).Any(m => 0 != (m.Flags & (int)ProductFlags.Controlled)))
                            obj.Flags |= settings.Hidden.OrderControllFlag;

                    #endregion

                    #region OrderDetailGifts

                    if (item.OrderDetailGifts.SafeCount() != 0 && ditm.Discount != null)
                    {
                        var gift = ditm.Discount.Validate(db, item.ProductId ?? 0, item.VariantId, item.Quantity ?? 0);
                        int gmProId = gift.ProductId, gmVarId = gift.VariantId ?? 0;
                        var gditm = ditm.OrderDetailGifts.FirstOrDefault(x => x.Product.Id == gmProId && x.Variant.GetId(0) == gmVarId);
                        if (gditm == null)
                            ditm.OrderDetailGifts.Add(gditm = new OrderDetailGift()
                            {
                                OrderDetail = ditm,
                                Product = Product.DAL.ProductMgr.GetDom(db, gift.ProductId ?? 0),
                                Variant = Product.DAL.VariantMgr.GetDom(db, gift.VariantId ?? 0),
                            });

                        gditm.Quantity = gift.Quantity;
                        gditm.ProductName = gditm.Product.GetName(gditm.Variant);
                        gditm.ProductSKU = gditm.Product.SKU;

                        ditm.OrderDetailGifts.RemoveAll(x =>
                            gditm.Product.Id != x.Product.Id || gditm.Variant.GetId(0) != x.Variant.GetId(0));
                    }

                    #endregion

                    var qtyDelta = oldQty - ditm.QuantityOri;
                    if (!obj.OrderStatus.InList(OrderStatus.PaymentWaiting) && ditm.Product != null)
                    {
                        if (Product.DAL.ProductMgr.UpdateQuantity(db, ditm.Product, ditm.Variant, qtyDelta, false, false, true, false,
                            new ProdQtyLogModel() { Reason = (int)ProdQtyLogReason.Ordered, RefIdFunc = () => obj.Id },
                            bundleData: (ditm.OrderDetailBundles.SafeCount() == 0 ? null : ditm.OrderDetailBundles.Select(m => new System.Tuple<ProductBase, VariantBase, int>(m.Product, m.Variant, m.Quantity)).ToList())))
                        {
                            ditm.Product.OrderDetailNum = null;

                            ditm.StockQty = ditm.GetStockQty(qtyDelta);

                            if (ditm.Product.Type.InList(ProductType.Bundle))
                                ditm.OrderDetailBundles.Action(m => m.StockQty = m.GetStockQty(m.Quantity * qtyDelta));
                        }
                        else return null;
                    }
                }

                foreach (var ditm in obj.OrderDetails)
                {
                    #region Coupon

                    if (ditm.Coupon != null && (fcalc || couponClearLst.Any(x => x != null && x.Id == ditm.Coupon.Id)))
                    {
                        ditm.CouponCode = ditm.Coupon.Code;
                        ditm.CouponValue = exchangeRate * ditm.Coupon.Validate(db, taxCountryCode, ditm.Product.Id, ditm.Variant.GetId(),
                            ((obj.GetRawOrderTotal(taxCountryCode) ?? 0) - (obj.GetDiscountTotal() ?? 0)) / exchangeRate, customer.Email, 1, ditm.DiscountValue ?? 0, vatEnabled ? ditm.PriceInc : ditm.Price) ?? 0;
                    }

                    #endregion

                    ditm.FinalPrice = ditm.GetFinalPrice() ?? 0;
                }

                obj.ShippingCostAdm = overrideShipCost || model.DiscountValue2 < 0 ? (decimal?)model.ShippingCost : (exchangeRate * obj.Shipping.GetFee((obj.GetShipOrderTotal(taxCountryCode) ?? 0) / exchangeRate, obj.GetShipWeightTotal() ?? 0, taxCountryCode, null, obj.GetShipExtraCostTotal()));
                obj.OrderDetails
                    .Where(x => x.Id != 0 && model.OrderDetails.All(w => w.Id != x.Id))
                    .Action(m => m.QuantityOri = 0);

                obj.OrderTotalAdmOri = obj.GetRawOrderTotal(taxCountryCode) ?? 0;
                obj.VatTotalAdmOri = vatEnabled ? (obj.GetVatTotal() ?? 0) : 0;
            }

            #endregion

            #region Coupon

            if (obj.Id == 0 || fcalc || obj.OrderTotalAdm != bkOrderTotal || obj.Coupon.GetId(0) != (model.CouponId ?? 0))
            {
                if (null != (obj.Coupon = CouponMgr.GetDom(db, model.CouponId ?? 0)))
                {
                    obj.CouponCode = obj.Coupon.Code;
                    obj.CouponValue = exchangeRate * obj.Coupon.Validate(db, ((obj.GetRawOrderTotal(taxCountryCode) ?? 0) - (obj.GetDiscountTotal() ?? 0)) / exchangeRate, customer.Email) ?? 0;
                }
                else
                {
                    obj.CouponCode = null;
                    obj.CouponValue = 0;
                }
            }

            if (bkCoupon.GetId(0) != obj.Coupon.GetId(0))
                couponClearLst.AddRange(new[] { bkCoupon, obj.Coupon });

            obj.TotalCouponAdm = obj.GetCouponTotal() ?? 0;

            #endregion

            obj.DepositTotalOri = obj.OrderDetails.Sum(m => m.Quantity*(m.DepositAmount ?? 0));
            obj.FinalTotalAdm = obj.OrderTotalAdm - obj.TotalDiscountAdm - obj.TotalCouponAdm - obj.GiftCardTotal + obj.DepositTotal +
                                (obj.OrderStatus == 5 ? 0 : obj.PaymentCostAdm + obj.ShippingCostAdm);

            #region Discount/Coupon Validate
            if (obj.Id == 0 || fcalc)
            {
                var rawOrderTotal = (obj.GetRawOrderTotal(taxCountryCode) ?? 0) / exchangeRate;
                if (obj.Discount != null &&
                    0 == DiscountMgr.ListAppliable(db, rawOrderTotal, obj.OrderDetails.Count, obj.Discount.Id, customer.CustomerCGroups.Select(x => x.Group.Id).ToList()).Count)
                    return null;

                if (obj.Coupon != null && obj.Coupon.Validate(db, rawOrderTotal - (obj.GetDiscountTotal() ?? 0) / exchangeRate, customer.Email) == null) 
                    return null;
            }

            if (model.DiscountValue2 != 0 && (obj.Id == 0 || !admin))
            {
                //obj.Discount2 = DiscountMgr.GetDom(db, model.DiscountId2 ?? 0);
                obj.DiscountValue2 = model.DiscountValue2;
                //if (obj.Discount == null)
                //{
                //    obj.Discount = obj.Discount2;
                //    obj.DiscountValue = model.DiscountValue2;
                //    obj.DiscountName = obj.Discount2.GetName();
                //}
                //else obj.DiscountName = ((obj.DiscountName ?? "") + " + " + obj.Discount2.GetName()).TrimLength(200);
            }

            if (!obj.OfferToken.IsNullOrEmpty())
                obj.TotalDiscountOri = obj.TotalDiscountAdm;

            #endregion

            #region BackOrderDetails

            if (model.BackOrderDetails != null)
            {
                //if (obj.BackOrderDetails.Count != 0) return null;
                obj.BackOrderDetails.Clear();//old data

                foreach (var item in model.BackOrderDetails)
                {
                    var odid = item.OrderDetailId;
                    var ditm = (odid == 0 ? null : obj.BackOrderDetails.FirstOrDefault(x => x.OrderDetail.Id == odid));
                    if (ditm == null)
                        obj.BackOrderDetails.Add(ditm = new BackOrderDetail()
                        {
                            Order = obj,
                            OrderDetail = obj.OrderDetails.FirstOrDefault(x => x.Id == odid),
                        });

                    ditm.Quantity = item.Quantity ?? 0;
                }

                //obj.BackOrderDetails.RemoveAll(x => x.OrderDetail == null || model.BackOrderDetails.All(w => w.OrderDetailId != x.OrderDetail.Id));

                if (obj.BackOrderDetails.Count != 0)
                {
                    backorder = Rest(obj);
                    obj.BackOrderDetails.Clear();
                }
            }

            #endregion

            #region GiftCard

            //if (model.GiftCards != null)
            //{
            //    foreach (var gCard in model.GiftCards)
            //   {
            //    }
            //    obj.GiftCardTotal = model.GiftCards.Sum(x => x.Item2);
            //}

            #endregion

            #region Recurring

            if (model.Id == 0 || (model.OrderStatus.InList(OrderStatus.New, OrderStatus.Processing, OrderStatus.PaymentWaiting) && !obj.PaymentMethod.IsOnlinePayment))
            {
                var recurTypes = Enum.GetValues(typeof (RecurringType)).Cast<int>().ToList();
                var mLst = model.Recurring.ToList().Where(x => x.RecurEach > 0).ToList();
                var dLst = obj.Recurring.ToList();
                if (mLst.SafeCount() != 0)
                {
                    var orderJson = JsonConvert.SerializeObject(model, new JsonSerializerSettings()
                    {
                        NullValueHandling = NullValueHandling.Ignore,
                        DateFormatHandling = DateFormatHandling.IsoDateFormat,
                        DefaultValueHandling = DefaultValueHandling.Ignore,
                    });

                    deletedRecurrings = dLst.Where(x => mLst.All(z => x.Recurring != z.Recurring || x.RecurEach != z.RecurEach)).ToList();

                    foreach (var recur in mLst)
                    {
                        int rType = recur.Recurring ?? 0, rEach = recur.RecurEach ?? 0;
                        var dItm = dLst.FirstOrDefault(x => x.Recurring == rType && x.RecurEach == rEach);
                        if (dItm == null) dLst.Add(dItm = new RecurOrder()
                        {
                            Order = obj, CreatedDate = obj.CreatedDate,
                            Recurring = recur.Recurring ?? 0, RecurEach = recur.RecurEach ?? 0,
                            Status = (byte)(obj.PaymentMethod.IsOnlinePayment ? RecurOrderStatus.Pending : RecurOrderStatus.Active),
                            OrderJson = orderJson
                        });

                        dItm.FromDate = (DateTime)recur.FromDate;
                        dItm.ToDate = recur.ToDate;
                    }

                    obj.Recurring = null;
                    dLst.OrderByDescending(x => recurTypes.IndexOf(x.Recurring))
                        .Action(x => { x.NextRecurOrder = obj.Recurring; obj.Recurring = x; });
                }
                else { deletedRecurrings = dLst; obj.Recurring = null; }
            } 

            #endregion

            #region POS

            if (model.Type.InList(OrderType.Pos))
            {
                if (model.Credited > 0)
                {
                    obj.Credited = model.Credited;
                    obj.OrderStatus = (byte)OrderStatus.Credit;
                }

                obj.ShippingCost = obj.ShippingCost ?? 0;
                obj.ShippingVat = obj.ShippingVat ?? 0;
                obj.SplitPayment = model.SplitPayment;
                obj.SplitPayData = model.SplitPayData;
                obj.DiscountValue = model.DiscountValue;
                obj.ItemDiscountTotal = obj.OrderDetails.Sum(m => m.DiscountValue * m.Quantity) ?? 0;
                obj.TotalDiscountOri = obj.TotalDiscountAdm = (obj.DiscountValue ?? 0) + obj.ItemDiscountTotal;
                obj.VatTotalOri = obj.OrderDetails.Sum(m => m.Price * m.Quantity * m.Vat);
                obj.VatTotalAdmOri = obj.OrderDetails.Sum(m => (m.Price - m.DiscountValue / (1 + m.Vat)) * m.Quantity * m.Vat) ?? 0;
                obj.OrderTotalOri = obj.OrderTotalAdmOri;
                obj.OrderTotalAdmOri -= (obj.TotalDiscountOri + obj.PaymentCost + obj.ShippingCost - (decimal)(obj.RoundingUp ?? 0)) ?? 0; //FinalTotal
                obj.FinalTotal = obj.FinalTotalAdm;

                if (obj.PaymentMethod.GetId() == 20 || obj.SplitPayment != 0)
                    obj.GiftCardTotalPos = (obj.SplitPayment == 0 ? obj.FinalTotal : obj.GetSplitPayments(db).Where(m => m.Item1.Id == 20).Sum(m => m.Item2)) ?? 0;

                if (!noUpdate && obj.OrderDetails.Any(m => !m.SerialNumber.IsNullOrEmpty()))
                    ProcessSerialNumber(db, obj).Action(m =>
                    {
                        if (fActs != null) fActs.Add(sess => sess.Update(m));
                    });
            }

            #endregion

            return obj;
        }

        #endregion

        #region SaveorUpdate
        /// <summary>[Cpos]</summary>
        public static bool SaveorUpdate(IDB db, OrderModel model, bool admin = false, OrderType type = OrderType.Web,
            bool overridePayCost = false, bool overrideShipCost = false, bool editCustomer = false, bool noCart = false,
            bool useTransaction = true, bool editCompany = false)
        {
            return SaveorUpdate(db, model, admin, false, type, overridePayCost, overrideShipCost, editCustomer, noCart, useTransaction, editCompany, false);
        }

        internal static bool SaveorUpdate(IDB db, OrderModel model, bool admin, bool offer, OrderType type,
            bool overridePayCost, bool overrideShipCost, bool editCustomer, bool noCart,
            bool useTransaction, bool editCompany, bool editDetail)
        {
            Domain.Order backOrder;
            Customer customer;
            var fActs = new List<Action<ISession>>();
            List<RecurOrder> deletedRecurrings;
            var couponClearLst = new List<Coupon>();
            var discountClearLst = new List<Domain.Discount>();
            var noUpdate = !noCart && (model.OrderStatus.InList(OrderStatus.PaymentWaiting) || (type.InList(OrderType.Pos) && model.OrderStatus.InList(OrderStatus.Credit)))
                && type.InList(OrderType.Pos);
                                               
            var obj = SaveorUpdate(db, model, noUpdate, out customer, out backOrder, out deletedRecurrings,
                couponClearLst, discountClearLst, admin, false, overridePayCost, overrideShipCost, editCustomer, fActs, editCompany, editDetail);

            if (obj == null) return false;

            var settings = SettingsBiz.GetSettings();

            if (obj.Id == 0)
            {
                obj.Type = (byte)type;
                obj.MakeOrderDetailSourceId();
            }

            #region Cart

            var paymentMethod = (PaymentMethod)obj.PaymentMethod;
            if (!noCart && (obj.OrderStatus.InList(OrderStatus.PaymentWaiting) || (type.InList(OrderType.Pos) && obj.OrderStatus.InList(OrderStatus.Credit)))
                && (offer || paymentMethod.IsOnlinePayment() || type.InList(OrderType.Pos)))
            {
                var cart = BaseMgr<Cart, Cart>.GetDom(db, obj.CartId ?? 0) ?? new Cart() { CreatedDate = DateTime.Now, Data = "" };

                if (!offer && cart.Id != 0 && !cart.PaymentData.IsNullOrEmpty() && obj.PaymentMethod.GetProcessor() is VippsProcessor)
                {
                    db.DataService.Delete(cart);
                    cart = new Cart() { CreatedDate = DateTime.Now, Data = "" };
                }

                if (customer.Id == 0 && !customer.Email.IsNullOrEmpty())
                {    if (useTransaction)
                    {
                        if (!SaveOrUpdate(db, new[] { customer }.ToList()))
                            LogHelper.LogText(new Exception(string.Format("SaveorUpdate customer[email={0}] failed", customer.Email)));
                    }
                    else db.GetSession().SaveOrUpdate(customer);
                } 
                else customer = CustomerMgr.GetDom(db, customer.Id) ?? customer;

                obj.Customer = customer;
                cart.Customer = customer.Email.IsNullOrEmpty() ? null : customer;

                if (offer)
                {
                    cart.Token = cart.Token.IsNullOrEmpty() ? Guid.NewGuid().ToString("N") : cart.Token;
                    cart.ExpiredDate = model.OfferExpiredDate;
                    cart.HideDiscPerProd = model.OfferHideDiscPerProd;
                    model.Flags |= (int) OrderFlagType.Offered |
                                   (model.OfferHideDiscPerProd == true ? (int) OrderFlagType.OfferHideDiscPerProd : 0);
                    model.OfferToken = obj.OfferToken = cart.Token;
                    model.Raw = obj;
                }

                if (useTransaction)
                {
                    if (!SaveOrUpdate(db, new[] { cart }.ToList()))
                        LogHelper.LogText(new Exception(string.Format("SaveorUpdate cart[old id={0}] failed", obj.CartId))); 
                }
                else db.GetSession().SaveOrUpdate(cart);

                model.CartId = obj.CartId = cart.Id;
                model.Customer.Id = customer.Id;

                cart.Currency = model.Currency;
                cart.LangCode = model.LangCode;
                cart.Data = JsonConvert.SerializeObject(model, GetJsonSettings());

                try
                {
                    JsonConvert.DeserializeObject<OrderModel>(cart.Data, GetJsonSettings());
                }
                catch (Exception err)
                {
                    var req = HttpContext.Current == null ? null : HttpContext.Current.Request;
                    LogHelper.LogText(new Exception(string.Format("OrderMgr.SaveorUpdate.SerializeObject: Cart data verified failed. [CartId={0}, Url='{1}', Referer='{2}', Locale='{3}', Msg='{4}']", 
                        obj.CartId, req == null ? null : req.RawUrl, req == null ? null : req.UrlReferrer, Thread.CurrentThread.CurrentCulture.SafeProp(m => m.Name), err.Message)));
                }

                if (useTransaction)
                {
                    if (!SaveOrUpdate(db, new[] { cart }.ToList())) 
                        LogHelper.LogText(new Exception(string.Format("SaveorUpdate cart[new id={0}] failed", obj.CartId))); 
                }
                else db.GetSession().SaveOrUpdate(cart);

                return true;
            } 
           
            #endregion

            #region SaveOrUpdate

            var addNew = obj.Id == 0;
            RecurOrder recurring =  obj.Recurring;
            if (obj.Id == 0) obj.Recurring = null;
           
            if (SaveOrUpdate(db, useTransaction, (session, order) =>
            {
                session.SaveOrUpdate(customer);
                order.Customer = customer;

                if (backOrder != null) session.SaveOrUpdate(backOrder);

                if (deletedRecurrings != null)
                    deletedRecurrings.Action(x => { x.NextRecurOrder = null; })
                        .Action(x => session.Update(x));

                if (order.Id == 0)
                {
                    if (settings.Hidden.OrderDetailDataTag)
                        foreach (var item in order.OrderDetails.Where(m => !m.Data.IsNullOrEmpty()))
                        {
                            var dItm = item;
                            var dataExp = (IDictionary<string, object>) item.Data.ToExpando();
                            if (dataExp != null && dataExp.Count != 0)
                            {
                                dataExp.Where(m => !string.IsNullOrEmpty((m.Value ?? "").ToString()))
                                    .ForEach(m => dItm.OrderDetailTags.Add(new OrderDetailTag()
                                {
                                    Order = obj, OrderDetail = dItm, Customer = customer, Tag = (m.Value ?? "").ToString(), Key = m.Key
                                }));
                            }
                        }

                    ProcessCustomerProduct(order, customer, db, session);

                    CurrencyMgr.UpdateRef(session, order.Currency);
                }

                return true;

            }, (session, order) =>
            {
                #region check versiom
                var versionSetting = Claw.Modules.Eshop.Helper.VersionHelper.GetVersionSetting();
                if (versionSetting != null && versionSetting.PowerOffice)
                {
                    QueuePowerOffice(null, order, session);
                }
                #endregion

                fActs.Action(f => f(session));

                if (recurring != null)
                {
                    recurring.ToList().Where(x => x.Id == 0).Action(session.SaveOrUpdate);
                    order.Recurring = recurring;
                    session.SaveOrUpdate(order);
                }

                if (addNew && order.GiftCardTotal > 0 && !order.GiftCardData.IsNullOrEmpty())
                {
                    var dbSess = new DefaultDb(session);
                    var request = HttpContext.Current == null ? null : new HttpRequestWrapper(HttpContext.Current.Request);
                    var giftCardMgr = Singleton<IGiftCardMgr>.Default;
                    var giftCardTotal = order.GiftCardTotal;
                    var lstGc = JsonConvert.DeserializeObject<List<GiftCardOrder>>(order.GiftCardData)
                        .Select(m => new { Old = m, New = giftCardMgr.Get(m.Code, dbSess) })
                        .Where(m => m.New != null && m.New.Active && (m.New.ExpiredDate ?? DateTime.MaxValue).Date >= DateTime.Now.Date).ToList();

                    lstGc.Action(m =>
                    {    
                        var gVal = Math.Min(giftCardTotal, m.New.Balance);

                        if (giftCardMgr.Apply(dbSess, m.New.Id, gVal, order.Id, session, request))
                        {
                            giftCardTotal -= gVal;
                            m.Old.AmountUsed = gVal;
                        }
                        else throw new Exception(string.Format("Apply giftcard [code = {0}, amount = {1}] failed", m.New.Code, (double)gVal));
                    });

                    if (giftCardTotal > 0.0001m) throw new Exception(string.Format("Apply giftcards failed. Remaining amount = {0}", (double)giftCardTotal));

                    order.GiftCardData = JsonConvert.SerializeObject(lstGc.Select(m => m.Old));
                }

                if (addNew && obj.OrderStatus.InList(OrderStatus.New))
                {
                    if (obj.Coupon.SafeProp(m => m.OneTime))
                    {
                        obj.Coupon.Active = false;
                        session.Update(obj.Coupon);
                    }

                    obj.OrderDetails.Where(m => m.Coupon.SafeProp(x => x.OneTime)).Action(m =>
                    {
                        m.Coupon.Active = false;
                        session.Update(m.Coupon);
                    });
                }

                if (order.CartId != null)
                {
                    var cart = BaseMgr<Cart, Cart>.GetDom(db, obj.CartId ?? 0);
                    if (cart != null) session.Delete(cart);
                }

                if (model.CartIdOffered != null)
                {
                    var cart = BaseMgr<Cart, Cart>.GetDom(db, model.CartIdOffered ?? 0);
                    if (cart != null)
                    {
                        cart.RefOrderId = order.Id;
                        //session.Delete(cart);
                        session.Update(cart);
                    }
                }

                return MakeReservationId(session, order) && (backOrder == null || MakeReservationId(session, backOrder)) &&
                    (FuncSaveorUpdate == null || (FuncSaveorUpdate(model, order, session) && (backOrder == null || FuncSaveorUpdate(null, backOrder, session))));
            } , obj))
            {
                SeparatedDb sepDb = null;
                couponClearLst.Where(x => x != null).Distinct(x => x.Id).Action(x => x.ClearStatistics(sepDb = sepDb ?? new SeparatedDb()));
                discountClearLst.Where(x => x != null).Distinct(x => x.Id).Action(x => x.ClearStatistics(sepDb = sepDb ?? new SeparatedDb()));

                new OrderMgr().OnUpdate(obj, db, addNew, useTransaction);

                model.Id = obj.Id;
                model.ReservationId = obj.ReservationId;

                if (useTransaction) db.GetSession().Evict(obj);

                if (deletedRecurrings.SafeCount() != 0)
                    (sepDb ?? new SeparatedDb()).DataService.Delete(deletedRecurrings);

                return true;
            }
            else return false;

            #endregion
        }

        #endregion

        #region ProcessSerialNumber

        public static List<object> ProcessSerialNumber(IDB db, Domain.Order obj)
        {
            var updLst = new List<object>();
            var szSerialNum = StringTable.SerialNumbers;
            List<ProdQtyLog> logs = null;

            obj.OrderDetails.Where(m => !m.SerialNumber.IsNullOrEmpty()).ToList()
                .Action(m =>
                {
                    var lst = m.SerialNumber.Split(new[] { ',', ';' }).Select(z => z.Trim()).Where(z => !z.IsNullOrEmpty()).ToList();
                    if (m.Product.Type.InList(ProductType.Bundle))
                    {
                        m.OrderDetailBundles
                            .Where(x => 0 != (x.Product.Flags & (int) ProductFlags.SerialNumber))
                            .Action(x =>
                            {
                                x.Product.ProductSerials.RemoveAll(z => lst.Any(w => w.EqualsIgCase(z.SerialNumber)));
                                updLst.Add(x.Product);
                            });
                    }
                    else
                    {
                        m.Product.ProductSerials.RemoveAll(z => lst.Any(w => w.EqualsIgCase(z.SerialNumber)));
                        updLst.Add(m.Product);
                    }

                })
                .GroupBy(m => string.Format("{0}:{1}", m.Product.Id, m.Variant.GetId(0))).ToList()
                .Action(m =>
                {
                    var lst = m.ToList();
                    var isBundle = lst[0].Product.Type.InList(ProductType.Bundle);
                    logs = /*logs ?? */ProdQtyLogMgr.GetAll(db, obj.Id, isBundle ? ProdQtyLogReason.Bundled : ProdQtyLogReason.Ordered);
                    if (isBundle)
                    {
                        lst[0].OrderDetailBundles.Where(w => !w.SerialNumber.IsNullOrEmpty()).Action(w =>
                        {
                            var tmp = logs.FirstOrDefault(z => w.Product.Id == z.Product.Id && w.Variant.GetId(0) == z.Variant.GetId(0));
                            if (tmp != null)
                            {
                                tmp.Note = string.Format("{0}{1}{2} {3}", tmp.Note, tmp.Note.IsNullOrEmpty() ? "" : ", ", (tmp.Note ?? "").IndexOf(szSerialNum) != -1 ? "," : (szSerialNum + ":"), w.SerialNumber);
                                tmp.Note = tmp.Note.TrimLength(4000);
                                updLst.Add(tmp);
                            }
                        });
                    }
                    else
                    {
                        var tmp = logs.FirstOrDefault(z => m.Key == string.Format("{0}:{1}", z.Product.Id, z.Variant.GetId(0)));
                        if (tmp != null)
                        {
                            tmp.Note = string.Format("{0}{1}{2} {3}", tmp.Note, tmp.Note.IsNullOrEmpty() ? "" : ", ", (tmp.Note ?? "").IndexOf(szSerialNum) != -1 ? "," : (szSerialNum + ":"), m.Select(z => z.SerialNumber).ToString(", "));
                            tmp.Note = tmp.Note.TrimLength(4000);
                            updLst.Add(tmp);
                        }
                    }
                });

            return updLst;
        } 
        
        #endregion

        #region ProcessCustomerProduct

        public static void ProcessCustomerProduct(Domain.Order order, Customer customer, IDB db, ISession session)
        {
            var settings = SettingsBiz.GetSettings();

            if (settings.Hidden.CustomerProduct)
            {
                if (settings.Hidden.CustCompany && order.Customer.Company != null)
                    order.Company = order.Customer.Company;

                var prodIds = order.OrderDetails.Select(m => m.Product.Id).Distinct().ToString(",");
                var lst = GetList<CustomerProduct>(db, string.Format(@"
                        from {0} as custP where custP.Customer.Id = {1} and custP.Product.Id in ({2}) and custP.Company is null", typeof(CustomerProduct).FullName, order.Customer.Id, prodIds));

                var lstCo = !settings.Hidden.CustCompany || order.Customer.Company == null || order.Customer.Company.Owner == null
                    ? null
                    : GetList<CustomerProduct>(db, string.Format(@"from {0} as custP where custP.Company.Id = {1} and custP.Product.Id in ({2})", typeof(CustomerProduct).FullName, order.Customer.Company.Id, prodIds));

                foreach (var item in order.OrderDetails)
                {
                    var mItm = item;
                    var dataExp = (IDictionary<string, object>)item.Data.ToExpando();
                    var dItm = lst.FirstOrDefault(m => m.Product.Id == mItm.Product.Id && m.Variant.GetId(0) == mItm.Variant.GetId(0) && m.Data.EqualsIgCase(mItm.Data));
                    var dItmCo = lstCo == null ? null : lstCo.FirstOrDefault(m => m.Product.Id == mItm.Product.Id && m.Variant.GetId(0) == mItm.Variant.GetId(0) && m.Data.EqualsIgCase(mItm.Data));

                    if (dItm == null)
                        order.Customer.CustomerProducts.Add(dItm = new CustomerProduct()
                        {
                            Customer = customer,
                            Product = mItm.Product,
                            Variant = mItm.Variant,
                            Data = mItm.Data,
                            Active = true,
                            CreatedDate = order.CreatedDate,
                            Tags = dataExp == null ? null : dataExp.Select(m => (m.Value ?? "").ToString()).FirstOrDefault(m => !m.IsNullOrEmpty()),
                            QtyReorder = mItm.Quantity
                        });

                    if (dItmCo == null && lstCo != null)
                        order.Customer.CustomerProducts.Add(dItmCo = new CustomerProduct()
                        {
                            Customer = customer.Company.Owner,
                            Company = customer.Company,
                            Product = mItm.Product,
                            Variant = mItm.Variant,
                            Data = mItm.Data,
                            Active = true,
                            CreatedDate = order.CreatedDate,
                            Tags = dItm.Tags,
                            QtyReorder = mItm.Quantity
                        });

                    dItm.Orders = (dItm.Orders ?? 0) + 1;
                    dItm.Quantity += mItm.Quantity;
                    dItm.ModifiedDate = order.CreatedDate;

                    if (dItmCo != null)
                    {
                        dItmCo.Orders = (dItmCo.Orders ?? 0) + 1;
                        dItmCo.Quantity += mItm.Quantity;
                        dItmCo.ModifiedDate = order.CreatedDate;
                        session.SaveOrUpdate(dItmCo);
                    }

                    session.SaveOrUpdate(dItm);
                }
            }
        }

        #endregion

        #region CompletePayment

        public static bool CompletePayment(IDB db, Domain.Order obj)
        {
            if (obj != null && obj.OrderStatus.InList(OrderStatus.New) && obj.IsRecurOneTime())
            {
                obj.Flags &= ~(int)OrderFlagType.RenewPayment;
                return true;
            }

            if (obj == null || !obj.OrderStatus.InList(OrderStatus.PaymentWaiting)) return false;

            if (obj.IsRecurOneTime()) obj.Flags &= ~(int) OrderFlagType.RenewPayment;

            var retCode = true;
            bool preOrder = false;
            var settings = SettingsBiz.GetSettings();
            var fActs = new List<Action>();
            foreach (var itm in obj.OrderDetails)
            {
                if(!preOrder)
                {
                    preOrder = itm.Variant != null ? itm.Variant.BackDate.HasValue : itm.Product.BackDate.HasValue;
                }
                if (Product.DAL.ProductMgr.UpdateQuantity(db, itm.Product, itm.Variant, -1 * itm.QuantityOri, false, false, true, false, 
                    new ProdQtyLogModel() { Reason = (int)ProdQtyLogReason.Ordered, RefId = obj.Id },
                    bundleData: (itm.OrderDetailBundles.SafeCount() == 0 ? null : itm.OrderDetailBundles.Select(m => new System.Tuple<ProductBase, VariantBase, int>(m.Product, m.Variant, m.Quantity)).ToList())))
                {
                    itm.Product.OrderDetailNum = null;
                    itm.Product.TotalSales = null;

                    itm.StockQty = itm.GetStockQty(itm.QuantityOri);

                    var qtyDelta = itm.QuantityOri;
                    if (itm.Product.Type.InList(ProductType.Bundle))
                        itm.OrderDetailBundles.Action(m => m.StockQty = m.GetStockQty(m.Quantity * qtyDelta));
                }
                else retCode = false;

                if (itm.Product.Id == settings.Hidden.GiftCardProductId && !itm.Data.IsNullOrEmpty())
                {
                    var itm2 = itm;
                    var cEmail = obj.Customer.Email;
                    var giftData = JsonConvert.DeserializeObject<GiftCardOrderItem>(itm.Data);
                    if (itm2.PriceInc > 0 && !giftData.Email.IsNullOrEmpty())
                    {
                        for (var i = 0; i < itm2.Quantity; i++)
                            fActs.Add(() =>
                            {
                                Singleton<IGiftCardMgr>.Default.Create(db, itm2.PriceInc ?? 0, giftData.Name,
                                    cEmail, obj.DeliverMobile, DateTime.Now.AddYears(1), null, itm2, db.GetSession());

                                itm2.GC2reditNote = false;
                                obj.GiftCardSales += itm2.GiftCards.Cast<IGiftCard>().Sum(m => m.Amount);
                            });
                    }
                }
            }

            obj.OrderStatus = /*preOrder ? (byte)OrderStatus.PayBeforeDelivery : */(byte)OrderStatus.New;

            if (obj.Coupon != null && obj.Coupon.OneTime) obj.Coupon.Active = false;

            obj.OrderDetails
                .Where(m => m.Coupon.SafeProp(x => x.OneTime))
                .Action(m => m.Coupon.Active = false);

            if (obj.Recurring != null && obj.Recurring.Order.GetId() == obj.Id)
                obj.Recurring.ToList().Action(x =>
                {
                    x.Status = (byte)RecurOrderStatus.Active;
                    x.PaymentData = obj.GetPaymentData().SafeProp(m => m.RecurringInfo);
                });

            if (retCode) fActs.Action(f => f());

            #region check versiom
            var versionSetting = Claw.Modules.Eshop.Helper.VersionHelper.GetVersionSetting();
            if (retCode && versionSetting != null && versionSetting.PowerOffice) QueuePowerOffice(db, obj);
            #endregion

            return retCode;
        }

        #endregion

        #region MakeReservationId
        public static bool MakeReservationId(ISession session, Domain.Order order)
        {
            if (order.ReservationId.IsNullOrEmpty())
            {
                var maxLoop = 1000;
                while (--maxLoop > 0)
                {
                    order.ReservationId = order.GetReservationId();
                    if (!Exists<Domain.Order>(new DataService(session), m => m.Id, order.Id, null,
                        new[] { new KeyValuePair() { Key = "ReservationId", Value = order.ReservationId } }))
                    {
                        session.SaveOrUpdate(order);
                        return true;
                    }
                }
                return false;
            }
            else return true;
        }
        #endregion

        #region GetCouponCount

        public static int GetCouponCount<TRef>(IDB db, int couponId)
        {
            var hSql = string.Format(@"
            select count(*) from {0} as obj 
            where obj.Coupon.Id = {1} ", typeof(TRef).FullName, couponId);

            var ret = GetList<object>(db, hSql);

            return ret.Count == 0 ? 0 : (int)Convert.ChangeType(ret[0], typeof(int));
        }

        public static int GetCouponCount(IDB db, int customerId, int couponId)
        {
            var hSqlOrder = string.Format(@"
            select count(*) from {0} as order 
            where order.Coupon.Id = {1} and order.Customer.Id = {2} ", typeof(Domain.Order).FullName, couponId, customerId);

            var hSqlODetail = string.Format(@"
            select count(distinct ord.Id) from {0} as ord join ord.OrderDetails as odetail
            where odetail.Coupon.Id = {1} and ord.Customer.Id = {2} ", typeof(Domain.Order).FullName, couponId, customerId);

            var retorder = GetList<object>(db, hSqlOrder);
            var retodetail = GetList<object>(db, hSqlODetail);
            var ret = 0;

            ret += retorder.Count == 0 ? 0 : (int)Convert.ChangeType(retorder[0], typeof(int));
            ret += retodetail.Count == 0 ? 0 : (int)Convert.ChangeType(retodetail[0], typeof(int));

            return ret;
        }

        #endregion

        #region GetDiscountCount

        public static int GetDiscountCount<TRef>(IDB db, int discountId)
        {
            var hSql = string.Format(@"
            select count(*) from {0} as obj 
            where obj.Discount.Id = {1} ", typeof(TRef).FullName, discountId);

            var ret = GetList<object>(db, hSql);

            return ret.Count == 0 ? 0 : (int)Convert.ChangeType(ret[0], typeof(int));
        }

        #endregion
                                                                                                           
        #region GetPaymentMethodCount

        public static int GetPaymentMethodCount(IDB db, int paymentMethodId)
        {
            var hSql = string.Format(@"
            select count(*) from {0} as obj 
            where obj.PaymentMethod.Id = {1} ", typeof(Domain.Order).FullName, paymentMethodId);

            var ret = GetList<object>(db, hSql);

            return ret.Count == 0 ? 0 : (int)Convert.ChangeType(ret[0], typeof(int));
        }

        #endregion

        #region GetShippingCount

        public static int GetShippingCount(IDB db, int shippingId)
        {
            var hSql = string.Format(@"
            select count(*) from {0} as obj 
            where obj.Shipping.Id = {1} ", typeof(Domain.Order).FullName, shippingId);

            var ret = GetList<object>(db, hSql);

            return ret.Count == 0 ? 0 : (int)Convert.ChangeType(ret[0], typeof(int));
        }

        #endregion

        #region GetProductTotalSales

        public static List<int[]> GetProductTotalSales(IDB db, params int[] productIds)
        {
            if (productIds.SafeCount() == 0) return new List<int[]>();

            var hSql = string.Format(@"
            select obj.Product.Id, count(*) from {0} as obj 
            where obj.Product.Id in ({1})
            group by obj.Product.Id ", typeof(OrderDetail).FullName, productIds.ToString(","));

            var ret = GetList<object[]>(db, hSql);

            return ret.Select(x => x.Select(k => (int)Convert.ChangeType(k, typeof(int))).ToArray()).ToList();
        }

        #endregion

        #region GetCustomerTotalSales

        public static System.Tuple<int?, decimal?> GetCustomerTotalSales(IDB db, int customerId)
        {
            var hasCurrency = typeof(Domain.Order).GetProperty("Currency") != null;

            var hSql = string.Format(@"
            select  sum(case ord.OrderStatus when {7} then 0 else 1 end), 
                    sum(case ord.OrderStatus when {3} then ord.OrderTotal else ord.OrderTotal - ord.TotalCoupon - ord.TotalDiscount + ord.ShippingCost + ord.PaymentCost end {2})     
            from {0} as ord 
            where ord.Customer.Id = {1}
            and ord.OrderStatus in ({3},{4},{5},{6}, {7}, {8}) ", typeof(Domain.Order).FullName, customerId, hasCurrency ? "/isnull(ord.ExchangeRate,1)" : "",
                 OrderStatus.Credit.Val(), OrderStatus.New.Val(), OrderStatus.Sent.Val(), OrderStatus.PayBeforeDelivery.Val(), OrderStatus.RestOrder.Val(), OrderStatus.Processing.Val());

            var ret = GetList<object[]>(db, hSql).FirstOrDefault();

            return ret == null ? new System.Tuple<int?, decimal?>(0, 0) : new System.Tuple<int?, decimal?>(ret[0].SafeChangeType<int?>() ?? 0, ret[1].SafeChangeType<decimal?>() ?? 0);
        }

        #endregion

        #region Clone

        public static Domain.Order Clone(IDB db, long id, bool odetailId = false, bool finalTotal = false, bool bakCopy = false)
        {
            return Clone(GetDom(db, id), odetailId, finalTotal, bakCopy);
        }

        public static Domain.Order Clone(IDB db, int id, bool odetailId = false, bool finalTotal = false, bool bakCopy = false)
        {
            return Clone(GetDom(db, id), odetailId, finalTotal, bakCopy);
        }

        public static Domain.Order Clone(Domain.Order src, bool odetailId = false, bool finalTotal = false, bool bakCopy = false)
        {
            if (src == null) return null;

            var obj = new Domain.Order()
            {
                CreatedDate = DateTime.Now,
                ModifiedDate = DateTime.Now,
                ReservationId = null,
                Invoice = null,
                Credited = null,
                DeliveredDate = null,
                OriginRest = null,
                PackerId = null,
                PackerName = null,
                PackingStart = null,
                PackingEnd = null,
            };

            #region Order

            //obj.TaxCode = src.TaxCode;
            //obj.TaxCompany = src.TaxCompany;
            //obj.TaxAddress = src.TaxAddress; 
            obj.OrgNumber = src.OrgNumber;
            obj.CompanyName = src.CompanyName;
            obj.BillingName = src.BillingName;
            obj.BillingPhone = src.BillingPhone;
            obj.BillingMobile = src.BillingMobile;
            obj.BillingAddress = src.BillingAddress;
            obj.DeliverName = src.DeliverName;
            obj.DeliverPhone = src.DeliverPhone;
            obj.DeliverMobile = src.DeliverMobile;
            obj.DeliverAddress = src.DeliverAddress; ;
            //obj.ShippingTrackingId = src.ShippingTrackingId;
            obj.TransactionId = src.TransactionId;
            obj.OrderStatus = src.OrderStatus;
            obj.Comments = src.Comments;
            obj.LangCode = src.LangCode;
            obj.Currency = src.Currency;
            obj.ExchangeRate = src.ExchangeRate;
            obj.UserIp = src.UserIp;
            obj.UserHost = src.UserHost;
            obj.UserAgent = src.UserAgent;
            obj.ExtraInfo1 = src.ExtraInfo1;
            obj.ExtraInfo2 = src.ExtraInfo2;
            obj.ExtraInfo3 = src.ExtraInfo3;
            obj.ExtraInfo4 = src.ExtraInfo4;

            obj.Flags = 0;

            obj.Customer = src.Customer;
            obj.DeliverPostnr = src.DeliverPostnr;
            obj.DeliverPosted = src.DeliverPosted;
            obj.DeliverCity = src.DeliverCity;
            obj.DeliverCountry = src.DeliverCountry;
            obj.BillingPostnr = src.BillingPostnr;
            obj.BillingPosted = src.BillingPosted;
            obj.BillingCity = src.BillingCity;
            obj.BillingCountry = src.BillingCountry;

            obj.VatEnabled = src.VatEnabled;
            obj.Type = src.Type;

            obj.Shipping = src.Shipping;
            obj.ShippingName = src.ShippingName;
            obj.ShippingVat = src.ShippingVat;
            obj.ShippingCost = src.ShippingCost;
            obj.ShippingCostAdm = src.ShippingCostAdm;

            obj.PaymentMethod = src.PaymentMethod;
            obj.PaymentName = src.PaymentName;
            obj.PaymentVat = src.PaymentVat;
            obj.PaymentCost = src.PaymentCost;
            obj.PaymentCostAdm = src.PaymentCostAdm;

            obj.Discount = src.Discount;
            obj.DiscountName = src.DiscountName;
            obj.DiscountValue = src.DiscountValue;
            obj.TotalDiscountOri = src.TotalDiscountOri;
            obj.TotalDiscountAdm = src.TotalDiscountAdm;
            obj.TotalDiscountRes = src.TotalDiscountRes;

            obj.Coupon = src.Coupon;
            obj.CouponCode = src.CouponCode;
            obj.CouponValue = src.CouponValue;
            obj.TotalCouponOri = src.TotalCouponOri;
            obj.TotalCouponAdm = src.TotalCouponAdm;
            obj.TotalCouponRes = src.TotalCouponRes;
            obj.GiftCardTotal = 0;
            obj.GiftCardData = null;
            obj.DepositTotalOri = src.DepositTotal;

            obj.OrderTotalEx = obj.OrderTotalEx;
            obj.OrderTotalOri = src.OrderTotalOri;
            obj.VatTotalOri = src.VatTotalOri;

            if (bakCopy)
            {
                obj.OrderTotalBak = src.OrderTotalBak;
                obj.VatTotalBak = src.VatTotalBak;
                obj.FinalTotalBak = src.FinalTotalBak;
            }

            obj.OrderTotalAdmOri = src.OrderTotalAdmOri;
            obj.VatTotalAdmOri = src.VatTotalAdmOri;

            obj.OrderTotalRes = src.OrderTotalRes;
            obj.VatTotalRes = src.VatTotalRes;
            obj.RoundingUp = src.RoundingUp;

            //obj.GiftCardTotal = src.GiftCardTotal; 
            obj.Recurring = src.Recurring;
            obj.ShippingService = src.ShippingService;
            obj.ShippingPickupPoint = src.ShippingPickupPoint;
            obj.PackingService = src.PackingService;
            obj.CartId = src.CartId;

            obj.OfferHideDiscPerProd = src.OfferHideDiscPerProd;
            obj.OfferExpiredDate = src.OfferExpiredDate;
            obj.OfferToken = src.OfferToken;
            obj.OfferMsg = src.OfferMsg;
            obj.OfferSmsMsg = src.OfferSmsMsg;
            obj.OfferSendSms = src.OfferSendSms;
            obj.OfferEmailSent = src.OfferEmailSent;
            obj.CustomDiscountRate = src.CustomDiscountRate;
            obj.OfferProfitPerProd = src.OfferProfitPerProd;

            if (finalTotal)
            {
                obj.FinalTotal = src.FinalTotal;
                obj.FinalTotalAdm = src.FinalTotalAdm;
                obj.FinalTotalBak = src.FinalTotalBak;
            }

            #endregion

            #region OrderDetails

            foreach (var item in src.OrderDetails.OrderBy(m => m.Id))
            {
                #region Basic

                var ditm = new OrderDetail()
                {
                    Id = odetailId ? item.Id : 0,
                    Order = obj,
                    Manual = item.Manual,
                    Product = item.Product,
                    Variant = item.Variant,
                    Price = item.Price,
                    PriceAdm = item.PriceAdm,
                    PriceInc = item.PriceInc,
                    PriceOrigin = item.PriceOrigin,
                    CostPrice = item.CostPrice,
                    QuantityOri = item.QuantityOri,
                    RestQty = item.RestQty,
                    RecurQty = item.RecurQty,
                    RecurType = item.RecurType,
                    RecurEach = item.RecurEach,
                    SrcRecurType = item.SrcRecurType,
                    SrcRecurEach = item.SrcRecurEach,
                    SourceId = item.SourceId ?? item.Id,
                    Data = item.Data,
                    Vat = item.Vat,
                    Delivered = item.Delivered,
                    ProductName = item.ProductName,
                    ProductSKU = item.ProductSKU,
                    ProductShippingType = item.ProductShippingType,
                    ProductWeight = item.ProductWeight,
                    ShippingCostExtra = item.ShippingCostExtra,
                    DepositAmount = item.DepositAmount,
                    ProductWUnit = item.ProductWUnit,
                    CategoryId = item.CategoryId,

                    Discount = item.Discount,
                    DiscountName = item.DiscountName,
                    DiscountValue = item.DiscountValue,

                    Coupon = item.Coupon,
                    CouponCode = item.CouponCode,
                    CouponValue = item.CouponValue,

                    FinalPrice = item.FinalPrice,
                    PriceBak = bakCopy ? item.PriceBak : null,
                    QuantityOriBak = bakCopy ? item.QuantityOriBak : null,
                };

                obj.OrderDetails.Add(ditm);

                ditm.Product.OrderDetailNum = null;
                ditm.Product.TotalSales = null;

                #endregion

                ditm.OrderDetailBundles = item.OrderDetailBundles.Select(x => new OrderDetailBundle()
                {
                    OrderDetail = ditm,
                    Product = x.Product,
                    Variant = x.Variant,
                    Quantity = x.Quantity,
                    ProductName = x.ProductName,
                    ProductSKU = x.ProductSKU,
                }).ToList();

                ditm.OrderDetailGifts = item.OrderDetailGifts.Select(x => new OrderDetailGift()
                {
                    OrderDetail = ditm,
                    Product = x.Product,
                    Variant = x.Variant,
                    Quantity = x.Quantity,
                    ProductName = x.ProductName,
                    ProductSKU = x.ProductSKU,
                }).ToList();
            }

            #endregion

            return obj;
        }

        #endregion

        #region Credit

        /// <summary>Cpos</summary>
        public static Domain.Order Credit(IDB db, int id, List<CreditOrderDetailModel> creditOrderDetails,
            string comment, HttpRequestBase request, bool useTransaction = true,
            Func<Domain.Order, Invoice> fInvoice = null)
        {
            return Credit2(db, id, creditOrderDetails, comment, request, useTransaction, fInvoice);
        }
        
        /// <summary>Cpos</summary>
        public static Domain.Order Credit(IDB db, int id, List<CreditOrderDetailModel> creditOrderDetails,
          string comment, bool autoSave, HttpRequestBase request, bool useTransaction = true,
          Func<Domain.Order, Invoice> fInvoice = null)
        {
            return Credit2(db, id, creditOrderDetails, comment, request, useTransaction, fInvoice, autoSave: autoSave);
        }

        public static Domain.Order Credit2(IDB db, int id, List<CreditOrderDetailModel> creditOrderDetails,
            string comment, HttpRequestBase request, bool useTransaction = true, Func<Domain.Order, Invoice> fInvoice = null, 
            bool skipRefund = false, bool autoSave = true)
        {
            var obj = Clone(db, id, true);
            var originOrder = GetDom(db, id);

            if (obj == null || originOrder == null || creditOrderDetails.SafeCount() == 0) return null;

            var paymentcost = creditOrderDetails.FirstOrDefault(x => x.OrderDetailId == -1);
            var shippingcost = creditOrderDetails.FirstOrDefault(x => x.OrderDetailId == -2);
            var taxCountryCode = obj.GetTaxCountryCode();

            #region Order

            obj.Credited = id;
            obj.OrderStatus = (byte)OrderStatus.Credit;
            obj.Invoice = null;
            if (request != null)
            {
                obj.UserIp = request.UserHostAddress;
                obj.UserHost = request.UserHostName;
                obj.UserAgent = request.UserAgent;
            }
            else if (HttpContext.Current != null)
            {
                obj.UserIp = HttpContext.Current.Request.UserHostAddress;
                obj.UserHost = HttpContext.Current.Request.UserHostName;
                obj.UserAgent = HttpContext.Current.Request.UserAgent;
            }
            obj.Comments = comment;

            obj.VatEnabled = CountryMgr.IsVatEnable(db, taxCountryCode);

            obj.ShippingCostAdm = obj.ShippingCost = (shippingcost == null ? 0 : -1) * Math.Abs(obj.ShippingCost ?? 0);
            //obj.ShippingCostAdm = (shippingcost == null ? 0 : -1) * Math.Abs(obj.ShippingCostAdm ?? 0);
            obj.PaymentCostAdm = obj.PaymentCost = (paymentcost == null ? 0 : -1) * Math.Abs(obj.PaymentCost ?? 0);
            //obj.PaymentCostAdm = (paymentcost == null ? 0 : -1) * Math.Abs(obj.PaymentCostAdm ?? 0);

            if (obj.PaymentCost < 0 && -1*obj.PaymentCost == originOrder.GetGiftCardTotal(-1))
            {
                obj.GiftCardTotal += obj.PaymentCost ?? 0;  //obj.PaymentCost = 0;
            }

            if (obj.ShippingCost < 0 && -1 * obj.ShippingCost == originOrder.GetGiftCardTotal(-2))
            {
                obj.GiftCardTotal += obj.ShippingCost ?? 0; //obj.ShippingCost = 0;
            }

            obj.TotalCouponOri = 0 * Math.Abs(obj.TotalCouponOri);
            obj.TotalCouponAdm = 0 * Math.Abs(obj.TotalCouponAdm);
            obj.TotalCouponRes = 0;
            obj.TotalDiscountOri = 0 * Math.Abs(obj.TotalDiscountOri);
            obj.TotalDiscountAdm = 0 * Math.Abs(obj.TotalDiscountAdm);
            obj.TotalDiscountRes = 0;
            obj.RoundingUp = 0;
            obj.DepositTotalOri = 0;

            obj.AuthorizationId = null;
            obj.TransactionId = null;
            obj.PaymentData = null;

            if (skipRefund) obj.Flags |= (int)OrderFlagType.SkippedRefundCredit;

            #endregion

            var odQtyLogLst = new List<OrderDetail>();
            foreach (var itm in obj.OrderDetails.Join(creditOrderDetails,
                o => o.Id, m => m.OrderDetailId, (o, m) => new { O = o, M = m }))
            {
                if (itm.O.Quantity != 0) 
                    obj.GiftCardTotal -= originOrder.GetGiftCardTotal(itm.M.OrderDetailId ?? 0) * Math.Abs(itm.M.Quantity ?? 0) / itm.O.Quantity;

                itm.O.Id = 0;
                itm.O.RestQty = 0;
                itm.O.QuantityOri = -1 * Math.Abs(itm.M.Quantity ?? 0);
                itm.O.PriceAdm = itm.O.Price;
                itm.O.Price = itm.M.Price;
                itm.O.PriceInc = itm.M.PriceInc;
                itm.O.DiscountValue = itm.O.CouponValue = 0;
                itm.O.DepositAmount = null;
                itm.O.Discount = null;
                itm.O.Coupon = null;
                itm.O.PriceOrigin = null;

                if(itm.M.Add2Store ?? true)
                {
                    odQtyLogLst.Add(itm.O);
                    Product.DAL.ProductMgr.UpdateQuantity(db, itm.O.Product, itm.O.Variant, Math.Abs(itm.O.QuantityOri),
                        false, false, true, false, new ProdQtyLogModel() { Reason = (int)ProdQtyLogReason.Credited, RefIdFunc = () => obj.Id },
                        bundleData: (itm.O.OrderDetailBundles.SafeCount() == 0 ? null : itm.O.OrderDetailBundles.Select(m => new System.Tuple<ProductBase, VariantBase, int>(m.Product, m.Variant, m.Quantity)).ToList()));
                }
            }

            obj.OrderDetails.RemoveAll(x => x.Quantity >= 0 || x.Id != 0);

            obj.OrderTotalEx = obj.OrderDetails.Sum(x => ((x.PriceAdm ?? 0) - x.Price) * (1 + x.Vat) * x.QuantityOri);

            obj.OrderTotalRes = 0;
            obj.OrderTotalOri = -1 * Math.Abs(obj.GetRawOrderTotal(taxCountryCode) ?? 0);
            obj.VatTotalRes = 0;
            obj.VatTotalOri = -1 * Math.Abs(obj.GetVatTotal() ?? 0);

            obj.OrderTotalAdmOri = obj.OrderTotalOri;
            obj.VatTotalAdmOri = obj.VatTotalOri;

            if (obj.VatEnabled)
            {
                var shippingRatio = obj.ShippingVat / (1m + obj.ShippingVat);
                var paymentRatio = obj.PaymentVat / (1m + obj.PaymentVat);

                obj.OrderTotalOri += (obj.ShippingCost ?? 0) + (obj.PaymentCost ?? 0);
                obj.OrderTotalAdmOri += (obj.ShippingCostAdm ?? 0) + (obj.PaymentCostAdm ?? 0);
                obj.VatTotalOri += ((obj.ShippingCost * shippingRatio) ?? 0) + ((obj.PaymentCost * paymentRatio) ?? 0);
                obj.VatTotalAdmOri += ((obj.ShippingCostAdm * shippingRatio) ?? 0) + ((obj.PaymentCostAdm * paymentRatio) ?? 0);
            }
            else
            { // vat always zero
                obj.OrderTotalOri += (obj.ShippingCost ?? 0) / (1m + (obj.ShippingVat ?? 0)) +
                                  (obj.PaymentCost ?? 0) / (1m + (obj.PaymentVat ?? 0));
                obj.OrderTotalAdmOri += (obj.ShippingCostAdm ?? 0) / (1m + (obj.ShippingVat ?? 0)) +
                                     (obj.PaymentCostAdm ?? 0) / (1m + (obj.PaymentVat ?? 0));
            }

            obj.FinalTotalAdm = obj.OrderTotalAdm - obj.TotalDiscountAdm - obj.TotalCouponAdm - obj.GiftCardTotal + obj.DepositTotal;
            obj.FinalTotal = obj.FinalTotalAdm;

            if (!autoSave) return obj;

            if (SaveOrUpdate(db, useTransaction, (sess, order) =>
            {
                if (fInvoice == null)
                {
                    var invoice = new InvoiceModel() {OrderId = order.Id};
                    InvoiceMgr.SaveorUpdate(db, invoice, sess);
                    order.Invoice = InvoiceMgr.GetDom(sess, invoice.Id);
                }
                else order.Invoice = fInvoice(order);

                sess.SaveOrUpdate(order);

                order.Customer.OrderNum = null; order.Customer.StatsInfo = null;

                sess.SaveOrUpdate(order.Customer);

                var flags = SettingsBiz.GetSettings().Hidden.SendEmail4ReturnedOrder ? (int) OrderNoteFlags.SentEmail : 0;

                OrderNoteMgr.SaveorUpdate(db, new OrderNoteModel()
                {
                    Subject = string.Format(Order.StringTable.OrderCreditLogTempl, originOrder.Id, originOrder.Invoice.GetId(0), CurrentUser.Username),
                    Body = comment,
                    Private = false, 
                    Flags = flags,
                    OrderStatus = order.OrderStatus,
                    OrderId = order.Id
                }, sess, request);

                OrderNoteMgr.SaveorUpdate(db, new OrderNoteModel()
                {
                    Subject = string.Format(Order.StringTable.OrderCreditOriginLogTempl, order.Id, CurrentUser.Username),
                    Body = comment,
                    Private = false,
                    Flags = flags,
                    OrderStatus = originOrder.OrderStatus,
                    OrderId = originOrder.Id
                }, sess, request);

                foreach (var itm in odQtyLogLst)
                {
                    //ProdQtyLogMgr.SaveorUpdate(db, new ProdQtyLogModel() { Reason = (int)ProdQtyLogReason.Credited, RefId = obj.Id, ProductId = itm.Product.Id, VariantId = itm.Variant != null ? (int?)itm.Variant.Id : null, Quantity = Math.Abs(itm.QuantityOri) }, false);
                }

                return MakeReservationId(sess, order) && (FuncSaveorUpdate == null || FuncSaveorUpdate(null, order, sess));
            }, obj))
            {
                new OrderMgr().OnUpdate(obj, db, true, useTransaction);

                if (useTransaction) db.GetSession().Evict(originOrder);

                return obj;
            }

            return null;
        }

        #endregion

        #region Rest

        public static Domain.Order Rest(Domain.Order origin)
        {
            var obj = Clone(origin);

            #region Order

            obj.ShippingCost = 0;
            obj.PaymentCost = 0;
            obj.ShippingCostAdm = 0;
            obj.PaymentCostAdm = 0;
            //obj.Coupon = null; obj.CouponCode = null; need this for ratioDisc
            obj.CouponValue = 0;
            //obj.Discount = null; obj.DiscountName = null;
            obj.DiscountValue = 0;
            obj.TotalCouponOri = 0;
            obj.TotalDiscountOri = 0;
            obj.TotalCouponRes = 0;
            obj.TotalDiscountRes = 0;
            obj.OrderTotalRes = 0;
            obj.VatTotalRes = 0;
            obj.TotalCouponAdm = 0;
            obj.TotalDiscountAdm = 0;
            obj.OrderStatus = (byte)OrderStatus.RestOrder;

            obj.OriginRest = origin;
            obj.ExpectedDelivery = origin.ExpectedDelivery;

            #endregion

            var lstAct = new List<Action>();
            obj.OrderDetails.Action(x => x.QuantityOri = x.RestQty = 0);

            #region OrderDetails

            foreach (var item in origin.BackOrderDetails)
            {
                var prodId = item.OrderDetail.Product.Id;
                var variId = item.OrderDetail.Variant.GetId(0); 
                var recurType = item.OrderDetail.RecurType ?? 0;
                var recurEach = item.OrderDetail.RecurEach ?? 0;
                var odData = item.OrderDetail.Data;
                var backOd = obj.OrderDetails
                    .FirstOrDefault(x => x.Product.Id == prodId && x.Variant.GetId(0) == variId && (x.RecurType ?? 0) == recurType && (x.RecurEach ?? 0) == recurEach && x.Data.EqualsIgCase(odData));

                if (backOd == null) continue;

                backOd.Delivered = false;
                backOd.QuantityOri = item.Quantity;
                //backOd.Coupon = null;
                //backOd.CouponCode = null;
                //backOd.CouponValue = 0;
                //backOd.Discount = null;
                //backOd.DiscountName = null;
                //backOd.DiscountValue = 0;

                var tmpItm = item;
                lstAct.Add(() => tmpItm.OrderDetail.RestQty += backOd.Quantity);
            }

            #endregion

            obj.OrderDetails.RemoveAll(x => x.Quantity == 0 || origin.BackOrderDetails
                .All(w => w.OrderDetail.Product.Id != x.Product.Id ||
                          w.OrderDetail.Variant.GetId(0) != x.Variant.GetId(0)));

            var taxCountryCode = obj.GetTaxCountryCode();
            var orderTotalOri = obj.GetRawOrderTotal(taxCountryCode, null, false) ?? 0;

            obj.OrderTotalAdmOri = obj.GetRawOrderTotal(taxCountryCode) ?? 0;
            obj.VatTotalAdmOri = obj.GetVatTotal() ?? 0;
            obj.DepositTotalOri = obj.OrderDetails.Sum(m => m.Quantity*(m.DepositAmount ?? 0));

            obj.OrderTotalOri = obj.OrderTotalAdmOri;
            obj.VatTotalOri = obj.VatTotalAdmOri;

            if (origin.OrderTotalOri != 0)
            {
                if (origin.Discount == null) obj.TotalDiscountOri = obj.OrderDetails.Where(m => !m.Manual).Sum(m => m.DiscountValue) ?? 0;
                else
                {
                    obj.TotalDiscountOri = (orderTotalOri * origin.TotalDiscountOri) / origin.OrderTotalOri;
                    if (obj.TotalDiscountOri == 0) obj.Discount = null;
                }

                if (origin.Coupon == null) obj.TotalCouponOri = obj.OrderDetails.Where(m => !m.Manual).Sum(m => m.CouponValue * m.Quantity) ?? 0;
                else
                {
                    obj.TotalCouponOri = (orderTotalOri * origin.TotalCouponOri) / origin.OrderTotalOri;
                    if (obj.TotalCouponOri == 0) obj.Coupon = null;
                }

                if (Math.Abs(obj.TotalDiscountOri - origin.TotalDiscount) < 0.1m)
                    obj.TotalDiscountOri = origin.TotalDiscount;
                if (Math.Abs(obj.TotalCouponOri - origin.TotalCoupon) < 0.1m)
                    obj.TotalCouponOri = origin.TotalCoupon;
            }

            if (Math.Abs(obj.OrderTotalOri - origin.OrderTotal) < 0.1m)
                obj.OrderTotalOri = origin.OrderTotal;

            if (Math.Abs(obj.VatTotalOri - origin.VatTotal) < 0.1m)
                obj.VatTotalOri = origin.VatTotal;

            obj.GiftCardTotal = obj.GetGiftCardTotal();
            lstAct.Action(m => m());

            origin.TotalDiscountRes += obj.TotalDiscountOri;
            origin.TotalCouponRes += obj.TotalCouponOri;
            origin.OrderTotalRes += obj.OrderTotalOri;
            origin.VatTotalRes += obj.VatTotalOri;
            origin.DepositTotalRes += obj.DepositTotalOri;
            origin.GiftCardTotal -= obj.GiftCardTotal;

            if (origin.FinalTotalBak != null)
            {
                var delta = obj.OrderTotalOri - obj.TotalDiscountOri - obj.TotalCouponOri - obj.GiftCardTotal + obj.DepositTotal;
                if (delta <= origin.FinalTotalBak || delta == 0) origin.FinalTotalBak -= delta;
                else
                {
                    obj.FinalTotalBak = origin.FinalTotalBak;
                    origin.FinalTotalBak = 0;
                }
            }

            return obj;
        }

        #endregion

        #region Recur

        public static Domain.Order Recur(RecurOrder recurOrder, IDB db, bool autoSave = false, bool skipZeroQty = true, bool liveShipCost = true)
        {
            if (recurOrder == null || recurOrder.Order == null) return null;

            var settings = SettingsBiz.GetSettings();
            var origin = recurOrder.Order;
            var obj = Clone(origin);

            origin.BackOrderDetails.Clear();
            obj.OrderDetails.RemoveAll(x => (x.RecurQty ?? 0) <= ((skipZeroQty || x.RecurType == null) ? 0 : -1) || (x.RecurType != null &&
                ((x.SrcRecurType ?? x.RecurType.Value) != (recurOrder.SrcRecurring ?? recurOrder.Recurring) ||
                (x.SrcRecurEach ?? (x.RecurEach ?? 0)) != (recurOrder.SrcRecurEach ?? recurOrder.RecurEach))));

            obj.OrderDetails.Action(x =>
            {
                x.QuantityOri = x.RecurQty ?? 0;
                //x.CouponValue = x.DiscountValue = 0m;
                //x.Coupon = null;
                //x.Discount = null;
                x.RestQty = 0;
                if (settings.Hidden.RecurringProductLatestPrice)
                {
                    decimal oldPriceInc = x.Price * (x.Vat + 1), oldPrice = x.Price, oldPriceOri = x.PriceOrigin ?? 0;
                    var not4Recur = ((Domain.Discount)(x.Variant != null ? (x.Variant.Discount ?? x.Product.Discount) : x.Product.Discount)).SafeProp(m => m.Not4Recurring);
                    var rd = x.Product.ProductRecurDiscs.FirstOrDefault(m => m.Type == x.RecurType && m.RecurEach == x.RecurEach);
                    x.Vat = x.Product.Vat.Value;
                    x.PriceOrigin = x.PriceAdm = x.Product.CombinedPrice(x.Variant) ?? 0;
                    x.Price = (x.Variant == null ? (not4Recur ? x.Product.Price : x.Product.PriceFE) : (not4Recur ? (x.Variant.Price ?? 0) : x.Variant.PriceFE)) - (rd == null ? 0 : (x.PriceOrigin ?? 0) * rd.Value);
                    if (oldPriceOri > 0 && oldPriceOri > oldPrice)
                    {
                        x.Price = Math.Min(x.Price, (x.PriceOrigin ?? 0) * (1 - (oldPriceOri - oldPrice) / oldPriceOri));
                    }
                    x.PriceInc = x.Product.IncVat(x.Price);
                    x.CostPrice = x.Product.CombinedCostPrice(x.Variant);
                    x.CouponValue = oldPriceInc == 0 ? 0 : x.CouponValue * Math.Min(1, (x.PriceInc ?? 0) / oldPriceInc);
                    x.DiscountValue = oldPriceInc == 0 ? 0 : x.DiscountValue * Math.Min(1, (x.PriceInc ?? 0) / oldPriceInc);
                    x.FinalPrice = x.GetFinalPrice() ?? 0;
                }
            });

            if (obj.OrderDetails.Count == 0) return null;

            #region Order

            if (obj.Shipping != null)
            {
                var isFreeShip = obj.DiscountValue < 0 || obj.CouponValue < 0;
                obj.ShippingVat = obj.VatEnabled ? obj.Shipping.Vat.Value : 0;
                obj.ShippingCostAdm = isFreeShip ? 0 : obj.ExchangeRate * obj.GetShippingFee(false, db);

                if (liveShipCost) obj.ShippingCost = isFreeShip ? 0 : obj.ExchangeRate*Singleton<IOrderMgr2>.Default.GetLiveShippingCost(obj.Shipping, obj);
                else obj.ShippingCost = obj.ShippingCostAdm;
            }
            else obj.ShippingCost = obj.ShippingCostAdm = 0;

            //obj.Discount = null;
            //obj.DiscountName = null;
            //obj.DiscountValue = 0;
            //obj.Coupon = null;
            //obj.CouponCode = null;
            //obj.CouponValue = 0;
            obj.ItemDiscountTotal = obj.OrderDetails.Sum(m => m.Quantity * (m.DiscountValue ?? 0));
            obj.DepositTotalOri = obj.OrderDetails.Sum(m => m.Quantity * (m.DepositAmount ?? 0));

            var subtotalDisc = obj.OrderDetails.Sum(m => m.QuantityOri*(m.PriceInc - m.DiscountValue) ?? 0);
            obj.TotalDiscountOri = (origin.TotalDiscountOri - origin.ItemDiscountTotal) * Math.Min(1, subtotalDisc / (origin.OrderTotalOri - origin.ItemDiscountTotal));
            //obj.TotalDiscountOri = obj.TotalDiscountOri.RoundEx(0);

            obj.TotalCouponOri = origin.Coupon == null
                ? obj.OrderDetails.Sum(m => m.QuantityOri * (m.CouponValue ?? 0))
                : (origin.TotalCouponOri * Math.Min(1, subtotalDisc / (origin.OrderTotalOri - origin.TotalDiscountOri)));
            //obj.TotalCouponOri = obj.TotalCouponOri.RoundEx(0);

            obj.TotalCouponRes = 0;
            obj.TotalDiscountRes = 0;
            obj.OrderTotalRes = 0;
            obj.VatTotalRes = 0;
            obj.TotalCouponAdm = obj.TotalCouponOri;
            obj.TotalDiscountAdm = obj.TotalDiscountOri;
            obj.OrderStatus = (byte)OrderStatus.PaymentWaiting;

            obj.Recurring = recurOrder; 
            obj.AuthorizationId = null;
            obj.TransactionId = null;
            obj.PaymentData = null;

            #endregion

            obj.OrderTotalAdmOri = obj.OrderTotalOri = obj.GetRawOrderTotal(obj.GetTaxCountryCode(), db) ?? 0;
            obj.VatTotalAdmOri = obj.VatTotalOri = obj.GetVatTotal() ?? 0;
            obj.DepositTotalOri = obj.OrderDetails.Sum(m => m.Quantity * (m.DepositAmount ?? 0));

            #region PaymentMethod

            var payment = (PaymentMethod)obj.PaymentMethod;
            if (payment != null && payment.PaymentSetting.SafeProp(m => m.Id).InList(GatewayID.KlarnaCheckout/*, GatewayID.KlarnaPayments*/))
            {
                var klarnaSettings = Mapper.Map<KlarnaCheckoutSetting, KlarnaCheckoutSettingModel>((KlarnaCheckoutSetting)payment.GetSetting());
                if (klarnaSettings != null && (klarnaSettings.RecurringOrderMode ?? 0).InList(RecurringOrderMode.Global) && klarnaSettings.RecurringDiscounts.SafeCount() != 0)
                {
                    var recurDisc = klarnaSettings.RecurringDiscounts.FirstOrDefault(x => x.Type.InList(recurOrder.Recurring));
                    if (recurDisc != null && recurDisc.Value > 0m && recurDisc.Value <= 1m)
                        obj.TotalDiscountOri += (obj.DiscountValue = recurDisc.Value * obj.OrderTotal) ?? 0;
                    //else obj.DiscountValue = 0;

                    if (obj.Discount == null)
                    {
                        obj.Discount = recurDisc == null ? null : (Domain.Discount) recurDisc.GetDiscount(db);
                        obj.DiscountName = obj.Discount == null ? null : obj.Discount.GetName(obj.LangCode);
                    }
                }
                else if (klarnaSettings != null && (klarnaSettings.RecurringOrderMode ?? 0).InList(RecurringOrderMode.SpecificProducts))
                {
                    //var recurDisc = obj.OrderDetails.Select(x => x.GetProdRecurDiscountRate()).FirstOrDefault(x => x.Item1 > 0);
                    //if (recurDisc != null)
                    //    obj.TotalDiscountOri = (obj.DiscountValue = obj.GetProdRecurDiscount()) ?? 0;
                    //else obj.DiscountValue = 0;

                    //obj.Discount = recurDisc == null || recurDisc.Item2.Discount == null ? null : (Domain.Discount)recurDisc.Item2.Discount;
                    //obj.DiscountName = obj.Discount == null ? null : obj.Discount.GetName(obj.LangCode);
                }
            }

            #endregion

            if (obj.PaymentMethod != null)
            {
                obj.PaymentVat = obj.VatEnabled ? obj.PaymentMethod.TaxRate.Value : 0;
                obj.PaymentCostAdm = obj.PaymentCost = obj.PaymentMethod.GetFee(db, obj.GetTaxCountryCode(), obj.OrderTotalOri - obj.TotalCoupon - obj.TotalDiscount - obj.GiftCardTotal + obj.DepositTotal) * obj.ExchangeRate;
            }
            else obj.PaymentCost = obj.PaymentCostAdm = 0;

            obj.FinalTotalAdm = obj.FinalTotal = obj.OrderTotal - obj.TotalDiscount - obj.TotalCoupon - obj.GiftCardTotal + obj.DepositTotal + obj.PaymentCost + obj.ShippingCost;
            
            if (!autoSave) return obj;

            if (SaveOrUpdate(db, (session, order) =>
            {
                order.OrderDetails.Action(x => x.QuantityOri = x.RecurQty ?? 0);
                order.Customer.OrderNum = null; 
                order.Customer.StatsInfo = null;
                session.Update(order.Customer);
                if (obj.Discount != null && (obj.Discount.TotalOrders ?? 0) == 0)
                {
                    obj.Discount.TotalOrders = null;
                    session.Update(obj.Discount);
                }
                return true;
            }, (session, order) =>
            {
                if (VersionHelper.GetVersionSetting(db).SafeProp(m => m.PowerOffice))
                    QueuePowerOffice(null, order, session);
                return MakeReservationId(session, order);
            }, obj))
            {
                new OrderMgr().OnUpdate(obj, db);
                return obj;
            }
            else return null;
        }

        #endregion

        #region Refresh

        public static OrderModel Refresh(IDB db, OrderModel model, bool overridePayCost = false, bool overrideShipCost = false)
        {
            if (model == null) return null;

            if (model.Id != 0)
            {
                long orderId = model.Id;

                if (orderId == 0 && model.CartId > 0 && model.OfferEmailSent == false)
                {
                    orderId = model.CartOrderId ?? 0;
                }

                var raw = Clone(db, orderId, true, true, true);
                var origin = GetDom(db, orderId);
                var vatEnable = CountryMgr.IsVatEnable(raw.GetTaxCountryCode());
 
                raw.Flags = origin.Flags;

                model.RoundPrice(db, m => m.Id == 0);

                if (model.OrderTotalBak.IsNull())
                {
                    model.OrderTotalBak = raw.OrderTotalBak ?? raw.OrderTotalOri;
                    model.VatTotalBak = raw.VatTotalBak ?? raw.VatTotalOri;
                    model.FinalTotalBak = raw.FinalTotalBak ?? raw.FinalTotal;
                }

                model.OrderTotal = model.OrderTotalBak - raw.OrderTotalRes;
                model.VatTotal = model.VatTotalBak - raw.VatTotalRes;

                foreach (var od in model.OrderDetails.Where(m => m.Id == 0 || m.Manual || m.AllowEdit == true || ((decimal?)m.PriceBak ?? (decimal?)m.Price) != (decimal?)m.Price || (m.QuantityBak ?? m.Quantity) != m.Quantity))
                {
                    var product = ProductMgr.GetDom(db, od.ProductId ?? 0);
                    var variant = VariantMgr.GetDom(db, od.VariantId ?? 0);

                    if (od.Id == 0) od.Price = product.ExcVat(od.PriceInc, od.ProductId == 1 ? od.Vat : null);

                    if (od.Id == 0 || od.Manual)
                    {
                        var delta = ((vatEnable ? od.PriceInc : od.Price)*od.Quantity) ?? 0;
                        model.OrderTotal += delta;
                        model.FinalTotal += delta;
                        raw.OrderTotalOri += delta;

                        model.VatTotal += (vatEnable ? od.Price : 0)*od.Vat*od.Quantity;

                        if (od.Id == 0)
                            raw.OrderDetails.Add(new OrderDetail()
                            {
                                Order = raw,
                                Product = product,
                                Variant = variant,
                                ProductName = od.ProductName,
                                Vat = od.Vat,
                                Tags = od.Tags,
                                Price = od.Price,
                                PriceInc = od.PriceInc,
                                PriceAdm = od.Price,
                                FinalPrice = od.PriceInc - (od.DiscountValue ?? 0) - (od.CouponValue ?? 0),
                                QuantityOri = od.Quantity ?? 0,
                                DiscountValue = od.DiscountValue ?? 0,
                                CouponValue = od.CouponValue ?? 0,
                                Manual = true,
                                Timestamp = od.Timestamp,
                            });
                        else
                        {
                            var mid = od.Id;
                            var ditm = raw.OrderDetails.FirstOrDefault(m => m.Id == mid);
                            
                            if (od.ProductId == 1) ditm.Vat = od.Vat;

                            ditm.ProductName = od.ProductName;
                            ditm.QuantityOri = od.Quantity ?? 0;
                            ditm.Price = product.ExcVat(od.PriceInc, od.ProductId == 1 ? od.Vat : null) ?? 0;
                            ditm.PriceInc = od.PriceInc;                          
                        }
                    }
                    else
                    {
                        var mid = od.Id;
                        var ditm = raw.OrderDetails.FirstOrDefault(m => m.Id == mid);

                        ditm.ProductName = od.ProductName;

                        ditm.PriceBak = ditm.PriceBak ?? ditm.Price;
                        ditm.QuantityOriBak = ditm.QuantityOriBak ?? ditm.QuantityOri;

                        ditm.QuantityOri = od.Quantity ?? 0;

                        od.Price = ditm.Price = product.ExcVat(od.PriceInc, od.ProductId == 1 ? od.Vat : null) ?? 0;
                        ditm.PriceInc = od.PriceInc;

                        var delta = (vatEnable
                            ? od.PriceInc * od.Quantity - product.IncVat(ditm.PriceBak ?? ditm.Price, od.ProductId == 1 ? od.Vat : null) * ditm.QuantityBak
                            : od.Price * od.Quantity - (ditm.PriceBak ?? ditm.Price) * ditm.QuantityBak) ?? 0;

                        model.OrderTotal += delta;
                        model.FinalTotal += delta;
                        raw.OrderTotalOri += delta;

                        model.VatTotal += (vatEnable ? (od.Price * od.Quantity - (ditm.PriceBak ?? ditm.Price) * ditm.QuantityBak) * (od.ProductId == 1 ? od.Vat.Value : product.Vat.Value) : 0) ?? 0;
                    }
                }

                raw.OriginRest = origin.OriginRest;
                model.Raw = raw;
                model.FinalTotal = model.FinalTotalBak + model.OrderTotal - model.OrderTotalBak + raw.OrderTotalRes;

                return model;
            }

            var bkStatus = model.OrderStatus;
            var currency = CurrencyMgr.GetOrDefault(model.Currency);

            model.ExchangeRate = currency.Rate;
            model.OrderStatus = (byte)OrderStatus.PaymentWaiting;

            if (!overrideShipCost) model.ShippingCost = model.GetShippingFee(db, true);

            #region Coupon

            if (model.Id == 0)
            {
                model.CouponId = null;
                model.CouponCode = null;
                model.CouponValue = null;

                model.OrderDetails.Action(m =>
                {
                    m.CouponId = null;
                    m.CouponCode = null;
                    m.CouponValue = null;
                });
            }
            
            #endregion

            model.RoundPrice(db);
            model.TryApplyDiscount(db);
            model.TryApplyCoupon(db);

            Customer customer;
            Domain.Order backOrder;
            List<RecurOrder> deletedRecurrings;
            var coupons = new List<Coupon>();
            var discounts = new List<Domain.Discount>();

            var dom = SaveorUpdate(db, model, out customer, out backOrder, out deletedRecurrings, coupons, discounts, false, true,
                overridePayCost, overrideShipCost, editDetail: true);

            if (dom != null && dom.Id == 0)
            {
                var setting = new OMoneyMc.Setting()
                {
                    Currency = currency,
                    Roundable = true,
                    SkipRate = true,
                };

                model.PaymentCost = new OMoneyMc(dom.PaymentCostAdm, setting).Value;
                model.ShippingCost = new OMoneyMc(dom.ShippingCostAdm, setting).Value;

                dom = SaveorUpdate(db, model, out customer, out backOrder, out deletedRecurrings, coupons, discounts, false, true, true, true, editDetail: true);

                dom.TotalCouponOri = dom.TotalCouponAdm;
                dom.TotalDiscountOri = dom.TotalDiscountAdm;

                dom.OrderTotalOri = dom.OrderTotalAdmOri;
                dom.VatTotalOri = dom.VatTotalAdmOri;
                dom.FinalTotal = dom.FinalTotalAdm; 
                
                dom.OrderStatus = bkStatus;
            }

            return Mapper.Map<Domain.Order, OrderModel>(dom);
        }

        #endregion

        #region AddEditDetails

        public static System.Tuple<bool, string> AddEditDetails(IDB db, OrderModel model, out List<string> addedItems, out List<System.Tuple<string, string, OrderDetail>> changedItems)
        {
            var settings = SettingsBiz.GetSettings(false, db);
            var obj = GetDom(db, model == null ? 0 : model.Id);

            addedItems = new List<string>();
            changedItems = new List<System.Tuple<string, string, OrderDetail>>();
                                                          
            if (obj == null || model == null || !obj.OrderStatus.InList(OrderStatus.New, OrderStatus.Processing)) return new System.Tuple<bool, string>(false, null);

            if (obj.Id != 0 && model.OrderDetails.Any(m => m.Id != 0 && m.AllowEdit == true) && !obj.CanUpdateOrder()) return new System.Tuple<bool, string>(false, null);

            var vatEnable = CountryMgr.IsVatEnable(obj.GetTaxCountryCode());

            model.RoundPrice(db, m => m.Id == 0);

            obj.OrderTotalOri = (obj.OrderTotalBak = obj.OrderTotalBak ?? obj.OrderTotalOri) ?? 0;
            obj.VatTotalOri = (obj.VatTotalBak = obj.VatTotalBak ?? obj.VatTotalOri) ?? 0;
            obj.FinalTotal = obj.FinalTotalBak = obj.FinalTotalBak ?? obj.FinalTotal;

            var errMsg = "";
            var qtyChanges = new List<System.Tuple<OrderDetail, int>>();
            var monFields = new[]
            {
              BaseMgr<OrderDetailModel, OrderDetail>.Expr(m => m.Price), BaseMgr<OrderDetailModel, OrderDetail>.Expr(m => m.Quantity), BaseMgr<OrderDetailModel, OrderDetail>.Expr(m => m.ProductName), BaseMgr<OrderDetailModel, OrderDetail>.Expr(m => m.Vat)
            }.ToFieldName();

            foreach (var od in model.OrderDetails.Where(m => (m.Id == 0 && m.Quantity != 0) || (m.Manual && m.Id != 0) || m.AllowEdit == true || ((decimal?)m.PriceBak ?? (decimal?)m.Price) != (decimal?)m.Price || (m.QuantityBak ?? m.Quantity) != m.Quantity))
            {
                var product = ProductMgr.GetDom(db, od.ProductId ?? 0);
                var variant = VariantMgr.GetDom(db, od.VariantId ?? 0);

                if (od.Id == 0) od.Price = product.ExcVat(od.PriceInc, od.ProductId == 1 ? od.Vat : null);

                if (od.Id == 0 || od.Manual)
                {
                    obj.OrderTotalOri += ((vatEnable ? od.PriceInc : od.Price) * od.Quantity) ?? 0;
                    obj.VatTotalOri += ((vatEnable ? od.Price : 0) * (od.ProductId == 1 && od.Vat >= 0 ? (decimal)od.Vat : product.Vat.Value) * od.Quantity) ?? 0;

                    if (od.Id == 0)
                    {
                        var ditm = new OrderDetail()
                        {
                            Order = obj,
                            Product = product,
                            Variant = variant,
                            ProductName = od.ProductName,
                            ProductSKU = od.ProductId == 1 ? null : (variant == null ? product.SKU : variant.SKU),
                            ProductWUnit = (byte)WeightUnit.Kg,
                            ProductWeight = product.WeightInKg(settings, variant),
                            ProductShippingType = (int)ShippingType.RequireShipping,
                            Vat = od.Vat,
                            Tags = od.Tags,
                            CostPrice = od.ProductId == 1 ? null : (variant == null ? product.CostPrice : variant.CostPrice),
                            Price = od.Price,
                            PriceInc = od.PriceInc,
                            PriceAdm = od.Price,
                            FinalPrice = od.PriceInc - (od.DiscountValue ?? 0) - (od.CouponValue ?? 0),
                            QuantityOri = od.Quantity ?? 0,
                            DiscountValue = od.DiscountValue ?? 0,
                            CouponValue = od.CouponValue ?? 0,
                            Manual = true,
                        }; 
                        
                        obj.OrderDetails.Add(ditm);

                        addedItems.Add(product.GetName(variant));
                        qtyChanges.Add(new System.Tuple<OrderDetail, int>(ditm, ditm.QuantityOri));
                    }
                    else
                    {
                        var mid = od.Id;
                        var ditm = obj.OrderDetails.FirstOrDefault(m => m.Id == mid);
                        var originVals = ditm.Extract(monFields);

                        if (od.ProductId == 1 && od.Vat >= 0) ditm.Vat = od.Vat;

                        qtyChanges.Add(new System.Tuple<OrderDetail, int>(ditm, (od.Quantity ?? 0) - ditm.QuantityOri));  

                        ditm.ProductName = od.ProductName;
                        ditm.QuantityOri = od.Quantity ?? 0;
                        ditm.Price = product.ExcVat(od.PriceInc, od.ProductId == 1 ? od.Vat : null) ?? 0;
                        ditm.PriceInc = od.PriceInc;

                        var finalVals = ditm.Extract(monFields);
                        var changes = originVals.GetChanges(finalVals);
                        if (changes.Count != 0)
                            changedItems.AddRange(changes.Select(m => new System.Tuple<string, string, OrderDetail>(m.Key, m.Value, ditm)));
                    }
                }
                else
                {
                    var mid = od.Id;
                    var ditm = obj.OrderDetails.FirstOrDefault(m => m.Id == mid);
                    var originVals = ditm.Extract(monFields);

                    ditm.ProductName = od.ProductName;

                    ditm.PriceBak = ditm.PriceBak ?? ditm.Price;
                    ditm.QuantityOriBak = ditm.QuantityOriBak ?? ditm.QuantityOri;

                    qtyChanges.Add(new System.Tuple<OrderDetail, int>(ditm, (od.Quantity ?? 0) - ditm.QuantityOri));  

                    ditm.QuantityOri = od.Quantity ?? 0;

                    ditm.Price = product.ExcVat(od.PriceInc, od.ProductId == 1 ? od.Vat : null) ?? 0;
                    ditm.PriceInc = od.PriceInc;

                    obj.OrderTotalOri += (vatEnable
                        ? od.PriceInc * od.Quantity - product.IncVat(ditm.PriceBak ?? ditm.Price, od.ProductId == 1 ? od.Vat : null) * ditm.QuantityBak
                        : od.Price * od.Quantity - (ditm.PriceBak ?? ditm.Price) * ditm.QuantityBak) ?? 0;

                    obj.VatTotalOri += (vatEnable ? (od.Price * od.Quantity - (ditm.PriceBak ?? ditm.Price) * ditm.QuantityBak) * (od.ProductId == 1 ? od.Vat.Value : product.Vat.Value) : 0) ?? 0;

                    var finalVals = ditm.Extract(monFields);
                    var changes = originVals.GetChanges(finalVals);
                    if (changes.Count != 0)
                        changedItems.AddRange(changes.Select(m => new System.Tuple<string, string, OrderDetail>(m.Key, m.Value, ditm)));
                }
            }

            if (settings.Hidden.ProductControll && settings.Hidden.OrderControllFlag != 0)
            {
                if (obj.OrderDetails.Any(m => m.Quantity > 0 && 0 != (m.Product.Flags & (int)ProductFlags.Controlled)))
                    obj.Flags |= settings.Hidden.OrderControllFlag;
                else obj.Flags = obj.Flags & ~settings.Hidden.OrderControllFlag;
            }

            obj.FinalTotal += obj.OrderTotalOri - obj.OrderTotalBak;

            if (SaveOrUpdate(db, true, (sess, order) =>
            {
                foreach (var itm in qtyChanges.Where(m => m.Item2 != 0))
                {
                    if (ProductMgr.UpdateQuantity(db, itm.Item1.Product, itm.Item1.Variant, -1 * itm.Item2, false, false, true, false,
                        new ProdQtyLogModel() { Reason = (int)ProdQtyLogReason.Ordered, RefIdFunc = () => obj.Id }))
                    {
                        itm.Item1.Product.OrderDetailNum = null;
                        sess.Update(itm.Item1.Product);
                        if (itm.Item1.Variant != null) sess.Update(itm.Item1.Variant);
                    }                    
                }

                return true;
            }, (sess, order) =>
            {
                var updGatewayIds = (settings.Hidden.UpdatableOrderGatewayIds ?? "").Split(',', ';').Select(m => m.TryParseInt() ?? 0).Where(m => m > 0).ToArray();

                if ((order.FinalTotalBak ?? order.FinalTotal) >= order.FinalTotal || ((obj.PaymentMethod.PaymentSystemId.InList(updGatewayIds) && !obj.PaymentMethod.IsOnlinePayment) || 0 != (order.Flags & (int)OrderFlagType.Manual))) return true;
                errMsg = StringTable.OrderTotalMustNotGreaterThanReservedAmount;
                return false;
            }, obj))
            {
                new OrderMgr().OnUpdate(obj, db);
                return new System.Tuple<bool, string>(true, null);
            }
            else return new System.Tuple<bool, string>(false, errMsg);
        } 

        #endregion

        #region Take

        public static List<Domain.Order> Take(IDB db, int count, int productid, byte status, int? variantid = null)
        {
            return Take(db, count, productid, variantid, status);
        }

        public static List<Domain.Order> Take(IDB db, int count, int productid, int? variantid, params byte[] statuses)
        {
            if (statuses.SafeCount() == 0) return new List<Domain.Order>();

            int total;
            var hSql = string.Format(@"
            select ord from {0} as odetails join odetails.Order as ord
            where odetails.Product.Id = {1} and ord.OrderStatus in ({2}) ", typeof(OrderDetail).FullName, productid, statuses.ToString(","));

            if (variantid != null) hSql += " and odetails.Variant.Id = " + variantid;

            return GetList<Domain.Order>(db, hSql, 1, count, out total, null);
        }
        #endregion

        #region CountVariant

        public static List<System.Tuple<int, int>> CountVariant(IDB db, byte status, int productid, int[] variantids)
        {
            return CountVariant(db, productid, variantids, new[] { status });
        }

        public static List<System.Tuple<int, int>> CountVariant(IDB db, int productid, int[] variantids, params byte[] statuses)
        {
            if (variantids.SafeCount() == 0 || statuses.SafeCount() == 0) return new List<System.Tuple<int, int>>();

            var hSql = string.Format(@"
            select odetails.Variant.Id,count(*) from {0} as odetails join odetails.Order as ord
            where ord.OrderStatus in ({1}) and odetails.Product.Id = {2} and odetails.Variant.Id in ({3})
            group by odetails.Variant.Id",
                typeof(OrderDetail).FullName, statuses.ToString(","), productid, variantids.ToString(","));

            return GetList<object[]>(db, hSql).Select(x => new { Id = (int)Convert.ChangeType(x[0], typeof(int)), Count = (int)Convert.ChangeType(x[1], typeof(int)) })
                .Select(x => new System.Tuple<int, int>(x.Id, x.Count))
                .ToList();
        }

        #endregion

        #region AnyOrder
        
        public static bool AnyOrder(IDB db, IProduct product)
        {
            if (product == null || product.Id == 0) return false;
                     
            int total;
            return GetList<int>(db, string.Format("select odetail.Id from {0} as odetail where odetail.Product.Id = {1} ",
                    typeof (Domain.OrderDetail).FullName, product.Id), 1, 1, out total, null, null).Count != 0;
        }

        public static bool AnyOrder(IDB db, IVariant variant)
        {
            if (variant == null || variant.Id == 0) return false;

            int total;
            return GetList<int>(db, string.Format("select odetail.Id from {0} as odetail where odetail.Variant.Id = {1} ",
                typeof(Domain.OrderDetail).FullName, variant.Id), 1, 1, out total, null, null).Count != 0;
        } 

        #endregion

        #region Get

        public OrderModel Get(IDB db, string reservationId)
        {
            var obj = db.DataService.GetObject<Domain.Order>(0, "ReservationId", reservationId);
            return Mapper.Map<Domain.Order, OrderModel>(obj);
        }

        public static OrderModel GetEmail(IDB db, string email)
        {
            if (email.IsNullOrEmpty()) return null;

            int total;
            var hSql = string.Format(@"select ord from {0} as ord 
            where ord.Customer.Email like '{1}'
            order by ord.CreatedDate desc", typeof(Domain.Order).FullName, TranslateKWord(email));

            var ret = GetList<Domain.Order>(db, hSql, 1, 1, out total, null).FirstOrDefault();

            if (ret != null) return Mapper.Map<Domain.Order, OrderModel>(ret);

            var cust = CustomerMgr.Get(db, email);

            if (cust == null) return null;

            return new OrderModel()
            {
                Customer = Mapper.Map<Customer, CustomerModel>(cust),
                BillingName = cust.BillingName,
                BillingMobile = cust.BillingMobile,
                BillingPhone = cust.BillingPhone,
                BillingAddress = cust.BillingAddress,
                BillingCity = cust.BillingCity,
                BillingPostnr = cust.BillingPostnr,
                BillingPosted = cust.BillingPosted,
                BillingCountryCode = cust.BillingCountry.SafeProp(m => m.Code),
                BillingCountryId = cust.BillingCountry.SafeProp(m => m.Id),
                BillingCountryName = cust.BillingCountry.GetName(),

                DeliverName = cust.DeliverName,
                DeliverMobile = cust.DeliverMobile,
                DeliverPhone = cust.DeliverPhone,
                DeliverAddress = cust.DeliverAddress,
                DeliverCity = cust.DeliverCity,
                DeliverPostnr = cust.DeliverPostnr,
                DeliverPosted = cust.BillingPosted,
                DeliverCountryCode = cust.DeliverCountry.SafeProp(m => m.Code),
                DeliverCountryId = cust.DeliverCountry.SafeProp(m => m.Id),
                DeliverCountryName = cust.DeliverCountry.GetName(),
            };
        }

        public static OrderModel Get(IDB db, long id)
        {
            return Get(db, id, cpos: false);
        }

        public static OrderModel Get(IDB db, long id, bool? cpos)
        {
            return Get(db, id, cpos, false);
        }

        public static OrderModel Get(IDB db, long id, bool? cpos, bool resetDiscount)
        {
            Domain.Order obj;
            if (id >= Cart.Seed)
            {
                var cart = db.GetSession().Get<Cart>((int) (id - Cart.Seed));

                if (cart == null)
                {
                    obj = db.DataService.GetObject<Domain.Order>(GetSiteId(db), "CartId", (int)(id - Cart.Seed));
                    
                    if (obj != null && cpos != null)
                        if ((cpos.Value && !obj.Type.InList(OrderType.Pos)) ||
                            (!cpos.Value && !obj.Type.InList(OrderType.Web)))
                            return null;

                    goto LB_END;
                }

                var ret = cart.Data.IsNullOrEmpty() ? null : JsonConvert.DeserializeObject<OrderModel>(cart.Data, GetJsonSettings());

                if (ret != null)
                {
                    ret.OfferLastView = cart.LastView;
                    ret.OfferEmailView = cart.EmailView;
                    ret.OfferOrderId = cart.RefOrderId;
                    if(cpos != null)
                        if ((cpos.Value && cart.DataCpos.IsNullOrEmpty()) ||
                            (!cpos.Value && !cart.DataCpos.IsNullOrEmpty())) return null;

                    if (resetDiscount)
                    {
                        ret.DiscountId = null;
                        ret.DiscountValue = null;
                        ret.DiscountName = null;
                    }
                }

                return ret;
            }

            obj = GetDom(db, id);

            LB_END:
            return obj == null ? null : Mapper.Map<Domain.Order, OrderModel>(obj);
        }

        public static Domain.Order GetDom(IDB db, long id, ISession session = null)
        {
            if (id <= 0) return null;

            return GetDom(session ?? db.GetSession(), id);
        }

        public static Domain.Order GetDom(ISession session, long id)
        {
            return GetDom(session, id, cpos: false);
        }

        public static Domain.Order GetDom(ISession session, long id, bool? cpos)
        {
            return GetDom(session, id, cpos, false);
        }

        public static Domain.Order GetDom(ISession session, long id, bool? cpos, bool resetDiscount)
        {
            if (id <= 0) return null;

            if (id >= Cart.Seed)
            {
                var cart = session.Get<Cart>((int) (id - Cart.Seed));
                var model = cart == null || cart.Data.IsNullOrEmpty() ? null : JsonConvert.DeserializeObject<OrderModel>(cart.Data, GetJsonSettings());

                if (cpos != null && cart != null)
                    if ((cpos.Value && cart.DataCpos.IsNullOrEmpty()) ||
                        (!cpos.Value && !cart.DataCpos.IsNullOrEmpty())) return null;

                if (model == null)
                {
                    var db = new DefaultDb(session);
                    return db.DataService.GetObject<Domain.Order>(GetSiteId(db), "CartId", (int)(id - Cart.Seed));
                }

                Domain.Order backOrder;
                Customer customer;
                List<RecurOrder> deletedRecurrings;

                if (resetDiscount)
                {
                    model.DiscountId = null;
                    model.DiscountValue = null;
                }

                var obj = SaveorUpdate(new DefaultDb(session), model, out customer, out backOrder, out deletedRecurrings, new List<Coupon>(), new List<Domain.Discount>());

                if (obj != null)
                {
                    obj.Customer = customer;
                    obj.PaymentData = cart.PaymentData; //obj.PaymentData.IfNull(cart.PaymentData);
                }

                return obj;
            }

            return session.Get<Domain.Order>((int) id);
        }

        #endregion

        #region GetNextId

        public static int? GetNextId(IDB db, int id, byte status, bool previous = false)
        {
            var hSql = string.Format(@"
            select ord from {0} as ord 
            where ord.OrderStatus = {1} and ord.Id {2} {3}
            order by ord.Id {4}", typeof(Domain.Order).FullName, status, previous ? "<" : ">", id, previous ? "desc" : "asc");

            int total;
            var ret = GetList<Domain.Order>(db, hSql, 1, 1, out total, null);

            return ret.Count == 0 ? (int?)null : ret[0].Id;
        }

        #endregion

        #region GetProductStats

        /// <summary>[Cpos], return (productid,variantid,quanity)</summary>
        public static List<System.Tuple<int, int?, int>> GetProductStats(IDB db, List<System.Tuple<int, int?>> list)
        {
            if (list.SafeCount() == 0) return new List<System.Tuple<int, int?, int>>();

            var hSql = string.Format(@"
            select odetails.Product.Id, odetails.Variant.Id, sum(odetails.Quantity)
            from {0} as ord join ord.OrderDetails as odetails 
            where odetails.Product.Id in ({1}) and ord.OrderStatus in ({2},{3},{4},{5}) ",
                typeof(Domain.Order).FullName, list.Select(x => x.Item1).ToString(","),
                (byte)OrderStatus.New, (byte)OrderStatus.RestOrder, (byte)OrderStatus.Processing, (byte)OrderStatus.PayBeforeDelivery);

            hSql += " group by odetails.Product.Id, odetails.Variant.Id ";

            var ret = GetList<object[]>(db, hSql)
                .Select(arr => new
                {
                    ProductId = arr[0].SafeChangeType<int>(),
                    VariantId = arr[1].SafeChangeType<int?>(),
                    Count = arr[2].SafeChangeType<int>()
                }).ToList();

            return list
                .Select(m => new System.Tuple<int, int?, int>(m.Item1, m.Item2, ret.Where(z => z.ProductId == m.Item1 && (m.Item2 == null || z.VariantId == m.Item2)).Sum(z => z.Count)))
                .ToList();
        }

        #endregion

        #region BindStats

        public static void BindStats(IDB db, List<Domain.Product> list)
        {
            if (list.SafeCount() == 0) return;

            var dirty = list.Where(x => x.VariantNum == null)
                .Action(x => x.VariantNum = x.Variants.Count(m => !m.Deleted))
                .ToList();

            list = list.Where(x => x.OrderDetailNum == null).ToList();

            if (list.SafeCount() == 0)
            {
                if (dirty.Count != 0) Update(db, dirty);
                return;
            }

            var hSql = string.Format(@"
            select odetails.Product.Id, count(*)
            from {0} as ord join ord.OrderDetails as odetails 
            where odetails.Product.Id in ({1}) and ord.OrderStatus in ({2},{3},{4},{5}) ",
                typeof(Domain.Order).FullName, list.Select(x => x.Id).ToString(","),
                (byte)OrderStatus.New, (byte)OrderStatus.RestOrder, (byte)OrderStatus.Processing, (byte)OrderStatus.PayBeforeDelivery);

            hSql += " group by odetails.Product.Id ";

            var ret = GetList<object[]>(db, hSql)
                .Select(arr => new
                {
                    ProductId = arr[0].SafeChangeType<int>(),
                    Count = arr[1].SafeChangeType<int>()
                }).ToList();

            list.Action(x => x.OrderDetailNum = 0);

            foreach (var itm in list.Join(ret,
                o => o.Id, m => m.ProductId, (o, m) => new { O = o, M = m }))
            {
                itm.O.OrderDetailNum = itm.M.Count;
            }

            Update(db, list);
        }

        /// <summary>BackorderProduct</summary>
        public static void BindStats(IDB db, List<ProductModel> list)
        {
            if (list.SafeCount() == 0) return;

            var hSql = string.Format(@"
            select odetails.Product.Id,sum(odetails.QuantityOri),count(distinct ord.Id)
            from {0} as ord join ord.OrderDetails as odetails 
            where ord.OrderStatus in ({1},{3}) and odetails.Product.Id in ({2}) ",
                typeof(Domain.Order).FullName, (byte)OrderStatus.New, list.Select(x => x.Id).ToString(","), (byte)OrderStatus.Processing);

            hSql += " group by odetails.Product.Id ";

            var ret = GetList<object[]>(db, hSql)
                .Select(arr => new
                {
                    ProductId = arr[0].SafeChangeType<int>(),
                    //VariantId = arr[1].SafeChangeType<int?>(),
                    QtyInOrders = arr[1],
                    Orders = arr[2]
                }).ToList();

            list.Action(x => x.QtyInOrders = x.Orders = 0);

            foreach (var itm in list.Join(ret,
                o => o.Id, m => m.ProductId, (o, m) => new { O = o, M = m }))
            {
                itm.O.QtyInOrders = itm.M.QtyInOrders.SafeChangeType<int?>();
                itm.O.Orders = itm.M.Orders.SafeChangeType<int?>();
            }
        }

        public static void BindStats(IDB db, List<Customer> list)
        {
            if (list.SafeCount() == 0) return;

            list = list.Where(x => x.OrderNum == null).ToList();

            if (list.SafeCount() == 0) return;

            var hSql = string.Format(@"
            select ord.Customer.Id, count(*)
            from {0} as ord  
            where ord.Customer.Id in ({1})
            group by ord.Customer.Id", typeof(Domain.Order).FullName, list.Select(x => x.Id).ToString(","));

            var ret = GetList<object[]>(db, hSql)
                .Select(arr => new
                {
                    CustomerId = arr[0].SafeChangeType<int>(),
                    Count = arr[1].SafeChangeType<int>()
                }).ToList();

            list.Action(x => { x.OrderNum = 0; x.ModifiedDate = DateTime.Now; });

            foreach (var itm in list.Join(ret,
                o => o.Id, m => m.CustomerId, (o, m) => new { O = o, M = m }))
            {
                itm.O.OrderNum = itm.M.Count;
            }

            Update(db, list);
        }

        public static void BindStats(IDB db, List<RecurOrder> list)
        {
            if (list.SafeCount() == 0) return;

            list = list.Where(x => x.OrderNum == null).ToList();

            if (list.SafeCount() == 0) return;

            var hSql = string.Format(@"
            select ord.Recurring.Id, count(*)
            from {0} as ord  
            where ord.Recurring.Id in ({1})
            and ord.Id <> ord.Recurring.Order.Id 
            group by ord.Recurring.Id", typeof(Domain.Order).FullName, list.Select(x => x.Id).ToString(","));

            var ret = GetList<object[]>(db, hSql)
                .Select(arr => new
                {
                    RecurringId = arr[0].SafeChangeType<int>(),
                    Count = arr[1].SafeChangeType<int>()
                }).ToList();

            list.Action(x => x.OrderNum = 1);

            foreach (var itm in list.Join(ret,
                o => o.Id, m => m.RecurringId, (o, m) => new { O = o, M = m }))
            {
                itm.O.OrderNum = itm.M.Count + 1;
            }

            Update(db, list);
        }

        #endregion

        #region QueuePowerOffice

        public static void QueuePowerOffice(IDB db, Domain.Order obj, ISession session = null)
        {
            var settings = SettingsBiz.GetSettings();
            if (!settings.Hidden.PowerOffice || !settings.Hidden.PoGoInvoicing || obj.PowerOfficeId != null ||
                !obj.OrderStatus.InList(OrderStatus.New, OrderStatus.Processing, OrderStatus.PayBeforeDelivery)) return;

            session = session ?? db.GetSession();

            obj.PowerOfficeId = Guid.Empty;

            DelayQueueMgr.Add(session, typeof (OrderMgr), obj.Id, null);

            session.Flush();
        } 

        #endregion

        #region DoDelayJob

        public static void DoDelayJob(IDB db, DelayQueue itm)
        {
            var order = db.DataService.GetObject<Domain.Order>(itm.RefId);
            var log = new OrderNoteModel() { Private = true, Flags = 0, OrderId = order.Id, OrderStatus = order.OrderStatus };
            var retries = 3;
            LB_LOOP:
            try
            {
                var go = new Helper.PowerOfficeHelper(db, SettingsBiz.GetSettings().Display.Admin.PowerOffice);

                if (order != null && (order.PowerOfficeId ?? Guid.Empty) == Guid.Empty)
                {
                    var ret = go.SaveInvoice(order);
                    order.PowerOfficeId = ret.Id;
                    order.Update(db.DataService);
                    
                    log.Subject = "Push order to PoGo succeeded".ModLocalize();
                    log.Body = string.Format("PoGo invoice id = {0}".ModLocalize(), ret.Id);

                    Eshop.DAL.OrderNoteMgr.SaveorUpdate(db, log);
                }
            }
            catch (Exception err)
            {
                if (--retries >= 0) goto LB_LOOP;
                else 
                {
                    log.Subject = "Push order to PoGo failed".ModLocalize();
                    log.Body = (err.Message ?? "").TrimEnd('.') + ".\r\n" + (err.StackTrace ?? "").TrimLength(1000);

                    Eshop.DAL.OrderNoteMgr.SaveorUpdate(db, log);

                    //throw; 
                }
            }
        }

        #endregion

        #region OnUpdate
        protected override void OnUpdate(Domain.Order obj)
        {
            OnUpdate(obj, null);
        }

        protected void OnUpdate(Domain.Order obj, IDB db, bool addNew = false, bool useTransaction = true)
        {
            OrderScope.Invalidate();
            try
            {
                if (addNew && obj.OrderStatus.InList(OrderStatus.Sent, OrderStatus.Credit)) Singleton<IMembershipMgr>.Default.FireOrderCreated(obj, db ?? new DefaultDb(), useTransaction);
            }
            catch (Exception err) { LogErr(err); }

            if (obj != null) Singleton<IOrderMgr2>.Default.ClearReportCache(db ?? new DefaultDb(), ReportType.MonthlySales, obj.CreatedDate, useTransaction);
        }
        #endregion

        #region OnDelete
        protected override void OnDelete(Domain.Order obj)
        {
        }
        #endregion

        #region IOrderMgr
        
        IOrder IOrderMgr.GetDom(IDB db, long id, ISession session)
        {
            return OrderMgr.GetDom(db, id, session);
        }

        int IOrderMgr.GetShippingCount(IDB db, int shippingId)
        {
            return OrderMgr.GetShippingCount(db, shippingId);
        }

        int IOrderMgr.GetPaymentMethodCount(IDB db, int paymentMethodId)
        {
            return OrderMgr.GetPaymentMethodCount(db, paymentMethodId);
        }

        bool IOrderMgr.CompletePayment(IDB db, IOrder obj)
        {
            return OrderMgr.CompletePayment(db, (Domain.Order)obj);
        }

        int IOrderMgr.GetCouponCount(IDB db, int customerId, int couponId)
        {
            return OrderMgr.GetCouponCount(db, customerId, couponId);
        }

        int IOrderMgr.GetOrderCouponCount(IDB db, int couponId)
        {
            return OrderMgr.GetCouponCount<Domain.Order>(db, couponId);
        }

        int IOrderMgr.GetOrderDetailCouponCount(IDB db, int couponId)
        {
            return OrderMgr.GetCouponCount<OrderDetail>(db, couponId);
        }

        int IOrderMgr.GetOrderDiscountCount(IDB db, int discountId)
        {
            return OrderMgr.GetDiscountCount<Domain.Order>(db, discountId);
        }

        int IOrderMgr.GetOrderDetailDiscountCount(IDB db, int discountId)
        {
            return OrderMgr.GetDiscountCount<OrderDetail>(db, discountId);
        }

        List<int[]> IOrderMgr.GetProductTotalSales(IDB db, params int[] productIds)
        {
            return OrderMgr.GetProductTotalSales(db, productIds);
        }

        void IOrderMgr.BindStats(IDB db, List<IProduct> list)
        {
            OrderMgr.BindStats(db, list.Cast<Domain.Product>().ToList());
        }

        void IOrderMgr.BindStats(IDB db, List<IProduct2> list)
        {
            OrderMgr.BindStats(db, list.Cast<ProductModel>().ToList());
        }

        List<System.Tuple<int, int>> IOrderMgr.CountVariant(IDB db, byte status, int productid, int[] varinatids)
        {
            return OrderMgr.CountVariant(db, status, productid, varinatids);
        }

        List<System.Tuple<int, int>> IOrderMgr.CountVariant(IDB db, int productid, int[] varinatids, params byte[] statuses)
        {
            return OrderMgr.CountVariant(db, productid, varinatids, statuses);
        }

        List<int> IOrderMgr.GetDependOrders(IDB db, int count, int productid, byte status, int? varinatid)
        {
            return OrderMgr.Take(db, count, productid, status, varinatid).Select(x => x.Id).ToList();
        }

        List<int> IOrderMgr.GetDependOrders(IDB db, int count, int productid, int? varinatid, params byte[] statuses)
        {
            return OrderMgr.Take(db, count, productid, varinatid, statuses).Select(x => x.Id).ToList();
        }

        List<IOrder> IOrderMgr.GetDependOrders(IDB db, int backProductId)
        {
            int total;
            return OrderMgr.GetAll(db, null, null, 1, int.MaxValue, out total, SortDirection.Ascending, backproductId: backProductId).Select(x => (IOrder)x.GetRaw()).ToList();
        }

        bool IOrderMgr.AnyOrder(IDB db, IProduct product)
        {
            return OrderMgr.AnyOrder(db, product);  
        }

        bool IOrderMgr.AnyOrder(IDB db, IVariant variant)
        {
            return OrderMgr.AnyOrder(db, variant); 
        }

        IOrder[] IOrderMgr.FirePaymentDone(IDB db, IOrder obj)
        {
            if (FuncPaymentDone != null)
            {
                FuncPaymentDone((Domain.Order)obj, db);
                return new [] {obj};
            }
            else if (FuncPaymentDone2 != null)
            {
                var ret= FuncPaymentDone2((Domain.Order)obj, db);
                return ret == null ? new IOrder[0] : ret.Cast<IOrder>().ToArray();
            }

            return new[] { obj };
        }

        #endregion

        #region GetJsonSettings
        public static JsonSerializerSettings GetJsonSettings()
        {
            return new JsonSerializerSettings()
            {
                Culture = CultureInfo.InvariantCulture,
                Converters = new List<JsonConverter>() { new OMoney.JsonConverter(), new OMoneyMc.JsonConverter(), new OFloat.JsonConverter(), new ODateTime.JsonConverter() }
            };
        } 
        #endregion
    }
}