using System;
using System.Text;
using System.Collections.Generic;

namespace Claw.Modules.Eshop.Domain
{
    public partial class Setting
    {
        public virtual int Id { get; set; }
        public virtual string Name { get; set; }
        public virtual string Value { get; set; }
    }
}
