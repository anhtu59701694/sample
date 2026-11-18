using System.Collections.Generic;

namespace Claw.Modules.Eshop.Domain
{
    public partial class TechSpecVal : Tuple<ProductTechSVal>
    {
        public virtual int Id { get; set; }
        public virtual TechSpec TechSpec { get; set; }
        public virtual string Value { get; set; }
        public virtual string Description { get; set; }
        public virtual int DisplayOrder { get; set; }

        public virtual string Code
        {
            get { return string.Format("tsv{0}", Id); }
        }

        public virtual IList<ProductTechSVal> ProductTechSVals 
        {
            get { return Get(ref _list0); }
            set { _list0 = value; }
        }

    }
}