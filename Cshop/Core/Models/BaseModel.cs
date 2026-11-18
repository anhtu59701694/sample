using AutoMapper;
using Claw.Core.Domain;
using Claw.Modules.Eshop.DAL;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Web.Routing;
using System.Web.Script.Serialization;

namespace Claw.Modules.Eshop.Models
{
    #region ILanguage

    public interface ILanguage
    {
        string LangCode { get; set; }
    }

    public interface ILanguage2 : ILanguage
    {
        bool Visible { get; set; }
    } 

    #endregion

    #region BaseModel<TLang>

    public class BaseModel<TLang>
        where TLang : class, ILanguage
    {
        public bool AllLangForcedActive = true;

        private TLang _language;
        private string _langCode;

        [Exportable]
        public int Id { get; set; }      

        public bool InlineEdit { get; set; }

        public bool QuickAdd { get; set; }

        public List<TLang> Languages { get; set; }

        protected BaseModel()
        {
            InlineEdit = false;
        }

        public static Type GetLangType() { return typeof(TLang); }

        #region GetLang

        public TLang GetLang(string langCode = null, bool defFirst = false)
        {
            if (langCode != null) return Languages.FirstOrDefault(m => m.LangCode.EqualsIgCase(langCode));
            else
            {
                if (_language == null)
                {
                    langCode = _langCode ?? LanguageMgr.FocusLangCode;
                    _language = defFirst
                        ? Languages.MatchOrFirst(m => m.LangCode.EqualsIgCase(langCode))
                        : Languages.FirstOrDefault(m => m.LangCode.EqualsIgCase(langCode));
                }
                return _language;
            }
        }

        protected TVal GetLang<TVal>(Func<TLang, TVal> valueSelector, bool defFirst = false)
        {
            var lang = GetLang(null, defFirst);
            return lang == null ? default(TVal) : valueSelector(lang);
        }

        #endregion

        #region SetLang
        protected void SetLang(Action<TLang> valueSetter, Func<TLang> fNew = null)
        {
            var lang = fNew == null
                ? GetLang()
                : GetLangOrAddNew(LanguageMgr.FocusLangCode, fNew);

            if (lang != null) valueSetter(lang);
        }
        #endregion

        #region GetLangOrAddNew
        public TLang GetLangOrAddNew(string langCode, Func<TLang> fNew)
        {
            TLang ret = GetLang(langCode);

            if (Languages == null) Languages = new List<TLang>();

            if (ret == null) Languages.Add(ret = fNew());

            return ret;
        } 
        #endregion

        #region SetCurrentLang
        public void SetCurrentLang(string langCode)
        {
            if (!langCode.IsNullOrEmpty())
            {
                _langCode = langCode;
                _language = null;
            }
        }
        #endregion

        #region Expando

        public ExpandoObject ToExpando(IEnumerable<Language> langs,
            Action<IDictionary<string, object>, TLang, Language> fLang)
        {
            var value = new ExpandoObject();
            var obj = value as IDictionary<string, object>;
            var args = new RouteValueDictionary(this);

            foreach (var arg in args)
            {
                obj.Add(arg.Key, arg.Value);
            }

            foreach (var lang in langs)
            {
                var langCode = lang.LanguageCode;
                var mlang = Languages.FirstOrDefault(x => x.LangCode.EqualsIgCase(langCode));

                fLang(obj, mlang, lang);
            }

            return value;
        }

        #endregion
    } 

    #endregion

    #region BaseModel<TModel, TLang>

    public class BaseModel<TModel, TLang> : BaseModel<TLang>
        where TLang : class, ILanguage
        where TModel : class
    {
        protected TModel _raw;

        #region BaseModel
        protected BaseModel()
        {
            _raw = Activator.CreateInstance<TModel>();
        }
        #endregion

        #region Raw
        [Newtonsoft.Json.JsonIgnore]
        [ScriptIgnore(ApplyToOverrides = true)]
        public TModel Raw
        {
            get { return null; }
            set { _raw = value; }
        }
        #endregion

        #region GetRaw
        public TModel GetRaw()
        {
            return _raw;
        } 
        #endregion
    } 

    #endregion

    #region IPhoto

    public interface IPhoto
    {
        string FileName { get; set; }
        string Thumb { get; }
        string FsUrl { get; }
        string Title { get; }
    } 

    #endregion

    #region PImage

    public struct PImage<TDom, TDImg, TMImg>
        where TDom : class
        where TDImg : class
        where TMImg : class, IPhoto
    {
        private Func<TDom, IList<TDImg>> _expression;
        private bool _triedGet;
        private TDom _owner;
        private TMImg _image;

        #region PImage

        public PImage(TDom owner, Func<TDom, IList<TDImg>> expression)
        {
            _expression = expression;
            _owner = owner;
            _triedGet = false;
            _image = null;
        } 

        #endregion

        #region TryGet

        void TryGet()
        {
            if (_image == null && !_triedGet)
            {
                var dimg = _expression(_owner).FirstOrDefault();
                if (dimg != null) _image = Mapper.Map<TDImg, TMImg>(dimg);
                _triedGet = true;
            }
        } 

        #endregion

        #region Thumb
        public string Thumb
        {
            get
            {
                TryGet();
                return _image == null ? string.Empty : _image.Thumb;
            }
        }
        #endregion

        #region FsUrl
        public string FsUrl
        {
            get
            {
                TryGet();
                return _image == null ? string.Empty : _image.FsUrl;
            }
        }
        #endregion

        #region Title
        public string Title
        {
            get
            {
                TryGet();
                return _image == null ? string.Empty : _image.Title;
            }
        }
        #endregion
    }
 
    #endregion

    #region ExportableAttribute

    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Parameter | AttributeTargets.Property, AllowMultiple = false)]
    public sealed class ExportableAttribute : Attribute
    {
        public string Name { get; set; }

        public bool CanImport { get; set; }
        public bool CanExport { get; set; }

        public ExportableAttribute(bool import = true, bool export = true)
        {
            CanImport = import;
            CanExport = export;
        }
    }

    #endregion

    #region BaseExModel<TModel>

    public class BaseExModel<TModel>
        where TModel : class
    {
        protected TModel _raw;

        protected BaseExModel()
        {
            _raw = Activator.CreateInstance<TModel>();
            InlineEdit = false;
        }

        [Newtonsoft.Json.JsonIgnore]
        [ScriptIgnore(ApplyToOverrides = true)]
        public TModel Raw { get { return null; } set { _raw = value; } }

        [Exportable]
        public int Id { get; set; }

        public bool InlineEdit { get; set; }

        public bool QuickAdd { get; set; }

        public TModel GetRaw() { return _raw; }
    }

    #endregion
}