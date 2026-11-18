using System;
using System.Text;
using System.Collections.Generic;

namespace Claw.Modules.Eshop.Domain
{
    public partial class Property : Tuple<PropertyLang, PropVal>, IDisplayOrder, IStatus
    {
        public virtual int Id { get; set; }
        public virtual string Code { get; set; }
        public virtual int DisplayOrder { get; set; }
        public virtual DateTime ModifiedDate { get; set; }
        public virtual DateTime CreatedDate { get; set; }
        public virtual bool Active { get; set; }
        public virtual string Icon { get; set; }

        public virtual IList<PropertyLang> Languages
        {
            get { return Get(ref _list0); }
            set { _list0 = value; }
        }

        public virtual IList<PropVal> PropVals
        {
            get { return Get(ref _list1); }
            set { _list1 = value; }
        }
    }
}
