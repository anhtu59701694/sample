namespace Claw.Modules.Eshop.Domain
{
    public partial class CustomerCGroup
    {
        public virtual int Id { get; set; }
        public virtual Customer Customer { get; set; }
        public virtual CustGroup Group { get; set; }
    }
}