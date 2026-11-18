using System;
using System.Text;
using System.Collections.Generic;

namespace Claw.Modules.Eshop.Domain
{
    public partial class CategoryLang
    {
        public virtual int Id { get; set; }
        public virtual string Name { get; set; }
        public virtual string Description { get; set; }
        public virtual bool Visible { get; set; }
        public virtual string LangCode { get; set; }
        public virtual string Alias { get; set; }
        public virtual string PageTitle { get; set; }
        public virtual string MetaDesc { get; set; }

        public virtual int? SubCates { get; set; }
        public virtual int? Products { get; set; }
        public virtual int? ProductsSub { get; set; }

        public virtual int? ProductsBundle { get; set; }
        public virtual int? ProductsBundleSub { get; set; }


        public virtual StatusStatList StatusStats { get; set; }

        public virtual Category Category { get; set; }
    }
}
