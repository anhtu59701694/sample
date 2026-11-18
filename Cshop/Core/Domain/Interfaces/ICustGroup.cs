using System.Collections.Generic;

namespace Claw.Modules.Eshop.Domain.Interfaces
{
    public interface ICustGroup : IDom
    {
        string Name { get; set; }
        int? DiscountNum { get; set; }
        bool Price4Login { get; set; }
        bool Discount { get; set; }
        bool? AnyCategory { get; set; }
        decimal? DiscountRate { get; set; }

        List<IDom> GetDiscountCategories();
    }
}