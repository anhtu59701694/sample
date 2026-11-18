using Claw.Modules.Eshop.DAL;
using Claw.Modules.Eshop.Domain.Interfaces;
using Claw.Modules.Eshop.Helper;
using Claw.Modules.Eshop.Models;
using Newtonsoft.Json;
using Ninject.Activation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using BasePayment = Eshop.Payments.BasePayment;

namespace Claw.Modules.Eshop.Domain
{
    public enum PaymentRetryStageType { Done = 0, Pending = 1 }
    public partial class Order : Tuple<OrderDetail, OrderNote, PaymentLog, BackOrderDetail, IOrder, Order, OrderDetailTag>, IOrder
    {
        public virtual int Id { get; set; }
        public virtual Company Company { get; set; }
        public virtual Customer Customer { get; set; }
        public virtual IPaymentMethod PaymentMethod { get; set; }
        public virtual Discount Discount { get; set; }
        public virtual Discount Discount2 { get; set; }
        public virtual Coupon Coupon { get; set; }
        public virtual IOrder OriginRest { get; set; }
        public virtual RecurOrder Recurring { get; set; }
        public virtual string OrgNumber { get; set; }
        public virtual string CompanyName { get; set; }
        public virtual string InvoiceEmail { get; set; }
        public virtual string DeliverName { get; set; }
        public virtual string DeliverPhone { get; set; }
        public virtual string DeliverMobile { get; set; }
        public virtual string DeliverAddress { get; set; }
        public virtual string DeliverPostnr { get; set; }
        public virtual string DeliverPosted { get; set; }
        public virtual string DeliverCity { get; set; }
        public virtual Country DeliverCountry { get; set; }
        public virtual string BillingName { get; set; }
        public virtual string BillingPhone { get; set; }
        public virtual string BillingMobile { get; set; }
        public virtual string BillingAddress { get; set; }
        public virtual string BillingPostnr { get; set; }
        public virtual string BillingPosted { get; set; }
        public virtual string BillingCity { get; set; }
        public virtual Country BillingCountry { get; set; }
        public virtual decimal? PaymentCostAdm { get; set; }
        public virtual decimal? PaymentCost { get; set; }
        public virtual decimal? PaymentVat { get; set; }
        public virtual string PaymentName { get; set; }
        public virtual int SplitPayment { get; set; }
        public virtual string SplitPayData { get; set; }
        public virtual decimal SplitCashTotal { get; set; }
        public virtual decimal SplitCardTotal { get; set; }
        public virtual decimal SplitGiftTotal { get; set; }

        public virtual string AuthorizationId { get; set; }
        public virtual string TransactionId { get; set; }
        public virtual string PaymentData { get; set; }
        public virtual string PaymentResultId { get; set; }
        public virtual string PaymentCardMaskedPan { get; set; }
        public virtual IShipping Shipping { get; set; }
        public virtual string ShippingName { get; set; }
        public virtual decimal? ShippingCostAdm { get; set; }
        public virtual decimal? ShippingCost { get; set; }
        public virtual decimal? ShippingVat { get; set; }
        public virtual string ShippingTrackingId { get; set; }
        public virtual string ReservationId { get; set; }
        public virtual decimal? DiscountValue { get; set; }
        public virtual decimal DiscountValue2 { get; set; }
        public virtual string DiscountName { get; set; }
        public virtual decimal? CouponValue { get; set; }
        public virtual string CouponCode { get; set; }
        //public virtual decimal GiftCardTotal { get; set; }
        public virtual byte OrderStatus { get; set; }
        public virtual byte Type { get; set; }
        public virtual string LangCode { get; set; }
        public virtual string Currency { get; set; }
        public virtual decimal? ExchangeRate { get; set; }
        public virtual bool VatEnabled { get; set; }

        public virtual decimal OrderTotalRes { get; set; }
        public virtual decimal VatTotalRes { get; set; }

