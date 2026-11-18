using System.Collections.Generic;

namespace Claw.Modules.Eshop.Domain.Interfaces
{
    public interface IShipping : IDom
    {
        string Name { get; }
        Vat Vat { get; set; }
        int ServiceProvider { get; set; }

        decimal GetFeeRaw(decimal orderTotal, decimal weightTotal, string countryCode, DAL.IDB db = null, decimal extraCostTotal = 0);

        IList<IShippingPayment> ShippingPayments { get; set; }
        bool IsZipCodeRequired();
        bool IsBringService();
        bool IsStorePickup();
    }
}