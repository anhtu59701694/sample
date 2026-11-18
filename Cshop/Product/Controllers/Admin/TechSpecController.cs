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
    public class TechSpecController : BaseController<TechSpecModel, TechSpec>
    {
        private const string IndexView = "Admin/TechSpecsIndex.cshtml";
        private const string EditView = "Admin/TechSpecsEdit.cshtml";

        #region TechSpecController
        public TechSpecController(IDataService ds) : base(ds)
        {
            InitImageSettings(c => c.Display.Product.TechSpecIcon, c => c.Display.Product.TechSpecIcon);
        } 
        #endregion

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

            string keyword = GetKeyword();

            var items = TechSpecMgr.GetAll(this, page,
                pageSize, out totalRows, sortDir, sortField, keyword)
                .Action(m =>
                {
                    m.InlineEdit = true;
                    m.Values = m.GetRaw().TechSpecVals.SafeCount();
                });

            return new DataSourceResult() { Data = items, Total = totalRows };
        }

        #endregion

        #region ListValue
        [ValidateInput(false)]
        public ActionResult ListValue(int id, string lang, string optlang, string mode)
        {
            return RedirectToAction("Index", "TechSpecVal", new { techSpecId = id, lang, optlang, mode });
        }
        #endregion

        #region Input.Get
        [ValidateInput(false)]
        [AdminPerm("CatalogBits", PermissionMask.View)]
        public ActionResult Input(int id, string lang, string optlang, string mode)
        {
            var obj = TechSpecMgr.Get(this, id) ?? new TechSpecModel();

            ViewBag.Languages = LanguageMgr.GetAll();
            ViewBag.Settings = GetSettings();

            return ClawView(EditView, obj);
        }
        #endregion

        #region Input.Post
        [HttpPost]
        [ValidateInput(false)]
        [AdminPerm("CatalogBits", PermissionMask.Edit)]
        public ActionResult Input([DataSourceRequest]DataSourceRequest request, TechSpecModel obj,
            string name, string lang, string mode)
        {
            if (obj == null)
                return AcceptJson
                    ? JsonInline(request, new TechSpecModel(), false)
                    : RedirectToAction("Index", new { lang, mode });

            #region InlineEdit

            if (obj.InlineEdit)
            {
                if (obj.Id != 0)
                    obj = CopyValidate(obj, TechSpecMgr.Get(this, obj.Id), (nVal, oVal) =>
                    {
                        nVal.InlineEdit = true;
                        nVal.Name = name;
                        nVal.DisplayOrder = oVal.DisplayOrder;
                        nVal.Values = oVal.Values;
                    });
            }

            #endregion

            if (ModelState.ModLocalize().IsValid)
            {
                #region MoveFile
                try
                {
                    var tmpPath = ModuleUrl.TempFolder.ResolveClientUrl();
                    if (obj.IconFsUrl != null && obj.IconFsUrl.StartsWith(tmpPath))
                        obj.Icon = MoveFile(ModuleUrl.ImageFolder, ModuleUrl.TempFolder, obj.Icon);
                }
                catch (Exception er)
                {
                    LogErr(er); goto LB_ERR;
                }
                #endregion

                #region DownloadImage
                if (obj.Icon != null && Regex.IsMatch(obj.Icon, @"^http(s)?://"))
                {
                    var urlI = obj.Icon;
                    if (SaveUrl(obj.Id + '-' + Regex.Replace(obj.Name ?? "", "[^a-zA-Z0-9]+", ""), ref urlI, ModuleUrl.ImageFolder, ModuleUrl.ThumbFolder))
                    {
                        obj.Icon = urlI;
                    }
                    else
                    {
                        ModelState.AddModelError(m => m.Icon, obj, StringTable.ErrMsgSaveImageUrl(obj.Icon));
                        goto LB_ERR;
                    }
                }
                #endregion

                if (TechSpecMgr.SaveorUpdate(this, obj))
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
                ModelState.TransformModelError<TechSpecModel>((field, dispName, modelErr) =>
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
                retCode = BaseMgr<TechSpecModel, TechSpec>.Delete(this, false, (sess, obj) => true, null, ids.Select(x => x.Id).ToArray());

            if (!IndexAction) SetViewMsg(retCode, StringTable.Delete);

            return JsonStatus(StringTable.Delete, retCode);
        }

        #endregion

        #region Feature

        [HttpPost]
        [AdminPerm]
        public virtual ActionResult Feature(int id, bool feature)
        {
            var obj = TechSpecMgr.Get<TechSpec>(id);
            if (obj != null)
            {
                obj.Featured = feature;
                var ret = obj.Update(DataService);

                return JsonStatus(StringTable.UpdateFeature, ret);
            }

            return JsonStatus(StringTable.UpdateFeature, false);
        }

        #endregion
    }
}