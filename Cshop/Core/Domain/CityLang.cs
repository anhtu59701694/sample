namespace Claw.Modules.Eshop.Domain
{
    public partial class CityLang
    {
        public virtual int Id { get; set; }

        public virtual City City { get; set; }

        public virtual string Name { get; set; }

        public virtual string LangCode { get; set; }
    }
}