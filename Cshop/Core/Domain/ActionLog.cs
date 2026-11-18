using System;
using System.Collections.Generic;

namespace Claw.Modules.Eshop.Domain
{
    public partial class ActionLog
    {
        public virtual int Id { get; set; }
        public virtual int? RefId { get; set; }
        public virtual int Action { get; set; }
        public virtual string Activity { get; set; }
        public virtual string UserIp { get; set; }
        public virtual string UserName { get; set; }
        public virtual string UserHost { get; set; }
        public virtual string UserAgent { get; set; }
        public virtual DateTime CreatedDate { get; set; }
    }
}