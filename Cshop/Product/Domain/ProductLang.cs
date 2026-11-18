using System;
using System.Text;
using System.Collections.Generic;

namespace Claw.Modules.Eshop.Domain
{
    public partial class ProductLang
    {
        public virtual int Id { get; set; }
        public virtual string Name { get; set; }
        public virtual string Tags { get; set; }
        public virtual string Description { get; set; }
        public virtual bool Visible { get; set; }
        public virtual string LangCode { get; set; }
        public virtual string Alias { get; set; }
        public virtual string PageTitle { get; set; }
        public virtual string MetaDesc { get; set; }
        public virtual string ExtraInfo { get; set; }
        public virtual string ExtraInfo2 { get; set; }
        public virtual string ExtraInfo3 { get; set; }
        public virtual string ExtraInfo4 { get; set; }
        public virtual ProductBase Product { get; set; }
    }
}
