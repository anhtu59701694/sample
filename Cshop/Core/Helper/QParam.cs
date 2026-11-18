using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Web;
using System.Web.Routing;
using System.Text;
using System.Globalization;

namespace Claw.Modules.Eshop.Helper
{
    public class QParam
    {
        #region Fields

        private static List<string> TrivialArgs = new List<string>()
        {
            "categoryid", "status", "prodtype", "manufacturerid", "visibilityweb", "visibilitypos", "name", "sku", "propertyid",
            "refid", "productname", "fromdate", "todate", "paymentmethodid", "shippingid","backorder","backproductid",
            "groupid", "customerid", "emailmarketing", "productid", "roleid","mo","RecurOrderId"
        };

        public object Id;
        public string Mode;
        public string Lang;
        public string OptLang;
        public int? PageIdx;
        public int? PageSiz;
        public string GridSort;
        public string Keyword;
        public object ReId;
        public string ReController;

        RouteValueDictionary _targs = new RouteValueDictionary();

        #endregion

        #region QParam

        public QParam()
        {
            var curCxt = HttpContext.Current;
            if (curCxt == null) return;

            var action = curCxt.GetRouteValue("action");
            if (action != null && action.ToString().EqualsIgCase("index"))
            {
                if (null == (ReId = curCxt.GetRouteValue("id")))
                {
                    var sId = curCxt.Request.QueryString["id"];
                    if (!string.IsNullOrEmpty(sId)) ReId = sId;
                }
            }
            else
            {
                var reId = curCxt.Request.QueryString["reid"];
                if (!string.IsNullOrEmpty(reId)) ReId = reId;

                OptLang = curCxt.Request.QueryString["optlang"];

                PageIdx = GetPageIdx();
                PageSiz = GetPageSize();
                GridSort = GetGridSort();
                Keyword = GetKWord();
            }

            Mode = curCxt.Request.QueryString["mode"];
            if (string.IsNullOrEmpty(Mode))
                Mode = (curCxt.GetRouteValue("mode") ?? "").ToString();

            Lang = curCxt.Request.QueryString["lang"];
            if (string.IsNullOrEmpty(Lang))
                Lang = (curCxt.GetRouteValue("lang") ?? "").ToString();

            ReController = curCxt.Request.QueryString["recontroller"];
            if (!string.IsNullOrEmpty(ReController) &&
                ReController.EqualsIgCase((curCxt.GetRouteValue("controller") ?? "").ToString()))
            {
                ReController = null;
            }

            foreach (var akey in TrivialArgs)
            {
                _targs[akey] = curCxt.Request.QueryString[akey].UrlDecode();
            }
        }

        #endregion

        #region Properties

        public string CategoryId
        {
            get { return (string)_targs["categoryid"]; }
            set { _targs["categoryid"] = value; }
        }

        public string Status
        {
            get { return (string)_targs["status"]; }
            set { _targs["status"] = value; }
        }

        public string Type
        {
            get { return (string)_targs["type"]; }
            set { _targs["type"] = value; }
        }

        public string ManufacturerId
        {
            get { return (string)_targs["manufacturerid"]; }
            set { _targs["manufacturerid"] = value; }
        }

        public string Visibility
        {
            get { return (string)_targs["visibility"]; }
            set { _targs["visibility"] = value; }
        }

        public string Name
        {
            get { return (string)_targs["name"]; }
            set { _targs["name"] = value; }
        }
        
        public string Sku
        {
            get { return (string)_targs["sku"]; }
            set { _targs["sku"] = value; }
        }

        public string PropertyId
        {
            get { return (string)_targs["propertyid"]; }
            set { _targs["propertyid"] = value; }
        }

        #endregion

        #region Targs

        public RouteValueDictionary Targs
        {
            get { return _targs; }
        }

        #endregion

        #region IsFontEnd
        public static bool IsFontEnd
        {
            get
            {
                if (HttpContext.Current != null)
                {
                    return HttpContext.Current.Request["mode"].EqualsIgCase("frontend") || IsPgComponent;
                }
                else return false;
            }
        }
        #endregion

        #region IsInputForm
        public static bool IsInputForm
        {
            get
            {
                if (HttpContext.Current != null)
                {
                    var action = HttpContext.Current.GetRouteValue("action").ToStringSafe();
                    return action.EqualsIgCase("Input");
                }
                else return false;
            }
        }
        #endregion

        #region IsPgComponent
        public static bool IsPgComponent
        {
            get
            {
                if (HttpContext.Current != null)
                {
                    var fePg = HttpContext.Current.GetRouteValue("FrontEndPage");
                    return fePg != null && !string.IsNullOrEmpty(fePg as string);
                }
                else return false;
            }
        }
        #endregion

