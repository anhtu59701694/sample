using System.Collections.Generic;
using Claw.Modules.Eshop.Models;

namespace Claw.Modules.Eshop.Domain.Interfaces
{
    public interface IShopCartItem
    {
        ICoupon Coupon { get; }
        IDiscount Discount { get; }

        decimal CouponValue { get; }
        decimal DiscountValue { get; }

        IProduct Product { get; }

        IVariant Variant { get; }

        int Quantity { get; }

        OMoneyMc Price { get; set; }

        RecurringType? RecurType { get; set; }

        int? RecurTimes { get; set; }

        bool? SelfDiscount { get; set; }

        string Key { get; set; }
    }

    public interface IShoppingCart
    {
        System.Tuple<MembershipType, IMembership> GetMembership();

        int Count(int? productId);

        ICoupon Coupon { get; }
        IDiscount Discount { get; }

        decimal CouponValue { get; }
        decimal DiscountValue { get; }

        decimal GiftCardTotal { get; set; }

        string CacheKey { get; }

        IEnumerable<IShopCartItem> Items { get; }

        IShoppingCart Clone();

        IShoppingCart Add(int productId, int? variantId = null, int? recurType = null, int? recurTimes = null, string data = null, bool? selfDiscount = null);
    }

    public interface IMembership
    {
        MembershipType Type { get; }

        bool Active { get; }

        List<System.Tuple<int, double>> Benefits { get; }
    }
}