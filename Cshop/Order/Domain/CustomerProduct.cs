using System;
using System.Text;
using System.Collections.Generic;

namespace Claw.Modules.Eshop.Domain
{
    public partial class CustomerProduct
    {
        public virtual int Id { get; set; }
        public virtual Company Company { get; set; }
        public virtual Customer Customer { get; set; }
        public virtual ProductBase Product { get; set; }
        public virtual VariantBase Variant { get; set; }
        public virtual int Quantity { get; set; }
        public virtual int? QtyReorder { get; set; }
        public virtual string Data { get; set; }
        public virtual string Tags { get; set; }
        public virtual int? Orders { get; set; }
        public virtual bool Active { get; set; }
        public virtual DateTime ModifiedDate { get; set; }
        public virtual DateTime CreatedDate { get; set; }
    }
}
