namespace Claw.Modules.Eshop.Domain
{
    public partial class CountryLang
    {
        public virtual int Id { get; set; }

        public virtual string Name { get; set; }

        public virtual string LangCode { get; set; }

        public virtual Country Country { get; set; }
    }
}