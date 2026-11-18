using System;
using System.Collections.Generic;

namespace Claw.Modules.Eshop.Domain
{
    public partial class Bonger : Tuple<BongerProduct>
    {
        public virtual int Id { get; set; }
        public virtual Country Country { get; set; }
        public virtual Customer Customer { get; set; }
        public virtual string Name { get; set; }
        public virtual string Phone { get; set; }
        public virtual string Email { get; set; }
        public virtual string Address { get; set; }
        public virtual string Postnr { get; set; }
        public virtual string Posted { get; set; }
        public virtual string Notes { get; set; }
        public virtual DateTime ModifiedDate { get; set; }
        public virtual DateTime CreatedDate { get; set; }
        public virtual byte Status { get; set; }

        public virtual IList<BongerProduct> BongerProducts
        {
            get { return Get(ref _list0); }
            set { _list0 = value; }
        }
    }
}