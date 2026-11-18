using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Drawing.Imaging;
using System.Text;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Data.Odbc;
using System.Diagnostics;
using System.Drawing;
using System.Dynamic;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Linq.Expressions;
using System.Text.RegularExpressions;
using System.Net;
using System.Web;
using System.Web.Caching;
using System.Web.Routing;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.Mvc;
using System.Linq;
using AutoMapper.Internal;
using Claw.Controls.Modules;
using Claw.Core;
using Claw.Core.DAL;
using Claw.Core.Domain;
using Claw.Core.Extensions;
using Claw.Core.Logs;
using Claw.Core.Utility;
using Claw.Modules.Eshop.Controllers.Admin;
using Claw.Modules.Eshop.DAL;
using Claw.Modules.Eshop.Helper;
using Claw.Modules.Eshop.Models;
using Kendo.Mvc;
using Kendo.Mvc.UI;
using Kendo.Mvc.Extensions;
using Claw.Core.Interfaces;
using Claw.Core.Enumerations;
using NHibernate.Linq;
using NHibernate.Util;
using Quartz;
using WebSupergoo.ABCpdf9;
using Generic = System.Collections.Generic;
using ObjectModel = System.Collections.ObjectModel;
using Module = Claw.Core.Domain.Module;
using Spire.Barcode;
using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;
using System.Data;
using System.Collections.Specialized;
using Encoder = System.Drawing.Imaging.Encoder;
using System.Net.Mail;
using Ninject;
using Microsoft.Web.Infrastructure.DynamicModuleHelper;
using Eshop.Payments.BasePayment;

[assembly: PreApplicationStartMethod(typeof(Claw.Modules.Eshop.Controllers.Admin.EagerLoadHttpModule), "Start")]
namespace Claw.Modules.Eshop.Controllers.Admin
{
    public delegate DataSourceResult GridEagerLoadDataHandler(int page, int pageSize, string sortField, SortDirection sortDir);

    #region CInfo
    public class CInfo
    {
        public string Name, SName, Controller;
        public bool Term;
    }
    #endregion

    #region ExpandoJsonConverter
    public class ExpandoJsonConverter : JavaScriptConverter
    {
        public override object Deserialize(Generic.IDictionary<string, object> dictionary, Type type,
            JavaScriptSerializer serializer)
        {
            throw new NotImplementedException();
        }

        public override Generic.IDictionary<string, object> Serialize(object obj, JavaScriptSerializer serializer)
        {
            var result = new Dictionary<string, object>();
            var dictionary = obj as Generic.IDictionary<string, object>;

            foreach (var item in dictionary)
                result.Add(item.Key, item.Value);

            return result;
        }

        public override IEnumerable<Type> SupportedTypes
        {
            get { return new ObjectModel.ReadOnlyCollection<Type>(new Type[] { typeof(ExpandoObject) }); }
        }
    }
    #endregion

    #region BaseController

    public class BaseController : BaseModuleController, IDB
    {
        #region Names

        private static CInfo[] _names = null;

        public static CInfo[] Names
        {
            get
            {
                _names = _names ?? new[]
                {
                    new CInfo() {Name = "Categories", SName = "Category", Controller = "Category"},
                    new CInfo() {Name = "Products", SName = "Product", Controller = "Product"}, 
                    new CInfo() {Name = "Featured products", SName = "Featured product", Controller = "FeaturedProduct"}, 
                    new CInfo() {Name = "Back-ordered products", SName = "Back-ordered product", Controller = "BackorderProduct"},
                    new CInfo() {Name = "States", SName = "Status", Controller = "Status"},    
                    new CInfo() {Name = "Discounts", SName = "Discount", Controller = "Discount"},
                    new CInfo() {Name = "Direct discounts", SName = "Direct discount", Controller = "DirDiscount"},
                    new CInfo() {Name = "Coupons", SName = "Coupon", Controller = "Coupon"},
                    new CInfo() {Name = "Variants", SName = "Variant", Controller = "Property"},
                    new CInfo() {Name = "Variant values", SName = "Variant value", Controller = "PropVal"},
                    new CInfo() {Name = "ImportExport", SName = "ImportExport", Controller = "ImportExport"},
                    new CInfo() {Name = "Manufacturer", SName = "Manufacturer", Controller = "Manufacturer"},
                    new CInfo() {Name = "Vat", SName = "vat", Controller = "vat"},
                    new CInfo() {Name = "Orders", SName = "Order", Controller = "Order"},
                    new CInfo() {Name = "Order notes", SName = "Order note", Controller = "OrderNote"},
                    new CInfo() {Name = "Shippings", SName = "Shipping", Controller = "Shipping"},
                    new CInfo() {Name = "Shipping zones", SName = "Shipping zone", Controller = "ShippingZone"},
                    new CInfo() {Name = "Email templates", SName = "Email template", Controller = "EmailTempl"},
                    new CInfo() {Name = "PaymentMethod", SName = "PaymentMethod", Controller = "PaymentMethod"},
                    new CInfo() {Name = "Customer groups", SName = "Customer group", Controller = "CustGroup"},
                    new CInfo() {Name = "Customers", SName = "Customer", Controller = "Customer"},
                    new CInfo() {Name = "Activities", SName = "Activity", Controller = "ActionLog"},
                    new CInfo() {Name = "Traces", SName = "Trace", Controller = "Trace"},
                    new CInfo() {Name = "Currencies", SName = "Currency", Controller = "Currency"},
                    new CInfo() {Name = "ReportProductSales", SName = "ReportProductSales", Controller = "ReportProd"},
                    new CInfo() {Name = "ReportSalesByMonth", SName = "ReportSalesByMonth", Controller = "ReportSales"},
                };

                var ret = _names.ToList();

                ret.Last().Term = true;

                return ret.ToArray();
            }
        }

        #endregion

        #region Fields

        private static readonly object SyncObj = new object();
        private static volatile bool _eagerLoadingDone = false;

        protected Color BackgroundColor = Color.Transparent;
        protected int ThumbWidth = 50;
        protected int ThumbHeight = 50;
        protected int ImageWidth = 100;
        protected int ImageHeight = 100;
        protected int ImageQuality = 90; //0 to 100 

        #endregion

        #region BaseController

        public BaseController(IDataService dataService)
            : base(dataService)
        {
        }

        static BaseController()
        {
            StartJob<TaskJob>(1);
        }

        #endregion

        #region Initialize

        public static void Initialize(IDB db)
        {
            bool loadingDone;

            lock (SyncObj)
            {
                if (!(loadingDone = _eagerLoadingDone)) _eagerLoadingDone = true;
            }

            if (!loadingDone)
            {
                _eagerLoadingDone = true;

                var site = db.DataService.GetCurrentSite();

                if (site == null)
                {
                    if (Debugger.IsAttached) Debugger.Break();
                    return;
                }

                //Task.Run(() => StringLocalizeExtensions.DoLoadFull(site.Id));

                var module = db.DataService.GetModuleByName(ModuleMgr.Name, site.Id);

                CompileViews(module, site);

                DelayTasks(new List<string>() { ModuleUrl.FrontUrl("EagerLoad", "Job", null) });
            }

        }

        #endregion

        #region SiteId

        public int SiteId
        {
            get { return CurrentSite.Id; }
        }

        #endregion

        #region Controller

        public string Controller
        {
            get { return (string)HttpContext.Request.RequestContext.RouteData.Values["controller"]; }
        }

        #endregion

        #region CurrentUser
        /// <summary>
        /// never null
        /// </summary>
        protected new User CurrentUser
        {
            get { return base.CurrentUser ?? new User(); }
        }
        #endregion

        #region IndexAction
        protected bool IndexAction
        {
            get
            {
                var routeInfo = Request != null && Request.UrlReferrer != null ? new RouteInfo(Request.UrlReferrer) : null;
                return routeInfo != null && "index".EqualsIgCase(routeInfo["action"]);
            }
        }
        #endregion

        #region RouteInfo

        protected RouteInfo RouteInfo
        {
            get { return new RouteInfo(Request.Url); }
        }

        #endregion

        #region AcceptJson
        protected bool AcceptJson
        {
            get
            {
                return Request.Headers["X-Requested-With"].EqualsIgCase("XMLHttpRequest") ||
                       Request.AcceptTypes.Any(x => x.IndexOf("application/json", StringComparison.InvariantCultureIgnoreCase) != -1);
            }
        }
        #endregion

        #region DataService
        IDataService IDB.DataService
        {
            get { return DataService; }
        }
        #endregion

        #region Caching

        string GetCacheKey(string key)
        {
            return ModuleMgr.Name + "::" + Controller + "::" + key;
        }

        protected void SetCache<T>(string key, T obj, int seconds = 15*60,
            CacheItemPriority priority = CacheItemPriority.Default)
        {
            ClawCache.Insert(GetCacheKey(key), obj, seconds, priority);
        }

        protected T GetCache<T>(string key) where T : class
        {
            return ClawCache.Get<T>(GetCacheKey(key));
        }

        protected void ClearCache(string key)
        {
            ClawCache.Remove(GetCacheKey(key));
        }

        #endregion

        #region TempData

        protected void SetTempData<T>(string key, T obj)
        {
            TempData[GetCacheKey(key)] = obj;
        }

        protected T GetTempData<T>(string key, bool keep = true) where T : class
        {
            object oVal;

            key = GetCacheKey(key);

            if (TempData.TryGetValue(key, out oVal))
            {
                if (keep) TempData.Keep(key);
                return (T)oVal;
            }
            else return default(T);
        }

        protected void ClearTempData(string key)
        {
            TempData.Remove(GetCacheKey(key));
        }

        #endregion

        #region ErrModelState
        public object ErrModelState
        {
            get
            {
                return ModelState.Where(x => x.Value.Errors.Count > 0).ToArray();
            }
        }
        #endregion

        #region TrimReq

        protected void TrimReq(DataSourceRequest request)
        {
            request.Page = (request.Page < 1) ? 1 : request.Page;
            request.PageSize = (request.PageSize < 1) ? 1 : request.PageSize;
        }

        #endregion

        #region GetSettings
        public static SettingsBiz GetSettings()
        {
            var ds = (ClawContext.Current == null ? null : ClawContext.Current.DataService);
            if (ds == null)
            {
                if (Debugger.IsAttached) Debugger.Break();
                return new SettingsBiz();
            }
            else return SettingsBiz.GetSettings();
        }

        protected SettingsBiz.SizeBiz GetSettings(Func<SettingsBiz, SettingsBiz.SizeBiz> expr)
        {
            var biz = GetSettings();
            return biz == null ? null : expr(biz);
        }

        #endregion

        #region InitImageSettings

        protected bool InitImageSettings(Func<SettingsBiz, SettingsBiz.SizeBiz> exprImg,
            Func<SettingsBiz, SettingsBiz.SizeBiz> exprThumb, bool setImgQuality = true)
        {
            var biz = GetSettings();
            if (biz == null) return false;

            if (setImgQuality) ImageQuality = biz.Display.Admin.Image.Quality;
            BackgroundColor = ColorTranslator.FromHtml(biz.Display.Admin.Image.BackgroundColor);

            var settingsImg = exprImg(biz);
            if (settingsImg != null)
            {
                ImageWidth = settingsImg.Width;
                ImageHeight = settingsImg.Height;
            }

            if (exprThumb != null)
            {
                var settingsThumb = exprThumb(biz);
                if (settingsThumb != null)
                {
                    ThumbWidth = settingsThumb.Width;
                    ThumbHeight = settingsThumb.Height;
                }
            }

            return true;
        }

        #endregion

        #region CalcAutoWidth
        public int CalcAutoWidth(int height, int defVal = 0)
        {
            return ThumbHeight == 0 ? 0 : (ThumbWidth * height) / ThumbHeight;
        }
        #endregion

        #region GetParam

        protected T GetParam<T>(string name)
        {
            var ret = GetParam(typeof(T), name);
            if (ret == null) return default(T);
            else return (T)ret;
        }

        protected object GetParam(Type type, string name)
        {
            var val = Request.Params[name];
            if (string.IsNullOrEmpty(val)) return null;

            if (type.IsNullableType())
                return Convert.ChangeType(val, type.GetTypeOfNullable());

            return Convert.ChangeType(val, type);
        }

        #endregion

        #region GetPatternParam

        protected T GetPatternParam<T>(string prefix, string suffix)
        {
            var ret = GetPatternParam(typeof(T), prefix, suffix);
            if (ret == null) return default(T);
            else return (T)ret;
        }

        protected object GetPatternParam(Type type, string prefix, string suffix)
        {
            foreach (var key in Request.Params.AllKeys)
            {
                if (key.StartsWith(prefix) && key.EndsWith(suffix))
                {
                    var val = Request.Params[key];
                    if (string.IsNullOrEmpty(val)) return null;

                    if (type.IsNullableType()) return Convert.ChangeType(val, type.GetTypeOfNullable());
                    else return Convert.ChangeType(val, type);
                }
            }

            return null;
        }

        #endregion

        #region GetFilter

        protected TVal GetFilter<TModel, TVal>(DataSourceRequest request, Expression<Func<TModel, TVal>> expression)
        {
            if (request == null || request.Filters == null || request.Filters.Count == 0) goto LB_DEF;

            var member = ((MemberExpression)expression.Body).Member.Name;

            var filter = (FilterDescriptor)request.Filters.FirstOrDefault(x => ((FilterDescriptor)x).Member == member);

            if (filter == null || filter.Value == null) goto LB_DEF;

            var val = filter.Value;
            if (val is TVal) return (TVal)val;
            else
            {
                Type type = typeof(TVal);
                if (val.GetType().IsPrimitive)
                {
                    if (type.IsPrimitive) return (TVal)Convert.ChangeType(val, type);
                    else if (null != (type = Nullable.GetUnderlyingType(type)))
                        return (TVal)Convert.ChangeType(val, type);
                }
            }

            return (TVal)filter.Value;

        LB_DEF:
            return default(TVal);
        }

        #endregion

        #region GetSortField

