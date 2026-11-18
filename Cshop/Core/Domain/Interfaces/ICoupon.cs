using Claw.Modules.Eshop.DAL;

namespace Claw.Modules.Eshop.Domain.Interfaces
{
    public interface ICoupon : IDom
    {
        string Code { get; set; }
        string Name { get; set; }
        int DiscountType { get; set; }
        bool Exclusive { get; set; }

        decimal? Validate(IDB db, decimal orderTotal, string email);

        decimal? Validate(IDB db, string countryCode,
            int productid, int? variantid, decimal orderTotal, string email, int delta = 1, decimal discountValue = 0, decimal? price = null);
    }
}