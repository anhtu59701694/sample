using AutoMapper;
using Claw.Controls.Attributes;
using Claw.Controls.Modules;
using Claw.Core;
using Claw.Core.Domain;
using Claw.Core.Enumerations;
using Claw.Core.Extensions;
using Claw.Core.Interfaces;
using Claw.Core.Logs;
using Claw.Core.Utility;
using Claw.Modules.Eshop.DAL;
using Claw.Modules.Eshop.Domain;
using Claw.Modules.Eshop.Domain.Interfaces;
using Claw.Modules.Eshop.Helper;
using Claw.Modules.Eshop.Models;
using Eshop.Payment.DIBS;
using Eshop.Shipping.Bring;
using Kendo.Mvc.Extensions;
using Kendo.Mvc.UI;
using Newtonsoft.Json;
using NHibernate.Mapping;
using NPOI.HSSF;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Dynamic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Mail;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using System.Web.UI.WebControls;
using static ICSharpCode.SharpZipLib.Zip.ExtendedUnixData;
using KlarnaCheckoutPaymentData = Eshop.Payments.KlarnaCheckout.KlarnaCheckoutPaymentData;
using KlarnaCheckoutProcessor = Eshop.Payments.KlarnaCheckout.KlarnaCheckoutProcessor;
using PaymentData = Eshop.Payments.BasePayment.PaymentData;

namespace Claw.Modules.Eshop.Controllers.Admin
{
    [AdminPerm("OrderBits", byte.MaxValue)]
    public class OrderController : BaseController<OrderModel, Domain.Order>
    {
        public static volatile Action<OrderDetailModel> FuncOdetailDataEx = null;

        private const string IndexView = "Admin/OrdersIndex.cshtml";
        private const string EditView = "Admin/OrdersEdit.cshtml";

        #region OrderController

        static OrderController()
        {
            QParam.AddTrivialArgs("shippingService");   
  
            ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
        }

        public OrderController(IDataService ds) :
            base(ds)
        {
        }
        #endregion

        #region Index
        [ValidateInput(false)]
        [AdminPerm("OrderBits", PermissionMask.View)]
        public ActionResult Index(OrderModel.AdvSearchModel searchForm,
            bool? backorder, int? backproductid, int? customerid, int? recurOrderId)
        {
            ViewBag.Settings = GetSettings();
            ViewBag.Backorder = backorder;
            ViewBag.BackProduct = Product.DAL.ProductMgr.Get(this, backproductid ?? 0);
            ViewBag.RecurOrderId = recurOrderId;
            ViewBag.Customer = CustomerMgr.Get(this, customerid ?? 0);
            ViewBag.SearchInfo = searchForm ?? new OrderModel.AdvSearchModel();
            ViewBag.PaymentMethods = PaymentMethodMgr.GetAll(this, -1);
            ViewBag.Shippings = ShippingMgr.GetAll(this);

            ViewBag.Currencies = CurrencyMgr.GetAll(this, null, true).Select(x => x.GetRaw()).ToList();

            var sform = searchForm;
            ViewBag.EagerData = new GridEagerLoadDataHandler((page, pageSize, sortField, sortDir) => List(page, pageSize, sortField, sortDir, sform));

            return ClawView(IndexView);
        }
        #endregion

        #region List

        [ValidateInput(false)]
        [AdminPerm("OrderBits", PermissionMask.View)]
        public ActionResult List([DataSourceRequest] DataSourceRequest request, OrderModel.AdvSearchModel searchForm)
        {
            TrimReq(request);

            string sortField = "Id";
            var sortDir = SortDirection.Descending;

            GetSortField(request, ref sortField, ref sortDir);

            return Json(List(request.Page, request.PageSize, sortField, sortDir, searchForm));
        }

        private DataSourceResult List(int page, int pageSize, string sortField, SortDirection sortDir,
            OrderModel.AdvSearchModel searchForm)
        {
            if (searchForm == null) searchForm = new OrderModel.AdvSearchModel();

            int totalRows;
            var email = searchForm.Email.SafeTrim();
            var status = searchForm.Status;
            var name = searchForm.Name.SafeTrim();
            var refId = searchForm.RefId.SafeTrim();
            var productName = searchForm.ProductName.SafeTrim();
            var paymentId = searchForm.PaymentMethodId;
            var shippingId = searchForm.ShippingId;
            var fromdate = searchForm.FromDate;
            var todate = null as ODateTime;
            var backorder = GetParam<bool?>("backorder");
            var backproductId = GetParam<int?>("backproductid");
            var customerid = GetParam<int?>("customerid");
            var recurOrderId = GetParam<int?>("recurOrderId");
            var shippingService = GetParam<bool?>("shippingService");
            var pendingMailbox = GetParam<bool?>("pendingMailbox") ?? false;
            var bringShipping = GetParam<bool?>("bringShipping") ?? false;
            var tilRegnskap = GetParam<bool?>("tilRegnskap");
            var offer = this is OfferController;
            var porterbuddy = GetParam<bool>("porterbuddy");
            var renewPayment = GetParam<bool?>("renewPayment");
            var sent2PickupPoint = GetParam<bool?>("sent2PickupPoint");
            var dropShip = GetParam<bool?>("dropship");

            if (fromdate != null && fromdate.Value != null) todate = fromdate.Value.Value.AddDays(1);

            if (backorder != null|| this is CartController || offer) sortField = "Id"; ;

            var items = this is CartController || offer
                ? CartMgr.GetAll(this, page, pageSize, out totalRows, sortDir, sortField, null, offer)
                : OrderMgr.GetAllEx(this, status, email, page, pageSize, out totalRows,
                sortDir, sortField, name, refId, productName, fromdate ?? searchForm.DateFrom, todate ?? searchForm.DateTo, paymentId, shippingId,
                backorder, backproductId, customerid, recurOrderId, shippingService, pendingMailbox, bringShipping,
                tilRegnskap: tilRegnskap, porterbuddy: porterbuddy, flagcustom0: searchForm.FlagCustom0,
                flagcustom1: searchForm.FlagCustom1, serialNumber: searchForm.SerialNumber, renewPayment: renewPayment, 
                sent2PickupPoint: sent2PickupPoint, dropShip: dropShip ?? searchForm.FlagDropShip)
                .Action(m =>
                {
                    var raw = m.GetRaw();
                    m.InlineEdit = true;
                    m.InitOrderDetails(this);

                    if (FuncOdetailDataEx != null) m.OrderDetails.Action(FuncOdetailDataEx);

                    if (m.SplitPayment != 0)
                        m.PaymentName = raw.GetSplitPayments(this).Select(z => z.Item1.GetName()).ToString("; ");

                    m.ShippingTrackingUrl = raw.GetShippingTrackingUrl();

                    if (raw.GiftCardTotal != 0) m.FinalTotal = m.FinalTotal.Value + raw.GiftCardTotal;
                });

            return new DataSourceResult() { Data = items, Total = totalRows };
        }

        #endregion

        #region View
        [ValidateInput(false)]
        public ActionResult View(long id, string lang, string optlang, string mode)
        {
            var offer = this is OfferController;

            if (offer && id > Cart.Seed)
            {
                var cart = DataService.GetObject<Cart>((int)(id - Cart.Seed));
                if (cart != null) return Redirect(string.Format("/cart?offerid={0}&token={1}", cart.Id, cart.Token));
            }

            return RedirectToAction("Input", new { id, lang, optlang, mode });
        }
        #endregion
        #region ViewSaft
        [ValidateInput(false)]
        public ActionResult ViewSaft(int id, string lang, string optlang, string mode)
        {
            var obj = OrderMgr.GetDom(this, id);
            return Content(obj != null && obj.CashRegisterNumber != null && obj.Invoice != null ? obj.Invoice.Signature : null, "text/plain");
        }
        #endregion
        #region EditRecurring
        [ValidateInput(false)]
        public ActionResult EditRecurring(int id, string lang, string optlang, string mode)
        {
            return RedirectToAction("Input", "RecurOrder", new { id, lang, optlang, mode });
        }
        #endregion

        #region EmailList
        [AdminPerm("OrderBits", PermissionMask.View)]
        public ActionResult EmailList(long id)
        {
            var order = OrderMgr.Get(this, id);
            if (order != null && id < Cart.Seed)
            {
                order.InitOrderDetails(this);
                order.InitMandatoryInfo();
            }
            return ClawView("Admin/_ProductsListEmail.cshtml", order);
        }
        #endregion

        #region Input.Get
        [ValidateInput(false)]
        [AdminPerm("OrderBits", PermissionMask.View)]
        public ActionResult Input(long id, int? customerId, string lang, string optlang, string mode, int? offerIdCloned = null)
        {
            var offer = this is OfferController;
            var cart = this is CartController;
            var obj = id == 0 && offerIdCloned != null
                ? OrderMgr.Get(this, Cart.Seed + offerIdCloned.Value, null, true)
                : OrderMgr.Get(this, id, null);

            if (offer && obj != null) obj.Raw = id == 0 && offerIdCloned != null
                ? OrderMgr.GetDom(this.GetSession(), Cart.Seed + offerIdCloned.Value, null, true) 
                : OrderMgr.GetDom(this, id);

            ViewBag.Settings = GetSettings();
            ViewBag.Countries = CountryMgr.GetAll(this);
            ViewBag.PaymentMethods = PaymentMethodMgr.GetAll(this, obj == null ? 0 : obj.Id, true);
            ViewBag.Shippings = ShippingMgr.GetAll(this, true);
            ViewBag.Currencies = CurrencyMgr.GetAll(this, null, true).Select(x => x.GetRaw()).ToList();
            ViewBag.Languages = LanguageMgr.GetAll();

            if (obj == null)
            {
                obj = new OrderModel()
                {
                    Customer = CustomerMgr.Get(this, customerId ?? 0) ?? new CustomerModel() { Individual = true },
                    OrderStatus = (byte)OrderStatus.New,
                    Currency = CurrencyMgr.Default.Code,
                    LangCode = LanguageMgr.FocusLangCode,
                    BillingCountryId = 1,
                    DeliverCountryId = 1,
                    OrderDetails = new List<OrderDetailModel>(),
                };
                if (obj.Customer.Id != 0)
                    foreach (var itm in typeof(CustomerModel).GetProperties())
                    {
                        var f = typeof(OrderModel).GetProperty(itm.Name);
                        if (f != null && f.CanWrite && (f.Name.InList("OrgNumber", "CompanyName", "InvoiceEmail") || f.Name.StartsWith("Deliver") || f.Name.StartsWith("Billing")))
                            f.SetValue(obj, itm.GetValue(obj.Customer));
                    }
            }
            else
            {
                var raw = obj.GetRaw();
                obj.Currency = obj.Currency.IsNullOrEmpty() ? CurrencyMgr.Default.Code : obj.Currency;
                obj.LangCode = obj.LangCode.IsNullOrEmpty() ? LanguageMgr.FocusLangCode : obj.LangCode;
                obj.ShipOrderTotal = raw.GetShipOrderTotal(obj.DeliverCountryCode);
                obj.ShipWeightTotal = raw.GetShipWeightTotal();
                obj.OriginalCredited = raw.GetOriginCreditId();
                obj.CouponCodeShared = obj.GetRaw().GetSharedCoupon(x => x.Code);
                obj.CouponNameShared = obj.GetRaw().GetSharedCoupon(x => x.Name);

                if (raw.Recurring != null &&
                    raw.Recurring.Status.InList(RecurOrderStatus.Pending)) obj.Recurring = null;
            }

            if (cart || offer)
            {
                obj.Id = obj.CartId ?? 0;
                obj.TotalWeight = obj.GetTotalWeight(this);
                obj.OrderNotes = new List<OrderNoteModel>();
                obj.PaymentLogs = new List<PaymentLogModel>();

                if (offer && obj.OrderDetails != null)
                    obj.OrderDetails.Action(m =>
                    {
                        var prod = Product.DAL.ProductMgr.GetDom(this, m.ProductId ?? 0);
                        var vari = Product.DAL.VariantMgr.GetDom(this, m.VariantId ?? 0);

                        if (prod == null) return;

                        m.CostPrice = vari != null ? vari.CostPrice : prod.SafeProp(z => z.CostPrice);

                        if (m.CostPrice.IsNull() && m.PriceInc > 0)
                            m.CostPrice = (0.8m * m.PriceInc) / (1 + m.Vat);
                    });

                if (offer) {
                    obj.OfferSmsMsg = obj.OfferSmsMsg ?? StringTable.OfferSmsMessageTempl;
                    if (obj.CartId > 0) {
                        var data = new { Order = obj, shop_url = ClawContext.Current.CurrentSite.SiteUrl, shop_name = GetSettings().General.StoreName };
                        obj.OfferSmsMsg = obj.OfferSmsMsg.ReplaceEx(data);
                    }
                    obj.InitMandatoryInfo(this); 
                }

                if (offer && offerIdCloned != null)
                {
                    var raw = obj.GetRaw();

                    obj.Id = raw.Id = 0;
                    obj.CartId = raw.CartId = null;
                    obj.OfferToken = raw.OfferToken = null;
                    obj.Customer.Id = raw.Customer.Id = 0;
                    obj.CartIdOffered = null;
                    obj.OfferOrderId = null;
                    obj.CartNotified = null;
                    obj.OfferSendSms = null;
                    obj.OfferSmsMsg = StringTable.OfferSmsMessageTempl;
                    obj.Comments = null;
                }
            }
            else
            {
                obj.InitMandatoryInfo(this);
                obj.InitOrderDetails(this);
                obj.InitOrderNotes();
                obj.InitPaymentLogs();
            }

            obj.ItemTotal = obj.OrderDetails.Count;

            ViewBag.TemplOrderDetail = new OrderDetailModel()
            {
                OrderId = obj.Id,
                Delivered = false,
                CouponValue = 0,
                DiscountValue = 0
            };

            return ClawView(EditView, obj);
        }
        #endregion

        #region Input.Post