        protected bool GetSortField(DataSourceRequest request, ref string fieldName, ref SortDirection sortDir)
        {
            if (request == null || request.Sorts == null || request.Sorts.Count == 0) return false;
            var sort = (SortDescriptor)request.Sorts[0];

            fieldName = sort.Member;
            sortDir = (sort.SortDirection == ListSortDirection.Ascending
                ? SortDirection.Ascending
                : SortDirection.Descending);

            return true;
        }

        #endregion

        #region GetKeyword

        protected string GetKeyword()
        {
            var kword = Request.Params["keyword"];
            if (kword != null) return kword.Trim();
            else return string.Empty;
        }

        #endregion

        #region CheckRegex

        protected static bool CheckRegex<TModel>(ModelStateDictionary state,
            Expression<Func<TModel, string>> field, TModel model)
        {
            Exception error;
            if (IsValidRegex(field.Compile()(model), out error)) return true;
            else
            {
                state.AddModelError(GetFieldName(field), StringTable.ErrRegex);
                return false;
            }
        }

        #endregion

        #region GetFieldName

        protected static string GetFieldName<TModel, TVal>(Expression<Func<TModel, TVal>> expression)
        {
            var ret = ExpressionHelper.GetExpressionText(expression);
            if (string.IsNullOrEmpty(ret) && Debugger.IsAttached) Debugger.Break();
            return ret;
        }

        #endregion

        #region GetMaxFieldLength
        protected static int GetMaxFieldLength<TModel>(Expression<Func<TModel, string>> expression)
        {
            var linqExpr = (MemberExpression)expression.Body;

            var attr = linqExpr.Member.GetCustomAttributes(typeof(StringLengthAttribute), false)
                .Cast<StringLengthAttribute>()
                .FirstOrDefault();

            return attr == null ? int.MaxValue : attr.MaximumLength;
        }
        #endregion

        #region GetMinFieldLength
        protected static int GetMinFieldLength<TModel>(Expression<Func<TModel, string>> expression)
        {
            var linqExpr = (MemberExpression)expression.Body;

            var attr = linqExpr.Member.GetCustomAttributes(typeof(StringLengthAttribute), false)
                .Cast<StringLengthAttribute>()
                .FirstOrDefault();

            return attr == null ? 1 : attr.MinimumLength;
        }
        #endregion

        #region IsValidRegex

        protected static bool IsValidRegex(string value, out Exception error)
        {
            try
            {
                Regex.IsMatch("", value);
                error = null;
                return true;
            }
            catch (Exception err)
            {
                error = err;
                return false;
            }
        }

        #endregion

        #region CheckExt

        protected static bool CheckExt<TModel>(ModelStateDictionary state,
            Expression<Func<TModel, string>> field, TModel model)
        {
            if (Regex.IsMatch(field.Compile()(model), @"^\.[a-z][a-z0-9]{0,9}$")) return true;
            else
            {
                state.AddModelError(GetFieldName(field), StringTable.ErrFileExt);
                return false;
            }
        }

        #endregion

        #region GetFiles
        protected string[] GetFiles(string path)
        {
            var keyCache = "basecontroller_get_files_" + path.TrimStart('~').TrimEnd('/').Replace('/', '_').ToLower();
            int nTries = 3;
        LB_LOOP:
            try
            {
                var lst = ClawCache.Get<string[]>(keyCache);
                if (lst == null)
                {
                    lst = Directory.GetFiles(Globals.MapPath(path))
                        .Select(x => Path.GetFileName(x).ToLower())
                        .OrderBy(x => x).ToArray();

                    ClawCache.Insert(keyCache, lst, 5 * 60);
                }
                return lst;
            }
            catch (Exception er)
            {
                LogErr(er);
                if (--nTries >= 0)
                {
                    Thread.Sleep(1000); goto LB_LOOP;
                }
                else return new string[0];
            }
        }
        #endregion

        #region TryValidate

        protected bool TryValidate<TModel, TVal>(TModel obj, Expression<Func<TModel, TVal>> member = null)
        {
            List<ValidationResult> results;
            return TryValidate(obj, member, out results);
        }

        protected bool TryValidate<TModel, TVal>(TModel obj, Expression<Func<TModel, TVal>> member,
            out List<ValidationResult> results, bool validAll = false)
        {
            var context = new ValidationContext(obj, serviceProvider: null, items: null);
            results = new List<ValidationResult>();

            if (member != null)
            {
                context.MemberName = GetFieldName(member);
                if (!Validator.TryValidateProperty(member.Compile()(obj), context, results)) return false;
                if (validAll) return Validator.TryValidateObject(obj, context, results);
                return true;
            }
            return Validator.TryValidateObject(obj, context, results);
        }

        protected bool TryValidate<TModel>(TModel obj, bool recursive = false, string prefix = "")
        {
            var ret = TryValidateModel(obj, prefix);

            foreach (var prop in obj.GetType().GetProperties())
            {
                if (recursive && prop.PropertyType.IsListType())
                {
                    int i = 0; var lst = (IEnumerable)prop.GetValue(obj);
                    if (lst != null)
                        foreach (var item in lst)
                        {
                            if (!TryValidate(item, true, string.Format("{0}[{1}]", prop.Name, i++))) ret = false;
                        }
                }
            }

            return ret;
        }

        #endregion

        #region TryCreatePath

        protected bool TryCreatePath(string path)
        {
            var mapPath = Globals.MapPath(path);
            if (Directory.Exists(mapPath)) return false;
            else
            {
                Directory.CreateDirectory(mapPath);
                return true;
            }
        }

        #endregion

        #region TryDeleteFile
        protected bool TryDeleteFile(string filePath)
        {
            if (System.IO.File.Exists(filePath))
            {
                System.IO.File.Delete(filePath);
                return true;
            }
            else return false;
        }
        #endregion

        #region TryDeleteDir
        protected bool TryDeleteDir(string dirPath, bool recursive = false)
        {
            if (System.IO.Directory.Exists(dirPath))
            {
                System.IO.Directory.Delete(dirPath, recursive);
                return true;
            }
            else return false;
        }
        #endregion

        #region TryMoveFile
        public static bool TryMoveFile(string srcFile, ref string dstFile, bool skipInject = false, bool delSrcFile = true)
        {
            if (!System.IO.File.Exists(srcFile)) return false;

            var tmp = dstFile;
            int iseq = 1;

            while (!skipInject && System.IO.File.Exists(tmp))
            {
                tmp = InjectFilePath(dstFile, iseq++);
            }

            IOUtility.CopyFile(srcFile, dstFile = tmp, true);
            if (delSrcFile) IOUtility.DeleteFile(srcFile);

            return true;
        }
        #endregion

        #region TryCloneFile
        public static bool TryCloneFile(string srcFile, out string resultFile, bool trimPath = true)
        {
            resultFile = null; srcFile = Globals.MapPath(srcFile);
            if (!System.IO.File.Exists(srcFile)) return false;

            var tmp = srcFile;
            int iseq = 1;

            while (System.IO.File.Exists(tmp))
            {
                tmp = InjectFilePath(srcFile, iseq++);
            }

            IOUtility.CopyFile(srcFile, resultFile = tmp, true);

            if (trimPath) resultFile = Path.GetFileName(resultFile);

            return true;
        }
        #endregion

        #region TryCloneImage

        protected bool TryCloneImage(string srcFile, out string resultFile, Func<string, string> fThumb,
            string srcThumb = null)
        {
            if (!TryCloneFile(srcFile, out resultFile, false)) return false;

            string tmpThumbDir = ModuleUrl.TempThumbFolder;

            TryCreatePath(tmpThumbDir);
            if (string.IsNullOrEmpty(srcThumb))
            {
                string tmpThumbFile = Globals.MapPath(fThumb(Path.GetFileName(resultFile)));
                CreateImageThumb(resultFile, resultFile, ref tmpThumbFile, ThumbWidth, ThumbHeight, BackgroundColor, ImageQuality);
            }
            else if (!TryCloneFile(srcThumb, out resultFile, false)) return false;

            resultFile = Path.GetFileName(resultFile);

            return true;
        }

        #endregion

        #region InjectFilePath

        public static string InjectFilePath(string filePath, int iseq)
        {
            return string.Format("{0}{1}{2}({3}){4}", Path.GetDirectoryName(filePath), '\\',
                Path.GetFileNameWithoutExtension(filePath), iseq, Path.GetExtension(filePath));
        }

        #endregion

        #region SaveFile

        protected void SaveFile(HttpPostedFileBase file, ref string filePath)
        {
            var tmp = filePath;
            int iseq = 1;

            while (System.IO.File.Exists(tmp))
            {
                tmp = InjectFilePath(filePath, iseq++);
            }

            file.SaveAs(filePath = tmp);
        }

        protected void SaveFile(System.Drawing.Image image, ref string filePath)
        {
            var tmp = filePath;
            int iseq = 1;

            while (System.IO.File.Exists(tmp))
            {
                tmp = InjectFilePath(filePath, iseq++);
            }

            image.Save(filePath = tmp, ImageFormat.Png);
        }

        protected void SaveFile(HttpWebResponse wResp, ref string filePath)
        {
            var tmp = filePath;
            int iseq = 1;

            while (System.IO.File.Exists(tmp))
            {
                tmp = InjectFilePath(filePath, iseq++);
            }

            using (var rs = wResp.GetResponseStream())
            {
                using (var sw = new FileStream(filePath = tmp, FileMode.Create))
                {
                    var buf = new byte[4 * 1024];
                    while (true)
                    {
                        int n = rs.Read(buf, 0, buf.Length);
                        if (n > 0) sw.Write(buf, 0, n);
                        else break;
                    }
                }
            }
        }

        #endregion

        #region ExtractZip

        protected bool ExtractZip(HttpPostedFileBase file, out string dirPath)
        {
            string tmpModDir = ModuleUrl.TempFolder, log = null;
            string tmpFile = Globals.MapPath(tmpModDir.CombineWPath(file.FileName));

            SaveFile(file, ref tmpFile);

            var tmpDir = dirPath = Globals.MapPath(tmpModDir.CombineWPath(Path.GetFileNameWithoutExtension(file.FileName) + ".dir"));
            int iseq = 1;

            while (System.IO.Directory.Exists(tmpDir))
            {
                tmpDir = InjectFilePath(dirPath, iseq++);
            }

            var ret = ZipHelper.ExtractZipFile(tmpFile, dirPath = tmpDir, ref log);

            TryDeleteFile(tmpFile);

            return ret;
        }

        #endregion

        #region ResizeImage

        protected void ResizeImage(string srcFilePath, ref string dstFilePath, int width, int height, Color background,
            int quality = 90)
        {
            Bitmap bmp = FixResizeImage(srcFilePath, width, height, background, quality);

            var tmp = dstFilePath;
            int iseq = 1;

            while (System.IO.File.Exists(tmp))
            {
                tmp = InjectFilePath(dstFilePath, iseq++);
            }

            bmp.Save(dstFilePath = tmp, quality);
            bmp.Dispose();
        }

        #endregion

        #region FixResizeImage

        protected static Bitmap FixResizeImage(string sourceFile, int width, int height, Color bgColor, int quality = 80)
        {
            if (!string.IsNullOrEmpty(sourceFile))
            {
                var tmpPath = string.Format(@"{0}\{1}{2}", Path.GetDirectoryName(sourceFile), Guid.NewGuid(), Path.GetFileName(sourceFile));
                if (FixResizeImage(sourceFile, tmpPath, width, height, bgColor, quality))
                {
                    using (var image = System.Drawing.Image.FromFile(tmpPath))
                    {
                        var copy = CopyHelper.DeepCopy<System.Drawing.Image>(image) as Bitmap;

                        image.Dispose();

                        try
                        {
                            System.IO.File.Delete(tmpPath);
                        }
                        catch (Exception exp)
                        {
                            LogHelper.Log4net(exp, "CreateThumbnail");
                        }

                        return copy;
                    }
                }
            }

            return null;
        }

        protected static bool FixResizeImage(string sourceFile, string targetFile, int width, int height, Color bgColor, int quality = 80)
        {
            try
            {
                using (var srcImg = System.Drawing.Image.FromFile(sourceFile))
                {
                    if (width < 1 || height < 1 || srcImg.Width < 1 || srcImg.Height < 1) return false;

                    var srcSz = new SizeF(srcImg.Width, srcImg.Height);
                    var ext = Path.GetExtension(targetFile);
                    var codec = ImageTools.GetCodecByExt(ext);

                    var eps = new EncoderParameters(1);
                    eps.Param[0] = new EncoderParameter(Encoder.Quality, quality);

                    #region Same Size
                    if ((int)srcSz.Width == width && (int)srcSz.Height == height)
                    {
                        var fileInfo = new FileInfo(sourceFile);
                        var size = fileInfo.Length;

                        if (size / 1024 > 100) srcImg.Save(targetFile, codec, eps);
                        else srcImg.Save(targetFile);

                        srcImg.Dispose();

                        if (Claw.Core.Configurations.ClawConfiguration.GetConfig().ImageOptimize)
                        {
                            var imageOptimizer = new Claw.Core.Utility.Optimize.ImageOptimizer(targetFile);
                            return imageOptimizer.Optimize();
                        }

                        return true;
                    }
                    #endregion

                    if (srcSz.Width < width || srcSz.Height < height)
                    {
                        var minSz = new SizeF(Math.Min(srcSz.Width, width), Math.Min(srcSz.Height, height));
                        var wSz = new SizeF(minSz.Width, (minSz.Width * height) / width);
                        var hSz = new SizeF((minSz.Height * width) / height, minSz.Height);
                        if (wSz.Width * wSz.Height >= hSz.Width * hSz.Height)
                        {
                            width = (int)wSz.Width; height = (int)Math.Round(Math.Min(wSz.Height, (minSz.Height * wSz.Width) / srcSz.Width));
                        }
                        else
                        {
                            width = (int)Math.Round(Math.Min(hSz.Width, (minSz.Width * hSz.Height) / srcSz.Height)); height = (int)hSz.Height;
                        }
                    }

                    var p3Alpha = srcImg.PixelFormat == PixelFormat.Format16bppArgb1555 ||
                                srcImg.PixelFormat == PixelFormat.Format32bppPArgb ||
                                srcImg.PixelFormat == PixelFormat.Format64bppPArgb ||
                                srcImg.PixelFormat == PixelFormat.Format32bppArgb ||
                                srcImg.PixelFormat == PixelFormat.Format64bppArgb;
                    var bgTransOrWhite = bgColor.ToArgb() == Color.Transparent.ToArgb() || bgColor.ToArgb() == Color.White.ToArgb();
                    var brushColor = p3Alpha ? (bgTransOrWhite ? Color.Transparent : bgColor) : (bgTransOrWhite ? Color.White : bgColor);
                    var dstSz = height * srcSz.Width >= width * srcSz.Height
                        ? new SizeF(width, (width * (srcSz.Height / srcSz.Width)))
                        : new SizeF(height * (srcSz.Width / srcSz.Height), height);

                    if (dstSz.Width > srcSz.Width || dstSz.Height > srcSz.Height) dstSz = srcSz;

                    if (brushColor.ToArgb() == Color.Transparent.ToArgb() && !srcImg.ContainsTransparent())
                        brushColor = bgTransOrWhite ? Color.White : bgColor;

                    width = (int)Math.Round(Math.Min(dstSz.Width, width));
                    height = (int)Math.Round(Math.Min(dstSz.Height, height));

                    using (var destImage = new System.Drawing.Bitmap(width, height))
                    {
                        using (var graphic = Graphics.FromImage(destImage))
                        {
                            graphic.FillRectangle(new SolidBrush(brushColor), new Rectangle(0, 0, width, height));
                            graphic.DrawImage(srcImg, 0.5f * (width - dstSz.Width), 0.5f * (height - dstSz.Height), dstSz.Width, dstSz.Height);

                            destImage.Save(targetFile, codec, eps);

                            graphic.Dispose();
                            destImage.Dispose();
                            srcImg.Dispose();

                            if (Claw.Core.Configurations.ClawConfiguration.GetConfig().ImageOptimize)
                            {
                                var imageOptimizer = new Claw.Core.Utility.Optimize.ImageOptimizer(targetFile);
                                return imageOptimizer.Optimize();
                            }
                        }
                    }
                }
            }
            catch (Exception exp)
            {
                LogHelper.Log4net(exp, exp.Message);
                return false;
            }

            return true;
        }

