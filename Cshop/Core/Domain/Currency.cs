using System;
using System.Text;
using System.Collections.Generic;

namespace Claw.Modules.Eshop.Domain
{
    public partial class Currency : IStatus, IDisplayOrder
    {
        public Currency()
        {
            
        }

        public Currency(Currency obj)
        {
            if (obj == null) return;

            typeof (Currency).GetProperties().Action(m => m.SetValue(this, m.GetValue(obj)));
        }

        public virtual int Id { get; set; }
        public virtual string Name { get; set; }
        public virtual string Code { get; set; }
        public virtual decimal Rate { get; set; }
        public virtual string Token { get; set; }
        public virtual bool Location { get; set; }
        public virtual string DecimalSep { get; set; }
        public virtual string ThousandSep { get; set; }
        public virtual int DecimalPlaces { get; set; }
        public virtual bool Active { get; set; }
        public virtual int DisplayOrder { get; set; }
        public virtual bool? AnyOrder { get; set; }
        public virtual Currency Clone() 
        {
            return (Currency)MemberwiseClone();
        }
    }
}
