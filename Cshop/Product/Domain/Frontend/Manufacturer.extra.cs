using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Claw.Core;
using Claw.Modules.Eshop;

namespace Claw.Modules.Eshop.Domain
{
    public partial class Manufacturer : Tuple<ManufacturerLang, ProductBase>, IDisplayOrder, IStatus
    {
        //public virtual bool Visible
        //{
        //    get
        //    {
        //        var lang = ClawContext.Current.CurrentLanguage.LanguageCode;

        //        foreach (ManufacturerLang detail in LanguagesFe)
        //        {
        //            if (string.Compare(detail.LangCode.Trim(), lang, StringComparison.OrdinalIgnoreCase) == 0)
        //            {
        //                return detail.Visible;
        //            }
        //        }

        //        return false;
        //    }
        //}

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
        //public virtual bool ValidToShow()
        //{
        //    if (Active && Visible)
        //        return true;
        //    return false;
        //}
        public virtual string GetImage
        {
            get
            {
                var fileName = "/Claw/Sites/Default/Modules/Eshop/Images/Manufacturer/no-img.jpg";
                if (Logo != null)
                {
                    fileName = Logo;
                }
                return ModuleUrl.ImagePath(fileName).TrimStart('~');
            }
        }
        public virtual string GetImageThumb
        {
            get
            {
                var fileName = "/Claw/Sites/Default/Modules/Eshop/Images/Manufacturer/no-thumb-img.jpg";
                if (Logo != null)
                {
                    fileName = Logo;
                }
                return ModuleUrl.ImageThumbPath(fileName).TrimStart('~');
            }
        }
    }
}