        #endregion

        #region CreateImageThumb

        protected void CreateImageThumb(string originFile, string srcFilePath, ref string dstFilePath, int width, int height,
            Color background, int quality = 90)
        {
            //Bitmap bmpThumb = ImageTools.CreateThumbnail(srcFilePath, width, height, background);

            var tmp = dstFilePath;
            int iseq = 1;

            //while (System.IO.File.Exists(tmp))
            //{
            //    tmp = InjectFilePath(dstFilePath, iseq++);
            //}

            //bmpThumb.Save(dstFilePath = tmp, quality);
            //bmpThumb.Dispose();
            CreateThumbnail(originFile, dstFilePath = tmp, width, height, background, quality);
        }

        #endregion

        #region CreateThumbnail
        public static bool CreateThumbnail(string sourceFile, string targetFile, int width, int height,
            Color bgColor, int quality = 80)
        {
            try
            {
                using (var srcImg = System.Drawing.Image.FromFile(sourceFile))
                {
                    if (width < 1 || height < 1 || srcImg.Width < 1 || srcImg.Height < 1) return false;

                    var p3Alpha = srcImg.PixelFormat == PixelFormat.Format16bppArgb1555 ||
                                  srcImg.PixelFormat == PixelFormat.Format32bppPArgb ||
                                  srcImg.PixelFormat == PixelFormat.Format64bppPArgb ||
                                  srcImg.PixelFormat == PixelFormat.Format32bppArgb ||
                                  srcImg.PixelFormat == PixelFormat.Format64bppArgb ||
                                  (srcImg.Palette != null && srcImg.Palette.Entries != null &&
                                  srcImg.Palette.Entries.Any(x => x.A == 0));
                    var bgTransOrWhite = bgColor.ToArgb() == Color.Transparent.ToArgb() || bgColor.ToArgb() == Color.White.ToArgb();
                    var brushColor = p3Alpha ? (bgTransOrWhite ? Color.Transparent : bgColor) : (bgTransOrWhite ? Color.White : bgColor);
                    var s = new SizeF(srcImg.Width, srcImg.Height);
                    var d = height * s.Width >= width * s.Height
                        ? new SizeF(width, (width * (s.Height / s.Width)))
                        : new SizeF(height * (s.Width / s.Height), height);

                    if (d.Width > s.Width || d.Height > s.Height) d = s;

                    var isTransparent = false;
                    if (brushColor.ToArgb() == Color.Transparent.ToArgb() && !srcImg.ContainsTransparent())
                        brushColor = bgTransOrWhite ? Color.White : bgColor;
                    else
                        isTransparent = true;

                    using (var dstImg = new System.Drawing.Bitmap(width, height))
                    {
                        using (var g = Graphics.FromImage(dstImg))
                        {
                            g.FillRectangle(new SolidBrush(brushColor), 0, 0, dstImg.Width, dstImg.Height);
                            g.DrawImage(srcImg, 0.5f * (width - d.Width), 0.5f * (height - d.Height), d.Width, d.Height);

                            var ext = Path.GetExtension(targetFile);
                            if (isTransparent && ext.ToLower() == ".gif") {
                                ext = ".png";
                            }

                            var codec = ImageTools.GetCodecByExt(ext);
                            var eps = new EncoderParameters(1);
                            eps.Param[0] = new EncoderParameter(Encoder.Quality, quality);

                            dstImg.Save(targetFile, codec, eps);

                            g.Dispose(); dstImg.Dispose(); srcImg.Dispose();

                            if (Claw.Core.Configurations.ClawConfiguration.GetConfig().ImageOptimize)
                            {
                                var imageOptimizer = new Claw.Core.Utility.Optimize.ImageOptimizer(targetFile);
                                return imageOptimizer.Optimize();
                            }
                        }
                    }
                }
            }
            catch (Exception exp)
            {
                return false;
            }

            return true;
        }
        #endregion

        #region NormalizeFileName

        protected string NormalizeFileName(string file, object id)
        {
            var name = Path.GetFileNameWithoutExtension(file) ?? "no_name";

            if (name.Length > 50) name = name.Substring(0, 50);
            var ret = Globals.GenerateAlias(name) + Path.GetExtension(file);

            if (id != null) ret = string.Format("{0}_{1}", id, ret);

            return ret;
        }

        #endregion

        #region JsonUpload
        protected ActionResult JsonUpload(object data, bool normal = false)
        {
            if (normal) return Json(data);

            HttpContext cxt;
            if (ClawContext.Current != null && null != (cxt = ClawContext.Current.Context))
            {
                if (cxt.Request != null && cxt.Request.AcceptTypes != null &&
                    cxt.Request.AcceptTypes.Any(x => x.EqualsIgCase("application/json")))
                {
                    return Json(data, "text/html");
                }
            }

            return Json(data, "text/plain");
        }
        #endregion

        #region DoUploadImage

        protected bool DoUploadImage(object id, HttpPostedFileBase image, out string imagePath)
        {
            string thumbPath;
            if (DoUploadImage(id, image, out imagePath, out thumbPath, true))
            {
                imagePath = thumbPath; return true;
            }
            else return false;
        }

        protected bool DoUploadImage(object id, HttpPostedFileBase image, out string imagePath, out string thumbPath, bool createThumb)
        {
            imagePath = thumbPath = null;

            if (!ImageTools.IsImageExtension(Path.GetExtension(image.FileName))) return false;

            string tmpModDir = ModuleUrl.TempFolder;
            string tmpThumbDir = ModuleUrl.TempThumbFolder;
            string fileName = NormalizeFileName(Path.GetFileName(image.FileName), id);

            TryCreatePath(tmpModDir);

            string tempFilePath = Globals.MapPath(tmpModDir.CombineWPath("temp_" + fileName));
            string tmpImageFile = Globals.MapPath(ModuleUrl.TempImagePath(fileName));

            SaveFile(image, ref tempFilePath);

            int width = ImageWidth, height = ImageHeight;
            if (!GetSettings().Display.Admin.Image.AutoResize)
            {
                using (var img = System.Drawing.Image.FromFile(tempFilePath))
                {
                    width = img.Width; height = img.Height;
                }
            }
            ResizeImage(tempFilePath, ref tmpImageFile, width, height, BackgroundColor, ImageQuality);

            imagePath = Globals.ResolveClientUrl(tmpModDir.CombineWPath(Path.GetFileName(tmpImageFile)));

            if (createThumb)
            {
                TryCreatePath(tmpThumbDir);
                string tmpThumbFile = Globals.MapPath(ModuleUrl.TempThumbPath(Path.GetFileName(tmpImageFile)));
                CreateImageThumb(tempFilePath, tmpImageFile, ref tmpThumbFile, ThumbWidth, ThumbHeight, BackgroundColor, ImageQuality);

                thumbPath = Globals.ResolveClientUrl(tmpThumbDir.CombineWPath(Path.GetFileName(tmpThumbFile)));
            }

            var tmpOriginalFile = Globals.MapPath(ModuleUrl.TempOriginalPath(Path.GetFileName(tmpImageFile)));
            TryCreatePath(ModuleUrl.TempOriginalFolder);
            TryMoveFile(tempFilePath, ref tmpOriginalFile, true);//TryDeleteFile(tempFilePath);

            return true;
        }

        #endregion

        #region DoUploadImageZip

        #region ImgUL

        protected class ImgUL
        {
            public string RawFileName { get; set; }
            public string FilePath;
            public string ThumbUrl;
            public int Size;

            public string FileName { get { return Path.GetFileName(FilePath); } }
            public string FileExt { get { return Path.GetExtension(FilePath).TrimStart('.'); } }
            public string MimeType { get { return "image/" + FileExt; } }
        }

        #endregion

        protected bool DoUploadImageZip(object id, HttpPostedFileBase fileZip, bool createThumb, out List<ImgUL> images)
        {
            images = new List<ImgUL>();

            if (!Path.GetExtension(fileZip.FileName).EqualsIgCase(".zip")) return false;

            string tmpModDir = ModuleUrl.TempFolder;
            string tmpThumbDir = ModuleUrl.TempThumbFolder;
            string tmpZipDir;

            if (!ExtractZip(fileZip, out tmpZipDir)) return false;

            TryCreatePath(tmpModDir);

            foreach (var tempFilePath in Directory.GetFiles(tmpZipDir, "*", SearchOption.AllDirectories))
            {
                if (ImageTools.IsImageExtension(Path.GetExtension(tempFilePath)))
                {
                    try
                    {
                        var rawFileName = Path.GetFileName(tempFilePath);
                        string fileName = NormalizeFileName(rawFileName, id);
                        string tmpImageFile = Globals.MapPath(ModuleUrl.TempImagePath(fileName));

                        ResizeImage(tempFilePath, ref tmpImageFile, ImageWidth, ImageHeight, BackgroundColor,
                            ImageQuality);

                        var img = new ImgUL()
                        {
                            RawFileName = rawFileName,
                            FilePath = Globals.ResolveClientUrl(tmpModDir.CombineWPath(Path.GetFileName(tmpImageFile)))
                        };

                        if (createThumb)
                        {
                            TryCreatePath(tmpThumbDir);
                            string tmpThumbFile = Globals.MapPath(ModuleUrl.TempThumbPath(Path.GetFileName(tmpImageFile)));
                            CreateImageThumb(tempFilePath, tmpImageFile, ref tmpThumbFile, ThumbWidth, ThumbHeight,
                                BackgroundColor, ImageQuality);
                            img.ThumbUrl = Globals.ResolveClientUrl(tmpThumbDir.CombineWPath(Path.GetFileName(tmpThumbFile)));
                        }

                        images.Add(img);

                        var tmpOriginalFile = Globals.MapPath(ModuleUrl.TempOriginalPath(Path.GetFileName(tmpImageFile)));
                        TryCreatePath(ModuleUrl.TempOriginalFolder);
                        TryMoveFile(tempFilePath, ref tmpOriginalFile, true);
                    }
                    catch (Exception er) { LogErr(er); }
                }
                else TryDeleteFile(tempFilePath);
            }

            TryDeleteDir(tmpZipDir, true);

            return true;
        }

        #endregion

        #region UploadImage

        [HttpPost]
        public ActionResult UploadImage(long id, HttpPostedFileBase imageUpload, bool enableZip = false, bool jsonUpload = false)
        {
            if (imageUpload == null)
                return jsonUpload
                    ? JsonUpload(new { Status = ActionResultStatus.Fail, Message = string.Format(StringTable.TemplActionUnsuccess, StringTable.UploadImage.ToLower()) })
                    : JsonStatus(StringTable.UploadImage, false);

            Func<object, ActionResult> fRet = (data) => jsonUpload ? JsonUpload(data) : Json(data);
            var images = new List<ImgUL>();
            try
            {
                bool retCode;
                if (enableZip && Path.GetExtension(imageUpload.FileName).EqualsIgCase(".zip"))
                    retCode = DoUploadImageZip(id, imageUpload, true, out images);
                else
                {
                    string imagePath, thumbPath;
                    retCode = DoUploadImage(id, imageUpload, out imagePath, out thumbPath, true);
                    if (retCode) images.Add(new ImgUL()
                    {
                        FilePath = imagePath,
                        ThumbUrl = thumbPath,
                        Size = imageUpload.ContentLength
                    });
                }

                if (retCode)
                {
                    if (enableZip) return fRet(new { Status = ActionResultStatus.Success, Message = string.Empty, Images = images });
                    else return fRet(new { Status = ActionResultStatus.Success, Message = string.Empty, Image = images.FirstOrDefault() });
                }
                else return fRet(new { Status = ActionResultStatus.Fail, Message = StringTable.UploadImageErrExt });
            }
            catch (Exception e) { return fRet(new { Status = ActionResultStatus.Fail, e.Message }); }
        }

        #endregion

        #region DoUploadFile

