using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Web;
using System.Web.Mvc;
using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;
using NPOI.SS.Util;

namespace Claw.Modules.Eshop.Helper
{
    public static class ExportHelper
    {

        #region ToExcelR2S

        /// <summary>one row to one sheet</summary>
        public static void ToExcelR2S<TModel>(List<TModel> list, ref HSSFWorkbook workbook, Func<TModel,string> fSheetName, string[] fields,
            Dictionary<string, LambdaExpression> fieldDic = null, Func<PropertyInfo, string> fDispName = null,
            Action<HSSFWorkbook, ISheet, ICell> filter = null, Func<HSSFWorkbook, ISheet, TModel, bool, int, int> fCustom = null, string breakField = null)
        {
            workbook = workbook ?? new HSSFWorkbook();

            var hdrfont = workbook.CreateFont();
            var hdrCss = workbook.CreateCellStyle();
            hdrCss.WrapText = true;
            hdrCss.VerticalAlignment = VerticalAlignment.Center;
            hdrCss.LeftBorderColor = hdrCss.TopBorderColor = hdrCss.RightBorderColor = hdrCss.BottomBorderColor = NPOI.HSSF.Util.HSSFColor.Grey25Percent.Index;
            hdrCss.BorderLeft = hdrCss.BorderRight = hdrCss.BorderTop = hdrCss.BorderBottom = BorderStyle.Thin;
            hdrCss.DataFormat = workbook.CreateDataFormat().GetFormat("Text");
            hdrCss.SetFont(hdrfont);
            if (fieldDic == null)
            {
                var param = Expression.Parameter(typeof(TModel), "x");
                fieldDic = fields.ToDictionary(x => x.ToLower(), x => Expression.Lambda(typeof(TModel).ToExpression(x, param), param));
            }

            if (fDispName == null) fDispName = x => x.GetDisplayName();

            fields = fields.Select(x => x.ToLower()).Where(x => fieldDic.ContainsKey(x)).ToArray();

            var colWidths = new int[fields.Length];
            var dataCss = workbook.CreateCellStyle();
            dataCss.WrapText = true;
            dataCss.VerticalAlignment = VerticalAlignment.Top;
            dataCss.LeftBorderColor = dataCss.TopBorderColor = dataCss.RightBorderColor = dataCss.BottomBorderColor = NPOI.HSSF.Util.HSSFColor.Grey25Percent.Index;
            dataCss.BorderLeft = dataCss.BorderRight = dataCss.BorderTop = dataCss.BorderBottom = BorderStyle.Thin;
            dataCss.DataFormat = workbook.CreateDataFormat().GetFormat("Text");
            var diExpCompiled = new Dictionary<string, Delegate>();
            foreach (var itm in list)
            {
                int rowNumber = 0;
                var sheet = fSheetName == null ? workbook.CreateSheet() : workbook.CreateSheet(fSheetName(itm));
                sheet.DisplayGridlines = false;

                if (fCustom != null) rowNumber += fCustom(workbook, sheet, itm, true, rowNumber);

                var viewData = new ViewDataDictionary<TModel>(itm);

                for (int i = 0; i < fields.Length; i++)
                {
                    var row = sheet.CreateRow(rowNumber++);
                    if (breakField != null && fields[i].StartsWith(breakField, StringComparison.OrdinalIgnoreCase))
                    {
                        sheet.AddMergedRegion(new CellRangeAddress(row.RowNum, row.RowNum, 0, 1));
                        continue;
                    }

                    var exprHdr = fieldDic[fields[i]].Body as MemberExpression;
                    var headerName = exprHdr == null ? fields[i] : exprHdr.GetDisplayName(fDispName);
                    var cellHdr = row.CreateCell(0);
                    cellHdr.SetCellValue(headerName);
                    cellHdr.CellStyle = hdrCss;

                    var exprDat = fieldDic[fields[i]];
                    var cellDat = row.CreateCell(1);
                    row.CreateCell(2).CellStyle = cellDat.CellStyle = dataCss;

                    if (filter != null) { filter(workbook, sheet, cellHdr); filter(workbook, sheet, cellDat); }

                    var bodyExpr = exprDat.Body as MemberExpression;

                    if (bodyExpr != null && bodyExpr.Expression is ParameterExpression)
                    {
                        var metadata = ModelMetadata.FromStringExpression(ExpressionHelper.GetExpressionText(exprDat), viewData);
                        if (cellDat.CellType == CellType.Numeric)
                        {
                            if (metadata.Model != null) cellDat.SetCellValue(metadata.Model.ToExcelNumber());
                            else cellDat.SetCellType(CellType.Blank);
                        }
                        else
                        {
                            var sVal = (metadata.Model ?? "").ToString();
                            cellDat.SetCellValue(sVal);
                            if (!string.IsNullOrWhiteSpace(sVal))
                            {
                                sheet.AddMergedRegion(new CellRangeAddress(cellDat.RowIndex, cellDat.RowIndex,
                                    cellDat.ColumnIndex, cellDat.ColumnIndex + 1));
                                row.Height = (short)(2.2 * row.Height); 
                            }
                        }
                    }
                    else
                    {
                        Delegate oFunc;
                        if (!diExpCompiled.TryGetValue(fields[i], out oFunc)) diExpCompiled.Add(fields[i], oFunc = exprDat.Compile());
                        var cVal = oFunc.DynamicInvoke(itm);
                        if (cellDat.CellType == CellType.Numeric)
                        {
                            if (cVal != null) cellDat.SetCellValue(cVal.ToExcelNumber());
                            else cellDat.SetCellType(CellType.Blank);
                        }
                        else cellDat.SetCellValue((cVal ?? "").ToString());
                    }
                }

                if (fCustom != null) fCustom(workbook, sheet, itm, false, rowNumber);

                for (var i = 0; i < 3; i++)
                {
                    //if (colWidths[i] != default(int)) sheet.SetColumnWidth(i, colWidths[i]);
                    if (i == 2) sheet.SetColumnWidth(i, 2*colWidths[1]);
                    else
                    {
                        var oldWidth = sheet.GetColumnWidth(i);
                        try { sheet.AutoSizeColumn(i); sheet.SetColumnWidth(i, colWidths[i] = sheet.GetColumnWidth(i) + 5*256); }
                        catch (Exception err) { sheet.SetColumnWidth(i, colWidths[i] = oldWidth); }
                    }
                }
            }

            if (list.Count >= 1000) System.Threading.Tasks.Task.Run(() => { System.Threading.Thread.Sleep(5000); GC.Collect(); });
        }
        
