using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Threading;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc.Html;
using Claw.Controls.Modules;
using Claw.Core;
using Claw.Core.Configurations;
using Claw.Core.DAL;
using Claw.Core.Domain;
using Claw.Core.Interfaces;
using Claw.Core.Utility;
using Claw.Modules.Eshop.Controllers.Admin;
using Claw.Modules.Eshop.Models;
using Kendo.Mvc.UI;
using Kendo.Mvc.UI.Fluent;
using Claw.Core.Extensions;
using NHibernate.Engine;
using NHibernate.Linq;
using Ninject;
using NPOI.SS.Formula.Functions;
using Encoder = System.Text.Encoder;
using MapperInt = AutoMapper.Internal;
using Claw.Modules.Eshop.DAL;
using System.Reflection;
using System.Dynamic;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.Web.Script.Serialization;
using NHibernate;
using Claw.Modules.Eshop.Helper;
using Eshop.Payments.BasePayment;

namespace Claw.Modules.Eshop
{
    #region RouteInfo
    /// <summary>parse url into route data [controller,action,id,...]</summary>
    public class RouteInfo : HttpContextBase
    {
        #region InternalRequestContext

        private class InternalRequestContext : HttpRequestBase
        {
            private readonly Uri _uri;

            public InternalRequestContext(Uri uri) { _uri = uri; }

            public override string AppRelativeCurrentExecutionFilePath
            {
                get { return "~" + _uri.AbsolutePath; }
            }

            public override string PathInfo { get { return string.Empty; } }
        }

        #endregion

        private readonly HttpRequestBase _request;
        public RouteData RouteData { get; private set; }

        public RouteInfo(Uri uri)
        {
            if (uri != null)
            {
                _request = new InternalRequestContext(uri);
                RouteData = RouteTable.Routes.GetRouteData(this);
            }
        }

        public override HttpRequestBase Request { get { return _request; } }

        /// <summary>get data item by key. Return "" if not found</summary>
        public string this[string key]
        {
            get
            {
                if (RouteData != null)
                {
                    object val;
                    if (RouteData.Values.TryGetValue(key, out val)) return (val ?? "").ToString();
                }
                return string.Empty;
            }
        }

        public RouteInfo Remove(params string[] keys)
        {
            if (RouteData != null)
                foreach (var key in keys)
                {
                    RouteData.Values.Remove(key);
                }

            return this;
        }
    }

    #endregion
    #region DDListItem
    /// <summary>Item class for Kendo DropDownList</summary>
    public class DDListItem
    {
        public const string Template = "<div title='#: data.Title#' class='#: data.CssClass#' #=data.Events# >#: data.Text#</div>";

        public string Value { get; set; }
        public string Text { get; set; }
        public bool Active { get; set; }
        public bool? LangVisible { get; set; }
        public object Model { get; set; }
        public int id { get; set; }

        #region CssClass
        public virtual string CssClass
        {
            get
            {
                if (Value == null) return string.Empty;
                else return Active && (LangVisible ?? false) && Value != "-1" ? "" : "item-lang-disabled";
            }
        }
        #endregion

        #region Events
        public virtual string Events
        {
            get
            {
                return string.Empty;
            }
        }
        #endregion

        #region Title
        public string Title
        {
            get
            {
                if (Value == null) return string.Empty;
                else return Active
                        ? ((LangVisible ?? false) ? "" : StringTable.ItemLanguageDisabled)
                        : StringTable.ItemDeactivated;
            }
        }
        #endregion
    }

    /// <summary>support grouping in DropDownList</summary>
    public class DDListItemGroup : DDListItem
    {
        public bool IsGroup { get; set; }

        public override string CssClass
        {
            get { return base.CssClass + (IsGroup ? " group" : " item"); }
        }

        #region Events
        /// <summary>javascript event handlers</summary>
        public override string Events
        {
            get
            {
                if (IsGroup) return " onclick='return false' onmouseover='event.cancelBubble = true' ";
                else return string.Empty;
            }
        }
        #endregion
    }

    #endregion

    /// <summary>extensions methods for the module</summary>
    public static class ModExtensions
    {
        public static readonly string ModuleName;
        public static readonly string ModuleNameSpace;
        public static readonly string PaymentNameSpace;

        #region ModExtensions

        static ModExtensions()
        {
            var name = typeof(ModExtensions).Module.Name;

            int iL = name.LastIndexOf('.');
            int iM = name.LastIndexOf('.', iL - 1);

            if (iL != -1 && iM != -1) name = name.Substring(iM + 1, iL - iM - 1);
            ModuleName = "Eshop";//name.ToLower();
            ModuleNameSpace = typeof(ModExtensions).Namespace;
            PaymentNameSpace = typeof(BasePaymentProcessor).Namespace;

            if (PaymentNameSpace != null && 0 < (iL = PaymentNameSpace.LastIndexOf('.')))
            {
                PaymentNameSpace = PaymentNameSpace.Substring(0, iL);
            }
        }

        #endregion

        #region GetController
        /// <summary>return current controller name</summary>
        public static string GetController(this HtmlHelper html)
        {
            return (string)html.ViewContext.HttpContext.Request.RequestContext.RouteData.Values["controller"];
        }
        #endregion

        #region ModLocalize
        /// <summary>return phrase text value in current language from phrase code that scoped in this module</summary>
        public static string ModLocalize(this string value, string defValue = null, bool admin = true)
        {
            var key = ((admin ? "eshop_adm_" : "eshop_fe_") + value.Replace(' ', '_').ToLower());
            if (HttpContext.Current == null)
            {
                if (IoC.Kernel == null) return defValue ?? value;

                var db = new DefaultDb(new DataService(IoC.Kernel.Get<ISession>()));
                var langCode = ClawConfiguration.GetConfig().AdminLanguage;
                var lst = db.DataService.GetList<Phrase>(/*BaseMgr.GetSiteId(db)*/0, "PhraseName", key);
                var itm = lst.MatchOrFirst(x => x.PhraseType.EqualsIgCase(ModuleName));
                if (itm == null)
                {
                    var p = new Phrase(){ PhraseName = key, PhraseType = ModuleName.ToLower(), Site = db.DataService.GetCurrentSite()};
                    p.Phrasecms_PhraseLanguageses = new[] {new PhraseLanguage() {Phrase = p,LanguageCode = langCode, PhraseValue = defValue ?? value}};
                    try { p.SaveorUpdate(db.DataService, true); }
                    catch (Exception err) { 
                        Claw.Core.Logs.LogHelper.Log4net(new Exception($"val={value}, mod={ModuleName}, defVal={defValue}, err={err.Message}", err), ClawExceptionType.InsertOrUpdateObject); 
                    }
                    return defValue ?? value;
                }
                else return itm.Phrasecms_PhraseLanguageses
                        .Cast<PhraseLanguage>().ToList()
                        .MatchOrFirst(x => x.LanguageCode.EqualsIgCase(langCode)).SafeProp(x => x.PhraseValue);
            }
            else return (key + "," + ModuleName).ToLower().Localize(defValue ?? value);
        }

        #endregion

        #region GetLocalize
        /// <summary>return phrase text value in current language from a property of model that scoped in this module</summary>
        public static string GetLocalize(string model, string name, string value, bool admin = true)
        {
            return ((admin ? "eshop_adm_" : "eshop_fe_") + model + "_" + name + "," + ModuleName).ToLower().Localize(value);
        }

        #endregion

        #region GlobalLocalize
        /// <summary>return phrase text value in current language from phrase code that is in global scope</summary>
        public static string GlobalLocalize(this string value, string text = null)
        {
            return ("eshop_global_" + value.Replace(' ', '_')).ToLower().Localize(text ?? value);
        }

        #endregion

        #region ModErrLocalize
        /// <summary>return phrase text value in current language from error message phrase that scoped in this module</summary>
        public static string ModErrLocalize(this string value)
        {
            if (string.IsNullOrEmpty(value)) return value;

            const string patt = "eshop_adm_", pattFe = "eshop_fe_";        

            if (value.StartsWith("eshop_adm_errmsg_"))
            {
                int i = value.IndexOf(',');
                if (i != -1 && i + 1 < value.Length)
                    return value.Substring(patt.Length, i - patt.Length).Trim()
                        .ModLocalize(value.Substring(i + 1, value.Length - i - 1).Trim());
            }

            if (value.StartsWith("eshop_fe_errmsg_"))
            {
                int i = value.IndexOf(',');
                if (i != -1 && i + 1 < value.Length)
                    return value.Substring(pattFe.Length, i - pattFe.Length).Trim()
                        .ModLocalize(value.Substring(i + 1, value.Length - i - 1).Trim());
            }

            return value;
        }

        #endregion

        #region ModLocalize.ModelState
        /// <summary>convert error message phrases to current language</summary>
        public static ModelStateDictionary ModLocalize(this ModelStateDictionary modelState)
        {
            var errList = new List<KeyValuePair>();

            foreach (var item in modelState)
                foreach (var err in item.Value.Errors)
                {
                    errList.Add(new KeyValuePair() { Key = item.Key, Value = err.ErrorMessage.ModErrLocalize() });
                }

            errList.Reverse();

            foreach (var item in errList)
            {
                modelState.Remove(item.Key);
                modelState.AddModelError(item.Key, HttpUtility.HtmlDecode((string)item.Value));
            }

            return modelState;
        }

        #endregion

        #region Merge.ModelState
        /// <summary>copy errors from source to target</summary>
        public static void Merge(this ModelStateDictionary target, ModelStateDictionary source)
        {
            foreach (var item in source)
                foreach (var err in item.Value.Errors)
                {
                    source.AddModelError(item.Key, err.ErrorMessage);
                }
        }

        #endregion

        #region FirstErrorMsg.ModelState
        /// <summary>return first error message</summary>
        public static string FirstErrorMsg(this ModelStateDictionary modelState)
        {
            return modelState.SelectMany(m => m.Value.Errors).Select(m => m.ErrorMessage).FirstOrDefault();
        }

        #endregion

        #region RemoveKey.ModelState

        /// <summary>remove items in modelState by prefix and shift indexes down 1 unit to remove the gap</summary>
        public static void RemoveKey(this ModelStateDictionary modelState, string prefix,
            int index = -1, int maxIndex = -1, bool matchExact = false, int? staticPrefixBackOffset = null)
        {
            var staticPbo = staticPrefixBackOffset;
            var staticPrefix = staticPbo == null ? null : prefix.Substring(0, prefix.Length - (staticPbo ?? 0));
            var keyList = staticPrefix == null
                ? modelState.Keys.ToList()
                : modelState.Keys.Where(x => x.StartsWith(staticPrefix)).ToList();

            var dkey = (index == -1 ? prefix : string.Format(prefix, index));
            int remCount = 0;
            foreach (var key in keyList.Where(x => matchExact ? x.EqualsIgCase(dkey) : x.StartsWith(dkey))
                .OrderBy(x => x).ToArray())
            {
                modelState.Remove(key); keyList.Remove(key); remCount++;
            }

            //shift indexes down by 1
            if (remCount != 0 && index != -1 && maxIndex != -1)
                for (int i = index + 1; i <= maxIndex; i++)
                {
                    string d2Key = string.Format(prefix, i), n2Key = string.Format(prefix, i - 1);
                    foreach (var m2Key in keyList.Where(x => x.StartsWith(d2Key)).ToArray())
                    {
                        var val = modelState[m2Key];
                        if (val == null && Debugger.IsAttached) Debugger.Break();
                        var newKey = m2Key.Replace(d2Key, n2Key);
                        modelState.Add(newKey, val); keyList.Add(newKey);
                        //modelState.RemoveKey(m2Key, -1, -1, true);
                        modelState.Remove(m2Key); keyList.Remove(m2Key);
                    }
                }
        }

        #endregion

        #region AddModelError
        /// <summary>add an error to modelState</summary>
        public static void AddModelError<TModel, TVal>(this ModelStateDictionary modelState,
            Expression<Func<TModel, TVal>> field, TModel model, string errorMsg)
        {
            modelState.AddModelError(((MemberExpression)field.Body).Member.Name, errorMsg);
        }

        #endregion

        #region TransformModelError
        /// <summary>convert error message phrases to current language and translate phrases by fTransform</summary>
        public static void TransformModelError<TModel>(this ModelStateDictionary modelState,
            Func<string, string, ModelError, string> fTransform)
        {
            var type = typeof(TModel);
            foreach (var key in modelState.Keys.Where(x => !string.IsNullOrEmpty(x)))
            {
                var mstate = modelState[key];
                foreach (var err in mstate.Errors.ToArray())
                {
                    var displayName = GetLocalize(TrimModel(type.Name), key, key);
                    var newVal = fTransform(key, displayName, err);
                    if (err.ErrorMessage != newVal)
                    //if (err.ErrorMessage != displayName)
                    {
                        mstate.Errors.Remove(err);
                        //mstate.Errors.Add(displayName);
                        mstate.Errors.Add(newVal);
                    }
                }
            }
        }

        #endregion

        #region LocalizeNameFor
        /// <summary>return display name of a property</summary>
        public static string LocalizeNameFor<TModel, TValue>(this HtmlHelper<IEnumerable<TModel>> html,
            Expression<Func<TModel, TValue>> expression)
        {
            return GetDisplayName(expression);
        }
        /// <summary>return display name of a property</summary>
        public static string LocalizeNameFor<TModel, TValue>(this HtmlHelper<TModel> html,
            Expression<Func<TModel, TValue>> expression)
        {
            return GetDisplayName(expression);
        }
        /// <summary>return display name of a property</summary>
        public static string LocalizeNameFor<TModel, TValue>(this HtmlHelper<TModel> html,
            Expression<Func<TModel, TValue>> expression, bool admin = true)
        {
            return GetDisplayName(expression, admin);
        }
        #endregion

        #region LocalizeName
        /// <summary>return display name of a property</summary>
        /// <param name="model">the property of this model</param>
        /// <param name="expression">property selector</param>
        public static string LocalizeName<T, TModel, TValue>(this HtmlHelper<T> html, TModel model,
            Expression<Func<TModel, TValue>> expression, bool admin = true)
        {
            return GetDisplayName(expression, admin);
        }

        #endregion
        /// <summary></summary>
        #region LocalizeTextBoxFor

        public static MvcHtmlString LocalizeTextBoxFmFor<TModel>(this HtmlHelper<TModel> htmlHelper,
            Expression<Func<TModel, OMoney>> expression, Object htmlAttributes, bool kendoValidate = false, bool admin = true)
        {
            var metaData = ModelMetadata.FromLambdaExpression(expression, htmlHelper.ViewData);
            var expModel = (OMoney)metaData.Model;

            if (expModel != null) expModel.ForceFullMode = true;

            var ret = LocalizeTextBoxForEx(htmlHelper, expression, htmlAttributes, kendoValidate, admin);

            if (expModel != null) expModel.ForceFullMode = false;

            return ret;
        }

        public static MvcHtmlString LocalizeTextBoxFor<TModel, TProperty>(this HtmlHelper<TModel> htmlHelper,
            Expression<Func<TModel, TProperty>> expression, Object htmlAttributes, bool kendoValidate = false, bool admin = true)
        {
            return LocalizeTextBoxForEx(htmlHelper, expression, htmlAttributes, kendoValidate, admin);
        }

        public static MvcHtmlString LocalizeTextBoxFor<TModel, TProperty>(
            this HtmlHelper<IEnumerable<TModel>> htmlHelper,
            Expression<Func<TModel, TProperty>> expression, Object htmlAttributes, bool kendoValidate = false, bool admin = true)
        {
            return LocalizeTextBoxForEx(htmlHelper, expression, htmlAttributes, kendoValidate, admin);
        }

