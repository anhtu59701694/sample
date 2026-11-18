using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Claw.Modules.Eshop.Domain
{
    public partial class EshopOwner
    {
        public virtual int Id { get; set; }

        public virtual Guid CustomerId { get; set; }

        public virtual string CompanyName { get; set; }

        public virtual string OrgNum { get; set; }

        public virtual string Address { get; set; }

        public virtual string PostNr { get; set; }

        public virtual string City { get; set; }

        public virtual string Country { get; set; }

        public virtual string ContactName { get; set; }

        public virtual string ContactEmail { get; set; }

        public virtual string ContactPhone { get; set; }

        public virtual string Information { get; set; }

        public virtual byte CshopVersion { get; set; }

        public virtual string PushUrl { get; set; }

        public virtual string Token { get; set; }

        public virtual DateTime RegisteredDate { get; set; }

        public virtual DateTime EffectiveDate { get; set; }

        public virtual DateTime ExpiredDate { get; set; }

        public virtual DateTime CreatedDate { get; set; }

        public virtual DateTime? ModifedDate { get; set; }
    }
}