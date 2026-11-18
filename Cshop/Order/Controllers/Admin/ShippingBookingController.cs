using System.ComponentModel.DataAnnotations;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Text;
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
    public class ShippingBookingController : BaseController<ShippingBookingModel, ShippingBooking>
    {
        private const string IndexView = "Admin/ShippingBookingsIndex.cshtml";
        private const string EditView = "Admin/ShippingBookingsEdit.cshtml";

        #region ShippingBookingController
        public ShippingBookingController(IDataService ds) : base(ds)
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

            var items = ShippingBookingMgr.GetAll(this, page,
                pageSize, out totalRows, sortDir, sortField, keyword);

            return new DataSourceResult() { Data = items, Total = totalRows };
        }

        #endregion

        #region Input
        [ValidateInput(false)]
        [AdminPerm("OrderBits", PermissionMask.View)]
        public ActionResult Input(int id, int? customerId, string lang, string optlang, string mode)
        {     
            var settings = GetSettings().Display.Admin.BringShipping;
            Customer customer = null;
           
            var obj = ShippingBookingMgr.Get(this, id) ?? new ShippingBookingModel()
            {
                Service = (int)ShippingServiceType.Bring,
                WidthInCm = 23,
                HeightInCm = 13,
                LengthInCm = 10,
                RecipientCountryCode = "NO",
                CodAccountNumber = settings.CodAccountNumber,
            };

            if (obj.Id == 0 && null != (customer = CustomerMgr.GetDom(this, customerId ?? 0)))
            {
                obj.RecipientName = customer.DeliverName;
                obj.RecipientAddress = customer.DeliverAddress;
                obj.RecipientPostalCode = customer.DeliverPostnr;
                obj.RecipientCity = customer.DeliverPosted;
                obj.RecipientCountryCode = customer.DeliverCountry.Code;
                obj.RecipientNotiMobile = customer.DeliverMobile.IfNull(customer.DeliverPhone).TrimPhone();
                obj.RecipientNotiEmail = customer.Email;
            }

            ViewBag.Settings = GetSettings();
            ViewBag.Customers = BringShippingMgr.GetCustomers(Request.Url);
            ViewBag.Countries = CountryMgr.GetAll(this);

            return ClawView(EditView, obj);
        }

        [HttpPost]
        [ValidateInput(false)]
        //[ValidateAntiForgeryToken]
        [AdminPerm("OrderBits", PermissionMask.Edit)]
        public ActionResult Input([DataSourceRequest]DataSourceRequest request, ShippingBookingModel obj,
            string name, string lang, string mode)
        {
            var errMsg = "";

            if (obj == null) return RedirectToAction("Index", new { lang, mode });

            ModelState.RemoveKey(GetFieldName<ShippingBookingModel, object>(m => m.Amount));
            ModelState.RemoveKey(GetFieldName<ShippingBookingModel, object>(m => m.Currency));
            ModelState.RemoveKey(GetFieldName<ShippingBookingModel, object>(m => m.CodAccountNumber));
            ModelState.RemoveKey(GetFieldName<ShippingBookingModel, object>(m => m.RecipientNotiEmail));
            ModelState.RemoveKey(GetFieldName<ShippingBookingModel, object>(m => m.RecipientNotiMobile));

            if (ModelState.ModLocalize().IsValid)
            {
                var ret = BringShippingMgr.Booking(this, obj, Request);

                errMsg = ret.Item1 ? null : ret.Item2;

                if (ret.Item1)
                {
                    if (ShippingBookingMgr.SaveorUpdate(this, obj)) SetViewMsgInput(true);
                    else goto LB_ERR;
                }
                else goto LB_ERR;

                return RedirectToAction("Index", new { lang, mode });
            }

        LB_ERR:
            SetViewMsgInput(false, errMsg);

            ViewBag.Settings = GetSettings();
            ViewBag.Customers = BringShippingMgr.GetCustomers(Request.Url);
            ViewBag.Countries = CountryMgr.GetAll(this);

            return ClawView(EditView, obj);
        }
        #endregion

    }
}