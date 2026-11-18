using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
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
using Claw.Modules.Eshop;
using Claw.Modules.Eshop.Domain;
using Claw.Modules.Eshop.Models;
using System.Data;
using Claw.Modules.Eshop.Helper;
using System.Globalization;
using System.Linq.Expressions;
using NHibernate;
using NHibernate.Linq;
using NHibernate.Util;
using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;
using Quartz.Xml.JobSchedulingData20;

namespace Claw.Modules.Eshop.Controllers.Admin
{
    [AdminPerm("CatalogBits", byte.MaxValue)]
    public class ImportExportController : BaseController<ProductModel, Domain.Product>
    {
        private const string IndexView = "Admin/ImportExportsIndex.cshtml";

        #region ImportExportController
        public ImportExportController(IDataService ds) :
            base(ds)
        {
            InitImageSettings(x => x.Display.Product.Image, x => x.Display.Product.Thumb);
        }
        #endregion

        #region Index
        [ValidateInput(false)]
        [AdminPerm("CatalogBits", PermissionMask.View)]
        public ActionResult Index()
        {
            ViewBag.ListFields = null;
            ViewBag.Settings = GetSettings();

            return ClawView(IndexView);
        }
        #endregion

        #region List

        [ValidateInput(false)]
        [AdminPerm("CatalogBits", PermissionMask.View)]
        public ActionResult List([DataSourceRequest] DataSourceRequest request, bool countVariant = false)
        {
            //TempData["Html"] = "";
            int totalRows;
            var name = GetParam<string>("name");
            var sku = GetParam<string>("sku");
            var type = GetParam<int?>("type");
            var status = GetParam<int?>("status");
            var visibility = GetParam<bool?>("visibility");
            var categoryid = GetParam<int?>("categoryid");
            var manufacturerId = GetParam<int?>("manufacturerid");
            var exceptIds = GetParam<string>("exceptids");

            var advance = GetParam<string>("advance");

            TrimReq(request);

            string sortField = "Id", keyword = "1".Equals(advance) ? null : GetKeyword();
            var sortDir = SortDirection.Descending;

            GetSortField(request, ref sortField, ref sortDir);

            var items = ProductMgr.GetAll(this, LanguageMgr.FocusLangCode, categoryid, null, request.Page,
                request.PageSize, out totalRows, sortDir, sortField, (keyword ?? "").Trim(), exceptIds, type, status,
                manufacturerId, visibility, name, sku)
                .Action(m => { m.InlineEdit = true; });

            foreach (var item in items)
            {
                //item.Information.InitLink(item, Request);
                item.InitVariants(this);
                if (countVariant) item.VariantCount = item.GetRaw().GetVariantCount();
            }

            return Json(new DataSourceResult() { Data = items, Total = totalRows });
        }

        #endregion

        #region LoadExcel

        public ActionResult LoadExcel()
        {
            return RedirectToAction("Index");
        }

        /// <summary>
        /// <see cref="http://stackoverflow.com/questions/5855813/npoi-how-to-read-file-using-npoi"/>
        /// </summary>
        [HttpPost]
        [AdminPerm("CatalogBits", PermissionMask.Edit)]
        public ActionResult LoadExcel(HttpPostedFileBase getFile)
        {
            var settings = Helper.SettingsBiz.GetSettings();
            var action = StringTable.LoadExcelFile;

            if (getFile == null) { SetViewMsg(false, action); return ClawView(IndexView); }

            var listFields = new List<SelectListItem>();
            try
            {
                ViewBag.ExcelFileName = getFile.FileName;

                var hssfwb = new HSSFWorkbook(getFile.InputStream);
                var result = new DataSet();

                if (hssfwb.NumberOfSheets == 0) { SetViewMsg(false, action); return ClawView(IndexView); }

                #region ToDataSet

                IRow row;
                var fmtInfo = OMoney.FormatInfo(-1);
                var sheet = hssfwb.GetSheetAt(0);
                var table = new DataTable(sheet.SheetName);
                result.Tables.Add(table);
                for (int r = sheet.FirstRowNum; r <= sheet.LastRowNum; r++)
                {
                    if (null != (row = sheet.GetRow(r)) && row.Cells.Count != 0)
                    {
                        if (table.Columns.Count == 0)
                        {
                            for (int c = 0; c <= row.LastCellNum; c++)
                            {
                                var cell = row.GetCell(c);
                                if (cell != null && !cell.ToString(fmtInfo).IsNullOrEmpty())
                                {
                                    table.Columns.Add(cell.ToString());
                                    listFields.Add(new SelectListItem()
                                    {
                                        Text = cell.StringCellValue,
                                        Value = cell.StringCellValue
                                    });
                                }
                                else if (table.Columns.Count == 0)
                                {
                                    SetViewMsg(false, action); return ClawView(IndexView);
                                }
                            }
                        }
                        else
                        {
                            var dataRw = table.NewRow();
                            for (int c = 0; c < table.Columns.Count; c++)
                            {
                                var idx = c - 0;
                                var cell = row.GetCell(c);
                                if(settings.Hidden.ProdImportLineBreakNoConvert)
                                    dataRw[idx] = (cell != null ? cell.ToString(fmtInfo) : null) ?? "";
                                else
                                    dataRw[idx] = (cell != null ? cell.ToString(fmtInfo).Replace("\r\n", "</br>").Replace("\n", "</br>") : null) ?? "";
                            }
                            var nilCells = 0;
                            for (int i = 0; i < table.Columns.Count; i++)
                            {
                                if (dataRw[i] == null) dataRw[i] = "";
                                if (string.IsNullOrWhiteSpace(dataRw[i] as string)) nilCells++;
                            }
                            if (nilCells < table.Columns.Count) table.Rows.Add(dataRw);
                        }
                    }
                }

                #endregion

                Session["ExcelDataSet"] = result;
                Session["ImportedImageList"] = null;

                var anyDupCol = listFields.Count != listFields.GroupBy(x => x.Value.ToLower()).Count();
                if (result != null && result.Tables.Count != 0 && result.Tables[0].Rows.Count != 0 && !anyDupCol)
                {
                    //result.Tables[0].InitColumnName();
                    SetViewMsg(true, action);
                }
                else SetViewMsg(false, action, anyDupCol ? StringTable.DuplicateColumnName : null);
            }
            catch (Exception ex)
            {
                SetViewMsg(false, action, ex.Message);
            }

            ViewBag.ListFields = listFields.OrderBy(x => x.Text).ToList();
            ViewBag.Settings = GetSettings();

            return ClawView(IndexView);
        }
        
