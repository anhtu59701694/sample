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
    public class OrderNoteController : BaseController<OrderNoteModel, OrderNote>
    {
        private const string IndexView = "Admin/OrderNotesIndex.cshtml";
        private const string EditView = "Admin/OrderNotesEdit.cshtml";

        public OrderNoteController(IDataService ds) : base(ds) {}

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
            var orderid = GetParam<int>("OrderId");

            TrimReq(request);

            string sortField = "Id", keyword = GetKeyword();
            var sortDir = SortDirection.Descending;

            GetSortField(request, ref sortField, ref sortDir);

            var items = OrderNoteMgr.GetAll(this, orderid, request.Page,
                request.PageSize, out totalRows, sortDir, sortField)
                .Action(m => { m.InlineEdit = true; });
            items.ToList().ForEach(p => p.AuditInfo = p.AuditInfo ?? string.Empty);
            return Json(new DataSourceResult() { Data = items, Total = totalRows });
        }

        #endregion

        #region Input.Get
        [ValidateInput(false)]
        public ActionResult Input(int id, string lang, string optlang, string mode)
        {
            var obj = OrderNoteMgr.Get(this, id);

            ViewBag.Languages = LanguageMgr.GetAll();
            ViewBag.Settings = GetSettings();

            if (obj == null)
                obj = new OrderNoteModel();

            return ClawView(EditView, obj);
        }
        #endregion

        #region Input.Post
        [HttpPost]
        [ValidateInput(false)]
        public ActionResult Input([DataSourceRequest]DataSourceRequest request, OrderNoteModel obj, string lang, string mode)
        {
            if (obj == null)
                return AcceptJson
                    ? JsonInline(request, new OrderNoteModel(), false)
                    : RedirectToAction("Index", new { lang = lang, mode = mode });

            if (obj.Id == 0) obj.CreatedDate = DateTime.Now;

            if (ModelState.ModLocalize().IsValid)
            {
                if (OrderNoteMgr.SaveorUpdate(this, obj))
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
                ModelState.TransformModelError<OrderNoteModel>((field, dispName, modelErr) =>
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
    }
}