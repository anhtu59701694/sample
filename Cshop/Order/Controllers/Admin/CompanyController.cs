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
    [AdminPerm("OrderBits", byte.MaxValue)]
    public class CompanyController : BaseController<CompanyModel, Company>
    {
        private const string IndexView = "Admin/CompaniesIndex.cshtml";
        private const string EditView = "Admin/CompaniesEdit.cshtml";

        public CompanyController(IDataService ds) : base(ds) { }

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

            var items = CompanyMgr.GetAll(this, page, pageSize, out totalRows, sortDir, sortField, keyword, true);
    
            return new DataSourceResult() { Data = items, Total = totalRows };
        }

        #endregion

        #region ListUser
        [ValidateInput(false)]
        public ActionResult ListUser(int id, string lang, string mode)
        {
            return RedirectToAction("Index", "Customer", new { CompanyId = id, lang, mode });
        }
        #endregion

        #region Input.Get
        [ValidateInput(false)]
        public ActionResult Input(int id, string lang, string optlang, string mode)
        {
            var obj = CompanyMgr.Get(this, id) ?? new CompanyModel();

            ViewBag.Settings = GetSettings();

            return ClawView(EditView, obj);
        }
        #endregion

        #region Input.Post
        [HttpPost]
        [ValidateInput(false)]
        public ActionResult Input([DataSourceRequest]DataSourceRequest request, CompanyModel obj,string name,string orgNumber, string lang, string mode)
        {
            if (obj == null)
                return AcceptJson
                    ? JsonInline(request, new CompanyModel(), false)
                    : RedirectToAction("Index", new { lang, mode });

            #region InlineEdit

            if (obj.InlineEdit)
            {
                if (obj.Id == 0)
                {
                    obj.Name = name;
                    obj.OrgNumber = orgNumber;
                }
                else
                    obj = CopyValidate(obj, CompanyMgr.Get(this, obj.Id), (nVal, oVal) =>
                    {
                        nVal.InlineEdit = true;
                        nVal.Name = name;
                        nVal.OrgNumber = orgNumber;
                    });
            }

            #endregion

            if (ModelState.ModLocalize().IsValid)
            {
                if (null != CompanyMgr.SaveorUpdate(this, obj))
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
                ModelState.TransformModelError<CompanyModel>((field, dispName, modelErr) =>
                    string.Format("[{0}] {1}", dispName, modelErr.ErrorMessage));
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
    }
}