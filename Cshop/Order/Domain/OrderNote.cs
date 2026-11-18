using System;

namespace Claw.Modules.Eshop.Domain
{
    public partial class OrderNote
    {
        public virtual int Id { get; set; }
        public virtual Order Order { get; set; }
        public virtual string Subject { get; set; }
        public virtual string Body { get; set; }
        public virtual DateTime CreatedDate { get; set; }
        public virtual bool Private { get; set; }
        public virtual int Flags { get; set; }
        public virtual byte? OrderStatus { get; set; }
        public virtual string AuditInfo { get; set; }
    }
}