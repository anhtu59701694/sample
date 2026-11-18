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
    public class PropValController : BaseController<PropValModel, PropVal>
    {
        private const string IndexView = "Admin/PropValsIndex.cshtml";
        private const string EditView = "Admin/PropValsEdit.cshtml";

        public PropValController(IDataService ds)
            : base(ds)
        {
            var size = new SettingsBiz.SizeBiz { Width = 50, Height = 50 };
            InitImageSettings(c => size, c => size);
        }

        #region Index
        [ValidateInput(false)]
        [AdminPerm("CatalogBits", PermissionMask.View)]
        public ActionResult Index()
        {
            ViewBag.Settings = GetSettings();
            ViewBag.Properties = PropertyMgr.GetAll(this);

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
            var propertyid = GetParam<int?>("propertyid");

            string keyword = GetKeyword();

            var items = PropValMgr.GetAll(this, LanguageMgr.FocusLangCode, propertyid, null, page,
                pageSize, out totalRows, sortDir, sortField, keyword)
                .Action(m => { m.InlineEdit = true; });

            return new DataSourceResult() { Data = items, Total = totalRows };
        }

        #endregion

        #region EditProp
        [ValidateInput(false)]
        public ActionResult EditProp(int id, string lang, string optlang, string mode)
        {
            return RedirectToAction("Input", "Property", new { id = id, lang = lang, optlang = optlang, mode = mode });
        }
        #endregion

        #region Input.Get
        [ValidateInput(false)]
        [AdminPerm("CatalogBits", PermissionMask.View)]
        public ActionResult Input(int id, string lang, string optlang, string mode)
        {
            var obj = PropValMgr.Get(this, id);

            ViewBag.Languages = LanguageMgr.GetAll();
            ViewBag.Settings = GetSettings();
            ViewBag.Properties = PropertyMgr.GetAll(this);

            var propertyid = GetParam<int?>("propertyid");

            if (obj == null)
                obj = new PropValModel() { Languages = new List<PropValLangModel>() };

            obj.PropertyId = propertyid ?? 0;

            return ClawView(EditView, obj);
        }
        #endregion

        #region Input.Post
        [HttpPost]
        [ValidateInput(false)]
        [AdminPerm("CatalogBits", PermissionMask.Edit)]
        public ActionResult Input([DataSourceRequest]DataSourceRequest request, PropValModel obj,
            string name, string lang, string mode)
        {
            if (obj == null)
                return AcceptJson
                    ? JsonInline(request, new PropValModel(), false)
                    : RedirectToAction("Index", new { lang = lang, mode = mode });

            #region Languages

            if (obj.Languages != null)
                foreach (var itm in obj.Languages
                    .Select((x, idx) => new { Val = x, Index = idx })
                    .Where(x => x.Val.IsDataModelEmpty).OrderByDescending(x => x.Index).ToList())
                {
                    obj.Languages.Remove(itm.Val);
                    ModelState.RemoveKey(string.Format("{0}[{{0}}]",
                        GetFieldName<PropValModel, object>(m => m.Languages)),
                        itm.Index, obj.Languages.Count);
                }
            else obj.Languages = new List<PropValLangModel>();

            #endregion

            #region InlineEdit

            if (obj.InlineEdit)
            {
                if (obj.Id != 0)
                    obj = CopyValidate(obj, PropValMgr.Get(this, obj.Id), (nVal, oVal) =>
                    {
                        nVal.InlineEdit = true;
                        nVal.Name = name;
                        nVal.DisplayOrder = oVal.DisplayOrder;
                        TryValidateModel(nVal.GetLang());
                    });
                else
                {
                    var langCode = LanguageMgr.FocusLangCode;
                    var langModel = obj.GetLangOrAddNew(langCode, () => new PropValLangModel() { LangCode = langCode, PropertyId = obj.PropertyId});
                    obj.Name = name;
                    TryValidateModel(langModel);
                }
            }

            #endregion

            if (ModelState.ModLocalize().IsValid)
            {
                #region MoveFile
                try
                {
                    var tmpPath = ModuleUrl.TempFolder.ResolveClientUrl();
                    if (obj.ImageFsUrl != null && obj.ImageFsUrl.StartsWith(tmpPath))
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
                    if (SaveUrl(obj.Id + '-' + Regex.Replace(obj.Code ?? "", "[^a-zA-Z0-9]+", ""), ref urlI, ModuleUrl.ImageFolder, ModuleUrl.ThumbFolder))
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

                if (PropValMgr.SaveorUpdate(this, obj))
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
                ModelState.TransformModelError<PropValModel>((field, dispName, modelErr) =>
                    string.Format(obj.QuickAdd ? "{1}" : "[{0}] {1}", dispName, modelErr.ErrorMessage));
            else
            {
                SetViewMsgInput(false);
                ViewBag.Languages = LanguageMgr.GetAll();
                ViewBag.Settings = GetSettings();
                ViewBag.Properties = PropertyMgr.GetAll(this);
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
            var lstProd = new List<Domain.ProductBase>();

            if (retCode)
                retCode = BaseMgr<PropValModel, PropVal>.Delete(this, false, (sess, obj) =>
                {
                    var product = obj.VariantPropVals.Select(x => x.Variant).Select(z => z.Product);
                    lstProd.AddRange(product);
                    lstProd = lstProd.GroupBy(x => x.Id).Select(y => y.First()).ToList();

                    if (lstProd.Count > 0)
                        return false;

                    else return true;
                }, null, ids.Select(x => x.Id).ToArray());

            string errMsg = null;

            if (!retCode && lstProd.Count != 0)
                errMsg = string.Format(StringTable.StatusDeleteFailedMsg, lstProd.Take(3).Select(x => x.GetName()).ToString(", ") + (lstProd.Count > 3 ? " ..." : ""));

            if (!IndexAction) SetViewMsg(retCode, StringTable.Delete, errMsg);

            return JsonStatus(StringTable.Delete, retCode, errMsg);
        }

        #endregion

        #region ValidateCode
        public virtual ActionResult ValidateCode(string code, int? id)
        {
            return ValidateField("Code", code, id);
        }
        #endregion
    }
}