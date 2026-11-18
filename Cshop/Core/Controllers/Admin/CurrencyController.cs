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
    public class CurrencyController : BaseController<CurrencyModel, Currency>
    {
        private const string IndexView = "Admin/CurrenciesIndex.cshtml";
        private const string EditView = "Admin/CurrenciesEdit.cshtml";

        public CurrencyController(IDataService ds)
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

            var items = CurrencyMgr.GetAll(this, null, page, pageSize, out totalRows, sortDir, sortField)
                .Action(m => { m.InlineEdit = true; });

            return new DataSourceResult() { Data = items, Total = totalRows };
        }

        #endregion

        #region Input.Get

        [ValidateInput(false)]
        public ActionResult Input(int id, string lang, string optlang, string mode)
        {
            var obj = CurrencyMgr.Get(this, id);

            ViewBag.Languages = LanguageMgr.GetAll();
            ViewBag.Settings = GetSettings();

            if (obj == null)
                obj = new CurrencyModel() {Active = true, DecimalPlaces = 2, DisplayOrder = 1000 };

            return ClawView(EditView, obj);
        }

        #endregion

        #region Input.Post

        [HttpPost]
        [ValidateInput(false)]
        public ActionResult Input([DataSourceRequest]DataSourceRequest request, CurrencyModel obj,
            string name, string lang, string mode)
        {
            if (obj == null)
                return AcceptJson
                    ? JsonInline(request, new CurrencyModel(), false)
                    : RedirectToAction("Index", new { lang, mode });

            if (ModelState.ModLocalize().IsValid)
            {
                if (CurrencyMgr.SaveorUpdate(this, obj))
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
                ModelState.TransformModelError<CurrencyModel>((field, dispName, modelErr) =>
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
        public override ActionResult Delete(GridId[] ids)
        {
            var retCode = (ids != null && ids.Length > 0 && BaseMgr<CurrencyModel, Currency>.Delete(this,
                (sess, obj) => !obj.IsDefault(), null, ids.Select(x => x.Id).ToArray()));

            if (!IndexAction) SetViewMsg(retCode, StringTable.Deleted);

            return JsonStatus(StringTable.Deleted, retCode);
        }

        #endregion

        #region ValidateCode
        public virtual ActionResult ValidateCode(string code, int? id)
        {
            return ValidateField("Code", code, id);
        }
        #endregion

        #region SetDefault

        [HttpPost]
        public virtual ActionResult SetDefault(int id, bool value)
        {
            var ret = CurrencyMgr.Update(this, id, value);

            return JsonStatus(StringTable.UpdateDefault, ret);
        }

        #endregion

        #region Enable

        [HttpPost]
        [AdminPerm]
        public override ActionResult Enable(int id, bool active)
        {
            if (!active && CurrencyMgr.CheckDefault(this, id))
                return JsonStatus(StringTable.CanNotDisableObjectDefault, false);
                
            bool ret = BaseMgr<CurrencyModel, Currency>.Enable(this, id, active);

            return JsonStatus(StringTable.UpdateStatus, ret);
        }

        #endregion

    }
}