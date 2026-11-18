using System.Collections.Generic;

namespace Claw.Modules.Eshop.Domain.Interfaces
{
    public interface IOrderDetail : IDom
    {
        string ProductSKU { get; set; }
        string ProductName { get; set; }

        int Quantity {get;}
        int? StockQty { get; set; }
        string Data { get; set; }

        IProduct GetProduct();
        IVariant GetVariant();
        string GetProductUrl();
        string GetProductImage();
        List<IOrderDetailBundle> GetOrderDetailBundles();
    }
}