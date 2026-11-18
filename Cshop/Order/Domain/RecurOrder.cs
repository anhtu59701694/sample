using System;
using System.Collections.Generic;

namespace Claw.Modules.Eshop.Domain
{
    public partial class RecurOrder : Tuple<Order, RecurOrderLog>
    {
        public virtual int Id { get; set; }
        public virtual Order Order { get; set; }
        public virtual string OrderJson { get; set; }
        public virtual DateTime FromDate { get; set; }
        public virtual DateTime? ToDate { get; set; }
        public virtual int Recurring { get; set; }
        public virtual int RecurEach { get; set; }  
        public virtual int? SrcRecurring { get; set; }
        public virtual int? SrcRecurEach { get; set; }
        public virtual long RecurData { get; set; }
        public virtual byte Status { get; set; }
        public virtual DateTime? LastRunTime { get; set; }
        public virtual DateTime? NextRunning { get; set; }
        public virtual bool? LastRunOk { get; set; }
        public virtual string LastRunMsg { get; set; }
        public virtual int? LastRunOrderId { get; set; }
        public virtual DateTime CreatedDate { get; set; }
        public virtual string PaymentData { get; set; }
        public virtual int? OrderNum { get; set; }
        public virtual RecurOrder NextRecurOrder { get; set; }

        public virtual IList<Order> Orders 
        {
            get { return Get(ref _list0); }
            set { _list0 = value; }
        }

        public virtual IList<RecurOrderLog> RecurOrderLogs
        {
            get { return Get(ref _list1); }
            set { _list1 = value; }
        }
    }
}