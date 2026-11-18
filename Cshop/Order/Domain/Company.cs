using System;
using System.Collections.Generic;

namespace Claw.Modules.Eshop.Domain
{
    public partial class Company : Tuple<Customer>
    {
        public virtual int Id { get; set; }
        public virtual string Name { get; set; }
        public virtual string OrgNumber { get; set; }
        public virtual string Data { get; set; }
        public virtual Customer Owner { get; set; }
        public virtual DateTime CreatedDate { get; set; }
        public virtual int? UserNum { get; set; }

        public virtual IList<Customer> Customers
        {
            get { return Get(ref _list0); }
            set { _list0 = value; }
        }
    }
}