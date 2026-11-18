namespace Claw.Modules.Eshop.Domain
{
    public partial class ProductAccessory
    {
        public virtual int Id { get; set; }
        public virtual ProductBase Product { get; set; }
        public virtual ProductBase Accessory { get; set; }
        public virtual VariantBase AcceVariant { get; set; }
        public virtual ProductBundleGroup BundleGroup { get; set; }
    }
}