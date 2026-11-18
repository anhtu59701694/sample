using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Claw.Core;
using Claw.Core.Configurations;
using Claw.Core.Domain;
using Claw.Core.Enumerations;
using Claw.Core.Interfaces;
using System.Web;
using System.Web.Caching;
using Claw.Core.Logs;
using Claw.Core.Sercurity.Permissions;
using Claw.Modules.Eshop.DAL;
using Claw.Modules.Eshop.DAL.Interfaces;

namespace Claw.Modules.Eshop.Helper
{
    #region CacheScope
    public abstract class CacheScope
    {
        public abstract DateTime LastChange { get; }  
        public static Func<string> GetCurrentUserName = null;
    } 
    #endregion

    public static class CacheHelper<TScope,TItm>  
        where TItm : class 
        where TScope : CacheScope
    {
        #region Cell
        public class Cell
        {
            public TItm Data { get; set; }
            public DateTime Timestamp { get; set; }
            public DateTime Expiry { get; set; }
        } 
        #endregion

        private static readonly TScope Scope = Activator.CreateInstance<TScope>();

        #region Remove

        public static void Remove(string key, bool lang = true, bool user = false, bool currency = true, bool shoppingCart = false)
        {
            key = GetCacheKey(key, lang, user, currency, shoppingCart);

            CacheClear(key);
        }

        #endregion

        #region Get

        public static TItm Get(string key, Func<TItm> fData, int seconds, bool lang, bool currency, int minSeconds,
            bool shoppingCart = false, bool price4LoginOnly = false, bool customerDiscount = false)
        {
            return Get(key, fData, seconds, lang, currency, false, minSeconds, shoppingCart, price4LoginOnly, customerDiscount);
        }

        public static TItm Get(string key, Func<TItm> fData, int seconds, bool lang = true, bool currency = true,
            bool user = false, int minSeconds = 1, bool shoppingCart = false, bool price4LoginOnly = false,
            bool customerDiscount = false)
        {
            return Get(key, fData, seconds, lang, currency, user, minSeconds, shoppingCart, price4LoginOnly, customerDiscount, false);
        }

        public static TItm Get(string key, Func<TItm> fData, bool forced, int seconds, bool lang = true, bool currency = true,
            bool user = false, int minSeconds = 1, bool shoppingCart = false, bool price4LoginOnly = false,
            bool customerDiscount = false)
        {
            return Get(key, fData, seconds, lang, currency, user, minSeconds, shoppingCart, price4LoginOnly, customerDiscount, forced);
        }

        public static TItm Get(string key, Func<TItm> fData, int seconds, bool lang, bool currency,
            bool user, int minSeconds, bool shoppingCart, bool price4LoginOnly, bool customerDiscount, bool forced)
        {
            if (!forced && !Cacheable) return fData();
            //seconds = 3600;
            key = GetCacheKey(key, lang, user, currency, shoppingCart, price4LoginOnly, customerDiscount);

            var obj = CacheGet(key);

            try
            {
                if (obj != null && obj.Timestamp >= Scope.LastChange && obj.Expiry >= DateTime.Now) return obj.Data;
                else
                {
                    obj = new Cell() {Data = fData(), Timestamp = DateTime.Now.AddSeconds(minSeconds)};
                    obj.Expiry = obj.Timestamp.AddSeconds(Math.Max(0, seconds - minSeconds));
                }
            }
            catch (Exception err)
            {
                LogHelper.Log4net(err, string.Format("{0}\r\n{1}", HttpContext.Current.Request.Url, key));
                throw err;
            }

            if (obj.Data != null) CacheSet(key, obj, seconds);

            return obj.Data;
        }

        public static TItm GetDataCache(string key, Func<TItm> fData, int seconds, bool lang = true, bool currency = true,
            bool user = false, int minSeconds = 1, bool shoppingCart = false)
        {
            key = GetCacheKey(key, lang, user, currency, shoppingCart);

            var obj = CacheGet(key);

            try
            {
                if (obj != null && obj.Timestamp >= Scope.LastChange) return obj.Data;
                else obj = new Cell() { Data = fData(), Timestamp = DateTime.Now.AddSeconds(minSeconds) };
            }
            catch (Exception err)
            {
                LogHelper.Log4net(err, string.Format("{0}\r\n{1}", HttpContext.Current.Request.Url, key));
                throw err;
            }

            if (obj.Data != null) CacheSet(key, obj, seconds);

            return obj.Data;
        }

        #endregion

        #region GetCacheKey

        public static string GetCacheKey(string key, bool lang = true, bool user = false, bool currency = true,
            bool shoppingCart = false, bool price4LoginOnly = false, bool customerDiscount = false)
        {
            var ctx = ClawContext.Current;
            var settings = SettingsBiz.GetSettings();
            var langCode = (ctx.CurrentLanguage != null ? ctx.CurrentLanguage.LanguageCode : null) ?? "";
            var username = (CacheScope.GetCurrentUserName != null ? CacheScope.GetCurrentUserName() : (HttpContext.Current != null && ctx.User != null ? ctx.User.Username : null)) ?? "";
            var cartSumQty = shoppingCart && settings.Hidden.ProdSelfDiscount ? new Domain.Tuple().ShoppingCart.CacheKey : "";
            var membershipKey = shoppingCart ? Singleton<IMembershipMgr>.Default.GetCurrentType() : 0;
            var showPrice = false;
            decimal? discountRate = null;
            Dictionary<int, decimal> cateDiscountRates = null;
            List<Domain.Interfaces.ICustGroup> groups = null;
            DateTime? modifiedDate = null;
            var price4Login = (price4LoginOnly || customerDiscount) && (Singleton<ISessionHelper>.Default.GetUserId(out showPrice, out  discountRate, out cateDiscountRates, out groups, out modifiedDate, customerDiscount) ?? 0) != 0;

            if (customerDiscount && (discountRate > 0 || (cateDiscountRates != null && cateDiscountRates.Count != 0) || (groups.SafeCount() != 0 && (!settings.Hidden.CustomerDiscountByGroup || groups.Any(m => m.Discount || m.DiscountRate > 0))))) 
                user = true;

            if (!user && price4LoginOnly) user = true;

            return string.Format("claw.modules.eshop:{0}:{1}:{2}:{3}:{4}::{5}::{6}",
                lang ? langCode : "", user ? (username + (modifiedDate == null ? "" : modifiedDate.Value.ToString("O"))) : "", currency && HttpContext.Current != null ? CurrencyMgr.FocusCurrencyCode : "",
                cartSumQty, membershipKey, key, price4Login ? ( showPrice ? 2 : 1) : 0).ToLower();
        } 

        #endregion

        #region Cacheable

        private static bool Cacheable
        {
            get
            {
                const StringComparison fcmp = StringComparison.OrdinalIgnoreCase;
                var ctx = HttpContext.Current == null ? null : ClawContext.Current;
                var username = (ctx != null && ctx.User != null ? ctx.User.Username : null) ?? "";
                var path = HttpContext.Current == null ? null : HttpContext.Current.Request.Url.AbsolutePath;

                if (username.Equals("Anonymous", StringComparison.OrdinalIgnoreCase)) return true;

                return ctx != null && path != null && ctx.CurrentAppType == ApplicationType.FrontEnd &&
                       !path.StartsWith("/admin/", fcmp) && !path.StartsWith("/AdminCP/", fcmp) &&
                       !path.StartsWith("/BackEnd/", fcmp) &&
                       !ContentPermission.GetContentPermission(AdminPermissionType.None, ContentPermissionType.Edit);
            }
        }

        #endregion

        #region CacheSet
        static void CacheSet(string key, Cell obj, int seconds = 1)
        {
            if (HttpContext.Current == null) return;

            HttpContext.Current.Cache.Insert(key, obj, null, DateTime.Now.AddSeconds(seconds), Cache.NoSlidingExpiration);
        } 
        #endregion

        #region CacheGet
        static Cell CacheGet(string key)
        {
            return HttpContext.Current == null ? null : (Cell) HttpContext.Current.Cache.Get(key);
        } 
        #endregion

        #region CacheClear
        static void CacheClear(string key)
        {
            HttpContext.Current.Cache.Remove(key);
        } 
        #endregion
    }