        static MvcHtmlString LocalizeTextBoxForEx<TModel, TProperty>(HtmlHelper htmlHelper,
            Expression<Func<TModel, TProperty>> expression, Object htmlAttributes, bool kendoValidate = false, bool admin = true)
        {
            var dicAttrs = TransformAttributes(htmlAttributes, expression, kendoValidate);
            var linqExpr = (MemberExpression)expression.Body;

            if (typeof(TProperty) == typeof(int) || typeof(TProperty) == typeof(int?))
            {
                dicAttrs["data-val-number"] = StringTable.ErrMsgRequireNumber;
            }
            else if (typeof(TProperty) == typeof(OFloat) || typeof(TProperty) == typeof(OMoney))
            {
                dicAttrs["data-val-number"] = StringTable.ErrMsgRequireNumber;
            }

            var attr = linqExpr.Member
                .GetCustomAttributes(typeof(StringLengthAttribute), false)
                .Cast<StringLengthAttribute>().FirstOrDefault();

            if (attr != null) dicAttrs["maxlength"] = attr.MaximumLength + 10;

            var inputHelper = htmlHelper as HtmlHelper<TModel>;
            var mvcVal = inputHelper != null
                ? inputHelper.TextBoxFor(expression, dicAttrs)
                : ((HtmlHelper<IEnumerable<TModel>>)htmlHelper).TextBoxFor(expression, dicAttrs);

            return LocalizeControl(mvcVal, false, admin);
        }

        #endregion

        #region LocalizeTextAreaFor

        public static MvcHtmlString LocalizeTextAreaFor<TModel, TProperty>(this HtmlHelper<TModel> htmlHelper,
            Expression<Func<TModel, TProperty>> expression, Object htmlAttributes, bool kendoValidate = false)
        {
            return LocalizeTextAreaForEx(htmlHelper, expression, htmlAttributes, kendoValidate);
        }

        public static MvcHtmlString LocalizeTextAreaForEx<TModel, TProperty>(this HtmlHelper htmlHelper,
            Expression<Func<TModel, TProperty>> expression, Object htmlAttributes, bool kendoValidate = false)
        {
            var dicAttrs = TransformAttributes(htmlAttributes, expression, kendoValidate);
            var linqExpr = (MemberExpression)expression.Body;

            var attr = linqExpr.Member
                .GetCustomAttributes(typeof(StringLengthAttribute), false)
                .Cast<StringLengthAttribute>().FirstOrDefault();

            if (attr != null) dicAttrs["maxlength"] = attr.MaximumLength + 10;

            var inputHelper = htmlHelper as HtmlHelper<TModel>;
            var mvcVal = inputHelper != null
                ? inputHelper.TextAreaFor(expression, dicAttrs)
                : ((HtmlHelper<IEnumerable<TModel>>)htmlHelper).TextAreaFor(expression, dicAttrs);

            return LocalizeControl(mvcVal);
        }

        #endregion

        #region LocalizeDateTextBoxFor

        public static MvcHtmlString LocalizeDateTextBoxFor<TModel>(this HtmlHelper<TModel> htmlHelper,
            Expression<Func<TModel, ODateTime>> exprParent, int modelId, Object htmlAttributes = null, bool admin = true)
        {
            string format = ODateTime.FormatString(modelId);

            var attrs = TransformAttributes(htmlAttributes) ?? new RouteValueDictionary();

            attrs.Append("class", "datepicker");

            const string afkey = "data-val-remote-additionalfields";
            var parAttrs = htmlHelper.GetUnobtrusiveValidationAttributes(exprParent);

            if (parAttrs.ContainsKey(afkey))
                parAttrs[afkey] = Regex.Replace((string)parAttrs[afkey], @"(^|,)\*\.\:\:", "$1", RegexOptions.Compiled);

            attrs.Merge(parAttrs);

            attrs["data-val-date"] = StringTable.ErrMsgRequireDate;

            var expression = Expression.Lambda(Expression.Property(exprParent.Body, typeof(ODateTime),
                GetFieldName<ODateTime, DateTime?>(m => m.Value)), exprParent.Parameters)
                as Expression<Func<TModel, DateTime?>>;

            return LocalizeControl(htmlHelper.TextBoxFor(expression, "{0:" + format + "}", attrs), admin);
        }

        #endregion

        #region LocalizeDateTimeTextBoxFor

        public static MvcHtmlString LocalizeDateTimeTextBoxFor<TModel>(this HtmlHelper<TModel> htmlHelper,
            Expression<Func<TModel, ODateTime>> exprParent, int modelId, bool shortTime = true, Object htmlAttributes = null)
        {
            string format = ODateTime.FormatString(modelId, true, shortTime);

            var attrs = TransformAttributes(htmlAttributes) ?? new RouteValueDictionary();

            attrs.Append("class", "datetimepicker");

            const string afkey = "data-val-remote-additionalfields";
            var parAttrs = htmlHelper.GetUnobtrusiveValidationAttributes(exprParent);

            if (parAttrs.ContainsKey(afkey))
                parAttrs[afkey] = Regex.Replace((string)parAttrs[afkey], @"(^|,)\*\.\:\:", "$1", RegexOptions.Compiled);

            attrs.Merge(parAttrs);

            attrs["data-val-date"] = StringTable.ErrMsgRequireDate;

            var expression = Expression.Lambda(Expression.Property(exprParent.Body, typeof(ODateTime),
                GetFieldName<ODateTime, DateTime?>(m => m.Value)), exprParent.Parameters)
                as Expression<Func<TModel, DateTime?>>;

            return LocalizeControl(htmlHelper.TextBoxFor(expression, "{0:" + format + "}", attrs));
        }

        #endregion

        #region LocalizeRadioButtonFor

        public static MvcHtmlString LocalizeRadioButtonFor<TModel, TProperty>(this HtmlHelper<TModel> htmlHelper,
            Expression<Func<TModel, TProperty>> expression, object value, Object htmlAttributes, bool admin = true)
        {
            var dicAttrs = TransformAttributes(htmlAttributes, expression);

            return LocalizeControl(htmlHelper.RadioButtonFor(expression, value, dicAttrs), admin: admin);
        }

        #endregion

        #region LocalizeCheckBoxFor

        public static MvcHtmlString LocalizeCheckBoxFor<TModel, TProp>(this HtmlHelper<TModel> htmlHelper,
            Expression<Func<TModel, TProp>> expression, Object htmlAttributes)
        {
            var ret = new TagBuilder("input");

            ret.Attributes.Add("type", "checkbox");
            ret.Attributes.Add("id", htmlHelper.IdFor(expression).ToString());
            ret.Attributes.Add("name", GetFieldName(expression));

            var val = expression.Compile().Invoke(htmlHelper.ViewData.Model);

            if (val.Equals(true))
            {
                ret.Attributes.Add("checked", "checked");
            }

            if (htmlAttributes != null) ret.MergeAttributes(TransformAttributes(htmlAttributes));

            return new MvcHtmlString(ret.ToString());
        }

        #endregion

        #region LocalizeBoolInputFor

        public static DropDownListBuilder LocalizeBoolInputFor<TModel>(this HtmlHelper<TModel> htmlHelper,
            Expression<Func<TModel, bool?>> expression, string optionLabel, Object htmlAttributes)
        {
            return htmlHelper.LocalizeDropDownListKendoFor(expression, new[]
            {
                new SelectListItem() {Value = "True", Text = StringTable.Yes},
                new SelectListItem() {Value = "False", Text = StringTable.No}
            }, optionLabel, htmlAttributes);
        }

        #endregion

        #region LocalizeDropDownListFor

        public static MvcHtmlString LocalizeDropDownListFor<TModel, TProperty>(this HtmlHelper<TModel> htmlHelper,
            Expression<Func<TModel, TProperty>> expression, IEnumerable<SelectListItem> selectList, string optionLabel, Object htmlAttributes, bool admin = true)
        {
            return LocalizeControl(htmlHelper.DropDownListFor(expression, selectList, optionLabel, TransformAttributes(htmlAttributes)), false, admin);
        }

        public static MvcHtmlString LocalizeDropDownListFor<TModel, TProperty>(this HtmlHelper htmlHelper, TModel model,
            Expression<Func<TModel, TProperty>> expression, IEnumerable<SelectListItem> selectList,
            string optionLabel, Object htmlAttributes, bool admin = true)
        {
            return LocalizeControl(htmlHelper.DropDownList(GetFieldName(expression), selectList, optionLabel, TransformAttributes(htmlAttributes)), false, admin);
        }

        #endregion

        #region LocalizePasswordFor

        public static MvcHtmlString LocalizePasswordFor<TModel, TProperty>(this HtmlHelper<TModel> htmlHelper,
            Expression<Func<TModel, TProperty>> expression, Object htmlAttributes, bool admin = true)
        {
            return LocalizeControl(htmlHelper.PasswordFor(expression, TransformAttributes(htmlAttributes)), false, admin);
        }

        #endregion

        #region LocalizeDropDownListKendoFor

        public static DropDownListBuilder LocalizeDropDownListKendoFor<TModel, TProperty>(this HtmlHelper<TModel> htmlHelper,
            Expression<Func<TModel, TProperty>> expression, IEnumerable<SelectListItem> selectList, string optionLabel,
            Object htmlAttributes)
        {
            return LocalizeDropDownListKendoFor<TModel, TProperty, SelectListItem>(htmlHelper, expression, selectList,
                m => m.Value, m => m.Text,
                optionLabel == null ? null : new SelectListItem() { Value = null, Text = optionLabel }, htmlAttributes);
        }

        public static DropDownListBuilder LocalizeDropDownListKendoLangFor<TModel, TProperty>(
            this HtmlHelper<TModel> htmlHelper,
            Expression<Func<TModel, TProperty>> expression, IEnumerable<DDListItem> selectList, string optionLabel,
            Object htmlAttributes)
        {
            return LocalizeDropDownListKendoFor<TModel, TProperty, DDListItem>(htmlHelper, expression, selectList,
                m => m.Value, m => m.Text, new DDListItem() { Text = optionLabel, Value = null }, htmlAttributes)
                .Template(DDListItem.Template);
        }

        public static DropDownListBuilder LocalizeDropDownListKendoFor<TModel, TProperty, TItem>(
            this HtmlHelper<TModel> htmlHelper,
            Expression<Func<TModel, TProperty>> expression, IEnumerable<TItem> items,
            Expression<Func<TItem, string>> exprData, Expression<Func<TItem, string>> exprText,
            TItem optionLabel = null, Object htmlAttributes = null, string databoundHandler = null,
            bool animation = false)
            where TItem : class
        {
            var ret = htmlHelper.Kendo().DropDownListFor(expression);

            if (optionLabel != null) ret.OptionLabel(optionLabel);

            ret.Events(events =>
            {
                events.Change("function(e) {this.element.valid();}");

                events.DataBound(@"function(e) {
                        var data = this.dataSource.data(), opts = this.options;
                        if(!opts.optionLabel && opts.optOldLabel) opts.optionLabel = opts.optOldLabel;
                        if(data.length == 0 && opts.optionLabel) {
                            data.push(opts.optOldLabel = opts.optionLabel); 
                            opts.optionLabel = null; this.refresh();
                        } 

                        var popup = e.sender.popup.element, dropdown = e.sender.element.closest('.k-dropdown');
                        popup.css('width', '');
                        var width = Math.min(350, parseInt(popup.css('width')) + 60);
                        if (width && width > 100) dropdown.css('width', width + 'px');
                        " + (string.IsNullOrEmpty(databoundHandler) ? "" : databoundHandler + "(this)") + @"
                    }");

                events.Open(@"function(e) {
                        var popup = e.sender.popup.element;
                        if (!popup.attr('x-width')) {
                            popup.css('width', '');
                            var width = Math.min(800, parseInt(popup.css('width')) + 20);
                            popup.css('width', width + 'px'); popup.attr('x-width', width);
                        } else popup.css('width', popup.attr('x-width') + 'px');
                    }");

                //if (items != null && items.Any() && items.First().GetType() == typeof(DDListItemGroup))
                //    events.Select(@"function(e) {
                //         if(e.item.find('.group').length != 0) {e.preventDefault(); } 
                //    }");
            });

            if (items != null) ret.BindTo(items);

            var attrs = TransformAttributes(htmlAttributes) ?? new RouteValueDictionary();
            var reqAttr = GetAttribute<RequiredAttribute, TModel, TProperty>(expression);
            var remoteAttr = GetAttribute<RemoteAttribute, TModel, TProperty>(expression);

            if (reqAttr != null) attrs["data-val-required"] = reqAttr.ErrorMessage.ModErrLocalize();
            if (remoteAttr != null) attrs["data-val-remote"] = remoteAttr.ErrorMessage.ModErrLocalize();

            attrs.Append("class", "kendo-dropdownlist");

            ret.DataValueField(GetFieldName(exprData))
                .DataTextField(GetFieldName(exprText))
                .HtmlAttributes(attrs)
                .Animation(animation);

            return ret;
        }

        public static DropDownListBuilder LocalizeDropDownListActiveItemKendoFor<TModel, TProperty, TItem>(
            this HtmlHelper<TModel> htmlHelper,
            Expression<Func<TModel, TProperty>> expression, IEnumerable<TItem> items,
            Expression<Func<TItem, string>> exprData, Expression<Func<TItem, string>> exprText,
            TItem optionLabel = null, Object htmlAttributes = null, string databoundHandler = null,
            bool animation = false)
            where TItem : class
        {
            var ret = htmlHelper.Kendo().DropDownListFor(expression);

            if (optionLabel != null) ret.OptionLabel(optionLabel);

            ret.Template("<span class='#: Active ? '': 'k-state-disabled'#'>#: ValTxt #</span>");

            ret.Events(events =>
            {
                events.Change("function(e) {this.element.valid(); }");

                events.Select("function(e){var dataItem = this.dataItem(e.item.index());if(!dataItem.Active){e.preventDefault();};}");

                events.DataBound(@"function(e) {
                        var data = this.dataSource.data(), opts = this.options;
                        if(!opts.optionLabel && opts.optOldLabel) opts.optionLabel = opts.optOldLabel;
                        if(data.length == 0 && opts.optionLabel) {
                            data.push(opts.optOldLabel = opts.optionLabel); 
                            opts.optionLabel = null; this.refresh();
                        } 

                        var popup = e.sender.popup.element, dropdown = e.sender.element.closest('.k-dropdown');
                        popup.css('width', '');
                        var width = Math.min(350, parseInt(popup.css('width')) + 60);
                        if (width && width > 100) dropdown.css('width', width + 'px');
                        " + (string.IsNullOrEmpty(databoundHandler) ? "" : databoundHandler + "(this)") + @"
                    }");

                events.Open(@"function(e) {
                        var popup = e.sender.popup.element;
                        if (!popup.attr('x-width')) {
                            popup.css('width', '');
                            var width = Math.min(800, parseInt(popup.css('width')) + 20);
                            popup.css('width', width + 'px'); popup.attr('x-width', width);
                        } else popup.css('width', popup.attr('x-width') + 'px');
                    }");
            });

            if (items != null) ret.BindTo(items);

            var attrs = TransformAttributes(htmlAttributes) ?? new RouteValueDictionary();
            var reqAttr = GetAttribute<RequiredAttribute, TModel, TProperty>(expression);
            var remoteAttr = GetAttribute<RemoteAttribute, TModel, TProperty>(expression);

            if (reqAttr != null) attrs["data-val-required"] = reqAttr.ErrorMessage.ModErrLocalize();
            if (remoteAttr != null) attrs["data-val-remote"] = remoteAttr.ErrorMessage.ModErrLocalize();

            attrs.Append("class", "kendo-dropdownlist");

            ret.DataValueField(GetFieldName(exprData))
                .DataTextField(GetFieldName(exprText))
                .HtmlAttributes(attrs)
                .Animation(animation);

            return ret;
        }

