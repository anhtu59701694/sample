namespace Claw.Modules.Eshop.Domain
{
    public partial class OrderDetailTag
    {
        public virtual int Id { get; set; }
        public virtual Customer Customer { get; set; }
        public virtual Order Order { get; set; }
        public virtual OrderDetail OrderDetail { get; set; }
        public virtual string Tag { get; set; }
        public virtual string Key { get; set; }
    }
}