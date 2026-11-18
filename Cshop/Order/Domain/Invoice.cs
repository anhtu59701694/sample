using System;
namespace Claw.Modules.Eshop.Domain
{
    public partial class Invoice
    {
        public virtual int Id { get; set; }

        public virtual string Data { get; set; }

        public virtual Order Order { get; set; }

        public virtual DateTime CreatedDate { get; set; }

        public virtual int? pos_ShopID { get; set; }

        public virtual string Signature { get; set; }

        public virtual int? SignData { get; set; }
    }
}