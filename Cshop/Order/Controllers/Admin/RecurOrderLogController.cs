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
    public class RecurOrderLogController : BaseController<RecurOrderLogModel, RecurOrderLog>
    {
        private const string IndexView = "Admin/RecurOrderLogsIndex.cshtml";
        private const string EditView = "Admin/RecurOrderLogsEdit.cshtml";

        public RecurOrderLogController(IDataService ds) : base(ds) { }

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
            var recurOrderId = GetParam<int>("RecurOrderId");

            TrimReq(request);

            string sortField = "Id", keyword = GetKeyword();
            var sortDir = SortDirection.Descending;

            GetSortField(request, ref sortField, ref sortDir);

            var items = RecurOrderLogMgr.GetAll(this, recurOrderId, request.Page,
                request.PageSize, out totalRows, sortDir, sortField);

            return Json(new DataSourceResult() { Data = items, Total = totalRows });
        }

        #endregion
    }
}