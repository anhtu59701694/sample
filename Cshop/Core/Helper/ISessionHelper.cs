using System;
using System.Collections.Generic;
using Claw.Modules.Eshop.Domain.Interfaces;

namespace Claw.Modules.Eshop.Helper
{
    public interface ISessionHelper
    {
        int? GetUserId(out bool showPrice, out decimal? discountRate, out Dictionary<int, decimal> cateDiscountRates);
        int? GetUserId(out bool showPrice, out decimal? discountRate, out Dictionary<int, decimal> cateDiscountRates, out List<ICustGroup> groups);
        int? GetUserId(out bool showPrice, out decimal? discountRate, out Dictionary<int, decimal> cateDiscountRates, out List<ICustGroup> groups, out DateTime? modifiedDate, bool checkCombineScope);
        int? GetUserId(object userInfo, out bool showPrice, out decimal? discountRate, out Dictionary<int, decimal> cateDiscountRates, out List<ICustGroup> groups, out DateTime? modifiedDate, bool checkCombineScope);
    }
} 