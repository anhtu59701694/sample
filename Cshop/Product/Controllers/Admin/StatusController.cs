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
    public class StatusController : BaseController<StatusModel, Status>
    {
        private const string IndexView = "Admin/StatusesIndex.cshtml";
        private const string EditView = "Admin/StatusesEdit.cshtml";

        #region StatusController
        public StatusController(IDataService ds) :
            base(ds)
        {
            //var size = new SettingsBiz.SizeBiz { Width = 50, Height = 50 };
            //InitImageSettings(c => size, c => size);
            InitImageSettings(c => c.Display.Product.IconStatus, c => c.Display.Product.IconStatus);
        }
        #endregion

        #region Index
        [ValidateInput(false)]
        [AdminPerm("CatalogBits", PermissionMask.View)]
        public ActionResult Index(int? statusid)
        {
            ViewBag.Settings = GetSettings();

            ViewBag.Statuses = StatusMgr.GetAll(this, 0).ToList();

            if (statusid != null && statusid.Value != 0)
                ViewBag.Status = StatusMgr.Get(this, statusid.Value);

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
 
            var items = StatusMgr.GetAll(this, LanguageMgr.FocusLangCode, page,
                pageSize, out totalRows, sortDir, sortField, keyword).Action(m => { m.InlineEdit = true; });

            return new DataSourceResult() { Data = items, Total = totalRows };
        }

        #endregion

        #region ListItem
        [ValidateInput(false)]
        public ActionResult ListItem(int id, string lang, string mode)
        {
            return RedirectToAction("Index", "Item", new { TopicId = id, lang = lang, mode = mode });
        }
        #endregion

        #region Input
        [ValidateInput(false)]
        [AdminPerm("CatalogBits", PermissionMask.View)]
        public ActionResult Input(int id, string lang, string optlang, string mode)
        {
            var obj = StatusMgr.Get(this, id);

            //show drop for Zero Qty Status

            //List<SelectListItem> itemsStatus = new List<SelectListItem>();
            //itemsStatus = StatusMgr.GetAll(this, 0).Where(x => x.Id != id).Select(x => new SelectListItem { Value = x.Id.ToString(), Text = x.Name }).ToList<SelectListItem>();

            //var tempValue = new SelectListItem { Value = "0", Text = "-- None --", Selected = true };
            //itemsStatus.Insert(0, tempValue);

            ViewBag.itemsStatus = StatusMgr.GetAll(this, 1).Where(x => x.Id != id).ToList(); ;

            ViewBag.Languages = LanguageMgr.GetAll();
            ViewBag.Settings = GetSettings();

            if (obj == null)
                obj = new StatusModel() { Languages = new List<StatusLangModel>() };

            return ClawView(EditView, obj);
        }

        [HttpPost]
        [ValidateInput(false)]
        //[ValidateAntiForgeryToken]
        [AdminPerm("CatalogBits", PermissionMask.Edit)]
        public ActionResult Input([DataSourceRequest]DataSourceRequest request, StatusModel obj,
            string name, string lang, string mode)
        {
            if (obj == null) return RedirectToAction("Index", new { lang = lang, mode = mode });

            #region Languages

            if (obj.Languages != null)
            {
                foreach (var itm in obj.Languages
                    .Select((x, idx) => new { Val = x, Index = idx })
                    .Where(x => x.Val.IsDataModelEmpty).OrderByDescending(x => x.Index).ToList())
                {
                    obj.Languages.Remove(itm.Val);
                    ModelState.RemoveKey(string.Format("{0}[{{0}}]",
                        GetFieldName<StatusModel, object>(m => m.Languages)),
                        itm.Index, obj.Languages.Count);
                }
            }
            else obj.Languages = new List<StatusLangModel>();

            #endregion

            #region InlineEdit

            if (obj.InlineEdit)
            {
                if (obj.Id == 0)
                {
                    var langCode = LanguageMgr.FocusLangCode;
                    obj.GetLangOrAddNew(langCode, () => new StatusLangModel() { LangCode = langCode });
                    obj.Name = name;
                }
                else
                    obj = CopyValidate(obj, StatusMgr.Get(this, obj.Id), (nVal, oVal) =>
                    {
                        nVal.InlineEdit = true;
                        nVal.Name = name;
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
                    var url = obj.Icon;
                    if (SaveUrl(obj.Id + '-' + Regex.Replace(obj.Name ?? "", "[^a-zA-Z0-9]+", ""), ref url, ModuleUrl.ImageFolder, ModuleUrl.ThumbFolder))
                    {
                        obj.Icon = url;
                    }
                    else
                    {
                        ModelState.AddModelError(m => m.Icon, obj, StringTable.ErrMsgSaveImageUrl(obj.Icon));
                        goto LB_ERR;
                    }
                }
                #endregion
                #region remove char specical of Code
                //var code = obj.Code.Replace("-", "");
                //var strNumber = Regex.Replace(code, @"[^\d]", string.Empty);
                //var strChar = Regex.Replace(code, @"[\d-]", string.Empty);

                //obj.Code = (string.IsNullOrEmpty(strChar) ? "a_" : strChar) + strNumber;
                #endregion

                if (StatusMgr.SaveorUpdate(this, obj))
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
                ModelState.TransformModelError<StatusModel>((field, dispName, modelErr) =>
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
            var retCode = ids != null && ids.Length > 0 && ids.All(x => x.Id > int.Parse(Status.StatusIDEqual));
            var lstProd = new List<Domain.ProductBase>();

            if (retCode)
                retCode = BaseMgr<StatusModel, Status>.Delete(this, false, (sess, obj) =>
                {
                    if (obj.Products.Count > 0)
                    {
                        lstProd.AddRange(obj.Products);
                        return false;
                    }
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

        #region Status EnableBuy
        [HttpPost]
        [AdminPerm("CatalogBits", PermissionMask.Edit)]
        public virtual ActionResult StatusEnableBuy(int id, bool value)
        {
            var ret = StatusMgr.Update(this, id, null, value);

            return JsonStatus(StringTable.Updated, ret);
        }

        #endregion

        #region Status ShowPrice
        [HttpPost]
        [AdminPerm("CatalogBits", PermissionMask.Edit)]
        public virtual ActionResult StatusShowPrice(int id, bool value)
        {
            var ret = StatusMgr.Update(this, id, null, null, value);

            return JsonStatus(StringTable.Updated, ret);
        }

        #endregion

        #region Status FrontEnd
        [HttpPost]
        [AdminPerm("CatalogBits", PermissionMask.Edit)]
        public virtual ActionResult StatusShowFrontEnd(int id, bool value)
        {
            var ret = StatusMgr.Update(this, id, null, null, null, value);

            return JsonStatus(StringTable.Updated, ret);
        }

        #endregion
    }
}