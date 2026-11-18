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
    [AdminPerm]
    public class EmailTemplController : BaseController<EmailTemplModel, EmailTempl>
    {
        private const string IndexView = "Admin/EmailTemplsIndex.cshtml";
        private const string EditView = "Admin/EmailTemplsEdit.cshtml";

        public EmailTemplController(IDataService ds)
            : base(ds)
        {
        }

        #region Index
        [ValidateInput(false)]
        public ActionResult Index()
        {
            ViewBag.Settings = GetSettings();

            ViewBag.EagerData = new GridEagerLoadDataHandler(List);

            return ClawView(IndexView);
        }
        #endregion

        #region List

        [ValidateInput(false)]
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

            var items = EmailTemplMgr.GetAll(this, LanguageMgr.FocusLangCode, null, page,
                pageSize, out totalRows, sortDir, sortField, keyword)
                .Action(m => { m.InlineEdit = true; });

            return new DataSourceResult() { Data = items, Total = totalRows };
        }

        #endregion

        #region Input.Get
        [ValidateInput(false)]
        public ActionResult Input(int? id, string lang, string optlang, string mode)
        {
            if (id == null) return HttpNotFound();

            var obj = EmailTemplMgr.Get(this, id ?? 0);

            ViewBag.Languages = LanguageMgr.GetAll();
            ViewBag.Settings = GetSettings();

            if (obj == null)
                obj = new EmailTemplModel() { Languages = new List<EmailTemplLangModel>() };

            return ClawView(EditView, obj);
        }
        #endregion

        #region Input.Post
        [HttpPost]
        [ValidateInput(false)]
        public ActionResult Input([DataSourceRequest]DataSourceRequest request, EmailTemplModel obj,
            string lang, string mode)
        {
            if (obj == null)
                return AcceptJson
                    ? JsonInline(request, new EmailTemplModel(), false)
                    : RedirectToAction("Index", new { lang = lang, mode = mode });

            #region Languages

            if (obj.Languages != null)
                foreach (var itm in obj.Languages
                    .Select((x, idx) => new { Val = x, Index = idx })
                    .Where(x => x.Val.IsDataModelEmpty).OrderByDescending(x => x.Index).ToList())
                {
                    obj.Languages.Remove(itm.Val);
                    ModelState.RemoveKey(string.Format("{0}[{{0}}]",
                        GetFieldName<EmailTemplModel, object>(m => m.Languages)),
                        itm.Index, obj.Languages.Count);
                }
            else obj.Languages = new List<EmailTemplLangModel>();

            #endregion

            if (ModelState.ModLocalize().IsValid)
            {
                #region MoveFile
                try
                {
                    var tmpPath = ModuleUrl.TempFolder.ResolveClientUrl();
                    if (obj.AttachFile != null && obj.AttachFile.StartsWith(tmpPath))
                        obj.AttachFile = MoveFile(ModuleUrl.AttachmentFolder, ModuleUrl.TempFolder, obj.AttachFile);
                }
                catch (Exception er)
                {
                    LogErr(er); goto LB_ERR;
                }

                #endregion

                if (EmailTemplMgr.SaveorUpdate(this, obj))
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
                ModelState.TransformModelError<EmailTemplModel>((field, dispName, modelErr) =>
                    string.Format("[{0}] {1}", dispName, modelErr.ErrorMessage));
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
        public override ActionResult Delete(GridId[] ids)
        {
            if (ids != null && ids.Length > 0 && ids.All(x => x.Id > int.Parse(EmailTempl.EmailtemplIDEqual)) &&
                BaseMgr<EmailTemplModel, EmailTempl>.Delete(this, (sess, obj) => true, null, ids.Select(x => x.Id).ToArray()))
            {
                if (!IndexAction) SetViewMsg(true, StringTable.Deleted);
                return JsonStatus(StringTable.Deleted, true);
            }
            else
            {
                if (!IndexAction) SetViewMsg(false, StringTable.Deleted);
                return JsonStatus(StringTable.Deleted, false);
            }
        }

        #endregion

        #region ValidateCode
        public virtual ActionResult ValidateCode(string code, int? id)
        {
            return ValidateField("Code", code, id);
        }
        #endregion

        #region ValidateType
        public virtual ActionResult ValidateType(int type, int? id)
        {

            return ValidateField("Type", type, id);

        }

        #endregion
        #region ValidateSubject
        public virtual ActionResult ValidateSubject()
        {
            return ValidateLangField("Subject");
        }
        #endregion

    }
}