namespace Claw.Modules.Eshop.Domain.Interfaces
{
    public interface IShippingPayment : IDom
    {
        IShipping Shipping { get; set; }
        IPaymentMethod PaymentMethod { get; set; }
    }
}