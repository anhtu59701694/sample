using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Text;
using Claw.Controls.Modules;
using Claw.Core;
using Ninject;
using Claw.Core.Domain;
using Claw.Core.Extensions;
using Claw.Core.Web.Mvc.Bundle;
using Claw.Modules.Eshop.Helper;
using Claw.Core.Interfaces;

namespace Claw.Modules.Eshop.DAL
{
    public class LanguageMgr : BaseMgr
    {
        const string OverrideLangCodeKey = "Claw.Modules.Eshop.DAL.LanguageMgr.OverrideLangCode";

        public static string KendoPath;

        #region LanguageMgr

        static LanguageMgr()
        {
            if (!ModuleMgr.IsEmbedMode)
            {
                var bundleMgr = BundleConfigurationManager.GetBundleConfigCollection();
                var config = bundleMgr.JsBundles.FirstOrDefault(x => x.BundlePath.EqualsIgCase("~/js/kendo"));
                var file = (config == null
                    ? null
                    : config.Files.FirstOrDefault(
                        x => x.FilePath.EndsWith("/kendo.all.js") || x.FilePath.EndsWith("/kendo.core.js")));

                KendoPath = (file == null ? null : VirtualPathUtility.GetDirectory(file.FilePath).TrimEnd('/'));
            }

            lock (StringLocalizeExtensions.SyncObj)
            {
                StringLocalizeExtensions.CurrentLangFilters.Add(ModExtensions.ModuleName, GetLocalizedLangCode);
                if (ModExtensions.ModuleName.ToLower() != ModExtensions.ModuleName)
                    StringLocalizeExtensions.CurrentLangFilters.Add(ModExtensions.ModuleName.ToLower(), GetLocalizedLangCode);
            }
        }

        #endregion

        #region GetLocalizedLangCode

        static StringLocalizeExtensions.Setting GetLocalizedLangCode()
        {
            var oLang = HttpContext.Current.Items[OverrideLangCodeKey];

            if (oLang != null && !string.IsNullOrEmpty(oLang as string))
            {
                return new StringLocalizeExtensions.Setting()
                {
                    LangCode = (string)oLang,
                    ForceAdminLang = false
                };
            }
            else return null;
        }

        #endregion

        #region OverrideLangCode

        public static void OverrideLangCode(string langCode)
        {
            HttpContext.Current.Items[OverrideLangCodeKey] = langCode;
        }

        #endregion

        #region Get

        public static Language Get(string langCode, IDB db = null)
        {
            if (langCode.IsNullOrEmpty()) return null;

            Language oVal;

            GetAllRaw(db).Item2.TryGetValue(langCode.ToLower(), out oVal);

            return oVal;
        }

        #endregion

        #region GetOrDefault

        public static Language GetOrDefault(string langCode)
        {
            if (ClawContext.Current != null && ClawContext.Current.Context != null)
                return Get(langCode) ?? ClawContext.Current.GetDefaultOrCurrentLang();

            return Get(langCode) ?? GetDefaultDBLang();
        }

        public static Language GetDefaultDBLang() { return GetDefaultDBLang(null); }

        public static Language GetDefaultDBLang(IDataService ds)
        {
            ds = ds ?? (IoC.Kernel == null ? null : IoC.Kernel.Get<IDataService>());
            if (ds != null)
            {
                var lst = ds.GetList<Language>(string.Format("select a from {0} a where a.Active = 1", typeof(Language)));
                if (lst != null && lst.Any())
                    return lst[0];
            }

            return new Language()
            {
                LanguageCode = "nb-no",
                ShortLangCode = "no",
                ThousandsSeperator = ".",
                DecimalSeperator = ",",
                LanguageName = "Norwegian",
                CharSet = "utf-8",
                TextDirection = "rtl",
                InputDateFormat = "dd/MM/yyyy",
                ViewDateFormat = "dd/MM/yyyy",
                InputTimeFormat = "hh:mm:ss",
                ViewTimeFormat = "hh:mm:ss"
            };
        }

        #endregion

        #region GetAll

        static Tuple<List<Language>, Dictionary<string, Language>> GetAllRaw(IDB db = null)
        {
            if (db == null && (ClawContext.Current == null || ClawContext.Current.DataService == null))
                return new Tuple<List<Language>, Dictionary<string, Language>>(new List<Language>(), new Dictionary<string, Language>());
               
            return Helper.DataCache<Helper.LanguageScope, System.Tuple<List<Language>, Dictionary<string, Language>>>.Get(string.Format("LanguageMgr.GetAll"), () =>
            {
                var ds = db != null ? db.DataService : (ClawContext.Current != null ? ClawContext.Current.DataService : null);
                var lst = (ds == null ? null : ds.GetActiveLanguages(ds.GetSiteId())) ?? new List<Language>();
                return new Tuple<List<Language>, Dictionary<string, Language>>(lst, lst.ToDictionary(x => x.LanguageCode.ToLower(), x => x));
            }, 15, true);
        }

