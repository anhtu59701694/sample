using System;
using System.Text;
using System.Collections.Generic;

namespace Claw.Modules.Eshop.Domain
{
    public partial class PropValLang
    {
        public virtual int Id { get; set; }
        public virtual string Name { get; set; }
        public virtual string LangCode { get; set; }
        public virtual PropVal PropVal { get; set; }
    }
}
