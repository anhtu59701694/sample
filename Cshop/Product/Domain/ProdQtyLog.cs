using System;
using System.Text;
using System.Collections.Generic;

namespace Claw.Modules.Eshop.Domain
{
    public partial class ProdQtyLog
    {
        private int? _refId;

        public virtual int Id { get; set; }
        public virtual ProductBase Product { get; set; }
        public virtual VariantBase Variant { get; set; }
        public virtual int Quantity { get; set; }
        public virtual int? StockQty { get; set; }
        public virtual int? StockQty1 { get; set; }
        public virtual int? StockQty2 { get; set; }
        public virtual int? QtyPos { get; set; }
        public virtual int Reason { get; set; }
        public virtual string Note { get; set; }

        public virtual int? RefId
        {
            get { return _refId ?? (RefIdFunc != null ? RefIdFunc(): null); } 
            set { _refId = value; }
        }

        public virtual Func<int?> RefIdFunc { get; set; }

        public virtual DateTime CreatedDate { get; set; }
    }
}
