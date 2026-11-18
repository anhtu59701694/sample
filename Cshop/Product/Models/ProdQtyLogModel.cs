using System;
using System.ComponentModel.DataAnnotations;
using System.Web.Mvc;
using System.Web.Script.Serialization;
using Claw.Core;
using Claw.Core.DAL;
using Claw.Modules.Eshop.DAL;
using Newtonsoft.Json;

namespace Claw.Modules.Eshop.Models
{
    public enum ProdQtyLogReason
    {
        [Display(Name = "Order")] Ordered = 1,
        [Display(Name = "Got in from supplier")] Supplied = 2,
        [Display(Name = "Kreditnote")] Credited = 3,
        [Display(Name = "Cancelled")] Cancelled = 4,
        [Display(Name = "Imported")] Imported = 5,
        [Display(Name = "Bundle")] Bundled = 6,
        [Display(Name = "Bundle quantity increased automatically")] AutoQtyBundle = 7,
        [Display(Name = "First creation")] FirstCreation = 8,
        [Display(Name = "Confirm quantity")] ConfirmQty = 9,
        [Display(Name = "Drop shipping")] DropShip = 10
    }

    [CustomValidation(typeof(ProdQtyLogModel), "Validate")]
    public class ProdQtyLogModel
    {
        public static ProdQtyLogReason[] HiddenReasons = new[] { ProdQtyLogReason.FirstCreation, ProdQtyLogReason.Imported, ProdQtyLogReason.AutoQtyBundle };

        public int Id { get; set; }

        public int? Quantity { get; set; }

        [Display(Name = "Stock qty")]
        public int? StockQty { get; set; }


        [Display(Name = "Lokalt stk.")]
        public int? StockQty1 { get; set; }


        [Display(Name = "Fjern stk.")]
        public int? StockQty2 { get; set; }

        [Display(Name = "Storage <br/>location")]
        public int? QtyPos { get; set; }

        [JsonIgnore]
        [ScriptIgnore]
        [Display(Name = "Storage location")]
        public string QtyPosTxt { get { return QtyPos == null ? null : StringTable.GetQtyPosName((Domain.QtyPos)QtyPos, null); } }

        public int? Reason { get; set; }

        [Display(Name = "Reason")]
        public string ReasonTxt { get { return typeof(ProdQtyLogReason).LocalizeEnumValue(Reason ?? 0); } }

        public string Note { get; set; }

        [Display(Name = "Ref id")]
        public int? RefId { get; set; }

        public Func<int?> RefIdFunc { get; set; }

        [Display(Name = "Created date")]
        public ODateTime CreatedDate { get; set; }

        public int? ProductId { get; set; }

        public int? VariantId { get; set; }

        [Display(Name = "Variant")]
        public string VariantName { get; set; }

        #region Validate
        public static ValidationResult Validate(ProdQtyLogModel obj, ValidationContext cxt)
        {
            if (obj == null) return null;

            return null;
        }
        #endregion
    }
}