        #endregion

        #region UploadImageImport
        [HttpPost]
        [AdminPerm("CatalogBits", PermissionMask.Edit)]
        public ActionResult UploadImageImport(HttpPostedFileBase imageUpload)
        {
            if (imageUpload == null || !Path.GetExtension(imageUpload.FileName).EqualsIgCase(".zip"))
                return JsonUpload(new
                {
                    Status = ActionResultStatus.Fail,
                    Message = string.Format(StringTable.TemplActionUnsuccess, StringTable.UploadImage.ToLower())
                });

            var images = new List<ImgUL>();

            var retCode = DoUploadImageZip(null, imageUpload, true, out images);

            Session["ImportedImageList"] = images;

            return JsonUpload(new
            {
                Status = retCode ? ActionResultStatus.Success : ActionResultStatus.Fail,
                Message = string.Format(retCode ? StringTable.TemplActionSuccess : StringTable.TemplActionUnsuccess, StringTable.UploadImage.ToLower())
            });
        }

        #endregion

        #region UpdateExcel

        [HttpPost]
        [AdminPerm("CatalogBits", PermissionMask.Edit)]
        public ActionResult UpdateExcel(bool validate, string lang)
        {      
            var errors = new List<string>();
            var dataset = (DataSet)Session["ExcelDataSet"];
            if (dataset == null || dataset.Tables.Count == 0) return RedirectToAction("Index", new { lang });

            var dicProdOk = new Dictionary<int, Domain.ProductBase>();
            var variantCount = 0;
            var datatable = dataset.Tables[0];

            var properties = PropertyMgr.GetAll(this, true);
            var expType = typeof(ProductExportModel);
            var lstField = Request.Form.AllKeys.Where(x => x.StartsWith("ddl"))
                .Select(x => Request.Form[x])
                .Where(x => !x.IsNullOrEmpty())
                .ToList();

            if (properties.Count != 0)
            {
                var fieldArr = new[] { "SKU", "VariantSKU" }.Select(expType.GetProperty)
                    .Select(x => x.GetDisplayName()).ToArray();
                for (var k = 0; k < fieldArr.Length; k += 2)
                    if (lstField.Any(x => x.EqualsIgCase(fieldArr[k]))) lstField.Add(fieldArr[k + 1]);

                if (datatable.Columns.IndexOf(fieldArr[1]) != -1 && lstField.All(x => !x.EqualsIgCase(fieldArr[1])))
                    lstField.Add(fieldArr[1]);
            }

            var colProdMap = datatable.ToProp2ColumnMap(expType, (p, f) => lstField.Any(x => x.EqualsIgCase(f)), false);
            var idColIndex = datatable.Columns.IndexOf("Id");
            var varSkuColIndex = colProdMap.ContainsKey("VariantSKU") ? datatable.Columns.IndexOf(colProdMap["VariantSKU"]) : -1;
            var proSkuColIndex = colProdMap.ContainsKey("SKU") ? datatable.Columns.IndexOf(colProdMap["SKU"]) : -1;
            var qtyColIndex = colProdMap.ContainsKey("Quantity") ? datatable.Columns.IndexOf(colProdMap["Quantity"]) : -1;

            var tranRows = datatable.Rows.Cast<DataRow>().Select(row => new
            {
                Row = row,
                SId = idColIndex == -1 ? "0" : row["Id"].ToString(),
                VariantSku = varSkuColIndex == -1 ? "" : row[varSkuColIndex].ToString(),
                ProdSku = proSkuColIndex == -1 ? "" : row[proSkuColIndex].ToString(),
                Quantity = qtyColIndex == -1 ? "" : row[qtyColIndex].ToString(),
            }).ToList();

            var session = this.GetSession();
            var db = new DefaultDb(session);
            var dicProdId = db.DataService.GetAll<Domain.ProductBase>().ToDictionary(x => x.Id, x => x);
            var dicprodSku = dicProdId.GroupBy(x => (x.Value.SKU ?? "").ToLower()).ToDictionary(x => x.Key, x => x.First().Value);
            var dicVariSku = db.DataService.GetAll<Domain.Variant>()
                .GroupBy(x => (x.SKU ?? "").ToLower())
                .ToDictionary(x => x.Key, x => x.First());

            session.SetBatchSize(100);

            for (var i = 0; i < tranRows.Count; i++)
            {
                try
                {
                    var row = tranRows[i].Row;
                    var sId = tranRows[i].SId;
                    var variantSku = (tranRows[i].VariantSku ?? "").ToLower();
                    var prodSku = (tranRows[i].ProdSku ?? "").ToLower();
                    var quantity = tranRows[i].Quantity;
                    var variant = null as VariantBase;

                    if (string.IsNullOrWhiteSpace(sId)) row["Id"] = sId = "0";

                    var id = sId.TryParseInt() ?? -1;
                    var product = (id > 0 ? dicProdId.SafeGet(id) : null) ??
                                  (prodSku.IsNullOrEmpty() ? null : dicprodSku.SafeGet(prodSku));

                    #region Variant

                    if (!variantSku.IsNullOrEmpty())
                    {
                        if (product != null)
                            variant = product.Variants.FirstOrDefault(x => !x.Deleted && x.SKU.EqualsIgCase(variantSku, true));

                        if (variant == null)
                            variant = dicVariSku.SafeGet(variantSku);

                        if (product == null && variant != null) product = variant.Product;
                    }

                    #endregion

                    #region Validate

                    if (product == null)
                    {
                        errors.Add(string.Format("Invalid Id = {1} at row {0}", i + 2, sId));
                        continue;
                    }

                    int qtyVal;
                    if (quantity.IsNullOrEmpty() || !int.TryParse(quantity, out qtyVal))
                    {
                        errors.Add(string.Format("Invalid column [{2}] = {1} at row {0}", i + 2, quantity,
                            colProdMap["Quantity"]));
                        continue;
                    }

                    if (variant == null && !variantSku.IsNullOrEmpty())
                    {
                        errors.Add(string.Format("Invalid column [{2}] = {1} at row {0}", i + 2, variantSku,
                            colProdMap["VariantSKU"]));
                        continue;
                    }

                    #endregion

                    var delta = qtyVal - (variant == null ? product.Quantity : variant.Quantity);
                    if (delta != 0)
                    {
                        Product.DAL.ProductMgr.UpdateQuantity(db, product, variant, delta, false, false, true,false,
                            new ProdQtyLogModel() { Reason = (int)ProdQtyLogReason.Imported });

                        session.Update(product);

                        if (variant != null) session.Update(variant);
                    }

                    if (++variantCount%100 == 0) session.Flush();

                    dicProdOk[product.Id] = product;
                }
                catch (Exception err)
                {
                    session.Clear();
                    errors.Add(string.Format("Error = {1} at row {0}", i + 2, err.Message));
                }
            }

            session.Flush();

            return JsonStatus(StringTable.ImportData, errors.Count == 0, errors.Count == 0 
                ? string.Format(StringTable.ImportProductOkMsg, dicProdOk.Count, variantCount)
                : string.Format(StringTable.ImportProductPartialOkMsg, dicProdOk.Count, variantCount, datatable.Rows.Count, errors.Take(5).ToString("<br>")));  
        }

