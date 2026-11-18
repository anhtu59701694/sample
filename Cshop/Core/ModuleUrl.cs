using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Web;
using Claw.Core;
using Claw.Core.Extensions;
using Claw.Core.Domain;
using System.Web.Routing;
using System.Web.Mvc;
using Claw.Modules.Eshop.DAL;
using Claw.Modules.Eshop.Helper;
using Claw.Modules.Eshop.Models;
using System.IO;
using NHibernate.Linq;

namespace Claw.Modules.Eshop
{
    public interface IModuleUrl
    {
        string GetFrontEndViewUrl(string controller, string alias, object model = null, Language lang = null);
        string GetProductUrl(Domain.Interfaces.IProduct product, Domain.Interfaces.IVariant variant = null, Domain.Interfaces.IOrderDetail orderDetail = null);
    }

    internal class ModuleUrl : Core.ModuleUrl { }
}

namespace Claw.Modules.Eshop.Core
{
    public class ModuleUrl 
    {
        public static string ExtIconPath = "~/Content/common/ext-icons/";
        private static string _invoicePdfFolder;

        public static string AdminUrl(string controller, string action, int? id)
        {
            return AdminUrl(controller, action, id == null ? "" : id.ToString());
        }

        public static string AdminUrl(string controller, string action, string id = "")
        {
            return SiteUrls.Instance.GetUrl("Eshop_Admin", controller, action, new { id = id });
        }

        public static string AdminUrl(string controller, string action, RouteValueDictionary routeValues)
        {
            return SiteUrls.Instance.GetUrl("Eshop_Admin", controller, action, routeValues);
        }

        public static string SettingUrl(string controller, int pcid, string action = "index")
        {
            return SiteUrls.Instance.GetUrl("Eshop_Settings", controller, action, new { id = pcid });
        }

        public static string FrontUrl(string controller, int pcid, string action = "index")
        {
            return SiteUrls.Instance.GetUrl("Eshop_FrontEnd", controller, action, new { id = pcid });
        }

        public static string FrontUrl(string controller, string action, RouteValueDictionary routeValues)
        {
            return SiteUrls.Instance.GetUrl("Eshop_FrontEnd", controller, action, routeValues);
        }

        public static string GetFrontEndViewUrls(string controller, string alias, object model = null)
        {
            var ret = new ExpandoObject();
            var lst = LanguageMgr.GetAll()
                .Select(x => new { Lang = x, Url = GetFrontEndViewUrl(controller, alias, model, x) });

            foreach (var itm in lst)
            {
                ((IDictionary<string, object>)ret).Add(itm.Lang.ShortLangCode, itm.Url);
            }

            return ret.ToJson();
        }

        public static string GetFrontEndViewUrl(string controller, string alias, object model = null, Language lang = null)
        {
            return Singleton<IModuleUrl>.Default.GetFrontEndViewUrl(controller, alias, model, lang);
        }

        protected static List<Language> GetActiveLanguages(int siteid)
        {
            return Helper.DataCache<Helper.LanguageScope, List<Language>>.Get(string.Format("Language.GetActiveLanguages:{0}", siteid), () =>
                ClawContext.Current.DataService.GetActiveLanguages(siteid), 15, true);
        }

        public static string LangIcon(string langCode, bool medium = false)
        {
            if (langCode == null) return null;
            else
            {
                if (medium) return string.Format("/content/common/flags/medium/lang_{0}.gif", langCode.ToLower());
                else return string.Format("/content/common/flags/lang_{0}.gif", langCode.ToLower());
            }
        }

        public static string IconUrl(string fileName)
        {
            return ("~/claw/sites/Default/Modules/Eshop/Images/admin/" + fileName).ResolveClientUrl();
        }

        public static string ContentUrl(string fileName, bool resolve = false)
        {
            var ret = "~/claw/sites/Default/Modules/Eshop/" + fileName;
            return resolve ? ret.ResolveClientUrl() : ret;
        }

        public static string TempFolder
        {
            get { return SiteControl.Instance.GetUserTempPath() + "/Modules/Eshop"; }
        }

        public static string TempThumbFolder
        {
            get { return SiteControl.Instance.GetUserTempPath() + "/Modules/Eshop/_thumbs"; }
        }