        #region GetReController
        public static string GetReController()
        {
            var curCxt = HttpContext.Current;
            if (curCxt != null)
            {
                var controller = curCxt.Request["recontroller"]; ;
                if (!string.IsNullOrEmpty(controller)) return controller;
            }

            return null;
        }
        #endregion

        #region GetId
        public static int? GetId()
        {
            var curCxt = HttpContext.Current;
            if (curCxt != null)
            {
                int oVal;
                var id = curCxt.GetRouteValue("id");
                if (id != null)
                {
                    if (id is int) return (int?)id;
                    if (int.TryParse(id.ToString(), out oVal)) return oVal;
                }
                else
                {
                    var sId = curCxt.Request.QueryString["id"];
                    if (!string.IsNullOrEmpty(sId) && int.TryParse(sId, out oVal)) return oVal;
                }
            }

            return null;
        }
        #endregion

        #region GetPageIdx
        public static int? GetPageIdx()
        {
            var curCxt = HttpContext.Current;
            if (curCxt != null)
            {
                int page;
                var sPage = curCxt.Request.QueryString["pageidx"];
                if (string.IsNullOrEmpty(sPage))
                    sPage = (curCxt.GetRouteValue("pageidx") ?? "").ToString();
                if (!string.IsNullOrEmpty(sPage) && int.TryParse(sPage, out page))
                    return page > 1 ? page : (int?)null;
            }

            return null;
        }
        #endregion

        #region GetPageSize
        public static int? GetPageSize()
        {
            var curCxt = HttpContext.Current;
            if (curCxt != null)
            {
                int pageSz;
                var sPageSz = curCxt.Request.QueryString["pagesiz"];
                if (string.IsNullOrEmpty(sPageSz))
                    sPageSz = (curCxt.GetRouteValue("pagesiz") ?? "").ToString();
                if (!string.IsNullOrEmpty(sPageSz) && int.TryParse(sPageSz, out pageSz))
                    return pageSz > 1 ? pageSz : (int?)null;
            }

            return null;
        }
        #endregion

        #region GetKWord

        public static string GetKWord()
        {
            var curCxt = HttpContext.Current;
            if (curCxt != null)
            {
                var keyword = curCxt.Request.QueryString["keyword"].UrlDecode();
                if (string.IsNullOrEmpty(keyword))
                    keyword = (curCxt.GetRouteValue("keyword") ?? "").ToString();

                return keyword;
            }

            return string.Empty;
        }

        #endregion

        #region GetGridSort

        public static string GetGridSort()
        {
            var curCxt = HttpContext.Current;
            if (curCxt != null)
            {
                var gridSort = curCxt.Request.QueryString["grid-sort"];
                if (string.IsNullOrEmpty(gridSort))
                    gridSort = (curCxt.GetRouteValue("grid-sort") ?? "").ToString();

                if (gridSort.IsNullOrEmpty() && curCxt.Request.QueryString.GetValues("grid-sort").SafeCount() != 0)
                    gridSort = "none";
                
                return gridSort;
            }

            return string.Empty;
        }

        public static void GetGridSort<TModel>(out string field, out ListSortDirection dir)
        {
            var gridSort = GetGridSort();
            if (!string.IsNullOrEmpty(gridSort) && gridSort.Length > 4 && gridSort.IndexOf('-') != -1)
            {
                var type = typeof (TModel);
                var idx = gridSort.IndexOf('-');
                field = gridSort.Substring(0, idx);
                if (gridSort.IndexOf("-asc", idx) != -1) dir = ListSortDirection.Ascending;
                else dir = ListSortDirection.Descending;
                var flags = BindingFlags.IgnoreCase | BindingFlags.FlattenHierarchy | BindingFlags.Public | BindingFlags.Instance;
                var prop = type.GetProperty(field, flags);
                if (prop == null) field = null;
                else field = prop.Name;
            }
            else
            {
                field = null;
                dir = ListSortDirection.Descending;
            }
        }

        #endregion

        #region GetField
        public static T GetField<T>(string name, bool formOnly = true, bool queryStringOnly = false)
        {
            var curCxt = HttpContext.Current;
            if (curCxt != null)
            {
                var val = (formOnly ? curCxt.Request.Form[name] : (queryStringOnly ? curCxt.Request.QueryString[name] : curCxt.Request[name]));
                if (typeof(T) == typeof(OMoney))
                {
                    decimal oVal; var fInfo = OMoney.FormatInfo(GetField<int>("Id"));
                    if (decimal.TryParse(val, NumberStyles.Number, fInfo, out oVal))
                        return (T)(object)new OMoney(oVal);
                }
                else if (typeof(T) == typeof(ODateTime))
                {
                    DateTime oVal;
                    var format = ODateTime.FormatString(GetField<int>("Id"));
                    if (DateTime.TryParseExact(val, format, null, DateTimeStyles.None, out oVal))
                        return (T)(object)new ODateTime(oVal);
                }
                return (T)val.ChangeType(typeof(T));
            }

            return default(T);
        }
        #endregion

