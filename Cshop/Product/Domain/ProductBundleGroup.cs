namespace Claw.Modules.Eshop.Domain
{
    public partial class ProductBundleGroup
    {
        public virtual int Id { get; set; }
        public virtual string Name { get; set; }
        public virtual decimal? MinPrice { get; set; }
        public virtual int? Accessories { get; set; }
        public virtual bool ShowVariant { get; set; }
        public virtual ProductBase Product { get; set; }
    }
}