        /// <summary>OrderTotalAdmOri - OrderTotalRes</summary>
        public virtual decimal OrderTotalAdm { get { return OrderTotalAdmOri - OrderTotalRes; } protected set { } }

        /// <summary>VatTotalAdmOri - VatTotalRes</summary>
        public virtual decimal VatTotalAdm { get { return VatTotalAdmOri - VatTotalRes; } protected set { } }

        public virtual decimal OrderTotalAdmOri { get; set; }
        public virtual decimal VatTotalAdmOri { get; set; }

        public virtual decimal TotalCouponAdm { get; set; }
        public virtual decimal TotalDiscountAdm { get; set; }
        public virtual decimal? FinalTotalAdm { get; set; }

        public virtual decimal OrderTotalEx { get; set; }

        /// <summary>OrderTotalOri - OrderTotalRes</summary>
        public virtual decimal OrderTotal { get { return OrderTotalOri - OrderTotalRes; } protected set { } }

        /// <summary>VatTotalOri - VatTotalRes</summary>
        public virtual decimal VatTotal { get { return VatTotalOri - VatTotalRes; } protected set { } }

        public virtual decimal OrderTotalOri { get; set; }
        public virtual decimal VatTotalOri { get; set; }

        /// <summary>backup of OrderTotalOri before manual add</summary>
        public virtual decimal? OrderTotalBak { get; set; }
        public virtual decimal? VatTotalBak { get; set; }
        public virtual decimal? FinalTotalBak { get; set; }

        /// <summary>refresh</summary>
        public virtual decimal? GetFinalTotal(decimal? orderTotalOri)
        {
            return (orderTotalOri ?? OrderTotalOri) - OrderTotalRes - TotalDiscount - TotalCoupon - GiftCardTotal + DepositTotal + (OrderStatus == 5 ? 0 : PaymentCost + ShippingCost); 
        }

        public virtual decimal? GetFinalTotal()
        {
            return GetFinalTotal(null);
        }

        public virtual decimal TotalCouponRes { get; set; }
        public virtual decimal TotalDiscountRes { get; set; }

        public virtual decimal TotalCouponOri { get; set; }
        public virtual decimal TotalDiscountOri { get; set; }

        public virtual decimal GiftCardTotalPos { get; set; }
        public virtual decimal GiftCardTotal { get; set; }
        public virtual string GiftCardData { get; set; }
        public virtual decimal GiftCardSales { get; set; }
        public virtual decimal DepositTotalOri { get; set; }
        public virtual decimal DepositTotalRes { get; set; }

        /// <summary>DepositTotalOri - DepositTotalRes</summary>
        public virtual decimal DepositTotal { get { return DepositTotalOri - DepositTotalRes; } protected set { } }

        /// <summary>TotalCouponOri - TotalCouponRes</summary>
        public virtual decimal TotalCoupon { get { return TotalCouponOri - TotalCouponRes; } protected set { } }

        /// <summary>TotalDiscountOri - TotalDiscountRes</summary>
        public virtual decimal TotalDiscount { get { return TotalDiscountOri - TotalDiscountRes; } protected set { } }

        public virtual decimal ItemDiscountTotal { get; set; }

        public virtual decimal? FinalTotal { get; set; }
        public virtual DateTime CreatedDate { get; set; }
        public virtual DateTime ModifiedDate { get; set; }
        public virtual DateTime? DeliveredDate { get; set; }
        public virtual DateTime? ReportDate { get; set; }
        public virtual DateTime? ExpectedDelivery { get; set; }
        public virtual bool? IsFreeShipping { get; set; }
        public virtual string Comments { get; set; }
        public virtual string UserIp { get; set; }
        public virtual string UserHost { get; set; }
        public virtual string UserAgent { get; set; }
        public virtual Invoice Invoice { get; set; }
        public virtual int? Credited { get; set; }
        public virtual string ExtraInfo1 { get; set; }
        public virtual string ExtraInfo2 { get; set; }
        public virtual string ExtraInfo3 { get; set; }
        public virtual string ExtraInfo4 { get; set; }
        public virtual string Token { get; set; }
        public virtual int? PaymentRetryCount { get; set; }
        public virtual int? PaymentRetryStage { get; set; }
        public virtual DateTime? PaymentRetryTimestamp { get; set; }