        #endregion

        #region ToExcel

        public static byte[] ToExcel<TModel>(List<TModel> list, string[] fields, bool autosizeColumns = false,
            Dictionary<string, LambdaExpression> fieldDic = null, Func<PropertyInfo, string> fDispName = null)
        {
            return ToExcel(list, fields, fieldDic, fDispName, null, 0, 0, null, autosizeColumns);
        }

        public static byte[] ToExcel<TModel>(List<TModel> list, string[] fields,
            Dictionary<string, LambdaExpression> fieldDic = null, Func<PropertyInfo, string> fDispName = null,
            Action<HSSFWorkbook, ISheet, ICell> filter = null, int topRows = 0, int bottomRows = 0,
            Action<HSSFWorkbook, ISheet, IRow[], IRow[]> fCustom = null, bool autosizeColumns = true, string sheetName = null)
        {
            HSSFWorkbook workbook = null;

            ToExcel(list, ref workbook, sheetName, autosizeColumns, fields, fieldDic, fDispName, filter, topRows, bottomRows, fCustom);

            //Write the workbook to a memory stream
            using (var output = new MemoryStream())
            {
                workbook.Write(output);
                return output.ToArray();
            }
        }

        public static void ToExcel<TModel>(List<TModel> list, ref HSSFWorkbook workbook, string sheetName, bool autosizeColumns, string[] fields,
            Dictionary<string, LambdaExpression> fieldDic = null, Func<PropertyInfo, string> fDispName = null,
            Action<HSSFWorkbook, ISheet, ICell> filter = null, int topRows = 0, int bottomRows = 0,
            Action<HSSFWorkbook, ISheet, IRow[], IRow[]> fCustom = null)
        {
            //Create new Excel workbook
            workbook = workbook ?? new HSSFWorkbook();
            int rowNumber = 0;

            //Create new Excel sheet
            var sheet = sheetName.IsNullOrEmpty() ? workbook.CreateSheet() : workbook.CreateSheet(sheetName);

            var topRws = new List<IRow>();

            for (int r = 0; r < topRows; r++)
            {
                var rcs = workbook.CreateCellStyle();
                rcs.SetFont(workbook.CreateFont());
                var row = sheet.CreateRow(rowNumber++);
                for (int i = 0; i < fields.Length; i++)
                {
                    row.CreateCell(i).CellStyle = rcs;
                }
                topRws.Add(row);
            }

            //Create a header row
            var headerRow = sheet.CreateRow(rowNumber++);

            var hdrfont = workbook.CreateFont();
            //hdrfont.FontName = "Calibri";
            //hdrfont.FontHeightInPoints = 10;
            hdrfont.Color = (NPOI.HSSF.Util.HSSFColor.White.Index);
            hdrfont.Boldweight = (short)FontBoldWeight.Bold;

            var hdrCss = workbook.CreateCellStyle();
            hdrCss.Alignment = HorizontalAlignment.CenterSelection;
            hdrCss.FillForegroundColor = NPOI.HSSF.Util.HSSFColor.LightBlue.Index;
            hdrCss.FillPattern = FillPattern.SolidForeground;
            hdrCss.LeftBorderColor = hdrCss.RightBorderColor = hdrCss.BottomBorderColor = hdrCss.TopBorderColor = NPOI.HSSF.Util.HSSFColor.White.Index;
            hdrCss.SetFont(hdrfont);
            hdrCss.BorderLeft = hdrCss.BorderRight = hdrCss.BorderTop = hdrCss.BorderBottom = BorderStyle.Thin;
            if (fieldDic == null)
            {
                var param = Expression.Parameter(typeof(TModel), "x");
                fieldDic = fields.ToDictionary(x => x.ToLower(), x => Expression.Lambda(typeof(TModel).ToExpression(x, param), param));
            }

            if (fDispName == null) fDispName = x => x.GetDisplayName();

            fields = fields.Select(x => x.ToLower()).Where(x => fieldDic.ContainsKey(x)).ToArray();

            //headerRow.CreateCell(1).SetCellValue("Name");
            for (int i = 0; i < fields.Length; i++)
            {
                var expr = fieldDic[fields[i]].Body as MemberExpression;
                var headerName = expr == null ? fields[i] : expr.GetDisplayName(fDispName);
                var cell = headerRow.CreateCell(i);
                cell.SetCellValue(headerName);
                //cell.CellStyle.FillPattern = FillPattern.NoFill;
                cell.CellStyle = hdrCss;
            }

            // Freeze the header row so it is not scrolled
            //sheet.CreateFreezePane(0, rowNumber, 0, rowNumber);

            //Populate the sheet with values from the grid data
            var dataCss = workbook.CreateCellStyle();
            dataCss.BorderLeft = dataCss.BorderRight = dataCss.BorderTop = dataCss.BorderBottom = BorderStyle.Thin;
            dataCss.DataFormat = workbook.CreateDataFormat().GetFormat("Text");
            var diExpCompiled = new Dictionary<string, Delegate>();
            foreach (var itm in list)
            {
                //Create a new row
                var row = sheet.CreateRow(rowNumber++);
                var viewData = new ViewDataDictionary<TModel>(itm);

                //row.CreateCell(1).SetCellValue(wo.Name);
                for (int i = 0; i < fields.Length; i++)
                {
                    var expr = fieldDic[fields[i]];
                    var cell = row.CreateCell(i);
                    cell.CellStyle = dataCss;

                    if (filter != null) filter(workbook, sheet, cell);

                    var bodyExpr = expr.Body as MemberExpression;

                    if (bodyExpr != null && bodyExpr.Expression is ParameterExpression)
                    {
                        var metadata = ModelMetadata.FromStringExpression(ExpressionHelper.GetExpressionText(expr), viewData);
                        if (cell.CellType == CellType.Numeric)
                        {
                            if (metadata.Model != null) cell.SetCellValue(metadata.Model.ToExcelNumber());
                            else cell.SetCellType(CellType.Blank);
                        }
                        else cell.SetCellValue((metadata.Model ?? "").ToString());
                    }
                    else
                    {
                        Delegate oFunc;
                        if (!diExpCompiled.TryGetValue(fields[i], out oFunc)) diExpCompiled.Add(fields[i], oFunc = expr.Compile());
                        var cVal = oFunc.DynamicInvoke(itm);
                        if (cell.CellType == CellType.Numeric)
                        {
                            if (cVal != null) cell.SetCellValue(cVal.ToExcelNumber());
                            else cell.SetCellType(CellType.Blank);
                        }
                        else cell.SetCellValue((cVal ?? "").ToString());
                    }
                }
            }

            // Hide the gridlines
            sheet.DisplayGridlines = false;
            var bottomRws = new List<IRow>();

            for (int r = 0; r < bottomRows; r++)
            {
                var rcs = workbook.CreateCellStyle();
                rcs.SetFont(workbook.CreateFont());
                rcs.FillForegroundColor = NPOI.HSSF.Util.HSSFColor.LightYellow.Index;
                rcs.FillPattern = FillPattern.SolidForeground;
                rcs.BorderLeft = rcs.BorderRight = rcs.BorderTop = rcs.BorderBottom = BorderStyle.Thin;
                var row = sheet.CreateRow(rowNumber++);
                for (int i = 0; i < fields.Length; i++)
                {
                    row.CreateCell(i).CellStyle = rcs;
                }
                bottomRws.Add(row);
            }

            if (fCustom != null) fCustom(workbook, sheet, topRws.ToArray(), bottomRws.ToArray());

            // set default style and auto size for columns
            var sharefont = workbook.CreateFont();
            //sharefont.FontName = "Calibri";
            //sharefont.FontHeightInPoints = 12;
            if (autosizeColumns)
                for (var i = 0; i < fields.Length; i++)
                {
                    var oldWidth = sheet.GetColumnWidth(i);
                    var shareCss = workbook.CreateCellStyle();
                    shareCss.DataFormat = workbook.CreateDataFormat().GetFormat("Text");
                    shareCss.SetFont(sharefont);
                    try
                    {
                        sheet.SetDefaultColumnStyle((short) i, shareCss);
                        sheet.AutoSizeColumn(i);
                        sheet.SetColumnWidth(i, sheet.GetColumnWidth(i) + 256); 
                    }
                    catch (Exception err)
                    {
                        sheet.SetColumnWidth(i, oldWidth);
                    }
                }

            if (list.Count >= 1000)
            {
                System.Threading.Tasks.Task.Run(() =>
                {
                    System.Threading.Thread.Sleep(5000);
                    GC.Collect();
                });
            }
        }

