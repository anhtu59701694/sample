using System.Linq;
using Claw.Modules.Eshop.Domain.Interfaces;
using System.Collections.Generic;
namespace Claw.Modules.Eshop.Domain
{
    public partial class CustGroup : Tuple<CustomerCGroup, IDom, CategoryCustGroup>, ICustGroup
    {
        public virtual int Id { get; set; }
        public virtual string Name { get; set; }
        public virtual int? CustomerNum { get; set; }
        public virtual int? DiscountNum { get; set; }
        public virtual bool Price4Login { get; set; }

        /// <summary>Product group discount</summary>
        public virtual bool Discount { get; set; }
        
        public virtual bool? AnyCategory { get; set; }
        public virtual decimal? DiscountRate { get; set; }

        public virtual IList<CustomerCGroup> CustomerCGroups
        {
            get { return Get(ref _list0); }
            set { _list0 = value; }
        }

        public virtual IList<IDom> DiscountCGroups
        {
            get { return Get(ref _list1); }
            set { _list1 = value; }
        }

        public virtual IList<CategoryCustGroup> CategoryCustGroups
        {
            get { return Get(ref _list2); }
            set { _list2 = value; }
        }

        public virtual List<IDom> GetDiscountCategories()
        {
            return GetDiscountCategories(false);
        }

        public virtual List<IDom> GetDiscountCategories(bool includeParent, bool nocache = false)
        {
            var obj = DAL.CustGroupMgr.GetDom(new DAL.DefaultDb(), Id);

            if (obj == null) return new List<IDom>();

            var ret = new List<Category>();

            obj.CategoryCustGroups.Action(m =>
            {
                ret.Add(m.Category);
                m.Category.GetDescendants(ret, nocache);
                if (includeParent)
                {
                    var par = m.Category.Parent;
                    while (par != null) { ret.Add(par); par = par.Parent; }
                }
            });

            return ret.Cast<IDom>().ToList();
        }
    }
}