        /// <summary>Temp fields</summary>
        public virtual bool? OfferHideDiscPerProd { get; set; }
        /// <summary>Temp fields</summary>
        public virtual DateTime? OfferExpiredDate { get; set; }
        /// <summary>Temp fields</summary>
        public virtual string OfferToken { get; set; }
        /// <summary>Temp fields</summary>
        public virtual string OfferMsg { get; set; }
        /// <summary>Temp fields</summary>
        public virtual string OfferSmsMsg { get; set; }
        /// <summary>Temp fields</summary>
        public virtual bool? OfferSendSms { get; set; }

        /// <summary>Temp fields</summary>
        public virtual bool? OfferEmailSent { get; set; }
        /// <summary>Temp fields</summary>
        public virtual decimal? CustomDiscountRate { get; set; }
        /// <summary>Temp fields</summary>    
        public virtual bool? OfferProfitPerProd { get; set; }
        /// <summary>Temp fields</summary>
        public virtual bool? RecurringTokenOnly { get; set; }
        /// <summary>Temp fields</summary>
        public virtual string RecurringReturnUrl { get; set; }

        public virtual bool IsRecurringTokenOnly() 
        {
            return RecurringTokenOnly == true || IsRenewRecur();
        }
        public virtual bool IsRenewRecur()
        {
            return 0 != (Flags & (int)OrderFlagType.RenewRecur);
        }

        /// <summary>
        /// Item1: url, Item2: title
        /// </summary>
        public virtual List<System.Tuple<string, string>> Images
        {
            get { return ExtraInfo2.IsNullOrEmpty() ? null : JsonConvert.DeserializeObject<List<System.Tuple<string, string>>>(ExtraInfo2); }
            set { ExtraInfo2 = (value.SafeCount() == 0 ? null : JsonConvert.SerializeObject(value)); }
        }
        
        public virtual int Flags
        {
            get { return ExtraInfo3.IsNullOrEmpty() ? 0 : int.Parse(ExtraInfo3); }
            set { ExtraInfo3 = (value == 0 ? null : value.ToString()); }
        }

        public virtual int FlagsEx { get; set; }

        /// <summary>credit orderid of pos change order</summary>
        public virtual int? ChangedCreditId
        {
            get { return ExtraInfo4.IsNullOrEmpty() ? (int?)null : int.Parse(ExtraInfo4); } 
            set { ExtraInfo4 = (value == null ? null : value.ToString()); }
        }

        public virtual int? PackerId { get; set; }
        public virtual string PackerName { get; set; }
        public virtual DateTime? PackingStart { get; set; }
        public virtual DateTime? PackingEnd { get; set; }
        public virtual Guid? PowerOfficeId { get; set; }
        public virtual string PaidByType { get; set; }

        public virtual string ShippingPickupPoint { get; set; }
        public virtual int? ShippingService { get; set; }
        public virtual string ShippingData { get; set; }

        public virtual string BringBookingData { get; set; }
        public virtual string BringMailBoxData { get; set; }
        public virtual int BringBookingStatus { get; set; }

        public virtual int? PackingService { get; set; }
        public virtual string PackingData { get; set; }

        public virtual int? CartId { get; set; }

        public virtual int? UserId { get; set; }
        public virtual double? Cash { get; set; }
        public virtual int? CashRegisterNumber { get; set; }
        public virtual double? RoundingUp { get; set; }
        public virtual bool? ZReport { get; set; }
        public virtual bool XReport { get; set; }

