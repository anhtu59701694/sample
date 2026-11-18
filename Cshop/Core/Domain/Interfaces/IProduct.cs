namespace Claw.Modules.Eshop.Domain.Interfaces
{
    public interface IProduct : IDom, IDisplayOrder
    {
        int? OrderDetailNum { get; set; }  
    }
    public interface IVariant : IDom
    {
    }
}