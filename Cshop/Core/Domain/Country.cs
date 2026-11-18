using System.Collections.Generic;
using System.Linq;
using Claw.Modules.Eshop.DAL;
using Claw.Modules.Eshop;

namespace Claw.Modules.Eshop.Domain
{
    public partial class Country : Tuple<CountryLang, City>
    {
        private CountryLang _lang;

        public virtual int Id { get; set; }

        public virtual string Code { get; set; }

        public virtual Region Region { get; set; }

        #region Name

        public virtual string Name
        {
            get
            {
                if (_lang == null)
                {
                    var langCode = LanguageMgr.FocusLangCode;
                    _lang = Languages.MatchOrFirst(x => x.LangCode.EqualsIgCase(langCode));
                }
                return _lang == null ? string.Empty : _lang.Name;
            }
        } 

        #endregion

        public virtual IList<CountryLang> Languages
        {
            get { return Get(ref _list0); }
            set { _list0 = value; }
        }

        public virtual IList<City> Cities
        {
            get { return Get(ref _list1); }
            set { _list1 = value; }
        }
    }
}