using System;
using Claw.Modules.Eshop.DAL;
using BasePayment=Eshop.Payments.BasePayment;
using System.Collections.Generic;

namespace Claw.Modules.Eshop.Domain.Interfaces
{
    [Flags]
    public enum OrderFlagType 
    { 
        MembershipNotifing = 1, NotUsePaymentDeliverAddr = 2, AccountingSubmitted = 4, TilRegnskap = 8, Manual = 16, 
        SkippedRefundCredit = 32, NotUsePaymentBillingAddr = 64, Offered = 128, DropShipMark = 256, DropShipExported = 512, 
        Custom0 = 1024, Custom1 = 2048, RenewPayment = 4096, Sent2PickupPoint = 8192, OfferHideDiscPerProd = 16384, 
        RenewRecur = 32768, Confirmed = 65536
    }

    public interface IOrder : IDom
    {
        int? CartId { get; set; }

        byte OrderStatus { get; set; }
        int Flags { get; set; }

        IPaymentMethod PaymentMethod { get; set; }
        IShipping Shipping { get; set; }
        ICustomer Customer { get; }
        IOrder OriginRest { get; set; }

        string TransactionId { get; set; }
        string ReservationId { get; set; }
        string AuthorizationId { get; set; }
        string PaymentData { get; set; }
        string PaymentResultId { get; set; }
        string PaymentCardMaskedPan { get; set; }

        bool? RecurringTokenOnly { get; set; }
        string RecurringReturnUrl { get; set; }

        string ShippingName { get; set; }
        string ShippingTrackingId { get; set; }

        string BillingAddress { get; set; }
        string BillingPosted { get; set; }
        string BillingMobile { get; set; }
        string BillingName { get; set; }
        string BillingPostnr { get; set; }
        Country BillingCountry { get; set; }

        string DeliverName { get; set; }
        string DeliverMobile { get; set; }
        string DeliverAddress { get; set; }
        string DeliverPosted { get; set; }
        string DeliverPostnr { get; set; }
        Country DeliverCountry { get; set; }

        decimal OrderTotal { get; }
        decimal VatTotal { get; }
        decimal? GetFinalTotal();

        IList<IOrder> BackOrders { get; set; }

        bool CompletePayment(IDB db);
        string GetPaymentSystemName();
        BasePayment.PaymentData GetPaymentData(bool resolveType = true);
        BasePayment.Order ToPaymentOrder(bool? giftcard = null);
        List<BasePayment.OrderDetail> ToPaymentOrderDetails(IOrderDetail odetail = null, bool? giftcard = null);
        List<BasePayment.OrderDetail> ToPaymentOrderDetailsIncManual();
        void HandleNewEvent(IDB db, BasePayment.PaymentData data, bool sendMailYet);
        bool IsMoneyReturnable();

        long GetCartOrderId();
        bool CommitCart(IDB db, string email = null);
        void MakeOrderDetailSourceId();
        bool ValidPaymentData(Predicate<BasePayment.PaymentData> predicate);
        bool UpdatePaymentData(IDB db, bool completed = false, Controllers.Admin.BaseController controller = null);
        List<IOrderDetail> GetOrderDetailsEx();
        List<IOrderDetailBundle> GetOrderDetailBundles();
        string GetShippingTrackingUrl();
        System.Tuple<string, bool> GetShippingInfo();
        void UpdateCompanyInfo(Dictionary<string, string> data);
        bool IsValidOnlinePaymentAmount();
        bool CaptureGiftcardAmount(Controllers.Admin.BaseController controller, ref bool sendMailYet);
        bool IsRecurOneTime();
        bool IsRecurringTokenOnly();
        bool IsRenewRecur();
    }
}