        #region Args
        public static string Args(bool skipOptLang = false, bool skipController = true, bool skipTrivialArgs = false)
        {
            if (HttpContext.Current != null)
            {
                return new QParam().ToString(skipOptLang, skipController, skipTrivialArgs);
            }
            else return string.Empty;
        }
        #endregion

        #region Merge
        public static RouteValueDictionary Merge(object routeValues)
        {
            var ret = new RouteValueDictionary(routeValues);

            if (HttpContext.Current != null)
            {
                var args = new QParam();
                Type type = typeof(QParam);
                foreach (var f in type.GetFields(BindingFlags.Instance | BindingFlags.Public))
                {
                    var key = f.Name.ToLower();
                    if (key == "gridsort")
                    {
                        if (!ret.ContainsKey(key = "grid-sort")) ret.Add(key, args.GridSort);
                    }
                    else if (!ret.ContainsKey(key)) ret.Add(key, f.GetValue(args));
                }

                foreach (var targ in args.Targs)
                {
                    if (!string.IsNullOrEmpty((string)targ.Value) && !ret.ContainsKey(targ.Key))
                        ret.Add(targ.Key, targ.Value);
                }
            }

            return ret;
        }
        #endregion

        #region Reset

        public static bool Reset(RouteValueDictionary route)
        {
            foreach (var itm in route.Where(x => x.Value == null || x.Value == (object)string.Empty).ToArray())
            {
                route.Remove(itm.Key);
            }

            int old = route.Count;

            route.Remove("page");
            route.Remove("keyword");
            route.Remove("grid-sort");

            foreach (var key in TrivialArgs)
            {
                route.Remove(key);
            }

            var ret = route.Count < old;

            route.Remove("optlang");
            route.Remove("reid");
            route.Remove("recontroller");

            return ret;
        }

        #endregion

        #region AddTrivialArgs

        public static void AddTrivialArgs(params string[] args)
        {
            TrivialArgs = new List<string>(args.Where(x => !x.InList(TrivialArgs.ToArray())).Concat(TrivialArgs));
        }

        #endregion

        #region ToString

        public string ToString(bool skipOptLang, bool skipController, bool skipTrivialArgs = false)
        {
            var ret = new StringBuilder();

            if (!skipController && ReId != null)
            {
                ret.Append("?reid=");
                ret.Append(ReId);
            }

            if (!skipTrivialArgs && Id != null)
            {
                ret.Append(ret.Length == 0 ? "?id=" : "&id=");
                ret.Append(Id);
            }

            if (!string.IsNullOrEmpty(Mode))
            {
                ret.Append(ret.Length == 0 ? "?mode=" : "&mode=");
                ret.Append(Mode);
            }

            if (!string.IsNullOrEmpty(Lang))
            {
                ret.Append(ret.Length == 0 ? "?lang=" : "&lang=");
                ret.Append(Lang);
            }

            if (PageIdx != null)
            {
                ret.Append(ret.Length == 0 ? "?pageidx=" : "&pageidx=");
                ret.Append(PageIdx);
            }

            if (PageSiz != null)
            {
                ret.Append(ret.Length == 0 ? "?pagesiz=" : "&pagesiz=");
                ret.Append(PageSiz);
            }

            if (!string.IsNullOrEmpty(GridSort))
            {
                ret.Append(ret.Length == 0 ? "?grid-sort=" : "&grid-sort=");
                ret.Append(GridSort);
            }

            if (!string.IsNullOrEmpty(Keyword))
            {
                ret.Append(ret.Length == 0 ? "?keyword=" : "&keyword=");
                ret.Append(Keyword.UrlEncode());
            }

            if (!skipOptLang && !string.IsNullOrEmpty(OptLang))
            {
                ret.Append(ret.Length == 0 ? "?optlang=" : "&optlang=");
                ret.Append(OptLang);
            }

            if (!skipTrivialArgs)
            {
                foreach (var targ in _targs)
                {
                    if (targ.Value != null && !string.IsNullOrEmpty((string)targ.Value))
                    {
                        ret.AppendFormat("{0}{1}={2}", ret.Length == 0 ? '?' : '&', targ.Key, targ.Value.UrlEncode());
                    }
                }
            }

            if (!skipController && !string.IsNullOrEmpty(ReController))
            {
                ret.Append(ret.Length == 0 ? "?recontroller=" : "&recontroller=");
                ret.Append(ReController);
            }

            return ret.ToString();
        }

        #endregion
    }
}