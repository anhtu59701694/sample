using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Claw.Core;
using Claw.Core.Domain;
using Claw.Core.Enumerations;
using Claw.Core.Interfaces;
using Claw.Core.Logs;
using Claw.Modules.Eshop.DAL;
using Claw.Modules.Eshop.Models;
using Claw.Modules.Eshop.Domain;
using Claw.Modules.Eshop.Helper;
using Claw.Modules.Eshop.Domain.Interfaces;
using System.Data;

namespace Claw.Modules.Eshop
{
    public static class DomExtensions
    {         
        #region GetTitleHint
        public static string GetTitleHint(this SettingsBiz.SizeBiz obj)
        {
            if (obj == null) return string.Empty;

            return string.Format("{0}x{1}", obj.Width, obj.Height);
        }
        #endregion

        #region GetName

        public static string GetName(this Claw.Core.Domain.Role obj)
        {
            return obj == null ? string.Empty : obj.RoleName;
        }

        public static string GetName(this Country obj, string lang = null)
        {
            if (obj == null) return string.Empty;

            var langCode = lang ?? LanguageMgr.FocusLangCode;
            var gLang = obj.Languages.MatchOrFirst(m => m.LangCode.EqualsIgCase(langCode));

            return gLang == null ? string.Empty : gLang.Name;
        }

        public static string GetName(this City obj, string lang = null)
        {
            if (obj == null) return string.Empty;

            var langCode = lang ?? LanguageMgr.FocusLangCode;
            var gLang = obj.Languages.MatchOrFirst(m => m.LangCode.EqualsIgCase(langCode));

            return gLang == null ? string.Empty : gLang.Name;
        }

        public static string GetName(this Region obj, string lang = null)
        {
            if (obj == null) return string.Empty;

            var langCode = lang ?? LanguageMgr.FocusLangCode;
            var gLang = obj.Languages.MatchOrFirst(m => m.LangCode.EqualsIgCase(langCode));

            return gLang == null ? string.Empty : gLang.Name;
        }

        public static string GetName(this District obj, string lang = null)
        {
            if (obj == null) return string.Empty;

            return obj.Name;
        }

        public static string GetName(this IPaymentMethod obj, string lang = null)
        {
            if (obj == null) return string.Empty;

            return obj.GetNameEx(lang);
        }

        public static string GetName(this ICustGroup obj)
        {
            if (obj == null) return string.Empty;

            return obj.Name;
        }

        public static string GetName(this IShipping obj)
        {
            if (obj == null) return string.Empty;

            return obj.Name;
        }

        public static string GetName(this IDiscount obj)
        {
            if (obj == null) return string.Empty;

            return obj.Name;
        }
                     
        #endregion

        #region GetId

        public static int? GetId(this Claw.Core.Domain.Role obj, int? defVal = null)
        {
            if (obj == null) return defVal;
            else return obj.Id;
        }

        public static int? GetId(this Country obj, int? defVal = null)
        {
            if (obj == null) return defVal;
            else return obj.Id;
        }

        public static int? GetId(this Region obj, int? defVal = null)
        {
            if (obj == null) return defVal;
            else return obj.Id;
        }

        public static int? GetId(this IDom obj, int? defVal = null)
        {
            if (obj == null) return defVal;
            else return obj.Id;
        }

        #endregion

        #region GetCode
        public static string GetCode(this Country obj)
        {
            return obj != null ? obj.Code : string.Empty;
        }
        #endregion

        #region FormatPercent

        public static string FormatPercent(this OFloat value)
        {
            if (value.IsNull()) return string.Empty;

            return new OFloat(value.Value * 100) + " %";
        }

        #endregion

        #region IsDefault
        public static bool IsDefault(this Currency obj)
        {
            if (obj == null) return false;

            var defCurrency = SettingsBiz.GetSettings().General.Currency;

            return defCurrency.EqualsIgCase(obj.Code);
        } 
        #endregion

        #region Format

        public static string Format(this CurrencyModel obj, decimal value)
        {
            if (obj == null) return string.Empty;

            return string.Format("{0}: {1} {2}", obj.Code, new OMoney(value), obj.Token);
        }

        #endregion

        #region Combine

        public static OMoney Combine(this OMoney value, OMoney target)
        {
            if (value == null || value.Value == 0) return target;

            return value;
        }

        public static decimal? Combine(this decimal? value, decimal? target)
        {
            if (value == null || value.Value == 0) return target;

            return value;
        }

        #endregion

        #region NormalizePriceInc
        public static OMoney NormalizePriceInc(this decimal? priceInc, decimal? defVal = null, bool notNull = false)
        {
            if (priceInc == null && defVal == null) return notNull ? new OMoney() : null;

            var setting = SettingsBiz.GetSettings().Display.Others;
            var ret = setting.PriceFocusInc ? priceInc.RoundEx(setting.PriceRound) : priceInc;
            return new OMoney(ret ?? defVal);
        } 
        #endregion

