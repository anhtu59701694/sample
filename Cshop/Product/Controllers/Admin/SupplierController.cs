using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.UI.WebControls;
using AutoMapper;
using Claw.Controls.Attributes;
using Claw.Core.DAL;
using Claw.Core.Enumerations;
using Claw.Core.Interfaces;
using Claw.Modules.Eshop.DAL;
using Claw.Modules.Eshop.Domain;
using Claw.Modules.Eshop.Models;
using Kendo.Mvc.Extensions;
using Kendo.Mvc.UI;
using System.Text.RegularExpressions;

namespace Claw.Modules.Eshop.Controllers.Admin
{
    [AdminPerm("CatalogBits", byte.MaxValue)]
    public class SupplierController : BaseController<SupplierModel, Supplier>
    {
        private const string IndexView = "Admin/SupplierIndex.cshtml";
        private const string EditView = "Admin/SupplierEdit.cshtml";

        #region SupplierController
        public SupplierController(IDataService ds)
            : base(ds)
        {

        }
        #endregion

        #region Index
        [ValidateInput(false)]
        [AdminPerm("CatalogBits", PermissionMask.View)]
        public ActionResult Index(int? manufacturerId)
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

            var sortField = "Id";
            var sortDir = SortDirection.Descending;

            GetSortField(request, ref sortField, ref sortDir);

            return Json(List(request.Page, request.PageSize, sortField, sortDir));
        }

        private DataSourceResult List(int page, int pageSize, string sortField, SortDirection sortDir)
        {
            int totalRows;
            var keyword = GetKeyword();

            var items = SupplierMgr.GetAll(this, page, pageSize, out totalRows, sortDir, sortField, keyword);

            return new DataSourceResult() { Data = items, Total = totalRows };
        }

        #endregion

        #region Enable
        [HttpPost]
        public override ActionResult Enable(int id, bool active)
        {
            var ret = SupplierMgr.Enable(this, id, active);

            return JsonStatus(StringTable.UpdateStatus, ret);
        }
        #endregion

        #region Input
        [ValidateInput(false)]
        [AdminPerm("CatalogBits", PermissionMask.View)]
        public ActionResult Input(int id, string lang, string optlang, string mode)
        {
            var obj = SupplierMgr.Get(this, id);

            ViewBag.Languages = LanguageMgr.GetAll();
            ViewBag.Settings = GetSettings();

            if (obj == null)
                obj = new SupplierModel();

            return ClawView(EditView, obj);
        }

        [HttpPost]
        [ValidateInput(false)]
        [AdminPerm("CatalogBits", PermissionMask.Edit)]
        public ActionResult Input(SupplierModel obj, string lang, string mode)
        {
            if (obj == null)
                return RedirectToAction("Index", new { lang = lang, mode = mode });

            if (BaseMgr.Exists<Domain.Supplier>(m => m.Id, obj.Id, null, new KeyValuePair() { Key = "Name", Value = obj.Name }))
            {
                goto LB_ERR;
                //return ClawView(EditView, obj);
                //return JsonInline(request, obj, false, null, JsonRequestBehavior.DenyGet, StringTable.ErrMsgUniqueNameManuf);
            }

            if (ModelState.ModLocalize().IsValid)
            {
                if (SupplierMgr.SaveorUpdate(this, obj))
                {
                    return RedirectToAction("Index", new { lang = lang, mode = mode });
                }
                else goto LB_ERR;
            }

        LB_ERR:
            SetViewMsgInput(false);
            ViewBag.Languages = LanguageMgr.GetAll();
            ViewBag.Settings = GetSettings();

            return ClawView(EditView, obj);
        }
        #endregion

        #region Delete

        [HttpPost]
        [AdminPerm("CatalogBits", PermissionMask.Edit)]
        public override ActionResult Delete(GridId[] ids)
        {
            if (ids != null && ids.Length > 0 && BaseMgr<SupplierModel, Supplier>.Delete(this,
                (session, obj) =>
                {
                    if (obj == null) return false;

                    foreach (var p in obj.Products)
                    {
                        p.Supplier = null;
                        session.Update(p);
                    }

                    obj.Products.Clear();

                    return true;
                }, null, ids.Select(x => x.Id).ToArray()))
            {
                if (!IndexAction) SetViewMsg(true, StringTable.Delete);
                return JsonStatus(StringTable.Delete, true);
            }

            if (!IndexAction) SetViewMsg(false, StringTable.Delete);
            return JsonStatus(StringTable.Delete, false);
        }

        #endregion

        #region ValidateName

        [HttpPost]
        public override ActionResult ValidateName()
        {
            var valid = SupplierModel.Validate(new SupplierModel()
            {
                Id = GetPatternParam<int>("Id", ""),
                Name = GetPatternParam<string>("Name", ""),

            }, null);

            return Json(null == valid, JsonRequestBehavior.AllowGet);
        }

        #endregion
    }
}