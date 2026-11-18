namespace Claw.Modules.Eshop.Domain
{
    public partial class ProductTechSVal
    {
        public virtual int Id { get; set; }
        public virtual Product Product { get; set; }
        public virtual TechSpecVal TechSpecVal { get; set; }
    }
}