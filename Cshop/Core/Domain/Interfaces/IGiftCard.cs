using System;
using Claw.Modules.Eshop.DAL;

namespace Claw.Modules.Eshop.Domain.Interfaces
{
    public interface IGiftCard : IDom
    {
        string Code { get; set; }
        string BarCode { get; set; }
        decimal Amount { get; set; }
        decimal Balance { get; set; }
        DateTime? ExpiredDate { get; set; }
        bool Active { get; set; }
    }
}

namespace Claw.Modules.Eshop.Domain
{
    public partial class GiftCardOrder
    {
        public virtual string Code { get; set; }

        public virtual decimal Amount { get; set; }

        public virtual decimal Balance { get; set; }

        public virtual DateTime? ExpiredDate { get; set; }

        /// <summary>Amount used in the current order</summary>
        public virtual decimal AmountUsed { get; set; }
    }

    public class GiftCardOrderItem
    {
        public virtual string Name { get; set; }

        public virtual string Email { get; set; }

        public virtual int? Amount { get; set; }

        public virtual string Message { get; set; }
    }  
}