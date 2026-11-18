using System;
using System.Collections.Generic;

namespace Claw.Modules.Eshop.Domain
{
    public partial class UserView
    {
        public virtual Guid Id { get; set; }

        public virtual Guid UserId { get; set; }

        public virtual int RefId { get; set; }

        public virtual int Type { get; set; }

        public virtual int Count { get; set; }

        public virtual DateTime Timestamp { get; set; }
    }
}