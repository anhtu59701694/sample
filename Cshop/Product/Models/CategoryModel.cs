using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Dynamic;
using System.Linq;
using Claw.Core;
using Claw.Core.DAL;
using Claw.Modules.Eshop.Controllers.Admin;
using Claw.Modules.Eshop.Domain;
using Claw.Modules.Eshop.DAL;
using System.Web.Mvc;
using AutoMapper;

namespace Claw.Modules.Eshop.Models
{
    [CustomValidation(typeof(CategoryModel), "Validate")]
    public class CategoryModel : BaseModel<Category, CategoryLangModel>, Domain.IDisplayOrder
    {
        #region StatInfo
        public class StatInfo
        {
            [Display(Name = "Sub category")]
            public int Children { get; set; }
            public string ChildrenLink { get; set; }

            [Display(Name = "Products")]
            public int Products { get; set; }
            public string ProductsLink { get; set; }

            [Display(Name = "Products in sub-categories")]
            public int ProductsSub { get; set; }
            public string ProductsSubLink { get; set; }

            /// <summary>phrase only</summary>
            [Display(Name = "Danh sách sản phẩm có trạng thái '{0}' trong '{1}' và các danh mục con")]
            public object StatusStat { get; set; }

            /// <summary>
            /// 2017.05.25
            /// </summary>
            [Display(Name = "Products bundle")]
            public int ProductsBundle { get; set; }
            public string ProductsBundleLink { get; set; }

            [Display(Name = "Products bundle in sub-categories")]
            public int ProductsBundleSub { get; set; }
            public string ProductsBundleSubLink { get; set; }

            /// <summary>phrase only</summary>
            [Display(Name = "Danh sách sản phẩm có trạng thái '{0}' trong '{1}' và các danh mục con")]
            public object StatusStatBundle { get; set; }
        }
        #endregion

        public const int MaxDeep = 10;

        private string _hierOrder;

        public CategoryModel()
        {
            Visibility = VisibilityType.Web; DisplayOrder = 1000;
            PublishedDate = DateTime.Now;
        }

        public string Image { get; set; }

        [Display(Name = "Photo")]
        public string ImageThumb { get; set; }

        public string ImageFsUrl { get; set; }

        public string Banner0 { get; set; }

        [Display(Name = "Banner 0")]
        public string Banner0Thumb { get; set; }

        public string Banner0FsUrl { get; set; }

        public string Banner1 { get; set; }

        [Display(Name = "Banner 1")]
        public string Banner1Thumb { get; set; }

        public string Banner1FsUrl { get; set; }

        public string Banner2 { get; set; }

        [Display(Name = "Banner 2")]
        public string Banner2Thumb { get; set; }

        public string Banner2FsUrl { get; set; }

        [Display(Name = "Url")]
        public string BannerUrl0 { get; set; }

        [Display(Name = "Url")]
        public string BannerUrl1 { get; set; }

        [Display(Name = "Url")]
        public string BannerUrl2 { get; set; }

        [Display(Name = "Sort order for products in category")]
        public int? Sorting { get; set; }

        [Range(1, 999, ErrorMessage = StringTable.ErrMsgRangeLimit)]
        [RegularExpression(@"^(\d*)$", ErrorMessage = StringTable.ErrMsgRequireNumber)]
        [Display(Name = "Number product per page")]
        public int? PagingSz { get; set; }

        [Display(Name = "Show product from sub-categories")]
        public bool? IncSubCate { get; set; }

        [Required(ErrorMessage = StringTable.ErrMsgRequiredField)]
        [Display(Name = "Visibility")]
        public VisibilityType Visibility { get; set; }

        #region VisibleWeb
        [Display(Name = "On webshop")]
        public bool VisibleWeb
        {
            get { return 0 != (Visibility & VisibilityType.Web); }
            set
            {
                if (value) Visibility |= VisibilityType.Web;
                else Visibility &= ~VisibilityType.Web;
            }
        }
        #endregion

        #region VisiblePos
        [Display(Name = "On pos")]
        public bool VisiblePos
        {
            get { return 0 != (Visibility & VisibilityType.Pos); }
            set
            {
                if (value) Visibility |= VisibilityType.Pos;
                else Visibility &= ~VisibilityType.Pos;
            }
        }
        #endregion

        [Required(ErrorMessage = StringTable.ErrMsgRequiredField)]
        [Range(0, 9999, ErrorMessage = StringTable.ErrMsgRangeLimit)]
        [Display(Name = "Sort order")]
        public int DisplayOrder { get; set; }

        [Display(Name = "Published Date")]
        public ODateTime PublishedDate { get; set; }

        [Display(Name = "Created Date")]
        public ODateTime CreatedDate { get; set; }

        public DateTime? ModifiedDate { get; set; }

        [Display(Name = "Parent category")]
        public int? ParentId { get; set; }

        public List<CategoryProductModel> Products { get; set; }
        public int ProductsSub { get; set; }

        [Display(Name = "Customer discounts")]
        public List<object> CustomerDiscounts { get; set; }

        #region Alias

        public string Alias
        {
            get { return GetLang(m => m.Alias); }
        }

        #endregion

        #region Name

        [Required(ErrorMessage = StringTable.ErrMsgRequiredField)]
        [StringLength(150, ErrorMessage = StringTable.ErrMsgLengthLimit)]
        [Display(Name = "Category name")]
        public string Name
        {
            get { return GetLang(m => m.Name); }
            set { SetLang(m => { m.Name = value; }); }
        }

        #endregion

        #region LangVisible

        [Display(Name = "Visibility")]
        public bool? LangVisible
        {
            get { return GetLang(m => m.Visible); }
        }

        #endregion

        public bool UpPathHasLang { get { return _raw.UpPathHasLang(LanguageMgr.FocusLangCode); } }

        public bool ParentLangVisible { get { return _raw.ParentLangVisible(); } }

        public int Deep() { return _raw.Deep(); }

        #region HierOrder

        public string HierOrder
        {
            get
            {
                if (_hierOrder == null) _hierOrder = _raw.HierOrder();
                return _hierOrder;
            }
        }

        #endregion

        public string GetHierOrderTxt() { return _raw.HierOrder(false); }

        public bool AnyChild { get { return _raw.AnyChild(); } }

        public bool GetChildrenCount() { return _raw.AnyChild(); }

        public bool AncestorLangVisible() { return _raw.AncestorLangVisible(); }

        [Display(Name = "Information")]
        public ExpandoObject Information { get; set; }

        #region InitProducts
        public void InitProducts(IDB db)
        {
            Products = _raw.GetProducts(db);
        }
        #endregion

        #region Validate
        public static ValidationResult Validate(CategoryModel obj, ValidationContext cxt)
        {
            if (obj == null) return null;

            if (obj.VisibleWeb && (obj.PublishedDate == null || obj.PublishedDate.Value == null))
                return new ValidationResult(StringTable.ErrMsgRequiredField, new[] { "PublishedDate" });

            return null;
        }
        #endregion
    }
}