        public static DropDownListBuilder LocalizeDropDownListKendoLangFor<TModel, TProp>(this HtmlHelper<TModel> htmlHelper,
            Expression<Func<TModel, TProp>> expression, string actionList, string controller, string cascadeFrom = null,
            string optionLabel = null, Object htmlAttributes = null, string databoundHandler = null)
        {
            var metadata = ModelMetadata.FromLambdaExpression(expression, htmlHelper.ViewData);
            return LocalizeDropDownListKendoLang(htmlHelper, htmlHelper.GetHtmlFieldName(expression), metadata.Model,
                actionList, controller, cascadeFrom, optionLabel, htmlAttributes, databoundHandler);
        }
        #endregion

        #region LocalizeDropDownListKendoLang

        public static DropDownListBuilder LocalizeDropDownListKendoLang(this HtmlHelper htmlHelper,
            string name, object value, IEnumerable<DDListItem> items,
            string optionLabel = null, Object htmlAttributes = null, bool resizeWidth = true)
        {
            return LocalizeDropDownListKendo<DDListItem>(htmlHelper, name, value, items, m => m.Value, m => m.Text,
                optionLabel == null ? null : new DDListItem() { Text = optionLabel, Value = null }, htmlAttributes, null, false, resizeWidth)
                .Template(DDListItem.Template);
        }

        public static DropDownListBuilder LocalizeDropDownListKendoLang(this HtmlHelper htmlHelper,
            string name, object value, string actionList, string controller, string cascadeFrom = null,
            string optionLabel = null, Object htmlAttributes = null, string databoundHandler = null, 
            string cascadeFromName = null)
        {
            var ret = LocalizeDropDownListKendo<DDListItem>(htmlHelper, name, value, null,
                m => m.Value, m => m.Text, optionLabel == null ? null : new DDListItem() { Text = optionLabel, Value = null },
                htmlAttributes, databoundHandler)
                .Template(DDListItem.Template)
                .DataSource(ds => ds.Read(crud =>
                {
                    crud.Action(actionList, controller);
                    if (cascadeFrom != null)
                        crud.Data("function(){ return {" + (cascadeFromName ?? cascadeFrom) + ":$('#" + cascadeFrom + "').val(), lang: '" + LanguageMgr.FocusLangCode + @"'}; }");
                }).ServerFiltering(true));

            if (cascadeFrom != null) ret.CascadeFrom(cascadeFrom);

            return ret;
        }

        #endregion

        #region LocalizeDropDownListKendo

        public static DropDownListBuilder LocalizeDropDownListKendo(this HtmlHelper htmlHelper,
            string name, object value, IEnumerable<SelectListItem> selectList, string optionLabel,
            Object htmlAttributes)
        {
            return LocalizeDropDownListKendo<SelectListItem>(htmlHelper, name, value, selectList,
                m => m.Value, m => m.Text,
                optionLabel == null ? null : new SelectListItem() { Value = null, Text = optionLabel }, htmlAttributes);
        }

