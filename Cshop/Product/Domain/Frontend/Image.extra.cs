using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Claw.Core;
using Claw.Modules.Eshop;

namespace Claw.Modules.Eshop.Domain
{
    public partial class Image : Tuple<ImageLang>, IDisplayOrder
    {
        public virtual string Title
        {
            get
            {
                //if(Languages.FirstOrDefault() == null) return "";

                var lang = ClawContext.Current.CurrentLanguage.LanguageCode;
                var ret = Languages.MatchOrFirst(x => x.LangCode.EqualsIgCase(lang));
                return ret == null ? "" : ret.Title;

                //foreach (ImageLang detail in Languages)
                //{
                //    if (string.Compare(detail.LangCode.Trim(), lang, StringComparison.OrdinalIgnoreCase) == 0)
                //    {
                //        return detail.Title;
                //    }
                //}

                //return Languages.FirstOrDefault().Title;
            }
        }
        public virtual string ImageUrl
        {
            get
            {
                var fileName = "/Claw/Sites/Default/Modules/Eshop/Images/Product/no-thumb-img.jpg";
                if (!string.IsNullOrEmpty(FileName))
                {
                    fileName = FileName;
                }
                return ModuleUrl.ImagePath(fileName).TrimStart('~');
            }
        }
        public virtual string ImageThumbUrl
        {
            get
            {
                var fileName = "/Claw/Sites/Default/Modules/Eshop/Images/Product/no-img.jpg";
                if (!string.IsNullOrEmpty(FileName))
                {
                    fileName = FileName;
                }
                return ModuleUrl.ImageThumbPath(fileName).TrimStart('~');
            }
        }
    }
}