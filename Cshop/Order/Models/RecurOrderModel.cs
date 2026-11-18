using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using NHibernate.Linq;

namespace Claw.Modules.Eshop.Models
{
    public class RecurOrderModel : BaseExModel<Domain.RecurOrder>
    {
        [Display(Name = "Original order id")]
        [Required(ErrorMessage = StringTable.ErrMsgRequiredField)]
        public int? OrderId { get; set; }

        public string OrderJson { get; set; }

        public OrderModel NextOrder { get; set; }

        [Display(Name = "Name")]
        public string BillingName { get { return _raw == null || _raw.Order == null ? null : _raw.Order.BillingName; } }

        #region BillingInfo
        [Display(Name = "Billing")]
        public string BillingInfo
        {
            get
            {
                return _raw == null || _raw.Order == null ? null : string.Format("{0} {1} {2} {3}",
                        string.IsNullOrEmpty(_raw.Order.BillingAddress) ? "" : _raw.Order.BillingAddress + ",",
                        _raw.Order.BillingPostnr,
                        _raw.Order.BillingPosted,
                        string.IsNullOrEmpty(_raw.Order.BillingCity) ? "" : "," + _raw.Order.BillingCity);
            }
        }
        #endregion

        [Display(Name = "Payment name")]
        public string PaymentName { get { return _raw == null || _raw.Order == null ? null : _raw.Order.PaymentName; } }

        [Display(Name = "Shipping name")]
        public string ShippingName { get { return _raw == null || _raw.Order == null ? null : _raw.Order.ShippingName; } }

        [Display(Name = "Started date")]
        [Required(ErrorMessage = StringTable.ErrMsgRequiredField)]
        public ODateTime FromDate { get; set; }

        [Display(Name = "Expired date")]
        public ODateTime ToDate { get; set; }

        [Required(ErrorMessage = StringTable.ErrMsgRequiredField)]
        public int? Recurring { get; set; }

        [Display(Name = "Recurring")]
        public string RecurringTxt { get { return typeof(RecurringType).LocalizeEnumValue(Recurring ?? 0); } }

        [Display(Name = "Repeat each")]
        [Required(ErrorMessage = StringTable.ErrMsgRequiredField)]
        [Range(1,1000,ErrorMessage = StringTable.ErrMsgRangeLimit)]
        public int? RecurEach { get; set; }

        public string RecurEachTxt
        {
            get
            {
                return string.Format("{0} {1} {2}", StringTable.RecuringEach, RecurEach, StringTable.RecurEach((RecurringType) (Recurring ?? 0)).ToLower());
            }
        }

        public long? RecurData { get; set; }

        [Display(Name = "Next running")]
        public ODateTime NextRunning { get; set; }

        public byte? Status { get; set; }

        [Display(Name = "Status")]
        public string StatusTxt { get { return typeof(RecurOrderStatus).LocalizeEnumValue(Status ?? 0); } }

        [Display(Name = "Last run time")]
        public ODateTime LastRunTime { get; set; }

        [Display(Name = "Last run")]
        public bool? LastRunOk { get; set; }

        [Display(Name = "Last run log")]
        public string LastRunMsg { get; set; }

        [Display(Name = "Last run order id")]
        public int? LastRunOrderId { get; set; }

        [Display(Name = "Payment data")]
        public string PaymentData { get; set; }

        [Display(Name = "Next recuring order")]
        public RecurOrderModel NextRecurOrder { get; set; }

        [Display(Name = "Created date")]
        public ODateTime CreatedDate { get; set; }

        [Display(Name = "Orders")]
        public int? OrderNum { get; set; }

    }
}