        public static DropDownListBuilder LocalizeDropDownListKendo<TItem>(this HtmlHelper htmlHelper,
            string name, object value, IEnumerable<TItem> items,
            Expression<Func<TItem, string>> exprData, Expression<Func<TItem, string>> exprText,
            TItem optionLabel = null, Object htmlAttributes = null, string databoundHandler = null,
            bool animation = false, bool resizeWidth = true)
            where TItem : class
        {
            var ret = htmlHelper.Kendo().DropDownList();

            if (optionLabel != null) ret.OptionLabel(optionLabel);

            ret.Events(events =>
                events.DataBound(@"function(e) {
                        var data = this.dataSource.data(), opts = this.options;
                        if(!opts.optionLabel && opts.optOldLabel) opts.optionLabel = opts.optOldLabel;
                        if(data.length == 0 && opts.optionLabel) {
                            data.push(opts.optOldLabel = opts.optionLabel); 
                            opts.optionLabel = null; this.refresh();
                        } 

                        if(" + (resizeWidth ? 1 : 0) + @" == 1) {
                            var popup = e.sender.popup.element, dropdown = e.sender.element.closest('.k-dropdown');
                            popup.css('width', '');
                            var width = Math.min(350, parseInt(popup.css('width')) + 60);
                            if (width && width > 100) dropdown.css('width', width + 'px');
                            " + (string.IsNullOrEmpty(databoundHandler) ? "" : databoundHandler + "(this)") + @"
                        }
                    }")
                    .Open(@"function(e) {
                        var popup = e.sender.popup.element;
                        if (!popup.attr('x-width')) {
                            popup.css('width', '');
                            var width = Math.min(800, parseInt(popup.css('width')) + 20);
                            popup.css('width', width + 'px'); popup.attr('x-width', width);
                        } else popup.css('width', popup.attr('x-width') + 'px');
                    }"));

            if (items != null) ret.BindTo(items);

            ret.Name(name)
                .HtmlAttributes(htmlAttributes)
                .Animation(animation)
                .DataValueField(GetFieldName(exprData))
                .DataTextField(GetFieldName(exprText));

            if (value != null) ret.Value(value.ToString());

            return ret;
        }

        #endregion

        #region LocalizeColorPickerFor

        public static ColorPickerBuilder LocalizeColorPickerFor<TModel, TProperty>(this HtmlHelper<TModel> htmlHelper,
            Expression<Func<TModel, TProperty>> expression, string selectHandler = null)
            where TModel : class
        {
            var name = GetFieldName(expression);
            return htmlHelper.Kendo().ColorPicker()
                .Name(name)
                .Value((htmlHelper.ViewData.Eval(name) ?? "").ToString())
                .Events(events =>
                {
                    if (!string.IsNullOrEmpty(selectHandler)) events.Select(selectHandler);
                })
                .HtmlAttributes(new { @id = htmlHelper.IdFor(expression).ToString() });

        }

        #endregion

        #region LocalizeImageLoading

        public static MvcHtmlString LocalizeImageLoading(this HtmlHelper htmlHelper, Object htmlAttributes = null)
        {
            var img = new TagBuilder("img");

            //img.MergeAttribute("src",
            //    "data:image/gif;base64,R0lGODlhEAAQAPfiAL+7uouEgHFoY52XlMnGxXx0b6+rqcfEw7y4t21kX3JpZMC8u4iBfXtybqynpXJpZXpybXRsZ3RrZnpxbY+IhL66uX93c5qUkcjFxIqDf2xjXrKurHtzbsG+vHlxbHFpZK+rqKKcmqGbmH93cnJqZXNrZnNqZoeAfG5lYcG+vWphXImCfn52crOurKGbmaOem4iAfXJqZqynpI6IhLu3tZmSj6Gcma2oppqTkLi0spKLiJ+ZlpCJhb25uG5lYLu3tpmTkMK+vXdvanBnYpyWk5OMiYF6drSvrYZ/e46Hg7aysHlxbXhwa4F5dbq2ta6qqLeysbWxr6Cal6umpHFoZGtiXX11caqloo6HhG1kYH94dHZuaYV+esfDwoR9eX94c7OvrrSwr4iAfJyWlMO/vpGKhsC8uraxsLGtqnFpZZCJhr+8u3BnY7Wwrr67usC9vHVsZ8K9vJaQjYuDf5uVkpWPjKSfnKikoq2pp2xkYIR8eIV9eYB5dHRrZ7GsqmxkX7Gtq8jFw42GgrOvrLy3tr+8usG9vIJ6dry4tpSNiqeioKmkoZCKh2lgW4J7d7Ktq5eRjn53c6WfnXZtaa+rqnRtaHtzb5iRjqejoaCamLKuq4R9eq+qqMK/vrm2tLSxrrOwrcC7urSwrqKdmrezsXBoY6qlo5GLiJuVk6ijoHlybaCbmJONiYV+e6ain6mlonlwbJyVk7GtrKmkopSOi3hwbJWQjZ6ZlpCIhYF6daSem66pp7u2tbizsm9mYm1lYI2Gg6OenKCbmcK/vX52cXt0b4iBfp2YlcC9u5aRjpqTkaSenI+Ihbm0s5iSkKejn6ulo3NrZ7OurW5mYZ+Zl4eBfYyFgXVtaYmCf5GKh4uEf3VtaGphXaOfnHZuaqmlo6Kem7WyrwAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAACH/C05FVFNDQVBFMi4wAwEAAAAh+QQFBQDiACwAAAAAEAAQAAAInQDFCRwoLlwKgggJpoJyAEPCgZaMiVtUgcABcc1oIPwVQU8icTQIiCtEaAEAglViQGBA0AwyAD0QDlGQEAACgioSPByoaYM4bhoECNgJ7UaLDSioPHiwE9MTAwbESZCwU+AUGQRLREjoLBPCUhFgFSCIq46IFwQreSgwQlwkLuJ0XBggBWEBFuK0OMrAQ9wlOlUPwQhAoSpBBgEeBgQAIfkEBQUA4gAsAAAAABAAEAAACJwAxQkcKE7DNYIICTb640FMwoFVNIhLIABCBnHdwCDUkOeDAHFDIIjbYaADAYK+xD34OPAVAAIYEMKRkBAmQSoRHg4EYEZciQgTPOjsRSNIByEcChTQCaUCAADiRljQKfAHAoIWDiVchAahFSMnAhAUpmhDG4KOGASgII6ZHHEvHBh4hDBAEnGMWBEJIe7KDapFcAywQZXghQEPAwIAIfkEBQUA4gAsAAAAABAAEAAACJwAxQkcKO4PCoIICaJAQUVAwoHRIogjIeGBQxUaEG7Z4oGJuD4PxCXQEKMBQQhWCkwg6ONDgz0I+YxI2IABQRZNHg4McUVcEyMwTuh0IKrCAiQZAgTQ+QYDAQLiZlDQKfDAAYJqiiRUUgEhMB1ABhCkdKYDGYJFLgwQIW4VJnFHEAAwhHCAFHEhJD05Is5JD6p2ZBhoQZWgAwMPAwIAIfkEBQUA4gAsAAAAABAAEAAACJ0AxQkcKM6bEIIICS6pxWFCwoG5jIhjYaHAEnHT2CDc5IUBEnFfCogrIUCcAoIZBAVgQJCECQUfEJaZkVBByYFJdDwcmMCHuGxFgNTYqUKDAAWQiAwYsHPShAYNxIlwsVMgljkEbdhJaGAWwlu6poAg+GPNAiUEJTkAsUEcJSjiFgQi0AkhiEfiBoWpEERcFwxVoyACkKIqQQQAHgYEACH5BAUFAOIALAAAAAAQABAAAAidAMUJHCiOCxKCCAmuqJaBQcKBp3SIo0AhAAxxljggtFXnwiVxPAKI01LMSgGCqHYMuEDQArECEBC+EJGwwASC1F48HFiChLhRy2RM2YmCjQQJd/AYMLAzS5oHD8RtaLFToAABBDVFSdgoAcInYH4AINgqgoIhBMMgANBBHKE34mowgGACIQBD4gotwEBDXKI9VeMcIFChKkEMBB4GBAAh+QQFBQDiACwAAAAAEAAQAAAImwDFCRwoLhkkgggJXgBC5ELCgeCCictkYwAOcdoCIHSlyMEVcSEGiGNkTZDGgQ78GHBAkEeSACsQttmQMAADgpyOPBxowYI4QKAQ/NipioOFETl6AACwUwiHAgXEdQiyU6CHCQQBxEmYhQRCXqEOECCoIo8EOAQXiF0gLsA2cQkEPEhIYJi4HSs8DBEn4ENVHBkgCKhKUAyEhwEBACH5BAUFAOIALAAAAAAQABAAAAicAMUJHCju2R2CCAma+obHQcKBsqSJQ9PCgAxxscYg/HQGgRNxRwyIC3FsxwCCnhYAQEDQhZQBdBCS6ZBwwAWCThY8HKiGhzgEbg4c2LkiAIUZKQ4QILCTS4YAAcQtqLBT4AkYBEPgSMiEBUJaWE40IEhiywg+BLHpaaBAXJUs4iJMKOCVYAMT4hIkSNNHHBMIVX0IeCChKsHBDwMCACH5BAUFAOIALAAAAAAQABAAAAibAMUJHCguSg6CCAmSytEDQcKBCNyIq5ACACJxDm4gzLHmQBdxQQCIG7TLDwiCLVIQCERwAyAQGgkqWZAQhAOCFGo8HGjDhbhIRuZg2alsjAsRkyY0aLBTDpEBA8QpELBTYA0gBBP4SHiCAkIVCQQoIKjFy4wyWT8okCDugxBxTRgESIJwrLgSlTh8EYdkRdUISwpYqEpwQoGHAQEAOw==");
            img.MergeAttribute("src", "/Areas/admin/styles/images/loading_input.gif");

            if (htmlAttributes != null)
            {
                var attrs = new RouteValueDictionary(htmlAttributes);
                if (attrs.ContainsKey("style")) attrs["style"] = attrs["style"] + ";display:none";
                else attrs.Add("style", "display:none");
                img.MergeAttributes(attrs);
            }
            else img.MergeAttribute("style", "display:none");

            return MvcHtmlString.Create(img.ToString());
        }

        #endregion

        #region LocalizeControl

        public static MvcHtmlString LocalizeControl(this string htmlVal, bool removeLineBreak = false)
        {
            return MvcHtmlString.Create(htmlVal).LocalizeControl(removeLineBreak);
        }

        public static MvcHtmlString LocalizeControl(this MvcHtmlString mvcVal, bool removeLineBreak = false, bool admin = true)
        {
            var rawVal = mvcVal.ToString();

            StringBuilder ret = null;
            string patt = admin ? "\"eshop_adm_errmsg_" : "\"eshop_fe_errmsg_", ppre = admin ? "\"eshop_adm" : "\"eshop_fe";
            int i = 0, k, w, m, iOld = 0;
            while (i < rawVal.Length && (i = rawVal.IndexOf(patt, i)) != -1)
            {
                if (-1 != (m = rawVal.LastIndexOf(' ', i)) &&
                    rawVal.IndexOf("data-val", m, i - m, StringComparison.OrdinalIgnoreCase) == -1)
                {
                    i += patt.Length;
                    continue;
                }

                if (ret == null) ret = new StringBuilder(rawVal, iOld, i + 1, rawVal.Length);
                else ret.Append(rawVal, iOld, i - iOld + 1);
                if ((k = rawVal.IndexOfAny(new char[] { ',', '"' }, i + patt.Length)) != -1)
                {
                    if (rawVal[k] == ',')
                    {
                        if ((w = rawVal.IndexOf('"', k + 1)) != -1)
                        {
                            var name = rawVal.Substring(i + 1 + ppre.Length, k - i - 1 - ppre.Length).Trim();
                            var value = rawVal.Substring(k + 1, w - k - 1).Trim();
                            ret.Append(name.ModLocalize(HttpUtility.HtmlDecode(value)));
                            iOld = i = w;
                            continue;
                        }
                    }
                }
                iOld = i;
                break;
            }

            if (ret != null && iOld < rawVal.Length) ret.Append(rawVal, iOld, rawVal.Length - iOld);

            if (removeLineBreak)
            {
                return new MvcHtmlString((ret == null ? mvcVal.ToString() : ret.ToString()).Replace("\r\n", " "));
            }
            else return ret == null ? mvcVal : new MvcHtmlString(ret.ToString());
        }

        #endregion

        #region LocalizeGridFor

        public static KendoGrid<TModel> LocalizeGridFor<TModel>(this HtmlHelper<IEnumerable<TModel>> htmlHelper,
            string name, string controller, string deleteAction = "Delete", string editAction = null,
            string addAction = null, string editEventHandler = null, int? pageSize = null, string detailEventHandler = null)
            where TModel : class
        {
            return new KendoGrid<TModel>(htmlHelper, htmlHelper.Kendo().Grid<TModel>(), name, controller,
                deleteAction, editAction, addAction, editEventHandler, pageSize, detailEventHandler);
        }

        public static KendoGrid<TModel> LocalizeGridFor<TModel>(this HtmlHelper htmlHelper,
            string name, string controller, string deleteAction = "Delete", string editAction = null,
            string addAction = null, string editEventHandler = null, int? pageSize = null,
            string detailEventHandler = null, Action<KendoGrid<TModel>> firstInit = null)
            where TModel : class
        {
            return new KendoGrid<TModel>(htmlHelper, htmlHelper.Kendo().Grid<TModel>(), name, controller,
                deleteAction, editAction, addAction, editEventHandler, pageSize, detailEventHandler, firstInit);
        }

        #endregion

        #region LocalizeGridTextBoxFor

        public static string LocalizeGridTextBoxFor<TModel, TProperty>(this HtmlHelper htmlHelper,
            Expression<Func<TModel, TProperty>> expression, Object htmlAttributes)
        {
            var input = LocalizeTextBoxForEx(htmlHelper, expression, htmlAttributes, true);
            var validMsg = htmlHelper.ValidationMessage(GetFieldName(expression));

            return input + validMsg.ToString();
        }

        #endregion

        #region LocalizeGridTextAreaFor

        public static string LocalizeGridTextAreaFor<TModel, TProperty>(this HtmlHelper htmlHelper,
            Expression<Func<TModel, TProperty>> expression, Object htmlAttributes)
        {
            var input = LocalizeTextAreaForEx(htmlHelper, expression, htmlAttributes, true);
            var validMsg = htmlHelper.ValidationMessage(GetFieldName(expression));

            return input + validMsg.ToString();
        }

        #endregion

        #region LocalizeListView

        public static ListViewBuilder<TModel> LocalizeListView<TModel>(this HtmlHelper htmlHelper,
            string name, string templateid, string dataBoundHandler = null, IEnumerable items = null,
            int pageSize = 4, string itemPhrase = null)
            where TModel : class
        {
            var ret = htmlHelper.Kendo().ListView<TModel>()
                .TagName("ul")
                .Name(name)
                .ClientTemplateId(templateid)
                .Pageable(x => x.Messages(mbuilder => mbuilder
                    .ItemsPerPage(StringTable.GridPagingItemsPPage(itemPhrase))
                    .Display(StringTable.GridPagingDisplay(itemPhrase))
                    .First(StringTable.GridPagingFirst)
                    .Previous(StringTable.GridPagingPrevious)
                    .Last(StringTable.GridPagingLast)
                    .Next(StringTable.GridPagingNext)
                    .Refresh(StringTable.GridPagingRefresh)));

            if (!string.IsNullOrEmpty(dataBoundHandler))
                ret.Events(x => x.DataBound(dataBoundHandler));

            if (items != null)
            {
                ret.BindTo(items).DataSource(dataSource => dataSource
                    .Create(crud => { })
                    .ServerOperation(false)
                    .PageSize(pageSize));
            }

            return ret;
        }

        #endregion

        #region LocalizeTreeFor

        public static KendoTree<TModel> LocalizeTreeFor<TModel>(this HtmlHelper<IEnumerable<TModel>> htmlHelper,
            string name, string controller)
            where TModel : class
        {
            return new KendoTree<TModel>(htmlHelper, htmlHelper.Kendo().TreeView(), name, controller);

        }

        #endregion

        #region LocalizeTree

        public static KendoTree<TModel> LocalizeTree<TModel>(this HtmlHelper htmlHelper,
            string name, string controller)
            where TModel : class
        {
            return new KendoTree<TModel>(htmlHelper, htmlHelper.Kendo().TreeView(), name, controller);

        }

        #endregion

        #region LocalizeComboBox

        public static ComboBoxBuilder LocalizeComboBox(this HtmlHelper htmlHelper, string name, string templateid,
            string valueField, string textField, IEnumerable items,
            string dataBoundHandler = null, bool enable = true, bool ignoreCase = true)
        {
            var ret = htmlHelper.Kendo().ComboBox()
                .Name(name)
                .Enable(enable)
                .IgnoreCase(ignoreCase)
                .TemplateId(templateid)
                .DataValueField(valueField)
                .DataTextField(textField);

            if (!string.IsNullOrEmpty(dataBoundHandler))
                ret.Events(x => x.DataBound(dataBoundHandler));

            if (items != null)
                ret.BindTo(items);

            return ret;
        }

        public static ComboBoxBuilder LocalizeComboBox(this HtmlHelper htmlHelper,
            string name, string templateid, string controller, string listAction,
            string valueField, string textField,
            string dataBoundHandler = null, bool enable = true, bool ignoreCase = true)
        {
            var ret = htmlHelper.Kendo().ComboBox()
                .Name(name)
                .Enable(enable)
                .IgnoreCase(ignoreCase)
                .TemplateId(templateid)
                .DataSource(datasource => datasource.Read(listAction, controller))
                .DataValueField(valueField)
                .DataTextField(textField);

            if (!string.IsNullOrEmpty(dataBoundHandler))
                ret.Events(x => x.DataBound(dataBoundHandler));

            return ret;
        }

        public static ComboBoxBuilder LocalizeComboBox<TModel, TProp>(this HtmlHelper<TModel> htmlHelper,
            Expression<Func<TModel, TProp>> expression, string templateid, string controller, string listAction,
            string valueField, string textField,
            string dataBoundHandler = null, bool enable = true, bool ignoreCase = true)
        {
            var metadata = ModelMetadata.FromLambdaExpression(expression, htmlHelper.ViewData);
            var ret = htmlHelper.Kendo().ComboBox()
                .Name(htmlHelper.GetHtmlFieldName(expression))
                .Value(metadata.Model.ToStringSafe())
                .Enable(enable)
                .IgnoreCase(ignoreCase)
                .TemplateId(templateid)
                .DataSource(datasource => datasource.Read(listAction, controller))
                .DataValueField(valueField)
                .DataTextField(textField);

            if (!string.IsNullOrEmpty(dataBoundHandler))
                ret.Events(x => x.DataBound(dataBoundHandler));

            return ret;
        }

        public static MvcHtmlString LocalizeComboBox2<TModel, TProp>(this HtmlHelper<TModel> htmlHelper,
            Expression<Func<TModel, TProp>> expression, string templateid, string controller, string listAction,
            string valueField, string textField, object htmlAttributes = null,
            string dataBoundHandler = null, bool enable = true, bool ignoreCase = true)
        {
            var attrs = TransformAttributes(htmlAttributes, expression) ?? new RouteValueDictionary();
            var reqAttr = GetAttribute<RequiredAttribute, TModel, TProp>(expression);
            var lenAttr = GetAttribute<StringLengthAttribute, TModel, TProp>(expression);

            if (reqAttr != null) attrs["data-val-required"] = reqAttr.ErrorMessage.ModErrLocalize();
            if (lenAttr != null) attrs["maxlength"] = lenAttr.MaximumLength + 10;

            var metadata = ModelMetadata.FromLambdaExpression(expression, htmlHelper.ViewData);
            var ret = htmlHelper.Kendo().ComboBox()
                .Name(htmlHelper.GetHtmlFieldName(expression))
                .Value(metadata.Model.ToStringSafe())
                .Enable(enable)
                .IgnoreCase(ignoreCase)
                .TemplateId(templateid)
                .DataSource(datasource => datasource.Read(listAction, controller))
                .DataValueField(valueField)
                .DataTextField(textField);

            if (!string.IsNullOrEmpty(dataBoundHandler))
                ret.Events(x => x.DataBound(dataBoundHandler));

            ret.HtmlAttributes(attrs);

            return LocalizeControl(ret.ToHtmlString());
        }

        #endregion

        #region SimpleHiddenFor
        public static MvcHtmlString SimpleHiddenFor<TModel, TProperty>(this HtmlHelper<TModel> htmlHelper, Expression<Func<TModel, TProperty>> expression, object htmlAttributes)
        {
            var metadata = ModelMetadata.FromLambdaExpression(expression, htmlHelper.ViewData);

            return htmlHelper.Hidden(htmlHelper.GetHtmlFieldName(expression), metadata.Model, htmlAttributes);
        }
        #endregion

        #region LocalizeValidationMessageFor

        public static MvcHtmlString LocalizeValidationMessageFor<TModel, TProperty>(this HtmlHelper<TModel> htmlHelper,
            Expression<Func<TModel, TProperty>> expression, object htmlAttributes = null)
        {
            var attrs = TransformAttributes(htmlAttributes) ?? new RouteValueDictionary();
            var memInfo = (PropertyInfo)((MemberExpression)expression.Body).Member;

            attrs.Append("class", "error");

            if (memInfo.PropertyType == typeof(ODateTime))
            {
                var modelName = GetFieldName(expression) + "." + GetFieldName<ODateTime, DateTime?>(m => m.Value);

                attrs["data-valmsg-id"] = htmlHelper.Id(modelName);

                return htmlHelper.ValidationMessage(modelName, attrs);
            }
            else
            {
                if (memInfo.PropertyType == typeof(DateTime?) && Debugger.IsAttached)
                    Debugger.Break();

                attrs["data-valmsg-id"] = htmlHelper.IdFor(expression);

                return htmlHelper.ValidationMessageFor(expression, null, attrs);
            }
        }

        #endregion

        #region LocalizeEnumValues

        public static List<SelectListItem> LocalizeEnumValues(this Type type)
        {
            return type.LocalizeEnumValues<int>();
        }

        public static List<SelectListItem> LocalizeEnumValues<TEnum>(this Type type, Predicate<TEnum> cond = null, Func<object, string> fvalue = null)
        {
            return type.GetFields()
                .Where(x => x.IsStatic && (cond == null || cond((TEnum)x.GetValue(null))))
                .Select(x =>
                {
                    var cval = x.GetValue(null);
                    return new SelectListItem()
                    {
                        Value = fvalue != null ? fvalue(cval) : ((TEnum)cval).ToString(),
                        Text = x.GetDisplayName()
                    };
                })
                .ToList();
        }
        #endregion

        #region LocalizeEnumValue
        public static string LocalizeEnumValue<TEnum>(this Type type, TEnum value)
            where TEnum : struct, IComparable<TEnum>
        {
            return type.GetFields()
                .Where(x => x.IsStatic && x.GetValue(null).EqualsNumber(value))
                .Select(x => x.GetDisplayName()).FirstOrDefault();
        }
        #endregion

        #region GetDisplayName

        public static string GetDisplayName<TModel, TValue>(Expression<Func<TModel, TValue>> expression, bool admin = true)
        {
            var linqExpr = (MemberExpression)expression.Body;

            var attr =
                linqExpr.Member.GetCustomAttributes(typeof(DisplayAttribute), false)
                    .Cast<DisplayAttribute>()
                    .FirstOrDefault();

            return GetLocalize(TrimModel(linqExpr.Expression.Type.GetModelName()), linqExpr.Member.Name,
                attr == null ? linqExpr.Member.Name : attr.Name, admin);
        }

        public static string GetDisplayName(this FieldInfo field)
        {
            var attr = field.GetCustomAttributes(typeof(DisplayAttribute), false)
                .Cast<DisplayAttribute>()
                .FirstOrDefault();

            return GetLocalize(TrimModel(field.FieldType.GetModelName()), field.Name, attr == null ? field.Name : attr.Name);
        }

        public static string GetDisplayName(this PropertyInfo prop)
        {
            var attr = prop.GetCustomAttributes(typeof(DisplayAttribute), false)
                .Cast<DisplayAttribute>()
                .FirstOrDefault();

            return GetLocalize(TrimModel(prop.DeclaringType.GetModelName()), prop.Name, attr == null ? prop.Name : attr.Name);
        }

        public static string GetDisplayName<TMod>(this string fieldName, TMod model)
        {
            var meta = ModelMetadata.FromStringExpression(fieldName, new ViewDataDictionary(model));

            var ret = (meta == null ? null : meta.DisplayName) ?? fieldName;

            if (ret.IsNullOrEmpty() && Debugger.IsAttached) Debugger.Break();

            return ret;
        }

        public static string GetExportDisplayName<TModel, TProp>(this HtmlHelper<IEnumerable<TModel>> htmlHelper,
            Expression<Func<TModel, TProp>> expression)
        {
            return ((PropertyInfo)((MemberExpression)expression.Body).Member).GetExportDisplayName();
        }

        public static string GetExportDisplayNameEx(this PropertyInfo prop)
        {
            var attr = prop.GetCustomAttributes(typeof(ExportableAttribute), false)
                .Cast<ExportableAttribute>()
                .FirstOrDefault();

            if (attr == null) return prop.GetDisplayName();

            return GetLocalize("export_" + TrimModel(prop.DeclaringType.GetModelName()), prop.Name, attr == null || attr.Name.IsNullOrEmpty() ? prop.Name : attr.Name);

        }

        public static string GetExportDisplayName(this PropertyInfo prop)
        {
            var attr = prop.GetCustomAttributes(typeof(ExportableAttribute), false)
                .Cast<ExportableAttribute>()
                .FirstOrDefault();

            if (attr == null && Debugger.IsAttached) Debugger.Break();

            return GetLocalize("export_" + TrimModel(prop.DeclaringType.GetModelName()), prop.Name, attr == null || attr.Name.IsNullOrEmpty() ? prop.Name : attr.Name);

        }

        public static string GetDisplayName<TMod, TProp>(this TMod model, Expression<Func<TMod, TProp>> expression)
        {
            var meta = ModelMetadata.FromLambdaExpression(expression, new ViewDataDictionary<TMod>(model));

            var ret = (meta == null ? null : meta.DisplayName) ?? expression.GetFieldName();

            if (ret.IsNullOrEmpty() && Debugger.IsAttached) Debugger.Break();

           return GetLocalize(TrimModel(typeof(TMod).Name), expression.GetFieldName(), ret, false);
        }

        #endregion

        #region GetFieldName

        public static string GetFieldName<TModel, TVal>(this Expression<Func<TModel, TVal>> expression)
        {
            var ret = ExpressionHelper.GetExpressionText(expression);
            if (string.IsNullOrEmpty(ret) && Debugger.IsAttached) Debugger.Break();
            return ret;
        }

        #endregion

        #region ToFieldName

        public static List<string> ToFieldName(this IEnumerable<LambdaExpression> expressions)
        {
            if (expressions == null) return new List<string>();

            return expressions.Select(x => x.ToFieldName()).ToList();
        }

        public static string ToFieldName(this LambdaExpression expression)
        {
            if (expression == null) return string.Empty;

            var ret = ExpressionHelper.GetExpressionText(expression);

            if (string.IsNullOrEmpty(ret) && Debugger.IsAttached) Debugger.Break();

            return ret;
        }

        #endregion

        #region GetHtmlFieldName

        public static string GetHtmlFieldName<TModel, TVal>(this HtmlHelper<TModel> htmlHelper, Expression<Func<TModel, TVal>> expression)
        {
            return htmlHelper.ViewContext.ViewData.TemplateInfo.GetFullHtmlFieldName(GetFieldName(expression));
        }

        #endregion

        #region GetAttribute
        public static T GetAttribute<T, TModel, TProperty>(Expression<Func<TModel, TProperty>> expression)
        {
            var linqExpr = (MemberExpression)expression.Body;

            return linqExpr.Member.GetCustomAttributes(typeof(T), false)
                .Cast<T>()
                .FirstOrDefault();
        }
        #endregion

        #region TransformAttributes

        public static RouteValueDictionary TransformAttributes(object htmlAttributes)
        {
            return TransformAttributes<object, object>(htmlAttributes, null);
        }

        public static RouteValueDictionary TransformAttributes<TModel, TValue>(object htmlAttributes,
            Expression<Func<TModel, TValue>> expression = null, bool kendoValidate = false)
        {
            var dicAttrs = new RouteValueDictionary(htmlAttributes);

            if (htmlAttributes != null)
                foreach (var itm in dicAttrs.ToList())
                {
                    if (itm.Value == null)
                    {
                        dicAttrs.Remove(itm.Key);
                        continue;
                    }
                    if (itm.Key.EqualsIgCase("data_val_required"))
                    {
                        dicAttrs["data-val-required"] = itm.Value;
                        dicAttrs.Remove(itm.Key);
                        continue;
                    }
                    if (itm.Key.EqualsIgCase("data_bind"))
                    {
                        dicAttrs["data-bind"] = itm.Value;
                        dicAttrs.Remove(itm.Key);
                        continue;
                    }
                    if (itm.Key.EqualsIgCase("data_val"))
                    {
                        dicAttrs["data-val"] = itm.Value;
                        dicAttrs.Remove(itm.Key);
                        continue;
                    }
                }

            if (expression != null)
            {
                var memExpr = ((MemberExpression)expression.Body).Member;

                foreach (var attr in memExpr.GetCustomAttributes(typeof(ValidationAttribute))
                    .Cast<ValidationAttribute>())
                {
                    if (attr is StringLengthAttribute)
                        dicAttrs["data-val-length"] = FormatErrorMessage(attr, expression);
                    else if (attr is RangeAttribute)
                        dicAttrs["data-val-range"] = FormatErrorMessage(attr, expression);
                    else if (attr is RemoteAttribute)
                        dicAttrs["data-val-remote"] = FormatErrorMessage(attr, expression);

                }
            }

            if (!kendoValidate) dicAttrs["data-validate"] = "false";

            return dicAttrs;
        }

        #endregion

        #region FormatErrorMessage

        public static string FormatErrorMessage<TModel, TValue>(ValidationAttribute attribute,
            Expression<Func<TModel, TValue>> expression)
        {
            var errMsg = attribute.ErrorMessage.ModErrLocalize();
            var args = new List<object>() { GetDisplayName(expression) };

            if (attribute is StringLengthAttribute)
            {
                var sAttr = attribute as StringLengthAttribute;
                args.Add(sAttr.MaximumLength); args.Add(sAttr.MinimumLength);
            }
            else if (attribute is RangeAttribute)
            {
                var rAttr = attribute as RangeAttribute;
                args.Add(rAttr.Minimum); args.Add(rAttr.Maximum);
            }

            return string.Format(errMsg, args.ToArray());
        }

        #endregion

        #region Append.RouteValueDictionary

        private static void Append(this RouteValueDictionary attributes, string key, string value,
            string separator = " ")
        {
            object oVal;
            if (attributes.TryGetValue(key, out oVal))
            {
                attributes[key] = oVal.ToString() + separator + value;
            }
            else attributes.Add(key, value);
        }

        #endregion

        #region GetUnobtrusiveValidationAttributes

        public static IDictionary<string, object> GetUnobtrusiveValidationAttributes<TModel, TProperty>(this HtmlHelper<TModel> htmlHelper, Expression<Func<TModel, TProperty>> expression)
        {

            var metadata = ModelMetadata.FromLambdaExpression(expression, htmlHelper.ViewData);
            return htmlHelper.GetUnobtrusiveValidationAttributes(GetFieldName(expression), metadata);
        }

        public static IDictionary<string, object> GetUnobtrusiveValidationAttributes<TModel, TProperty>(this HtmlHelper<IEnumerable<TModel>> htmlHelper, Expression<Func<TModel, TProperty>> expression)
        {
            var metadata = ModelMetadata.FromLambdaExpression(expression, new ViewDataDictionary<TModel>(htmlHelper.ViewData));
            return htmlHelper.GetUnobtrusiveValidationAttributes(GetFieldName(expression), metadata);
        }

        #endregion

        #region TextBoxFor

        public static MvcHtmlString TextBoxFor<TModel, TProperty>(this HtmlHelper<IEnumerable<TModel>> htmlHelper,
            Expression<Func<TModel, TProperty>> expression, RouteValueDictionary htmlAttributes = null)
        {
            var dicAttrs = htmlAttributes ?? new RouteValueDictionary();
            var validateAttrs = htmlHelper.GetUnobtrusiveValidationAttributes(expression);

            return htmlHelper.TextBox(GetFieldName(expression), null, dicAttrs.Merge(validateAttrs));
        }

        #endregion

        #region TextAreaFor

        public static MvcHtmlString TextAreaFor<TModel, TProperty>(this HtmlHelper<IEnumerable<TModel>> htmlHelper,
            Expression<Func<TModel, TProperty>> expression, RouteValueDictionary htmlAttributes = null)
        {
            var dicAttrs = htmlAttributes ?? new RouteValueDictionary();
            var validateAttrs = htmlHelper.GetUnobtrusiveValidationAttributes(expression);

            return htmlHelper.TextArea(GetFieldName(expression), null, dicAttrs.Merge(validateAttrs));
        }

        #endregion

        #region TrimModel

        public static string TrimModel(string name)
        {
            return name.Length > 5 && name.EndsWith("Model") ? name.Substring(0, name.Length - 5) : name;
        }

        #endregion

        #region GetModelName

        public static string GetModelName(this Type type)
        {
            var name = type.FullName;
            var idx = name.LastIndexOf('.');
            if ((!name.StartsWith(ModuleNameSpace) && !name.StartsWith(PaymentNameSpace)) &&
                Debugger.IsAttached) Debugger.Break();
            if (idx != -1) name = name.Substring(idx + 1);
            return name.Replace("Model+", "_").Replace('+', '_');
        }

        #endregion

        #region MatchOrFirst

        public static T MatchOrFirst<T>(this IList<T> list, Predicate<T> cond)
        {
            if (list == null) return default(T);

            T ret = list.FirstOrDefault();

            foreach (var item in list)
            {
                if (cond(item))
                {
                    ret = item;
                    break;
                }
            }

            return ret;
        }

        public static T MatchOrFirst<T>(this IList list, Predicate<T> cond)
        {
            T ret = (T)(list.Count == 0 ? default(T) : list[0]);

            foreach (T item in list)
            {
                if (cond(item))
                {
                    ret = item;
                    break;
                }
            }

            return ret;
        }

        #endregion

        #region FirstOrDefault

        public static T FirstOrDefault<T>(this IList list, Predicate<T> cond)
        {
            T ret = default(T);

            foreach (T item in list)
            {
                if (cond(item))
                {
                    ret = item;
                    break;
                }
            }

            return ret;
        }

        public static T FirstOrDefault<T>(this List<T> list, Predicate<T> cond)
        {
            T ret = default(T);

            if (list != null)
                foreach (T item in list)
                {
                    if (cond(item))
                    {
                        ret = item;
                        break;
                    }
                }

            return ret;
        }

        #endregion

        #region FirstOrAddNew

        public static T FirstOrAddNew<T>(this List<T> list, Predicate<T> cond, Func<T> fNew)
            where T : class
        {
            T ret = null;

            foreach (T item in list)
            {
                if (cond(item)) { ret = item; break; }
            }

            if (ret == null) list.Add(ret = fNew());

            return ret;
        }

        #endregion

        #region Sum

        public static int Sum<T>(this IList list, Func<T, int> func)
        {
            int ret = 0;

            foreach (T item in list)
            {
                ret += func(item);
            }

            return ret;
        }

        #endregion

        #region BitOr

        public static int BitOr<T>(this IEnumerable<T> list, Func<T, int> func)
        {
            int ret = 0;

            foreach (T item in list)
            {
                ret |= func(item);
            }

            return ret;
        }

        public static long BitOr<T>(this IEnumerable<T> list, Func<T, long> func)
        {
            long ret = 0;

            foreach (T item in list)
            {
                ret |= func(item);
            }

            return ret;
        }

        #endregion

        #region BitAnd

        public static int BitAnd<T>(this IEnumerable<T> list, Func<T, int> func)
        {
            int ret = 0;

            foreach (T item in list)
            {
                ret &= func(item);
            }

            return ret;
        }

        public static long BitAnd<T>(this IEnumerable<T> list, Func<T, long> func)
        {
            long ret = 0;

            foreach (T item in list)
            {
                ret &= func(item);
            }

            return ret;
        }

        #endregion

        #region Any

        public static bool Any<T>(this IList list, Func<T, bool> func)
        {
            foreach (T item in list)
            {
                if (func(item)) return true;
            }

            return false;
        }

        #endregion

        #region All

        public static bool All<T>(this T obj, Predicate<string> fieldCond, Predicate<object> valueCond)
            where T : class
        {
            if (obj == null) return true;

            return typeof(T).GetProperties()
                .Where(m => fieldCond(m.Name))
                .All(m => valueCond(m.GetValue(obj)));
        }

        #endregion

        #region Take

        public static List<T> Take<T>(this IEnumerable<T> list, int count, Predicate<T> cond)
        {
            if (list == null) return null;

            var ret = new List<T>();

            foreach (var itm in list)
            {
                if (cond(itm))
                {
                    ret.Add(itm);
                    if (ret.Count >= count) break;
                }
            }

            return ret;
        }

        #endregion

        #region Count

        public static int Count<T>(this IList list, Func<T, bool> cond)
        {
            int ret = 0;

            foreach (T item in list)
            {
                if (cond(item)) ret++;
            }

            return ret;
        }

        #endregion

        #region GetPage
        public static List<T> GetPage<T>(this List<T> list, int pageIndex, int pageSize)
        {
            if (list == null) return null;

            int iB = (pageIndex - 1) * pageSize, iE = Math.Min(list.Count, iB + pageSize);

            if (iB < list.Count && iE > iB) return list.GetRange(iB, iE - iB);
            else return new List<T>();
        }
        #endregion

        #region SafeCount

        public static int SafeCount<T>(this IList<T> obj)
        {
            return obj == null ? 0 : obj.Count;
        }

        public static int SafeCount<T>(this T[] obj)
        {
            return obj == null ? 0 : obj.Length;
        }

        #endregion

        #region SafeTrim

        public static string SafeTrim(this string value)
        {
            return value == null ? null : value.Trim();
        }

        #endregion

        #region Copy

        public static void Copy<T, TVal>(this IList list, Dictionary<TVal, object> result, Predicate<T> cond, Func<T, TVal> func)
        {
            foreach (T item in list)
            {
                if (cond(item))
                {
                    var val = func(item);
                    if (!result.ContainsKey(val)) result.Add(val, item);
                }
            }
        }

        #endregion

        #region RemoveAll

        public static void RemoveAll<T>(this IList list, Predicate<T> cond)
        {
            var rem = new List<T>();

            foreach (T item in list)
                if (cond(item)) rem.Add(item);

            foreach (var item in rem)
                list.Remove(item);
        }

        public static void RemoveAll<T>(this IList<T> list, Predicate<T> cond)
        {
            var rem = new List<T>();

            foreach (T item in list)
                if (cond(item)) rem.Add(item);

            foreach (var item in rem)
                list.Remove(item);
        }

        #endregion

        #region GetLangCode

        public static string GetLangCode(this ClawContext context)
        {
            if (context != null && context.CurrentLanguage != null) return context.CurrentLanguage.LanguageCode;
            else return null;
        }

        #endregion

        #region GetDefaultLangCode

        public static string GetDefaultLangCode(this ClawContext context)
        {
            if (context != null && context.DefaultLanguage != null) return context.DefaultLanguage.LanguageCode;
            else return null;
        }

        #endregion

        #region GetCurrentOrDefaultLangCode

        public static string GetCurrentOrDefaultLangCode(this ClawContext context)
        {
            if (context != null)
            {
                if (context.CurrentLanguage != null) return context.CurrentLanguage.LanguageCode;
                else if (context.DefaultLanguage != null) return context.DefaultLanguage.LanguageCode;
            }
            return null;
        }

        #endregion

        #region GetCurrentOrDefaultLang

        public static Language GetCurrentOrDefaultLang(this ClawContext context)
        {
            if (context != null)
            {
                if (context.CurrentLanguage != null) return context.CurrentLanguage;
                else return context.DefaultLanguage;
            }

            return null;
        }

        #endregion

        #region GetDefaultOrCurrentLangCode

        public static string GetDefaultOrCurrentLangCode(this ClawContext context)
        {
            if (context != null)
            {
                if (context.DefaultLanguage != null) return context.DefaultLanguage.LanguageCode;
                else if (context.CurrentLanguage != null) return context.CurrentLanguage.LanguageCode;
            }
            return null;
        }

        #endregion

        #region GetDefaultOrCurrentLang

        public static Language GetDefaultOrCurrentLang(this ClawContext context)
        {
            if (context != null)
            {
                if (context.DefaultLanguage != null) return context.DefaultLanguage;
                else return context.CurrentLanguage;
            }
            return null;
        }

        #endregion

        #region IsSuperAdmin

        public static bool IsSuperAdmin(this ClawContext context)
        {
            if (context != null && context.User != null) return context.User.IsSuperAdmin;

            return false;
        }

        #endregion

        #region GetRouteValue
        public static object GetRouteValue(this HttpContext cxt, string name)
        {
            RequestContext reqCxt;
            if (cxt == null || cxt.Request == null || (reqCxt = cxt.Request.RequestContext) == null) return null;
            if (reqCxt.RouteData == null || reqCxt.RouteData.Values == null) return null;

            return reqCxt.RouteData.Values[name];
        }
        #endregion

        #region GetFirstName
        public static string GetFirstName(this string fullname)
        {
            if (fullname.IsNullOrEmpty()) return fullname;

            var idx = fullname.Trim().IndexOf(' ');

            return idx == -1 ? fullname : fullname.Substring(0, idx).Trim();
        }
        #endregion

        #region GetLastName
        public static string GetLastName(this string fullname)
        {
            if (fullname.IsNullOrEmpty()) return fullname;

            var idx = fullname.Trim().IndexOf(' ');

            return idx == -1 ? "" : fullname.Substring(idx).Trim();
        }
        #endregion

        #region ActionUrl

        public static string ActionUrl(this HtmlHelper htmlHelper, string action = "", string controller = null)
        {
            if (string.IsNullOrEmpty(action)) action = "index";
            var uhelper = new UrlHelper(htmlHelper.ViewContext.RequestContext, htmlHelper.RouteCollection);
            var url = uhelper.Action(action, controller ?? htmlHelper.GetController()) ?? "";
            if (action.EqualsIgCase("index"))
            {
                var idx = url.LastIndexOf('/');
                if (idx != -1) return url.Substring(0, idx);
            }
            return url;
        }

        #endregion

        #region GetRouteInfo
        public static RouteInfo GetRouteInfo(this HttpContext context, string path)
        {
            var request = context.Request;
            var tmp = ("http://" + request.Url.Authority).CombineWPath(request.ApplicationPath).TrimEnd('/');
            return new RouteInfo(new Uri(tmp.CombineWPath(path)));
        }
        #endregion

        #region RenderActionUrl

        public static void RenderActionUrl(this HtmlHelper htmlHelper, string url,
            object routeVals = null)
        {
            var route = HttpContext.Current.GetRouteInfo(url);

            if (route.RouteData != null && routeVals != null)
                route.RouteData.Values.Merge(new RouteValueDictionary(routeVals));

            htmlHelper.RenderAction(route["action"], route["controller"],
                route.Remove("controller", "action").RouteData.Values);
        }

        #endregion

        #region RenderToString

        public static string RenderToString(this Controller controller, string action, object routeValues,
            bool ignoreError = false, string controllerName = null, string overrideLangCode = null, RouteCollection routes = null)
        {
            //Current lang BE
            string adminLang = ClawContext.Current.AdminLanguage.LanguageCode;
            try
            {

                if (overrideLangCode != null)
                {
                    LanguageMgr.OverrideLangCode(overrideLangCode);
                    ClawContext.Current.AdminLanguage.LanguageCode = Claw.Core.Extensions.StringLocalizeExtensions.IsAdminMode ? overrideLangCode : adminLang;
                }

                var ctrlCxt = controller.ControllerContext ?? new ControllerContext(HttpContext.Current.Request.RequestContext, controller);
                var viewCxt = new ViewContext(ctrlCxt, new WebFormView(ctrlCxt, "Index"),
                    controller.ViewData, controller.TempData, new StringWriter());
                var helper = routes == null
                    ? new HtmlHelper(viewCxt, new ViewPage())
                    : new HtmlHelper(viewCxt, new ViewPage(), routes);

                helper.RenderAction(action, controllerName,
                    new RouteValueDictionary(new RouteValueDictionary(routeValues).Merge(controller.ViewData)));

                return helper.ViewContext.Writer.ToString();
            }
            catch (Exception e)
            {
                if (ignoreError) return null;
                else throw e;
            }
            finally
            {
                if (overrideLangCode != null)
                {
                    LanguageMgr.OverrideLangCode(null);
                    ClawContext.Current.AdminLanguage.LanguageCode = adminLang;
                }

            }
        }

        #endregion

        #region GetHtmlHelper

        public static HtmlHelper<TModel> GetHtmlHelper<TModel>(this HtmlHelper htmlHelper, TModel model = null)
            where TModel : class
        {
            if (model == null) model = Activator.CreateInstance<TModel>();
            var controllerCxt = htmlHelper.ViewContext.Controller.ControllerContext;
            var viewCxt = new ViewContext(controllerCxt, new WebFormView(controllerCxt, "Index"),
                new ViewDataDictionary<TModel>(model),
                new TempDataDictionary(), new StringWriter());

            var helper = new HtmlHelper<TModel>(viewCxt, new ViewPage());

            helper.ViewData.Model = model;

            return helper;
        }

        #endregion

        #region ToDictionary

        public static RouteValueDictionary ToDictionary(this Uri url, IDictionary<string, object> values = null)
        {
            if (url == null) return null;

            var splitter = new[] { '=' };
            var ret = (values == null ? new RouteValueDictionary() : new RouteValueDictionary(values));

            foreach (var arg in url.Query.Split(new[] { '?', '&' }, StringSplitOptions.RemoveEmptyEntries))
            {
                var arr = arg.Split(splitter, StringSplitOptions.RemoveEmptyEntries);
                if (arr.Length != 2 || ret.ContainsKey(arr[0])) continue;
                ret.Add(arr[0], arr[1]);
            }

            return ret;
        }

        public static RouteValueDictionary ToDictionary(this object value, bool recursive = false,
            bool resolveValue = true)
        {
            var result = new RouteValueDictionary();
            value.ToDictionary(result, null, recursive, resolveValue);
            return result;
        }

        static void ToDictionary(this object value, RouteValueDictionary result,
            string prefix = null, bool recursive = false, bool resolveValue = true)
        {
            if (value == null) return;

            var modNamespace = typeof(ModExtensions).Namespace ?? string.Empty;

            foreach (var prop in value.GetType().GetProperties())
            {
                var propVal = prop.GetValue(value);
                if (recursive && prop.PropertyType.Namespace != null && !prop.PropertyType.IsEnum &&
                    prop.PropertyType.Namespace.StartsWith(modNamespace))
                {
                    prop.GetValue(value).ToDictionary(result,
                        (prefix ?? string.Empty) + prop.Name + '.', true, resolveValue);

                    continue;
                }

                object oVal;
                if (resolveValue)
                {
                    if (prop.PropertyType.IsEnum)
                    {
                        var basetype = prop.PropertyType.GetEnumUnderlyingType();
                        if (basetype == typeof(byte)) oVal = (byte)propVal;
                        else oVal = (int)propVal;
                    }
                    else oVal = propVal;
                }
                else oVal = new System.Tuple<object, PropertyInfo>(value, prop);

                if (prefix == null) result.Add(prop.Name, oVal);
                else result.Add(prefix + prop.Name, oVal);
            }
        }

        #endregion

        #region ToFieldDictionary

        public static Dictionary<string, LambdaExpression> ToFieldDictionary(this Type type,
            Predicate<PropertyInfo> filter = null, PropertyInfo prefix = null)
        {
            if (prefix == null) return type.ToFieldDictionary(filter, null, null);
            else
            {
                var param = Expression.Parameter(prefix.DeclaringType, "x");
                return type.ToFieldDictionary(filter, param, Expression.Property(param, prefix));  
            }
        }

        public static Dictionary<string, LambdaExpression> ToFieldDictionary(this Type type,
            Predicate<PropertyInfo> filter, ParameterExpression prefixParam = null, MemberExpression prefixProp = null)
        {
            if (type == null) return new Dictionary<string, LambdaExpression>();

            prefixParam = prefixParam ?? Expression.Parameter(type, "x");
            var prefixName = prefixProp == null ? "" : ExpressionHelper.GetExpressionText(Expression.Lambda(prefixProp, prefixParam));

            return type.GetProperties()
                .Where(x => filter == null || filter(x))
                .ToDictionary(
                    x => (prefixName == null ? "" : prefixName + ".") + x.Name,
                    x => Expression.Lambda(prefixProp == null
                        ? Expression.Property(prefixParam, x)
                        : Expression.Property(prefixProp, x), prefixParam));
        }

        #endregion

        #region ToRouteValueDictionary

        public static RouteValueDictionary ToRouteValueDictionary<T>(this IEnumerable<T> items,
            Func<T, string> keySelector, Func<T, object> valueSelector)
        {
            var ret = new RouteValueDictionary();

            foreach (var item in items)
            {
                ret[keySelector(item)] = valueSelector(item);
            }

            return ret;
        }

        #endregion

        #region ToProp2ColumnMap

        public static Dictionary<string, string> ToProp2ColumnMap(this DataTable table, Type type,
            Func<PropertyInfo, string, bool> fCond = null, bool useExportAttr = true)
        {
            var ret = new Dictionary<string, string>();

            if (table == null || type == null) return ret;

            foreach (var prop in type.GetProperties().ToList()
                .Where(p => !useExportAttr || p.GetCustomAttributes<ExportableAttribute>().Any(x => x.CanImport)))
            {
                var displayName = useExportAttr ? prop.GetExportDisplayName() : prop.GetDisplayName();
                if (displayName != null && table.Columns.Contains(displayName))
                {
                    if (fCond == null || fCond(prop, displayName)) ret.Add(prop.Name, displayName);
                }
            }

            return ret;
        }

        #endregion

        #region EqualsIgCase
        /// <summary>
        ///  Equals Ignore Case
        /// </summary>
        
        public static bool EqualsIgCase(this string value, string target, bool noEmpty = false)
        {
            if (noEmpty && (value.IsNullOrEmpty() || target.IsNullOrEmpty())) return false;

            if (value == null) return target == null;
            else return value.Equals(target, StringComparison.OrdinalIgnoreCase);
        }
        #endregion

        #region EqualsSafe
        public static bool EqualsSafe(this object value, object target)
        {
            if (value == null && target == null) return true;
            if (value == null || target == null) return false;

            return value.Equals(target);
        }
        #endregion

        #region EqualsNumber

        public static bool EqualsNumber<T>(this object value, T target)
            where T : struct, IComparable<T>
        {
            if (value == null) return false;
            else return target.CompareTo(value.ChangeType<T>()) == 0;
        }

        #endregion

        #region ChangeType

        public static T ChangeType<T>(this object value)
        {
            return (T)System.Convert.ChangeType(value, typeof(T));
        }

        public static object ChangeType(this string value, Type type)
        {
            if (type == typeof(string)) return value;
            if (value.IsNullOrEmpty()) return type.GetDefaultValue();

            if (MapperInt.PrimitiveExtensions.IsNullableType(type)) return System.Convert.ChangeType(value, MapperInt.PrimitiveExtensions.GetTypeOfNullable(type));

            return System.Convert.ChangeType(value, type);
        }

        #endregion

        #region SafeChangeType
        public static T SafeChangeType<T>(this object value)
        {
            if (MapperInt.PrimitiveExtensions.IsNullableType(typeof(T)))
            {
                if (value == null) return (T)(object)null;
                else return (T)System.Convert.ChangeType(value, MapperInt.PrimitiveExtensions.GetTypeOfNullable(typeof(T)));
            }

            return (T)System.Convert.ChangeType(value, typeof(T));
        }
        #endregion

        #region GetFileExtensionValidRegEx

        public static string GetFileExtensionValidRegEx(this string value, bool enableZip = true)
        {
            if (string.IsNullOrEmpty(value)) return ".";
            value = value.Replace(".", "").Replace("*", "");
            var arr = value.Split(new char[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries);
            if (arr.Length == 0) return ".";
            else
            {
                if (!enableZip) arr = arr.Where(x => !x.EqualsIgCase("zip")).ToArray();
                return @"\." + Regex.Replace(arr.ToString(@"|\."), @"\s", "") + "$";
            }
        }

        #endregion

        #region GetFileExtensionDropZone

        public static string GetFileExtensionDropZone(this string value, bool enableZip = true)
        {
            if (string.IsNullOrEmpty(value)) return "";
            value = value.Replace(".", "").Replace("*", "");
            var arr = value.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries);
            if (arr.Length == 0) return "";
            else
            {
                if (enableZip) arr = arr.Union(new[] { "zip" }).ToArray();
                else arr = arr.Where(x => !x.EqualsIgCase("zip")).ToArray();

                return arr.Distinct(x => x.ToLower().Trim()).Select(x => "." + x).ToString(@",");
            }
        }

        #endregion

        #region GetSiteId
        public static int GetSiteId(this IDataService ds)
        {
            Site site = (ds != null ? ds.GetCurrentSite() : null);
            return site == null ? 0 : site.Id;
        }
        #endregion

        #region ActionUrl

        public static string ActionUrl(this HtmlHelper htmlHelper, string action, string controller = null,
            object routeValues = null)
        {
            var uhelper = new UrlHelper(htmlHelper.ViewContext.RequestContext, htmlHelper.RouteCollection);
            return uhelper.Action(action, controller ?? htmlHelper.GetController(), routeValues);
        }

        public static string ActionUrl(this HttpRequestBase request, string action, string controller,
            object routeValues = null)
        {
            var uhelper = new UrlHelper(request.RequestContext);
            return uhelper.Action(action, controller, routeValues);
        }

        #endregion

        #region Save.Bitmap

        /// <param name="quality">0-100</param>
        public static void Save(this Bitmap bmp, string filePath, int quality = 90)
        {
            EncoderParameters eps = new EncoderParameters(1);
            eps.Param[0] = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, quality);
            ImageCodecInfo ici = ImageTools.GetCodecByExt(Path.GetExtension(filePath));

            bmp.Save(filePath, ici, eps);
        }