        public static string TempOriginalFolder
        {
            get { return SiteControl.Instance.GetUserTempPath() + "/Modules/Eshop/_original"; }
        }

        public static string ImageFolder
        {
            get { return (HttpContext.Current == null ? "" : SiteControl.Instance.GetUserImagePath()) + "/Modules/Eshop"; }
        }

        public static string AttachmentFolder
        {
            get { return SiteControl.Instance.GetUserImagePath() + "/Modules/Eshop/Attachment"; }
        }

        public static string InvoicePdfFolder
        {
            get
            {
                if (_invoicePdfFolder == null)
                {
                    var tmp = SiteControl.Instance.GetUserImagePath() + "/Modules/Eshop/InvoicePdf";
                    var folder = Globals.MapPath(tmp);

                    if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);

                    var lst = Directory.GetDirectories(folder);
                    if (lst.Length != 0) _invoicePdfFolder = lst[0];
                    else Directory.CreateDirectory(_invoicePdfFolder = folder.CombineWPath(Guid.NewGuid().ToString("N")));

                    _invoicePdfFolder = tmp.CombineWPath(new DirectoryInfo(_invoicePdfFolder).Name);
                }

                return _invoicePdfFolder;
            }
        }

        public static string PowerOfficeFolder
        {
            get { return SiteControl.Instance.GetUserImagePath() + "/Modules/Eshop/PowerOffice"; }
        }

        public static string OriginalImgFolder
        {
            get { return SiteControl.Instance.GetUserImagePath() + "/Modules/Eshop/OriginalImg"; }
        }

        public static string AbsoluteImageFolder
        {
            get { return Globals.ResolveClientUrl(ImageFolder); }
        }

        public static string ImagePath(string fileName)
        {
            if (string.IsNullOrEmpty(fileName)) return "";
            else return VirtualPathUtility.Combine(ImageFolder + "/", fileName);
        }

        public static string ImageThumbPath(string fileName)
        {
            if (string.IsNullOrEmpty(fileName)) return "";
            else return VirtualPathUtility.Combine(ThumbFolder + "/", fileName);
        }

        public static string AttachmentPath(string fileName)
        {
            if (string.IsNullOrEmpty(fileName)) return "";
            else return VirtualPathUtility.Combine(AttachmentFolder + "/", fileName);
        }

        public static string OriginalImgPath(string fileName)
        {
            if (string.IsNullOrEmpty(fileName)) return "";
            else return VirtualPathUtility.Combine(OriginalImgFolder + "/", fileName);
        }

        public static string TempImagePath(string fileName)
        {
            return VirtualPathUtility.Combine(TempFolder + "/", fileName);
        }

        public static string TempAttachmentPath(string fileName)
        {
            return VirtualPathUtility.Combine(TempFolder + "/", fileName);
        }

        public static string AbsoluteImagePath(string fileName)
        {
            if (string.IsNullOrEmpty(fileName)) return "";
            else return Globals.ResolveClientUrl(ImagePath(fileName));

        }

        public static string ThumbFolder
        {
            get { return (HttpContext.Current == null ? "" : SiteControl.Instance.GetUserThumbnailImagePath()) + "/Modules/Eshop"; }
        }

        public static string AbsoluteThumbFolder
        {
            get { return Globals.ResolveClientUrl(ThumbFolder); }
        }

        public static string ThumbPath(string fileName)
        {
            if (string.IsNullOrEmpty(fileName)) return "";
            else return VirtualPathUtility.Combine(ThumbFolder + "/", fileName);
        }

        public static string TempThumbPath(string fileName)
        {
            if (string.IsNullOrEmpty(fileName)) return string.Empty;
            else return VirtualPathUtility.Combine(TempThumbFolder + "/", fileName);
        }

        public static string TempOriginalPath(string fileName)
        {
            if (string.IsNullOrEmpty(fileName)) return string.Empty;
            else return VirtualPathUtility.Combine(TempOriginalFolder + "/", fileName);
        }

        public static string AbsoluteThumbPath(string fileName)
        {
            return Globals.ResolveClientUrl(ThumbPath(fileName));
        }

    }
}