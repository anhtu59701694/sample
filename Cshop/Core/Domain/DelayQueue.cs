using System;

namespace Claw.Modules.Eshop.Domain
{
    public partial class DelayQueue
    {
        public virtual long Id { get; set; }
        public virtual string Type { get; set; }
        public virtual string Data { get; set; }
        public virtual int RefId { get; set; }
        public virtual DateTime CreatedDate { get; set; }
    }
}