        #endregion

        #region ContainsTransparent

        public static bool ContainsTransparent(this Image image)
        {
            var bitmap = image as Bitmap;
            if (bitmap == null) return false;
            else return bitmap.ContainsTransparent();
        }

        public static bool ContainsTransparent(this Bitmap image)
        {
            var t = Color.Transparent;
            for (int y = 0; y < Math.Min(5, image.Height); y += 2)
            {
                for (int x = 0; x < image.Width; x += Math.Max(1, image.Width/10))
                {
                    var px = image.GetPixel(x, y);
                    if (px.A == t.A /*&& px.R == t.R && px.G == t.G && px.B == t.B*/) return true;
                }
            }

            for (int y = Math.Max(0, image.Height - 5); y < image.Height; y += 2)
            {
                for (int x = 0; x < image.Width; x += Math.Max(1, image.Width/10))
                {
                    var px = image.GetPixel(x, y);
                    if (px.A == t.A /*&& px.R == t.R && px.G == t.G && px.B == t.B*/) return true;
                }
            }

            for (int x = 0; x < Math.Min(5, image.Width); x += 2)
            {
                for (int y = 0; y < image.Height; y += Math.Max(1, image.Height / 10))
                {
                    var px = image.GetPixel(x, y);
                    if (px.A == t.A /*&& px.R == t.R && px.G == t.G && px.B == t.B*/) return true;
                }
            }

            for (int x = Math.Max(0, image.Width - 5); x < image.Width; x += 2)
            {
                for (int y = 0; y < image.Height; y += Math.Max(1, image.Height / 10))
                {
                    var px = image.GetPixel(x, y);
                    if (px.A == t.A /*&& px.R == t.R && px.G == t.G && px.B == t.B*/) return true;
                }
            }

            return false;
        }