        public virtual IList<OrderDetail> OrderDetails
        {
            get { return Get(ref _list0); }
            set { _list0 = value; }
        }

        public virtual IList<OrderNote> OrderNotes
        {
            get { return Get(ref _list1); }
            set { _list1 = value; }
        }

        public virtual IList<PaymentLog> PaymentLogs
        {
            get { return Get(ref _list2); }
            set { _list2 = value; }
        }

        public virtual IList<BackOrderDetail> BackOrderDetails
        {
            get { return Get(ref _list3); }
            set { _list3 = value; }
        }

        public virtual IList<IOrder> BackOrders
        {
            get { return Get(ref _list4); }
            set { _list4 = value; }
        }

        public virtual IList<Order> OriginCredits
        {
            get { return Get(ref _list5); }
            set { _list5 = value; }
        }

        public virtual IList<OrderDetailTag> OrderDetailTags
        {
            get { return Get(ref _list6); }
            set { _list6 = value; }
        }

        public virtual byte ExtraService { get; set; }
        public virtual string BringInfo { get; set; }

        public virtual BasePayment.Order ToPaymentOrder(bool? giftcard = null)
        {
            return DomExtensions.ToPaymentOrder(this, giftcard);
        }

        public virtual List<BasePayment.OrderDetail> ToPaymentOrderDetails(IOrderDetail odetail = null, bool? giftcard = null)
        {
            return DomExtensions.ToPaymentOrderDetails(this, (OrderDetail)odetail, giftcard);
        }

        public virtual List<BasePayment.OrderDetail> ToPaymentOrderDetailsIncManual()
        {
            return DomExtensions.ToPaymentOrderDetails(this, null, null, true);
        }

        public virtual bool CompletePayment(IDB db)
        {
            return OrderMgr.CompletePayment(db, this);
        }

        public virtual string GetPaymentSystemName()
        {
            return DomExtensions.GetPaymentSystemName(this);
        }

        public virtual void HandleNewEvent(IDB db, BasePayment.PaymentData data, bool sendMailYet)
        {
            DomExtensions.HandleNewEvent(this, db, data, sendMailYet);
        }

        public virtual BasePayment.PaymentData GetPaymentData(bool resolveType = true)
        {
            return DomExtensions.GetPaymentData(this, resolveType);
        }

        public virtual long GetCartOrderId()
        {
            return (CartId > 0 ? Cart.Seed + CartId : Id) ?? 0;
        }

        public virtual bool CommitCart(IDB db, string email = null)
        {
            if ((CartId ?? 0) == 0 || Id != 0) return false;

            var model = OrderMgr.Get(db, Cart.Seed + CartId ?? 0);

            if (!email.IsNullOrEmpty() && model.Customer.Email.IsNullOrEmpty()) 
                model.Customer.Email = email;

            model.DeliverName = model.DeliverName ?? "";
            model.DeliverAddress = model.DeliverAddress ?? "";
            model.DeliverPostnr = model.DeliverPostnr ?? "";

            model.BillingName = model.BillingName ?? "";
            model.BillingAddress = model.BillingAddress ?? "";
            model.BillingPostnr = model.BillingPostnr ?? "";

            if (OrderMgr.SaveorUpdate(db, model, false, OrderType.Web, false, false, false, true))
            {
                Id = Id == 0 ? model.Id : Id;
                var dom = OrderMgr.GetDom(db, Id);
                if (dom != null && PaymentData?.Length > 0)
                {
                    dom.PaymentData = PaymentData;
                    dom.Update(db.DataService);
                }
                return true;
            }
            else return false;
        }

        public virtual void MakeOrderDetailSourceId()
        {
            OrderDetails.ToList().Action((od, iseq) => od.SourceId = CartId > 0 ? ((CartId%1000000)*100 + iseq) : od.SourceId);
        }

