using System;

namespace Claw.Modules.Eshop.Domain
{
    public partial class ShippingBooking
    {
        public virtual int Id { get; set; }
        public virtual int Service { get; set; }
        public virtual int? OrderId { get; set; }
        public virtual decimal? Amount { get; set; }
        public virtual string Currency { get; set; }
        public virtual int? WidthInCm { get; set; }
        public virtual int? HeightInCm { get; set; }
        public virtual int? LengthInCm { get; set; }
        public virtual decimal? WeightInKg { get; set; }
        public virtual string GoodsDescription { get; set; }
        public virtual int? NumberOfItems { get; set; }
        public virtual string CustomerNum { get; set; }
        public virtual int? CustomerId { get; set; }
        public virtual string ProductId { get; set; }
        public virtual bool? ServiceCod { get; set; }
        public virtual bool? Rfid { get; set; }
        public virtual string RecipientPostalCode { get; set; }
        public virtual string RecipientCity { get; set; }
        public virtual string RecipientName { get; set; }
        public virtual string RecipientAddress { get; set; }
        public virtual string RecipientCountryCode { get; set; }
        public virtual bool? RecipientNoti { get; set; }
        public virtual string RecipientNotiEmail { get; set; }
        public virtual string RecipientNotiMobile { get; set; }
        public virtual string CodAccountNumber { get; set; }
        public virtual string Data { get; set; }
        public virtual string Result { get; set; }
        public virtual string BookingId { get; set; }
        public virtual string LabelUrl { get; set; }
        public virtual string TrackingUrl { get; set; }
        public virtual string WaybillUrl { get; set; }
        public virtual DateTime CreatedDate { get; set; }
    }
}