        #endregion

        #region CombineWPath

        public static string CombineWPath(this string value, string path)
        {
            return value.CombineWPath(path, '/');
        }

        public static string CombineWPath(this string value, string path, char sep)
        {
            if (string.IsNullOrEmpty(value)) return path;

            if (value.EndsWith(sep.ToString())) return value + path.TrimStart(sep);
            else return value + sep + path.TrimStart(sep);
        }

        #endregion

        #region ResolveClientUrl

        public static string ResolveClientUrl(this string value)
        {
            if (string.IsNullOrEmpty(value) || HttpContext.Current == null) return value;

            return Globals.ResolveClientUrl(value);
        }

        #endregion

        #region DPadLeft

        public static string DPadLeft(this string value, int delta, char c)
        {
            if (value == null) return null;
            else return value.PadLeft(value.Length + delta, c);
        }

        #endregion

        #region Left

        public static string Left(this string value, int length, string padding = null)
        {
            if (value == null || value.Length <= length) return value;

            var ret = value.Substring(0, length);
            if (padding == null) return ret;
            return ret + padding;
        }

        #endregion

        #region RoundEx
        /// <summary>
        /// - [greater than 0] decimal length 
        /// - [less than 0] round up to Math.Pow(10,n)
        /// </summary>          
        public static decimal? RoundEx(this decimal? value, int decimals)
        {
            if (value == null) return null;

            if (decimals >= 0) return Math.Round(value ?? 0, decimals);
            var n = Math.Pow(10, Math.Abs(decimals));

            return (decimal?)(Math.Round(((long)value) / n, 0) * n);
        }

        public static decimal RoundEx(this decimal value, int decimals)
        {
            return ((decimal?)value).RoundEx(decimals) ?? 0;
        }
        public static decimal ToCeilling(this decimal value)
        {
            var settings = SettingsBiz.GetSettings();
            bool ceilingPrice = settings.Display.Product.PriceRound;
            return ceilingPrice ? Math.Ceiling(value) : RoundEx(value, 2);
        }
        #endregion

        #region Replace
        public static string Replace(this MvcHtmlString node, object attributes)
        {
            var dic = new RouteValueDictionary(attributes);
            var html = node.ToString();
            foreach (var itm in dic)
            {
                var key = itm.Key;
                if (key.EqualsIgCase("data_val_regex_pattern")) key = "data-val-regex-pattern";
                html = Regex.Replace(html, key + "=\"([^\"]*)\"", key + "='" + itm.Value + "'",
                    RegexOptions.IgnoreCase | RegexOptions.Compiled);

            }
            return html;
        }

        public static string Replace<TModel>(this string data, TModel model, ViewDataDictionary viewData = null)
           where TModel : class
        {
            if (model == null || data == null) return data;

            //var dic = model.ToDictionary();
            viewData = viewData ?? new ViewDataDictionary(model);
            const string patt = @"\{\{(?<token>\w+(\.\w+)*)\}\}";
            var mc = Regex.Matches(data, patt, RegexOptions.Compiled | RegexOptions.IgnoreCase);

            foreach (var tokName in mc.Cast<System.Text.RegularExpressions.Match>()
                .Where(x => x.Success).Select(x => x.Groups["token"].Value).Distinct())
            {
                var modelMetaData = ModelMetadata.FromStringExpression(tokName, viewData);
                if (modelMetaData == null) continue;

                var tokVal = (modelMetaData.Model ?? "").ToString();
                data = data.Replace("{{" + tokName + "}}", tokVal);
            }

            return data.Replace("\\{{", "{{").Replace("\\}}", "}}");
        }

