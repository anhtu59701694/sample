using System;
using System.Text;
using System.Collections.Generic;

namespace Claw.Modules.Eshop.Domain
{
    public partial class ProductBundle
    {
        public virtual int Id { get; set; }
        public virtual int Quantity { get; set; }
        public virtual ProductBase Bundle { get; set; }
        public virtual ProductBase Product { get; set; }
        public virtual VariantBase Variant { get; set; }
    }
}
