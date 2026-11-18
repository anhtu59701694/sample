namespace Claw.Modules.Eshop.Domain
{
    public partial class BackOrderDetail
    {
        public virtual int Id { get; set; }
        public virtual Order Order { get; set; }
        public virtual OrderDetail OrderDetail { get; set; }
        public virtual int Quantity { get; set; }
    }
}