using Claw.Modules.Eshop.DAL;
using System;

namespace Claw.Modules.Eshop.Domain.Interfaces
{
    public interface IDiscount : IDom
    {
        string Name { get; }
        int BonusType { get; }
        decimal BonusValue { get; }
        bool Active { get; }
        bool LoggedInUserOnly { get; }
        bool Apply2InstockOnly { get; }
        int? Scope { get; }
        bool IsScopeWeb { get; }
        bool IsScopePos { get; }
        DateTime CreatedDate { get; }

        decimal? Validate(decimal orderTotal, int itemTotal);
        dynamic Validate(IDB db, int productid, int? variantid, int quantity);
    }
}