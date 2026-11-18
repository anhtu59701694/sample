using System;

namespace Claw.Modules.Eshop.Domain
{
    public partial class Trace
    {
        public virtual int Id { get; set; }
        public virtual string Data { get; set; }
        public virtual DateTime CreatedDate { get; set; }
    }
}