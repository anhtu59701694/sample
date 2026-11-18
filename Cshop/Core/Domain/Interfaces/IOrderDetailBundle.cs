namespace Claw.Modules.Eshop.Domain.Interfaces
{
    public interface IOrderDetailBundle : IDom
    {
        string ProductSKU { get; set; }
        string ProductName { get; set; }

        int Quantity {get;}
        int? StockQty { get; set; }

        IOrderDetail GetOrderDetail();
        IProduct GetProduct();
        IVariant GetVariant();
    }
}