using System;
using System.Text;
using System.Collections.Generic;

namespace Claw.Modules.Eshop.Domain
{
    public partial class CategoryCustomer : IDom
    {
        public virtual int Id { get; set; }
        public virtual decimal DiscountRate { get; set; }
        public virtual string Notes { get; set; }
        public virtual Category Category { get; set; }
        public virtual Customer Customer { get; set; }
    }
}
