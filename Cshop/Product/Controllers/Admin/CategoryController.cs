using System.ComponentModel.DataAnnotations;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using Claw.Controls.Attributes;
using Claw.Controls.Modules;
using Claw.Core;
using Claw.Core.Enumerations;
using Claw.Core.Extensions;
using Claw.Core.Interfaces;
using Claw.Core.Logs;
using Claw.Core.Utility;
using System;
using System.Collections.Generic;
using System.Web.UI.WebControls;
using System.Linq;
using System.Web;
using Claw.Modules.Eshop.Helper;
using Kendo.Mvc.Extensions;
using Kendo.Mvc.UI;
using System.Web.Mvc;
using Claw.Modules.Eshop.DAL;
using Claw.Modules.Eshop.Domain;
using Claw.Modules.Eshop.Models;
using System.Text.RegularExpressions;

namespace Claw.Modules.Eshop.Controllers.Admin
{
    [AdminPerm("CatalogBits", byte.MaxValue)]
    public class CategoryController : BaseController<CategoryModel, Category>
    {
        protected const string IndexView = "Admin/CategoriesIndex.cshtml";
        protected const string EditView = "Admin/CategoriesEdit.cshtml";

        #region CategoryController
        public CategoryController(IDataService ds) :
            base(ds)
        {
            InitImageSettings(x => x.Display.Category.Image, x => x.Display.Category.Icon);
        }
        #endregion

        #region Index
        [ValidateInput(false)]
        [AdminPerm("CatalogBits", PermissionMask.View)]
        public virtual ActionResult Index(int? categoryid, bool topcate = false)
        {
            ViewBag.Settings = GetSettings();
            ViewBag.Categories = CategoryMgr.GetAll(this, 0, topcate ? -1 : (int?)null).Where(x => x.AnyChild || x.Id == categoryid).ToList();
            ViewBag.Statuses = StatusMgr.GetAll(this, 1).ToList();

            if (categoryid != null && categoryid.Value != 0)
                ViewBag.Category = CategoryMgr.Get(this, categoryid.Value);

            ViewBag.EagerData = new GridEagerLoadDataHandler(List);

            return ClawView(IndexView);
        }
        #endregion

        #region List

        [ValidateInput(false)]
        [AdminPerm("CatalogBits", PermissionMask.View)]
        public ActionResult List([DataSourceRequest] DataSourceRequest request)
        {
            TrimReq(request);

            string sortField = "Id";
            var sortDir = SortDirection.Descending;

            GetSortField(request, ref sortField, ref sortDir);

            return JsonEx(List(request.Page, request.PageSize, sortField, sortDir));
        }

        protected DataSourceResult List(int page, int pageSize, string sortField, SortDirection sortDir)
        {
            int totalRows;
            var categoryid = GetParam<int?>("categoryid") ?? -1;

            string keyword = GetKeyword();

            var statuses = StatusMgr.GetAll(this, 1);
            var langCode = LanguageMgr.FocusLangCode;
            var items = CategoryMgr.GetAll(this, LanguageMgr.FocusLangCode, categoryid, null, page,
                pageSize, out totalRows, sortDir, sortField, keyword)
                .Action(m =>
                {
                    var statInfo = m.GetRaw().GetStatistics(this, Request, statuses);
                    m.InlineEdit = true;
                    m.Information = statInfo;
                    m.ProductsSub = statInfo.Get<int>("ProductsSub");
                });

            if (categoryid > 0) items = items.OrderBy(x => x.DisplayOrder).ThenByDescending(x => x.Id).ToList();

            return new DataSourceResult() { Data = items, Total = totalRows };
        }

        #endregion

        #region ListProduct
        [ValidateInput(false)]
        public ActionResult ListProduct(int categoryid, string status, string lang, string optlang, string mode)
        {
            return RedirectToAction("Index", "Product", new { categoryid, status, lang, optlang, mode });
        }
        [ValidateInput(false)]
        public ActionResult ListBundleProduct(int categoryid, string status, string lang, string optlang, string mode)
        {
            return RedirectToAction("Index", "BundleProduct", new { categoryid, status, lang, optlang, mode });
        }
        #endregion

