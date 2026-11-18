using Claw.Modules.Eshop.Domain.Interfaces;

namespace Claw.Modules.Eshop.Domain
{
    public partial class ProductRecurDisc : IDom
    {
        public virtual int Id { get; set; }
        public virtual int Type { get; set; }
        public virtual int RecurEach { get; set; }
        public virtual decimal Value { get; set; }
        public virtual bool DefOpt { get; set; }
        public virtual ProductBase Product { get; set; }
        public virtual IDiscount Discount { get; set; }
    }
}