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
    public class RecurOrderController : BaseController<RecurOrderModel, RecurOrder>
    {
        private const string IndexView = "Admin/RecurOrdersIndex.cshtml";
        private const string EditView = "Admin/RecurOrdersEdit.cshtml";

        #region RecurOrderController
        public RecurOrderController(IDataService ds)
            : base(ds)
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
            var orderid = GetParam<int?>("orderid");
            var recurring = GetParam<int?>("recurring");

            var items = RecurOrderMgr.GetAll(this, orderid, recurring, page, pageSize, out totalRows, sortDir, sortField);

            return new DataSourceResult() { Data = items, Total = totalRows };
        }

        #endregion

        #region ListOrder
        [ValidateInput(false)]
        public ActionResult ListOrder(int id, string lang, string mode)
        {
            return RedirectToAction("Index", "Order", new { RecurOrderId = id, lang, mode });
        }
        #endregion

        #region EditOrder
        [ValidateInput(false)]
        public ActionResult EditOrder(int id, string lang, string mode)
        {
            return RedirectToAction("Input", "Order", new { id, lang, mode });
        }
        #endregion

        #region Input.Get
        [ValidateInput(false)]
        [AdminPerm("OrderBits", PermissionMask.View)]
        public ActionResult Input(int id, string lang, string optlang, string mode)
        {
            var obj = RecurOrderMgr.Get(this, id) ?? new RecurOrderModel();

            obj.NextOrder = Mapper.Map<OrderModel>(OrderMgr.Recur(obj.GetRaw(), this, skipZeroQty: false));

            obj.NextOrder.InitMandatoryInfo();
            obj.NextOrder.InitOrderDetails(this);

            ViewBag.Settings = GetSettings();
            ViewBag.Currencies = CurrencyMgr.GetAll(this, null, true).Select(x => x.GetRaw()).ToList();

            return ClawView(EditView, obj);
        }
        #endregion

        #region Input.Post
        [HttpPost]
        [ValidateInput(false)]
        [AdminPerm("OrderBits", PermissionMask.Edit)]
        public ActionResult Input([DataSourceRequest]DataSourceRequest request, RecurOrderModel obj, string lang, string mode)
        {
            if (obj == null)
                return AcceptJson
                    ? JsonInline(request, new RecurOrderModel(), false)
                    : RedirectToAction("Index", new { lang, mode });

            ModelState.RemoveKey("NextOrder");

            if (ModelState.ModLocalize().IsValid)
            {
                if (RecurOrderMgr.SaveorUpdate(this, obj, null, Request))
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
                ModelState.TransformModelError<RecurOrderModel>((field, dispName, modelErr) =>
                    string.Format("[{0}] {1}", dispName, modelErr.ErrorMessage));
            else
            {
                SetViewMsgInput(false);

                obj.NextOrder = Mapper.Map<OrderModel>(OrderMgr.Recur(RecurOrderMgr.GetDom(this, obj.Id), this, skipZeroQty: false));

                obj.NextOrder.InitMandatoryInfo();
                obj.NextOrder.InitOrderDetails(this);

                ViewBag.Settings = GetSettings();
                ViewBag.Currencies = CurrencyMgr.GetAll(this, null, true).Select(x => x.GetRaw()).ToList();
            }

            return obj.InlineEdit
                ? JsonInline(request, obj, false)
                : ClawView(EditView, obj);
        }
        #endregion
    }
}