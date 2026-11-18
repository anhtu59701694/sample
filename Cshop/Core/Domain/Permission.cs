using System;

namespace Claw.Modules.Eshop.Domain
{
    public partial class Permission
    {
        public virtual int Id { get; set; }
        public virtual Claw.Core.Domain.Role Role { get; set; }
        public virtual long Bitmask { get; set; }
        public virtual DateTime ModifiedDate { get; set; }
        public virtual DateTime CreatedDate { get; set; }
    }
}