        #region Input.Get
        [ValidateInput(false)]
        [AdminPerm("CatalogBits", PermissionMask.View)]
        public ActionResult Input(int id, string lang, string optlang, string mode)
        {
            var obj = CategoryMgr.Get(this, id);

            ViewBag.Languages = LanguageMgr.GetAll();
            ViewBag.Settings = GetSettings();
            ViewBag.Categories = CategoryMgr.GetAll(this, id, null);

            if (obj == null)
                obj = new CategoryModel()
                {
                    Visibility = (VisibilityType)GetSettings().Hidden.DefaultVisibility,
                    Languages = new List<CategoryLangModel>(),
                    Products = new List<CategoryProductModel>()
                };
            else
                if (BaseMgr.GetList<object>(this, string.Format("select count(*) from {0} where Category.Id = {1}", typeof(CategoryProduct).FullName, obj.Id)).FirstOrDefault().SafeChangeType<int>() < GetSettings().Hidden.CateEditMaxProducts) obj.InitProducts(this);
                else obj.Products = null;

            ViewBag.TemplProduct = new CategoryProductModel();

            return ClawView(EditView, obj);
        } 

        #endregion

        #region Input.Post

        [HttpPost]
        [ValidateInput(false)]
        //[ValidateAntiForgeryToken]
        [AdminPerm("CatalogBits", PermissionMask.Edit)]
        public ActionResult Input([DataSourceRequest]DataSourceRequest request, CategoryModel obj,
            string name, string lang, string mode)
        {
            if (obj == null) return RedirectToAction("Index", new { lang = lang, mode = mode });

            #region Languages

            foreach (var itm in obj.Languages
                .Select((x, idx) => new { Val = x, Index = idx })
                .Where(x => x.Val.IsDataModelEmpty).OrderByDescending(x => x.Index).ToList())
            {
                obj.Languages.Remove(itm.Val);
                ModelState.RemoveKey(string.Format("{0}[{{0}}]",
                    GetFieldName<CategoryModel, object>(m => m.Languages)),
                    itm.Index, obj.Languages.Count);
            }

            #endregion

            #region Products

            if (obj.Products != null && !obj.InlineEdit)
                foreach (var itm in obj.Products
                    .Select((x, idx) => new { Val = x, Index = idx })
                    .Where(x => x.Val.IsDataModelEmpty).OrderByDescending(x => x.Index).ToList())
                {
                    obj.Products.Remove(itm.Val);
                    ModelState.RemoveKey(string.Format("{0}[{{0}}]",
                        GetFieldName<CategoryModel, object>(m => m.Products)),
                        itm.Index, obj.Products.Count);
                }
            //else obj.Products = new List<CategoryProductModel>();

            #endregion

            #region InlineEdit

            if (obj.InlineEdit && obj.Id != 0)
            {
                var statuses = StatusMgr.GetAll(this, 1);
                var dom = CategoryMgr.GetDom(this, obj.Id);
                obj = CopyValidate(obj, CategoryMgr.Get(this, obj.Id), (nVal, oVal) =>
                {
                    nVal.InlineEdit = true;
                    nVal.Name = name;
                    nVal.DisplayOrder = oVal.DisplayOrder;
                    TryValidateModel(nVal.GetLang());
                });
                obj.Information = dom.GetStatistics(this, Request, statuses);
            }

            #endregion

            if (ModelState.ModLocalize().IsValid)
            {
                #region MoveFile
                try
                {
                    var tmpPath = ModuleUrl.TempFolder.ResolveClientUrl();
                    if (obj.ImageFsUrl != null && obj.ImageFsUrl.StartsWith(tmpPath))
                        obj.Image = MoveFile(ModuleUrl.ImageFolder, ModuleUrl.TempFolder, obj.Image);
                    if (obj.Banner0FsUrl != null && obj.Banner0FsUrl.StartsWith(tmpPath))
                        obj.Banner0 = MoveFile(ModuleUrl.ImageFolder, ModuleUrl.TempFolder, obj.Banner0);
                    if (obj.Banner1FsUrl != null && obj.Banner1FsUrl.StartsWith(tmpPath))
                        obj.Banner1 = MoveFile(ModuleUrl.ImageFolder, ModuleUrl.TempFolder, obj.Banner1);
                    if (obj.Banner2FsUrl != null && obj.Banner2FsUrl.StartsWith(tmpPath))
                        obj.Banner2 = MoveFile(ModuleUrl.ImageFolder, ModuleUrl.TempFolder, obj.Banner2);
                }
                catch (Exception er)
                {
                    LogErr(er); goto LB_ERR;
                }
                #endregion

                #region DownloadImage

                if (obj.Image != null && Regex.IsMatch(obj.Image, @"^http(s)?://"))
                {
                    var url = obj.Image;
                    if (SaveUrl(obj.Id + '-' + Regex.Replace(obj.Alias ?? "", "[^a-zA-Z0-9]+",""), ref url, ModuleUrl.ImageFolder, ModuleUrl.ThumbFolder))
                    {
                        obj.Image = url;
                    }
                    else
                    {
                        ModelState.AddModelError(m => m.Image, obj, StringTable.ErrMsgSaveImageUrl(obj.Image));
                        goto LB_ERR; 
                    }
                }

                if (obj.Banner0 != null && Regex.IsMatch(obj.Banner0, @"^http(s)?://"))
                {
                    var url = obj.Banner0;
                    if (SaveUrl(obj.Id + '-' + Regex.Replace(obj.Alias ?? "", "[^a-zA-Z0-9]+", ""), ref url, ModuleUrl.ImageFolder, ModuleUrl.ThumbFolder))
                    {
                        obj.Banner0 = url;
                    }
                    else
                    {
                        ModelState.AddModelError(m => m.Banner0, obj, StringTable.ErrMsgSaveImageUrl(obj.Banner0));
                        goto LB_ERR;
                    }
                }

                if (obj.Banner1 != null && Regex.IsMatch(obj.Banner1, @"^http(s)?://"))
                {
                    var url = obj.Banner1;
                    if (SaveUrl(obj.Id + '-' + Regex.Replace(obj.Alias ?? "", "[^a-zA-Z0-9]+", ""), ref url, ModuleUrl.ImageFolder, ModuleUrl.ThumbFolder))
                    {
                        obj.Banner1 = url;
                    }
                    else
                    {
                        ModelState.AddModelError(m => m.Banner1, obj, StringTable.ErrMsgSaveImageUrl(obj.Banner1));
                        goto LB_ERR;
                    }
                }

                if (obj.Banner2 != null && Regex.IsMatch(obj.Banner2, @"^http(s)?://"))
                {
                    var url = obj.Banner2;
                    if (SaveUrl(obj.Id + '-' + Regex.Replace(obj.Alias ?? "", "[^a-zA-Z0-9]+", ""), ref url, ModuleUrl.ImageFolder, ModuleUrl.ThumbFolder))
                    {
                        obj.Banner2 = url;
                    }
                    else
                    {
                        ModelState.AddModelError(m => m.Banner2, obj, StringTable.ErrMsgSaveImageUrl(obj.Banner2));
                        goto LB_ERR;
                    }
                } 

                #endregion

                if (CategoryMgr.SaveorUpdate(this, obj))
                {
                    if (!obj.InlineEdit) SetViewMsgInput(true);
                }
                else goto LB_ERR;

                return obj.InlineEdit
                    ? JsonEx(new[] { obj }.ToDataSourceResult(request, ModelState))
                    : RedirectToAction("Index", new { lang = lang, mode = mode });
            }

        LB_ERR:

            if (obj.InlineEdit)
                ModelState.TransformModelError<CategoryModel>((field, dispName, modelErr) =>
                    string.Format("[{0}] {1}", dispName, modelErr.ErrorMessage));
            else
            {
                SetViewMsgInput(false);
                ViewBag.Languages = LanguageMgr.GetAll();
                ViewBag.Settings = GetSettings();
                ViewBag.Categories = CategoryMgr.GetAll(this, obj.Id, null);
                ViewBag.TemplProduct = new CategoryProductModel();

                obj.InitProductImages(this);
            }

            return obj.InlineEdit
                ? JsonEx(new[] { obj }.ToDataSourceResult(request, ModelState))
                : ClawView(EditView, obj);
        }
        #endregion

