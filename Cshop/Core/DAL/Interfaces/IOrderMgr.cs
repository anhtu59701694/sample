using System.Collections.Generic;
using Claw.Modules.Eshop.Domain;
using NHibernate;
using System;
using Claw.Modules.Eshop.Domain.Interfaces;

namespace Claw.Modules.Eshop.DAL.Interfaces
{
    public interface IOrderMgr : IDalMgr
    {
        IOrder GetDom(IDB db, long id, ISession session = null);
        int GetShippingCount(IDB db, int shippingId);
        int GetPaymentMethodCount(IDB db, int paymentMethodId);
        bool CompletePayment(IDB db, IOrder obj);
        int GetCouponCount(IDB db, int customerId, int couponId);
        int GetOrderCouponCount(IDB db, int couponId);
        int GetOrderDetailCouponCount(IDB db, int couponId);
        int GetOrderDiscountCount(IDB db, int discountId);
        int GetOrderDetailDiscountCount(IDB db, int discountId);

        List<int[]> GetProductTotalSales(IDB db, params int[] productIds);
        void BindStats(IDB db, List<IProduct> list);
        void BindStats(IDB db, List<IProduct2> list);
        List<System.Tuple<int, int>> CountVariant(IDB db, byte status, int productid, int[] varinatids);
        List<System.Tuple<int, int>> CountVariant(IDB db, int productid, int[] variantids, params byte[] statuses);
        List<int> GetDependOrders(IDB db, int count, int productid, byte status, int? variantid = null);
        List<int> GetDependOrders(IDB db, int count, int productid, int? variantid, params byte[] statuses);
        List<IOrder> GetDependOrders(IDB db, int backProductId);
        bool AnyOrder(IDB db, IProduct product);
        bool AnyOrder(IDB db, IVariant variant);
        IOrder[] FirePaymentDone(IDB db, IOrder obj);
    }

    public interface IMembershipMgr : IDalMgr
    {
        IMembership Get(int customerId);
        decimal GetDiscountRate(IShoppingCart shoppingCart);
        bool IsFreeShipping(IShoppingCart shoppingCart, decimal orderTotal);
        void FireOrderCreated(IOrder obj, IDB db, bool useTransaction = true);
        void FireCustomerUpdated(ICustomer obj, IDB db);
        int GetCurrentType();
    }

    public interface IShoppingCartMgr : IDalMgr
    {
        IShoppingCart Get();
    }
}