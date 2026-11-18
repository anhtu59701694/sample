using System.Collections.Generic;

namespace Claw.Modules.Eshop.Domain
{
    public partial class Region : Tuple<RegionLang, Country>
    {
        public virtual int Id { get; set; }

        public virtual string Code { get; set; }

        public virtual int DisplayOrder { get; set; }
        
        public virtual IList<RegionLang> Languages
        {
            get { return Get(ref _list0); }
            set { _list0 = value; }
        }

        public virtual IList<Country> Countries
        {
            get { return Get(ref _list1); }
            set { _list1 = value; }
        }
    }
}