        #endregion

        #region ToExpression

        public static Expression ToExpression(this Type type, string expresion, Expression param = null)
        {
            if (type == null | expresion.IsNullOrEmpty()) return null;

            var prop = type.GetProperty(expresion);

            param = param ?? Expression.Parameter(type, "x");

            if (prop != null) return Expression.Property(param, prop);

            if (expresion.IndexOf('.') == -1) throw new InvalidExpressionException(expresion);

            var arr = expresion.Split('.');
            var ret = param;
            var ptype = type;

            for (int i = 0; i < arr.Length; i++)
            {
                prop = ptype.GetProperty(arr[i]);
                if (prop == null) throw new InvalidExpressionException(expresion);
                ptype = prop.PropertyType;
                ret = Expression.Property(ret, prop);
            }

            return ret;
        }

        public static Expression ToExpression(this Type type, string collectionProperty, int index,
            Expression param = null)
        {
            if (type == null | collectionProperty.IsNullOrEmpty()) return null;

            var collProp = type.GetProperty(collectionProperty);
            var itemProp = collProp.PropertyType.GetProperty("Item");

            param = param ?? Expression.Parameter(type, "x");

            return Expression.MakeIndex(Expression.Property(param, collProp), itemProp, new[] { Expression.Constant(index) });
        }

        #endregion

        #region GetDisplayName
        public static string GetDisplayName(this MemberExpression expression, Func<PropertyInfo, string> fDispName)
        {
            if (expression == null) return null;

            if (expression.Expression is ParameterExpression || expression.Expression is IndexExpression)
                return fDispName((PropertyInfo)expression.Member);

            return ((MemberExpression)expression.Expression).GetDisplayName(fDispName);
        }
        #endregion

        #region ToExcelNumber
        /// <summary>resolve -0</summary>
        public static double ToExcelNumber(this object value)
        {
            if (value == null) return 0;

            if (value.GetType() == typeof(OFloat)) return ((OFloat)value).Value.ToExcelNumber();
            if (value.GetType() == typeof(OMoney)) return ((OMoney)value).Value.ToExcelNumber();
            if (value.GetType() == typeof(OMoneyMc)) return ((OMoneyMc)value).Value.ToExcelNumber();

            var numVal = Math.Round(value.SafeChangeType<double>(), 2);
            return numVal.Equals(0.00) ? 0 : numVal;
        }
        #endregion
    }
}