        public static List<Language> GetAll(IDB db = null)
        {
            return GetAllRaw(db).Item1;
        }

        #endregion

        #region FocusLangCode

        public static string FocusLangCode
        {
            get
            {
                var curCxt = HttpContext.Current;
                if (curCxt != null)
                {
                    var overrideLang = curCxt.Items[OverrideLangCodeKey];

                    if (overrideLang != null && !string.IsNullOrEmpty(overrideLang as string))
                        return (string)overrideLang;

                    var action = curCxt.GetRouteValue("action");
                    if (action != null && action.ToString().EqualsIgCase("Input"))
                    {
                        return OptLangCode(QParam.GetId() ?? 0);
                    }
                    else
                    {
                        var lang = curCxt.Request["lang"];
                        if (string.IsNullOrEmpty(lang))
                            lang = (curCxt.GetRouteValue("lang") ?? "").ToString();

                        if (!string.IsNullOrEmpty(lang))
                        {
                            if (lang.Length == 2) return Translate(lang);
                            else return lang.ToLower();
                        }
                    }
                }

                return GetDefaultDBLang().LanguageCode.ToLower();
            }
        }

        #endregion

        #region FocusLang

        public static Language FocusLang
        {
            get
            {
                return GetOrDefault(FocusLangCode);
            }
        }

        #endregion

        #region ValidOptLang

        public static Language ValidOptLang(int id)
        {
            var langCode = OptLangCode(id);
            return LanguageMgr.GetOrDefault(langCode);
        }

        #endregion

        #region ValidOptLangCode

        public static string ValidOptLangCode(int id)
        {
            return ValidOptLang(id).LanguageCode;
        }

        #endregion

        #region OptLangCode

        public static string OptLangCode(string id)
        {
            return OptLangCode(id.IsZeroId() ? 0 : 1);
        }

        public static string OptLangCode(int id)
        {
            return OptLangCode((long) id);
        }

        public static string OptLangCode(long id)
        {
            var curCxt = HttpContext.Current;
            if (curCxt != null)
            {
                var optlang = (id != 0 ? null : curCxt.Request["optlang"]);
                if (!string.IsNullOrEmpty(optlang)) return optlang.ToLower();
                else
                {
                    var lang = curCxt.Request["lang"];
                    if (string.IsNullOrEmpty(lang))
                        lang = (curCxt.GetRouteValue("lang") ?? "").ToString();

                    if (!string.IsNullOrEmpty(lang))
                    {
                        if (lang.Length == 2) return Translate(lang);
                        else return lang.ToLower();
                    }
                }
            }

            if (ClawContext.Current != null && ClawContext.Current.Context != null)
                return ClawContext.Current.GetDefaultOrCurrentLangCode().ToLower();


            return GetDefaultDBLang().LanguageCode.ToLower();
        }

        #endregion

        #region OptLang

        public static Language OptLang(string id)
        {
            return OptLang(id.IsZeroId() ? 0 : 1);
        }

        public static Language OptLang(int id)
        {
            return GetOrDefault(OptLangCode(id));
        }

        public static Language OptLang(long id)
        {
            return GetOrDefault(OptLangCode(id));
        }

        #endregion

        #region Translate
        public static string Translate(string lang)
        {
            var cxt = ClawContext.Current;

            if (cxt.CurrentLanguage != null && cxt.CurrentLanguage.ShortLangCode.EqualsIgCase(lang))
                return cxt.CurrentLanguage.LanguageCode.ToLower();
            if (cxt.DefaultLanguage != null && cxt.DefaultLanguage.ShortLangCode.EqualsIgCase(lang))
                return cxt.DefaultLanguage.LanguageCode.ToLower();

            return GetAll()
                .Where(x => x.ShortLangCode.EqualsIgCase(lang))
                .Select(x => x.LanguageCode).FirstOrDefault() ?? lang;
        }
        #endregion

        #region NumberRegex

        public static string GetNumberRegex(Language lang = null, int? id = null)
        {
            if (lang == null) lang = OptLang(id ?? (QParam.GetId() ?? 0));
            return string.Format(@"^-?(?:\d+|\d{{1,3}}(?:\{0}\d{{3}})+)?(?:\{1}\d+)?$",
                lang.ThousandsSeperator, lang.DecimalSeperator);
        }

        #endregion
    }
}