        #endregion

        #region ImportExcel
        [HttpPost]
        [AdminPerm("CatalogBits", PermissionMask.Edit)]
        public ActionResult ImportExcel(bool validate, string lang, bool skipDuplicatedProd = false, int pageIndex = 1, int pageSize = int.MaxValue)
        {
            var errors = new List<string>();
            var dataset = (DataSet)Session["ExcelDataSet"];
            var imageList = ((List<ImgUL>)Session["ImportedImageList"] ?? new List<ImgUL>())
                .Select(x => new { x.FileName, x.FilePath, x.RawFileName }).ToList();

            if (dataset == null || dataset.Tables.Count == 0) return RedirectToAction("Index", new { lang });

            var expType = typeof(ProductExportModel);
            var datatable = dataset.Tables[0];
            var properties = PropertyMgr.GetAll(this, true);

            var tmpPath = Core.ModuleUrl.TempFolder.ResolveClientUrl();
            var lstField = Request.Form.AllKeys.Where(x => x.StartsWith("ddl"))
                .Select(x => Request.Form[x])
                .Where(x => !x.IsNullOrEmpty())
                .ToList();

            var updateCols = new[] {"Id", "SKU", "VariantSKU", "Quantity"}.Select(expType.GetProperty).Select(x => x.GetDisplayName()).ToList();
            if (datatable.Columns.Cast<DataColumn>().All(x => updateCols.Any(z => z.EqualsIgCase(x.ColumnName))))
                return UpdateExcel(validate, lang);

            if (properties.Count != 0)
            {
                var fieldArr = new[] {"SKU", "VariantSKU"}.Select(expType.GetProperty)
                    .Select(x => x.GetDisplayName()).ToArray();
                for (var k = 0; k < fieldArr.Length; k += 2)
                    if (lstField.Any(x => x.EqualsIgCase(fieldArr[k]))) lstField.Add(fieldArr[k + 1]);

                if (datatable.Columns.IndexOf(fieldArr[1]) != -1 && lstField.All(x => !x.EqualsIgCase(fieldArr[1])))
                    lstField.Add(fieldArr[1]);
            }

            var colProdMap = datatable.ToProp2ColumnMap(expType, (p, f) => lstField.Any(x => x.EqualsIgCase(f)), false);
            var idColIndex = datatable.Columns.IndexOf("Id");
            var varSkuColIndex = colProdMap.ContainsKey("VariantSKU") ? datatable.Columns.IndexOf(colProdMap["VariantSKU"]) : -1;
            var proSkuColIndex = colProdMap.ContainsKey("SKU") ? datatable.Columns.IndexOf(colProdMap["SKU"]) : -1;
            var proAliasColIndex = colProdMap.ContainsKey("Alias") ? datatable.Columns.IndexOf(colProdMap["Alias"]) : -1;
            var proNameColIndex = colProdMap.ContainsKey("Name") ? datatable.Columns.IndexOf(colProdMap["Name"]) : -1;
            var varPropColIndies = properties.Select((x, i) => new {i, x.Id, col = datatable.Columns.IndexOf(x.Name)}).Where(x => x.col != -1).ToList();
            var lstModel = new List<System.Tuple<ProductModel, int, VariantModel, List<CategoryModel>>>();
            var lstVarPropValModel = new List<System.Tuple<VariantPropValModel, PropValModel>>(); 
            var settings = SettingsBiz.GetSettings();

            if (properties.Any(x => datatable.Columns.IndexOf(x.Name) != -1))
                properties.Action((x, i) => colProdMap.Add(string.Format("Properties[{0}].Value", i), x.Name));

            var categories = CategoryMgr.GetAll(this, 0).Action(x =>
                {
                    var lst = x.GetRaw().GetHierOrderList();
                    x.Languages.Action(z => z.Alias = lst.ToString(c => c.GetName(z.LangCode), ">"));
                });
            var manufacturers = Product.DAL.ManufacturerMgr.GetAll(this, 0);
            var statuses = StatusMgr.GetAll(this, 0);
            var vats = VatMgr.GetAll(this)
                .Select(x => new { x.Id, Value = (x.Value.Value * 100).ToString(), RawVal = x.Value }).ToList();

            var tranRows = datatable.Rows.Cast<DataRow>().Select(row => new
            {
                Row = row,
                SId = idColIndex == -1 ? "0" : row["Id"].ToString(),
                VariantSku = varSkuColIndex == -1 ? "" : row[varSkuColIndex].ToString(),
                ProdSku = proSkuColIndex == -1 ? "" : row[proSkuColIndex].ToString(),
                ProdAlias = proAliasColIndex == -1 ? "" : row[proAliasColIndex].ToString(),
                ProdName = proNameColIndex == -1 ? "" : row[proNameColIndex].ToString(),
            }).ToList();

            //if(pageSize == int.MaxValue) Product.DAL.ProductMgr.Preload(this);

            for (int i = 0; i < tranRows.Count; i++)
            {
                var row = tranRows[i].Row;
                var sId = tranRows[i].SId;
                var variantSku = tranRows[i].VariantSku;
                var prodSku = tranRows[i].ProdSku;
                var prodAlias = tranRows[i].ProdAlias;
                var prodName = tranRows[i].ProdName;
                var variantModel = null as VariantModel;
                var variProps = varPropColIndies.Select(x => new {x.i, x.col, x.Id, val = row[x.col].ToString()}).ToList();

                if (string.IsNullOrWhiteSpace(sId)) row["Id"] = sId = "0";

                var id = sId.TryParseInt() ?? -1;
                var model = lstModel.FirstOrDefault(x => (id > 0 && x.Item1.Id == id) ||
                    (id == 0 && (x.Item1.SKU.EqualsIgCase(prodSku, true) || x.Item1.Alias.EqualsIgCase(prodAlias, true) ||
                        (prodAlias.IsNullOrEmpty() && x.Item1.Name.EqualsIgCase(prodName, true)))))
                    .SafeProp(x => x.Item1);
                var modelAdded = model != null;

                model = model ?? ProductMgr.Get(this, id);

                if (idColIndex == -1) model = model ?? Product.DAL.ProductMgr.Get(this, prodSku, prodAlias, variantSku);

                var raw = model == null ? null : model.GetRaw();
                if (model == null && id != 0)
                {
                    errors.Add(string.Format("Invalid Id = {1} at row {0}", i + 2, sId));
                    continue;
                }
                else model = model ?? new ProductModel();

                #region Variant
                if (!variantSku.IsNullOrEmpty() || variProps.Count != 0)
                {
                    if (raw != null)
                        variantModel = raw.Variants
                            .Where(x => !x.Deleted && (x.SKU.EqualsIgCase(variantSku, true) || (x.VariantPropVals.Count != 0 && x.VariantPropVals.All(
                                v => variProps.Any(z => z.Id == v.PropVal.Property.Id && z.val.EqualsIgCase(v.PropVal.Name, true))))))
                            .Select(Mapper.Map<VariantBase, VariantModel>)
                            .FirstOrDefault();

                    if (variantModel == null &&
                        (!variantSku.IsNullOrEmpty() || variProps.Any(x => !x.val.IsNullOrEmpty())))
                        variantModel = lstModel
                            .FirstOrDefault(x => x.Item3 != null && x.Item1 == model && (x.Item3.SKU.EqualsIgCase(variantSku, true) || 
                                (x.Item3.Properties.Count != 0 &&
                                x.Item3.Properties.All(v => variProps.Any(z => z.Id == v.PropId && z.val.EqualsIgCase(v.PropValName, true))))))
                            .SafeProp(x => x.Item3) ?? new VariantModel() { SKU = variantSku, ProductId = model.Id };
                } 
                #endregion

                if (variantModel == null) { if (modelAdded) model = new ProductModel(); }
                else if ((model.Id > 0) ^ (id > 0))
                {
                    errors.Add(string.Format("Invalid Product [Id = {1}, SKU = {2}, Alias = {3}, Name = {4}] at row {0}", i + 2, sId, prodSku, prodAlias, prodName));
                    continue;
                }

                model.WUnit = model.WUnit ?? (byte) settings.Display.Others.MUnit;
                model.ShippingType = model.ShippingType ?? (int)ShippingType.RequireShipping;
                model.SalePrice = model.SalePrice.IsNull() ? 0 : model.SalePrice;
                model.SalePriceInc = model.SalePriceInc.IsNull() ? 0 : model.SalePriceInc;

                ModelState.Clear();

                var exportModel = new ProductExportModel(model, variantModel == null ? null : new ProductExportModel.VariantModel(variantModel), categories, statuses, manufacturers, modelAdded);
                var ret = UpdateModel(exportModel, row, colProdMap.Keys.ToList(), colProdMap, null, (field, value) =>
                {
                    #region Visible
                    if (value.IsNullOrEmpty())
                    {
                        switch (field)
                        {
                            case "VisibleWeb":
                                return "true";
                            case "VisiblePos":
                                return "false";
                            default:
                                return value;
                        }
                    } 
                    #endregion

                    #region Transform
                    if (field.InList("VisibleWeb", "VisiblePos"))
                    {
                        if (value.EqualsIgCase("yes")) return "true";
                        if (value.EqualsIgCase("no")) return "false";
                        if (!value.InList("true", "false", "yes", "no"))
                            ModelState.AddModelError(field, string.Format(StringTable.ErrMsgInvalidFieldValue, value));
                    }

                    //if (field.EqualsIgCase("ProductStatusId"))
                    //{
                    //    var status = statuses.Where(x => x.Name.EqualsIgCase(value) || x.Code.EqualsIgCase(value))
                    //        .Select(x => x.Id.ToString())
                    //        .FirstOrDefault();
                    //    if (status == null)
                    //        ModelState.AddModelError(field, string.Format(StringTable.ErrMsgInvalidFieldValue, value));
                    //    return status;
                    //}

                    if (field.EqualsIgCase("ManufacturerId"))
                    {
                        var manu = manufacturers.Where(x => x.Languages.Any(z => z.Name.EqualsIgCase(value) || z.Alias.EqualsIgCase(value)) || x.Alias.EqualsIgCase(value))
                            .Select(x => x.Id.ToString())
                            .FirstOrDefault();
                        if (manu == null)
                            ModelState.AddModelError(field, string.Format(StringTable.ErrMsgInvalidFieldValue, value));
                        return manu;
                    }

                    if (field.EqualsIgCase("VatId"))
                    {
                        var vat = vats.Where(x => x.Value == value)
                            .Select(x => x.Id.ToString())
                            .FirstOrDefault();
                        if (vat == null)
                            ModelState.AddModelError(field, string.Format(StringTable.ErrMsgInvalidFieldValue, value));
                        return vat;
                    }

                    if (field.EqualsIgCase("ImageExpList"))
                    {
                        var images = value.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                            .Select(x => x.Trim()).ToList();
                        var errImgs = images.Where(x => imageList.All(w => !w.RawFileName.EqualsIgCase(x))).ToList();
                        if (errImgs.Count != 0)
                            ModelState.AddModelError(field, string.Format(StringTable.ErrMsgInvalidImageValue, errImgs.FirstOrDefault()));
                        return images.Distinct(x => x.ToLower()).ToString(";");
                    }

                    //if (field.EqualsIgCase("ActiveLang.Alias") && !value.IsNullOrEmpty())
                    //{
                    //    value = value.Trim().ToLower().Replace(" ", "-");
                    //}

                    #endregion

                    return value;
                });

                #region ModelState.RemoveKeys

                if (SettingsBiz.GetSettings().Display.Others.PriceFocusInc)
                {
                    ModelState.RemoveKey(GetFieldName<ProductModel, OMoney>(m => m.Price));
                    ModelState.RemoveKey(GetFieldName<ProductModel, OMoney>(m => m.SalePrice));
                    ModelState.RemoveKey(GetFieldName<ProductModel, OMoney>(m => m.CostPrice));
                }
                else
                {
                    ModelState.RemoveKey(GetFieldName<ProductModel, OMoney>(m => m.PriceInc));
                    ModelState.RemoveKey(GetFieldName<ProductModel, OMoney>(m => m.SalePriceInc));
                    ModelState.RemoveKey(GetFieldName<ProductModel, OMoney>(m => m.CostPriceInc));
                }

                ModelState.RemoveKey(GetFieldName<ProductModel, ProductLangModel>(m => m.ActiveLang));

                if (!model.SelfDiscActive)
                {
                    ModelState.RemoveKey(GetFieldName<ProductModel, int?>(m => m.SelfDiscMinQty));
                    ModelState.RemoveKey(GetFieldName<ProductModel, object>(m => m.SelfDiscValue));
                }

                if (!model.PriceCkActive)
                    ModelState.RemoveKey(GetFieldName<ProductModel, OMoney>(m => m.PriceCkMinVal));

                if (!model.PriceCkActive || !model.PriceCkVariant)
                    ModelState.RemoveKey(GetFieldName<ProductModel, string>(m => m.PriceCkUrl));

                ModelState.RemoveKey(GetFieldName<ProductModel, OFloat>(m => m.BundleDiscountRate));

                #endregion

                if (ModelState.IsValid)
                {
                    #region Init Model

                    #region FakeNoUpdate
                    model.SelfDiscMinQty = 2;
                    model.SelfDiscValue = 1;
                    model.PriceCkMinVal = 1;
                    model.PriceCkUrl = "http://pricecheck.com";
                    #endregion

                    if (model.TechSpecCostExtra.IsNull()) model.TechSpecCostExtra = null;
                    if (model.DepositAmount.IsNull()) model.DepositAmount = null;
                    if (model.BundleDiscountRate.IsNull()) model.BundleDiscountRate = null;

                    if (model.Vat != null && colProdMap.Keys.Any(x => x.EqualsIgCase("Vat")))
                    {
                        var vat = vats.FirstOrDefault(x => Math.Abs(x.RawVal - (model.Vat.Value ?? 0)) < 0.0001f);
                        model.VatId = vat == null ? (int?)null : vat.Id;
                    }

                    #region Images
                    if (model.ImageExpList.IsNullOrEmpty()) model.Images = null;
                    else
                    {
                        model.InitImages();
                        model.Images.AddRange(model.ImageExpList.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                            .Select(x =>
                            {
                                var img = imageList.FirstOrDefault(w => w.RawFileName.EqualsIgCase(x));
                                return img == null ? null : new ImageModel()
                                {
                                    Thumb = img.FilePath,
                                    FsUrl = img.FilePath,
                                    FileName = img.FileName,
                                    Languages = new List<ImageLangModel>()
                                };
                            }).Where(x => x != null));
                    } 
                    #endregion

                    #region Price

                    foreach (var key in colProdMap.Keys)
                    {
                        if (key.EqualsIgCase("Price")) exportModel.PriceInc = exportModel.IncVat(exportModel.Price);
                        if (key.EqualsIgCase("SalePrice")) exportModel.SalePriceInc = exportModel.IncVat(exportModel.SalePrice);
                        if (key.EqualsIgCase("CostPrice")) exportModel.CostPriceInc = exportModel.IncVat(exportModel.CostPrice);

                        if (key.EqualsIgCase("PriceInc")) exportModel.Price = exportModel.ExcVat(exportModel.PriceInc);
                        if (key.EqualsIgCase("SalePriceInc")) exportModel.SalePrice = exportModel.ExcVat(exportModel.SalePriceInc);
                        if (key.EqualsIgCase("CostPriceInc")) exportModel.CostPrice = exportModel.ExcVat(exportModel.CostPriceInc);
                    }

                    if (model.Price.IsNull()) model.Price = null;
                    if (model.PriceInc.IsNull()) model.PriceInc = null;
                    if (model.SalePrice.IsNull()) model.SalePrice = null;
                    if (model.SalePriceInc.IsNull()) model.SalePriceInc = null;
                    if (model.CostPrice.IsNull()) model.CostPrice = null;
                    if (model.CostPriceInc.IsNull()) model.CostPriceInc = null;
                    if (model.Weight.IsNull()) model.Weight = null;

                    #endregion

                    #region Variant

                    if (variantModel != null)
                    {
                        #region FakeNoUpdate
                        variantModel.PriceCkMinVal = 1;
                        variantModel.PriceCkUrl = "http://pricecheck.com";
                        #endregion

                        #region Images

                        if (variantModel.ImageExpList.IsNullOrEmpty()) variantModel.Images = null;
                        else
                        {
                            variantModel.InitImages();
                            variantModel.Images.AddRange(variantModel.ImageExpList.Split(new[] {';'},
                                StringSplitOptions.RemoveEmptyEntries)
                                .Select(x =>
                                {
                                    var img = imageList.FirstOrDefault(w => w.RawFileName.EqualsIgCase(x));
                                    return img == null ? null : new VariantImageModel()
                                        {
                                            Thumb = img.FilePath,
                                            FsUrl = img.FilePath,
                                            FileName = img.FileName,
                                            Languages = new List<VariantImageLangModel>()
                                        };
                                }).Where(x => x != null));
                        }

                        #endregion

                        #region PropVal

                        variantModel.Properties = new List<VariantPropValModel>();

                        if (exportModel.Properties != null)
                            foreach (var itm in exportModel.Properties
                                .Select((x, idx) => new { Prop = properties[idx], PropVals = properties[idx].GetPropVals(), x.Value })
                                .Where(x => !x.Value.IsNullOrEmpty()))
                            {
                                var pVal = itm.PropVals.FirstOrDefault(
                                    x => x.Languages.Any(z => z.Name.EqualsIgCase(itm.Value))) ?? new PropValModel()
                                    {
                                        PropertyId = itm.Prop.Id,
                                        Code = itm.Value.GenerateAlias(),
                                        Languages = new List<PropValLangModel>()
                                        {
                                            new PropValLangModel(){ LangCode = LanguageMgr.FocusLangCode, Name = itm.Value }
                                        }
                                    };

                                var varPropVal = new VariantPropValModel()
                                {
                                    VariantId = variantModel.Id,
                                    PropId = pVal.PropertyId,
                                    PropValId = pVal.Id,
                                    PropValName = pVal.Name,
                                };

                                lstVarPropValModel.Add(new System.Tuple<VariantPropValModel, PropValModel>(varPropVal, pVal));
                                variantModel.Properties.Add(varPropVal);
                            }
                        else variantModel.Properties = null;

                        #endregion

                        if (model.Id == 0)
                        {
                            if (model.Price.IsNull()) model.Price = (decimal?)variantModel.Price;
                            if (model.PriceInc.IsNull()) model.PriceInc = (decimal?)variantModel.PriceInc;
                            if (model.Weight.IsNull()) model.Weight = (double?)variantModel.Weight;
                        }

                        if (variantModel.Price.IsNull()) variantModel.Price = null;
                        if (variantModel.PriceInc.IsNull()) variantModel.PriceInc = null;
                        if (variantModel.SalePrice.IsNull()) variantModel.SalePrice = null;
                        if (variantModel.SalePriceInc.IsNull()) variantModel.SalePriceInc = null;
                        if (variantModel.CostPrice.IsNull()) variantModel.CostPrice = null;
                        if (variantModel.CostPriceInc.IsNull()) variantModel.CostPriceInc = null;
                        if (variantModel.Weight.IsNull()) variantModel.Weight = null;

                        if (variantModel.Id == 0) variantModel.Active = model.VisibleWeb;

                    }

                    #endregion

                    #endregion

                    if (TryValidate(model, true) && (variantModel == null || TryValidate(variantModel, true)))
                        lstModel.Add(new System.Tuple<ProductModel, int, VariantModel, List<CategoryModel>>(
                            model, i + 1, variantModel, exportModel.Categories));
                }

                if (!ModelState.IsValid)
                {
                    ModelState.RemoveKey(GetFieldName<ProductModel, ProductLangModel>(m => m.ActiveLang));

                    errors.AddRange(ModelState.ModLocalize()
                        .Select(x => new {x.Key, Err = x.Value.Errors.FirstOrDefault()})
                        .Where(x => x.Err != null)
                        .Select(x => string.Format("At row {0}, Id = {1} [{2}] [{3}]", i + 2, id,
                            colProdMap.ContainsKey(x.Key) ? colProdMap[x.Key] : x.Key, x.Err.ErrorMessage)));
                }
            }

            var itemsUpdated = 0;
            var prodLst = lstModel.GroupBy(x => x.Item1, (key, lst) =>
            {
                var lst2 = lst.ToList();
                return new { Item1 = key, lst2[0].Item2, lst2[0].Item4, Item3 = lst2.Select(w => w.Item3).Where(w => w != null).ToList() };
            }).ToList();
            var aliasDic = new Dictionary<string, object>();
            var skuDic = new Dictionary<string, object>();

            foreach (var itm in prodLst.ToArray())
            {
                if (!itm.Item1.Alias.IsNullOrEmpty())
                {
                    var key = itm.Item1.Alias.ToLower();
                    if (aliasDic.ContainsKey(key)) {
                        if (skipDuplicatedProd) prodLst.Remove(itm);
                        else errors.Add(string.Format("At row {0}, Id = {1} [Alias={2}] [{3}]",
                            itm.Item2, itm.Item1.Id, itm.Item1.Alias, StringTable.ErrMsgUniqueConstraint.ModErrLocalize()));
                    }
                    else aliasDic.Add(key, itm);
                }

                if (!itm.Item1.SKU.IsNullOrEmpty())
                {
                    var key = itm.Item1.SKU.ToLower();
                    if (skuDic.ContainsKey(key)) {
                        if (skipDuplicatedProd) prodLst.Remove(itm);
                        else errors.Add(string.Format("At row {0}, Id = {1} [SKU={2}] [{3}]",
                            itm.Item2, itm.Item1.Id, itm.Item1.SKU, StringTable.ErrMsgUniqueConstraint.ModErrLocalize()));
                    }
                    else skuDic.Add(key, itm);
                }
            }

            int prodCount = 0, variantCount = 0;
            if (!validate || errors.Count == 0)
            {
                ITransaction trans = null;
                try
                {
                    #region SaveorUpdate

                    var session = this.GetSession();
                    trans = validate ? session.BeginTransaction() : null;

                    session.SetBatchSize(100);

                    #region PropVals

                    var dbPValDic = new Dictionary<string, PropValModel>();
                    foreach (var tuple in lstVarPropValModel.Where(x => x.Item1.PropValId == 0))
                    {
                        var key = string.Format("{0}:{1}", tuple.Item2.PropertyId, tuple.Item2.Name).ToLower();
                        PropValModel oVal;
                        if (dbPValDic.TryGetValue(key, out oVal)) tuple.Item2.Id = oVal.Id;
                        else 
                            if (Product.DAL.PropValMgr.SaveorUpdate(this, tuple.Item2, false)) dbPValDic.Add(key, tuple.Item2);
                            else errors.Add(string.Format(StringTable.ErrMsgImportUpdateFailed, null, null));

                        tuple.Item1.PropValId = tuple.Item2.Id;
                    }

                    #endregion

                    var cateLst = new List<CategoryModel>();
                  
                    foreach (var itm in (pageSize == int.MaxValue ? prodLst : prodLst.GetPage(pageIndex, pageSize)))
                    {
                        try 
                        {
                            var model = itm.Item1;
                            var raw = model.GetRaw();
                            var proCates = raw.SafeProp(x => x.CategoryProducts);
                            var oldProdQty = raw == null || raw.Id == 0 ? model.Quantity : raw.Quantity;
                            var newProdQty = model.Quantity;
                            
                            itemsUpdated++;

                            model.Variants = null;
                            model.Bundlings = null;
                            model.CategoryIds = null;
                            model.Accessories = null;

                            #region Images

                            if (model.Images != null)
                                foreach (var img in model.Images)
                                {
                                    if (img.FsUrl.StartsWith(tmpPath))
                                        img.FileName = MoveFile(ModuleUrl.ImageFolder, ModuleUrl.TempFolder, img.FsUrl, null, false);
                                }

                            #endregion

                            #region Category

                            if (itm.Item4.SafeCount() != 0)
                            {
                                CategoryModel parCate = null;
                                foreach (var cateModel in itm.Item4)
                                {
                                    var cate = cateModel;
                                    if (cate.Id == 0 &&
                                        !cateLst.Where(x => x.Languages.Any(z => z.Alias.EqualsIgCase(cate.Alias)))
                                            .Action(x => cate.Id = x.Id)
                                            .Any())
                                    {
                                        cate.Languages.Action(x => x.Alias = null);
                                        cate.ParentId = parCate == null ? (int?) null : parCate.Id;
                                        if (CategoryMgr.SaveorUpdate(this, cate, false))
                                        {
                                            var cate2 = CategoryMgr.Get(this, cate.Id);
                                            var hierLst = cate2.GetRaw().GetHierOrderList();
                                            cate.Languages.Action(x => x.Alias = hierLst.ToString(z => z.GetName(x.LangCode), ">"));
                                            cateLst.Add(cate2);
                                        }
                                        else
                                            errors.Add(string.Format(StringTable.ErrMsgImportUpdateFailed, itm.Item2, model.Id));
                                    }
                                    parCate = cate;
                                }
                                if (itm.Item4.Last().Id != 0)
                                    model.CategoryIds = (proCates ?? new Domain.CategoryProduct[0])
                                        .Select(x => x.Category.Id)
                                        .Concat(new[] {itm.Item4.Last().Id})
                                        .Distinct().ToString(",");
                            }

                            #endregion

                            #region Product

                            if (raw != null && raw.Id != 0) model.Quantity = raw.Quantity;

                            if (model.Id == 0 && itm.Item3.SafeCount() != 0)
                            {
                                if (model.Price.IsNull()) model.Price = itm.Item3.Max(x => x.Price);
                                if (model.PriceInc.IsNull()) model.PriceInc = itm.Item3.Max(x => x.PriceInc);
                                if (model.SalePrice.IsNull()) model.SalePrice = itm.Item3.Where(x => x.SalePrice > 0).Max(x => x.SalePrice);
                                if (model.SalePriceInc.IsNull()) model.SalePriceInc = itm.Item3.Where(x => x.SalePriceInc > 0).Max(x => x.SalePriceInc);
                                if (model.CostPrice.IsNull()) model.CostPrice = itm.Item3.Where(x => x.CostPrice > 0).Max(x => x.CostPrice);
                                if (model.CostPriceInc.IsNull()) model.CostPriceInc = itm.Item3.Where(x => x.CostPriceInc > 0).Max(x => x.CostPriceInc);

                                if (model.Weight.IsNull()) model.Weight = itm.Item3.Where(x => x.Weight > 0d).Max(x => x.Weight);
                            }

                            if (settings.Hidden.Price4LoginOnly && settings.Hidden.Price4LoginForcedAllProd)
                            {
                                model.Price4LoginOnly = true;
                            }
    
                            if (Product.DAL.ProductMgr.SaveorUpdate(this, model, false, Request, false, true))
                            {
                                prodCount++;

                                #region Variants

                                foreach (var varModel in itm.Item3)
                                {
                                    var varRaw = varModel.GetRaw();
                                    var oldVariQty = varRaw == null || varRaw.Id == 0 ? 0/*varModel.Quantity*/ : varRaw.Quantity;
                                    var newVariQty = varModel.Quantity;

                                    varModel.ProductId = model.Id;
                                    varModel.Languages = null;

                                    if (varModel.Images != null)
                                    {
                                        foreach (var img in varModel.Images)
                                        {
                                            if (img.FsUrl.StartsWith(tmpPath))
                                                img.FileName = MoveFile(ModuleUrl.ImageFolder, ModuleUrl.TempFolder,
                                                    img.FsUrl, null, false);
                                        }
                                        if (!settings.Hidden.VariantMultiImage)
                                            varModel.Image = varModel.Images.FirstOrDefault().SafeProp(x => x.FileName);
                                    }

                                    if (varRaw != null && varRaw.Id != 0) varModel.Quantity = varRaw.Quantity;
                                    else varModel.Quantity = 0;

                                    if (Product.DAL.VariantMgr.SaveorUpdate(this, varModel, false)) variantCount++;
                                    else errors.Add(string.Format(StringTable.ErrMsgImportUpdateFailed, itm.Item2,
                                            model.Id));

                                    raw = Product.DAL.ProductMgr.GetDom(this, model.Id);
                                    varRaw = Product.DAL.VariantMgr.GetDom(this, varModel.Id);

                                    if (raw != null && varRaw != null && varRaw.Id != 0 /* && newVariQty != oldVariQty*/)
                                    {
                                        Product.DAL.ProductMgr.UpdateQuantity(this, raw, varRaw, newVariQty - oldVariQty, false, false, true, true,
                                            new ProdQtyLogModel() { Reason = (int)ProdQtyLogReason.Imported });
                                        session.Update(varRaw);
                                    }
                                }

                                #endregion

                                if (raw != null && raw.Id != 0 && raw.Variants.SafeCount() == 0 && newProdQty != oldProdQty)
                                    Product.DAL.ProductMgr.UpdateQuantity(this, raw, null, newProdQty - oldProdQty, false, false, true, false,
                                        new ProdQtyLogModel() { Reason = (int)ProdQtyLogReason.Imported });

                                if (raw != null && raw.Id != 0)
                                {
                                    if (raw.Variants.SafeCount() != 0 && !settings.Hidden.ProductVariMainPriceDisabled)
                                    {
                                        var tmpLst = raw.Variants.Where(m => !m.Deleted && m.Active && m.Price > 0).ToList();
                                        raw.Price = tmpLst.Count == 0 ? raw.Price : tmpLst.Min(m => m.Price ?? 0);
                                    }

                                    session.Update(raw);
                                } 
                            }
                            else errors.Add(string.Format(StringTable.ErrMsgImportUpdateFailed, itm.Item2, model.Id));

                            #endregion

                            if (!validate) session.Flush();
                        }
                        catch (Exception err) { errors.Add(err.Message); if (!validate) Session.Clear(); }
                    }

                    #region Commit

                    if (errors.Count == 0 || !validate)
                    {
                        session.Flush(); if (trans != null) trans.Commit();
                        if (imageList != null) imageList.Action(x => IOUtility.DeleteFile(x.FilePath));
                    }
                    else { if (trans != null) trans.Rollback(); session.Clear(); } 

                    #endregion

                    #endregion
                }
                catch (Exception err)
                {
                    errors.Add(err.Message);
                    if (trans != null) trans.Rollback();
                    Session.Clear();
                    if (validate) prodCount = variantCount = 0;
                }
            }
            
            return JsonStatus(StringTable.ImportData, errors.Count == 0, errors.Count == 0 
                ? string.Format(StringTable.ImportProductOkMsg, prodCount, variantCount)
                : string.Format(StringTable.ImportProductPartialOkMsg, prodCount, variantCount, datatable.Rows.Count, errors.Take(5).ToString("<br>")),
                m => m.Data = new { pageIndex, pageSize, itemsTotal = prodLst.SafeCount(), itemsUpdated });
        }
        #endregion