        protected bool DoUploadFile(object id, HttpPostedFileBase file, out string filePath)
        {
            string fileName = NormalizeFileName(Path.GetFileName(file.FileName), id);

            TryCreatePath(ModuleUrl.TempFolder);

            filePath = Globals.MapPath(ModuleUrl.TempAttachmentPath(fileName));

            SaveFile(file, ref filePath);

            filePath = ModuleUrl.TempAttachmentPath(Path.GetFileName(filePath)).TrimStart('~');

            return true;
        }

        #endregion

        #region UploadFile

        [HttpPost]
        public ActionResult UploadFile(int id, HttpPostedFileBase fileUpload)
        {
            if (fileUpload == null)
                return JsonUpload(new { Status = ActionResultStatus.Fail, Message = StringTable.UploadImageErrExt });

            string filePath;
            if (DoUploadFile(id, fileUpload, out filePath))
            {
                return JsonUpload(new
                {
                    Status = ActionResultStatus.Success,
                    Message = string.Empty,
                    FilePath = filePath,
                    FileName = Path.GetFileName(filePath),
                    FileExt = Path.GetExtension(filePath).TrimStart('.').ToLower(),
                    MimeType = fileUpload.ContentType
                });
            }
            else return JsonUpload(new
            {
                Status = ActionResultStatus.Fail,
                Message = StringTable.UploadImageErrExt
            });
        }

        #endregion

        #region MoveFile

        /// <summary>
        /// return final file name
        /// </summary>
        protected string MoveFile(string permfolder, string tmpFolder, string fileUrl, string permThumbfolder = null, bool removeTmpFile = true)
        {
            var fileName = MoveFileEx(permfolder, tmpFolder, fileUrl, null, removeTmpFile);

            if (permfolder.EqualsIgCase(ModuleUrl.ImageFolder))
            {
                MoveFileEx(permThumbfolder ?? ModuleUrl.ThumbFolder, ModuleUrl.TempThumbFolder, fileUrl, fileName, removeTmpFile);
                MoveFileEx(ModuleUrl.OriginalImgFolder, ModuleUrl.TempOriginalFolder, fileUrl, fileName, removeTmpFile);
            }

            return fileName;
        }

        protected string MoveFileEx(string permfolder, string tmpFolder, string fileUrl, string newFileName = null, bool removeTmpFile = true)
        {
            var dstfileName = newFileName;
            if (dstfileName == null)
            {
                dstfileName = Path.GetFileNameWithoutExtension(fileUrl);
                dstfileName = Regex.Replace(dstfileName ?? "", @"\(\d+\)$", "");
                if (dstfileName.Length > 0) dstfileName += Path.GetExtension(fileUrl);
                else dstfileName = Path.GetFileName(fileUrl);
            }

            var srcPath = Globals.MapPath(tmpFolder.CombineWPath(Path.GetFileName(fileUrl)));
            var dstPath = Globals.MapPath(permfolder.CombineWPath(dstfileName));

            TryCreatePath(permfolder);

            TryMoveFile(srcPath, ref dstPath, newFileName != null, removeTmpFile);

            return Path.GetFileName(dstPath);
        }

        #endregion

        #region SaveUrl

        protected bool SaveUrl(object id, ref string url, string imageFolder, string thumbFolder = null)
        {
            if (!ImageTools.IsImageExtension(Path.GetExtension(url))) return false;

            int nTries = 3;
            HttpWebResponse wResp = null;

        LB_LOOP:
            try
            {
                var wReq = WebRequest.Create(url);
                wResp = wReq.GetResponse() as HttpWebResponse;

                if (wResp != null && wResp.StatusCode == HttpStatusCode.OK)
                {
                    var filename = string.Format("{0}{1}", id, Path.GetExtension(url));
                    var tmpfilePath = Globals.MapPath(ModuleUrl.TempFolder.CombineWPath(filename));
                    var imgfilePath = Globals.MapPath(imageFolder.CombineWPath(filename));

                    TryCreatePath(ModuleUrl.TempFolder);
                    SaveFile(wResp, ref tmpfilePath);

                    TryCreatePath(imageFolder);
                    int width = ImageWidth, height = ImageHeight;
                    if (!GetSettings().Display.Admin.Image.AutoResize)
                    {
                        using (var img = System.Drawing.Image.FromFile(tmpfilePath))
                        {
                            width = img.Width; height = img.Height;
                        }
                    }
                    ResizeImage(tmpfilePath, ref imgfilePath, width, height, BackgroundColor, ImageQuality);

                    url = Path.GetFileName(imgfilePath);

                    if (thumbFolder != null)
                    {
                        TryCreatePath(thumbFolder);
                        string tmpThumbFile = Globals.MapPath(ModuleUrl.ThumbPath(Path.GetFileName(imgfilePath)));
                        CreateImageThumb(tmpfilePath, tmpfilePath, ref tmpThumbFile, ThumbWidth, ThumbHeight, BackgroundColor, ImageQuality);
                    }

                    TryDeleteFile(tmpfilePath);

                    return true;
                }
            }
            catch (Exception err)
            {
                if (--nTries >= 0)
                {
                    Thread.Sleep(1000); goto LB_LOOP;
                }
                else LogErr(err);
            }
            finally
            {
                if (wResp != null) wResp.Close();
            }

            return false;
        }

        #endregion

        #region ToExcel

        protected byte[] ToExcel<TModel>(List<TModel> list, string[] fields,
            Dictionary<string, LambdaExpression> fieldDic = null, Func<PropertyInfo, string> fDispName = null,
            Action<HSSFWorkbook, ISheet, ICell> filter = null, int topRows = 0, int bottomRows = 0,
            Action<HSSFWorkbook, ISheet, IRow[], IRow[]> fCustom = null)
        {
            //Create new Excel workbook
            var workbook = new HSSFWorkbook();
            int rowNumber = 0;

            //Create new Excel sheet
            var sheet = workbook.CreateSheet();

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
            hdrfont.Color = (NPOI.HSSF.Util.HSSFColor.DarkBlue.Index);
            hdrfont.Boldweight = (short)FontBoldWeight.Bold;

            var hdrCss = workbook.CreateCellStyle();
            hdrCss.Alignment = HorizontalAlignment.CenterSelection;
            hdrCss.SetFont(hdrfont);
            hdrCss.FillForegroundColor = NPOI.HSSF.Util.HSSFColor.Grey25Percent.Index;
            hdrCss.FillPattern = FillPattern.SolidForeground;

            if (fieldDic == null)
            {
                var param = Expression.Parameter(typeof(TModel), "x");
                fieldDic = typeof(TModel).GetProperties()
                    .ToDictionary(x => x.Name.ToLower(), x => Expression.Lambda(Expression.Property(param, x), param));
            }

            if (fDispName == null) fDispName = x => x.GetDisplayName();

            fields = fields.Select(x => x.ToLower()).Where(x => fieldDic.ContainsKey(x)).ToArray();

            //headerRow.CreateCell(1).SetCellValue("Name");
            for (int i = 0; i < fields.Length; i++)
            {
                var headerName = fDispName((PropertyInfo)((MemberExpression)fieldDic[fields[i]].Body).Member);
                var cell = headerRow.CreateCell(i);
                cell.SetCellValue(headerName); cell.CellStyle = hdrCss;
            }

            // Freeze the header row so it is not scrolled
            sheet.CreateFreezePane(0, rowNumber, 0, rowNumber);

            //Populate the sheet with values from the grid data
            foreach (var itm in list)
            {
                //Create a new row
                var row = sheet.CreateRow(rowNumber++);
                var viewData = new ViewDataDictionary<TModel>(itm);

                //row.CreateCell(1).SetCellValue(wo.Name);
                for (int i = 0; i < fields.Length; i++)
                {
                    var metadata = ModelMetadata.FromStringExpression(ExpressionHelper.GetExpressionText(fieldDic[fields[i]]), viewData);
                    var cell = row.CreateCell(i);
                    cell.SetCellValue((metadata.Model ?? "").ToString());
                    if (filter != null) filter(workbook, sheet, cell);
                }
            }

            // Hide the gridlines
            sheet.DisplayGridlines = true;

            var bottomRws = new List<IRow>();

            for (int r = 0; r < bottomRows; r++)
            {
                var rcs = workbook.CreateCellStyle();
                rcs.SetFont(workbook.CreateFont());
                rcs.FillForegroundColor = NPOI.HSSF.Util.HSSFColor.LightYellow.Index;
                rcs.FillPattern = FillPattern.SolidForeground;

                var row = sheet.CreateRow(rowNumber++);
                for (int i = 0; i < fields.Length; i++)
                {
                    row.CreateCell(i).CellStyle = rcs;
                }
                bottomRws.Add(row);
            }

            if (fCustom != null) fCustom(workbook, sheet, topRws.ToArray(), bottomRws.ToArray());

            if (list.Count <= 1000)
            {
                // set default style and auto size for columns
                var sharefont = workbook.CreateFont();
                //sharefont.FontName = "Calibri";
                //sharefont.FontHeightInPoints = 12;
                var shareCss = workbook.CreateCellStyle();
                shareCss.SetFont(sharefont);

                for (int i = 0; i < fields.Length; i++)
                {
                    var oldWidth = sheet.GetColumnWidth(i);
                    try
                    {
                        sheet.SetDefaultColumnStyle((short)i, shareCss);
                        sheet.AutoSizeColumn(i);
                        sheet.SetColumnWidth(i, sheet.GetColumnWidth(i) + 256);
                    }
                    catch (Exception err) { sheet.SetColumnWidth(i, oldWidth); }
                }
            }
            else
            {
                System.Threading.Tasks.Task.Run(() =>
                {
                    System.Threading.Thread.Sleep(5000);
                    GC.Collect();
                });
            }

            //Write the workbook to a memory stream
            var output = new MemoryStream();
            workbook.Write(output);
            return output.ToArray();
        }

        #endregion

        #region PrintToPdf

        protected void PrintToPdf(string htmlData, ref string pdfFile, int marginX = 50, int marginY = 50,
            bool drawFrame = false)
        {
            PrintToPdf(new List<string>() { htmlData }, ref pdfFile, marginX, marginY, drawFrame);
        }

        protected void PrintToPdf(List<string> htmlDatas, ref string pdfFile, int marginX = 50, int marginY = 50, bool drawFrame = false)
        {
            var doc = PrintToPdf(htmlDatas, marginX, marginY, drawFrame);
            var tmp = pdfFile;
            int iseq = 1;

            while (System.IO.File.Exists(tmp))
            {
                tmp = InjectFilePath(pdfFile, iseq++);
            }

            doc.Save(pdfFile = tmp);
        }

        /// <param name="pageSizeName">
        ///     <see cref="http://www.websupergoo.com/helppdfnet/source/5-abcpdf/doc/2-properties/mediabox.htm"/>
        ///     <see cref="http://www.websupergoo.com/helppdfnet/source/5-abcpdf/xrect/2-properties/string.htm"/>
        /// </param>
        protected Doc PrintToPdf(string htmlData, int marginX = 50, int marginY = 50, bool drawFrame = false, string pageSizeName = null, bool setRect = false, bool landScape = false)
        {
            return PrintToPdf(new List<string>() { htmlData }, marginX, marginY, drawFrame, pageSizeName, setRect, landScape);
        }

        /// <param name="pageSizeName">
        ///     <see cref="http://www.websupergoo.com/helppdfnet/source/5-abcpdf/doc/2-properties/mediabox.htm"/>
        ///     <see cref="http://www.websupergoo.com/helppdfnet/source/5-abcpdf/xrect/2-properties/string.htm"/>
        /// </param>
        protected Doc PrintToPdf(List<string> htmlDatas, int marginX = 50, int marginY = 50, bool drawFrame = false, string pageSizeName = null, bool setRect = false, bool landScape = false)
        {
            var doc = new Doc();
            doc.HtmlOptions.AddLinks = true;
            #region Cookie
            //if (Request.Cookies.Count > 0)
            //{
            //    var builder = new System.Text.StringBuilder("Cookie: ");
            //    for (int i = 0; i < Request.Cookies.Count; ++i)
            //    {
            //        var cookie = Request.Cookies[i];
            //        if (i > 0) builder.Append("; ");
            //        builder.Append(cookie.Name + "=" + cookie.Value);
            //    }
            //    builder.Append("\r\n");
            //    doc.HtmlOptions.HttpAdditionalHeaders = builder.ToString();
            //}

            //doc.HtmlOptions.NoCookie = true;
            //doc.HtmlOptions.PageLoadMethod = PageLoadMethodType.MonikerForHtml; 
            #endregion

            if (!pageSizeName.IsNullOrEmpty()) doc.MediaBox.String = pageSizeName;

            if (setRect) doc.Rect.String = doc.MediaBox.String;
            else doc.Rect.Inset(marginX, marginY);

            if (landScape)
            {
                // apply a rotation transform
                var w = doc.MediaBox.Width;
                var h = doc.MediaBox.Height;
                var l = doc.MediaBox.Left;
                var b = doc.MediaBox.Bottom;
                doc.Transform.Rotate(90, l, b);
                doc.Transform.Translate(w, 0);

                // rotate our rectangle
                doc.Rect.Width = h;
                doc.Rect.Height = w;
            }

            for (int i = 0; i < htmlDatas.Count; i++)
            {
                var imageId = doc.AddImageHtml(htmlDatas[i]);

                while (true)
                {
                    if (drawFrame) doc.FrameRect();
                    if (!doc.Chainable(imageId))
                    {
                        if (i < htmlDatas.Count - 1) doc.Page = doc.AddPage();
                        break;
                    }

                    doc.Page = doc.AddPage();
                    imageId = doc.AddImageToChain(imageId);
                }
            }

            for (int i = 1; i <= doc.PageCount; i++)
            {
                doc.PageNumber = i;
                doc.Flatten();
            }

            return doc;
        }

        #endregion

        #region GenerateBarCode