    #region DefaultScope

    public class DefaultScope : CacheScope
    {
        private static volatile object _timestamp;

        static DefaultScope()
        {
            _timestamp = DateTime.Now;
        }

        public override DateTime LastChange { get { return (DateTime)_timestamp; } }

        public static void Invalidate()
        {
            _timestamp = DateTime.Now;
        }
    }

    #endregion

    #region ProductScope

    public class ProductScope : CacheScope
    {
        private static volatile object _timestamp;

        static ProductScope()
        {
            _timestamp = DateTime.Now;
        }

        public override DateTime LastChange { get { return (DateTime)_timestamp; } }

        public static void Invalidate()
        {
            _timestamp = DateTime.Now;
            CombineScope.Invalidate();
        }
        public static void Invalidate(DateTime timestamp)
        {
            _timestamp = timestamp;
            CombineScope.Invalidate();
        }
    }
 
    #endregion

    #region CategoryScope

    public class CategoryScope : CacheScope
    {
        private static volatile object _timestamp;

        static CategoryScope()
        {
            _timestamp = DateTime.Now;
        }

        public override DateTime LastChange { get { return (DateTime)_timestamp; } }

        public static void Invalidate()
        {
            _timestamp = DateTime.Now;
            CombineScope.Invalidate();
        }
        public static void Invalidate(DateTime timestamp)
        {
            _timestamp = timestamp;
            CombineScope.Invalidate();
        }
    }

