using System;
using System.ComponentModel.DataAnnotations;

namespace Claw.Modules.Eshop.Models
{
    [Flags]
    public enum LogActionType
    {
        Product = 1,
        Order = 2,
        Category = 3,
        User = 4,
        Discount = 5,
        [Display(Name = "Customer product")]
        CustProduct = 6,

        [Display(Name = "Product variant")]
        ProdVariant = 32,

        Add = 128,
        Edit = 129,
        Delete = 130
    };
}