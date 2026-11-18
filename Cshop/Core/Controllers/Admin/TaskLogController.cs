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

namespace Claw.Modules.Eshop.Controllers.Admin
{
    [AdminPerm]
    public class TaskLogController : BaseController<TaskLogModel, TaskLog>
    {
        private const string IndexView = "Admin/TaskLogsIndex.cshtml";
        private const string EditView = "Admin/TaskLogsEdit.cshtml";

        #region TaskLogController

        public TaskLogController(IDataService ds) :
            base(ds)
        {
        }

        #endregion

        #region Index
        [ValidateInput(false)]
        public ActionResult Index()
        {
            ViewBag.Tasks = TaskMgr.GetAll(this);
            ViewBag.Settings = GetSettings();

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

            var items = TaskLogMgr.GetAll(this, page, pageSize, out totalRows,
                sortDir, sortField);

            return new DataSourceResult() { Data = items, Total = totalRows };
        }

        #endregion
    }
}