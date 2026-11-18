using System.ComponentModel.DataAnnotations;
using System.Web.Script.Serialization;

namespace Claw.Modules.Eshop.Models
{
    public class CategoryCustomerModel
    {
        public CategoryCustomerModel() { }

        public CategoryCustomerModel(Domain.Category cate)
        {
            if (cate == null) return;

            CategoryId = cate.Id;
            CategoryName = cate.GetName();
            UpPath = cate.HierOrder(false);
        }

        public CategoryCustomerModel(Domain.Customer cust)
        {
            if (cust == null) return;

            CustomerId = cust.Id;
            CustomerName = cust.ContactName.IfNull(cust.DeliverName);
            CustomerEmail = cust.Email;
        }

        public int Id { get; set; }

        public int? CategoryId { get; set; }

        public int? CustomerId { get; set; }

        [Display(Name = "Discount")]
        [Required(ErrorMessage = StringTable.ErrMsgRequiredField)]
        [Range(typeof(OFloat),"0","100",ErrorMessage = StringTable.ErrMsgRangeLimit)]
        public OFloat DiscountRate { get; set; }

        [StringLength(100, ErrorMessage = StringTable.ErrMsgLengthLimit)]
        public string Notes { get; set; }

        public string CategoryName { get; set; }

        public string UpPath { get; set; }

        [Display(Name = "Name")]
        public string CustomerName { get; set; }

        [Display(Name = "Email")]
        public string CustomerEmail { get; set; }

        #region IsDataModelEmpty

        [ScriptIgnore(ApplyToOverrides = true)]
        public bool IsDataModelEmpty
        {
            get { return DiscountRate.IsNull() || DiscountRate == 0m || (CategoryId ?? 0) == 0; }
        }

        #endregion
    }
}