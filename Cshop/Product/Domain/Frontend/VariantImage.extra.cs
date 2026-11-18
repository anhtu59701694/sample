using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Claw.Core;
using Claw.Modules.Eshop.Controllers.Admin;
using Claw.Modules.Eshop.Helper;

namespace Claw.Modules.Eshop.Domain
{
    public partial class VariantImage : Tuple<VariantImageLang>
    {
        public virtual string Title
        {
            get
            {
                var lang = ClawContext.Current.CurrentLanguage.LanguageCode;

                var ret = Languages.MatchOrFirst(x => x.LangCode.EqualsIgCase(lang));
                return ret == null ? "" : ret.Title;

            }
        }

        public virtual string GetImage
        {
            get
            {
                var fileName = FileName;
               
                return ModuleUrl.ImagePath(fileName).TrimStart('~');
            }
        }
        public virtual string GetThumb
        {
            get
            {
                var fileName = FileName;
                
                return ModuleUrl.ImageThumbPath(fileName).TrimStart('~');
            }
        }
    }
}