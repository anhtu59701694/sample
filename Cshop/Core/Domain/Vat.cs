using System.Text;
using System.Collections.Generic;

namespace Claw.Modules.Eshop.Domain
{
    public partial class Vat : Tuple<IDom, IDom, IDom, IDom>, IStatus
    {
        public virtual int Id { get; set; }
        public virtual string Name { get; set; }
        public virtual decimal Value { get; set; }
        public virtual bool TakeAway { get; set; }
        public virtual bool Active { get; set; }

        public virtual IList<IDom> Products
        {
            get { return Get(ref _list0); }
            set { _list0 = value; }
        }

        public virtual IList<IDom> ProductSeconds
        {
            get { return Get(ref _list1); }
            set { _list1 = value; }
        }

        public virtual IList<IDom> Shippings
        {
            get { return Get(ref _list2); }
            set { _list2 = value; }
        }

        public virtual IList<IDom> PaymentMethods
        {
            get { return Get(ref _list3); }
            set { _list3 = value; }
        }
    }
}
