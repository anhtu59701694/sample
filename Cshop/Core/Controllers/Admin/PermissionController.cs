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
    public class PermissionController : BaseController<PermissionModel, Permission>
    {
        private const string IndexView = "Admin/PermissionsIndex.cshtml";
        private const string EditView = "Admin/PermissionsEdit.cshtml";

        #region PermissionController
        public PermissionController(IDataService ds)
            : base(ds)
        {
        } 
        #endregion

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

            var items = PermissionMgr.GetAll(this, null, page, pageSize, out totalRows, sortDir, sortField)
                .Action(x => x.Users = x.GetRaw().GetUserCount());

            return new DataSourceResult() { Data = items, Total = totalRows };
        }

        #endregion

        #region ListUser
        [ValidateInput(false)]
        public ActionResult ListUser(int id, string lang, string mode)
        {
            return RedirectToAction("Index", "User", new {roleid = id, lang, mode});
        }
        #endregion

        #region Input.Get
        [ValidateInput(false)]
        public ActionResult Input(int id, string lang, string optlang, string mode)
        {
            if (id == 0)
                return RedirectToRoute("Admin_Default",
                    new {Controller = "Roles", Action = "Index", id, lang, optlang, mode});

            var obj = PermissionMgr.Get(this, -1*id);

            ViewBag.Settings = GetSettings();

            return ClawView(obj == null ? IndexView : EditView, obj);
        }
        #endregion

        #region Input.Post

        [HttpPost]
        [ValidateInput(false)]
        public ActionResult Input([DataSourceRequest]DataSourceRequest request, PermissionModel obj,
            string name, string lang, string mode)
        {
            if (obj == null)
                return AcceptJson
                    ? JsonInline(request, new PermissionModel(), false)
                    : RedirectToAction("Index", new { lang, mode });

            #region InlineEdit

            if (obj.InlineEdit)
            {
                if (obj.Id != 0)
                    obj = CopyValidate(obj, PermissionMgr.Get(this, obj.Id), (nVal, oVal) =>
                    {
                        nVal.InlineEdit = true;
                    });
            }

            #endregion

            if (ModelState.ModLocalize().IsValid)
            {
                if (PermissionMgr.SaveorUpdate(this, obj))
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
                ModelState.TransformModelError<PermissionModel>((field, dispName, modelErr) =>
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
    }

    #region UserController
    [AdminPerm]
    public class UserController : BaseController<UserModel, Claw.Core.Domain.User>
    {
        private const string IndexView = "Admin/UsersIndex.cshtml";

        #region UserController

        public UserController(IDataService ds)
            : base(ds)
        {
        }

        #endregion

        #region Index

        [ValidateInput(false)]
        public ActionResult Index(int? roleId)
        {
            ViewBag.Settings = GetSettings();
            ViewBag.Role = BaseMgr.Get<Claw.Core.Domain.Role>(roleId ?? 0);

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
            var roleid = GetParam<int?>("roleid");

            var items = UserMgr.GetAll(this, roleid, page, pageSize, out totalRows, sortDir, sortField);

            return new DataSourceResult() { Data = items, Total = totalRows };
        }

        #endregion

        #region Input.Get

        [ValidateInput(false)]
        public ActionResult Input(int id, string lang, string optlang, string mode)
        {
            return RedirectToRoute("Admin_Default", new { Controller = "Users", Action = "Edit", id, lang, optlang, mode });
        }

        #endregion
    } 
    #endregion
}