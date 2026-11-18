
namespace Claw.Modules.Eshop.Domain
{
    public class ProductExtraInformation
    {
        public virtual int Id { get; set; }
        public virtual int ProductId { get; set; }
        public virtual string FieldName { get; set; }
        public virtual string FieldValue { get; set; }
        public virtual ProductBase Product { get; set; }
    }
}