using System.ComponentModel.DataAnnotations;
using System.Web.Script.Serialization;

namespace Claw.Modules.Eshop.Models
{
    public class CategoryCustGroupModel
    {
        public CategoryCustGroupModel() { }

        public CategoryCustGroupModel(Domain.Category cate)
        {
            if (cate == null) return;

            CategoryId = cate.Id;
            CategoryName = cate.GetName();
            UpPath = cate.HierOrder(false);
        }

        public int Id { get; set; }

        public int? CategoryId { get; set; }

        public string CategoryName { get; set; }

        public string UpPath { get; set; }

        public int? GroupId { get; set; }

        #region IsDataModelEmpty

        [ScriptIgnore(ApplyToOverrides = true)]
        public bool IsDataModelEmpty
        {
            get { return (CategoryId ?? 0) == 0; }
        }

        #endregion
    }
}