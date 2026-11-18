namespace Claw.Modules.Eshop.Domain
{
    public partial class VatCountry
    {
        public virtual int Id { get; set; }

        public virtual Country Country { get; set; }

        public virtual bool Active { get; set; }
    }
}