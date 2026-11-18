using Claw.Modules.Eshop.Domain.Interfaces;

namespace Claw.Modules.Eshop.Domain
{
    public partial class OrderDetailBundle : IOrderDetailBundle
    {
        public virtual int Id { get; set; }
        public virtual ProductBase Product { get; set; }
        public virtual VariantBase Variant { get; set; }
        public virtual OrderDetail OrderDetail { get; set; }
        public virtual int Quantity { get; set; }
        public virtual int? StockQty { get; set; }
        public virtual string ProductName { get; set; }
        public virtual string ProductSKU { get; set; }
        public virtual string SerialNumber { get; set; }

        public virtual IOrderDetail GetOrderDetail()
        {
            return OrderDetail;
        }

        public virtual IProduct GetProduct()
        {
            return Product;
        }

        public virtual IVariant GetVariant()
        {
            return Variant;
        }

    }
}