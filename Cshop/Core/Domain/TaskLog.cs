using System;
namespace Claw.Modules.Eshop.Domain
{
    public partial class TaskLog
    {
        public virtual int Id { get; set; }
        public virtual Task Task { get; set; }
        public virtual string Data { get; set; }
        public virtual DateTime CreatedDate { get; set; }
    }
}