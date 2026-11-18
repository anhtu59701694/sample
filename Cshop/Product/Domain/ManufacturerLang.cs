using System;
using System.Text;
using System.Collections.Generic;

namespace Claw.Modules.Eshop.Domain
{
    public partial class ManufacturerLang
    {
        public virtual int Id { get; set; }
        public virtual string Name { get; set; }
        public virtual string Description { get; set; }
        public virtual bool Visible { get; set; }
        public virtual string LangCode { get; set; }
        public virtual string Alias { get; set; }
        public virtual string PageTitle { get; set; }
        public virtual string MetaDesc { get; set; }
        public virtual Manufacturer Manufacturer { get; set; }
    }
}
