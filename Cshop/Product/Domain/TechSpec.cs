using System.Collections.Generic;

namespace Claw.Modules.Eshop.Domain
{
    public partial class TechSpec : Tuple<TechSpecVal>, IStatus
    {
        public virtual int Id { get; set; }
        public virtual string Name { get; set; }
        public virtual string Icon { get; set; }
        public virtual bool Active { get; set; }
        public virtual bool Featured { get; set; }
        public virtual int DisplayOrder { get; set; }
        public virtual string Description { get; set; }

        public virtual IList<TechSpecVal> TechSpecVals
        {
            get { return Get(ref _list0); }
            set { _list0 = value; }
        }
    }
}