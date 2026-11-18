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
    public class CityController : BaseController<CityModel, City>
    {
        private const string IndexView = "Admin/CitiesIndex.cshtml";
        private const string EditView = "Admin/CitiesEdit.cshtml";

        public CityController(IDataService ds)
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

            var items = CityMgr.GetAll(this, null, request.Page,
                request.PageSize, out totalRows, sortDir, sortField)
                .Action(m => { m.InlineEdit = true; });

            return Json(new DataSourceResult() { Data = items, Total = totalRows });
        }

        #endregion

        #region Input.Get
        [ValidateInput(false)]
        public ActionResult Input(int id, string lang, string optlang, string mode)
        {
            var obj = CityMgr.Get(this, id);

            ViewBag.Languages = LanguageMgr.GetAll();
            ViewBag.Settings = GetSettings();

            if (obj == null)
                obj = new CityModel() { Languages = new List<CityLangModel>() };

            return ClawView(EditView, obj);
        }
        #endregion

        #region Input.Post

        [HttpPost]
        [ValidateInput(false)]
        public ActionResult Input([DataSourceRequest]DataSourceRequest request, CityModel obj,
            string name, string lang, string mode)
        {
            if (obj == null)
                return AcceptJson
                    ? JsonInline(request, new CityModel(), false)
                    : RedirectToAction("Index", new { lang = lang, mode = mode });

            #region Languages

            if (obj.Languages != null)
                foreach (var itm in obj.Languages
                    .Select((x, idx) => new { Val = x, Index = idx })
                    .Where(x => x.Val.IsDataModelEmpty).OrderByDescending(x => x.Index).ToList())
                {
                    obj.Languages.Remove(itm.Val);
                    ModelState.RemoveKey(string.Format("{0}[{{0}}]",
                        GetFieldName<CityModel, object>(m => m.Languages)),
                        itm.Index, obj.Languages.Count);
                }
            else obj.Languages = new List<CityLangModel>();

            #endregion

            #region InlineEdit

            if (obj.InlineEdit)
            {
                if (obj.Id != 0)
                    obj = CopyValidate(obj, CityMgr.Get(this, obj.Id), (nVal, oVal) =>
                    {
                        nVal.InlineEdit = true;
                        nVal.Name = name;
                        TryValidateModel(nVal.GetLang());
                    });
                else
                {
                    var langCode = LanguageMgr.FocusLangCode;
                    obj.GetLangOrAddNew(langCode, () => new CityLangModel() { LangCode = langCode });
                    obj.Name = name;
                }
            }

            #endregion

            if (ModelState.ModLocalize().IsValid)
            {
                if (CityMgr.SaveorUpdate(this, obj))
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
                ModelState.TransformModelError<CityModel>((field, dispName, modelErr) =>
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

        #region ValidateCode
        public virtual ActionResult ValidateCode(string code, int? id)
        {
            return ValidateField("Code", code, id);
        }
        #endregion
    }
}