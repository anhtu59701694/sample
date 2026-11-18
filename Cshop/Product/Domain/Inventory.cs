using System;
using System.Collections.Generic;

namespace Claw.Modules.Eshop.Domain
{
    public partial class Inventory : Tuple<InventoryProduct>
    {
        public virtual int Id { get; set; }
        public virtual int Type { get; set; }
        public virtual int UserId { get; set; }
        public virtual string UserName { get; set; }
        public virtual DateTime CreatedDate { get; set; }

        public virtual IList<InventoryProduct> InventoryProducts
        {
            get { return Get(ref _list0); }
            set { _list0 = value; }
        }
    }
}