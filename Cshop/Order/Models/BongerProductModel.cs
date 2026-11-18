using System.ComponentModel.DataAnnotations;

namespace Claw.Modules.Eshop.Models
{
    public class BongerProductModel
    {
        public int Id { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = StringTable.ErrMsgMinLimit)]
        public int Quantity { get; set; }

        public int? BongerId { get; set; }

        [Required(ErrorMessage = StringTable.ErrMsgRequiredField)]
        [Display(Name = "Product")]
        public int? ProductId { get; set; }

        public int? VariantId { get; set; }

        [Display(Name = "Image")]
        public ImageModel ProductImage { get; set; }

        [Display(Name = "Name")]
        [Required(ErrorMessage = StringTable.ErrMsgRequiredField)]
        [StringLength(400, ErrorMessage = StringTable.ErrMsgLengthLimit)]
        public string ProductName { get; set; }

        [Display(Name = "SKU")]
        public string ProductSku { get; set; }

        public bool IsDataModelEmpty
        {
            get { return (ProductId ?? 0) == 0; }
        }
    }
}