    #endregion

    #region DiscountScope

    public class DiscountScope : CacheScope
    {
        private static volatile object _timestamp;

        static DiscountScope()
        {
            _timestamp = DateTime.Now;
        }

        public override DateTime LastChange { get { return (DateTime)_timestamp; } }

        public static void Invalidate()
        {
            _timestamp = DateTime.Now;
            CombineScope.Invalidate();
        }
    }

    #endregion

    #region CustomerScope

    public class CustomerScope : CacheScope
    {
        private static volatile object _timestamp;

        static CustomerScope()
        {
            _timestamp = DateTime.Now;
        }

        public override DateTime LastChange { get { return (DateTime)_timestamp; } }

        public static void Invalidate()
        {
            _timestamp = DateTime.Now;
            CombineScope.Invalidate();
        }
    }

    #endregion

    #region CombineScope

    public class CombineScope : CacheScope
    {
        private static volatile object _timestamp;

        static CombineScope()
        {
            _timestamp = DateTime.Now;
        }

        public override DateTime LastChange { get { return (DateTime)_timestamp; } }

        public static void Invalidate()
        {
            _timestamp = DateTime.Now;
        }
    }

    #endregion

    #region LanguageScope

    public class LanguageScope : CacheScope
    {
        static Claw.Core.LanguageScope _raw = new Claw.Core.LanguageScope();

        public override DateTime LastChange { get { return _raw.LastChange; } }

        public static void Invalidate()
        {
            Claw.Core.LanguageScope.Invalidate();
        }
    }

    #endregion

    #region CurrencyScope

    public class CurrencyScope : CacheScope
    {
        static Claw.Core.LanguageScope _langScope = new Claw.Core.LanguageScope();

        private static volatile object _timestamp;

        static CurrencyScope()
        {
            _timestamp = DateTime.Now;
        }

        public override DateTime LastChange
        {
            get { return (DateTime)_timestamp <= _langScope.LastChange ? _langScope.LastChange : (DateTime)_timestamp; }
        }

        public static void Invalidate()
        {
            _timestamp = DateTime.Now;
            CombineScope.Invalidate();
        }
    }

    #endregion

    #region OrderScope

    public class OrderScope : CacheScope
    {
        private static volatile object _timestamp;

        static OrderScope()
        {
            _timestamp = DateTime.Now;
        }

        public override DateTime LastChange { get { return (DateTime)_timestamp; } }

        public static void Invalidate()
        {
            _timestamp = DateTime.Now;
            ProductScope.Invalidate();
        }
    }

    #endregion

    #region DefaultScope

    public class EshopOwnerScope : CacheScope
    {
        private static volatile object _timestamp;

        static EshopOwnerScope()
        {
            _timestamp = DateTime.Now;
        }

        public override DateTime LastChange { get { return (DateTime)_timestamp; } }

        public static void Invalidate()
        {
            _timestamp = DateTime.Now;
        }
    }

    #endregion
}
