namespace Claw.Modules.Eshop.Domain
{
    public partial class VariantLang
    {
        public virtual int Id { get; set; }
        public virtual VariantBase Variant { get; set; }
        public virtual string Description { get; set; }
        public virtual string LangCode { get; set; }
    }
}