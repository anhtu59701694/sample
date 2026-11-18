using System;

namespace Claw.Modules.Eshop.Domain
{
    public partial class ProductSale
    {
        public virtual long Id { get; set; }
        public virtual DateTime DateMin { get; set; }          
        public virtual int Year { get; set; }
        public virtual int Month { get; set; }
        public virtual ProductBase Product { get; set; }
        public virtual int? Quantity { get; set; }
        public virtual int? Orders { get; set; }
        public virtual DateTime Timestamp { get; set; }
    }
}