namespace Claw.Modules.Eshop.Domain
{
    public partial class RegionLang
    {
        public virtual int Id { get; set; }

        public virtual Region Region { get; set; }

        public virtual string Name { get; set; }

        public virtual string LangCode { get; set; }
    }
}