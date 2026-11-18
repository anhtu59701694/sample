using System.Collections.Generic;
using System.Linq;
using Claw.Modules.Eshop.DAL;
using Claw.Modules.Eshop.Domain.Interfaces;
using Claw.Modules.Eshop.Product.Helper;

namespace Claw.Modules.Eshop.Domain
{
    public partial class OrderDetail : Tuple<OrderDetailBundle, OrderDetailGift, IDom, OrderDetailTag>, IOrderDetail
    {
        public virtual int Id { get; set; }

        /// <summary>Temp value</summary>
        public virtual ulong? Timestamp { get; set; }
        
        public virtual Order Order { get; set; }
        public virtual ProductBase Product { get; set; }
        public virtual VariantBase Variant { get; set; }
        public virtual Discount Discount { get; set; }

        /// <summary>Membership</summary>
        public virtual Discount Discount2 { get; set; }

        public virtual Coupon Coupon { get; set; }
        public virtual string ProductName { get; set; }
        public virtual string ProductSKU { get; set; }
        public virtual int QuantityOri { get; set; }
        public virtual int? QuantityOriBak { get; set; }

        /// <summary>QuantityOri - RestQty</summary>
        public virtual int Quantity { get { return QuantityOri - RestQty; } protected set { } }

        public virtual int QuantityBak { get { return (QuantityOriBak ?? QuantityOri) - RestQty; } protected set { } }

        public virtual int RestQty { get; set; }
        public virtual int? StockQty { get; set; }
        public virtual decimal Price { get; set; }
        public virtual decimal? PriceBak { get; set; }
        public virtual decimal? PriceAdm { get; set; }
        public virtual decimal? PriceOrigin { get; set; }
        public virtual decimal? CostPrice { get; set; }
        public virtual decimal Vat { get; set; }
        public virtual decimal? PriceInc { get; set; }
        public virtual decimal? CostPriceInc { get; set; }
        public virtual decimal ProductWeight { get; set; }
        public virtual byte ProductWUnit { get; set; }
        public virtual decimal? ShippingCostExtra { get; set; }
        public virtual decimal? DepositAmount { get; set; }
        public virtual int ProductShippingType { get; set; }
        public virtual string DiscountName { get; set; }
        public virtual decimal? DiscountValue { get; set; }
        public virtual decimal DiscountValue2 { get; set; }
        public virtual string CouponCode { get; set; }
        public virtual decimal? CouponValue { get; set; }
        public virtual decimal? FinalPrice { get; set; }
        public virtual bool Delivered { get; set; }
        public virtual int? RecurQty { get; set; }
        public virtual int? SourceId { get; set; }
        public virtual int? RecurType { get; set; }
        public virtual int? RecurEach { get; set; } 
        public virtual int? SrcRecurType { get; set; }
        public virtual int? SrcRecurEach { get; set; }
        public virtual string Data { get; set; }
        public virtual string Tags { get; set; }
        public virtual bool Manual { get; set; }
        public virtual bool? GC2reditNote { get; set; }
        public virtual string SerialNumber { get; set; }
        public virtual bool? Offered { get; set; }

        /// <summary>
        /// 1: giftcard
        /// </summary>
        public virtual int? CategoryId { get; set; }

        public virtual IList<OrderDetailBundle> OrderDetailBundles
        {
            get { return Get(ref _list0); }
            set { _list0 = value; }
        }

        public virtual IList<OrderDetailGift> OrderDetailGifts
        {
            get { return Get(ref _list1); }
            set { _list1 = value; }
        }

        public virtual IList<IDom> GiftCards
        {
            get { return Get(ref _list2); }
            set { _list2 = value; }
        }

        public virtual IList<OrderDetailTag> OrderDetailTags
        {
            get { return Get(ref _list3); }
            set { _list3 = value; }
        }

        public virtual string GetProductUrl()
        {
            return Singleton<IModuleUrl>.Default.GetProductUrl(Product, Variant, this);
        }

        public virtual string GetProductImage()
        {
            return Variant != null && !Variant.Image.IsNullOrEmpty()
                ? Variant.GetThumbFrist
                : Product.GetImageThumbFrist;
        }

        public virtual IProduct GetProduct()
        {
            return Product;
        }

        public virtual IVariant GetVariant()
        {
            return Variant;
        }

        public virtual List<IOrderDetailBundle> GetOrderDetailBundles()
        {
            return OrderDetailBundles.Cast<IOrderDetailBundle>().ToList();
        }
    }
}