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
    public class DistrictController : BaseController<DistrictModel, District>
    {
        private const string IndexView = "Admin/DistrictsIndex.cshtml";
        private const string EditView = "Admin/DistrictsEdit.cshtml";

        public DistrictController(IDataService ds)
            : base(ds)
        {
        }

        #region Index
        [ValidateInput(false)]
        public ActionResult Index()
        {
            ViewBag.Settings = GetSettings();

            return ClawView(IndexView);
        }
        #endregion

        #region List

        [ValidateInput(false)]
        public ActionResult List([DataSourceRequest] DataSourceRequest request)
        {
            int totalRows;

            TrimReq(request);

            string sortField = "Id";
            var sortDir = SortDirection.Descending;

            GetSortField(request, ref sortField, ref sortDir);

            var items = DistrictMgr.GetAll(this, null, request.Page,
                request.PageSize, out totalRows, sortDir, sortField)
                .Action(m => { m.InlineEdit = true; });

            return Json(new DataSourceResult() { Data = items, Total = totalRows });
        }

        #endregion

        #region Input.Get
        [ValidateInput(false)]
        public ActionResult Input(int id, string lang, string optlang, string mode)
        {
            var obj = DistrictMgr.Get(this, id);

            ViewBag.Settings = GetSettings();

            if (obj == null)
                obj = new DistrictModel();

            return ClawView(EditView, obj);
        }
        #endregion

        #region Input.Post
        [HttpPost]
        [ValidateInput(false)]
        public ActionResult Input([DataSourceRequest]DataSourceRequest request, DistrictModel obj,
            string lang, string mode)
        {
            if (obj == null)
                return AcceptJson
                    ? JsonInline(request, new DistrictModel(), false)
                    : RedirectToAction("Index", new { lang = lang, mode = mode });

            if (ModelState.ModLocalize().IsValid)
            {
                if (DistrictMgr.SaveorUpdate(this, obj))
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
                ModelState.TransformModelError<DistrictModel>((field, dispName, modelErr) =>
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

        #region ValidateCode
        public virtual ActionResult ValidateCode(string code, int? id)
        {
            return ValidateField("Code", code, id);
        }
        #endregion
    }
}