        public static System.Drawing.Image GenerateBarCode(string barcode, Spire.Barcode.BarCodeType? type = null,
            int? width = null, int? height = null, int? dpiX = null, int? dpiY = null, int? barHeight = null, bool showText = true,
            int? topMargin = null, int? bottomMargin = null, int? leftMargin = null, int? rightMargin = null)
        {
            var bzSetting = SettingsBiz.GetSettings().Display.BarCode;
            var bcSetting = new BarcodeSettings();
            Spire.Barcode.BarCodeType type2;

            if (!Enum.TryParse(bzSetting.Standard, out type2)) return null;

            bcSetting.Data = barcode;
            bcSetting.Type = type ?? type2;
            bcSetting.ShowText = showText;
            bcSetting.ShowTextOnBottom = true;
            bcSetting.Unit = GraphicsUnit.Millimeter;
            bcSetting.ResolutionType = ResolutionType.Printer;
            if (showText) 
                bcSetting.TextFont = new Font(bzSetting.FontFamily, bzSetting.FontSize);
            bcSetting.LeftMargin = leftMargin ?? bzSetting.LeftMargin;
            bcSetting.RightMargin = rightMargin ?? bzSetting.RightMargin;
            bcSetting.TopMargin = topMargin ?? bzSetting.TopMargin;
            bcSetting.BottomMargin = bottomMargin ?? bzSetting.BottomMargin;
            bcSetting.ImageWidth = width ?? bzSetting.Image.Width;
            bcSetting.ImageHeight = height ?? bzSetting.Image.Height;
            bcSetting.BarHeight = barHeight ?? bzSetting.BarHeight;
            bcSetting.DpiX = dpiX ?? bzSetting.DpiX;
            bcSetting.DpiY = dpiY ?? bzSetting.DpiY;

            var generator = new BarCodeGenerator(bcSetting);

            return generator.GenerateImage();
        }

        #endregion

        #region PrintBarCode

        /// <summary>print barcode to pdf</summary>
        /// <param name="width">mm</param>
        /// <param name="height">mm</param>
        public ActionResult PrintBarCode(string barcode, string topText, int? width = null, int? height = null, Spire.Barcode.BarCodeType? type = null)
        {
            const decimal m2ToPx = 2.8346456692913385826771653543307m; //72dpi
            var settings = GetSettings();
            var w = (int) ((width ?? settings.Display.BarCode.Image.Width)*m2ToPx);
            var h = (int)((height ?? settings.Display.BarCode.Image.Height) * m2ToPx);
            var barH = Math.Min((int)(settings.Display.BarCode.BarHeight * m2ToPx), (int)(h * 0.8));

            var marginTop = Math.Abs(settings.Hidden.BarCodeMarginTop * settings.Display.BarCode.DpiY) / 300;
            var img = GenerateBarCode(barcode, width: w, height: h + marginTop, barHeight: barH, showText: false, type: type);
            var ratio = img == null ? 0 : (img.Width * 1.0m) / w;
            var fontSize = topText.IsNullOrEmpty() ? 0 : (int)(ratio * 0.6667m * settings.Display.BarCode.FontSize);
            var textH = (int) (1.8*fontSize);

            img = CropImage(img, 0, marginTop + textH, img.Width, img.Height - marginTop - textH);

            var fstyle = "vertical-align: bottom;font-weight: bold;font-size:" + fontSize + "px;text-align: center;line-height:" + fontSize + "px;height: " + (textH - fontSize/2) + "px;vertical-align: middle;padding-bottom: " + (fontSize / 2) + "px;";
            var doc = PrintToPdf("<table style=\"border-collapse:collapse;width:" + img.Width + "px;height:" + ((h * img.Width) / w)
                + "px;\"><tr><td style=\"" + fstyle + "\">" + (topText ?? "") + "</td></tr><tr><td style=\"text-align:center;vertical-align: middle;\"><img style=\"width:"
                + img.Width + "px;height:" + Math.Min(img.Height, h * img.Width*95 / (w*100) - (int)(textH*1.2)) + "px;\" src=\"" 
                + img.ToBase64() + "\" /></td></tr></table>", 0, 0, false, string.Format("0 0 {0} {1}", w, h), true);

            return File(doc.GetStream(), "application/pdf", string.Format("{0}.pdf", barcode));
        }
        
        #endregion

        #region GenerateQrCode

        public static System.Drawing.Image GenerateQrCode(string data)
        {
            var bzSetting = SettingsBiz.GetSettings().Display.QrCode;
            var bcSetting = new BarcodeSettings();

            bcSetting.Data2D = data;
            bcSetting.Data = data;
            bcSetting.Type = Spire.Barcode.BarCodeType.QRCode;
            bcSetting.Unit = GraphicsUnit.Millimeter;
            bcSetting.LeftMargin = 1;
            bcSetting.RightMargin = 1;
            bcSetting.TopMargin = 1;
            bcSetting.BottomMargin = 1;
            bcSetting.ShowText = false;
            bcSetting.QRCodeDataMode = QRCodeDataMode.AlphaNumber;
            bcSetting.QRCodeECL = QRCodeECL.M;
            bcSetting.DpiX = bzSetting.DpiX;
            bcSetting.DpiY = bzSetting.DpiY;

            var generator = new BarCodeGenerator(bcSetting);
            var img = generator.GenerateImage();

            if (img.Height <= img.Width) return img;
            return CropImage(img, 0, img.Height - img.Width, img.Width, img.Width);
        }

        #endregion

        #region CropImage

        public static Bitmap CropImage(System.Drawing.Image src, int x, int y, int width, int height)
        {
            var r = new Rectangle(x, y, width, height);
            var target = new Bitmap(width, height);

            using (var g = Graphics.FromImage(target))
            {
                g.DrawImage(src, new Rectangle(0, 0, target.Width, target.Height), r, GraphicsUnit.Pixel);
            }

            return target;
        }

        #endregion

        #region CreateImage

        protected string CreateImage(string color, int width, int height, string thumbFile = null)
        {
            var img = new Bitmap(width, height);

            using (var g = Graphics.FromImage(img))
            {
                g.FillRectangle(new SolidBrush(ColorTranslator.FromHtml(color)), 0, 0, width, height);
            }

            if (thumbFile != null)
            {
                var tmpThumbFolder = ModuleUrl.TempThumbFolder;
                TryCreatePath(tmpThumbFolder);
                var tmpthumbfile = Globals.MapPath(tmpThumbFolder.CombineWPath(Path.GetFileName(thumbFile)));

                SaveFile(img, ref tmpthumbfile);

                return Globals.ResolveClientUrl(tmpThumbFolder.CombineWPath(Path.GetFileName(tmpthumbfile)));
            }
            else
            {
                var tmpFolder = ModuleUrl.TempFolder;
                TryCreatePath(tmpFolder);
                var tmpfilePath = Globals.MapPath(tmpFolder.CombineWPath(color.Replace("#", "color") + ".png"));

                SaveFile(img, ref tmpfilePath);

                return Globals.ResolveClientUrl(tmpFolder.CombineWPath(Path.GetFileName(tmpfilePath)));
            }
        }

        #endregion

        #region UpdateModel

        protected bool UpdateModel<TModel>(TModel model, DataRow row, List<string> properties = null,
            Dictionary<string, string> columnMap = null, string prefix = null, Func<string, string, string> filter = null)
            where TModel : class
        {
            if (properties == null) properties = typeof(TModel).GetProperties().Select(x => x.Name).ToList();

            var collection = new NameValueCollection();

            foreach (var property in properties)
            {
                var name = property;
                var colName = columnMap == null ? name
                    : columnMap.Where(x => x.Key.EqualsIgCase(name)).Select(x => x.Value).FirstOrDefault();

                if (colName != null && (columnMap != null || row.Table.Columns.Contains(colName)))
                {
                    var value = row[colName].ToString();
                    if (!value.IsNullOrEmpty()) value = value.Trim();
                    collection.Add(name, filter == null ? value : filter(name, value));
                }
            }

            return UpdateModel(model, collection, prefix);
        }

        protected bool UpdateModel<TModel>(TModel model, NameValueCollection collection, string prefix = null)
            where TModel : class
        {
            var data = new NameValueCollectionValueProvider(collection, null);
                           
            return TryUpdateModel(model, prefix, data);
        }

        /// <summary>Name must be unique</summary>
        public bool UpdateModelEx(object model, NameValueCollection collection, string prefix = null)
        {
            if (model == null || collection == null || collection.Count == 0) return true;

            const BindingFlags flags = BindingFlags.NonPublic | BindingFlags.InvokeMethod | BindingFlags.Instance;
            var func = typeof(BaseController).GetMethod("UpdateModelEx", flags).MakeGenericMethod(model.GetType());

            return (bool)func.Invoke(this, new[] { model, collection, null });
        }

        private bool UpdateModelEx<TModel>(TModel model, NameValueCollection collection, string prefix = null)
            where TModel : class
        {
            return UpdateModel(model, collection, prefix);
        }

        #endregion

        #region Copy

        protected bool Copy<TModel>(TModel source, TModel target, params string[] fields)
            where TModel : class
        {
            if (target == null || source == null) return false;

            var collection = new NameValueCollection();
            var viewData = new ViewDataDictionary<TModel>(source);

            for (int i = 0; i < fields.Length; i++)
            {
                var metadata = ModelMetadata.FromStringExpression(fields[i], viewData);
                collection.Add(fields[i], (metadata.Model ?? "").ToString());
            }

            return UpdateModel(target, collection);
        }

        #endregion

        #region GetChanges

        protected List<ModelMetadata> GetChanges<TModel>(TModel source, TModel target,
            string[] fields, Dictionary<string, string> mapping = null)
            where TModel : class
        {
            var ret = new List<ModelMetadata>();

            if (target == null || source == null) return ret;

            var viewDataSrc = new ViewDataDictionary<TModel>(source);
            var viewDataDst = new ViewDataDictionary<TModel>(target);

            for (int i = 0; i < fields.Length; i++)
            {
                var metadataSrc = ModelMetadata.FromStringExpression(fields[i], viewDataSrc);
                var metadataDst = ModelMetadata.FromStringExpression(fields[i], viewDataDst);
                if ((metadataSrc.Model ?? "").ToString() != (metadataDst.Model ?? "").ToString())
                {
                    if (mapping == null || !mapping.ContainsKey(fields[i])) ret.Add(metadataDst);
                    else
                    {
                        var metadataDst2 = ModelMetadata.FromStringExpression(mapping[fields[i]], viewDataDst);
                        ret.Add(metadataDst2);
                    }
                }
            }

            return ret;
        }

        #endregion

        #region GoogleTranslate

        //protected string GoogleTranslate(string text, string langFrom, string langTo)
        //{
        //    const string ePattern = @"\s+Errors\s+\[(\s*Message\[(?<msg>[^\]]+)\]|\s*Location\[(?<loc>[^\]]+)\]|\s*Reason\[(?<rea>[^\]]+)\]|\s*Domain\[(?<dom>[^\]]+)\])+\s*\]";
        //    var service = new TranslateService(new BaseClientService.Initializer
        //    {
        //        ApplicationName = "Translate eShop",
        //        ApiKey = "[YOUR_API_KEY_HERE]",
        //    });

        //    var langs = service.Languages.List();
        //    var req = service.Translations.List(new Repeatable<string>(new[] { text }), langTo);
        //    req.Source = langFrom;

        //    try
        //    {
        //        var rr2 = langs.Execute();
        //        var result = req.Execute();
        //        return result.Translations.Select(x => x.TranslatedText).FirstOrDefault();
        //    }
        //    catch (Google.GoogleApiException e)
        //    {
        //        var m = Regex.Match(e.Message, ePattern, RegexOptions.IgnoreCase);
        //        if (m.Success)
        //            throw new Exception(string.Format("{0}: {1} - {2}", m.Groups["msg"], m.Groups["rea"], m.Groups["dom"]));
        //        else throw e;
        //    }
        //}

        #endregion

        #region SendEmail
        public static bool SendEmail(string subject, string body, string from, string to)
        {
            int tries = 3;
        LB_LOOP:
            try
            {
                var result = true;
                if (!string.IsNullOrEmpty(to))
                {
                    var msg = EmailSender.InstantSend(subject, body, true, from, to);
                    result &= string.IsNullOrEmpty(msg);
                }
                return result;
            }
            catch (Exception e)
            {
                if (--tries > 0)
                {
                    Thread.Sleep(1000);
                    goto LB_LOOP;
                }
                else LogErr(e);

                return false;
            }
        }

        public static bool SendEmailReplyTo(string subject, string body, string from, string to, string cc, string replyTo, string displayNameFrom = null, string dislayNameTo = null, string dislayNameReplyTo = null, string[] attachFile = null)
        {
            int tries = 3;
        LB_LOOP:
            try
            {
                var result = true;
                if (!string.IsNullOrEmpty(to))
                {
                    //var msg = EmailSender.InstantSend(subject, body, true, from, to, cc, displayNameFrom, dislayNameTo, System.Text.Encoding.UTF8, System.Text.Encoding.UTF8, !string.IsNullOrEmpty(attachFile) ? new[] { attachFile } : null);
                    var msg = EmailSender.SendReplyTo(subject, body, true, from, to, cc, null, replyTo, displayNameFrom, dislayNameTo, dislayNameReplyTo, System.Text.Encoding.UTF8, System.Text.Encoding.UTF8, attachFile != null ? attachFile.CloneFiles() : null);
                    result &= string.IsNullOrEmpty(msg);
                }

                return result;
            }
            catch (Exception e)
            {
                if (--tries > 0)
                {
                    Thread.Sleep(1000);
                    goto LB_LOOP;
                }
                else LogErr(e);

                return false;
            }
        }

        public static bool SendEmail(string subject, string body, string from, string to, string cc, string displayNameFrom = null, string dislayNameTo = null, string[] attachFile = null)
        {
            int tries = 3;
        LB_LOOP:
            try
            {
                var result = true;
                if (!string.IsNullOrEmpty(to))
                {
                    //var msg = EmailSender.InstantSend(subject, body, true, from, to, cc, displayNameFrom, dislayNameTo, System.Text.Encoding.UTF8, System.Text.Encoding.UTF8, !string.IsNullOrEmpty(attachFile) ? new[] { attachFile } : null);
                    var msg = EmailSender.Send(subject, body, true, from, to, cc, null, displayNameFrom, dislayNameTo, System.Text.Encoding.UTF8, System.Text.Encoding.UTF8, attachFile != null ? attachFile.CloneFiles() : null);
                    result &= string.IsNullOrEmpty(msg);
                }

                return result;
            }
            catch (Exception e)
            {
                if (--tries > 0)
                {
                    Thread.Sleep(1000);
                    goto LB_LOOP;
                }
                else LogErr(e);

                return false;
            }
        }

