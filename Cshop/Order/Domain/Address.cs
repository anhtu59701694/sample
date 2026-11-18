using System;

namespace Claw.Modules.Eshop.Domain
{
    public partial class Address 
    {
        public virtual int Id { get; set; }
        public virtual string Name { get; set; }
        public virtual string Phone { get; set; }
        public virtual string Mobile { get; set; }
        public virtual string Street { get; set; }
        public virtual string Postnr { get; set; }
        public virtual string Posted { get; set; }
        public virtual string City { get; set; }
        public virtual Country Country { get; set; }
        public virtual Customer Customer { get; set; }
        public virtual DateTime CreatedDate { get; set; }

    }
    public partial class AddressEx : Address
    {
        internal bool? IsDefault { get; set; }
    }
}