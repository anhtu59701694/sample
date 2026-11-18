using System;
using System.Text;
using System.Collections.Generic;

namespace Claw.Modules.Eshop.Domain
{
    public partial class VariantPropVal
    {
        public virtual int Id { get; set; }
        public virtual PropVal PropVal { get; set; }
        public virtual VariantBase Variant { get; set; }
    }
}
