namespace Claw.Modules.Eshop.Domain
{
    public partial class VariantImageLang
    {
        public virtual int Id { get; set; }
        public virtual VariantImage VariantImage { get; set; }
        public virtual string Title { get; set; }
        public virtual string LangCode { get; set; }
    }
}