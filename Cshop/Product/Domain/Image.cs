using System;
using System.Text;
using System.Collections.Generic;

namespace Claw.Modules.Eshop.Domain
{
    public partial class Image : Tuple<ImageLang>, IDisplayOrder
    {
        public virtual int Id { get; set; }
        public virtual string FileName { get; set; }
        public virtual int DisplayOrder { get; set; }
        public virtual ProductBase Product { get; set; }

        public virtual IList<ImageLang> Languages
        {
            get { return Get(ref _list0); }
            set { _list0 = value; }
        }
    }
}