        public static bool SendEmail(string subject, string body, string from, string to, string toAdmin, List<Attachment> attachments,
            string displayNameFrom = null, string dislayNameTo = null, string toCc = null)
        {
            var sendCustomer = false;
            int tries = 3;
        LB_LOOP:
            try
            {
                var result = true;
                List<Attachment> admAttachs = null;
                if (attachments != null && !toAdmin.IsNullOrEmpty())
                    admAttachs = attachments.Select(x =>
                    {
                        var ret = new Attachment(new System.IO.MemoryStream(x.ContentStream.ReadData()), x.ContentType);
                        x.ContentStream.Seek(0, SeekOrigin.Begin);
                        ret.ContentId = x.ContentId;
                        return ret;
                    }).ToList();

                if (!string.IsNullOrEmpty(to) && !sendCustomer)
                {
                    var msg = EmailSender.InstantSend(subject, body, true, from, to, toCc, null, displayNameFrom, dislayNameTo, System.Text.Encoding.UTF8, System.Text.Encoding.UTF8, attachments);
                    result &= string.IsNullOrEmpty(msg);
                    sendCustomer = true;
                }
                if (!string.IsNullOrEmpty(toAdmin))
                {
                    var msgAdmin = EmailSender.InstantSend(subject, body, true, from, toAdmin, null, null, displayNameFrom, dislayNameTo, System.Text.Encoding.UTF8, System.Text.Encoding.UTF8, admAttachs);
                    result &= string.IsNullOrEmpty(msgAdmin);
                }

                return result;
            }
            catch (Exception e)
            {
                if (--tries > 0)
                {
                    Thread.Sleep(1000);
                    goto LB_LOOP;
                }
                else LogErr(e);

                return false;
            }

        }

        public static bool SendEmailReplyToEx(string subject, string body, string from, string to, string toAdmin, string replyTo, List<Attachment> attachments,
            string displayNameFrom = null, string dislayNameTo = null, string toCc = null)
        {
            var sendCustomer = false;
            int tries = 3;
        LB_LOOP:
            try
            {
                var result = true;
                List<Attachment> admAttachs = null;
                if (attachments != null && !toAdmin.IsNullOrEmpty())
                    admAttachs = attachments.Select(x =>
                    {
                        var ret = new Attachment(new System.IO.MemoryStream(x.ContentStream.ReadData()), x.ContentType);
                        x.ContentStream.Seek(0, SeekOrigin.Begin);
                        ret.ContentId = x.ContentId;
                        return ret;
                    }).ToList();

                if (!string.IsNullOrEmpty(to) && !sendCustomer)
                {
                    var msg = InstantSendReplyTo(subject, body, true, from, to, toCc, null, replyTo, displayNameFrom, dislayNameTo, displayNameFrom, Encoding.UTF8, Encoding.UTF8, attachments);
                    result &= string.IsNullOrEmpty(msg);
                    sendCustomer = true;
                }

                if (!string.IsNullOrEmpty(toAdmin))
                {
                    var msgAdmin = EmailSender.InstantSend(subject, body, true, from, toAdmin, null, null, displayNameFrom, dislayNameTo, Encoding.UTF8, Encoding.UTF8, admAttachs);
                    result &= string.IsNullOrEmpty(msgAdmin);
                }

                return result;
            }
            catch (Exception e)
            {
                if (--tries > 0)
                {
                    Thread.Sleep(1000);
                    goto LB_LOOP;
                }
                else LogErr(e);

                return false;
            }

        }

        #endregion

        #region InstantSendReplyTo
        public static string InstantSendReplyTo(string subject, string body, bool isHtmlBody, string from, string to, string cc, string bcc, string replyTo, string displayNameFrom, string dislayNameTo, string displayNameReplyTo, Encoding subjectEncoding, Encoding bodyEncoding, List<Attachment> attachFiles)
        {
            //LogHelper.Log4net(new Exception(string.Format("InstantSendReplyTo -> subject:{0}, from:{1}, to:{2}, cc:{3}, bcc:{4}, replyTo:{5}", subject, from, to, cc, bcc, replyTo)), ClawExceptionType.DataProvider, "");

            try
            {
                var mail = new MailMessage();
                mail.From = new MailAddress(from, displayNameFrom);
                mail.IsBodyHtml = isHtmlBody;
                mail.Subject = subject;
                mail.SubjectEncoding = subjectEncoding;
                mail.BodyEncoding = bodyEncoding;

                //var regEx = new Regex(@"^([\w-\.]+)@((\[[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}\.)|(([\w-]+\.)+))([a-zA-Z]{2,4}|[0-9]{1,3})(\]?)$");
                
                if (!string.IsNullOrEmpty(replyTo) /*&& regEx.IsMatch(replyTo)*/)
                {
                    var mailReplyTo = string.IsNullOrEmpty(displayNameReplyTo) ? new MailAddress(replyTo) : new MailAddress(replyTo, displayNameReplyTo);
                    mail.ReplyToList.Add(mailReplyTo);
                }
                
                #region To email

                string[] addresses = to.Split(new string[] { ";", "," }, StringSplitOptions.RemoveEmptyEntries);

                if (addresses.Length == 1)
                {
                    mail.To.Add(new MailAddress(addresses[0], dislayNameTo));
                }
                else
                {
                    foreach (string adr in addresses)
                    {
                        //if (regEx.IsMatch(adr.Trim()))
                            mail.To.Add(adr);
                    }
                }

                #endregion

                #region CC email

                if (!string.IsNullOrEmpty(cc))
                {
                    string[] ccAddresses = cc.Split(new string[] { ";", "," }, StringSplitOptions.RemoveEmptyEntries);
                    if (ccAddresses != null && ccAddresses.Length > 0)
                    {
                        foreach (string adr in ccAddresses)
                        {
                            //if (regEx.IsMatch(adr.Trim()))
                                mail.CC.Add(adr);
                        }
                    }
                }

                #endregion

                #region BCC email

                if (!string.IsNullOrEmpty(bcc))
                {
                    string[] bccAddresses = bcc.Split(new string[] { ";", "," }, StringSplitOptions.RemoveEmptyEntries);
                    if (bccAddresses != null && bccAddresses.Length > 0)
                    {
                        foreach (string adr in bccAddresses)
                        {
                            //if (regEx.IsMatch(adr.Trim()))
                                mail.Bcc.Add(adr);
                        }
                    }
                }

                #endregion

                #region Attach files

                if (attachFiles != null)
                {
                    foreach (var item in attachFiles)
                    {
                        mail.Attachments.Add(item);
                    }
                }

                #endregion

                #region email body

                // Send email with plain text
                if (!mail.IsBodyHtml)
                {
                    var plainText = AlternateView.CreateAlternateViewFromString(body, Encoding.GetEncoding("iso-8859-1"), "text/plain");
                    plainText.TransferEncoding = System.Net.Mime.TransferEncoding.SevenBit;
                    mail.AlternateViews.Add(plainText);
                }
                else
                {
                    mail.Body = body;
                }

                #endregion

                var smtpClient = GetCurrentSystemEmailSetting();

                #region delete file when send complete

                /*
                smtpClient.SendCompleted += (object sender, AsyncCompletedEventArgs e) =>
                {
                    if (attachFiles != null)
                    {
                        foreach (var attachFile in attachFiles)
                        {
                            var path = attachFile ?? "";

                            if (path.StartsWith("~/"))
                                path = Globals.MapPath(path);

                            if (System.IO.File.Exists(path))
                            {
                                try
                                {
                                    System.IO.File.Delete(path);
                                }
                                catch (Exception ex)
                                {
                                    LogHelper.LogText(ex, ClawExceptionType.DeleteObject);
                                }
                            }
                        }
                    }
                };
                */

                #endregion

                smtpClient.Send(mail);

                mail.Dispose();
            }
            catch (SmtpFailedRecipientsException rexp)
            {
                LogHelper.LogText(rexp, ClawExceptionType.EmailUnableToSend);
                return rexp.ToString();
            }
            catch (Exception exp)
            {
                LogHelper.LogText(exp, ClawExceptionType.EmailUnableToSend);
                return exp.ToString();
            }

            return string.Empty;
        }

        public static SmtpClient GetCurrentSystemEmailSetting()
        {
            var site = IoC.Kernel.Get<IDataService>().GetCurrentSite();
            var settings = site.Settings;
            var mailServer = new SmtpClient(settings.SmtpServer, settings.SmtpPort);

            mailServer.EnableSsl = settings.SmtpEnableSSL;

            if (settings.SmtpAuthentication)
            {
                var authentication = new NetworkCredential(settings.SmtpUsername, settings.SmtpPassword);
                mailServer.Credentials = authentication;
            }

            return mailServer;
        }

        #endregion

        #region DelayTasks

        public class JobInfo
        {
            public List<string> Urls { get; set; }
            public CookieContainer Cookies { get; set; }

            #region JobInfo
            public JobInfo()
            {
                Cookies = new CookieContainer();
                var req = System.Web.HttpContext.Current.Request;
                foreach (var cookie in req.Cookies.AllKeys.Select(x => req.Cookies.Get(x)).Where(x => x != null))
                {
                    try
                    {
                        if (!cookie.Name.EqualsIgCase("ASP.NET_SessionId"))
                            Cookies.Add(new Cookie(cookie.Name, HttpUtility.UrlEncode(cookie.Value), cookie.Path, cookie.Domain ?? req.Url.Host));
                    }
                    catch (Exception err)
                    {
                        if (Debugger.IsAttached) Debugger.Break();
                        else LogErr(err);
                    }
                }
            }
            #endregion
        }

        protected static bool DelayTasks(List<string> urls)
        {
            if (urls.SafeCount() == 0) return false;

            var reqUrl = System.Web.HttpContext.Current.Request.Url;
            var reqHost = reqUrl.Scheme + "://" + reqUrl.Authority;

            return ThreadPool.QueueUserWorkItem(DoDelayJob, new JobInfo()
            {
                Urls = urls.Select(x => reqHost + x).ToList(),
            });
        }

        private static void DoDelayJob(object data)
        {
            var jInfo = (JobInfo)data;

            if (jInfo == null) return;

            foreach (var url in jInfo.Urls)
            {
                try
                {
                    var wReq = (HttpWebRequest)WebRequest.Create(url);
                    wReq.CookieContainer = jInfo.Cookies;
                    wReq.BeginGetResponse(DelayJobCallback, wReq);
                }
                catch (Exception err)
                {
                    LogErr(err);
                }
            }
        }

        private static void DelayJobCallback(IAsyncResult ar)
        {
            try
            {
                var wReq = (WebRequest)ar.AsyncState;
                var wResp = (HttpWebResponse)wReq.EndGetResponse(ar);
                if (wResp != null && wResp.StatusCode != HttpStatusCode.OK)
                {
                    LogErr(new Exception(wReq.RequestUri.AbsoluteUri + " failed with status code :" + wResp.StatusCode));
                }
            }
            catch (Exception err)
            {
                LogErr(err);
            }
        }

        #endregion

        #region ExecWebMethod
        public static bool ExecWebMethod(string url, out string errMsg, int retries = 3)
        {
            errMsg = null;

            if (url.IsNullOrEmpty() || EagerLoadHttpModule.SiteUrl.IsNullOrEmpty()) return false;

        LB_LOOP:
            try
            {
                var wReq = (HttpWebRequest)WebRequest.Create(EagerLoadHttpModule.SiteUrl + url);
                var tsk = wReq.GetResponseAsync();

                tsk.Wait();

                var resp = (HttpWebResponse)tsk.Result;

                if (resp.StatusCode == HttpStatusCode.OK) return true;
                else errMsg = resp.StatusDescription;
            }
            catch (Exception err)
            {
                if (--retries >= 0) goto LB_LOOP;
                else LogHelper.Log(err, errMsg = err.Message);
            }

            return false;
        } 
        #endregion

        #region JsonStatus

        protected ActionResult JsonStatus(string action, bool isSuccess, string message = null,
            Action<dynamic> fData = null)
        {
            return JsonStatus(action, isSuccess, message, fData, false);
        }

        protected ActionResult JsonStatus(string action, bool isSuccess, string message, Action<dynamic> fData, bool warning)
        {
            dynamic obj = new ExpandoObject();

            obj.Status = (isSuccess
                ? (warning ? ActionResultStatus.Warning : ActionResultStatus.Success)
                : ActionResultStatus.Fail);

            //obj.Message = message ?? string.Format("{0} {1}", action, (isSuccess ? StringTable.Success : StringTable.Unsuccess).ToString().ToLower());

            obj.Message = message ?? string.Format(isSuccess ? StringTable.TemplActionSuccess : StringTable.TemplActionUnsuccess, (action ?? "").ToLower());

            if (fData != null) fData(obj);

            return Json(obj);
        }

        #endregion

        #region SetViewMsgInput

        protected void SetViewMsgInput(bool isSuccess, string customMsg = null)
        {
            var action = GetParam<long>("Id") == 0 ? StringTable.AddNew : StringTable.Updated;
            var modstate = ModelState[""];

            if (modstate != null && modstate.Errors.Count != 0)
                customMsg = modstate.Errors[0].ErrorMessage;

            SetViewMsg(isSuccess, action, customMsg);
        }

        #endregion

        #region SetViewMsg

        protected void SetViewMsg(bool isSuccess, string action = null, string customMsg = null)
        {
            TempData["ActionStatus"] = (isSuccess ? ActionResultStatus.Success : ActionResultStatus.Fail);
            //var msg = string.Format("{0} {1}", action, (isSuccess ? StringTable.Success : StringTable.Unsuccess).ToString().ToLower());

            var msg = string.Format(isSuccess ? StringTable.TemplActionSuccess : StringTable.TemplActionUnsuccess, (action ?? "").ToLower());

            ViewBag.Message = TempData["ActionMessage"] = customMsg.IsNullOrEmpty() ? msg : customMsg;
        }