        #region Delete

        [HttpPost]
        [AdminPerm("CatalogBits", PermissionMask.Edit)]
        public override ActionResult Delete(GridId[] ids)
        {
            if (ids != null && ids.Length > 0 && BaseMgr<CategoryModel, Category>.Delete(this,
                (sess, obj) =>
                {
                    if (obj == null || obj.AnyChild()) return false;
                    else
                    {
                        if (obj.Parent != null) obj.Parent.Children.Remove(obj);
                        return true;
                    }
                }, null, ids.Select(x => x.Id).ToArray()))
            {
                if (!IndexAction) SetViewMsg(true, StringTable.Delete);
                return JsonStatus(StringTable.Delete, true);
            }
            else
            {
                if (!IndexAction) SetViewMsg(false, StringTable.Delete);
                return JsonStatus(StringTable.Delete, false);
            }
        }

        #endregion

        #region SetVisibleWeb

        [HttpPost]
        [AdminPerm("CatalogBits", PermissionMask.Edit)]
        public ActionResult SetVisibleWeb(int id, bool value)
        {
            var retCode = CategoryMgr.SaveOrUpdate(this, id, obj =>
            {
                if (value) obj.Visibility |= (byte)VisibilityType.Web;
                else obj.Visibility &= (byte)~VisibilityType.Web;
            }, true);

            return JsonStatus(StringTable.UpdateVisible, retCode);
        }

