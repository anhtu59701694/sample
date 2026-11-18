namespace Claw.Modules.Eshop.Domain
{
    public partial class ProductSerial
    {
        public virtual int Id { get; set; }
        public virtual ProductBase Product { get; set; }
        public virtual string SerialNumber { get; set; }
        public virtual bool Sold { get; set; }
    }
}