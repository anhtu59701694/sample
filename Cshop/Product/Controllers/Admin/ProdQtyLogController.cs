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

namespace Claw.Modules.Eshop.Controllers.Admin
{
    [AdminPerm("CatalogBits", byte.MaxValue)]
    public class ProdQtyLogController : BaseController<ProdQtyLogModel, ProdQtyLog>
    {
        public ProdQtyLogController(IDataService ds) : base(ds) {}

        #region List

        [ValidateInput(false)]
        [AdminPerm("CatalogBits", PermissionMask.View)]
        public ActionResult List([DataSourceRequest] DataSourceRequest request)
        {
            int totalRows;

            TrimReq(request);

            string sortField = "Id";
            var sortDir = SortDirection.Descending;

            GetSortField(request, ref sortField, ref sortDir);

            var productid = GetParam<int?>("productid");
            var variantid = GetParam<int?>("variantid");
            var serialNumber = GetParam<bool?>("serialNumber");

            var items = ProdQtyLogMgr.GetAll(this, productid, variantid, request.Page,
                request.PageSize, out totalRows, sortDir, sortField, serialNumber);

            return Json(new DataSourceResult() { Data = items, Total = totalRows });
        }

        #endregion

        #region Export

        public ActionResult Export(int? productid, int? variantid)
        {
            int total;
            var fileName = "ProdQtyHistory-" + (productid ?? 0) + "-" + DateTime.Now.ToString("ddMMyyyy") + ".xls";
            var list = ProdQtyLogMgr.GetAll(this, productid, variantid, 1, int.MaxValue, out total);
            var fields = new[] { "CreatedDate", "VariantName", "Quantity", "StockQty", "ReasonTxt", "Note", "RefId" };

            if (SettingsBiz.GetSettings().Hidden.Storage2Enabled)
                fields = fields.ToList().SafeProp(m =>
                {
                    m.InsertRange(4, new[] { "StockQty1", "StockQty2" }); m.Add("QtyPosTxt");
                    return m;
                }).ToArray();

            NPOI.HSSF.UserModel.HSSFWorkbook workbook = null;
            ExportHelper.ToExcel(list, ref workbook, string.Format("{0}", productid), list.SafeCount() < 500,
                fields);

            using (var output = new MemoryStream())
            {
                workbook.Write(output);
                return File(output.ToArray(), "application/vnd.ms-excel", fileName);
            }
        }

        #endregion

    }
}