        #endregion

        #region Move

        [HttpPost]
        [AdminPerm("CatalogBits", PermissionMask.Edit)]
        public ActionResult Move(int categoryid, int beforeId, int type)
        {
            var retCode = CategoryMgr.Move(this, categoryid, beforeId, type);

            if (!IndexAction) SetViewMsg(retCode, StringTable.UpdateDOrder);
            return JsonStatus(StringTable.UpdateDOrder, retCode);
        }

        #endregion

        #region ChangePrice

        [HttpPost]
        [AdminPerm("CatalogBits", PermissionMask.Edit)]
        public ActionResult ChangePrice(int id, bool increase, bool percent, OFloat value)
        {
            if (value == null || value.Value == null)
                return JsonStatus(Product.StringTable.UpdateProductPrice, false);

            var errors = new Dictionary<string, string>();
            var obj = CategoryMgr.ChangePrice(this, id, increase, percent, value);

            if (obj != null)
                foreach (var cp in obj.CategoryProducts)
                {
                    var err = ProductModel.ValidatePrices(this, cp.Product);
                    if (err != null) errors.Add(cp.Product.Id.ToString(), err.ErrorMessage);
                }

            var retCode = obj != null && errors.Count == 0 && BaseMgr<CategoryModel, Category>.SaveOrUpdate(this, obj);

            if (!IndexAction) SetViewMsg(retCode, Product.StringTable.UpdateProductPrice);

            return JsonStatus(Product.StringTable.UpdateProductPrice, retCode, null, m => m.Errors = errors);
        }

        #endregion

        #region GetMoveForm
        public ActionResult GetMoveForm(int id)
        {                   
            var obj = CategoryMgr.Get(this, id);

            if (obj == null) obj = new CategoryModel() { };

            ViewBag.Settings = GetSettings();
            ViewBag.Category = obj;

            return ClawView("Admin/_CategoriesMove.cshtml", obj);
        }
        #endregion

