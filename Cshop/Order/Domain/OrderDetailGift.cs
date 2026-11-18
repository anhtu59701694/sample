namespace Claw.Modules.Eshop.Domain
{
    public partial class OrderDetailGift : IDom
    {
        public virtual int Id { get; set; }
        public virtual ProductBase Product { get; set; }
        public virtual VariantBase Variant { get; set; }
        public virtual OrderDetail OrderDetail { get; set; }
        public virtual int Quantity { get; set; }
        public virtual string ProductName { get; set; }
        public virtual string ProductSKU { get; set; }
    }
}