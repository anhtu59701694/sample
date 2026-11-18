using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Claw.Core;
using Claw.Modules.Eshop.Models;
using Claw.Modules.Eshop;

namespace Claw.Modules.Eshop.Domain
{
    public partial class Category : Tuple<CategoryLang, CategoryProduct, Category, IDom, IDom, IDom, IDom>, IDisplayOrder
    {
        public virtual bool Visible
        {
            get
            {
                var lang = ClawContext.Current.CurrentLanguage.LanguageCode;
                var ret = LanguagesFe.FirstOrDefault(x => x.LangCode.EqualsIgCase(lang));
                return ret != null && ret.Visible;
            }
        }
        public virtual string Name
        {
            get
            {
                var lang = ClawContext.Current.CurrentLanguage.LanguageCode;
                var ret = LanguagesFe.MatchOrFirst(x => x.LangCode.EqualsIgCase(lang));
                return ret == null ? "" : ret.Name;

            }
        }
        public virtual string AliasLang
        {
            get
            {
                var lang = ClawContext.Current.CurrentLanguage.LanguageCode;
                var ret = LanguagesFe.MatchOrFirst(x => x.LangCode.EqualsIgCase(lang));
                return ret == null ? "" : ret.Alias;

            }
        }

        public virtual string Description
        {
            get
            {
                var lang = ClawContext.Current.CurrentLanguage.LanguageCode;
                var ret = LanguagesFe.MatchOrFirst(x => x.LangCode.EqualsIgCase(lang));
                return ret == null ? "" : ret.Description;

            }
        }

        public virtual string PageTitle
        {
            get
            {
                var lang = ClawContext.Current.CurrentLanguage.LanguageCode;
                var ret = LanguagesFe.MatchOrFirst(x => x.LangCode.EqualsIgCase(lang));
                return ret == null ? "" : ret.PageTitle;

            }
        }

        public virtual string MetaDesc
        {
            get
            {
                var lang = ClawContext.Current.CurrentLanguage.LanguageCode;
                var ret = LanguagesFe.MatchOrFirst(x => x.LangCode.EqualsIgCase(lang));
                return ret == null ? "" : ret.MetaDesc;
            }
        }
        
        public virtual bool ValidToShow()
        {
            var ctx = ClawContext.Current;
            if (0 != (VisibilityUpPath &(byte) VisibilityType.Web) && 0 != (Visibility &(byte) VisibilityType.Web) && Visible && DateTime.Now >= PublishedDate && LanguagesFe.Any(c => c.LangCode.EqualsIgCase(ctx.CurrentLanguage.LanguageCode)))
                return true;
            return false;
        }

        public virtual string GetImageFrist
        {
            get
            {
                var fileName = "/Claw/Sites/Default/Modules/eshop/Images/Category/no-img.jpg";
                if (!string.IsNullOrEmpty(Image))
                {
                    fileName = Image;
                }
                return ModuleUrl.ImagePath(fileName).TrimStart('~');
            }
        }
        public virtual string GetImageThumbFrist
        {
            get
            {
                var fileName = "/Claw/Sites/Default/Modules/eshop/Images/Category/no-thumb-img.png";
                if (!string.IsNullOrEmpty(Image))
                {
                    fileName = Image;
                }
                return ModuleUrl.ImageThumbPath(fileName).TrimStart('~');
            }
        }

        public virtual string Banner0Url { get { return ModuleUrl.ImagePath(Banner0).ResolveClientUrl(); } }

        public virtual string Banner1Url { get { return ModuleUrl.ImagePath(Banner1).ResolveClientUrl(); } }

        public virtual string Banner2Url { get { return ModuleUrl.ImagePath(Banner2).ResolveClientUrl(); } }

    }
}