        #region GetBrowseForm
        public ActionResult GetBrowseForm(int id, int? parentId)
        {
            var cate = CategoryMgr.Get(this, id);
            var obj = new ProductModel();

            obj.InitRelationIds(this, string.Format("{0}", parentId ?? (cate == null || cate.ParentId == null ? (int?)null : cate.ParentId)));

            return ClawView("Admin/_ProductsRelation.cshtml", obj);
        }
        #endregion

        #region ListCate
        public ActionResult ListCate(int? id, int? currentId)
        {
            var model = CategoryMgr.Get(this, currentId ?? 0);
            var hierOrder = model == null ? null : (model.HierOrder + '/');
            var lst = CategoryMgr.GetAll(this, 0, id ?? -1).Select(x => new
            {
                x.Id,
                x.Name,
                Deep = x.Deep(),
                Active = true,
                x.ParentId,
                x.AnyChild,
                HierOrder = x.GetHierOrderTxt(),
                HierOrder2 = x.HierOrder,
                x.LangVisible,
                AncestorActive = true,
                AncestorLangVisible = x.AncestorLangVisible(),
                x.UpPathHasLang,
            }).Where(x => x.Id != (currentId ?? 0) && x.Deep + 1 < CategoryModel.MaxDeep && (hierOrder == null || x.HierOrder2.IndexOf(hierOrder, StringComparison.OrdinalIgnoreCase) != 0) && x.UpPathHasLang);

            return Json(lst, JsonRequestBehavior.AllowGet);
        }
        #endregion

        #region ListDD

        public ActionResult ListDD(int movcateid)
        {
            var obj = CategoryMgr.Get(this, movcateid);

            var lst = CategoryMgr.GetAll(this, 0, obj == null ? 0 : obj.ParentId ?? -1)
                .Where(x => x.Id != movcateid).OrderBy(x => x.DisplayOrder)
                .Select(x => new DDListItem()
                {
                    Value = x.Id.ToString(),
                    Text = x.Name,
                    Active = true,
                    LangVisible = x.LangVisible
                });

            //var moveTop = new DDListItem { Text = StringTable.Top, Value = "-1" };
            //var moveBottom = new DDListItem { Text = StringTable.Bottom, Value = "-2" };

            //var list = lst.ToList();
            //list.Insert(0, moveTop);
            //list.Add(moveBottom);

            return Json(lst, JsonRequestBehavior.AllowGet);
        }

        #endregion

        #region ListTree
        public ActionResult ListTree(int? id, int categoryid)
        {
            var lst = CategoryMgr.GetAll(this, -1, id ?? -1)
                .OrderBy(x => x.DisplayOrder).ThenBy(x => x.Name)
                .Select(x => new
                {
                    x.Id,
                    Deep = x.Deep(),
                    x.Name,
                    Active = true,
                    AnyChild = x.Id != categoryid && x.AnyChild && x.GetRaw().GetChildrenCount() > 0,
                    x.LangVisible,
                    AncestorActive = true,
                    AncestorLangVisible = x.AncestorLangVisible(),
                    ParentId = x.GetRaw().Parent.GetId(0),
                });

            return Json(lst, JsonRequestBehavior.AllowGet);
        }
        #endregion
    }

    #region CateBanner

    public class CateBanner0Controller : CategoryController
    {
        public CateBanner0Controller(IDataService ds)
            : base(ds)
        {
            InitImageSettings(x => x.Display.Category.Banner0, x => new SettingsBiz.SizeBiz() { Height = 120, Width = 120 });
        }
    }

    public class CateBanner1Controller : CategoryController
    {
        public CateBanner1Controller(IDataService ds)
            : base(ds)
        {
            InitImageSettings(x => x.Display.Category.Banner1, x => new SettingsBiz.SizeBiz() { Height = 120, Width = 120 });
        }
    }

    public class CateBanner2Controller : CategoryController
    {
        public CateBanner2Controller(IDataService ds)
            : base(ds)
        {
            InitImageSettings(x => x.Display.Category.Banner2, x => new SettingsBiz.SizeBiz() { Height = 120, Width = 120 });
        }
    } 

    #endregion
}