using System.ComponentModel.DataAnnotations;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using AutoMapper;
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
using Kendo.Mvc.Extensions;
using Kendo.Mvc.UI;
using System.Web.Mvc;
using Claw.Modules.Eshop.DAL;
using Claw.Modules.Eshop.Domain;
using Claw.Modules.Eshop.Models;
using System.Text.RegularExpressions;
using Claw.Modules.Eshop.Helper;

namespace Claw.Modules.Eshop.Controllers.Admin
{
    [AdminPerm("CatalogBits", byte.MaxValue)]
    public class ProductBundleGroupController : BaseController<ProductBundleGroupModel, ProductBundleGroup>
    {
        private const string IndexView = "Admin/ProductBundleGroupsIndex.cshtml";
        private const string EditView = "Admin/ProductBundleGroupsEdit.cshtml";

        public ProductBundleGroupController(IDataService ds) : base(ds) { }

        #region Index
        [ValidateInput(false)]
        [AdminPerm("CatalogBits", PermissionMask.View)]
        public ActionResult Index()
        {
            ViewBag.Settings = GetSettings();
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

            return Json(List(request.Page, request.PageSize, sortField, sortDir));
        }

        private DataSourceResult List(int page, int pageSize, string sortField, SortDirection sortDir)
        {
            int totalRows;
            var productid = GetParam<int?>("productid");

            var items = ProductBundleGroupMgr.GetAll(this, productid, page,
                pageSize, out totalRows, sortDir, sortField)
                .Action(m => { m.InlineEdit = true; });

            return new DataSourceResult() { Data = items, Total = totalRows };
        }

        #endregion

        #region Input.Get

        [ValidateInput(false)]
        [AdminPerm("CatalogBits", PermissionMask.View)]
        public ActionResult Input(int id, string lang, string optlang, string mode)
        {
            var obj = ProductBundleGroupMgr.Get(this, id);

            ViewBag.Settings = GetSettings();

            var productid = GetParam<int?>("productid");

            if (obj == null) obj = new ProductBundleGroupModel() { };

            obj.ProductId = productid ?? 0;

            return ClawView(EditView, obj);
        }

        #endregion

        #region Input.Post

        [HttpPost]
        [ValidateInput(false)]
        [AdminPerm("CatalogBits", PermissionMask.Edit)]
        public ActionResult Input([DataSourceRequest]DataSourceRequest request, ProductBundleGroupModel obj,
            string name, bool? showVariant, string lang, string mode)
        {
            if (obj == null)
                return AcceptJson
                    ? JsonInline(request, new PropValModel(), false)
                    : RedirectToAction("Index", new { lang, mode });

            #region InlineEdit

            if (obj.InlineEdit)
            {
                if (obj.Id != 0)
                    obj = CopyValidate(obj, ProductBundleGroupMgr.Get(this, obj.Id), (nVal, oVal) =>
                    {
                        nVal.InlineEdit = true;
                        nVal.Name = name;
                        nVal.ShowVariant = showVariant ?? true;
                    });
                else
                {
                    obj.Name = name;
                    obj.ShowVariant = showVariant ?? true;
                }
            }

            #endregion

            if (ModelState.ModLocalize().IsValid)
            {
                if (ProductBundleGroupMgr.SaveorUpdate(this, obj))
                {
                    if (!obj.InlineEdit) SetViewMsgInput(true);
                }
                else goto LB_ERR;

                return obj.InlineEdit
                    ? JsonInline(request, obj, true)
                    : RedirectToAction("Index", new { lang, mode });
            }

        LB_ERR:

            if (obj.InlineEdit)
                ModelState.TransformModelError<ProductBundleGroupModel>((field, dispName, modelErr) =>
                    string.Format(obj.QuickAdd ? "{1}" : "[{0}] {1}", dispName, modelErr.ErrorMessage));
            else
            {
                SetViewMsgInput(false);
                ViewBag.Settings = GetSettings();
            }

            return obj.InlineEdit
                ? JsonInline(request, obj, false)
                : ClawView(EditView, obj);
        }

        #endregion

        #region Delete

        [HttpPost]
        [AdminPerm("CatalogBits", PermissionMask.Edit)]
        public override ActionResult Delete(GridId[] ids)
        {
            var retCode = ids != null && ids.Length > 0;

            if (retCode)
                retCode = ProductBundleGroupMgr.Delete(this, false, (sess, obj) =>
                {
                    obj.Accessories = obj.Accessories ?? obj.Product.Accessories.Count(m => m.BundleGroup != null && m.BundleGroup.Id == obj.Id);
                    return obj.Accessories == 0;
                }, null, ids.Select(x => x.Id).ToArray());

            if (!IndexAction) SetViewMsg(retCode, StringTable.Delete);

            return JsonStatus(StringTable.Delete, retCode);
        }

        #endregion

        #region ListD2

        public ActionResult ListD2(int id)
        {
            var ret = ProductBundleGroupMgr.GetAll(this, id);

            var lst = ret.Select(x => new DDListItem()
                {
                    Value = x.Id.ToString(),
                    Text = x.Name,
                    Active = true,
                    LangVisible = true
                });

            return Json(lst, JsonRequestBehavior.AllowGet);
        }

        #endregion

        #region SetShowVariant

        [HttpPost]
        [AdminPerm("CatalogBits", PermissionMask.Edit)]
        public ActionResult SetShowVariant(int id, bool value)
        {
            var retCode = BaseMgr<ProductBundleGroupModel, Domain.ProductBundleGroup>.SaveOrUpdate(this, id, obj => obj.ShowVariant = value);

            return JsonStatus(StringTable.SetShowVariant, retCode);
        }

        #endregion

    }
}