        public virtual bool IsMoneyReturnable()
        {
            return DomExtensions.IsMoneyReturnable(this);
        }

        ICustomer IOrder.Customer { get { return Customer; } }

        public virtual bool ValidPaymentData(Predicate<BasePayment.PaymentData> predicate)
        {
            return DomExtensions.ValidPaymentData(this, predicate);
        }

        public virtual bool UpdatePaymentData(IDB db, bool completed = false, Controllers.Admin.BaseController controller = null)
        {
            if (Id != 0)
            {
                var lst = new List<object>() { this };
                var lstActs = new List<Action>();

                if (completed && IsRenewRecur())
                {
                    Flags &= ~(int)OrderFlagType.RenewRecur;

                    BaseMgr.Update(db, lst);

                    var settings = SettingsBiz.GetSettings();
                    var reactive = QParam.GetField<bool>("reactive", false, true);
                    var resetLastRun = QParam.GetField<bool>("resetlastrun", false, true);
                    var recapture = QParam.GetField<bool>("recapture", false, true);
                    var recaptureOrderId = QParam.GetField<int?>("recaptureorderid", false, true);
                    var manual = controller?.Request?.RawUrl?.IndexOf("/admin/modules/eshop/order/RecaptureRecur", StringComparison.OrdinalIgnoreCase) >= 0;

                    Recurring.ToList()
                        .Where(m => m.LastRunOk == false && (((recaptureOrderId ?? 0) != m.LastRunOrderId && m.LastRunTime != null && (DateTime.Now - m.LastRunTime.Value).TotalDays < settings.Hidden.PaymentUpdateRecurOrderFailedRetryMaxDays) || recaptureOrderId == m.LastRunOrderId))
                        .Action(m =>
                    {
                        if (m.LastRunOrderId != null && controller != null && Settings.Hidden.DibsEasyRecurAutoCapture && m.Order.PaymentMethod.PaymentSystemId.InList(GatewayID.DIBS)) 
                        {
                            var errMsg = "";
                            var lastRunOrder = db.DataService.GetObject<Order>(m.LastRunOrderId);
                            if (lastRunOrder != null && lastRunOrder.OrderStatus.InList(Models.OrderStatus.PaymentWaiting))
                            {
                                if (!manual && settings.Hidden.PaymentUpdateRecurOrderFailedRetryDelayHours > 0 && settings.Hidden.PaymentRetryCount > 0)
                                {
                                    lastRunOrder.PaymentRetryCount = settings.Hidden.PaymentRetryCount;
                                    lastRunOrder.PaymentRetryStage = (int)PaymentRetryStageType.Done;
                                    lastRunOrder.PaymentRetryTimestamp = DateTime.Now.AddHours(-1 * settings.Hidden.PaymentRetryHours + settings.Hidden.PaymentUpdateRecurOrderFailedRetryDelayHours);
                                    lst.Add(lastRunOrder);
                                }
                                else
                                {
                                    try
                                    {
                                        BasePayment.PaymentData data = null;
                                        var log = new OrderNoteModel() { Subject = StringTable.CapturedPayment, OrderStatus = lastRunOrder.OrderStatus, OrderId = lastRunOrder.Id, Private = true };

                                        if (Payment.Helper.PaymentHelper.Recur(controller, lastRunOrder, m.Order, out errMsg, out data, out var isStopped, out var isExpired))
                                        {
                                            foreach (var ditm in lastRunOrder.OrderDetails)
                                            {
                                                if (Eshop.Product.DAL.ProductMgr.UpdateQuantity(db, ditm.Product, ditm.Variant, -1 * ditm.QuantityOri,
                                                    false, false, true, false,
                                                    new ProdQtyLogModel() { Reason = (int)ProdQtyLogReason.Ordered, RefIdFunc = () => lastRunOrder.Id }))
                                                {
                                                    ditm.Product.OrderDetailNum = null;
                                                    ditm.Product.TotalSales = null;
                                                    ditm.Product.Update(db.DataService);
                                                }
                                            }

                                            OrderMgr.ProcessCustomerProduct(lastRunOrder, lastRunOrder.Customer, db, db.GetSession());
                                            db.GetSession().Flush();

                                            lastRunOrder.OrderStatus = (byte)Models.OrderStatus.New;

                                            log.OrderStatus = lastRunOrder.OrderStatus;
                                            log.Body = string.Format(StringTable.OrderCaptureOkLogTempl, data.SafeProp(z => z.TransactionId), lastRunOrder.GetPaymentSystemName());
                                            m.LastRunOk = true; lastRunOrder.Flags &= ~(int)OrderFlagType.RenewPayment; lst.Add(lastRunOrder);
                                            try { Controllers.Admin.BaseController.ExecWebMethod(string.Format("/modules/eshop/WebJobOrder/SendEmail/{0}?securityCode={1}", lastRunOrder.Id, Controllers.FrontEnd.WebJobOrderController.SecurityCode), out _); } catch (Exception) { }
                                        }
                                        else log.Body = string.Format(StringTable.OrderCaptureFailedLogTempl, errMsg);

                                        m.LastRunMsg = errMsg;

                                        OrderNoteMgr.SaveorUpdate(db, log, null, controller.Request);
                                    }
                                    catch (Exception err) { BaseMgr.LogErr(err); }
                                }
                            }
                        }

                        if (reactive) m.Status = (byte)RecurOrderStatus.Active;
                        if (resetLastRun && !recapture) m.LastRunTime = null;

                        lst.Add(m);
                    });

                    if (Recurring != null) RecurOrderLogMgr.SaveorUpdate(db, new RecurOrderLogModel()
                    {
                        Content = "Update PaymentData",
                        RecurringId = Recurring.Id,
                    }, null, controller?.Request);
                }

                if (BaseMgr.Update(db, lst)) 
                {
                    lstActs.Action(m => m());
                    return true;
                }
                else return false; 
            }
            else if (CartId > 0)
            {
                var cart = db.DataService.GetObject<Cart>(CartId);
                cart.PaymentData = PaymentData;
                return BaseMgr.Update(db, new List<Cart>() { cart });
            }

            return false;
        }

