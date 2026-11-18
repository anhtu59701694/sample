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
using Claw.Modules.Eshop.Helper;
using System.Text.RegularExpressions;

namespace Claw.Modules.Eshop.Controllers.Admin
{
    [AdminPerm("CatalogBits", byte.MaxValue)]
    public class TechSpecValController : BaseController<TechSpecValModel, TechSpecVal>
    {
        private const string IndexView = "Admin/TechSpecValsIndex.cshtml";
        private const string EditView = "Admin/TechSpecValsEdit.cshtml";

        #region TechSpecValController
        public TechSpecValController(IDataService ds)
            : base(ds)
        {
            //InitImageSettings(c => c.Display.Admin.IconSize, c => c.Display.Admin.IconSize);
        } 
        #endregion

        #region Index
        [ValidateInput(false)]
        [AdminPerm("CatalogBits", PermissionMask.View)]
        public ActionResult Index()
        {
            ViewBag.Settings = GetSettings();
            ViewBag.TechSpecs = TechSpecMgr.GetAll(this);

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

            var techSpecId = GetParam<int?>("techSpecId");

            var items = TechSpecValMgr.GetAll(this,techSpecId, page, pageSize, out totalRows, sortDir, sortField)
                .Action(m =>
                {
                    m.InlineEdit = true;
                    m.Products = m.GetRaw().ProductTechSVals.SafeCount();
                });

            return new DataSourceResult() { Data = items, Total = totalRows };
        }

        #endregion

        #region ListProduct
        [ValidateInput(false)]
        public ActionResult ListProduct(int id, string lang, string optlang, string mode)
        {
            return RedirectToAction("Index", "Product", new { techSpecValId = id, lang, optlang, mode });
        }
        #endregion

        #region Input.Get
        [ValidateInput(false)]
        [AdminPerm("CatalogBits", PermissionMask.View)]
        public ActionResult Input(int id, string lang, string optlang, string mode)
        {
            var obj = TechSpecValMgr.Get(this, id);

            ViewBag.Languages = LanguageMgr.GetAll();
            ViewBag.Settings = GetSettings();

            if (obj == null)
                obj = new TechSpecValModel();

            return ClawView(EditView, obj);
        }
        #endregion

        #region Input.Post
        [HttpPost]
        [ValidateInput(false)]
        [AdminPerm("CatalogBits", PermissionMask.Edit)]
        public ActionResult Input([DataSourceRequest]DataSourceRequest request, TechSpecValModel obj,
            string value, int? displayOrder, string lang, string mode)
        {
            if (obj == null)
                return AcceptJson
                    ? JsonInline(request, new TechSpecValModel(), false)
                    : RedirectToAction("Index", new { lang, mode });

            #region InlineEdit

            if (obj.InlineEdit)
            {
                if (obj.Id != 0)
                    obj = CopyValidate(obj, TechSpecValMgr.Get(this, obj.Id), (nVal, oVal) =>
                    {
                        nVal.InlineEdit = true;
                        nVal.Value = value;
                        nVal.DisplayOrder = displayOrder ?? nVal.DisplayOrder;
                    });
                else { obj.Value = value; obj.DisplayOrder = displayOrder ?? 1000; }
            }

            #endregion

            if (ModelState.ModLocalize().IsValid)
            {
                if (TechSpecValMgr.SaveorUpdate(this, obj))
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
                ModelState.TransformModelError<TechSpecValModel>((field, dispName, modelErr) =>
                    string.Format(obj.QuickAdd ? "{1}" : "[{0}] {1}", dispName, modelErr.ErrorMessage));
            else
            {
                SetViewMsgInput(false);
                ViewBag.Languages = LanguageMgr.GetAll();
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
                retCode = BaseMgr<TechSpecValModel, TechSpecVal>.Delete(this, false, (sess, obj) => true, null, ids.Select(x => x.Id).ToArray());

            if (!IndexAction) SetViewMsg(retCode, StringTable.Delete);

            return JsonStatus(StringTable.Delete, retCode);
        }

        #endregion
    }
}