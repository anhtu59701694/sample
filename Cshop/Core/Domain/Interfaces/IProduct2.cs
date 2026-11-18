namespace Claw.Modules.Eshop.Domain.Interfaces
{
    public interface IProduct2 : IDom, IDisplayOrder
    {
        int? QtyInOrders { get; set; }
        int? Orders { get; set; } 
    }
}