        /// <summary>replace with [else if]</summary>
        /// <example>"aa{{@if(cond0)}}bb{{@endif}}cc{{@if(cond1)}}bb2{{@elseif}}dd2{{@endif}}".ReplaceEx(new { cond0 = true, cond1 = 0, cond2 = ""});</example>
        public static string ReplaceEx<TModel>(this string data, TModel model, ViewDataDictionary viewData = null)
            where TModel : class
        {
            if (model == null || data == null) return data;

            viewData = viewData ?? new ViewDataDictionary(model);

            const StringComparison cmpOpt = StringComparison.Ordinal;
            const string tOpenIf = "{{@if(", tCloseIf = ")}}", tElseIf = "{{@elseif}}", tEndIf = "{{@endif}}";

            int i = 0;
            var idx = data.IndexOf(tOpenIf, i, cmpOpt);

            if (idx < 0) return data.Replace(model, viewData);

            var ret = new StringBuilder(data.Length);

            while (i < data.Length && idx >= i)
            {
                int nidx, eidx;

                ret.Append(data.Substring(i, idx - i));

                idx += tOpenIf.Length;

                if (-1 == (nidx = data.IndexOf(tCloseIf, idx, cmpOpt))) break;

                var expr = data.Substring(idx, nidx - idx);
                var exprVal = expr.Eval(model, viewData);

                if (-1 == (eidx = data.IndexOf(tEndIf, nidx + tCloseIf.Length, cmpOpt))) break;

                idx = nidx + tCloseIf.Length;
                var midx = data.IndexOf(tElseIf, idx, eidx - idx, cmpOpt);

                if (exprVal)
                    ret.Append(midx == -1
                        ? data.Substring(idx, eidx - idx)
                        : data.Substring(idx, midx - idx));
                else
                    if (midx != -1) ret.Append(data.Substring(midx + tElseIf.Length, eidx - midx - tElseIf.Length));

                i = eidx + tEndIf.Length;

                idx = i + tOpenIf.Length < data.Length ? data.IndexOf(tOpenIf, i, cmpOpt) : -1;
            }

            if (i < data.Length) ret.Append(data.Substring(i, data.Length - i));

            return ret.ToString().Replace(model, viewData);
        }

        #endregion

        #region Eval
        /// <summary>return true if expression value is [true, !=0, !string.IsNullOrEmpty]</summary>
        public static bool Eval<TModel>(this string expression, TModel model, ViewDataDictionary viewData)
            where TModel : class
        {
            if (expression.IsNullOrEmpty() || model == null) return false;

            var modelMetaData = ModelMetadata.FromStringExpression(expression, viewData);

            if (modelMetaData != null && modelMetaData.ModelType != null)
            {
                var defVal = modelMetaData.ModelType.GetDefaultValue();
                if (modelMetaData.Model != null && !defVal.EqualsSafe(modelMetaData.Model))
                {
                    if (modelMetaData.ModelType == typeof(bool?)) return ((bool?)modelMetaData.Model) == true;
                    if (modelMetaData.ModelType == typeof(int?)) return (((int?)modelMetaData.Model) ?? 0) != 0;
                    if (modelMetaData.ModelType == typeof(long?)) return (((long?)modelMetaData.Model) ?? 0) != 0;

                    return (modelMetaData.ModelType != typeof(string) && modelMetaData.ModelType != typeof(StringBuilder)) ||
                           !string.IsNullOrEmpty(modelMetaData.Model.ToStringSafe());
                }
            }

            return false;
        }

        #endregion

        #region ClawRenderPartial

        public static void ClawRenderPartial(this HtmlHelper htmlHelper, string partialViewName, object model,
            ViewDataDictionary viewData)
        {
            var controller = new BaseModuleController(ClawContext.Current.DataService);
            partialViewName = controller.ClawView(partialViewName).ViewName;
            htmlHelper.RenderPartial(partialViewName, model, viewData);
        }

        #endregion

        #region Convert

        public static List<Tout> Convert<Tin, Tout>(this IEnumerable<Tin> items, Func<Tin, Tout> fConv)
        {
            var ret = new List<Tout>();

            foreach (var itm in items)
            {
                ret.Add(fConv(itm));
            }

            return ret;
        }

        #endregion

        #region InitColumnName

        public static void InitColumnName(this DataTable table, int headerRow = 0, int skipRows = 1)
        {
            if (table == null || table.Rows.Count == 0) return;

            for (int i = 0; i < table.Rows[0].ItemArray.Length; i++)
            {
                table.Columns[i].ColumnName = table.Rows[0][i].ToString();
            }

            table.Rows.RemoveAt(0);

            while (skipRows > 1)
            {
                table.Rows.RemoveAt(0);
                skipRows--;
            }
        }

        #endregion

        #region Action

        public static List<T> Action<T>(this List<T> items, Action<T> fAct)
        {
            foreach (var itm in items)
                fAct(itm);

            return items;
        }

        public static IList<T> Action<T>(this IList<T> items, Action<T> fAct)
        {
            foreach (var itm in items)
                fAct(itm);

            return items;
        }

        public static IEnumerable<T> Action<T>(this IEnumerable<T> items, Action<T> fAct)
        {
            foreach (var itm in items)
                fAct(itm);

            return items;
        }

        public static List<T> Action<T>(this List<T> items, Action<T, int> fAct)
        {
            int i = 0;
            foreach (var itm in items)
                fAct(itm, i++);

            return items;
        }

        #endregion

        #region IsZeroId
        public static bool IsZeroId(this string value)
        {
            return string.IsNullOrEmpty(value) || value == "0";
        }
        #endregion

        #region IsNullOrEmpty
        public static bool IsNullOrEmpty(this string value)
        {
            return string.IsNullOrEmpty(value);
        }
        #endregion

        #region IsNull
        public static bool IsNull(this OMoneyMc value)
        {
            return value == null || value.Value == null;
        }

        public static bool IsNull(this OMoney value)
        {
            return value == null || value.Value == null;
        }

        public static bool IsNull(this ODateTime value)
        {
            return value == null || value.Value == null;
        }

        public static bool IsNull(this OFloat value)
        {
            return value == null || value.Value == null;
        }

        #endregion

        #region NullIfZero

        public static OMoney NullIfZero(this decimal? value, bool notNull = false)
        {
            if (value == null || value == 0) return notNull ? new OMoney() : null;
            else return new OMoney(value);
        }

        public static OMoney NullIfZero(this OMoney value, bool notNull = false)
        {
            if (value == null || value == 0) return notNull ? new OMoney() : null;
            else return value;
        }

        #endregion

        #region ToExpando

        public static ExpandoObject ToExpando(this string json)
        {
            if (string.IsNullOrEmpty(json)) return null;

            json = json.Trim();

            if (json.StartsWith("{"))
                return JsonConvert.DeserializeObject<ExpandoObject>(json, new ExpandoObjectConverter());

            return JsonConvert.DeserializeObject<List<ExpandoObject>>(json, new ExpandoObjectConverter()).FirstOrDefault();
        }

        public static ExpandoObject ToExpando<T>(this T data, bool recursive = false)
        {
            var modNamespace = typeof(ModExtensions).Namespace ?? string.Empty;
            var value = new ExpandoObject();
            var obj = value as IDictionary<string, object>;
            var args = new RouteValueDictionary(data);

            foreach (var arg in args)
            {
                if (recursive && arg.Value != null)
                {
                    var type = arg.Value.GetType();
                    if (type.Namespace != null && type.Namespace.StartsWith(modNamespace))
                    {
                        obj.Add(arg.Key, arg.Value.ToExpando(true));
                        continue;
                    }
                }

                if (arg.Value != null)
                {
                    var type = arg.Value.GetType();
                    if (MapperInt.PrimitiveExtensions.IsEnumerableType(type) && type != typeof(string))
                    {
                        var lst = (IEnumerable)arg.Value;
                        var newVal = lst.Cast<object>().Select(x => x.ToExpando(true)).ToList();
                        obj.Add(arg.Key, newVal);
                        continue;
                    }
                }
                obj.Add(arg.Key, arg.Value);
            }

            return value;
        }

        #endregion

        #region ToJson

        public static string ToJsonEx(this object data)
        {
            if (data == null) return null;

            var serializer = new JavaScriptSerializer();

            serializer.RegisterConverters(new JavaScriptConverter[] { new Controllers.Admin.ExpandoJsonConverter() });

            return serializer.Serialize(data);
        }

        public static string ToJson(this ExpandoObject obj)
        {
            if (obj == null) return null;

            return JsonConvert.SerializeObject(obj);
        }

        public static string ToJson(this IEnumerable<ExpandoObject> obj)
        {
            if (obj == null) return null;

            return JsonConvert.SerializeObject(obj);
        }

        public static string ToJson(this ModelStateDictionary obj)
        {
            if (obj == null) return null;

            var dic = new Dictionary<string, string>();

            foreach (var itm in obj.Where(x => x.Value.Errors.Count != 0))
            {
                dic.Add(itm.Key, itm.Value.Errors[0].ErrorMessage);
            }

            return JsonConvert.SerializeObject(dic);
        }

        #endregion

        #region ToOFloat
        public static OFloat ToOFloat(this float? value)
        {
            if (value == null) return null;
            else return new OFloat(value.Value);
        }
        public static OFloat ToOFloat(this decimal? value)
        {
            if (value == null) return null;
            else return new OFloat(value.Value);
        }
        #endregion

        #region ToOMoney

        public static OMoney ToOMoney(this decimal? value)
        {
            return value.ToOMoney(false);
        }

        public static OMoney ToOMoney(this decimal? value, bool notNull)
        {
            if (value == null) return notNull ? new OMoney() : null;
            else return new OMoney(value.Value);
        }
        #endregion

        #region ToODateTime
        public static ODateTime ToODateTime(this DateTime? value)
        {
            if (value == null) return null;
            else return new ODateTime(value.Value);
        }
        #endregion

        #region NullOrGreaterE

        public static int? NullOrGreaterE(this int value, int min)
        {
            if (value < min) return null;
            else return value;
        }

        #endregion

        #region InRange

        public static bool InRange(this OMoney obj, decimal min, decimal max)
        {
            return obj != null && obj.Value != null && obj.Value >= min && obj.Value <= max;
        }

        public static bool InRange(this OFloat obj, float min, float max)
        {
            return obj != null && obj.Value != null && obj.Value >= min && obj.Value <= max;
        }

        #endregion

        #region InList
        public static bool InList<TEnum, TInt>(this TInt value, params TEnum[] list)
        {
            return list.Cast<TInt>().Contains(value);
        }
        public static bool InList(this string value, params string[] list)
        {
            return list.Any(x => x.EqualsIgCase(value));
        }
        #endregion

        #region Distinct
        public static IEnumerable<TItem> Distinct<TItem, TKey>(this IEnumerable<TItem> list, Func<TItem, TKey> keySelector)
        {
            if (list == null) return null;

            return list.GroupBy(keySelector).Select(x => x.First());
        }
        #endregion

        #region Val

        public static object Val<TEnum>(this TEnum value)
        {
            var type = Enum.GetUnderlyingType(typeof(TEnum));
            return System.Convert.ChangeType(value, type);
        }

        #endregion

        #region Get

        public static TVal Get<TVal>(this ExpandoObject obj, string name)
        {
            var dic = obj as IDictionary<string, object>;
            return (TVal)System.Convert.ChangeType(dic[name], typeof(TVal));
        }

        #endregion

        #region SafeProp

        public static TProp SafeProp<T, TProp>(this T obj, Func<T, TProp> selector)
            where T : class
        {
            if (obj == null) return default(TProp);
            return selector(obj);
        }

        public static TProp SafeProp<T, TProp>(this T obj, Func<T, TProp> selector, TProp defVal)
            where T : class
        {
            if (obj == null) return defVal;
            return selector(obj);
        }

        #endregion

        #region IfNull

        public static string IfNull(this string value, string defVal)
        {
            if (string.IsNullOrEmpty(value)) return defVal;
            return value;
        }

        #endregion

        #region ToString
        public static string ToString<T>(this IEnumerable<T> items, string separator)
        {
            StringBuilder ret = new StringBuilder();

            foreach (var itm in items)
            {
                ret.Append(itm); ret.Append(separator);
            }

            if (ret.Length > separator.Length) ret.Remove(ret.Length - separator.Length, separator.Length);

            return ret.ToString();
        }

        public static string ToString<T, TKey>(this IEnumerable<T> items, string separator,
            Func<T, TKey> sortKeySelector)
        {
            if (items == null) return null;

            return items.OrderBy(sortKeySelector).ToString(separator);
        }

        public static string ToString<T>(this List<T> list, Func<T, string> selector, string separator = ">")
        {
            if (list == null) return null;

            return list.Select(selector).ToString(separator);
        }
        #endregion

        #region ToBase36

        public static string ToBase36(this int value)
        {
            return ((uint)value).ToBase36();
        }

        public static string ToBase36(this uint value)
        {
            var ret = new StringBuilder();

            while (value > 0)
            {
                var n = value % 36;
                value = value / 36;
                ret.Insert(0, (char)(n < 10 ? '0' + n : 'a' + n - 10));
            }

            return ret.ToString();
        }

        #endregion

        #region ToBase64

        public static string ToBase64(this string filePath, bool imageData = true)
        {
            if (filePath.IsNullOrEmpty()) return null;

            if (!File.Exists(filePath)) return null;

            using (var file = File.OpenRead(filePath))
            {
                var prefix = imageData ? "data:image/" + (Path.GetExtension(filePath) ?? "jpg").TrimStart('.') + ";base64," : "";
                var bytes = new byte[file.Length];
                file.Read(bytes, 0, bytes.Length);
                return prefix + System.Convert.ToBase64String(bytes);
            }
        }

        public static string ToBase64(this Image image, bool imageData = true)
        {
            if (image == null) return null;

            var file = new MemoryStream();
            image.Save(file, ImageFormat.Png);
            file.Seek(0, SeekOrigin.Begin);

            var bytes = new byte[file.Length];
            file.Read(bytes, 0, bytes.Length);
            file.Close();

            return imageData
                ? "data:image/png;base64," + System.Convert.ToBase64String(bytes)
                : System.Convert.ToBase64String(bytes);
        }

        #endregion

        #region EncodeBase64
        public static string EncodeBase64(this string value)
        {
            if (value.IsNullOrEmpty()) return value;

            return System.Convert.ToBase64String(Encoding.UTF8.GetBytes(value));
        } 
        #endregion

        #region DecodeBase64
        public static string DecodeBase64(this string value)
        {
            if (value.IsNullOrEmpty()) return value;

            return Encoding.UTF8.GetString(System.Convert.FromBase64String(value));
        }
        #endregion

        #region ToObject

        public static T ToObject<T>(this FormCollection fc)
            where T : class
        {
            return (T)fc.ToObject(typeof(T));
        }

        public static object ToObject(this FormCollection fc, Type type, BaseController controller = null)
        {
            const BindingFlags bindFlags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.SetProperty;
            var properties = type.GetProperties(bindFlags);
            var obj = Activator.CreateInstance(type);

            if (controller != null)
            {
                var collection = new System.Collections.Specialized.NameValueCollection();
                fc.AllKeys.Action(x => collection.Add(x, fc[x]));
                controller.UpdateModelEx(obj, collection);
            }

            foreach (var prop in properties)
            {
                var pi = fc[prop.Name];
                if (pi == null) continue;
                prop.SetValue(obj, ChangeType(pi, prop.PropertyType));
            }

            return obj;
        }

        #endregion

        #region ToStringSafe
        public static string ToStringSafe(this object obj)
        {
            return obj == null ? null : obj.ToString();
        }

        public static string ToStringSafe(this object obj, string emptyVal)
        {
            var ret = obj == null ? null : obj.ToString();
            return string.IsNullOrEmpty(ret) ? emptyVal : ret;
        }

        #endregion

        #region TryParseInt
        public static int? TryParseInt(this string value)
        {
            int oVal;
            if (!value.IsNullOrEmpty() && int.TryParse(value, out oVal)) return oVal;
            return null;
        }
        #endregion

        #region UrlEncode
        public static string UrlEncode(this object value)
        {
            if (value == null) return null;
            return HttpUtility.UrlEncode(value.ToString());
        }
        #endregion

