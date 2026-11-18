using System;
using System.Collections.Generic;
using AutoMapper;
using Claw.Modules.Eshop.DAL;
using Claw.Modules.Eshop.Domain;
using Claw.Modules.Eshop.Models;

namespace Claw.Modules.Eshop.Order
{
    public class AutoMapper
    {
        public static void Configuration()
        {
            System.Threading.Tasks.Task.Run(() =>
            {
                new CustGroupMgr();
                new CustomerMgr();
                new InvoiceMgr();
                new OrderMgr();
                new OrderNoteMgr();
                new ShippingBookingMgr();

                Controllers.Admin.BaseController.StartJob<Helper.OrderCronJob>(1);
            });

            Mapper.CreateMap<Domain.Order, OrderModel>()
                .ForMember(m => m.Raw, conf => conf.MapFrom(m => m))
                .ForMember(m => m.Customer, conf => conf.MapFrom(m => m.GetCustomer()))
                .ForMember(m => m.FinalTotal, conf => conf.MapFrom(m => (decimal?)(m.Credited > 0 || m.OrderStatus.InList(OrderStatus.Credit) ? (m.FinalTotal + (decimal)(m.RoundingUp ?? 0)) : Math.Max(0, (m.FinalTotal ?? 0) + (decimal)(m.RoundingUp ?? 0))).ToOMoneyMc(m)))
                .ForMember(m => m.DeliveredDate, conf => conf.MapFrom(m => new ODateTime(DateTime.MinValue) { Value = m.DeliveredDate }))
                .ForMember(m => m.ExpectedDelivery, conf => conf.MapFrom(m => new ODateTime(DateTime.MinValue) { Value = m.ExpectedDelivery }))
                .ForMember(m => m.OfferExpiredDate, conf => conf.MapFrom(m => new ODateTime(DateTime.MinValue) { Value = m.OfferExpiredDate }))
                .ForMember(m => m.PaymentRetryTimestamp, conf => conf.MapFrom(m => new ODateTime(DateTime.MinValue) { Value = m.PaymentRetryTimestamp }))
                .ForMember(m => m.ReportDate, conf => conf.MapFrom(m => new ODateTime(DateTime.MinValue) { Value = m.ReportDate }))
                .ForMember(m => m.DeliverCountryCode, conf => conf.MapFrom(m => m.DeliverCountry.Code))
                .ForMember(m => m.BillingCountryCode, conf => conf.MapFrom(m => m.BillingCountry.Code))
                .ForMember(m => m.DeliverCountryName, conf => conf.MapFrom(m => m.DeliverCountry.GetName(null)))
                .ForMember(m => m.BillingCountryName, conf => conf.MapFrom(m => m.BillingCountry.GetName(null)))
                .ForMember(m => m.DeliverCountryId, conf => conf.MapFrom(m => m.DeliverCountry.GetId(null)))
                .ForMember(m => m.BillingCountryId, conf => conf.MapFrom(m => m.BillingCountry.GetId(null)))
                .ForMember(m => m.PaymentMethodId, conf => conf.MapFrom(m => m.PaymentMethod.Id))
                .ForMember(m => m.ShippingId, conf => conf.MapFrom(m => m.Shipping.Id))               
                .ForMember(m => m.DiscountId, conf => conf.MapFrom(m => m.Discount.GetId(null)))
                .ForMember(m => m.CouponId, conf => conf.MapFrom(m => m.Coupon.GetId(null)))
                .ForMember(m => m.InvoiceId, conf => conf.MapFrom(m => m.Invoice.GetId(null)))
                .ForMember(m => m.OriginalBackOrderId, conf => conf.MapFrom(m => m.OriginRest.GetId(null)))
                .ForMember(m => m.PaymentCost,
                    conf => conf.MapFrom(m => m.OrderStatus.InList(OrderStatus.Credit) ? 0 : m.PaymentCost))
                .ForMember(m => m.ShippingCost,
                    conf => conf.MapFrom(m => m.OrderStatus.InList(OrderStatus.Credit) ? 0 : m.ShippingCost))
                .ForMember(m => m.OrderDetails, conf => conf.MapFrom(m => null as List<OrderDetailModel>))
                .ForMember(m => m.OrderNotes, conf => conf.MapFrom(m => null as List<OrderNoteModel>))
                .ForMember(m => m.PaymentLogs, conf => conf.MapFrom(m => null as List<PaymentLogModel>))
                .ForMember(m => m.BackOrderDetails, conf => conf.MapFrom(m => null as List<BackOrderDetailModel>))
                .ForMember(m => m.Currency, conf => conf.MapFrom(m => m.Currency.ToUpper()))
                .ForMember(m => m.PackingStart, conf => conf.MapFrom(m => m.PackingStart.ToODateTime()))
                .ForMember(m => m.PackingEnd, conf => conf.MapFrom(m => m.PackingEnd.ToODateTime()))
                .ForMember(m => m.IsPosOrder, conf => conf.MapFrom(m => !m.PaidByType.IsNullOrEmpty()))
                .ForMember(m => m.PaymentCardMaskedPan, conf => conf.MapFrom(m => m.GetPaymentCardMaskedPan('X')))
                .ForMember(m => m.RoundingUp, conf => conf.MapFrom(m => new OMoney((decimal?)m.RoundingUp)))
                .ForMember(m => m.TotalDiscount, conf => conf.MapFrom(m => m.TotalDiscount + m.GetOfferDiscount()));

            Mapper.CreateMap<OrderDetail, OrderDetailModel>()
                .ForMember(m => m.Raw, conf => conf.MapFrom(m => m))
                .ForMember(m => m.FinalPrice, conf => conf.MapFrom(m => Math.Max(0, m.FinalPrice ?? 0)))
                .ForMember(m => m.OrderId, conf => conf.MapFrom(m => m.Order.Id))
                .ForMember(m => m.ProductName, conf => conf.MapFrom(m => m.GetRecurInfoDesc(m.Order.Recurring == null || m.Order.Recurring.Order.Id == m.Order.Id)))
                .ForMember(m => m.ProductId, conf => conf.MapFrom(m => m.Product.Id))
                .ForMember(m => m.VariantId, conf => conf.MapFrom(m => m.Variant.GetId(null)))
                .ForMember(m => m.DiscountId, conf => conf.MapFrom(m => m.Discount.GetId(null)))
                .ForMember(m => m.CouponId, conf => conf.MapFrom(m => m.Coupon.GetId(null)))
                .ForMember(m => m.ProductImage, conf => conf.MapFrom(m => m.Product.GetImage(m.Variant)))
                .ForMember(m => m.ProductDeleted, conf => conf.MapFrom(m => m.Product.Deleted))
                .ForMember(m => m.OriginPriceInc, conf => conf.MapFrom(m => (m.PriceBak ?? m.Price) * (1 + m.Vat)))
                .ForMember(m => m.OriginSalePriceInc, conf => conf.MapFrom(m => m.Product.CombinedSalePrice(m.Variant, false) * (1 + m.Vat)))
                .ForMember(m => m.PriceInc, conf => conf.MapFrom(m => m.PriceInc - (m.Id == 0 ? m.DiscountValue : 0)))
                .ForMember(m => m.Price, conf => conf.MapFrom(m => (m.PriceInc - (m.Id == 0 ? m.DiscountValue : 0)) / (1 + m.Vat)))
                .ForMember(m => m.GiftCard, conf => conf.MapFrom(m => m.GC2reditNote != null))
                .ForMember(m => m.CreditNote, conf => conf.MapFrom(m => m.GC2reditNote))
                .ForMember(m => m.PriceOrigin, conf => conf.MapFrom(m => m.PriceOrigin != null && ((m.PriceOrigin ?? 0) - m.Price + ((m.DiscountValue ?? 0)/* + (m.CouponValue ?? 0)*/) / (1 + m.Vat)) > 0.01m ? m.PriceOrigin : null));

            Mapper.CreateMap<OrderDetailBundle, OrderDetailBundleModel>()
                .ForMember(m => m.OrderDetailId, conf => conf.MapFrom(m => m.OrderDetail.Id))
                .ForMember(m => m.ProductId, conf => conf.MapFrom(m => m.Product.Id))
                .ForMember(m => m.VariantId, conf => conf.MapFrom(m => m.Variant.GetId(null)));

            Mapper.CreateMap<OrderDetailGift, OrderDetailGiftModel>()
                .ForMember(m => m.OrderDetailId, conf => conf.MapFrom(m => m.OrderDetail.Id))
                .ForMember(m => m.ProductId, conf => conf.MapFrom(m => m.Product.Id))
                .ForMember(m => m.VariantId, conf => conf.MapFrom(m => m.Variant.GetId(null)))
                .ForMember(m => m.Price, conf => conf.MapFrom(m => m.Product.CombinedSalePrice(m.Variant)))
                .ForMember(m => m.PriceInc, conf => conf.MapFrom(m => m.Product.CombinedSalePriceInc(m.Variant)))
                .ForMember(m => m.ProductImage, conf => conf.MapFrom(m => m.Product.GetImage(m.Variant)));

            Mapper.CreateMap<OrderNote, OrderNoteModel>()
                .ForMember(m => m.OrderId, conf => conf.MapFrom(m => m.Order.Id));

            Mapper.CreateMap<Customer, CustomerModel>()
                .ForMember(m => m.DeliverCountryId, conf => conf.MapFrom(m => m.DeliverCountry.GetId(null)))
                .ForMember(m => m.BillingCountryId, conf => conf.MapFrom(m => m.BillingCountry.GetId(null)))
                .ForMember(m => m.Orders, conf => conf.MapFrom(m => m.OrderNum))
                .ForMember(m => m.OwnerId, conf => conf.MapFrom(m => m.Owner.GetId(null)))
                .ForMember(m => m.StatsInfo, conf => conf.MapFrom(m => null as CustomerModel.StatsType))
                .ForMember(m => m.Individual, conf => conf.MapFrom(m => m.OrgNumber.IsNullOrEmpty() && m.CompanyName.IsNullOrEmpty()))
                .ForMember(m => m.Email, conf => conf.MapFrom(m => (m.Email ?? "").EndsWith("@invalid.local") ? "" : m.Email))
                .ForMember(m => m.DiscountRate, conf => conf.MapFrom(m => m.DiscountRate * 100))
                .ForMember(m => m.Raw, conf => conf.MapFrom(m => m))
                .ForMember(m => m.Addresses, conf => conf.MapFrom(m => m.AddressNum));

            Mapper.CreateMap<CustGroup, CustGroupModel>()
                .ForMember(m => m.Raw, conf => conf.MapFrom(m => m))
                .ForMember(m => m.Customers, conf => conf.MapFrom(m => m.CustomerNum))
                .ForMember(m => m.Discounts, conf => conf.MapFrom(m => m.DiscountNum))
                .ForMember(m => m.DiscountRate, conf => conf.MapFrom(m => m.DiscountRate*100));

            Mapper.CreateMap<Address, AddressModel>();
            Mapper.CreateMap<Company, CompanyModel>()
                .ForMember(m => m.Raw, conf => conf.MapFrom(m => m));
            Mapper.CreateMap<Bonger, BongerModel>()
                .ForMember(m => m.Raw, conf => conf.MapFrom(m => m))
                .ForMember(m => m.NoteLogs, conf => conf.MapFrom(m => m.Notes))
                .ForMember(m => m.Notes, conf => conf.MapFrom(m => ""));

            Mapper.CreateMap<Invoice, InvoiceModel>()
                .ForMember(m => m.pos_ShopID, conf => conf.MapFrom(m => m.pos_ShopID))
                .ForMember(m => m.OrderId, conf => conf.MapFrom(m => m.Order.Id));

            Mapper.CreateMap<RecurOrder, RecurOrderModel>()
                .ForMember(m => m.Raw, conf => conf.MapFrom(m => m))
                .ForMember(m => m.NextRunning, conf => conf.MapFrom(m => m.GetNextRunning().ToODateTime()))
                .ForMember(m => m.ToDate, conf => conf.MapFrom(m => m.ToDate.ToODateTime()))
                .ForMember(m => m.LastRunTime, conf => conf.MapFrom(m => m.LastRunTime.ToODateTime()));

            Mapper.CreateMap<RecurOrderLog, RecurOrderLogModel>()
               .ForMember(m => m.RecurringId, conf => conf.MapFrom(m => m.Recurring.Id));

            Mapper.CreateMap<ShippingBooking, ShippingBookingModel>();
        }
    }
}