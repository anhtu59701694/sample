namespace Claw.Modules.Eshop.Domain
{
    public partial class BongerProduct : IDom
    {
        public virtual int Id { get; set; }
        public virtual Bonger Bonger { get; set; }
        public virtual ProductBase Product { get; set; }
        public virtual VariantBase Variant { get; set; }
        public virtual int Quantity { get; set; }
        public virtual string ProductName { get; set; }
    }
}