        [HttpPost]
        [ValidateInput(false)]
        //[ValidateAntiForgeryToken]
        [AdminPerm("OrderBits", PermissionMask.Edit)]
        public ActionResult Input([DataSourceRequest] DataSourceRequest request, OrderModel obj,
            string lang, string mode, bool overridePayCost = false, bool overrideShipCost = false, bool offerSkipSend = false)
        {
            if (obj == null)
                return AcceptJson
                    ? JsonInline(request, new OrderModel(), false)
                    : RedirectToAction("Index", new { lang, mode });

            var offer = this is OfferController;
            var addNew = obj.Id == 0;


            if (offer)
            {
                #region RemoveKey

                ModelState.RemoveKey("BillingName");
                ModelState.RemoveKey("BillingAddress");
                ModelState.RemoveKey("BillingPostnr");
                ModelState.RemoveKey("BillingPosted");
                ModelState.RemoveKey("BillingMobile");
                ModelState.RemoveKey("DeliverName");
                ModelState.RemoveKey("DeliverAddress");
                ModelState.RemoveKey("DeliverPostnr");
                ModelState.RemoveKey("DeliverPosted");
                ModelState.RemoveKey("DeliverMobile");

                ModelState.RemoveKey("PaymentMethodId");
                ModelState.RemoveKey("PaymentCost");
                ModelState.RemoveKey("PaymentVat");

                ModelState.RemoveKey("ShippingId");
                ModelState.RemoveKey("ShippingCost");
                ModelState.RemoveKey("ShippingVat");

                var offerId = RouteData?.Values["id"]?.ToString().SafeProp(m => long.TryParse(m, out var tmpId) ? tmpId : 0) ?? 0;

                if (offerId > Cart.Seed && OrderMgr.Get(this, offerId)?.OfferEmailSent == false) 
                {
                    ModelState.RemoveKey("Id");
                    obj.CartId = (int)(offerId - Cart.Seed);
                }

                #endregion

                obj.PaymentVat = obj.ShippingVat = 0;
                obj.Customer.DeliverName = obj.DeliverName;
                obj.OfferEmailSent = !offerSkipSend;
            }
            else
            {
                ModelState.RemoveKey("OfferExpiredDate");

                obj.Customer.Fill(obj);
            }

            ModelState.RemoveKey("OfferSmsMsg");
            ModelState.RemoveKey("Customer.");
            ModelState.RemoveKey("Opt.");

            TryValidateModel(obj.Customer, "Customer");

            #region OrderDetails

            if (obj.OrderDetails != null)
                foreach (var itm in obj.OrderDetails
                    .Select((x, idx) => new { Val = x, Index = idx })
                    .Where(x => x.Val.IsDataModelEmpty).OrderByDescending(x => x.Index).ToList())
                {
                    obj.OrderDetails.Remove(itm.Val);
                    ModelState.RemoveKey(string.Format("{0}[{{0}}]",
                        GetFieldName<OrderModel, object>(m => m.OrderDetails)),
                        itm.Index, obj.OrderDetails.Count);
                }
            else obj.OrderDetails = new List<OrderDetailModel>();

            foreach (var odetail in obj.OrderDetails.Where(x => x.Id == 0 && (x.ProductId ?? 0) != 1))
            {
                var prod = Product.DAL.ProductMgr.GetDom(this, odetail.ProductId ?? 0);
                if (prod.Vat != null) odetail.Vat = prod.Vat.Value;
            }

            #endregion

            #region InlineEdit
            if (obj.InlineEdit && obj.Id != 0)
            {
                var newMod = OrderMgr.Get(this, obj.Id);
                newMod.DeliveredDate = obj.DeliveredDate;
                newMod.OrderStatus = obj.OrderStatus;
            }
            #endregion

            if (ModelState.ModLocalize().IsValid)
            {
                #region UserInfo & lang/currency/coupon/discount
                if (obj.Id == 0)
                {
                    obj.UserIp = Request.UserHostAddress;
                    obj.UserAgent = Request.UserAgent;
                    obj.UserHost = Request.UserHostName.ResolveHostName();
                    if (obj.LangCode.IsNullOrEmpty()) obj.LangCode = LanguageMgr.FocusLangCode;
                    if (obj.Currency.IsNullOrEmpty()) obj.Currency = CurrencyMgr.Default.Code;
                    obj.ExchangeRate = CurrencyMgr.GetOrDefault(obj.Currency).Rate;
                    //obj.TryApplyDiscount(this);
                    //obj.TryApplyCoupon(this);
                }
                #endregion

                obj.BackOrderDetails = null;

                if (OrderMgr.SaveorUpdate(this, obj, false, offer, OrderType.Web, overridePayCost, overrideShipCost, false, false, true, false, obj.Id == 0))
                {
                    if (!obj.InlineEdit) SetViewMsgInput(true);

                    if (offer)
                    {
                        var emailTempl = EmailTemplMgr.GetAll(this, (int)EmailTemplateType.OrderOffer).FirstOrDefault();

                        if (emailTempl != null && !offerSkipSend)
                        {
                            obj.InitOrderDetails(this);
                            obj.InitMandatoryInfo(this);

                            Singleton<ISendEmailMgr>.Default.Send(this, obj, obj.OrderStatus, "", emailTempl, false);

                            if (obj.OfferSendSms == true && !obj.DeliverMobile.IsNullOrEmpty() && !obj.OfferSmsMsg.IsNullOrEmpty())
                            {
                                #region SendSms            
                                var settingBiz = SettingsBiz.GetSettings();
                                var data = new { Order = obj, shop_url = ClawContext.Current.CurrentSite.SiteUrl, shop_name = settingBiz.General.StoreName };
                                var result = SmsHelper.SendSMS(settingBiz.Hidden.SmsCountryCode, settingBiz.Hidden.SmsFrom, obj.DeliverMobile, obj.OfferSmsMsg.ReplaceEx(data), (obj.CartId ?? 0).ToString());
                                if (result)
                                {
                                    var settings = SettingsBiz.GetSettings(true);
                                    var balance = settings.Hidden.SmsBalance - 1;
                                    settings.Hidden.SmsBalance = balance;
                                    SettingMgr.Save(settings);

                                    obj.Comments = (obj.Comments.IsNullOrEmpty() ? "" : (obj.Comments + "; ")) + new ODateTime(DateTime.Now).TextF;

                                    var cart = BaseMgr<Cart, Cart>.GetDom(this, obj.CartId ?? 0);

                                    cart.Data = JsonConvert.SerializeObject(obj, OrderMgr.GetJsonSettings());
                                    cart.Update(DataService);
                                }
                                else LogErr(new Exception(string.Format("Send offer sms to {0} failed. Msg: {1}", obj.DeliverMobile, obj.OfferSmsMsg.ReplaceEx(data))));
                                #endregion
                            }
                        }

                        return RedirectToAction("Index", "Offer", new { offer = true });
                    }
                }
                else goto LB_ERR;

                if (obj.SendNotifyEmail == true)
                {
                    LanguageMgr.OverrideLangCode(obj.LangCode);
                    string htmlProductDetail = this.RenderToString("EmailList", new { obj.Id });
                    //LanguageMgr.OverrideLangCode(null);

                    SendEmail(obj.Id, obj.OrderStatus, obj.Comments, true, htmlProductDetail, addNew);
                    LanguageMgr.OverrideLangCode(null);
                }

                var dom = OrderMgr.GetDom(this, obj.Id);
                dom.Flags |= (int)Domain.Interfaces.OrderFlagType.Manual;
                OrderMgr.Update(this, new[] { dom }.ToList());

                var orderNote = new OrderNoteModel
                {
                    OrderId = obj.Id,
                    Subject = string.Empty,
                    Private = false,
                    Flags = (int)OrderNoteFlags.SentEmail,
                    OrderStatus = obj.OrderStatus,
                    AuditInfo = string.Empty,
                    Body = string.Format(Order.StringTable.TakeNoteOfflinePaymentAdmin, DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss")),
                };

                OrderNoteMgr.SaveorUpdate(new DefaultDb(), orderNote);

                return obj.InlineEdit
                    ? JsonInline(request, obj, true)
                    : RedirectToAction("Index", new { lang, mode });
            }

        LB_ERR:
            if (obj.InlineEdit)
                ModelState.TransformModelError<OrderModel>((field, dispName, modelErr) =>
                    string.Format("[{0}] {1}", dispName, modelErr.ErrorMessage));
            else
            {
                SetViewMsgInput(false);
                ViewBag.Settings = GetSettings();
                ViewBag.Countries = CountryMgr.GetAll(this);
                ViewBag.PaymentMethods = PaymentMethodMgr.GetAll(this, obj.Id, true);
                ViewBag.Shippings = ShippingMgr.GetAll(this, true);
                ViewBag.Currencies = CurrencyMgr.GetAll(this, null, true).Select(x => x.GetRaw()).ToList();
                ViewBag.Languages = LanguageMgr.GetAll();
                ViewBag.TemplOrderDetail = new OrderDetailModel()
                {
                    OrderId = obj.Id,
                    Delivered = false,
                    CouponValue = 0,
                    DiscountValue = 0
                };

                obj.InitOrderDetails(this, true);
                obj.InitOrderNotes();
                obj.InitMandatoryInfo();
            }

            return obj.InlineEdit
                ? JsonInline(request, obj, false)
                : ClawView(EditView, obj);
        }

        #endregion

        #region EditBlock

        [HttpPost]
        [ValidateInput(false)]
        //[ValidateAntiForgeryToken]
        [AdminPerm("OrderBits", PermissionMask.Edit)]
        public ActionResult EditBlock(OrderModel model, string blockType, string lang, string mode)
        {
            var obj = OrderMgr.Get(this, model == null ? 0 : model.Id);
            var logs = new List<string>();
            var logSubject = "";
            var admin = false;
            var logPrivate = true; // show Admin
            string[] fields;

            if (obj == null || model == null || blockType.IsNullOrEmpty()) goto LB_ERR;

            ModelState.Clear();

            switch (blockType)
            {
                case "customer":
                    #region Customer
                    {
                        logSubject = Order.StringTable.CustomerInfoChanged;
                        obj.OrderDetails = null;
                        obj.BackOrderDetails = null;
                        fields = new[] { "Customer.Email" };
                        var changes = GetChanges(model, obj, fields)
                            .Select(x => string.Format("[{0}: {1}]", x.DisplayName, x.Model))
                            .ToList();
                        if (changes.Count != 0) logs.Add(string.Format(Order.StringTable.BodyLogUpdatedBilling, changes.ToString(", "), ClawContext.User.Username));
                        Copy(model, obj, fields);
                    }
                    break;
                    #endregion
                case "billing":
                    #region Billing
                    {
                        logSubject = Order.StringTable.BillingInfoChanged;
                        obj.OrderDetails = null;
                        obj.BackOrderDetails = null;
                        fields = new[]
                        {
                            "BillingName", "BillingAddress", "BillingPhone", "BillingMobile", "BillingCity",
                            "BillingCountryId", "BillingPostnr", "BillingPosted", "CompanyName", "OrgNumber", "InvoiceEmail"
                        };
                        var changes = GetChanges(model, obj, fields, new Dictionary<string, string>() { { "BillingCountryId", "BillingCountryName" } })
                            .Select(x => string.Format("[{0}: {1}]", x.DisplayName, x.Model))
                            .ToList();
                        if (changes.Count != 0) logs.Add(string.Format(Order.StringTable.BodyLogUpdatedBilling, changes.ToString(", "), ClawContext.User.Username));
                        Copy(model, obj, fields);
                    }
                    break;
                    #endregion
                case "delivery":
                    #region Delivery
                    {
                        logSubject = Order.StringTable.ShippingInfoChanged;
                        obj.OrderDetails = null;
                        obj.BackOrderDetails = null;
                        fields = new[]
                        {
                            "DeliverName", "DeliverAddress", "DeliverPhone", "DeliverMobile",
                            "DeliverCountryId", "DeliverPostnr", "DeliverPosted", "DeliverCity"
                        };
                        var changes = GetChanges(model, obj, fields, new Dictionary<string, string>() { { "DeliverCountryId", "DeliverCountryName" } })
                            .Select(x => string.Format("[{0}: {1}]", x.DisplayName, x.Model))
                            .ToList();
                        if (changes.Count != 0) logs.Add(string.Format(Order.StringTable.BodyLogUpdatedDelivery, changes.ToString(", "), ClawContext.User.Username));
                        Copy(model, obj, fields);
                    }
                    break;
                    #endregion
                case "orderdetails":
                    #region OrderDetails

                    admin = true;
                    logSubject = Order.StringTable.OrderDetailChanged;
                    logPrivate = false; // Show Fe
                    obj.BackOrderDetails = null;

                    var odetailComment = Request.Form["odetailcomment"];
                    if (model.OrderDetails == null || odetailComment.IsNullOrEmpty() ||
                        model.OrderDetails.All(x => x.Id != 0)) goto LB_ERR;

                    fields = Request.Form.AllKeys.Where(x => x.StartsWith("OrderDetails")).ToArray();
                    obj.InitOrderDetails(this);
                    model.InitOrderDetails(this, true);

                    model.OrderDetails.RemoveAll(m => m.IsDataModelEmpty);

                    foreach (var itm in model.OrderDetails.Where(m => m.Id == 0))
                    {
                        var stockQty = 0;
                        var mItm = new OrderDetailModel()
                        {
                            OrderId = obj.Id,
                            ProductId = itm.ProductId,
                            VariantId = itm.VariantId,
                            Price = itm.Price,
                            PriceInc = itm.PriceInc,
                            Quantity = itm.Quantity,
                            DiscountValue = 0,
                            CouponValue = 0,
                            OrderDetailBundles = new List<OrderDetailBundleModel>(),
                            OrderDetailGifts = new List<OrderDetailGiftModel>(),
                        };

                        if (!mItm.Validate(this, out stockQty, mItm.Quantity ?? 0, true))
                            ModelState.AddModelError(string.Format("OrderDetails[{0}].Quantity", itm.Iseq),
                                string.Format(Order.StringTable.ErrOrderDetailQuantity, stockQty, itm.Quantity));

                        obj.OrderDetails.Add(mItm);
                    }

                    logs.Add(string.Format(Order.StringTable.OrderDetailAddItemLogTempl,
                        obj.OrderDetails.Where(x => x.Id == 0).Select(x => x.ProductName).ToString(", "), ClawContext.User.Username, odetailComment));

                    TryValidate(obj, true); break;

                    #endregion
                case "backorderdetails":
                    #region BackOrderDetails

                    logSubject = Order.StringTable.RegisterRestorder;
                    logPrivate = false;

                    obj.OrderDetails = null;

                    fields = Request.Form.AllKeys.Where(x => x.StartsWith("BackOrderDetails")).ToArray();
                    obj.InitBackOrderDetails();

                    //if (obj.BackOrderDetails.Any(x => x.Quantity > 0)) goto LB_ERR;

                    foreach (var itm in obj.BackOrderDetails.Join(model.BackOrderDetails,
                        o => o.OrderDetailId, m => m.OrderDetailId, (o, m) => new { O = o, M = m }))
                    {
                        if (itm.M.Quantity < 0 || itm.M.Quantity > itm.O.OriginQty)
                            ModelState.AddModelError(string.Format("BackOrderDetails[{0}].Quantity", itm.O.Iseq),
                                string.Format(StringTable.ErrMsgRangeLimit.ModErrLocalize(), "", 0, itm.O.OriginQty));
                        else itm.O.Quantity = itm.M.Quantity;
                    }

                    if (obj.BackOrderDetails.All(m => m.OriginQty == m.Quantity))
                        return JsonStatus(StringTable.Update, false, Order.StringTable.ErrMsgRestOrderOverlapOrigin);

                    obj.BackOrderDetails.RemoveAll(m => m.IsDataModelEmpty);

                    TryValidate(obj, true); break;

                    #endregion
                default:
                    goto LB_ERR;
            }

            foreach (var key in ModelState.Keys.ToList())
                if (fields.All(x => !x.EqualsIgCase(key))) ModelState.Remove(key);

            if (ModelState.ModLocalize().IsValid)
            {
                var oBackOrderIds = obj.GetRaw().GetBackOrderIds();
                if (OrderMgr.SaveorUpdate(this, obj, admin, OrderType.Web, false, false, blockType.EqualsIgCase("customer"), editCompany: blockType.InList("billing", "delivery")))
                    obj = OrderMgr.Get(this, obj.Id);
                else goto LB_ERR;

                var nBackOrderIds = obj.GetRaw().GetBackOrderIds();
                if (oBackOrderIds.Count != nBackOrderIds.Count && blockType.EqualsIgCase("backorderdetails"))
                {
                    var backOrderId = nBackOrderIds.FirstOrDefault(x => oBackOrderIds.All(w => w != x));
                    //note log in backorder 
                    OrderNoteMgr.SaveorUpdate(this, new OrderNoteModel()
                    {
                        Subject = Order.StringTable.OriginalOrder,
                        Body = string.Format(Order.StringTable.BodyLogRegisteredNewBackorderFrom, obj.Id, ClawContext.User.Username),
                        Private = logPrivate,
                        OrderStatus = (byte)OrderStatus.RestOrder,
                        OrderId = backOrderId
                    }, null, Request);

                    //note log in original order
                    logs.Add(string.Format(Order.StringTable.BodyLogRegisteredBackorder, backOrderId, ClawContext.User.Username));
                }

                if (logs.Count != 0)
                    OrderNoteMgr.SaveorUpdate(this, new OrderNoteModel()
                    {
                        Subject = logSubject,
                        Body = logs.ToString(". "),
                        Private = logPrivate,
                        OrderStatus = obj.OrderStatus,
                        OrderId = obj.Id
                    }, null, Request);

                obj.InitOrderDetails(this);
                obj.InitMandatoryInfo();

                var backOrderUrls = obj.BackOrderIds.Select(x => new { Id = x, Value = ModuleUrl.AdminUrl(Controller, "View", x) }).ToList();

                return JsonStatus(StringTable.Update, true, null, x =>
                {
                    x.Order = obj.ToExpando(true);
                    x.Order.BackOrderUrls = backOrderUrls;
                });
            }

        LB_ERR:
            var errors = ModelState
                .Where(x => x.Value.Errors.Count != 0)
                .Select(x => new { Field = x.Key, ErrorMsg = x.Value.Errors[0].ErrorMessage });

            return JsonStatus(StringTable.Update, false, null, x => x.Errors = errors);
        }

        #endregion

        #region EditOrderDetails

        [HttpPost]
        [ValidateInput(false)]
        //[ValidateAntiForgeryToken]
        [AdminPerm("OrderBits", PermissionMask.Edit)]
        public ActionResult EditOrderDetails(OrderModel model, string lang, string mode)
        {
            return EditBlock(model, "orderdetails", lang, mode);
        }

        #endregion

        #region AddEditOrderDetails

        [HttpPost]
        [ValidateInput(false)]
        //[ValidateAntiForgeryToken]
        [AdminPerm("OrderBits", PermissionMask.Edit)]
        public ActionResult AddEditOrderDetails(OrderModel model, string odetailcomment, string lang, string mode)
        {
            System.Tuple<bool, string> ret = null;

            if (model == null || model.Id == 0 || model.OrderDetails.SafeCount() == 0 ||
                model.OrderDetails.All(m => m.Id != 0 && !(m.AllowEdit ?? false))) goto LB_ERR;

            ModelState.Keys.Where(m => !m.StartsWith("OrderDetails")).ToList().Action(m => ModelState.RemoveKey(m, matchExact: true));

            for (var i = 0; i < model.OrderDetails.Count; i++)
            {
                if (model.OrderDetails[i].Id != 0 || model.OrderDetails[i].IsDataModelEmpty)
                    ModelState.RemoveKey(string.Format("OrderDetails[{0}].", i));
            }

            model.OrderDetails.Where(m => m.Id == 0 && m.IsDataModelEmpty).ToList().Action(m => model.OrderDetails.Remove(m));

            List<string> addedItems;
            List<System.Tuple<string, string, OrderDetail>> changedItems;

            if (!ModelState.ModLocalize().IsValid || !(ret = OrderMgr.AddEditDetails(this, model, out addedItems, out changedItems)).SafeProp(m => m.Item1)) goto LB_ERR;

            var obj = OrderMgr.Get(this, model.Id);

            if (addedItems.Count != 0)
                OrderNoteMgr.SaveorUpdate(this, new OrderNoteModel()
                {
                    Subject = Order.StringTable.EditOrderDetails,
                    Body = string.Format(Order.StringTable.OrderDetailAddItemLogTempl, addedItems.ToString(", "), ClawContext.User.Username, odetailcomment),
                    Private = false,
                    OrderStatus = obj.OrderStatus,
                    OrderId = obj.Id
                }, null, Request);

            changedItems.Action(m =>
                OrderNoteMgr.SaveorUpdate(this, new OrderNoteModel()
                {
                    Subject = Order.StringTable.EditOrderDetails,
                    Body = string.Format(Order.StringTable.OrderDetailEditItemLogTempl, m.Item1, m.Item3.ProductName, m.Item2, ClawContext.User.Username, odetailcomment),
                    Private = false,
                    OrderStatus = obj.OrderStatus,
                    OrderId = obj.Id
                }, null, Request));

            obj.InitOrderDetails(this);
            obj.InitMandatoryInfo();

            var backOrderUrls = obj.BackOrderIds.Select(x => new { Id = x, Value = ModuleUrl.AdminUrl(Controller, "View", x) }).ToList();

            return JsonStatus(StringTable.Update, true, null, x =>
            {
                x.Order = obj.ToExpando(true);
                x.Order.BackOrderUrls = backOrderUrls;
            });

        LB_ERR:
            var errors = ModelState
                .Where(x => x.Value.Errors.Count != 0)
                .Select(x => new { Field = x.Key, ErrorMsg = x.Value.Errors[0].ErrorMessage });

            return JsonStatus(StringTable.Update, false, ret.SafeProp(m => m.Item2), x => x.Errors = errors);
        }

        #endregion

        #region Refresh

        [HttpPost]
        [ValidateInput(false)]
        //[ValidateAntiForgeryToken]
        [AdminPerm("OrderBits", PermissionMask.Edit)]
        public ActionResult Refresh(OrderModel model, string lang, string mode,
            bool overridePayCost = false, bool overrideShipCost = false)
        {
            var offer = this is OfferController;

            if (model.OrderDetails != null)
                model.OrderDetails.RemoveAll(m => m.IsDataModelEmpty);
            else model.OrderDetails = new List<OrderDetailModel>();

            foreach (var odetail in model.OrderDetails.Where(x => x.Id == 0))
            {
                var prod = Product.DAL.ProductMgr.GetDom(this, odetail.ProductId ?? 0);
                if ((odetail.ProductId > 1 || !(odetail.Vat >= 0)) && prod.Vat != null) odetail.Vat = prod.Vat.Value;
            }

            ModelState.Clear();

            var obj = OrderMgr.Refresh(this, model, overridePayCost, overrideShipCost);
            var raw = obj == null ? null : obj.GetRaw();

            if (raw != null)
            {
                obj.ShipOrderTotal = raw.GetShipOrderTotal(obj.DeliverCountryCode);
                obj.ShipWeightTotal = raw.GetShipWeightTotal();
                obj.TotalWeight = raw.GetTotalWeight();
                obj.RawOrderTotal = raw.GetRawOrderTotal(obj.DeliverCountryCode);
                obj.CouponCodeShared = raw.GetSharedCoupon(x => x.Code);
                obj.CouponNameShared = raw.GetSharedCoupon(x => x.Name);
            }

            if (obj != null)
            {
                if (obj.LangCode.IsNullOrEmpty()) obj.LangCode = LanguageMgr.FocusLangCode;
                if (obj.Currency.IsNullOrEmpty()) obj.Currency = CurrencyMgr.FocusCurrencyCode;

                obj.ExchangeRate = CurrencyMgr.GetOrDefault(obj.Currency).Rate;

                obj.InitOrderDetails(this, false, false, copyAllowEdit: true);
                obj.InitMandatoryInfo();

                #region Valid quantity
                foreach (var itm in obj.OrderDetails)
                {
                    var stockQty = 0;
                    if (!itm.Validate(this, out stockQty, itm.Quantity ?? 0))
                        ModelState.AddModelError(string.Format("OrderDetails[{0}].Quantity", itm.Iseq),
                            string.Format(Order.StringTable.ErrOrderDetailQuantity, stockQty, itm.Quantity));
                }
                #endregion
            }

            if (offer && obj != null /*&& !(model.OfferProfitPerProd ?? false)*/)
                foreach (var odetail in obj.OrderDetails.Where(x => x.CostPrice.IsNull() && x.ProductId > 1/* && x.PriceInc > 0*/))
                {
                    var prod = Product.DAL.ProductMgr.GetDom(this, odetail.ProductId ?? 0);
                    var vari = Product.DAL.VariantMgr.GetDom(this, odetail.VariantId ?? 0);
                    var oriPrice = vari != null ? vari.Price : prod.Price;
                    odetail.CostPrice = (0.8m * oriPrice); odetail.FakeCostPrice = true; 
                }

            string errMsg = null;
            var errors = ModelState
                .Where(x => x.Value.Errors.Count != 0)
                .Select(x => new { Field = x.Key, ErrorMsg = x.Value.Errors[0].ErrorMessage });

            var settings = SettingsBiz.GetSettings();
            var updGatewayIds = (settings.Hidden.UpdatableOrderGatewayIds ?? "").Split(',', ';').Select(m => m.TryParseInt() ?? 0).Where(m => m > 0).ToArray();

            if (obj != null && (raw == null || obj.Id == 0 || ((!raw.PaymentMethod.PaymentSystemId.InList(updGatewayIds) || raw.PaymentMethod.IsOnlinePayment) && 0 == (raw.Flags & (int)Domain.Interfaces.OrderFlagType.Manual))) &&
                errMsg.IsNullOrEmpty() && obj.FinalTotalBak < obj.FinalTotal) errMsg = StringTable.OrderTotalMustNotGreaterThanReservedAmount;

            return JsonStatus(StringTable.Update, obj != null && raw != null, null, x =>
            {
                x.Order = obj.ToExpando(true);
                x.Errors = errors;
                x.Message = errMsg;
            });
        }

        #endregion

        #region GetPrefillInfo

        [ValidateInput(false)]
        [AdminPerm("OrderBits", PermissionMask.Edit)]
        public ActionResult GetPrefillInfo(string email)
        {
            var obj = OrderMgr.GetEmail(this, email);

            return JsonStatus(StringTable.Update, obj != null, null, x =>
            {
                x.Order = obj.ToExpando(true);
            });
        }

        #endregion

        #region Credit[Return]

        [ValidateInput(false)]
        [AdminPerm("OrderBits", PermissionMask.Edit)]
        public ActionResult Credit(int id)
        {
            var obj = OrderMgr.GetDom(this, id) ?? new Domain.Order();

            ViewBag.Settings = GetSettings();

            var lst = obj.GetCreditOrderDetails().Action((m, i) => m.Iseq = i);

            return ClawView("Admin/_OrdersCredit.cshtml", lst);
        }

        [HttpPost]
        [ValidateInput(false)]
        //[ValidateAntiForgeryToken]
        [AdminPerm("OrderBits", PermissionMask.Edit)]
        public ActionResult Credit(int id, List<CreditOrderDetailModel> creditOrderDetails,
           string creditComment, string lang, string mode, bool skipRefund = false)
        {
            var obj = OrderMgr.GetDom(this, id);

            if (creditOrderDetails == null || obj == null || creditComment.IsNullOrEmpty()) goto LB_ERR;

            #region CreditOrderDetails

            foreach (var itm in obj.GetCreditOrderDetails().Action((m, i) => m.Iseq = i)
                .Join(creditOrderDetails, o => o.OrderDetailId, m => m.OrderDetailId, (o, m) => new { O = o, M = m }))
            {
                itm.M.Price = itm.O.Price;
                itm.M.PriceInc = itm.O.PriceInc;

                if (itm.M.Quantity < 0 || itm.M.Quantity > itm.O.OriginQty)
                    ModelState.AddModelError(string.Format("CreditOrderDetails[{0}].Quantity", itm.O.Iseq),
                        string.Format(StringTable.ErrMsgRangeLimit.ModErrLocalize(), "", 0, itm.O.OriginQty));
            }

            foreach (var itm in creditOrderDetails
                .Select((x, idx) => new { Val = x, Index = idx })
                .Where(x => x.Val.IsDataModelEmpty).OrderByDescending(x => x.Index).ToList())
            {
                creditOrderDetails.Remove(itm.Val);
                ModelState.RemoveKey(string.Format("{0}[{{0}}]", "CreditOrderDetails"),
                    itm.Index, creditOrderDetails.Count);
            }

            #endregion

            if (ModelState.ModLocalize().IsValid)
            {
                var db = new SeparatedDb();
                var creditOrder = OrderMgr.Credit2(db, id, creditOrderDetails, creditComment, Request, skipRefund: skipRefund);

                if (creditOrder == null) goto LB_ERR;

                OrderMgr.Evict(this, id);

                creditOrder.Invoice.Data = this.RenderToString("Invoice", new { id = creditOrder.Id }, true);

                creditOrder.Invoice.Update(db.DataService);

                string errMsg = null; Domain.Order origin;
                if (obj.PaymentMethod.IsOnlinePayment)
                {
                    if (!skipRefund && Payment.Helper.PaymentHelper.Credit(db, creditOrder, origin = OrderMgr.GetDom(db, id), out errMsg))
                    {
                        var paymentData = PaymentData.Deserialize(creditOrder.PaymentData);
                        OrderNoteMgr.SaveorUpdate(this, new OrderNoteModel()
                        {
                            Subject = string.Format(Order.StringTable.OrderReturnPaymentLogTempl, paymentData.SafeProp(m => m.CreditResult), creditOrder.GetPaymentSystemName(), creditOrder.Id),
                            Body = "",
                            OrderId = origin.OriginRest == null ? origin.Id : origin.OriginRest.Id,
                            OrderStatus = origin.OriginRest == null ? origin.OrderStatus : origin.OriginRest.OrderStatus,
                            Private = false
                        }, null, Request);
                    }
                    else
                    {
                        OrderNoteMgr.SaveorUpdate(db, new OrderNoteModel()
                        {
                            Subject = string.Format(skipRefund ? Order.StringTable.OrderPaymentCreditSkipRefundLog : Order.StringTable.OrderPaymentCreditFailedLog, creditOrder.PaymentMethod.PaymentSystemName, errMsg),
                            Body = string.Empty,
                            Private = false,
                            OrderStatus = creditOrder.OrderStatus,
                            OrderId = creditOrder.Id
                        }, null, Request);
                        OrderNoteMgr.SaveorUpdate(db, new OrderNoteModel()
                        {
                            Subject = errMsg = string.Format(skipRefund ? Order.StringTable.OrderPaymentCreditSkipRefundOriginLog : Order.StringTable.OrderPaymentCreditFailedOriginLog, creditOrder.PaymentMethod.PaymentSystemName, creditOrder.Id, errMsg),
                            Body = string.Empty,
                            Private = false,
                            OrderStatus = obj.OrderStatus,
                            OrderId = obj.Id
                        }, null, Request);

                        if (skipRefund) errMsg = null;
                    }
                }

                if (errMsg.IsNullOrEmpty())
                {
                    var gcOrder = OrderMgr.GetDom(db, creditOrder.Credited ?? 0);
                    if (gcOrder.OriginRest != null) gcOrder = (Domain.Order)gcOrder.OriginRest;

                    if (gcOrder.GiftCardTotal > 0 && gcOrder.Type.InList(OrderType.Web) && !gcOrder.GiftCardData.IsNullOrEmpty())
                    {
                        var giftCardTotal = -1 * creditOrder.GiftCardTotal;
                        if (giftCardTotal > 0.01m)
                        {
                            var giftCardMgr = Singleton<DAL.Interfaces.IGiftCardMgr>.Default;
                            var tmpGc = JsonConvert.DeserializeObject<List<GiftCardOrder>>(gcOrder.GiftCardData)
                                .Select(m => new { Old = m, New = giftCardMgr.Get(m.Code, db) })
                                .Where(m => m.New != null).OrderByDescending(m => m.New.ExpiredDate).FirstOrDefault();

                            if (tmpGc != null &&
                                !giftCardMgr.Refund(db, tmpGc.New.Id, giftCardTotal, creditOrder.Id, null, Request))
                                throw new Exception(string.Format("Refund giftcard [code = {0}, amount = {1}] failed",
                                    tmpGc.New.Code, (double)giftCardTotal));
                        }
                    }

                }

                //send mail
                if (GetSettings().Hidden.SendEmail4ReturnedOrder)
                {
                    int? invoiceId, backOrderId;
                    DoUpdateStatus(creditOrder.Id, creditOrder.OrderStatus, creditComment, false, out invoiceId, out backOrderId, true, (int)OrderNoteFlags.SentEmail);
                }

                var model = OrderMgr.Get(db, id);

                model.InitMandatoryInfo(db);

                return JsonStatus(StringTable.Return, true, errMsg, x => x.Order = model.ToExpando(true), !errMsg.IsNullOrEmpty());
            }

        LB_ERR:

            return JsonStatus(StringTable.Return, false);
        }

        #endregion

        #region GetNextId
        [HttpPost]
        [AdminPerm("OrderBits", PermissionMask.View)]
        public ActionResult GetNextId(int id, byte status, bool previous = false)
        {
            var order = OrderMgr.GetNextId(this, id, status, previous);
            if (order != null)
            {
                return Json((order.HasValue ? order.Value + "" : "0"), JsonRequestBehavior.AllowGet);
            }
            return Json("0", JsonRequestBehavior.AllowGet);
        }
        #endregion

        #region UpdateTracking
        [HttpPost]
        [AdminPerm("OrderBits", PermissionMask.Edit)]
        public ActionResult UpdateTracking(int id, string trackingId)
        {
            if (id == 0 || trackingId.IsNullOrEmpty())
                return JsonStatus(Order.StringTable.UpdateOrderTrackingId, false);

            var retCode = OrderMgr.Update(this, id, m => m.ShippingTrackingId, trackingId);

            var obj = retCode ? OrderMgr.GetDom(this, id) : null;

            if (obj != null && !obj.ShippingTrackingId.IsNullOrEmpty() && GetSettings().Hidden.OrderStatusTilRegnskap && 0 == (obj.Flags & (int)Domain.Interfaces.OrderFlagType.TilRegnskap))
                if (OrderMgr.Update(this, id, m => m.Flags, obj.Flags | (int)Domain.Interfaces.OrderFlagType.TilRegnskap))
                    OrderNoteMgr.SaveorUpdate(this, new OrderNoteModel()
                    {
                        Subject = Order.StringTable.OrderTilRegnskapSet,
                        Body = "",
                        OrderId = obj.Id,
                        OrderStatus = obj.OrderStatus,
                        Private = true
                    }, null, Request);

            return JsonStatus(Order.StringTable.UpdateOrderTrackingId, retCode, null, m => m.TrackingUrl = (obj == null ? null : obj.GetShippingTrackingUrl()));
        }
        #endregion

        #region UpdateOrderDetailTags
        [HttpPost]
        [AdminPerm("OrderBits", PermissionMask.Edit)]
        public ActionResult UpdateOrderDetailTags(int id, string tags)
        {
            if (id == 0) return JsonStatus(Order.StringTable.UpdateOrderDetailTags, false);

            var retCode = BaseMgr<OrderDetailModel, OrderDetail>.Update(this, id, m => m.Tags, tags);

            return JsonStatus(Order.StringTable.UpdateOrderDetailTags, retCode);
        }
        #endregion

        #region UpdateStatus
        [HttpPost]
        [AdminPerm("OrderBits", PermissionMask.Edit)]
        // commentFlags: OrderNoteFlags
        public ActionResult UpdateStatus(int id, byte status, string comment, bool isPrivate,
            bool isSendMail = false, int commentFlags = 0, bool sepDb = true, bool rollbackQty = false, string userName = null)
        {
            int? invoiceId, backOrderId;
            if (isSendMail) commentFlags = (int)OrderNoteFlags.SentEmail;

            var retCode = DoUpdateStatus(id, status, comment, isPrivate, out invoiceId, out backOrderId,
                isSendMail, commentFlags, sepDb, rollbackQty, userName);

            var db = new SeparatedDb();
            var obj = retCode ? OrderMgr.Get(db, id) : null;

            obj.InitMandatoryInfo(db);

            return JsonStatus(Order.StringTable.UpdateOrderStatus, retCode, null, x =>
            {
                x.Order = obj.ToExpando(true);
                x.InvoiceId = invoiceId;
            });
        }
        #endregion

        #region DoUpdateStatus

        bool DoUpdateStatus(int id, byte status, string comment, bool isPrivate,
            out int? reinvoiceId, out int? rebackOrderId, bool isSendMail = false, int commentFlags = 0, bool sepDb = true, bool rollbackQty = false, string userName = null)
        {
            var db = sepDb ? (IDB)new SeparatedDb() : this;
            var type = typeof(OrderStatus);
            var changeMsg = string.Empty;
            var order = OrderMgr.GetDom(db, id);
            int? invoiceId = reinvoiceId = rebackOrderId = null;

            if (order == null || !type.IsEnumDefined(status) ||
                (order.OrderStatus != status && (order.Credited != null || status.InList(OrderStatus.Credit)))) return false;

            var retCode = BaseMgr.SaveOrUpdate(db, (sess, obj) =>
            {
                if (obj.OrderStatus == status) changeMsg = null;
                else
                {
                    if (!isPrivate) LanguageMgr.OverrideLangCode(obj.LangCode);

                    changeMsg = string.Format(Order.StringTable.OrderStatusChangeLogTemplate,
                        type.LocalizeEnumValue(status), type.LocalizeEnumValue(obj.OrderStatus));

                    if (!isPrivate) LanguageMgr.OverrideLangCode(null);

                    obj.Customer.StatsInfo = null; sess.Update(obj.Customer);
                }
                var sOutNew = status.InList(OrderStatus.Cancelled);
                var sOutOld = obj.OrderStatus.InList(OrderStatus.Cancelled);

                #region Quantity & Cancelled & dropship rollbackQty

                if (((sOutNew ^ sOutOld) || rollbackQty) && !obj.OrderStatus.InList(OrderStatus.PaymentWaiting))
                {
                    var lstOd = obj.OrderDetails.Select(x => new { x.Product, x.Variant, x.QuantityOri, x.Quantity, x.OrderDetailBundles }).ToList();

                    if (status.InList(OrderStatus.Cancelled) && obj.BackOrders.SafeCount() != 0)
                    {
                        var processor = obj.PaymentMethod.GetProcessor();
                        var canPartialCapture = processor != null && processor.CanPartialCapture && obj.ValidPaymentData(m => m.Version > 0);

                        obj.BackOrders.Cast<Domain.Order>()
                            .Where(m => canPartialCapture || !obj.PaymentMethod.IsOnlinePayment || !processor.SafeProp(x => x.CanReturn) || !m.OrderStatus.InList(OrderStatus.RestOrder))
                            .Action(m => lstOd.AddRange(m.OrderDetails.Select(x => new { x.Product, x.Variant, QuantityOri = -1 * x.QuantityOri, Quantity = -1 * x.Quantity, x.OrderDetailBundles })));

                        if (!canPartialCapture && obj.PaymentMethod.IsOnlinePayment && processor != null && processor.CanReturn)
                            obj.BackOrders.Where(m => m.OrderStatus.InList(OrderStatus.RestOrder)).Action(bo => bo.OrderStatus = status);
                    }
                    
                    lstOd.GroupBy(x => string.Format("{0}:{1}", x.Product.Id, x.Variant.GetId()), (key, lst) =>
                    {
                        var lst2 = lst.ToList();
                        return new { lst2[0].Product, lst2[0].Variant, QuantityOri = lst2.Sum(z => z.QuantityOri), Quantity = lst2.Sum(z => z.Quantity), OdBundles = lst2.SelectMany(z => z.OrderDetailBundles).ToList() };
                    })
                    .Where(x => (rollbackQty ? x.Quantity : x.QuantityOri) != 0)
                    .Action(x =>
                    {
                        Product.DAL.ProductMgr.UpdateQuantity(new DefaultDb(sess), x.Product, x.Variant, (rollbackQty ? x.Quantity : x.QuantityOri) * ((sOutNew || rollbackQty) ? 1 : -1),
                            false, false, true, false, new ProdQtyLogModel() { Reason = (int)(rollbackQty ? ProdQtyLogReason.DropShip : ProdQtyLogReason.Cancelled), RefId = obj.Id },
                            bundleData: (x.OdBundles.SafeCount() == 0 ? null : x.OdBundles.Select(m => new System.Tuple<ProductBase, VariantBase, int>(m.Product, m.Variant, m.Quantity)).ToList()));

                        x.Product.TotalSales = null;
                    });
                }

                #endregion

                #region Incompleted -> New,Processing,PayBeforeDelivery
                if (obj.OrderStatus.InList(OrderStatus.PaymentWaiting) && status.InList(OrderStatus.New, OrderStatus.Processing, OrderStatus.PayBeforeDelivery))
                {
                    obj.OrderDetails.Action(x =>
                    {
                        Product.DAL.ProductMgr.UpdateQuantity(new DefaultDb(sess), x.Product, x.Variant, -1 * x.QuantityOri,
                            false, false, true, false, new ProdQtyLogModel() { Reason = (int)ProdQtyLogReason.Ordered, RefId = obj.Id });

                        x.Product.OrderDetailNum = null;
                        x.Product.TotalSales = null;
                    });
                }
                #endregion

                #region Reset Product Statistics

                var prodODetailStatuses = new[] { OrderStatus.New, OrderStatus.RestOrder, OrderStatus.Processing, OrderStatus.PayBeforeDelivery };
                if (obj.OrderStatus.InList(prodODetailStatuses) ^ status.InList(prodODetailStatuses))
                {
                    obj.OrderDetails.Action(x => x.Product.OrderDetailNum = null);
                }

                #endregion

                if (obj.OrderStatus != status && (status == (byte)OrderStatus.Sent /*|| status == (byte)OrderStatus.Hent*/))
                {
                    #region Invoice

                    if (obj.Invoice == null)
                    {
                        var invoice = new InvoiceModel()
                        {
                            OrderId = obj.Id,
                            //Data = this.RenderToString("Invoice", new { id }, true, null, obj.LangCode)
                        };

                        try
                        {
                            invoice.Data = this.RenderToString("Invoice", new { id }, true, null, obj.LangCode);
                        }
                        catch (Exception err) 
                        {
                            LogErr(err);
                        }

                        InvoiceMgr.SaveorUpdate(db, invoice, sess);

                        obj.Invoice = InvoiceMgr.GetDom(db, invoice.Id, sess);
                    }

                    #endregion

                    #region BackOrder

                    //if (obj.BackOrder == null && obj.BackOrderDetails.Any())
                    //{
                    //    var backOrder = OrderMgr.Rest(obj);
                    //    sess.SaveOrUpdate(backOrder);
                    //    obj.BackOrder = backOrder;
                    //    OrderMgr.MakeReservationId(sess, backOrder);
                    //    changeMsg += string.Format(" [RestOrderId = {0}]", backOrderId = backOrder.Id);
                    //}

                    #endregion

                    obj.DeliveredDate = DateTime.Now;
                    var szSerialNum = StringTable.SerialNumbers;
                    List<ProdQtyLog> logs = null;
                    obj.OrderDetails.Where(m => !m.SerialNumber.IsNullOrEmpty()).ToList()
                        .Action(m =>
                        {
                            var lst = m.SerialNumber.Split(new[]{',',';'}).Select(z => z.Trim()).Where(z => !z.IsNullOrEmpty()).ToList();
                            if (m.Product.Type.InList(ProductType.Bundle))
                            {
                                m.OrderDetailBundles
                                    .Where(x => 0 != (x.Product.Flags & (int)ProductFlags.SerialNumber))
                                    .Action(x =>
                                    {
                                        x.Product.ProductSerials.RemoveAll(z => lst.Any(w => w.EqualsIgCase(z.SerialNumber)));
                                        sess.Update(x.Product);
                                    });
                            }
                            else { m.Product.ProductSerials.RemoveAll(z => lst.Any(w => w.EqualsIgCase(z.SerialNumber))); sess.Update(m.Product); }
                            
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
                                        sess.Update(tmp);
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
                                    sess.Update(tmp);
                                }
                            }
                        });
                }

                obj.ModifiedDate = obj.OrderStatus != status ? DateTime.Now : obj.ModifiedDate;
                obj.OrderStatus = status;
                invoiceId = obj.Invoice.GetId();

                return true;
            }, (sess, obj) =>
            {
                #region OrderNote

                var bodyComment = "";
                if (changeMsg != null)
                {
                    if (!isPrivate) LanguageMgr.OverrideLangCode(obj.LangCode);

                    switch (status)
                    {
                        case (int)OrderStatus.Sent:
                            if (string.IsNullOrEmpty(obj.ShippingTrackingId))
                                bodyComment = string.Format(Order.StringTable.BodyLogOrderProcessedSend(false), DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss"), userName ?? ClawContext.User.Username);
                            else bodyComment = string.Format(Order.StringTable.BodyLogOrderProcessedSend(), DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss"), userName ?? ClawContext.User.Username, obj.ShippingTrackingId);

                            bodyComment = bodyComment + " Reason: " + comment;
                            break;
                        case (int)OrderStatus.Cancelled:
                            bodyComment = string.Format(Order.StringTable.BodyLogOrderCancelled, userName ?? ClawContext.User.Username, comment);
                            break;
                        default:
                            bodyComment = comment;
                            break;
                    }

                    if (!isPrivate) LanguageMgr.OverrideLangCode(null);
                }

                if (!status.InList(OrderStatus.Credit) && !string.IsNullOrEmpty(changeMsg ?? comment))
                    return OrderNoteMgr.SaveorUpdate(this, new OrderNoteModel()
                    {
                        Subject = changeMsg ?? comment,
                        Body = changeMsg != null ? bodyComment : "",
                        OrderId = id,
                        Flags = commentFlags,
                        OrderStatus = obj.OrderStatus,
                        Private = isPrivate
                    }, sess, Request, userName);

                #endregion

                return true;
            }, order);

            if (retCode)
            {
                if (order.PaymentMethod.IsOnlinePayment && order.PaymentMethod.GetProcessor().CanPartialCapture)
                {
                    var errMsg = "";
                    Payment.Helper.PaymentHelper.Finalize(this, db, order, ref errMsg);
                    if (!errMsg.IsNullOrEmpty())
                    {
                        LogErr(new Exception(errMsg));
                        OrderNoteMgr.SaveorUpdate(db, new OrderNoteModel()
                        {
                            Subject = StringTable.PaymentReleaseRemainingAmountError,
                            Body = errMsg.ModErrLocalize().Replace("{0}", order.GetPaymentSystemName()),
                            OrderId = id,
                            OrderStatus = order.OrderStatus,
                            Private = true
                        }, null, Request, userName);
                    }
                }

                try
                {
                    if (order.OrderStatus.InList(OrderStatus.Sent))
                        Singleton<DAL.Interfaces.IMembershipMgr>.Default.FireOrderCreated(order, db);
                }
                catch (Exception err) { LogErr(err); }

                OrderMgr.Evict(this, id);
                Singleton<IOrderMgr2>.Default.ClearReportCache(this, ReportType.MonthlySales, order.CreatedDate);
            }

            if (isSendMail && retCode && !string.IsNullOrEmpty(changeMsg ?? comment))
                SendEmail(id, status, comment, changeMsg != null, status.InList(OrderStatus.Credit) ? this.RenderToString("EmailList", new { id }) : null);

            reinvoiceId = invoiceId;

            return retCode;
        }

        #endregion

        #region Send

        [HttpPost]
        [AdminPerm("OrderBits", PermissionMask.Edit)]
        public ActionResult Send(int id, string action, string message, bool force = false)
        {
            var obj = OrderMgr.GetDom(this, id);
            bool retCode, needCapture;
            string errMsg = null;
            var settings = SettingsBiz.GetSettings();

            //if (obj == null || !obj.OrderStatus.InList(OrderStatus.New, OrderStatus.Processing, OrderStatus.RestOrder))
            if (obj == null || obj.OrderStatus.InList(OrderStatus.Sent, OrderStatus.Cancelled, OrderStatus.Credit, OrderStatus.PaymentWaiting/*, OrderStatus.Hent*/))
                return JsonStatus(StringTable.UpdateStatus, false);

            #region fCancelOrder

            Func<string, ExpandoObject> fCancelOrder = (errM) =>
            {
                int? invoiceId, backOrderId;
                var tmpRet = DoUpdateStatus(id, (byte)OrderStatus.Cancelled, errM, false, out invoiceId, out backOrderId, true, (int)OrderNoteFlags.SentEmail);
                var db = new SeparatedDb();
                var order = tmpRet ? OrderMgr.Get(db, id) : null;

                order.InitMandatoryInfo(db);

                return order.ToExpando(true);
            }; 

            #endregion

            #region fSendEmail

            Func<string, bool?, bool> fSendEmail = (errM, isRecur) =>
            {
                try
                {
                    var lang = LanguageMgr.Get(obj.LangCode, this).SafeProp(m => m.ShortLangCode);
                    var query = string.Format("{0}={1}", CurrencyModel.CurCode, obj.Currency);
                    var request = new System.Web.Hosting.SimpleWorkerRequest(lang, query, new StreamWriter(new MemoryStream()));
                    System.Web.HttpContext.Current = new HttpContext(request);
                    System.Web.HttpContext.Current.Request.RequestContext.RouteData.Values.Add("lang", lang);
                    System.Web.HttpContext.Current.Request.RequestContext.RouteData.Values.Add(CurrencyModel.CurCode, obj.Currency);
                    var controller = new OrderController(DataService);
                    controller.ControllerContext = new ControllerContext(System.Web.HttpContext.Current.Request.RequestContext, controller);
                    ClawContext.Current.InitRequestData();

                    var typeSendMailHelper = Type.GetType("Claw.Modules.Eshop.Helper.SendMailHelpers, Claw.Modules.Eshop.FrontEnd");
                    if (typeSendMailHelper != null)
                    {
                        var sendMailHelper = Activator.CreateInstance(typeSendMailHelper, controller);
                        var model = OrderMgr.Get(this, obj.Id);

                        model.InitOrderDetails(this);
                        model.InitMandatoryInfo(this);

                        var emailList = sendMailHelper.SafeInvokeMethod<string>("EmailList", model, null);

                        Singleton<ISendEmailMgr>.Default.Send(controller, model.Id, (byte)OrderStatus.PaymentWaiting, errM, true, emailList, false, isRecur);

                        if (settings.Hidden.RecurOneTimeEnabled)
                        {
                            obj.Flags |= (int)(Domain.Interfaces.OrderFlagType.NotUsePaymentDeliverAddr | Domain.Interfaces.OrderFlagType.NotUsePaymentBillingAddr | Domain.Interfaces.OrderFlagType.RenewPayment);
                            obj.Update(DataService, true);

                            errMsg = (errM ?? "") + ". " + StringTable.RecurOrderFailedSendNotiOkRecurOneTime;
                        }
                        else
                        {
                            var recur = obj.Recurring;
                            while (recur != null)
                            {
                                recur.Status = (byte) RecurOrderStatus.Inactive;
                                recur.Update(DataService, true);

                                recur = recur.NextRecurOrder;
                            }

                            errMsg = (errM ?? "") + ". " + StringTable.RecurOrderFailedSendNotiOk;
                        }

                        return true;
                    }
                    else LogErr(new Exception("OrderController.Send email failed, typeSendMailHelper is null"));
                }
                catch (Exception err)
                {
                    LogErr(err);
                    errMsg = (errM ?? "") + ". " + string.Format(StringTable.RecurOrderFailedSendNotiFailed, err.Message);
                }
                return false;
            }; 

            #endregion

            switch (action ?? "")
            {
                case "check":
                    retCode = obj.CanSend(out needCapture, out errMsg);

                    var serialItems = obj.OrderDetails
                        .Where(m => m.Product.IsSerial() && settings.Hidden.ProducSerialNumber).SelectMany(m => m.GetSerialQty())
                        .Select((m, idx) => new { Id = m.Item4, Iseq = idx + 1, ProductName = m.Item1.GetName(m.Item2), Quantity = m.Item3, ProductId = m.Item1.Id, VariantId = m.Item2.GetId(), SerialNumber = m.Item5 }).ToList();

                    var bundleGroupAccs = obj.OrderDetails
                        .Where(m => m.Product.Type.InList(ProductType.Bundle) && settings.Hidden.ProdBundleGroupAccessory)
                        .SelectMany(m => m.Product.Accessories.Where(z => z.BundleGroup != null && z.AcceVariant == null && !z.BundleGroup.ShowVariant && z.Accessory.Variants.Count != 0 && m.OrderDetailBundles.Any(w => w.Product.Id == z.Accessory.Id)).Select(z => new { z.Accessory, OD = m }))
                        .Select((m, idx) => new { m.OD.Id, ProductId = m.Accessory.Id, Iseq = idx + 1, ProductName = m.Accessory.GetName(), m.OD.Quantity, Variants = m.Accessory.Variants.Where(z => !z.Deleted).Select(z => new { z.Id, Name = z.GetPropDesc() }) }).ToList();

                    return JsonStatus(StringTable.UpdateStatus, retCode, errMsg, m =>
                    {
                        m.NeedCapture = needCapture; m.SerialItems = serialItems;
                        m.SerialHtml = serialItems.Count == 0 ? null : this.RenderToString("SerialHtml", new {obj.Id});
                        m.BundleGroupVariAccs = bundleGroupAccs;
                        m.BundleGroupVariAccHtml = bundleGroupAccs.Count == 0 ? null : this.RenderToString("BundleGroupVariAccHtml", new { obj.Id });
                    });
                case "capture":
                    retCode = obj.CanSend(out needCapture, out errMsg);

                    bool emailSent = false, canCancel = false, noAttempt = false, disableCancel = false;

                    if (retCode && needCapture)
                    {
                        retCode = Payment.Helper.PaymentHelper.Capture(this, obj, out errMsg);
                        noAttempt = (errMsg ?? "").IndexOf("[NoAttempt]", StringComparison.OrdinalIgnoreCase) != -1;
                        var body = "";
                        var processor = obj.PaymentMethod.GetProcessor();
                        if (retCode)
                        {
                            if (processor.GetType() == typeof(KlarnaCheckoutProcessor))
                            {
                                var data = PaymentData.Deserialize(obj.PaymentData) as KlarnaCheckoutPaymentData;
                                body = string.Format(Order.StringTable.OrderCaptureLogKlarna, obj.TransactionId, DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss"), data.StatusCapture, data.InvoiceId);
                            }
                            else
                                body = string.Format(Order.StringTable.OrderCaptureOkLogTempl, obj.TransactionId, obj.GetPaymentSystemName());
                        }
                        else
                        {
                            if (processor.GetType() == typeof(DIBSProcessor) && ((DIBSSetting)((PaymentMethod)obj.PaymentMethod).GetSetting()).SafeProp(m => m.DIBSPlatform).InList(DIBSPlatform.Easy))
                            {
                                if ((RecurOrderMgr.FuncIsRecurOrder != null && RecurOrderMgr.FuncIsRecurOrder(obj)) || (RecurOrderMgr.FuncIsRecurOrder == null && obj.Recurring != null && obj.Recurring.Order.Id != obj.Id && obj.Recurring.Status.InList(RecurOrderStatus.Active)))
                                {
                                    if (noAttempt || SettingsBiz.GetSettings().Hidden.RecurOrderFailedSendNoti)
                                    {
                                        emailSent = fSendEmail(errMsg, true);
                                        if (settings.Hidden.RecurOneTimeEnabled && obj.IsRecurOneTime()) disableCancel = true;
                                    }
                                    else canCancel = true;
                                }
                            }

                            body = string.Format(emailSent ? ((emailSent || noAttempt) && !disableCancel ? Order.StringTable.ErrMsgOrderCaptureFailedWEmailNoti : Order.StringTable.ErrMsgOrderCaptureFailedWEmailNotiRecurOneTime) : (noAttempt ? Order.StringTable.ErrMsgOrderCaptureFailedNoAttempt : Order.StringTable.ErrMsgOrderCaptureFailed), errMsg);
                        }

                        OrderNoteMgr.SaveorUpdate(this, new OrderNoteModel()
                        {
                            Subject = Order.StringTable.CapturedPayment,
                            Body = body,
                            OrderId = id,
                            OrderStatus = obj.OrderStatus,
                            Private = true, 
                            Flags = emailSent? (int)OrderNoteFlags.SentEmail : 0
                        }, null, Request);
                    }
                    else retCode = false;

                    return JsonStatus(StringTable.UpdateStatus, retCode, errMsg,
                        x => { x.NoForceStatus = emailSent || noAttempt; x.EmailSent = emailSent; x.CanCancel = canCancel; x.Order = (emailSent || noAttempt) && !disableCancel ? fCancelOrder(null) : null; x.OrderCanceled = (emailSent || noAttempt) && !disableCancel; });

                case "status":
                    retCode = obj.CanSend(out needCapture, out errMsg);

                    if (!retCode || (!force && needCapture) || (force && message.IsNullOrEmpty()))
                        return JsonStatus(StringTable.UpdateStatus, false, errMsg);

                    //if (force)
                    //    OrderNoteMgr.SaveorUpdate(this, new OrderNoteModel()
                    //    {
                    //        Subject = StringTable.SendOrder,
                    //        Body = string.Format(StringTable.OrderCaptureFailedLogTempl, message),
                    //        OrderId = id,
                    //        OrderStatus = obj.OrderStatus,
                    //        Private = true
                    //    }, null, Request);

                    var storePickup = obj.Shipping != null && ((Domain.Shipping)obj.Shipping).ServiceProvider.InList(ShippingService.StorePickup);
                    var isSendEmail = !storePickup || !SettingsBiz.GetSettings().Hidden.NotSendEmail4StorePickupOrder;
                    return UpdateStatus(id, (byte)OrderStatus.Sent, message, false, isSendEmail, (int)OrderNoteFlags.SentEmail);

                case "emailandcancelrecur"://old, not used
                    emailSent = fSendEmail(message, null);
                    return JsonStatus(StringTable.UpdateStatus, emailSent, errMsg, x => { x.Order = emailSent ? fCancelOrder(StringTable.CancelAndDeactiveRecurOrder) : null; });   
            }

            return JsonStatus(StringTable.UpdateStatus, false);
        }

        #endregion

        #region SendOneStep

        [AdminPerm("OrderBits", PermissionMask.Edit)]
        public ActionResult SendOneStep(int id, string shippingTrackingId, bool rollbackQty = false, string userName = null)
        {
            bool needCapture;
            string errMsg;

            var obj = OrderMgr.GetDom(this, id);

            if (obj == null || obj.OrderStatus.InList(OrderStatus.Sent, OrderStatus.Cancelled, OrderStatus.Credit, OrderStatus.PaymentWaiting))
                return JsonStatus(StringTable.UpdateStatus, false);

            if (!shippingTrackingId.IsNullOrEmpty())
            {
                obj.ShippingTrackingId = shippingTrackingId;
                DataService.Update(obj);
            }

            if (!obj.CanSend(out needCapture, out errMsg)) goto LB_ERROR;

            if (needCapture)
            {
                var note = new OrderNoteModel()
                {
                    Subject = Order.StringTable.CapturedPayment,
                    OrderId = id,
                    OrderStatus = obj.OrderStatus,
                    Private = true
                };

                var retCode = Payment.Helper.PaymentHelper.Capture(this, obj, out errMsg);

                if (retCode)
                {
                    var processor = obj.PaymentMethod.GetProcessor();
                    if (processor.GetType() == typeof(KlarnaCheckoutProcessor))
                    {
                        var data = PaymentData.Deserialize(obj.PaymentData) as KlarnaCheckoutPaymentData;
                        note.Body = string.Format(Order.StringTable.OrderCaptureLogKlarna, obj.TransactionId, DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss"), data.StatusCapture, data.InvoiceId);
                    }
                    else note.Body = string.Format(Order.StringTable.OrderCaptureOkLogTempl, obj.TransactionId, obj.GetPaymentSystemName());
                }
                else note.Body = string.Format(Order.StringTable.ErrMsgOrderCaptureFailed, errMsg);

                OrderNoteMgr.SaveorUpdate(this, note, null, Request, userName);

                if (!retCode) goto LB_ERROR;
            }

            var storePickup = obj.Shipping != null && ((Domain.Shipping)obj.Shipping).ServiceProvider.InList(ShippingService.StorePickup);
            var isSendEmail = !storePickup || !SettingsBiz.GetSettings().Hidden.NotSendEmail4StorePickupOrder;

            return UpdateStatus(id, (byte)OrderStatus.Sent, null, true, isSendEmail, (int)OrderNoteFlags.SentEmail, rollbackQty: rollbackQty, userName: userName);

        LB_ERROR:
            return JsonStatus(StringTable.UpdateStatus, false, errMsg);
        }
        #endregion

        #region RecaptureRecur

        [AdminPerm("OrderBits", PermissionMask.Edit)]
        public ActionResult RecaptureRecur(int id, bool recapture, int recaptureorderid)
        {
            string errMsg;

            var obj = OrderMgr.GetDom(this, id);

            if (obj == null || !obj.OrderStatus.InList(OrderStatus.PaymentWaiting))
                return JsonStatus(Order.StringTable.RecaptureRecur, false);

            obj.Flags |= (int)OrderFlagType.RenewRecur;
            var ret = obj.UpdatePaymentData(this, true, this) && obj.Recurring?.LastRunOk == true;

            return JsonStatus(Order.StringTable.RecaptureRecur, ret, ret ? null : obj.Recurring?.LastRunMsg);
        }
        #endregion

        #region UpdateDropShip

        [HttpPost]
        [AdminPerm("OrderBits", PermissionMask.Edit)]
        public virtual ActionResult UpdateDropShip(int[] ids, int id, bool value, bool checkPostalCode = false)
        {
            const int dropship = (int)OrderFlagType.DropShipMark; 
            var errors = new List<string>();
            string errMsg = null;

            var api = checkPostalCode && value ? new BookingApi(false, BringHelpers.GetWebConfig("BringApiUid"), BringHelpers.GetWebConfig("BringApiKey"), Request.Url.AbsoluteUri) : null;

            ids = ids?.Length > 0 ? ids : new[] { id };
            var orders = new List<Domain.Order>();

            var ret = ids.Length > 0 && OrderMgr.Action(this, true, (ses, obj) =>
            {
                string erMsg = null;
                PostalCode postalItm = null;

                if (api == null || !(obj.DeliverCountry?.Code ?? "").InList("NO", "DK", "FI", "SJ")
                    || (postalItm = api.GetPostalCode(obj.DeliverCountry.Code, obj.DeliverPostnr?.Trim(), ref erMsg)).SafeProp(m => (m?.postal_code ?? "").InList(obj.DeliverPostnr?.Trim()) && (m?.city ?? "").InList(obj.DeliverPosted?.Trim())))
                {
                    var oFlags = obj.Flags;
                    obj.Flags = value ? (dropship | obj.Flags) : (~dropship & obj.Flags);
                    obj.ModifiedDate = DateTime.Now; 
                    if(oFlags != obj.Flags) orders.Add(obj);
                    return true;
                }
                else
                {
                    errors.Add((id > 0 ? "" : $"{StringTable.Order} {obj.Id} = ") + $"{obj.DeliverPostnr}-{obj.DeliverPosted}" + (postalItm == null ? "" : $" -> {postalItm.postal_code}-{postalItm.city}"));
                    errMsg = errMsg ?? erMsg;
                    return false;
                }
            }, ids);

            if (ret) orders.Action(obj => OrderNoteMgr.SaveorUpdate(this, new OrderNoteModel()
            {
                Subject = string.Format(value ? Order.StringTable.AddDropShip : Order.StringTable.RemoveDropShip),
                Body = "",
                OrderId = obj.Id,
                OrderStatus = obj.OrderStatus,
                Private = true
            }, null, Request));

            return JsonStatus("Update Drop Shipping".ModLocalize(), ret, errMsg.IfNull(ret || errors.Count == 0 ? null : string.Format("Invalid postal code: {0}".ModLocalize(), errors.ToString(", "))));
        }

        #endregion

        #region UpdateSerialNumber

        [HttpPost]
        [AdminPerm("OrderBits", PermissionMask.Edit)]
        public virtual ActionResult UpdateSerialNumber(int id, bool cpos = false)
        {
            var obj = OrderMgr.GetDom(this, id);
            var sr = new StreamReader(Request.InputStream);
            var ret = false;
            var data = sr.ReadToEnd();
            var anonymous = new[] { new { Id = 0, Items = new[] { new { Serial = "", ProductId = 0 } }.Take(0).ToArray() } };
            var items = data.IsNullOrEmpty() ? null : JsonConvert.DeserializeAnonymousType(data, anonymous);
            var needUpdateProd = cpos && obj != null && obj.OrderStatus.InList(OrderStatus.Sent) && obj.OrderDetails.All(m => m.SerialNumber.IsNullOrEmpty());

            if (items != null && obj != null)
            {
                items.Where(m => m.Id > 0 && m.Items.SafeCount() != 0).Action(m =>
                {
                    var tmp = obj.OrderDetails.FirstOrDefault(z => z.Id == m.Id);
                    if (tmp != null)
                    {
                        tmp.SerialNumber = m.Items.Select(z => z.Serial).ToString(", ");
                        if (tmp.Product.Type.InList(ProductType.Bundle))
                        {
                            tmp.OrderDetailBundles.Action(z =>
                            {
                                z.SerialNumber = m.Items.Where(w => w.ProductId == z.Product.Id).Select(w => w.Serial).ToString(", ");
                            });
                        }
                    }
                });

                var updLst = new List<object>() {obj};

                if (needUpdateProd) updLst.AddRange(OrderMgr.ProcessSerialNumber(this, obj));

                ret = BaseMgr.Update(this, updLst);
            }

            return JsonStatus(StringTable.Update, ret, null, m => m.updateProd = needUpdateProd);
        }

        #endregion

        #region UpdateConfirm

        [HttpPost]
        [AdminPerm("OrderBits", PermissionMask.Edit)]
        public virtual ActionResult UpdateConfirm(int id)
        {
            var obj = OrderMgr.GetDom(this, id);
            var sr = new StreamReader(Request.InputStream);
            var ret = false;
            var data = sr.ReadToEnd();
            var anonymous = new[] { new { Barcode = "", Count = 0, Name = "", Id = 0, ProductId = 0 } };
            var items = data.IsNullOrEmpty() ? null : JsonConvert.DeserializeAnonymousType(data, anonymous);
   
            if (items != null && obj != null)
            {
                var orderNote = new OrderNoteModel
                {
                    OrderId = obj.Id,
                    Subject = StringTable.ConfirmPackage,
                    Private = true,
                    OrderStatus = obj.OrderStatus,
                    Body = items.Select(m => $"<a class='form-link' href='{ModuleUrl.AdminUrl("Product", "Input", m.ProductId)}'>{m.Barcode}</a> ({m.Name}): {m.Count}").ToString(", "),
                };

                OrderNoteMgr.SaveorUpdate(this, orderNote, null, Request);

                obj.Flags |= (int)OrderFlagType.Confirmed;

                var updLst = new List<object>() { obj };

                ret = BaseMgr.Update(this, updLst);
            }

            return JsonStatus(StringTable.Update, ret, null);
        }

        #endregion

        #region UpdateBundleGroupVariAcc

        [HttpPost]
        [AdminPerm("OrderBits", PermissionMask.Edit)]
        public virtual ActionResult UpdateBundleGroupVariAcc(int id)
        {
            var obj = OrderMgr.GetDom(this, id);
            var sr = new StreamReader(Request.InputStream);
            var ret = false;
            var data = sr.ReadToEnd();
            var anonymous = new[] { new { Id = 0, Quantity = 0, ProductId = 0, VariantId = 0 } };
            var items = data.IsNullOrEmpty() ? null : JsonConvert.DeserializeAnonymousType(data, anonymous);

            if (items != null && obj != null)
            {
                var updLst = new List<object>() { obj };

                items.Where(m => m.Id > 0 && m.VariantId > 0).Action(m =>
                {
                    var tmp = obj.OrderDetails.FirstOrDefault(z => z.Id == m.Id);
                    if (tmp != null)
                    {
                        if (tmp.Product.Type.InList(ProductType.Bundle))
                        {
                            var lstLog = ProdQtyLogMgr.GetAll(this, id, ProdQtyLogReason.Ordered);
                            tmp.OrderDetailBundles.Where(z => z.Product.Id == m.ProductId && (z.Variant == null || z.Variant.Id != m.VariantId)).Action(z =>
                            {
                                z.Variant = Product.DAL.VariantMgr.GetDom(this, m.VariantId);

                                Product.DAL.ProductMgr.UpdateQuantity(this, z.Product, z.Variant, -1 * tmp.Quantity * z.Quantity, false, false, true, false,
                                    new ProdQtyLogModel() { Reason = (int)ProdQtyLogReason.Bundled, RefId = id, Note = string.Format("BundleId: {0}", tmp.Product.Id) }, noBundle: true);

                                z.ProductName = z.Product.GetName(z.Variant);
                                z.ProductSKU = z.Product.CombinedSKU(z.Variant);
                                z.StockQty = z.GetStockQty(tmp.QuantityOri*z.Quantity);

                                updLst.Add(tmp.Product);

                                lstLog.Where(w => w.Product.Id == m.ProductId).Action(w =>
                                {
                                    w.Variant = z.Variant; updLst.Add(w);
                                });
                            });
                        }
                    }
                });

                ret = BaseMgr.Update(this, updLst);
            }

            return JsonStatus(StringTable.Update, ret);
        }

        #endregion

        #region Cancel

        [HttpPost]
        [AdminPerm("OrderBits", PermissionMask.Edit)]
        public ActionResult Cancel(int id, string comment, bool forced = false)
        {
            string errMsg = null;
            var retCode = true;
            var canForce = true;
            var obj = OrderMgr.GetDom(this, id);

            if (obj == null || obj.PaymentMethod == null ||
                !obj.OrderStatus.InList(OrderStatus.New, OrderStatus.PaymentWaiting, OrderStatus.Processing, OrderStatus.PayBeforeDelivery, OrderStatus.RestOrder)) return JsonStatus(StringTable.Cancel, false);

            if (obj.PaymentMethod.IsOnlinePayment && obj.BackOrders.SafeCount() != 0)
            {
                var processor = obj.PaymentMethod.GetProcessor();
                if (processor.CanReturn && (!processor.CanPartialCapture || obj.ValidPaymentData(m => m.Version == 0)) &&
                    obj.BackOrders.Any(x => !x.OrderStatus.InList(OrderStatus.RestOrder, OrderStatus.Cancelled)))
                {
                    canForce = retCode = false;
                    errMsg = Order.StringTable.ErrMsgCancelOrderHavingBackOrders;
                }
                if (processor.CanPartialCapture && obj.ValidPaymentData(m => m.Version > 0) && obj.BackOrders.Any(x => x.OrderStatus.InList(OrderStatus.RestOrder)))
                {
                    canForce = retCode = false;
                    errMsg = Order.StringTable.ErrMsgCancelOrderHavingRestOrders;
                }
            }

            if (retCode && obj.OrderStatus.InList(OrderStatus.RestOrder) && obj.OriginRest.OrderStatus.InList(OrderStatus.Sent) &&
                obj.OriginRest.ValidPaymentData(m => m.Version == 0 && !m.IsCaptured))
                return JsonStatus(Core.StringTable.Cancel, false);

            while (retCode && obj.OrderStatus.InList(OrderStatus.New, OrderStatus.Processing, OrderStatus.RestOrder) && obj.PaymentMethod.IsOnlinePayment)
            {
                if (Payment.Helper.PaymentHelper.UpdatePartialCancel(this, obj)) break;

                if (obj.OrderStatus.InList(OrderStatus.RestOrder) && !obj.PaymentMethod.GetProcessor().CanReturn) break;

                retCode = obj.OrderStatus.InList(OrderStatus.New, OrderStatus.Processing) && !obj.ValidPaymentData(m => m.AutoCapture)
                    ? Payment.Helper.PaymentHelper.Cancel(this, obj, out errMsg)
                    : Payment.Helper.PaymentHelper.Credit(this, obj, obj.OriginRest ?? obj, out errMsg, obj.OriginRest == null);

                var body = retCode
                    ? string.Format(Order.StringTable.OrderCancelLogTempl, obj.TransactionId, obj.GetPaymentSystemName(), obj.Id, ClawContext.User.Username, DateTime.Now) + (errMsg.IsNullOrEmpty() ? "" : (".\r\n " + errMsg.ModErrLocalize().Replace("{0}", obj.GetPaymentSystemName())))
                    : string.Format(Order.StringTable.ErrMsgOrdercancelledFailed, errMsg);

                if (!forced || !errMsg.IsNullOrEmpty())
                    OrderNoteMgr.SaveorUpdate(this, new OrderNoteModel()
                    {
                        Subject = Order.StringTable.CancelledTransactionPayment,
                        Body = body,
                        OrderId = id,
                        OrderStatus = obj.OrderStatus,
                        Private = false
                    }, null, Request);

                if (obj.OrderStatus.InList(OrderStatus.RestOrder))
                {
                    var paymentData = PaymentData.Deserialize(obj.PaymentData);
                    OrderNoteMgr.SaveorUpdate(this, new OrderNoteModel()
                    {
                        Subject = string.Format(Order.StringTable.OrderReturnPaymentLogTempl, paymentData.SafeProp(m => m.CreditResult), obj.GetPaymentSystemName(), obj.Id),
                        Body = "",
                        OrderId = obj.OriginRest.Id,
                        OrderStatus = obj.OriginRest.OrderStatus,
                        Private = false
                    }, null, Request);
                }

                break;
            }

            if (retCode || (forced && !errMsg.IsNullOrEmpty()))
            {
                var gcOrder = (Domain.Order)obj.OriginRest ?? obj;
                if (gcOrder.GiftCardTotal > 0 && gcOrder.Type.InList(OrderType.Web) && !gcOrder.GiftCardData.IsNullOrEmpty())
                {
                    var giftCardTotal = obj.GiftCardTotal;
                    if (giftCardTotal > 0.01m)
                    {
                        var giftCardMgr = Singleton<DAL.Interfaces.IGiftCardMgr>.Default;
                        var tmpGc = JsonConvert.DeserializeObject<List<GiftCardOrder>>(gcOrder.GiftCardData)
                            .Select(m => new { Old = m, New = giftCardMgr.Get(m.Code, this) })
                            .Where(m => m.New != null).OrderByDescending(m => m.New.ExpiredDate).FirstOrDefault();

                        if (tmpGc != null &&
                            !giftCardMgr.Refund(this, tmpGc.New.Id, giftCardTotal, obj.Id, null, Request))
                            throw new Exception(string.Format("Refund giftcard [code = {0}, amount = {1}] failed",
                                tmpGc.New.Code, (double)giftCardTotal));
                    }
                }

                //return UpdateStatus(id, (byte)OrderStatus.Cancelled, string.Format(StringTable.BodyLogOrderCancelled, ClawContext.User.Username, comment), true, true);
                return UpdateStatus(id, (byte)OrderStatus.Cancelled, comment, false, GetSettings().Hidden.SendEmail4CanceledOrder, (int)OrderNoteFlags.SentEmail);
            }
            else return JsonStatus(StringTable.Cancel, false, errMsg, x =>
            {
                x.CanForce = canForce;
                x.ActionMessage = !canForce ? null : errMsg.IsNullOrEmpty() ? null : string.Format(Order.StringTable.ConfirmCancelOrderOnError, obj.GetPaymentSystemName(), errMsg);
            });
        }

        #endregion

        #region Rest
        [AdminPerm("OrderBits", PermissionMask.Edit)]
        public ActionResult Rest(int id)
        {
            var obj = OrderMgr.Get(this, id) ?? new OrderModel();

            ViewBag.Settings = GetSettings();

            obj.InitBackOrderDetails();

            return ClawView("Admin/_OrdersRest.cshtml", obj);
        }
        #endregion

        #region Delete

        [HttpPost]
        [AdminPerm("OrderBits", PermissionMask.Edit)]
        public ActionResult Delete2(GridId2[] ids)
        {
            var retCode = false;

            if (this is OfferController && ids != null && ids.All(m => m.Id > Cart.Seed))
            {
                var lst = ids.Select(x => DataService.GetObject<Cart>((int)(x.Id - Cart.Seed)))
                    .Where(m => m != null && m.ExpiredDate != null && !m.Token.IsNullOrEmpty())
                    .ToList();

                retCode = lst.Count == ids.Length && DataService.Delete(lst);
            }

            if (!IndexAction) SetViewMsg(retCode, StringTable.Delete);
            return JsonStatus(StringTable.Delete, retCode);
        }

        [HttpPost]
        [AdminPerm("OrderBits", PermissionMask.Edit)]
        public override ActionResult Delete(GridId[] ids)
        {
            var retCode = ids != null && ids.Length > 0 && BaseMgr<OrderModel, Domain.Order>.Delete(this,
                (sess, obj) => false, null, ids.Select(x => x.Id).ToArray());

            if (!IndexAction) SetViewMsg(retCode, StringTable.Delete);
            return JsonStatus(StringTable.Delete, retCode);
        }
        #endregion

        #region UploadFile

        [HttpPost]
        public ActionResult UploadImage2(int id, HttpPostedFileBase imageUpload)
        {
            if (imageUpload == null)
                return JsonUpload(new { Status = ActionResultStatus.Fail, Message = StringTable.UploadImageErrExt });

            var obj = OrderMgr.GetDom(this, id);

            if (obj == null) return JsonUpload(new { Status = ActionResultStatus.Fail, Message = StringTable.ItemNotFound });

            var fileName = "Order-" + NormalizeFileName(Path.GetFileName(imageUpload.FileName), id);

            TryCreatePath(ModuleUrl.AttachmentFolder);

            var filePath = Globals.MapPath(ModuleUrl.AttachmentPath(fileName));

            SaveFile(imageUpload, ref filePath);

            filePath = ModuleUrl.AttachmentPath(Path.GetFileName(filePath)).TrimStart('~');

            var imgs = obj.Images ?? new List<System.Tuple<string, string>>();
            var img = new System.Tuple<string, string>(Path.GetFileName(filePath), imageUpload.FileName);

            imgs.Add(img);

            obj.Images = imgs;

            var ret = OrderMgr.Update(this, new[] { obj }.ToList());

            return JsonUpload(new
            {
                Status = ret ? ActionResultStatus.Success : ActionResultStatus.Fail,
                Message = ret ? string.Empty : StringTable.SaveUnsuccess,
                Data = new { Item1 = ModuleUrl.AttachmentPath(img.Item1).ResolveClientUrl(), img.Item2 }
            });
        }

        #endregion

        #region Export

        [AdminPerm("OrderBits", PermissionMask.View)]
        public ActionResult Export(int[] ids)
        {
            var type = typeof(System.Tuple<OrderModel, OrderDetailModel, OrderDetailNilModel>);
            var list = OrderMgr.GetList(this, ids ?? new int[0])
                .Action(x => { x.InitOrderDetails(this, false, true); x.InitMandatoryInfo(); })
                .SelectMany(x =>
                {
                    var discRate = x.OrderTotal == 0 ? 0 : ((x.TotalCoupon + x.TotalDiscount) / x.OrderTotal);
                    return x.OrderDetails.Select(w =>
                    {
                        var cate = w.GetCategory();
                        var nilMod = new OrderDetailNilModel()
                        {
                            DepartmentId = cate != null && w.ProductId > 0 ? cate.Id : (int?)null,
                            DepartmentName = cate != null && w.ProductId > 0 ? cate.Name : null,
                            DiscountExp = w.ProductId > 0 ? Math.Round(discRate, 4).ToString(CultureInfo.InvariantCulture) : ""
                        };
                        return Activator.CreateInstance(type, x, w, nilMod);
                    });
                })
                .ToList();

            var propOrder = type.GetProperty("Item1");
            var propDetail = type.GetProperty("Item2");
            var propOdNil = type.GetProperty("Item3");
            var propCust = typeof(OrderModel).GetProperty("Customer");
            var param = Expression.Parameter(type, "x");
            var fieldDic = typeof(OrderModel).ToFieldDictionary(null, propOrder)
                .Concat(typeof(OrderDetailModel).ToFieldDictionary(null, propDetail))
                .Concat(typeof(OrderDetailNilModel).ToFieldDictionary(null, propOdNil))
                .Concat(typeof(CustomerModel).ToFieldDictionary(null,
                    param, Expression.Property(Expression.Property(param, propOrder), propCust)))
                .ToDictionary(x => x.Key.ToLower(), x => x.Value);

            var ret = ToExcel(list, new[]
            {
                "Item1.InvoiceId", "Item1.InvoiceDate", "Item1.DueDate", "Item1.KID", "Item1.PaymentName","Item1.FinalTotalExp", 
                "Item1.Id", "Item1.CreatedDateExp", 
                "Item1.Customer.Id", "Item1.BillingName", "Item1.BillingAddress", "Item1.BillingAddress2", "Item1.BillingPostnr", 
                "Item1.BillingPosted", "Item1.Customer.Email", "Item1.Customer.ContactFirstName", "Item1.Customer.ContactLastName", 
                "Item1.AttnFirstName", "Item1.AttnLastName", "Item1.RefNum", "Item1.DeliveredDateExp", "Item1.DeliverInfo", "Item1.Comments", 
                "Item3.SubUnitPricePeriod", "Item3.SubUnitPricePeriodUnit", "Item3.SubInvoicePeriod", "Item3.SubInvoicePeriodType", 
                "Item3.SubInvoicingPeriodCount", "Item3.SubInvoicingPeriodUnit", "Item3.SubStartDate", "Item2.Quantity", "Item2.PriceIncExp", 
                "Item3.DiscountExp", "Item2.VatType","Item3.ODetailDesc", "Item2.ProductId", "Item2.ProductName", "Item3.DepartmentId", 
                "Item3.DepartmentName", "Item3.ProjectId", "Item3.ProjectName", "Item1.Currency", 
            }, fieldDic, p => p.GetExportDisplayName());

            return File(ret, "application/vnd.ms-excel", string.Format("Invoices_{0:ddMMyyyy}.xls", DateTime.Now));
        }

        #endregion

        #region OrderDetailDelivered

        [HttpPost]
        [AdminPerm("OrderBits", PermissionMask.Edit)]
        public virtual ActionResult OrderDetailDelivered(int id, bool value)
        {
            bool ret = BaseMgr<OrderDetailModel, OrderDetail>.Update(this, id, m => m.Delivered, value);

            return JsonStatus(Order.StringTable.UpdateDelivered, ret);
        }

        #endregion

        #region ValidateCouponCode

        public ActionResult ValidateCouponCode(string couponCode, string couponCodeShared, OrderModel model)
        {
            if (model == null || model.Customer == null)
                return ValidateField("CouponCode", couponCode.IsNullOrEmpty() ? couponCodeShared : couponCode, 0);
            else
            {
                var ret = OrderModel.Validate(model, null);
                return Json(ret == null ? "true" : ret.ErrorMessage, JsonRequestBehavior.AllowGet);
            }
        }

        #endregion

        #region UpdateOffer

        [HttpPost]
        [ValidateInput(false)]
        [AdminPerm("OrderBits", PermissionMask.Edit)]
        public ActionResult UpdateOffer(long id, ODateTime expiredDate, bool? hideDiscPerProd = null)
        {
            var ret = false;
            if (this is OfferController)
            {
                var obj = OrderMgr.Get(this, id);

                if (!expiredDate.IsNull() && expiredDate >= DateTime.Now.Date && id > Cart.Seed)
                {
                    var cart = BaseMgr<Cart, Cart>.GetDom(this, (int)(id - Cart.Seed));

                    if (hideDiscPerProd != null)
                        cart.HideDiscPerProd = obj.OfferHideDiscPerProd = hideDiscPerProd.Value;

                    obj.OfferExpiredDate = expiredDate;
                    cart.ExpiredDate = expiredDate;
                    cart.Data = JsonConvert.SerializeObject(obj, OrderMgr.GetJsonSettings());
                    ret = cart.Update(DataService);
                }
            }

            return JsonStatus(Order.StringTable.Update, ret);
        }

        #endregion

        #region SendEmailToCustomer
        /// <summary>Cpos</summary>
        [HttpPost]
        [ValidateInput(false)]
        [AdminPerm("OrderBits", PermissionMask.Edit)]
        public ActionResult SendEmailToCustomer(string sendEmailSubject, string sendEmailMessage,
            string sendEmailFrom, string sendEmailTo, byte OrderStatus, long OrderId, ODateTime offerExpiredDate = null, bool? hideDiscPerProd = null)
        {
            if (this is OfferController)
            {
                var obj = OrderMgr.Get(this, OrderId);
                var emailTempl = EmailTemplMgr.GetAll(this, (int)EmailTemplateType.OrderOffer).FirstOrDefault();

                if (!offerExpiredDate.IsNull() && offerExpiredDate >= DateTime.Now.Date && OrderId > Cart.Seed)
                {
                    var cart = BaseMgr<Cart, Cart>.GetDom(this, (int)(OrderId - Cart.Seed));

                    if (hideDiscPerProd != null)
                        cart.HideDiscPerProd = obj.OfferHideDiscPerProd = hideDiscPerProd.Value;

                    obj.OfferExpiredDate = offerExpiredDate;
                    obj.OfferEmailSent = true;
                    cart.ExpiredDate = offerExpiredDate;
                    cart.Data = JsonConvert.SerializeObject(obj, OrderMgr.GetJsonSettings());
                    cart.Update(DataService);
                }

                if (emailTempl != null)
                {
                    var ntries = 1;
                LB_LOOP:
                    Domain.Order backOrder;
                    Customer customer;
                    List<RecurOrder> deletedRecurrings;
                    var couponClearLst = new List<Coupon>();
                    var discountClearLst = new List<Domain.Discount>();

                    var raw = OrderMgr.SaveorUpdate(this, obj, out customer, out backOrder, out deletedRecurrings,
                        couponClearLst, discountClearLst, true);

                    if (raw == null && obj.DiscountId != null && --ntries >= 0)
                    {
                        obj.DiscountId = null;
                        obj.DiscountValue = 0;
                        obj.FinalTotal += obj.TotalDiscount;
                        obj.TotalDiscount = 0;
                        goto LB_LOOP;
                    }

                    obj.Raw = raw;
                    obj.InitOrderDetails(this);
                    obj.InitMandatoryInfo(this);

                    var ret = Singleton<ISendEmailMgr>.Default.Send(this, obj, obj.OrderStatus, "", emailTempl, false);

                    return JsonStatus(Order.StringTable.SentEmailToCustomer, ret, (ret ? Order.StringTable.MsgOfferEmailIsSent : Order.StringTable.MsgOfferEmailIsNotSent));
                }
            }

            var settingBiz = SettingsBiz.GetSettings();
            var result = OrderId < Cart.Seed && SendEmail(sendEmailSubject, sendEmailMessage, sendEmailFrom, sendEmailTo, settingBiz.General.NotifyEmail);

            if (result)
                OrderNoteMgr.SaveorUpdate(this, new OrderNoteModel()
                {
                    Subject = Order.StringTable.SendMailToCustomerTitle,
                    Body = string.Format(Order.StringTable.BodyNoteSendEmailNotification, DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss"), ClawContext.User.Username),
                    Private = false, // Display fe
                    Flags = (int)OrderNoteFlags.SentEmail,
                    OrderStatus = OrderStatus,
                    OrderId = (int)OrderId
                }, null, Request);

            return JsonStatus(Order.StringTable.SentEmailToCustomer, result, (result ? Order.StringTable.MsgNotificationIsSent : Order.StringTable.MsgErrorNotificationIsSent));
        }
        #endregion

        #region SendOfferSms    
        public ActionResult SendOfferSms(long cartOrderId, string mobile, string smsMsg)
        {        
            var settingBiz = SettingsBiz.GetSettings();
            var order = OrderMgr.Get(this, cartOrderId);
            var data = new { Order = order, shop_url = ClawContext.Current.CurrentSite.SiteUrl, shop_name = settingBiz.General.StoreName };
            var result = order!=null&& SmsHelper.SendSMS(settingBiz.Hidden.SmsCountryCode, settingBiz.Hidden.SmsFrom, mobile, smsMsg.ReplaceEx(data), (order.CartId ?? 0).ToString());
            if (result)
            {
                var settings = SettingsBiz.GetSettings(true);
                var balance = settings.Hidden.SmsBalance - 1;
                settings.Hidden.SmsBalance = balance;
                SettingMgr.Save(settings);

                order.OfferSmsMsg = smsMsg;
                order.Comments = (order.Comments.IsNullOrEmpty() ? "" : (order.Comments + "; ")) + new ODateTime(DateTime.Now).TextF;

                var cart = BaseMgr<Cart, Cart>.GetDom(this, order.CartId ?? 0);

                cart.Data = JsonConvert.SerializeObject(order, OrderMgr.GetJsonSettings());
                cart.Update(DataService);
            }
            else LogErr(new Exception(string.Format("Send offer sms to {0} failed, Msg: {1}", mobile, smsMsg.ReplaceEx(data))));

            return JsonStatus(Order.StringTable.SentEmailToCustomer, result, (result ? Order.StringTable.MsgNotificationIsSent : Order.StringTable.MsgErrorNotificationIsSent));
        }
        #endregion

        #region SendSMSToCustomer
        [HttpPost]
        [ValidateInput(false)]
        [AdminPerm("OrderBits", PermissionMask.Edit)]
        public ActionResult SendSMSToCustomer(string sendSMSFrom, string sendSMSTo, string sendSMSMessage, byte OrderStatus, int OrderId)
        {
            var settingBiz = SettingsBiz.GetSettings();
            var result = SmsHelper.SendSMS(settingBiz.Hidden.SmsCountryCode, sendSMSFrom, sendSMSTo, sendSMSMessage, OrderId.ToString());
            if (result)
            {
                #region Update balance

                var settings = SettingsBiz.GetSettings(true);
                var balance = settings.Hidden.SmsBalance - 1;
                settings.Hidden.SmsBalance = balance;
                SettingMgr.Save(settings);

                #endregion

                var smsLog = "sms_log_note".ModLocalize("From: {0}, To: {1}: SMS text: {2}. SMS balance: {3}");
                smsLog = string.Format(smsLog, sendSMSFrom, sendSMSTo, sendSMSMessage, balance);

                OrderNoteMgr.SaveorUpdate(this, new OrderNoteModel()
                {
                    Subject = Order.StringTable.SendSMSToCustomerTitle,
                    Body = string.Format(Order.StringTable.BodyNoteSendSMSNotification, DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss"), ClawContext.User.Username, smsLog),
                    Private = false, // Display fe
                    Flags = (int)OrderNoteFlags.SentSms,
                    OrderStatus = OrderStatus,
                    OrderId = OrderId
                }, null, Request);
            }

            return JsonStatus(Order.StringTable.SentEmailToCustomer, result, (result ? Order.StringTable.MsgNotificationIsSent : Order.StringTable.MsgErrorNotificationIsSent));
        }
        #endregion

        #region OrderSmsPackage

        [HttpPost]
        [ValidateInput(false)]
        [AdminPerm("OrderBits", PermissionMask.Edit)]
        public ActionResult OrderSmsPackage(int id, int amount, string price)
        {
            if (amount > 0)
            {
                var settingBiz = SettingsBiz.GetSettings();

                #region Send email to cateno admin

                var storeName = settingBiz.General.StoreName;
                var fromEmail = settingBiz.General.PublicEmail;
                var to = settingBiz.Hidden.SmsReceiverEmail;
                var ccEmail = settingBiz.General.NotifyEmail;
                var replyEmail = ccEmail;

                var subject = ResolveOrderSmsPackage(settingBiz, settingBiz.Hidden.SmsEmailSubject, amount, price);
                var body = ResolveOrderSmsPackage(settingBiz, settingBiz.Hidden.SmsEmailBody, amount, price);

                var sendMsg = EmailSender.SendReplyTo(subject, body, true, fromEmail, to, ccEmail, "", replyEmail, storeName, "", storeName, Encoding.UTF8, Encoding.UTF8, null);

                #endregion

                // Update new balance amount
                if (string.IsNullOrEmpty(sendMsg))
                {
                    #region Update balance

                    var settings = SettingsBiz.GetSettings(true);
                    var oldBalance = settings.Hidden.SmsBalance;
                    var newAmount = oldBalance + amount;
                    settings.Hidden.SmsBalance = newAmount;

                    SettingMgr.Save(settings);

                    #endregion

                    #region log action

                    var logTemplate = StringTable.OrderSmsLogTemplate;
                    var actionLog = new ActionLog()
                    {
                        RefId = id,
                        Action = (int)LogActionType.Add,
                        Activity = string.Format(logTemplate, StringTable.OrderSmsPackage, oldBalance, amount, settings.Hidden.SmsBalance),
                        UserIp = ClawContext.UserIp,
                        UserHost = Request.Url.Host,
                        UserAgent = Request.Headers["User-Agent"],
                        UserName = ClawContext.User.Username,
                        CreatedDate = DateTime.Now
                    };

                    ClawContext.DataService.Save(actionLog);

                    #endregion

                    return Json(new { Status = 0, amount = newAmount, Message = "Sent_Order_SMS_Email_Success".ModLocalize("Sent order SMS package to Cateno successfully.") });
                }
                else
                    return Json(new { Status = 1, Message = "Error_Order_SMS".ModLocalize("Failed to order SMS package") });
            }

            return Json(new { Status = 1, Message = "Error_Order_SMS_InvalidAmount".ModLocalize("SMS amount must be a positive number") });
        }

        private string ResolveOrderSmsPackage(SettingsBiz setting, string tempalte, int amount, string price)
        {
            var ret = tempalte ?? "";
            var ctx = ClawContext;

            ret = ret.Replace("{{StoreName}}", setting.General.StoreName);
            ret = ret.Replace("{{StoreEmail}}", setting.General.NotifyEmail);
            ret = ret.Replace("{{SiteUrl}}", ctx.CurrentSite.SiteUrl);
            ret = ret.Replace("{{SiteName}}", ctx.CurrentSite.SiteName);
            ret = ret.Replace("{{Amount}}", amount.ToString());
            ret = ret.Replace("{{Price}}", price);

            return ret;
        }

        #endregion

        #region UpdateTilRegnskap

        [HttpPost]
        [ValidateInput(false)]
        [AdminPerm("OrderBits", PermissionMask.Edit)]
        public ActionResult UpdateTilRegnskap(int id)
        {
            var settings = GetSettings();
            var obj = OrderMgr.GetDom(this, id);
            var ret = true;

            if (obj == null || !settings.Hidden.OrderStatusTilRegnskap) goto LB_ERROR;

            if (0 == (obj.Flags & (int)Domain.Interfaces.OrderFlagType.TilRegnskap))
            {
                ret = OrderMgr.Update(this, id, m => m.Flags, obj.Flags | (int)Domain.Interfaces.OrderFlagType.TilRegnskap);

                if (ret) OrderNoteMgr.SaveorUpdate(this, new OrderNoteModel()
                 {
                     Subject = Order.StringTable.OrderTilRegnskapSet,
                     Body = "",
                     OrderId = obj.Id,
                     OrderStatus = obj.OrderStatus,
                     Private = true
                 }, null, Request);
            }

            return JsonStatus(Order.StringTable.OrderTilRegnskap, ret);

        LB_ERROR:
            return JsonStatus(Order.StringTable.OrderTilRegnskap, false);
        }

        #endregion

        #region UpdateFlagCustom

        [HttpPost]
        [ValidateInput(false)]
        [AdminPerm("OrderBits", PermissionMask.Edit)]
        public ActionResult UpdateFlagCustom(int id, int flag, bool value, string smsMsg = null)
        {
            var settings = GetSettings();
            var obj = OrderMgr.GetDom(this, id);
            var flagName = typeof (Domain.Interfaces.OrderFlagType).LocalizeEnumValue(flag);
            var mask = (settings.Hidden.OrderFlagCustom0 ? (int) Domain.Interfaces.OrderFlagType.Custom0 : 0) |
                       (settings.Hidden.OrderFlagCustom1 ? (int) Domain.Interfaces.OrderFlagType.Custom1 : 0);
            var ret = true;

            if (obj == null || mask == 0) goto LB_ERROR;

            if ((value ? flag : 0) != (obj.Flags & mask))
            {
                var oFlags = obj.Flags;
                ret = OrderMgr.Update(this, id, m => m.Flags, value ? (obj.Flags | (flag & mask)) : (obj.Flags & ~(flag & mask)));

                if (ret) OrderNoteMgr.SaveorUpdate(this, new OrderNoteModel()
                {
                    Subject = Order.StringTable.UpdateOrderFlagCustom(value) + " " + flagName,
                    Body = "",
                    OrderId = obj.Id,
                    OrderStatus = obj.OrderStatus,
                    Private = true
                }, null, Request);

                if (smsMsg?.Length > 0 && oFlags != obj.Flags && settings.Hidden.OrderFlagC1SendSms && 0 != ((int)OrderFlagType.Custom1 & flag))
                {               
                    //var data = new { Order = obj, shop_url = ClawContext.Current.CurrentSite.SiteUrl, shop_name = settings.General.StoreName };
                    var result = SmsHelper.SendSMSEx(true, settings.Hidden.SmsCountryCode, settings.Hidden.SmsFrom, obj.DeliverMobile.IfNull(obj.DeliverPhone), smsMsg/*.ReplaceEx(data)*/, id.ToString());
                    
                    if (result)
                    {
                        var settings2 = SettingsBiz.GetSettings(true);
                        settings2.Hidden.SmsBalance--;
                        SettingMgr.Save(settings2);
                    }
                }
            }

            return JsonStatus(Order.StringTable.Update + " " + flagName, ret);

        LB_ERROR:
            return JsonStatus(Order.StringTable.Update + " " + flagName, false);
        }

        #endregion

        #region UpdateFlagSent2PickupPoint

        [HttpPost]
        [ValidateInput(false)]
        [AdminPerm("OrderBits", PermissionMask.Edit)]
        public ActionResult UpdateFlagSent2PickupPoint(int id)
        {
            var obj = OrderMgr.GetDom(this, id);
            const int val = (int)Domain.Interfaces.OrderFlagType.Sent2PickupPoint;
            var name = typeof(Domain.Interfaces.OrderFlagType).LocalizeEnumValue(val);

            if (obj == null || !obj.OrderStatus.InList(OrderStatus.New, OrderStatus.Processing, OrderStatus.RestOrder)) goto LB_ERROR;

            obj.Flags |= val;
            obj.OrderStatus = (byte)OrderStatus.Processing;

            if (obj.Token.IsNullOrEmpty())
            {
                var random = new Random();
                obj.Token = "".PadRight(6, '0').Select(m => random.Next(0, 9).ToString("X")).ToString("").ToUpper();
            }

            var ret = OrderMgr.Update(this, new[] {obj}.ToList());

            if (ret)
            {
                SendEmail(id, obj.OrderStatus, null, true, this.RenderToString("EmailList", new { id }));

                OrderNoteMgr.SaveorUpdate(this, new OrderNoteModel()
                {
                    Subject = Order.StringTable.UpdateOrderFlagSent2PickupPoint,
                    Body = "",
                    OrderId = obj.Id,
                    OrderStatus = obj.OrderStatus,
                    Flags = (int)OrderNoteFlags.SentEmail,
                    Private = true
                }, null, Request);
            }

            return JsonStatus(Order.StringTable.Update + " " + name, ret, null, x => x.OrderStatusTxt = Order.StringTable.OrderStatusSent2PickupPoint.Replace("{0}", typeof(OrderStatus).LocalizeEnumValue(obj.OrderStatus)));

        LB_ERROR:
            return JsonStatus(Order.StringTable.Update + " " + name, false);
        }

        #endregion

        #region PrintListEDIOrProteria
        [ValidateInput(false)]
        [AdminPerm("OrderBits", PermissionMask.View)]
        public ActionResult PrintListEDIOrProteria(string type, int[] ids)
        {
            var strDateTime = DateTime.Now.ToString("ddMMyyyyHHmmss");
            var rootTem = type + "Zip";
            var rootTemPatch = ModuleUrl.TempFolder.CombineWPath(rootTem);
            var folderTem = string.Format("{0}Zip-{1}", type, strDateTime);
            var folderTemPatch = ModuleUrl.TempFolder.CombineWPath(rootTem.CombineWPath(folderTem));

            if (IOUtility.DirectoryExists(Globals.MapPath(rootTemPatch)))
                IOUtility.DeleteDirectory(Globals.MapPath(rootTemPatch), true);
            IOUtility.EnsureDirectoryExists(Globals.MapPath(rootTemPatch));

            IOUtility.EnsureDirectoryExists(Globals.MapPath(folderTemPatch));

            foreach (var id in ids)
            {
                var obj = OrderMgr.GetDom(this, id);
                var fileName = string.Format("{2}{0}-{1}.txt", obj.Id, strDateTime, type);
                var filePatch = Globals.MapPath(folderTemPatch.CombineWPath(fileName));
                var info = new FileInfo(filePatch);
                if (!info.Exists)
                {
                    using (var writer = info.CreateText())
                    {
                        writer.WriteLine(type.ToLower().Equals("edi") ? obj.ToEDI() : obj.ToProteria());
                    }
                }
            }
            var fileZip = Globals.MapPath(ModuleUrl.TempFolder.CombineWPath(rootTem) + "/" + folderTem + ".zip");

            ZipHelper.CreateZipFile(fileZip, null,
                Globals.MapPath(folderTemPatch));

            var bytes = System.IO.File.ReadAllBytes(fileZip);

            return File(bytes, "application/octet-stream", folderTem + ".zip");
        }
        #endregion

        #region SendEmail

        protected bool SendEmail(int id, byte status, string comment, bool statusChange, string prodList = null,
            bool newOrder = false)
        {

            return Singleton<ISendEmailMgr>.Default.Send(this, id, status, comment, statusChange, prodList, newOrder);
        }

        #endregion

        #region ConfirmHtml
        [AdminPerm("OrderBits", PermissionMask.View)]
        public ActionResult ConfirmHtml(int id)
        {
            var obj = OrderMgr.Get(this, id);

            if (obj == null) return HttpNotFound();

            return ClawView("Admin/_OrdersConfirm.cshtml", obj);
        }
        #endregion

        #region SerialHtml
        [AdminPerm("OrderBits", PermissionMask.View)]
        public ActionResult SerialHtml(int id)
        {
            var obj = OrderMgr.Get(this, id);

            if (obj == null) return HttpNotFound();

            return ClawView("Admin/_OrdersSerial.cshtml", obj);
        }
        #endregion

        #region BundleGroupVariAccHtml
        [AdminPerm("OrderBits", PermissionMask.View)]
        public ActionResult BundleGroupVariAccHtml(int id)
        {
            var obj = OrderMgr.Get(this, id);

            if (obj == null) return HttpNotFound();

            return ClawView("Admin/_OrdersBundleGroupVariAcc.cshtml", obj);
        }
        #endregion

        #region InvoicePdf

        [AdminPerm("OrderBits", PermissionMask.View)]
        public Stream InvoicePdf(int id, string langCode = null)
        {
            int retries = 3;
        LB_LOOP:
            try
            {
                var obj = OrderMgr.Get(this, id);
                var filename = Globals.MapPath(Core.ModuleUrl.InvoicePdfFolder.CombineWPath(string.Format("{0}.pdf", obj.SafeProp(m => m.InvoiceId))));

                if (System.IO.File.Exists(filename)) return System.IO.File.OpenRead(filename);
                else
                {
                    var doc = PrintToPdf(this.RenderToString("Invoice", new { id }, false, null, langCode));
                    var ret = doc.GetStream();

                    ret.SaveAs(filename);
                    ret.Seek(0, SeekOrigin.Begin);

                    return ret;
                }
            }
            catch (Exception e)
            {
                if (--retries >= 0)
                {
                    Thread.Sleep(1000);
                    goto LB_LOOP;
                }

                LogErr(e);
                return null;
            }
        }

        #endregion

        #region Invoice
        [AdminPerm("OrderBits", PermissionMask.View)]
        public ActionResult Invoice(int id)
        {
            var db = new SeparatedDb();
            var obj = OrderMgr.Get(db, id) ?? new OrderModel();

            obj.InitOrderDetails(db);
            obj.InitMandatoryInfo(db);
            obj.CouponCodeShared = obj.GetRaw().GetSharedCoupon(x => x.Code);
            obj.CouponNameShared = obj.GetRaw().GetSharedCoupon(x => x.Name);

            return ClawView("FrontEnd/Pdf-Invoice.cshtml", obj);
        }
        #endregion

        #region PackingSlip

        [AdminPerm("OrderBits", PermissionMask.View)]
        public ActionResult PackingSlip(int id, string type)
        {
            var obj = OrderMgr.Get(this, id) ?? new OrderModel(); ;

            ViewBag.Type = type;

            obj.InitOrderDetails(this);
            obj.InitMandatoryInfo();
            obj.CouponCodeShared = obj.GetRaw().GetSharedCoupon(x => x.Code);
            obj.CouponNameShared = obj.GetRaw().GetSharedCoupon(x => x.Name);

            return ClawView("FrontEnd/Pdf-PackingSlip.cshtml", obj);
        }

        #endregion

        #region Delivery

        [AdminPerm("OrderBits", PermissionMask.View)]
        public ActionResult Delivery(int id)
        {
            var obj = OrderMgr.Get(this, id) ?? new OrderModel();

            obj.InitOrderDetails(this);
            obj.InitMandatoryInfo();
            obj.CouponCodeShared = obj.GetRaw().GetSharedCoupon(x => x.Code);
            obj.CouponNameShared = obj.GetRaw().GetSharedCoupon(x => x.Name);

            return ClawView("FrontEnd/Pdf-Delivery.cshtml", obj);
        }

        #endregion

        #region ListShipping

        //public ActionResult ListShipping(decimal orderTotal, decimal weightTotal,
        //    int? countryid = null, int? cityid = null, int? districtid = null)
        //{
        //    var lst = ShippingMgr.ListAppliable(this, orderTotal, weightTotal, countryid, cityid, districtid);

        //    return Json(lst, JsonRequestBehavior.AllowGet);
        //}

        #endregion

        #region ListPickupPointD2

        public ActionResult ListPickupPointD2(int id, string postalCode, string carrier = null)
        {
            var obj = postalCode.IsNullOrEmpty() ? null : OrderMgr.GetDom(this, id);

            if (obj == null || obj.ShippingService == null) return Json(null, JsonRequestBehavior.AllowGet);

            var lst = new DDListItem[0];

            switch ((ShippingServiceType)obj.ShippingService.Value)
            {
                case ShippingServiceType.Logistra:
                {
                    carrier = carrier.IfNull(obj.GetShippingInfo().SafeProp(m => m.Item1));
                    lst = LogistraMgr.GetServicePartners(obj.DeliverCountry.Code, postalCode, carrier ?? "tollpost_globe")
                            .Select(m => new DDListItem()
                            {
                                Text =
                                    string.Format("{0}, {1}, {3} {4}, {5} ", m.Name, m.Address1, m.Address2, m.City,
                                        m.Postcode, m.Country),
                                Value = m.Number,
                                Model = m,
                                Active = true,
                                LangVisible = true
                            }).ToArray();
                    break;
                }
                case ShippingServiceType.Bring:
                    lst = BringShippingMgr.GetPickupPoints(obj.DeliverCountry.Code, postalCode).Select(m => new DDListItem()
                    {
                        Text = string.Format("{0}, {1}, {3} {4}, {5} ", m.Name, m.Address, null, m.City, m.PostalCode, m.CountryCode),
                        Value = m.Id,
                        Model = m,
                        Active = true,
                        LangVisible = true
                    }).ToArray();
                    break;
                case ShippingServiceType.Postnord:
                    lst = PostnordMgr.GetPickupPoints(obj.DeliverCountry.Code, postalCode).Where(m => m.deliveryAddress != null).Select(m => new DDListItem()
                    {
                        Text = string.Format("{0}, {1} {2}, {3} {4}, {5} ", m.name, m.deliveryAddress.streetName, m.deliveryAddress.streetNumber, m.deliveryAddress.city, m.deliveryAddress.postalCode, m.deliveryAddress.countryCode),
                        Value = m.servicePointId,
                        Model = m,
                        Active = true,
                        LangVisible = true
                    }).ToArray();
                    break;
            }

            return Json(lst, JsonRequestBehavior.AllowGet);
        }

        #endregion

        #region UpdatePickupPoint

        [HttpPost]
        [ValidateInput(false)]
        [AdminPerm("OrderBits", PermissionMask.Edit)]
        public ActionResult UpdatePickupPoint(int id, string data)
        {
            var obj = OrderMgr.GetDom(this, id);

            if (obj == null || obj.ShippingService == null || data.IsNullOrEmpty()) goto LB_ERROR;

            var oldData = obj.ShippingPickupPoint.FormatShippingPickupPoint((ShippingServiceType)obj.ShippingService);
            var ret = OrderMgr.Update(this, id, m => m.ShippingPickupPoint, data);

            if (ret) OrderNoteMgr.SaveorUpdate(this, new OrderNoteModel()
            {
                Subject = Order.StringTable.UpdatePickupPoint,
                Body = string.Format("Former value: {0}", oldData),
                OrderId = obj.Id,
                OrderStatus = obj.OrderStatus,
                Private = true
            }, null, Request);

            var recurring = obj.Recurring;
            while (ret && recurring != null)
            {
                RecurOrderLogMgr.SaveorUpdate(this, new RecurOrderLogModel()
                {
                    Content = string.Format("Updated '{0}', former value: '{1}'", "PickupPoint", oldData),
                    RecurringId = recurring.Id,
                }, null, Request);
                recurring = recurring.NextRecurOrder;
            }

            return JsonStatus(Order.StringTable.UpdatePickupPoint, ret);

        LB_ERROR:
            return JsonStatus(Order.StringTable.UpdatePickupPoint, false);
        }

        #endregion

        #region Quick detail view

        [HttpPost]
        [AdminPerm("OrderBits", PermissionMask.View)]
        public ActionResult QuickView(long id)
        {
            var cart = this is CartController;
            var obj = OrderMgr.Get(this, id);

            ViewBag.Settings = GetSettings();
            ViewBag.Countries = CountryMgr.GetAll(this);
            ViewBag.PaymentMethods = PaymentMethodMgr.GetAll(this, obj == null ? 0 : obj.Id, true);
            ViewBag.Shippings = ShippingMgr.GetAll(this, true);
            ViewBag.Currencies = CurrencyMgr.GetAll(this, null, true).Select(x => x.GetRaw()).ToList();
            ViewBag.Languages = LanguageMgr.GetAll();

            if (obj == null)
                obj = new OrderModel()
                {
                    Customer = new CustomerModel() { Individual = true },
                    OrderStatus = (byte)OrderStatus.New,
                    Currency = CurrencyMgr.Default.Code,
                    LangCode = LanguageMgr.FocusLangCode,
                    BillingCountryId = 1,
                    DeliverCountryId = 1,
                };
            else
            {
                var raw = obj.GetRaw();
                obj.Currency = obj.Currency.IsNullOrEmpty() ? CurrencyMgr.Default.Code : obj.Currency;
                obj.LangCode = obj.LangCode.IsNullOrEmpty() ? LanguageMgr.FocusLangCode : obj.LangCode;
                obj.ShipOrderTotal = raw.GetShipOrderTotal(obj.DeliverCountryCode);
                obj.ShipWeightTotal = raw.GetShipWeightTotal();
                obj.OriginalCredited = raw.GetOriginCreditId();
                obj.CouponCodeShared = obj.GetRaw().GetSharedCoupon(x => x.Code);
                obj.CouponNameShared = obj.GetRaw().GetSharedCoupon(x => x.Name);

                if (raw.Recurring != null &&
                    raw.Recurring.Status.InList(RecurOrderStatus.Pending)) obj.Recurring = null;
            }

            if (cart)
            {
                obj.Id = obj.CartId ?? 0;
                obj.TotalWeight = obj.GetTotalWeight(this);
                obj.OrderNotes = new List<OrderNoteModel>();
                obj.PaymentLogs = new List<PaymentLogModel>();
            }
            else
            {
                obj.InitMandatoryInfo(this);
                obj.InitOrderDetails(this);
                obj.InitOrderNotes();
                obj.InitPaymentLogs();
            }

            obj.ItemTotal = obj.OrderDetails.Count;

            ViewBag.TemplOrderDetail = new OrderDetailModel()
            {
                OrderId = obj.Id,
                Delivered = false,
                CouponValue = 0,
                DiscountValue = 0
            };

            return View("~/Claw/Sites/Default/Modules/EShop/Admin/_QuickOrderDetail.cshtml", obj);
        }

        #endregion
    }

    [AdminPerm("OrderBits", byte.MaxValue)]
    public class CartController : OrderController
    {
        public CartController(IDataService ds) : base(ds) { }
    }

    [AdminPerm("OrderBits", byte.MaxValue)]
    public class OfferController : OrderController
    {
        public OfferController(IDataService ds) : base(ds) { }
    }
}

#region WebJobOrderController

namespace Claw.Modules.Eshop.Controllers.FrontEnd
{
    using Admin;

    public class WebJobOrderController : BaseController<OrderModel, Domain.Order>
    {
        public static string SecurityCode = Guid.NewGuid().ToString();

        public WebJobOrderController(IDataService ds) : base(ds) { }

        public ActionResult SendEmail(int id, string securityCode)
        {
            if (!SecurityCode.EqualsIgCase(securityCode)) goto LB_ERR;

            try
            {
                var dom = OrderMgr.GetDom(this, id);
                if (dom != null && !dom.LangCode.IsNullOrEmpty())
                    LanguageMgr.OverrideLangCode(dom.LangCode);

                var obj = OrderMgr.Get(this, id);

                if (obj == null) goto LB_ERR;

                obj.InitOrderDetails(this);
                obj.InitMandatoryInfo();

                var ret = Singleton<ISendEmailMgr>.Default.Send(this, obj, (byte)OrderStatus.New, obj.Comments,
                    obj.GetRaw().PaymentMethod.IsOnlinePayment);

                LanguageMgr.OverrideLangCode(null);

                return new HttpStatusCodeResult(HttpStatusCode.OK, ret.ToString());
            }
            catch (Exception err)
            {
                return new HttpStatusCodeResult(HttpStatusCode.InternalServerError, err.Message);
            }

        LB_ERR:
            return new HttpStatusCodeResult(HttpStatusCode.NonAuthoritativeInformation);
        }
    }
}

#endregion