        #endregion

        #region JsonInline
        protected internal ActionResult JsonInline(DataSourceRequest request, object model, bool isSuccess,
            Action<dynamic> fData = null, JsonRequestBehavior behavior = JsonRequestBehavior.DenyGet, string custMsg = null)
        {
            var action = GetParam<int>("Id") == 0 ? StringTable.AddNew : StringTable.Update;
            var ret = new[] { model }.ToDataSourceResult(request, ModelState);

            dynamic obj = new ExpandoObject();

            obj.Data = ret.Data;
            obj.Status = (isSuccess ? ActionResultStatus.Success : ActionResultStatus.Fail);
            //obj.Message = string.Format("{0} {1}", action, (isSuccess ? StringTable.Success : StringTable.Unsuccess).ToLower());
            obj.Message = custMsg ?? string.Format(isSuccess ? StringTable.TemplActionSuccess : StringTable.TemplActionUnsuccess, (action ?? "").ToLower());
            obj.Total = ret.Total;
            obj.Errors = ret.Errors;
            obj.AggregateResults = ret.AggregateResults;

            if (fData != null) fData(obj);

            return JsonEx(obj, behavior);
        }
        #endregion

        #region Json

        protected internal new ActionResult Json(object data, JsonRequestBehavior behavior = JsonRequestBehavior.DenyGet)
        {
            if (data is ExpandoObject)
            {
                var eobj = data as ExpandoObject;
                var jsondata = eobj.ToJson();
                return Content(jsondata, "application/json");
            }

            if (data is IEnumerable<ExpandoObject>)
            {
                var lst = data as IEnumerable<ExpandoObject>;

                return Content(lst.ToJson(), "application/json");
            }

            return base.Json(data, behavior);
        }

        #endregion

        #region JsonEx

        protected internal ActionResult JsonEx(object data, JsonRequestBehavior behavior = JsonRequestBehavior.DenyGet)
        {
            var serializer = new JavaScriptSerializer();
            serializer.RegisterConverters(new JavaScriptConverter[] { new ExpandoJsonConverter() });

            var json = serializer.Serialize(data);

            return Content(json, "application/json");
        }

        #endregion

        #region LogErr
        protected static void LogErr(Exception err, string msg = null)
        {
            try
            {
                if (Debugger.IsAttached) Debugger.Break();
                LogHelper.Log(err, msg ?? err.Message);
            } 
            catch (Exception ex) { }
        }
        #endregion

        #region Stop

        public ActionResult Stop()
        {
            return ClawView("Admin/Stop.cshtml");
        }

        #endregion

        #region StartJob

        public static void StartJob<TJob>(int minutes, string name = null, string settings = null)
            where TJob : IJob
        {
            if (ModuleMgr.IsEmbedMode) return;

            var type = typeof(TJob);
            IoC.Kernel.Get<Claw.Core.QuartzMVC.Scheduler.IQuartzScheduler>()
                .LoadCurrentJob(name ?? type.FullName, Assembly.GetCallingAssembly().GetName().Name, type.FullName,
                    minutes, settings ?? "");
        }

        #endregion

        #region CompileViews

        private static void CompileViews(Module module, Site site)
        {
            if (module == null || site == null) return;

            var appPath = Globals.MapPath("~/");
            var folders = new[] { site.SiteFolder, "Default" }
                .Select(x => Globals.MapPath(string.Format("~/Claw/Sites/{0}/Modules/{1}/", x, module.ModuleFolder)))
                .ToList();

            Task.Run(() =>
            {
                try
                {
                    var files = folders.Where(Directory.Exists)
                        .SelectMany(x => Directory.GetFiles(x, "*.cshtml", SearchOption.AllDirectories))
                        //.Where(x => x.IndexOf(@"\admin\", StringComparison.OrdinalIgnoreCase) != -1)
                        .Select(x => "~" + x.Replace(appPath, "/").Replace("\\", "/"))
                        .OrderByDescending(x => x.IndexOf("/admin/", StringComparison.OrdinalIgnoreCase))
                        .ToList();

                    foreach (var file in files)
                    {
                        try
                        {
                            //var type = System.Web.Compilation.BuildManager.GetCompiledType(file);
                        }
                        catch (Exception e)
                        {
                            LogErr(e, file);
                        }
                    }
                }
                catch (Exception err)
                {
                    LogErr(err);
                }
            });
        }

        #endregion

        #region RedirectToAction

        protected new RedirectToRouteResult RedirectToAction(string actionName, object routeValues)
        {
            var controller = (string)HttpContext.Request.RequestContext.RouteData.Values["controller"];
            return RedirectToAction(actionName, controller, routeValues);
        }

        protected new RedirectToRouteResult RedirectToAction(string actionName, string controller, object routeValues)
        {
            if (routeValues is string && Debugger.IsAttached) Debugger.Break();

            var routeVals = QParam.Merge(routeValues);
            var reController = routeVals["recontroller"];

            if (string.IsNullOrEmpty(actionName)) actionName = "index";
            if (reController != null) reController = reController.ToString();

            if (actionName.EqualsIgCase("index"))
            {
                var reid = routeVals["reid"];
                if (reid != null) reid = reid.ToString();
                if (!string.IsNullOrEmpty((string)reid))
                {
                    routeVals["id"] = reid; routeVals.Remove("reid");
                }
                routeVals.Remove("searchform");
                if (!string.IsNullOrEmpty((string)reController))
                {
                    routeVals.Remove("recontroller");
                    return base.RedirectToAction(actionName, (string)reController, routeVals);
                }
            }
            else if (actionName.EqualsIgCase("Input"))
            {
                var refRoute = new RouteInfo(Request.UrlReferrer);
                if ("Index".EqualsIgCase(refRoute["action"])) routeVals["recontroller"] = refRoute["controller"];
            }

            routeVals["s302"] = "~";

            return base.RedirectToAction(actionName, controller, routeVals);
        }

        #endregion

        #region OnActionExecuting

        protected override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            base.OnActionExecuting(filterContext);

            if (!Request.HttpMethod.EqualsIgCase("GET")) return;

            const StringComparison cmpOpt = StringComparison.OrdinalIgnoreCase;
            string langCode = null;
            var action = filterContext.ActionDescriptor.ActionName;
            var controller = filterContext.ActionDescriptor.ControllerDescriptor.ControllerName;
            var indexAct = action.EqualsIgCase("Index");
            object mode, lang;
            var args = filterContext.ActionParameters;
            var redirRoute = null as RouteValueDictionary;

            args.TryGetValue("mode", out mode);
            args.TryGetValue("lang", out lang);

            #region NilArgs

            var nilMode = mode == null || mode.ToString() == "";
            var nilLang = lang == null || (langCode = lang.ToString()) == "";

            if (nilMode)
            {
                mode = Request["mode"];
                nilMode = mode == null || mode.ToString() == "";
            }

            if (nilLang)
            {
                lang = Request["lang"];
                nilLang = lang == null || (langCode = lang.ToString()) == "";
            }

            #endregion

            #region Append[Lang,Mode]

            if (nilMode || nilLang)
            {
                const RegexOptions regexOpts = RegexOptions.Compiled | RegexOptions.IgnoreCase;
                var rargs = filterContext.RouteData.Values;
                var refer = HttpContext.Request.UrlReferrer;
                var focusLang = LanguageMgr.FocusLangCode;
                var qargs = (refer == null ? null : refer.Query.Split(new[] { '&', '?' }, StringSplitOptions.RemoveEmptyEntries));
                var qmode = (qargs == null || !nilMode ? null : qargs.FirstOrDefault(x => x.Length > 5 && x.StartsWith("mode=", cmpOpt)));
                var qlang = (qargs == null || !nilLang ? null : qargs.FirstOrDefault(x => x.Length > 5 && x.StartsWith("lang=", cmpOpt)));
                if (qmode != null) args["mode"] = rargs["mode"] = qmode = qmode.Substring(5);
                if (qlang != null) args["lang"] = rargs["lang"] = qlang = langCode = qlang.Substring(5);
                if (Request.Url != null && indexAct && ((qmode != null && Regex.IsMatch(qmode, @"^[a-z]+$", regexOpts)) ||
                     (qlang != null && !qlang.EqualsIgCase(focusLang) && Regex.IsMatch(qlang, @"^[a-z\-]+$", regexOpts))))
                {
                    redirRoute = Request.Url.ToDictionary(args);
                }
            }

            if (indexAct && !string.IsNullOrEmpty(langCode) && LanguageMgr.Get(langCode) == null)
            {
                if (redirRoute == null) redirRoute = Request.Url.ToDictionary(args);
                redirRoute["lang"] = ClawContext.Current.GetDefaultOrCurrentLangCode();
            }

            #endregion

            #region ResetParams

            if ((indexAct || (action.Length > 4 && action.StartsWith("List", cmpOpt))) && Request.UrlReferrer != null)
            {
                var refRoute = new RouteInfo(Request.UrlReferrer);
                var refController = refRoute["controller"];
                var sameControler = controller.EqualsIgCase(refController);
                if (!refController.EqualsIgCase("dashboard") && "Index".EqualsIgCase(refRoute["action"]) && (sameControler ^ indexAct))
                {
                    if (redirRoute == null)
                    {
                        var route = Request.Url.ToDictionary(args);
                        if (!route.ContainsKey("s302") && (indexAct || route.ContainsKey("grid")) &&
                            QParam.Reset(route)) redirRoute = route;
                    }
                    else if (!redirRoute.ContainsKey("s302")) QParam.Reset(redirRoute);
                }
            }

            #endregion

            if (redirRoute != null)
            {
                redirRoute["s302"] = "~"; redirRoute.Remove("searchform");
                filterContext.Result = new RedirectToRouteResult(redirRoute);
            }
            else { args.Remove("s302"); filterContext.RouteData.Values.Remove("s302"); }
        }

        #endregion

        #region Add
        [ValidateInput(false)]
        public ActionResult Add(string lang, string optlang, string mode)
        {
            return RedirectToAction("Input", new { id = 0, lang = lang, optlang = optlang, mode = mode });
        }
        #endregion
        #region Edit
        [ValidateInput(false)]
        public ActionResult Edit(int id, string lang, string optlang, string mode)
        {
            return RedirectToAction("Input", new { id = id, lang = lang, optlang = optlang, mode = mode });
        }
        #endregion
        #region MsTranslatorToken

