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
    public class PropertyController : BaseController<PropertyModel, Property>
    {
        private const string IndexView = "Admin/PropertiesIndex.cshtml";
        private const string EditView = "Admin/PropertiesEdit.cshtml";

        #region PropertyController
        public PropertyController(IDataService ds)
            : base(ds)
        {
            InitImageSettings(c => c.Display.Admin.IconSize, c => c.Display.Admin.IconSize);
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

            var items = PropertyMgr.GetAll(this, LanguageMgr.FocusLangCode, null, page,
                pageSize, out totalRows, sortDir, sortField, keyword)
                .Action(m => { m.InlineEdit = true; m.PropValListTxt = m.GetPropVals().Select(x => x.Name).ToString(", "); });

            return new DataSourceResult() { Data = items, Total = totalRows };
        }

        #endregion

        #region ListPropVal
        [ValidateInput(false)]
        public ActionResult ListPropVal(int id, string lang, string optlang, string mode)
        {
            return RedirectToAction("Index", "PropVal", new { propertyid = id, lang = lang, optlang = optlang, mode = mode });
        }
        #endregion

        #region Input.Get
        [ValidateInput(false)]
        [AdminPerm("CatalogBits", PermissionMask.View)]
        public ActionResult Input(int id, string lang, string optlang, string mode)
        {
            var obj = PropertyMgr.Get(this, id);

            ViewBag.Languages = LanguageMgr.GetAll();
            ViewBag.Settings = GetSettings();

            if (obj == null)
                obj = new PropertyModel() { Languages = new List<PropertyLangModel>() };

            return ClawView(EditView, obj);
        }
        #endregion

        #region Input.Post
        [HttpPost]
        [ValidateInput(false)]
        [AdminPerm("CatalogBits", PermissionMask.Edit)]
        public ActionResult Input([DataSourceRequest]DataSourceRequest request, PropertyModel obj,
            string name, string lang, string mode)
        {
            if (obj == null)
                return AcceptJson
                    ? JsonInline(request, new PropertyModel(), false)
                    : RedirectToAction("Index", new { lang = lang, mode = mode });

            #region Languages

            if (obj.Languages != null)
                foreach (var itm in obj.Languages
                    .Select((x, idx) => new { Val = x, Index = idx })
                    .Where(x => x.Val.IsDataModelEmpty).OrderByDescending(x => x.Index).ToList())
                {
                    obj.Languages.Remove(itm.Val);
                    ModelState.RemoveKey(string.Format("{0}[{{0}}]",
                        GetFieldName<PropertyModel, object>(m => m.Languages)),
                        itm.Index, obj.Languages.Count);
                }
            else obj.Languages = new List<PropertyLangModel>();

            #endregion

            #region InlineEdit

            if (obj.InlineEdit)
            {
                if (obj.Id != 0)
                    obj = CopyValidate(obj, PropertyMgr.Get(this, obj.Id), (nVal, oVal) =>
                    {
                        nVal.InlineEdit = true;
                        nVal.Name = name;
                        nVal.DisplayOrder = oVal.DisplayOrder;
                        TryValidateModel(nVal.GetLang());
                    });
                else
                {
                    var langCode = LanguageMgr.FocusLangCode;
                    obj.GetLangOrAddNew(langCode, () => new PropertyLangModel() { LangCode = langCode });
                    obj.Name = name;
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

                if (PropertyMgr.SaveorUpdate(this, obj))
                {
                    if (obj.InlineEdit) obj.PropValListTxt = obj.GetPropVals().Select(x => x.Name).ToString(", ");
                    else SetViewMsgInput(true);
                }
                else goto LB_ERR;

                return obj.InlineEdit
                    ? JsonInline(request, obj, true, m => m.DDOptionLabel = ("Select " + obj.Name.ToLower()).ModLocalize())
                    : RedirectToAction("Index", new { lang = lang, mode = mode });
            }

        LB_ERR:

            if (obj.InlineEdit)
                ModelState.TransformModelError<PropertyModel>((field, dispName, modelErr) =>
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
            var lstProd = new List<Domain.ProductBase>();

            if (retCode)
                retCode = BaseMgr<PropertyModel, Property>.Delete(this, false, (sess, obj) =>
                {
                    var ProductVariant = obj.PropVals.Select(x => x.VariantPropVals.Select(y => y.Variant).Select(z => z.Product));
                    foreach (var item in ProductVariant)
                    {
                        lstProd.AddRange(item);
                    }
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