        #region ExportToExcel
        [AdminPerm("CatalogBits", PermissionMask.View)]
        public ActionResult ExportExcel(string keyword, string mode, string fields, string ids, string fileName = null)
        {
            int totalRows;
            var properties = PropertyMgr.GetAll(this, true);

            #region Filter

            List<ProductModel> listProduct;
            var arrIds = ids.Split(new[] {",", ";"}, StringSplitOptions.RemoveEmptyEntries).Select(int.Parse).ToList();
            switch ((mode ?? "").ToLower())
            {
                case "category":
                    var listProdTemp = new List<ProductModel>();
                    foreach (var cateId in arrIds)
                    {
                        var listProductCate = Product.DAL.ProductMgr.GetAll(this, LanguageMgr.FocusLangCode, cateId, null, 1,
                            int.MaxValue, out totalRows, SortDirection.Ascending, "Id", (keyword ?? "").Trim());

                        if (listProductCate != null)
                            foreach (var prd in listProductCate)
                                if (!listProdTemp.Exists(t => t.Id == prd.Id)) listProdTemp.Add(prd);
                    }

                    listProduct = listProdTemp;

                    break;
                default:
                    var items = arrIds.SafeCount() != 0
                        ? Product.DAL.ProductMgr.GetList(this, arrIds.ToArray()) 
                        : Product.DAL.ProductMgr.GetAll(this, LanguageMgr.FocusLangCode, null, 
                        null, 1, int.MaxValue, out totalRows, SortDirection.Ascending, "Id", keyword);

                    listProduct = items;
                    break;
            }

            #endregion

            var settings = SettingsBiz.GetSettings();
            var lst = listProduct.Where(x => x.Id > 1).SelectMany(m =>
                {
                    var raw = m.GetRaw();
                    return raw.Variants.Count == 0
                        ? new[] {m.ToExportModel(properties)}
                        : (settings.Hidden.ProductVariMainPriceDisabled ? new[] { m.ToExportModel(properties) } : new ProductExportModel[0]).Concat(raw.GetVariants().Select(v => m.ToExportModel(properties, v)));
                }).ToList();
            var firstPropIdx = -1;
            var param = Expression.Parameter(typeof(ProductExportModel), "x");  
            var fieldLst = new[] {"Id"}.Union(fields.Split(new[] {','}, StringSplitOptions.RemoveEmptyEntries)).ToList();
            var fieldDic = typeof (ProductExportModel).GetProperties()
                .ToDictionary(x => x.Name.ToLower(), x => Expression.Lambda(Expression.Property(param, x), param));

            if (properties.Count != 0)
            {
                foreach (var itm in new[] {"SKU"})
                {
                    var idx = fieldLst.IndexOf(itm);
                    if (idx != -1) fieldLst.Insert(idx + 1, "Variant" + itm);
                }

                var priceIdx = firstPropIdx = fieldLst.Select(
                    (x, i) => (int?)(x.IndexOf("Price", StringComparison.OrdinalIgnoreCase) != -1 ? i : -1))
                    .FirstOrDefault(x => x != -1) ?? fieldLst.Count - 1;

                properties.Action((x, i) =>
                {
                    var exprIdx = typeof(ProductExportModel).ToExpression("Properties", i, param);
                    var propText = string.Format("Properties[{0}].Value", i);

                    fieldDic.Add(propText.ToLower(), Expression.Lambda(Expression.Property(exprIdx, "Value"), param));

                    fieldLst.Insert(priceIdx + i, propText);
                });
            }

            var numFields = typeof (ProductExportModel).GetProperties()
                .Where(x => x.PropertyType == typeof (int) || x.PropertyType == typeof (OFloat) ||
                            x.PropertyType == typeof (OMoney) || x.PropertyType == typeof (OMoneyMc))
                .Select(x => x.Name).ToList();
            HSSFWorkbook workbook = null;
            ICellStyle numStyle = null;

            ExportHelper.ToExcel(lst, ref workbook, Product.StringTable.Product, lst.SafeCount() < 500, fieldLst.ToArray(),
                fieldDic, null, (workbk, sheet, cell) =>
                {
                    if (!fieldLst.Select((x, i) => i == cell.ColumnIndex && numFields.Contains(x)).FirstOrDefault(x => x)) return;
                    if (numStyle == null)
                    {
                        numStyle = workbk.CreateCellStyle();
                        numStyle.BorderLeft = numStyle.BorderRight = numStyle.BorderTop = numStyle.BorderBottom = NPOI.SS.UserModel.BorderStyle.Thin;
                        numStyle.Alignment = HorizontalAlignment.Right;
                    }
                    cell.CellStyle = numStyle;
                    //cell.SetCellType(CellType.Numeric);
                }, 0, 0, (wbook, sheet, topRows, bottomRows) =>
                {
                    if (firstPropIdx != -1)
                    {
                        var header = sheet.GetRow(0).Cells;
                        properties.Action((x, i) => header[firstPropIdx + i].SetCellValue(x.Name));
                    }
                });

            fileName = fileName ?? ("Products_" + DateTime.Now.ToString("ddMMyyyy", CultureInfo.InvariantCulture) + ".xls");

            using (var output = new MemoryStream())
            {
                workbook.Write(output);
                return File(output.ToArray(), "application/vnd.ms-excel", fileName);
            }
        }
        #endregion

        #region ImportTemplate
        [AdminPerm("CatalogBits", PermissionMask.View)]
        public ActionResult ImportTemplate(string fields)
        {
            return ExportExcel("", "product", fields ?? "Id", "0","ImportTemplate.xls");
        } 
        #endregion
    }
}