        public ActionResult MsTranslatorToken()
        {
            try
            {
                var token = MsTranslator.GetAccessToken();

                return Content(string.Format(@"
                    window.msTranslatorToken = '{0}';  
                    window.msTranslatorExpiry = parseInt('{1}');
                ", token.access_token, token.expires_in), "application/x-javascript");
            }
            catch (Exception e)
            {
                return Content(string.Format("admincommon.showMessageDefault('{0}','{1}');", e.Message, ActionResultStatus.Fail),
                    "application/x-javascript");
            }
        }

        #endregion
        #region AsyncHtmlCache
        public ActionResult AsyncHtmlCache(string url, string baseUrl, int seconds = 15*60, bool userAgent = true)
        {
            seconds = Math.Min(12 * 3600, Math.Max(seconds, 15));

            var html = url.IsNullOrEmpty()
                ? null
                : Helper.AsyncDataCache<string>.Get(
                string.Format("Claw.Modules.Eshop.AsyncHtmlCache:[{0}]:::[{1}]", userAgent ? Request.UserAgent : "", url), () =>
                    {
                        int retries = 3;
                    LB_LOOP:
                        try
                        {
                            var wReq = (HttpWebRequest)WebRequest.Create(url);
                            wReq.UserAgent = Request.UserAgent;
                            using (var wResp = wReq.GetResponse())
                            {
                                using (var rs = wResp.GetResponseStream())
                                {
                                    if (rs == null) return null;

                                    var sr = new StreamReader(rs, Encoding.UTF8);
                                    var data = sr.ReadToEnd();
                                    sr.Close();

                                    if (baseUrl.IsNullOrEmpty()) return data;

                                    var idx = data.IndexOf("<head>", StringComparison.OrdinalIgnoreCase);

                                    if (idx == -1) return data;

                                    idx += 6;

                                    data = data.Substring(0, idx) + "<base href='" + baseUrl + "'>" + data.Substring(idx);

                                    return data;
                                }
                            }
                        }
                        catch (Exception err)
                        {
                            if (--retries >= 0)
                            {
                                Thread.Sleep(1000);
                                goto LB_LOOP;
                            }
                            else return err.Message;
                        }
                    }, seconds);

            if (html != null && html.Length > 500)
            {
                Response.CacheControl = HttpCacheability.Public.ToString();
                Response.Expires = seconds / 120;
            }

            return Content(html ?? string.Empty, "text/html");
        }

        #endregion
    }

    #endregion

    #region BaseController<TMod, TDom>

    public class BaseController<TMod, TDom> : BaseController
        where TMod : class
        where TDom : class
    {
        #region GridId
        public class GridId
        {
            public int Id { get; set; }
            public string Grid { get; set; }
        }
        public class GridId2
        {
            public long Id { get; set; }
            public string Grid { get; set; }
        }
        #endregion

        #region BaseController

        public BaseController(IDataService dataService)
            : base(dataService)
        {

        }

        #endregion

        #region CopyValidate

        protected TMod CopyValidate(TMod oldVal, TMod newVal, Action<TMod, TMod> fCopy)
        {
            ModelState.Clear();

            fCopy(newVal, oldVal);

            TryValidateModel(newVal);

            return newVal;
        }

        #endregion

        #region Enable

        [HttpPost]
        [AdminPerm]
        public virtual ActionResult Enable(int id, bool active)
        {
            bool ret = BaseMgr<TMod, TDom>.Enable(this, id, active);

            return JsonStatus(StringTable.UpdateStatus, ret);
        }

        #endregion

        #region Delete

        [HttpPost]
        [AdminPerm]
        public virtual ActionResult Delete(GridId[] ids)
        {
            var retCode = ids != null && ids.Length > 0 &&
                          BaseMgr<TMod, TDom>.Delete(this, null, null, ids.Select(x => x.Id).ToArray());

            if (!IndexAction) SetViewMsg(retCode, StringTable.Deleted);

            return JsonStatus(StringTable.Deleted, retCode);
        }

        #endregion

        #region ChangeDOrder

        [HttpPost]
        [AdminPerm]
        public ActionResult ChangeDOrder(int id, int value)
        {
            var obj = BaseMgr<TMod, TDom>.Get(this, id);
            if (obj is Domain.IDisplayOrder)
            {
                var iDorder = obj as Domain.IDisplayOrder; iDorder.DisplayOrder = value;
                if (TryValidate(iDorder, m => m.DisplayOrder) &&
                    BaseMgr<TMod, TDom>.SaveOrUpdate(this, id, d =>
                    {
                        ((Domain.IDisplayOrder)d).DisplayOrder = value;
                    }))
                {
                    return JsonStatus(StringTable.UpdateDOrder, true);
                }
            }

            return JsonStatus(StringTable.UpdateDOrder, false);
        }

        #endregion

        #region ValidateAlias

        public virtual ActionResult ValidateAlias(string alias, int? id)
        {
            return ValidateField("Alias", alias, id);
        }

        #endregion

        #region ValidateLangAlias

        public virtual ActionResult ValidateLangAlias()
        {
            return ValidateLangField("Alias");
        }

        #endregion

        #region ValidateField

        protected ActionResult ValidateField(string name, object value, int? id)
        {
            var retCode = true;

            if (id == null) goto LB_END;

            var type = typeof(TMod);
            var propVal = type.GetProperty(name);
            var propId = type.GetProperty("Id");

            if (propVal == null || propId == null) goto LB_END;

            var obj = Activator.CreateInstance<TMod>();

            propId.SetValue(obj, id);
            propVal.SetValue(obj, value);

            var attr = propVal.GetCustomAttributes(typeof(RemoteAttribute), false).Cast<RemoteAttribute>().FirstOrDefault();

            if (attr == null) goto LB_END;

            if (!string.IsNullOrEmpty(attr.AdditionalFields))
                foreach (var field in attr.AdditionalFields.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    if (field == "Id") continue;
                    var prop = type.GetProperty(field);
                    prop.SetValue(obj, GetParam(prop.PropertyType, field));
                }

            retCode = (null == type.GetMethod("Validate").Invoke(null, new[] { obj, null }));

        LB_END:
            return Json(retCode, JsonRequestBehavior.AllowGet);
        }

        #endregion

        #region ValidateName

        public virtual ActionResult ValidateName()
        {
            return ValidateLangField("Name");
        }

        #endregion

        #region ValidateLangField

        protected ActionResult ValidateLangField(string name)
        {
            var retCode = true;
            var type = typeof(TMod);
            var funcGetLangType = type.GetMethod("GetLangType",
                BindingFlags.FlattenHierarchy | BindingFlags.Static | BindingFlags.Public);

            if (funcGetLangType == null) goto LB_END;

            var langType = (Type)funcGetLangType.Invoke(null, null);
            var propName = langType.GetProperty(name);

            if (propName == null) goto LB_END;

            var attr = propName.GetCustomAttributes(typeof(RemoteAttribute), false).Cast<RemoteAttribute>().FirstOrDefault();

            if (attr == null) goto LB_END;

            var obj = Activator.CreateInstance(langType);

            propName.SetValue(obj, GetPatternParam<string>("Languages[", "]." + name));

            if (!string.IsNullOrEmpty(attr.AdditionalFields))
                foreach (var field in attr.AdditionalFields.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    var prop = langType.GetProperty(field);
                    prop.SetValue(obj, GetPatternParam(prop.PropertyType, "Languages[", "]." + field));
                }

            retCode = (null == langType.GetMethod("Validate").Invoke(null, new[] { obj, null }));

        LB_END:
            return Json(retCode, JsonRequestBehavior.AllowGet);
        }

        #endregion

        #region GetUniqueFieldHint

        public virtual ActionResult GetUniqueFieldHint(string name, string value, int? id)
        {
            var retVal = BaseMgr.GetUniqueFieldHint<TDom, TDom>(this, name, value, id);

            return Json(new { Value = retVal ?? "" }, JsonRequestBehavior.AllowGet);
        }

        #endregion

    }

    #endregion

    #region EagerLoadHttpModule

    public sealed class EagerLoadHttpModule : IHttpModule
    {
        private static volatile bool _initialized = false;
        public static volatile string SiteUrl;

        public static void Start()
        {
            DynamicModuleUtility.RegisterModule(typeof(EagerLoadHttpModule));
        }

        void IHttpModule.Init(HttpApplication context)
        {
            context.EndRequest += EndRequest;
        }

        private void EndRequest(object sender, EventArgs args)
        {
            if (!_initialized && HttpContext.Current.Response.StatusCode == 200)
            {
                _initialized = true;
                SiteUrl = HttpContext.Current.Request.Url.Scheme + "://" + HttpContext.Current.Request.Url.Authority;
                if (!ModuleMgr.IsEmbedMode)
                    BaseController.Initialize(new SeparatedDb());
            }
        }

        void IHttpModule.Dispose()
        {
        }
    }

    #endregion

    #region AuthorizeController
    public class AuthorizeController : BaseController
    {
        public AuthorizeController(IDataService ds) : base(ds) { }

        public ActionResult Index()
        {
            ClawContext.Current.Context.Response.Redirect(SiteUrls.Instance.ErrorUrl(CommonError.AccessDenied));
            return Content(StringTable.NoAuthorizeMsg);
        }
    }
    #endregion

    #region AdminPermAttribute
    public class AdminPermAttribute : Controls.Attributes.ClawAuthorizeAttribute
    {
        protected PropertyInfo Selector;
        protected byte Mask;

        public AdminPermAttribute(string selector, PermissionMask mask)
            : this(selector, (byte)mask)
        {
        }

        public AdminPermAttribute(string selector = null, byte mask = 0)
            : base(AdminPermissionType.Eshop, null)
        {
            Selector = selector.IsNullOrEmpty() ? null : typeof(PermissionModel).GetProperty(selector);
            Mask = mask;
        }

        protected override bool AuthorizeCore(HttpContextBase httpContext)
        {
            if (base.AuthorizeCore(httpContext))
            {
                if (BaseMgr.CurrentUser.IsAdmin() ||
                    (Selector != null && 0 != ((byte)Selector.GetValue(PermissionMgr.CurrentPerm) & Mask))) return true;

                httpContext.Response.Redirect(ModuleUrl.AdminUrl("Authorize", "Index"));
            }
            return false;
        }
    }
    #endregion
}

#region EagerLoad

namespace Claw.Modules.Eshop.Controllers.FrontEnd
{
    public class EagerLoadController : Admin.BaseController
    {
        #region EagerLoadController

        public EagerLoadController(IDataService ds)
            : base(ds)
        {
        }

        #endregion

        #region Job

        public ActionResult Job()
        {
            try
            {
                var startTime = DateTime.Now;
                var siteid = BaseMgr.GetSiteId(this);
                var languages = LanguageMgr.GetAll();
                var phrases = DataService.GetAll<Claw.Core.Domain.Phrase>().Where(x => x.Site == null || x.Site.Id == siteid);
                var adminLang = ClawContext.Current.AdminLanguage;

                if (adminLang != null &&
                    languages.All(
                        x => !x.LanguageCode.Equals(adminLang.LanguageCode, StringComparison.OrdinalIgnoreCase)))
                {
                    languages.Add(adminLang);
                }

                var phraseLangDic = DataService.GetAll<Claw.Core.Domain.PhraseLanguage>()
                    .Where(m => m.Phrase != null).GroupBy(m => m.Phrase.Id)
                    .ToDictionary(m => m.Key, m => m.ToList());

                foreach (var itm in phrases)
                {
                    var phraseLangs = phraseLangDic.SafeGet(itm.Id) ?? new List<Claw.Core.Domain.PhraseLanguage>();
                    foreach (var lang in languages)
                    {
                        var lst = new List<string> { itm.PhraseName + ", " + itm.PhraseType };
                        if (itm.PhraseType.EqualsIgCase("global")) lst.Add(itm.PhraseName);
                        foreach (var phrase in lst)
                        {
                            Localize(itm, phraseLangs, phrase,
                                phraseLangs.Select(x => x.PhraseValue).FirstOrDefault() ?? "",
                                new StringLocalizeExtensions.Setting()
                                {
                                    LangCode = lang.LanguageCode,
                                    ForceAdminLang = false
                                }, DataService as DataService, siteid);
                        }
                    }
                }

                var duration = (DateTime.Now - startTime).TotalSeconds;
            }
            catch (Exception err)
            {
                LogErr(err);
            }

            return Content("OK");
        }

        #endregion

        #region Localize

        public static string Localize(Phrase dbPhrase, List<PhraseLanguage> phraseLangs, string phrase, string defaultText, StringLocalizeExtensions.Setting setting, DataService dataService = null, int? siteid = null)
        {
            if (string.IsNullOrEmpty(phrase)) return phrase;

            //setting = setting ?? GetCurrentLangCode(phrase);

            try
            {
                return Claw.Core.DataCache<Claw.Core.PhraseScope, string>.Get(GetKeyCache(phrase, setting.LangCode),
                    () => LocalizeP(dbPhrase, phraseLangs, phrase, defaultText, setting.LangCode, setting.ForceAdminLang ?? true, dataService, siteid),
                    365 * 24 * 60 * 60, 20);
            }
            catch (Exception exp)
            {
                LogHelper.Log(exp, "Phrase name: " + phrase);
            }

            return phrase;
        }
        
        #endregion

        #region LocalizeP
        /// <summary>
        /// get localize the specified phrase.
        /// </summary>
        /// <param name="phrase">The phrase.</param>
        /// <param name="defaultText">The default text. This text will be inserted to database if no localize text found.</param>
        /// <returns> the specified localize phrase</returns>
        private static string LocalizeP(Phrase dbPhrase, List<PhraseLanguage> phraseLangs, string phrase, string defaultText = "", string forcedLang = null,
            bool forceAdminLang = true, DataService dataService = null, int? siteid = null)
        {
            var ctx = ClawContext.Current;
            var langCode = forcedLang ?? ctx.CurrentLanguage.LanguageCode;
            var mode = (forceAdminLang && ctx.RawUrl.ToLower().IndexOf("/admin/") >= 0) ? "admin" : "frontend";
            var key = string.Format(ClawCache.PhraseCacheFormat, Globals.GenerateAlias(phrase), langCode, mode);

            key = key.ToLower();
            var value = ClawCache.Get<string>(key);
            var autoInsertLocalizedText = Claw.Core.Configurations.ClawConfiguration.GetConfig().AutoInsertLocalizedText;

#if DEBUG
            if (value != null && !autoInsertLocalizedText) return value;
#else
            if (value != null) return value;
#endif

            #region phrase does not exist in cache

            var arr = phrase.Split(new [] { ",", ";" }, StringSplitOptions.RemoveEmptyEntries);

            if (arr != null)
            {
                var ds = dataService ?? ctx.DataService;

                var phraseName = arr[0].Trim();
                var phraseType = "global";
                var siteId = siteid ?? ctx.CurrentSite.Id;

                if (arr.Length >= 2) phraseType = arr[1].Trim();

                langCode = arr.Length == 3 ? arr[2].Trim() : langCode;

                // Get custom phrase for a site
                //var dbPhrase = ds.GetPhrase(phraseName, phraseType, siteId);
                //dbPhrase = dbPhrase ?? ds.GetPhrase(phraseName, phraseType);

                if (dbPhrase == null && !string.IsNullOrEmpty(defaultText))
                {
                    #region check auto insert

                    if (autoInsertLocalizedText)
                    {
                        dbPhrase = new Phrase() { PhraseName = phraseName, PhraseType = phraseType, };

                        var detail = new PhraseLanguage()
                        {
                            PhraseValue = Globals.HtmlEncode(defaultText),
                            LanguageCode = langCode, Phrase = dbPhrase
                        };

                        dbPhrase.Phrasecms_PhraseLanguageses.Add(detail);
                        ds.Save(dbPhrase);
                    }

                    #endregion

                    //ClawCache.Insert(key, defaultText);
                    return defaultText;
                }

                if (dbPhrase != null)
                {
                    var decodeValue = "";

                    #region check if current request is an admin request to force using admin language

                    if (forceAdminLang && ctx.RawUrl.IndexOf("/admin/", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        var adminLangCode = ctx.AdminLanguage.LanguageCode;
                        decodeValue = dbPhrase.PhraseValueByLang(adminLangCode) ?? defaultText;

                        //ClawCache.Insert(key, Globals.HtmlDecode(decodeValue));

                        return Globals.HtmlDecode(decodeValue);
                    }

                    #endregion

                    if (!string.IsNullOrEmpty(langCode)) {
                        //decodeValue = dbPhrase.PhraseValueByLang(langCode) ?? defaultText;
                        decodeValue = phraseLangs.FirstOrDefault(m => !m.PhraseValue.IsNullOrEmpty() && m.LanguageCode.EqualsIgCase(langCode)).SafeProp(m => m.PhraseValue);
                        decodeValue = decodeValue ?? (dbPhrase.PhraseValue ?? defaultText);
                    }
                    else decodeValue = dbPhrase.PhraseValue ?? defaultText;

                    //ClawCache.Insert(key, Globals.HtmlDecode(decodeValue));

                    return Globals.HtmlDecode(decodeValue);
                }
            }

            //ClawCache.Insert(key, phrase);
            return phrase;

            #endregion
        }
        #endregion

        #region GetKeyCache

        private static string GetKeyCache(string phrase, string langCode)
        {
            if (phrase != null && phrase.IndexOf(' ') != -1)
            {
                phrase = phrase.Replace(", ", ",").Replace(" ,", ",");
                phrase = phrase.Replace("; ", ";").Replace(" ;", ";");
            }

            return string.Format("[{0}][{1}]", langCode, (phrase ?? "").Trim()).ToLower();
        }

        #endregion
    }
}

#endregion
