using System;

namespace Claw.Modules.Eshop.Domain
{
    public partial class WishProduct 
    {
        public virtual int Id { get; set; }
        public virtual Interfaces.ICustomer Customer { get; set; }
        public virtual ProductBase Product { get; set; }
        public virtual DateTime CreatedDate { get; set; }
    }
}