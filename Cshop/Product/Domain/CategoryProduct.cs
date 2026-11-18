using System;
using System.Text;
using System.Collections.Generic;

namespace Claw.Modules.Eshop.Domain
{
    public partial class CategoryProduct
    {
        public virtual int Id { get; set; }
        public virtual Category Category { get; set; }
        public virtual ProductBase Product { get; set; }
    }
}
