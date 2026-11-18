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
    public class ManufacturerController : BaseController<ManufacturerModel, Manufacturer>
    {
        private const string IndexView = "Admin/ManufacturerIndex.cshtml";
        private const string EditView = "Admin/ManufacturerEdit.cshtml";

        #region ManufacturerController
        public ManufacturerController(IDataService ds)
            : base(ds)
        {
            InitImageSettings(c=>c.Display.Product.LogoManu,c=>c.Display.Product.LogoManu);
        }
        #endregion

        #region Index
        [ValidateInput(false)]
        [AdminPerm("CatalogBits", PermissionMask.View)]
        public ActionResult Index(int? manufacturerId)
        {
            ViewBag.Settings = GetSettings();

            ViewBag.Manufactureres = ManufacturerMgr.GetAll(this, 0).ToList();

            if (manufacturerId != null && manufacturerId.Value != 0)
                ViewBag.Manufacturer = ManufacturerMgr.Get(this, manufacturerId.Value);

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

            var items = ManufacturerMgr.GetAll(this,null, LanguageMgr.FocusLangCode, page,
                pageSize, out totalRows, sortDir, sortField, keyword).Action(m => { m.InlineEdit = true; });

            return new DataSourceResult() { Data = items, Total = totalRows };
        }

        #endregion

        #region Input
        [ValidateInput(false)]
        [AdminPerm("CatalogBits", PermissionMask.View)]
        public ActionResult Input(int id, string lang, string optlang, string mode)
        {
            var obj = ManufacturerMgr.Get(this, id);

            ViewBag.Languages = LanguageMgr.GetAll();
            ViewBag.Settings = GetSettings();

            if (obj == null)
                obj = new ManufacturerModel() { Languages = new List<ManufacturerLangModel>() };

            return ClawView(EditView, obj);
        }

        [HttpPost]
        [ValidateInput(false)]
        //[ValidateAntiForgeryToken]
        [AdminPerm("CatalogBits", PermissionMask.Edit)]
        public ActionResult Input([DataSourceRequest]DataSourceRequest request, ManufacturerModel obj,
            string name, string alias, string url, string lang, string mode)
        {
            if (obj == null) return RedirectToAction("Index", new { lang = lang, mode = mode });

            if (BaseMgr.Exists<Domain.Manufacturer>(m => m.Id, obj.Id, null,
                    new KeyValuePair() { Key = "Alias", Value = alias }))
            {
                return JsonInline(request, obj, false, null, JsonRequestBehavior.DenyGet, StringTable.ErrMsgUniqueNameManuf);
            }

            #region Languages

            if (obj.Languages != null)
            {
                foreach (var itm in obj.Languages
                    .Select((x, idx) => new {Val = x, Index = idx})
                    .Where(x => x.Val.IsDataModelEmpty).OrderByDescending(x => x.Index).ToList())
                {
                    obj.Languages.Remove(itm.Val);
                    ModelState.RemoveKey(string.Format("{0}[{{0}}]",
                        GetFieldName<ManufacturerModel, object>(m => m.Languages)),
                        itm.Index, obj.Languages.Count);
                }
            }
            else obj.Languages = new List<ManufacturerLangModel>();

            #endregion

            #region InlineEdit

            if (obj.InlineEdit)
            {
                if (obj.Id == 0)
                {
                    var langCode = LanguageMgr.FocusLangCode;
                    obj.GetLangOrAddNew(langCode, () => new ManufacturerLangModel() { LangCode = langCode });
                    obj.Name = name;
                    obj.Alias = alias;
                }
                else
                    obj = CopyValidate(obj, ManufacturerMgr.Get(this, obj.Id), (nVal, oVal) =>
                    {
                        nVal.InlineEdit = true;
                        nVal.Name = name;
                        nVal.Alias = alias;
                        nVal.Url = url;
                        nVal.DisplayOrder = oVal.DisplayOrder;
                        TryValidateModel(nVal.GetLang());
                    });
            }

            #endregion

            if (ModelState.ModLocalize().IsValid)
            {
                #region MoveFile
                try
                {
                    var tmpPath = ModuleUrl.TempFolder.ResolveClientUrl();
                    if (obj.ImageFsUrl != null && obj.ImageFsUrl.StartsWith(tmpPath))
                        obj.Logo = MoveFile(ModuleUrl.ImageFolder, ModuleUrl.TempFolder, obj.Logo);
                }
                catch (Exception er)
                {
                    LogErr(er); goto LB_ERR;
                }
                #endregion

                #region DownloadImage
                if (obj.Logo != null && Regex.IsMatch(obj.Logo, @"^http(s)?://"))
                {
                    var urlI = obj.Logo;
                    if (SaveUrl(obj.Id + '-' + Regex.Replace(obj.Alias ?? "", "[^a-zA-Z0-9]+", ""), ref urlI, ModuleUrl.ImageFolder, ModuleUrl.ThumbFolder))
                    {
                        obj.Logo = urlI;
                    }
                    else
                    {
                        ModelState.AddModelError(m => m.Logo, obj, StringTable.ErrMsgSaveImageUrl(obj.Logo));
                        goto LB_ERR;
                    }
                }
                #endregion

                if (ManufacturerMgr.SaveorUpdate(this, obj))
                {
                    if (!obj.InlineEdit) SetViewMsgInput(true);
                }
                else goto LB_ERR;

                return obj.InlineEdit
                    ? JsonInline(request, obj, true)
                    : RedirectToAction("Index", new { lang = lang, mode = mode });
            }

        LB_ERR:

            if (obj.InlineEdit)
                ModelState.TransformModelError<CategoryModel>((field, dispName, modelErr) =>
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
            if (ids != null && ids.Length > 0 && BaseMgr<ManufacturerModel, Manufacturer>.Delete(this,
                (session, obj) =>
                {
                    if (obj == null) return false;

                    foreach (var p in obj.Products)
                    {
                        p.Manufacturer = null;
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
    }
}