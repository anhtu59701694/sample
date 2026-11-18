using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;

namespace Claw.Modules.Eshop.Domain {

    public partial class Category : Tuple<CategoryLang, CategoryProduct, Category, IDom, IDom, IDom, IDom>, IDom, IDisplayOrder
    {
        private List<CategoryLang> _languages = null;

        public virtual int Id { get; set; }
        public virtual string Alias { get; set; }
        public virtual string Image { get; set; }
        public virtual string Banner0 { get; set; }
        public virtual string Banner1 { get; set; }
        public virtual string Banner2 { get; set; }
        public virtual string BannerUrl0 { get; set; }
        public virtual string BannerUrl1 { get; set; }
        public virtual string BannerUrl2 { get; set; }
        public virtual byte Visibility { get; set; }
        public virtual byte VisibilityUpPath { get; set; }
        public virtual int DisplayOrder { get; set; }
        public virtual Category Parent { get; set; }
        public virtual int? Sorting { get; set; }
        public virtual int? PagingSz { get; set; }
        public virtual bool? IncSubCate { get; set; }
        public virtual DateTime PublishedDate { get; set; }
        public virtual DateTime ModifiedDate { get; set; }
        public virtual DateTime CreatedDate { get; set; }

        public virtual int? Deep { get; set; }
        public virtual bool? UpPathHasLang { get; set; }
        public virtual bool? AncestorLangVisible { get; set; }
        public virtual string HierOrder { get; set; }

        public virtual IList<CategoryLang> LanguagesFe
        {
            get { return _languages ?? (_languages = Languages.ToList()); }
        }

        public virtual IList<CategoryLang> Languages
        {
            get { return Get(ref _list0); } 
            set { _list0 = value; }
        }

        public virtual IList<CategoryProduct> CategoryProducts
        {
            get { return Get(ref _list1); }
            set { _list1 = value; }
        }

        public virtual IList<Category> Children
        {
            get { return Get(ref _list2); }
            set { _list2 = value; }
        }

        public virtual IList<IDom> CategoryCoupons
        {
            get { return Get(ref _list3); }
            set { _list3 = value; }
        }

        public virtual IList<IDom> CategoryDiscounts
        {
            get { return Get(ref _list4); }
            set { _list4 = value; }
        }

        public virtual IList<IDom> CategoryCustomers
        {
            get { return Get(ref _list5); }
            set { _list5 = value; }
        }
        public virtual IList<IDom> CategoryCustGroups
        {
            get { return Get(ref _list6); }
            set { _list6 = value; }
        }
    }
}
