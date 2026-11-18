using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Claw.Core;
using Claw.Modules.Eshop;

namespace Claw.Modules.Eshop.Domain
{
    public partial class Property : Tuple<PropertyLang, PropVal>, IDisplayOrder
    {
        //public virtual bool Visible
        //{
        //    get
        //    {
        //        var lang = ClawContext.Current.CurrentLanguage.LanguageCode;

        //        foreach (PropertyLang detail in Languages)
        //        {
        //            if (string.Compare(detail.LangCode.Trim(), lang, StringComparison.OrdinalIgnoreCase) == 0)
        //            {
        //                return detail.Visible;
        //            }
        //        }

        //        return Languages.FirstOrDefault().Visible;
        //    }
        //}
        public virtual string Name
        {
            get
            {
                var lang = ClawContext.Current.CurrentLanguage.LanguageCode;
                
                var ret = Languages.MatchOrFirst(x => x.LangCode.EqualsIgCase(lang));
                return ret == null ? "" : ret.Name;

                //foreach (PropertyLang detail in Languages)
                //{
                //    if (string.Compare(detail.LangCode.Trim(), lang, StringComparison.OrdinalIgnoreCase) == 0)
                //    {
                //        return detail.Name;
                //    }
                //}

                
            }
        }
        //public virtual bool ValidToShow()
        //{
        //    if (Visible)
        //        return true;
        //    return false;
        //}
    }
}