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
using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;

namespace Claw.Modules.Eshop.Controllers.Admin
{
    [AdminPerm("CatalogBits", byte.MaxValue)]
    public class InventoryController : BaseController<InventoryModel, Inventory>
    {
        private const string IndexView = "Admin/InventoriesIndex.cshtml";

        #region InventoryController

        public InventoryController(IDataService ds) :
            base(ds)
        {
        }

        static InventoryController() { new InventoryMgr(); }

        #endregion

        #region Index
        [ValidateInput(false)]
        [AdminPerm("CatalogBits", PermissionMask.View)]
        public ActionResult Index(int? statusid)
        {
            ViewBag.Settings = GetSettings();

            ViewBag.EagerData = new GridEagerLoadDataHandler(List);

            return ClawView(IndexView);
        }
        #endregion

        #region List

        [ValidateInput(false)]
        [AdminPerm("CatalogBits", PermissionMask.View)]
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

            var items = InventoryMgr.GetAll(this, page,
                pageSize, out totalRows, sortDir, sortField, keyword);

            return new DataSourceResult() { Data = items, Total = totalRows };
        }

        #endregion

        #region Delete

        [HttpPost]
        [AdminPerm("CatalogBits", PermissionMask.Edit)]
        public override ActionResult Delete(GridId[] ids)
        {
            var retCode = ids.SafeCount() > 0 &&
                      BaseMgr<InventoryModel, Inventory>.Delete(this, false, (sess, obj) => true, null, ids.Select(x => x.Id).ToArray());

            if (!IndexAction) SetViewMsg(retCode, StringTable.Delete);

            return JsonStatus(StringTable.Delete, retCode);
        }

        #endregion

        #region Export

        [AdminPerm("CatalogBits", PermissionMask.View)]
        public ActionResult Export(int id)
        {
            int totalRows;
            var lim = new DAL.Tuple2<int, int>(3, 5);
            var model = InventoryMgr.Get(this, id);
            var fileName = string.Format("Inventory-{0:ddMMyyyy}-{1}.xls", model.SafeProp(x => x.CreatedDate),
                model.SafeProp(x => x.UserName));
            var updInv = model.SafeProp(m => m.Type).InList(InventoryType.Update);
            var list = InventoryMgr.GetDetail(this, id, 1, int.MaxValue, out totalRows, SortDirection.Ascending);
            var lstProd = list.Where(x => x.VariantId == null).ToList();
            var lstVari = list.Where(x => x.VariantId != null).ToList();
            var cols = updInv
                ? new[] { "ProductId", "ProductSku", "ProductName", "OldQuantity", "NewQuantity", "DiffQuantity", "OldLocation", "NewLocation" }
                : new[] { "ProductId", "ProductSku", "ProductName", "CostPrice.Value", "Quantity", "Total.Value", "Location", "VisibleWeb", "VisiblePos" };
            HSSFWorkbook workbook = null;
            ICellStyle numStyle = null;
            Action<HSSFWorkbook, ISheet, ICell> filter = (workbk, sheet, cell) =>
            {
                if (cell.ColumnIndex < lim.Item1 || cell.ColumnIndex > lim.Item2) return;
                if (numStyle == null)
                {
                    numStyle = workbk.CreateCellStyle();
                    numStyle.BorderLeft =
                        numStyle.BorderRight =
                            numStyle.BorderTop = numStyle.BorderBottom = NPOI.SS.UserModel.BorderStyle.Thin;
                    numStyle.Alignment = HorizontalAlignment.Right;
                }
                cell.CellStyle = numStyle;
                cell.SetCellType(CellType.Numeric);
            };

            ExportHelper.ToExcel(lstProd, ref workbook, StringTable.Product, list.SafeCount() < 500,
                cols, null, null, filter);

            cols = updInv
              ? new[] { "VariantId", "VariantSku", "ProductId", "ProductName", "VariantName", "OldQuantity", "NewQuantity", "DiffQuantity", "OldLocation", "NewLocation" }
              : new[] { "VariantId", "VariantSku", "ProductId", "ProductName", "VariantName", "CostPrice.Value", "Quantity", "Total.Value", "Location" };

            lim.Item1 = 5;
            lim.Item2 = 7;
            
            ExportHelper.ToExcel(lstVari, ref workbook, StringTable.Variant, list.SafeCount() < 500,
                cols, null, null, filter);

            using (var output = new MemoryStream())
            {
                workbook.Write(output);
                return File(output.ToArray(), "application/vnd.ms-excel", fileName);
            }
        }

        #endregion
    }
}