using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;

namespace Claw.Modules.Eshop.Domain
{
    public partial class Manufacturer : Tuple<ManufacturerLang, ProductBase>, IDisplayOrder, IStatus
    {
        private List<ManufacturerLang> _languages = null;

        public virtual int Id { get; set; }
        public virtual string Alias { get; set; }
        public virtual string Url { get; set; }
        public virtual string Logo { get; set; }
        public virtual bool Active { get; set; }
        public virtual int DisplayOrder { get; set; }

        public virtual IList<ManufacturerLang> LanguagesFe
        {
            get { return _languages ?? (_languages = Languages.ToList()); }
        }

        public virtual IList<ManufacturerLang> Languages
        {
            get { return Get(ref _list0); }
            set { _list0 = value; }
        }

        public virtual IList<ProductBase> Products
        {
            get { return Get(ref _list1); }
            set { _list1 = value; }
        }
    }
}
