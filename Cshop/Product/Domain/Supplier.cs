using Claw.Core;
using System.Collections.Generic;
using Claw.Core.Extensions;

namespace Claw.Modules.Eshop.Domain
{
    public partial class Supplier : Tuple<ProductBase>, IStatus
    {
        public virtual int Id { get; set; }

        public virtual string Name { get; set; }

        public virtual string Information { get; set; }

        public virtual bool Active { get; set; }

        public virtual IList<ProductBase> Products
        {
            get { return Get(ref _list0); }
            set { _list0 = value; }
        }
    }
}