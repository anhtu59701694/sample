namespace Claw.Modules.Eshop.Domain
{
    public partial class InventoryProduct
    {
        public virtual int Id { get; set; }
        public virtual Inventory Inventory { get; set; }
        public virtual ProductBase Product { get; set; }
        public virtual VariantBase Variant { get; set; }
        public virtual decimal? CostPrice { get; set; }
        public virtual int Quantity { get; set; }
        public virtual int? OldQuantity { get; set; }
        public virtual Status ProductStatus { get; set; }
        public virtual byte Visibility { get; set; }
        public virtual string Location { get; set; }
        public virtual string OldLocation { get; set; }
    }
}