        #region ToOFloat
        public static OFloat ToOFloat(this Vat obj)
        {
            if (obj == null) return null;
            return new OFloat(obj.Value);
        }
        #endregion

        #region AnyDepend

        public static bool AnyDepend(this Vat obj)
        {
            if (obj == null) return false;

            return obj.Products.Count != 0 || obj.ProductSeconds.Count != 0 ||
                   obj.Shippings.Count != 0 || obj.PaymentMethods.Count != 0;
        }

        #endregion

        #region IsAdmin

        public static bool IsAdmin(this Claw.Core.Domain.User obj)
        {
            return obj != null && (obj.IsSuperAdmin || obj.IsAdministrator);
        }

        #endregion

        #region IsClickCollect

        public static bool IsClickCollect(this User obj)
        {
            if (obj.IsAdmin()) return true;

            var settings = SettingsBiz.GetSettings();
            return obj != null && obj.Active && obj.Usercms_UserRoleses != null &&
                   obj.Usercms_UserRoleses.Cast<UserRole>()
                       .Any(m => m.Role.RoleName.EqualsIgCase(settings.Hidden.ClickCollectRoleName));
        }

        #endregion


        #region HasUserPermission

        public static bool HasUserPermission(this Claw.Core.Domain.User obj)
        {
            return obj != null && (obj.IsSuperAdmin || obj.IsAdministrator ||
                obj.Usercms_UserRoleses.Any<Claw.Core.Domain.UserRole>(x =>
                    (x.Role.MaskPermission & (long)AdminPermissionType.Users) == (long)AdminPermissionType.Users));//AdminPermissionType.Users
        }

        #endregion

        #region GetUserCount
        public static int? GetUserCount(this Permission obj)
        {
            if (obj == null || obj.Role == null || obj.Role.Rolecms_UserRoleses == null) return null;

            return obj.Role.Rolecms_UserRoleses.Count;
        } 
        #endregion

        #region GetUserRoles
        public static string GetUserRoles(this Claw.Core.Domain.User obj)
        {
            if (obj == null || obj.Usercms_UserRoleses == null) return null;

            return obj.Usercms_UserRoleses.Cast<UserRole>()
                .Where(x => (x.Role.MaskPermission & (long) AdminPermissionType.Eshop) == (long) AdminPermissionType.Eshop)
                .Select(x => x.Role.RoleName).ToString(" | ");
        }
        #endregion

        #region GetFee
        public static decimal GetFee(this IShipping obj, decimal orderTotal, decimal weightTotal, string countryCode, IDB db = null, decimal extraCostTotal = 0)
        {
            return obj == null ? 0 : obj.GetFeeRaw(orderTotal, weightTotal, countryCode, db, extraCostTotal);
        }  
        #endregion

        #region IDataService

        public static bool Update(this object obj, IDataService dataService)
        {
            return obj.Update(dataService, false);
        }

        public static bool Update(this object obj, IDataService dataService, bool throwOnErr)
        {
            NHibernate.ITransaction tran = null;
            try
            {
                var session = dataService.Session;

                tran = session.BeginTransaction();
                session.Update(obj);
                tran.Commit();
                return true;
            }
            catch (Exception exp)
            {
                try
                {
                    if (tran != null && tran.IsActive) tran.Rollback();
                }
                catch(Exception err) 
                { 
                    LogHelper.Log4net(err, ClawExceptionType.UpdateObject); 
                }

                if (Debugger.IsAttached) Debugger.Break();

                LogHelper.Log4net(exp, ClawExceptionType.UpdateObject);

                if (throwOnErr) throw exp;
            }
            return false;
        }

        public static bool Update<T>(this IEnumerable<T> list, IDataService dataService)
        {
            int count = 0;
            if (list != null)
            {
                foreach (T obj in list)
                {
                    if (obj.Update(dataService))
                        count++;
                }

                if (count == list.Count())
                    return true;
            }

            return false;
        }

        public static bool SaveorUpdate(this object obj, IDataService dataService)
        {
            return obj.SaveorUpdate(dataService, false);
        }

        public static bool SaveorUpdate(this object obj, IDataService dataService, bool throwOnErr)
        {
            NHibernate.ITransaction tran = null;
            try
            {
                var session = dataService.Session;

                tran = session.BeginTransaction();
                session.SaveOrUpdate(obj);
                tran.Commit();
                return true;
            }
            catch (Exception exp)
            {               
                try
                {
                    if (tran != null && tran.IsActive) tran.Rollback();
                }
                catch (Exception err)
                {
                    LogHelper.Log4net(err, ClawExceptionType.InsertOrUpdateObject);
                }

                if (Debugger.IsAttached) Debugger.Break();

                LogHelper.Log4net(exp, ClawExceptionType.InsertOrUpdateObject);

                if (throwOnErr) throw exp;
            }
            return false;
        }
 
        #endregion
    }
}