        #region UrlDecode
        public static string UrlDecode(this object value)
        {
            if (value == null) return null;
            return HttpUtility.UrlDecode(value.ToString());
        }
        #endregion

        #region ResolveHostName
        public static string ResolveHostName(this string ipAddress)
        {
            return ipAddress;

            if (ipAddress.IsNullOrEmpty()) return ipAddress;

            try
            {
                IPAddress ip;
                if (!IPAddress.TryParse(ipAddress, out ip)) return ipAddress;

                var entry = Dns.GetHostEntry(ip);

                return entry == null ? ipAddress : entry.HostName;
            }
            catch (Exception e)
            {
                return ipAddress;
            }
        }
        #endregion

        #region UrlInjectParam
        public static string UrlInjectParam(this string url, string name, string value)
        {
            if (url.IsNullOrEmpty() || name.IsNullOrEmpty()) return url;

            return url + (url.IndexOf('?') == -1 ? '?' : '&') + string.Format("{0}={1}", name, HttpUtility.UrlEncode(value ?? ""));
        }
        #endregion

        #region Extract

        public static Dictionary<string, string> Extract<T>(this T obj, List<string> fields)
        {
            var viewData = new ViewDataDictionary<T>(obj);
            return fields.ToDictionary(x => x, x =>
            {
                var meta = ModelMetadata.FromStringExpression(x, viewData);

                if (meta == null || meta.Model == null) return string.Empty;
                var type = meta.ModelType;
                if (MapperInt.PrimitiveExtensions.IsNullableType(type))
                    type = MapperInt.PrimitiveExtensions.GetTypeOfNullable(type);

                if (type == typeof(decimal) || type == typeof(float) || type == typeof(double))
                {
                    return ((decimal)meta.Model).ToString("F4");
                }
                else return meta.Model.ToString();
            });
        }

        #endregion

        #region GetChanges

        public static Dictionary<string, string> GetChanges(this Dictionary<string, string> src,
            Dictionary<string, string> dst)
        {
            var ret = new Dictionary<string, string>();

            if (src == null || dst == null) return ret;

            foreach (var itm in src.Join(dst, s => s.Key, d => d.Key, (s, d) => new { Src = s, Dst = d }))
            {
                if ((itm.Src.Value ?? "") != (itm.Dst.Value ?? ""))
                {
                    ret.Add(itm.Src.Key, itm.Src.Value);
                }
            }

            return ret;
        }

        #endregion

        #region SafeGet

        public static TVal SafeGet<TKey, TVal>(this Dictionary<TKey, TVal> obj, TKey key)
            where TVal : class
        {
            TVal oVal;

            if (obj != null && obj.TryGetValue(key, out oVal)) return oVal;

            return null;
        }

        public static TVal SafeGet<TKey, TVal>(this IDictionary<TKey, TVal> obj, TKey key)
            where TVal : class
        {
            TVal oVal;

            if (obj != null && obj.TryGetValue(key, out oVal)) return oVal;

            return null;
        }

        #endregion

        #region SafeInvoke

        public static TVal SafeInvokeProperty<TVal>(this object obj, string property)
        {
            if (obj == null) return default(TVal);

            var fProp = obj.GetType().GetProperty(property);

            return fProp == null ? default(TVal) : (TVal)fProp.GetValue(obj);
        }

        public static void SafeInvokeProperty(this object obj, string property, object value)
        {
            if (obj == null) return;

            var fProp = obj.GetType().GetProperty(property);

            if (fProp != null) fProp.SetValue(obj, value);
        }

        public static TVal SafeInvokeMethod<TVal>(this object obj, string method, params object[] args) 
        {
            return SafeInvokeMethodEx(obj, method, default(TVal), args);
        }

        public static TVal SafeInvokeMethodEx<TVal>(this object obj, string method, TVal defVal, params object[] args)
        {
            if (obj == null) return defVal;

            var fFunc = obj.GetType().GetMethod(method);

            return fFunc == null ? defVal : (TVal) fFunc.Invoke(obj, args);
        }

        #endregion

        #region GetSession

        public static ISession GetSession(this IDB db)
        {
            if (db == null || db.DataService == null) return null;

            return ((DataService)db.DataService).Session;
        }

        #endregion

        #region GenerateAlias

        public static string GenerateAlias(this string value, int maxLen = int.MaxValue)
        {
            if (value.IsNullOrEmpty()) return value;

            const string aX = "æåäàáạảãâầấậẩẫăằắặẳẵÆÅÄÀÁẠẢÃÂẦẤẬẨẪĂẰẮẶẲẴ";
            const string eX = "èéẹẻẽêềếệểễÈÉẸẺẼÊỀẾỆỂỄ";
            const string iX = "ìíịỉĩÌÍỊỈĨ";
            const string oX = "øöòóọỏõôồốộổỗơờớợởỡØÖÓÒỎÕỌÔỐỒỔỖỘƠỚỜỞỠỢ";
            const string uX = "ùúụủũưừứựửữÚÙỦŨỤƯỨỪỬỮỰüÜ";
            const string yX = "ỳýỵỷỹÝỲỶỸỴ";
            const string dX = "đðĐÐ";
            const string nX = "ñÑ";
            const string xX = "!@%^*()+=<>?/,.:;\' \"&#[]~$_";

            var arr = new[] { aX, "a", eX, "e", iX, "i", oX, "o", uX, "u", yX, "y", dX, "d", nX, "n", xX, " " };
            var ret = new StringBuilder();

            foreach (var c in value.Trim(' ', '-', '_'))
            {
                char t = c;
                for (int i = 0; i < arr.Length; i += 2)
                {
                    if (-1 != arr[i].IndexOf(c))
                    {
                        t = arr[i + 1][0];
                        break;
                    }
                }
                if (char.IsWhiteSpace(t)) ret.Append('-');
                else if (char.IsLetterOrDigit(t)) ret.Append(char.ToLower(t));
            }

            value = ret.Length < maxLen ? ret.ToString() : ret.ToString(0, maxLen);

            return Regex.Replace(value, @"\-{2,}", "-", RegexOptions.Compiled).Trim('-');
        }

        #endregion

        #region GenerateBarcode

        public static string GenerateBarcode(this string barcode, Spire.Barcode.BarCodeType? type = null,
            int? length = null, int dpi = 96, int? width = 300, int? height = 25, int cropY = 30)
        {
            length = length ?? Math.Max(4, Math.Min(50, SettingsBiz.GetSettings().Display.BarCode.CharLength));

            var img = BaseController.GenerateBarCode(barcode.PadLeft(length.Value, '0'),
                null, width, height, dpi, dpi, height);

            return BaseController.CropImage(img, 0, cropY, img.Width, img.Height - cropY).ToBase64();
        }

        #endregion

        #region ComputeHash

        public static string ComputeHash(this string value)
        {
            if (value.IsNullOrEmpty()) return value;

            var md5 = MD5.Create();
            var bytes = md5.ComputeHash(Encoding.UTF8.GetBytes(value));
            return bytes.Select(m => m.ToString("x2")).ToString(""); 
        }

        #endregion

        #region CalcAutoWidth
        public static int CalcAutoWidth(this HtmlHelper htmlHelper, int height = 40, int defVal = 0)
        {
            var controller = htmlHelper.ViewContext.Controller as BaseController;

            return controller == null ? defVal : controller.CalcAutoWidth(height);
        } 
        #endregion

        #region ContainsToLower
        /// <summary>
        ///  Contains ToLower
        /// </summary>
        public static bool ContainsToLower(this string value, string target)
        {
            if (value == null) return target == null;
            else return value.ToLower().Contains(target.ToLower());
        }
        #endregion

        #region NullIf

        public static int? NullIf(this int value, Predicate<int> cond)
        {
            return cond(value) ? (int?)null : value;
        }

        public static decimal? NullIf(this decimal value, Predicate<decimal> cond)
        {
            return cond(value) ? (decimal?)null : value;
        }
 
        #endregion

        #region BitmaskCmp

        public static bool BitmaskCmp(this byte bitmask, byte value)
        {
            return (bitmask & value) == value;
        }

        public static bool BitmaskCmp(this int bitmask, int value)
        {
            return (bitmask & value) == value;
        }

        public static bool BitmaskCmp(this long bitmask, long value)
        {
            return (bitmask & value) == value;
        }

        #endregion

        #region SubstringSafe

        public static string SubstringSafe(this string value, int index, int length)
        {
            if (value == null || index < 0 || index >= value.Length) return null;

            return value.Substring(index, Math.Min(length, value.Length - index));
        }

        #endregion

        #region JsonDeserializeSafe

        public static T JsonDeserializeSafe<T>(this string json) where T : class
        {
            if (json.IsNullOrEmpty()) return null;

            try
            {
                return JsonConvert.DeserializeObject<T>(json);
            }
            catch (Exception err)
            {
                if (Debugger.IsAttached) Debugger.Break();
                return null;
            }
        }

        #endregion

        #region TrimLength

        public static string TrimLength(this string value, int maxLen)
        {
            if (value.IsNullOrEmpty()) return value;

            return value.Length <= maxLen ? value : value.Substring(0, maxLen);
        } 

        #endregion

        #region TrimPhone

        public static string TrimPhone(this string value, bool prefix = false)
        {
            if (value.IsNullOrEmpty()) return value;

            var ret = Regex.Replace(value, @"\s+|[\(\)]+", "");

            return prefix ? (ret.Length <= 8 ? "+47" + ret : ret) : ret;
        }

        #endregion

        #region ToKid
        public static string ToKid(this string digits)
        {
            if (digits.IsNullOrEmpty()) return digits;

            Func<string, int> makeLuhn = d =>
            {
                var res = 0;
                var inc = d.Length % 2;
                for (var i = 0; i < d.Length; ++i)
                {
                    var n = int.Parse(d[i].ToString()) * (2 - (i + inc) % 2);
                    res += n > 9 ? n - 9 : n;
                }
                return res;
            };

            Func<string, int> luhnCheckDigit = value =>
            {
                var cs = makeLuhn(value + "0") % 10;
                return cs != 0 ? 10 - cs : 0;
            };

            return digits + luhnCheckDigit(digits);
        } 
        #endregion

        #region RestApiInvoke

        public static string RestApiInvoke(this string url, object data = null, string method = null, Action<HttpWebRequest> fPreExec = null)
        {
            const SecurityProtocolType securFlags = SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;

            if ((ServicePointManager.SecurityProtocol & securFlags) != securFlags) ServicePointManager.SecurityProtocol |= securFlags;

            var tries = 3;
        LB_LOOP:
            try
            {
                var wReq = WebRequest.CreateHttp(url);

                wReq.Method = method ?? (data == null ? "GET" : "POST");
                wReq.Accept = "application/json";

                if (data != null)
                {
                    wReq.ContentType = "application/json";
                    var txtDat = data is string ? (string)data : JsonConvert.SerializeObject(data, new JsonSerializerSettings() { NullValueHandling = NullValueHandling.Ignore });
                    using (var rs = wReq.GetRequestStream())
                    {
                        using (var s = new StreamWriter(rs)) s.Write(txtDat);
                    }
                }

                if (fPreExec != null) fPreExec(wReq);

                var wResp = (HttpWebResponse)wReq.GetResponse();
                using (var rs = wResp.GetResponseStream())
                {
                    if (rs == null) return null;
                    using (var s = new StreamReader(rs)) return s.ReadToEnd();
                }
            }
            catch (Exception err)
            {
                var errMsg = "";
                var wer = err as WebException;
                var resp = wer == null ? null : wer.Response as HttpWebResponse;
                if (resp != null)
                {
                    using (var rs = resp.GetResponseStream())
                    {
                        if (rs != null) using (var s = new StreamReader(rs)) errMsg = (s.ReadToEnd() ?? "").Trim();
                    }
                    if (new[] { HttpStatusCode.BadRequest, HttpStatusCode.InternalServerError, HttpStatusCode.Unauthorized, HttpStatusCode.Forbidden, HttpStatusCode.PaymentRequired }.Contains(resp.StatusCode))
                        throw new WebException(string.IsNullOrEmpty(errMsg) ? err.Message : errMsg, err, wer.Status, resp);
                }
                if (--tries >= 0)
                {
                    Thread.Sleep(3000); goto LB_LOOP;
                }
                else throw new WebException(string.IsNullOrEmpty(errMsg) ? err.Message : errMsg, err);
            }
        }

        #endregion

        #region TryUpdateProperties

        public static void TryUpdateProperties(this object obj, params KeyValuePair<string, object>[] items)
        {
            if (obj == null) return;

            var type = obj.GetType();

            foreach (var itm in items)
            {
                var prop = type.GetProperty(itm.Key);
                if (prop != null) prop.SetValue(obj, itm.Value);
            }
        }

        #endregion

        #region Validate
        public static List<System.Tuple<ValidationAttribute, PropertyInfo, string>> Validate<TModel>(this TModel model) where TModel : class
        {
            if (model == null) return null;

            var ret = new List<System.Tuple<ValidationAttribute, PropertyInfo, string>>();

            //typeof(TModel).GetCustomAttributes<ValidationAttribute>()
            //    .Where(m => !m.IsValid(model))
            //    .Action(m => new System.Tuple<ValidationAttribute, PropertyInfo, string>(m, null,m.FormatErrorMessage("")));

            typeof(TModel).GetProperties()
                .Select(m => new { Prop = m, Attrs = m.GetCustomAttributes<ValidationAttribute>() })
                .SelectMany(m => m.Attrs.Where(z => !z.IsValid(m.Prop.GetValue(model))).Select(z => new { m.Prop, Attr = z }))
                .Action(m => ret.Add(new System.Tuple<ValidationAttribute, PropertyInfo, string>(m.Attr, m.Prop, m.Attr.FormatErrorMessage(m.Prop.Name))));

            return ret;
        } 
        #endregion

        #region ValidNHibernateProxy

        public static TDom ValidNHibernateProxy<TDom>(this TDom obj, Func<TDom, object> fId = null, IDB db = null)
            where TDom : class
        {
            NHibernate.Proxy.INHibernateProxy proxy;

            if (obj == null || null == (proxy = (obj as NHibernate.Proxy.INHibernateProxy))) return obj;

            if (proxy.HibernateLazyInitializer == null ||
                proxy.HibernateLazyInitializer.Session != null && proxy.HibernateLazyInitializer.Session.IsOpen)
                return obj;

            db = db ?? new DefaultDb();

            if (fId == null)
            {
                var idom = obj as Domain.IDom;
                return db.GetSession().Get<TDom>(idom == null ? typeof(TDom).GetProperty("Id").GetValue(obj) : idom.Id);   
            }
            return db.GetSession().Get<TDom>(fId(obj));   
        } 

        #endregion

        #region CloneFiles
        public static string[] CloneFiles(this string[] files)
        {
            if (files.SafeCount() == 0) return files;

            var tempFolder = ModuleUrl.TempThumbFolder;
            return files.Select(m =>
            {
                var dF = Globals.MapPath(tempFolder.CombineWPath(Path.GetFileName(m), '/'));
                if (BaseController.TryMoveFile(Globals.MapPath(m), ref dF, false, false)) return tempFolder.CombineWPath(Path.GetFileName(dF), '/');
                else return null;
            }).Where(m => m != null).ToArray();
        }
        #endregion

        #region GetList
        public static List<Tval> GetList<Tval, Tid>(this List<Tid> ids, int chunkSize, Func<Tid[], IEnumerable<Tval>> fData)
        {
            var ret = new List<Tval>();

            for (var i = 0; i < ids.Count; i += chunkSize)
            {
                var tmpIds = new List<Tid>();
                for (var z = i; z < i + chunkSize && z < ids.Count; z++) tmpIds.Add(ids[z]);
                var tmpRet = fData(tmpIds.ToArray());
                if (tmpRet != null) ret.AddRange(tmpRet);
            }

            return ret;
        }
        #endregion

    }
}
