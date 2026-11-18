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
using Claw.Core.DAL;

namespace Claw.Modules.Eshop.Controllers.Admin
{
    [AdminPerm("OrderBits", byte.MaxValue)]
    public class CustGroupController : BaseController<CustGroupModel, CustGroup>
    {
        private const string IndexView = "Admin/CustGroupsIndex.cshtml";
        private const string EditView = "Admin/CustGroupsEdit.cshtml";

        #region CustGroupController
        public CustGroupController(IDataService ds) :
            base(ds)
        {
        }
        #endregion

        #region Index
        [ValidateInput(false)]
        [AdminPerm("OrderBits", PermissionMask.View)]
        public ActionResult Index()
        {
            ViewBag.Settings = GetSettings();

            ViewBag.EagerData = new GridEagerLoadDataHandler(List);

            return ClawView(IndexView);
        }
        #endregion

        #region List

        [ValidateInput(false)]
        [AdminPerm("OrderBits", PermissionMask.View)]
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

            var items = CustGroupMgr.GetAll(this, page,
                pageSize, out totalRows, sortDir, sortField, true)
                .Action(m => { m.InlineEdit = m.QuickAdd = true;});

            return new DataSourceResult() { Data = items, Total = totalRows };
        }

        #endregion

        #region ListCust
        [ValidateInput(false)]
        public ActionResult ListCust(int id, string lang,string mode)
        {
            return RedirectToAction("Index", "Customer", new {GroupId = id, lang, mode});
        }
        #endregion

        #region ListDiscount
        [ValidateInput(false)]
        public ActionResult ListDiscount(int id, string lang, string mode)
        {
            return RedirectToAction("Index", "Discount", new { GroupId = id, lang, mode });
        }
        #endregion

        #region Input
        [ValidateInput(false)]
        [AdminPerm("OrderBits", PermissionMask.View)]
        public ActionResult Input(int id, string categories, string lang, string optlang, string mode)
        {
            var obj = CustGroupMgr.Get(this, id);

            ViewBag.Settings = GetSettings();

            if (obj == null)
                obj = new CustGroupModel();

            if (obj.Id != 0 && this is GroupDiscountController)
                obj.InitCategories(this, categories);

            return ClawView(EditView, obj);
        }

        [HttpPost]
        [ValidateInput(false)]
        //[ValidateAntiForgeryToken]
        [AdminPerm("OrderBits", PermissionMask.Edit)]
        public ActionResult Input([DataSourceRequest]DataSourceRequest request, CustGroupModel obj,
            string name, string lang, string mode)
        {
            if (obj == null) return RedirectToAction("Index", new { lang, mode });

            string errMsg = null;

            if (ModelState.ModLocalize().IsValid)
            {
                if (BaseMgr.Exists<CustGroup>(m => m.Id, obj.Id, null, new KeyValuePair() {Key = "Name", Value = obj.Name}))
                {
                    errMsg = StringTable.ErrMsgUniqueConstraint.ModErrLocalize();
                    ModelState.AddModelError("Name", errMsg);
                    goto LB_ERR;
                }

                if (CustGroupMgr.SaveorUpdate(this, obj))
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
                ModelState.TransformModelError<CustGroupModel>((field, dispName, modelErr) =>
                    string.Format(obj.QuickAdd ? "{1}" : "[{0}] {1}", dispName, modelErr.ErrorMessage));
            else
            {
                SetViewMsgInput(false);
                ViewBag.Settings = GetSettings();

                if (obj.Id != 0 && this is GroupDiscountController)
                    obj.InitCategories(this, null, true);

            }

            return obj.InlineEdit
                ? JsonInline(request, obj, false, null, JsonRequestBehavior.DenyGet, errMsg)
                : ClawView(EditView, obj);
        }
        #endregion

        #region Delete

        [HttpPost]
        [AdminPerm("OrderBits", PermissionMask.Edit)]
        public override ActionResult Delete(GridId[] ids)
        {
            var retCode = ids.SafeCount() > 0 && BaseMgr<CustGroupModel, CustGroup>.Delete(this, false,
                (sess, obj) => obj != null && obj.CustomerCGroups.SafeCount() == 0 && obj.DiscountCGroups.SafeCount() == 0, 
                null, ids.Select(x => x.Id).ToArray());

            if (!IndexAction) SetViewMsg(retCode, StringTable.Delete);

            return JsonStatus(StringTable.Delete, retCode);
        }

        #endregion

        #region Price4Login

        [HttpPost]
        public virtual ActionResult Price4Login(int id, bool value)
        {
            var ret = CustGroupMgr.Update(this, id, m => m.Price4Login, value);

            return JsonStatus(StringTable.Update, ret);
        }

        #endregion

        #region Discount

        [HttpPost]
        public virtual ActionResult Discount(int id, bool value)
        {
            var ret = CustGroupMgr.Update(this, id, m => m.Discount, value);

            return JsonStatus(StringTable.Update, ret);
        }

        #endregion
    }

    public class GroupDiscountController : CustGroupController
    {
        public GroupDiscountController(IDataService ds) : base(ds) { }
    }
}