        public virtual List<IOrderDetail> GetOrderDetailsEx()
        {
            return OrderDetails.Cast<IOrderDetail>().ToList();
        }

        public virtual List<IOrderDetailBundle> GetOrderDetailBundles()
        {
            return OrderDetails.SelectMany(m => m.OrderDetailBundles).Cast<IOrderDetailBundle>().ToList();
        }

        public virtual string GetShippingTrackingUrl()
        {
            return DomExtensions.GetShippingTrackingUrl(this);
        }

        public virtual System.Tuple<string, bool> GetShippingInfo()
        {
            return DomExtensions.GetShippingInfo(this);
        }

        public virtual void UpdateCompanyInfo(Dictionary<string, string> data)
        {
            if (data == null || data.Count == 0) return;

            CompanyName = data.SafeGet("CompanyName");

            OrgNumber = data.SafeGet("OrgNumber");

            InvoiceEmail = data.SafeGet("InvoiceEmail");

            if (Customer != null && !data.SafeGet("ContactName").IsNullOrEmpty()) 
                Customer.ContactName = data.SafeGet("ContactName");
        }

        public virtual bool IsValidOnlinePaymentAmount()
        {
            return DomExtensions.IsValidOnlinePaymentAmount(this);
        }

        public virtual bool CaptureGiftcardAmount(Controllers.Admin.BaseController controller, ref bool sendMailYet)
        {
            return DomExtensions.CaptureGiftcardAmount(this, controller, ref sendMailYet);
        }

        public virtual bool IsRecurOneTime()
        {
            return DomExtensions.IsRecurOneTime(this);
        }

    }
}