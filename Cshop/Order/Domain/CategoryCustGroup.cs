using System;
using System.Text;
using System.Collections.Generic;

namespace Claw.Modules.Eshop.Domain
{
    public partial class CategoryCustGroup : IDom
    {
        public virtual int Id { get; set; }
        public virtual Category Category { get; set; }
        public virtual CustGroup Group { get; set; }
    }
}
