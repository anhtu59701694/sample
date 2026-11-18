using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Claw.Modules.Eshop.Domain;

namespace Claw.Modules.Eshop.Models
{
    public enum BongerStatus : byte
    {
        [Display(Name = "Uløst")]
        Pending = 0,

        [Display(Name = "Løst")]
        Resolved = 1,

        [Display(Name = "Venter på opphent fra kunde")]
        WaitCustPickup = 2,

        [Display(Name = "Venter på tilbakemelding/tilbakelevering av produkt fra leverandør")]
        WaitSuplierResp = 3
    }

    public class BongerModel : BaseExModel<Domain.Bonger>
    {
        [Required(ErrorMessage = StringTable.ErrMsgRequiredField)]
        [StringLength(100, ErrorMessage = StringTable.ErrMsgLengthLimit)]
        public string Name { get; set; }

        [StringLength(50, ErrorMessage = StringTable.ErrMsgLengthLimit)]
        public string Phone { get; set; }

        [StringLength(150, ErrorMessage = StringTable.ErrMsgLengthLimit)]
        public string Email { get; set; }

        [StringLength(100, ErrorMessage = StringTable.ErrMsgLengthLimit)]
        public string Address { get; set; }

        [StringLength(20, ErrorMessage = StringTable.ErrMsgLengthLimit)]
        public string Postnr { get; set; }

        [StringLength(50, ErrorMessage = StringTable.ErrMsgLengthLimit)]
        public string Posted { get; set; }

        [StringLength(64000, ErrorMessage = StringTable.ErrMsgLengthLimit)]
        public string Notes { get; set; }
        
        [StringLength(64000, ErrorMessage = StringTable.ErrMsgLengthLimit)]
        public string NoteLogs { get; set; }

        public byte? Status { get; set; }

        public string StatusTxt { get { return typeof (BongerStatus).LocalizeEnumValue(Status ?? 0); } }

        [Display(Name = "Modified date")]
        public ODateTime ModifiedDate { get; set; }

        [Display(Name = "Created date")]
        public ODateTime CreatedDate { get; set; }

        [Display(Name = "Country")]
        public int? CountryId { get; set; }

        public int? CustomerId { get; set; }

        public List<BongerProductModel> Products { get; set; }

        #region InitProducts
        public void InitProducts()
        {
            Products = _raw.BongerProducts.Select(m => new BongerProductModel()
            {
                Id = m.Id,
                BongerId = m.Bonger.Id,
                ProductId = m.Product.Id,
                ProductImage = m.Product.GetImage(m.Variant),
                ProductName = m.ProductName,
                ProductSku = m.Variant == null ? m.Product.SKU : m.Variant.SKU,
                Quantity = m.Quantity,
            }).ToList();
        } 
        #endregion
    }
}