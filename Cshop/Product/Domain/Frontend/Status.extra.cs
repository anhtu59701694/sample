using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Claw.Core;
using Claw.Modules.Eshop;

namespace Claw.Modules.Eshop.Domain
{
    public partial class Status : Tuple<ProductBase, StatusLang, VariantBase>
    {
        public virtual string Name
        {
            get
            {
                var lang = ClawContext.Current.CurrentLanguage.LanguageCode;
                var ret = Languages.MatchOrFirst(x => x.LangCode.EqualsIgCase(lang));
                return ret == null ? "" : ret.Name;

                //foreach (var detail in Languages)
                //{
                //    if (string.Compare(detail.LangCode.Trim(), lang, StringComparison.OrdinalIgnoreCase) == 0)
                //    {
                //        return detail.Name;
                //    }
                //}

                //return Languages.FirstOrDefault().Name;
            }
        }

        //public virtual bool ValidToShow()
        //{
        //    if ( Active )
        //        return true;
        //    return false;
        //}

        public virtual string GetImageFrist
        {
            get
            {
                var fileName = "/Claw/Sites/Default/Modules/eshop/Images/Status/no-img.jpg";
                if (!string.IsNullOrEmpty(Icon))
                {
                    fileName = Icon;
                }
                return ModuleUrl.ImagePath(fileName).TrimStart('~');
            }
        }
        public virtual string GetImageThumbFrist
        {
            get
            {
                var fileName = "/Claw/Sites/Default/Modules/eshop/Images/Status/no-thumb-img.jpg";
                if (!string.IsNullOrEmpty(Icon))
                {
                    fileName = Icon;
                }
                return ModuleUrl.ImageThumbPath(fileName).TrimStart('~');
            }
        }
    }
}