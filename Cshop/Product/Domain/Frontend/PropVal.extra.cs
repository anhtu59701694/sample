using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Claw.Core;
using Claw.Modules.Eshop;

namespace Claw.Modules.Eshop.Domain
{
    partial class PropVal : Tuple<PropValLang, VariantPropVal>
    {
        public virtual string Name
        {
            get
            {
                var lang = ClawContext.Current.CurrentLanguage.LanguageCode;
                var ret = Languages.MatchOrFirst(x => x.LangCode.EqualsIgCase(lang));
                return ret == null ? "" : ret.Name;

                //foreach (PropValLang detail in Languages)
                //{
                //    if (string.Compare(detail.LangCode.Trim(), lang, StringComparison.OrdinalIgnoreCase) == 0)
                //    {
                //        return detail.Name;
                //    }
                //}

                //return Languages.FirstOrDefault().Name;
            }
        }

        public virtual string GetIcon
        {
            get
            {
                var fileName = "/Claw/Sites/Default/Modules/Eshop/Images/Variant/no-icon.jpg";
                if (!string.IsNullOrEmpty(Icon))
                {
                    fileName = Icon;
                }
                return ModuleUrl.ImageThumbPath(fileName).TrimStart('~');
            }
        }

        public virtual bool ValidToShow()
        {
            if (Active)
                return true;
            return false;
        }
    }
}