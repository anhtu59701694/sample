using System.Collections.Generic;

namespace Claw.Modules.Eshop.Domain
{
    public partial class City : Tuple<CityLang, District>
    {
        public virtual int Id { get; set; }

        public virtual Country Country { get; set; }

        public virtual string Code { get; set; }

        public virtual IList<CityLang> Languages
        {
            get { return Get(ref _list0); }
            set { _list0 = value; }
        }

        public virtual IList<District> Districts
        {
            get { return Get(ref _list1); }
            set { _list1 = value; }
        }
    }
}