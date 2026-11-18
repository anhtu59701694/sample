using System;
namespace Claw.Modules.Eshop.Domain
{
    public partial class Cart
    {
        public static readonly long Seed = 5000000000;

        public virtual int Id { get; set; }

        public virtual string Data { get; set; }

        public virtual string DataCpos { get; set; }

        public virtual string PaymentData { get; set; }

        public virtual Customer Customer { get; set; }

        public virtual DateTime CreatedDate { get; set; }

        public virtual bool Notified { get; set; }

        public virtual string LangCode { get; set; }

        public virtual string Currency { get; set; }

        public virtual long OrderId { get { return Seed + Id; } }

        public virtual string Token { get; set; }

        public virtual DateTime? ExpiredDate { get; set; }

        public virtual int? RefOrderId { get; set; }

        public virtual DateTime? LastView { get; set; }

        public virtual DateTime? EmailView { get; set; }

        public virtual bool? HideDiscPerProd { get; set; }
    }
}