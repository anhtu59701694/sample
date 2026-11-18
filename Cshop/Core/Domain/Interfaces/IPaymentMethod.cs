using Claw.Modules.Eshop.DAL;
using BasePayment=Eshop.Payments.BasePayment;

namespace Claw.Modules.Eshop.Domain.Interfaces
{
    public interface IPaymentMethod : IDom    
    {
        string Name { get; }
        Vat TaxRate { get; set; }

        bool IsOnlinePayment { get; }
        bool PaymentIsRegistered { get; }
        string PaymentSystemName { get; }
        int PaymentSystemId { get; }

        string GetNameEx(string lang = null);
        decimal GetFee(IDB db, string countryCode, decimal orderTotal);
        BasePayment.BasePaymentProcessor GetProcessor(BasePayment.IGatewaySetting settings = null, IOrder order = null, bool? giftcard = null);
    }
}