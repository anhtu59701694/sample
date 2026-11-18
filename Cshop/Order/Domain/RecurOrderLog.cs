using System;

namespace Claw.Modules.Eshop.Domain
{
    public partial class RecurOrderLog
    {
        public virtual int Id { get; set; }
        public virtual RecurOrder Recurring { get; set; }
        public virtual string Content { get; set; }
        public virtual DateTime CreatedDate { get; set; }
        public virtual string AuditInfo { get; set; }
    }
}