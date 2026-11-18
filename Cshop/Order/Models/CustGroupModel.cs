using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web.Script.Serialization;
using Claw.Modules.Eshop.DAL;
using Newtonsoft.Json;

namespace Claw.Modules.Eshop.Models
{
    public class CustGroupModel : BaseExModel<Domain.CustGroup>
    {
        [Required(ErrorMessage = StringTable.ErrMsgRequiredField)]
        [StringLength(100, ErrorMessage = StringTable.ErrMsgLengthLimit)]
        public string Name { get; set; }

        public int? Customers { get; set; }

        public int? Discounts { get; set; }

        [Display(Name = "Show product price")]
        public bool Price4Login { get; set; }

        [Display(Name = "Enable discount")]
        public bool Discount { get; set; }

        [Display(Name = "Any category")]
        public bool? AnyCategory { get; set; }

        [Display(Name = "Discount rate")]
        public OFloat DiscountRate { get; set; }

        [ScriptIgnore]
        [Display(Name = "Discount categories")]
        public List<CategoryCustGroupModel> Categories { get; set; }

        #region InitCategories

        public void InitCategories(IDB db, string categories = null, bool mergeable = false)
        {
            if (!categories.IsNullOrEmpty())
            {
                Categories = new List<CategoryCustGroupModel>();

                var cates = JsonConvert.DeserializeAnonymousType(categories, new[] { new { Id = 0 } });
                if (cates.SafeCount() != 0)
                    foreach (var cate in cates)
                    {
                        var cateId = cate.Id;
                        var itm = Categories.FirstOrDefault(z => z.CategoryId == cateId);

                        if (itm == null) Categories.Add(new CategoryCustGroupModel(CategoryMgr.GetDom(db, cate.Id)) { GroupId = Id });
                    }
            }
            else
                if (mergeable)
                {
                    Categories = Categories ?? new List<CategoryCustGroupModel>();

                    foreach (var itm in Categories)
                    {
                        var cate = CategoryMgr.GetDom(db, itm.CategoryId ?? 0);
                        if (cate == null) continue;
                        itm.CategoryName = cate.GetName();
                        itm.UpPath = cate.HierOrder(false);
                    }
                }
                else Categories = _raw.CategoryCustGroups.Select(m => new CategoryCustGroupModel(m.Category){ Id = m.Id, GroupId = m.Group.Id }).ToList();
        }

        #endregion

    }
}