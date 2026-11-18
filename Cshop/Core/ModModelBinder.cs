using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Drawing;
using System.Web.Script.Serialization;
using Claw.Modules.Eshop.DAL;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Web;
using System.Web.Compilation;
using System.Web.Mvc;
using Claw.Modules.Eshop.Helper;
using System.ComponentModel;
using AutoMapper;
using Claw.Modules.Eshop.Models;
using Claw.Modules.Eshop.Domain;
using Claw.Modules.Eshop.Helper;
using Newtonsoft.Json;

namespace Claw.Modules.Eshop
{
    #region ModModelBinder

    public class ModModelBinder : DefaultModelBinder
    {
        private readonly string _typeNameKey;

        #region ModModelBinder

        public ModModelBinder(string typeNameKey = null)
        {
            _typeNameKey = typeNameKey ?? "__type__";
        }

        #endregion

        #region BindModel
        /// <summary>return model from cxtBinding</summary>
        public override object BindModel(ControllerContext cxtController, ModelBindingContext cxtBinding)
        {
            if (cxtBinding.ModelType == typeof(ODateTime))
            {
                return (ODateTime)GetDateValue(cxtBinding, "Value");
            }

            if (cxtBinding.ModelType == typeof(OColor))
            {
                var isTrans = GetBoolValue(cxtBinding, "IsTransparent");
                return (OColor)(isTrans ? "transparent" : GetValue(cxtBinding, "Value"));
            }

            if (cxtBinding.ModelType == typeof(OFloat))
            {
                return (OFloat)GetFloatValue(cxtBinding);
            }

            if (cxtBinding.ModelType == typeof(OMoney))
            {
                return (OMoney)GetMoneyValue(cxtBinding);
            }

            if (cxtBinding.ModelType == typeof(OMoneyMc))
            {
                return (OMoneyMc)GetMoneyMcValue(cxtBinding);
            }

            if (cxtBinding.ModelType == typeof(OInt))
            {
                return (OInt)GetIntValue(cxtBinding);
            }

            return base.BindModel(cxtController, cxtBinding);
        }

        #endregion

        #region GetDateValue
        /// <summary>convert cxtBinding to DateTime</summary>
        /// <param name="name">name="Value"</param>
        private DateTime? GetDateValue(ModelBindingContext cxtBinding, string name)
        {
            var key = name;
            if (!string.IsNullOrEmpty(cxtBinding.ModelName)) key = cxtBinding.ModelName + '.' + key;
            var val = cxtBinding.ValueProvider.GetValue(key);
            var n = 3;

            while (--n >= 0)
            {
                if (val == null || string.IsNullOrWhiteSpace(val.AttemptedValue))
                    val = cxtBinding.ValueProvider.GetValue(cxtBinding.ModelName + (n == 2 ? ".Text" : ""));
                else break;
            }

            if (n < 0)
            {
                if (cxtBinding.ModelMetadata.IsRequired)
                    cxtBinding.ModelState.AddModelError(key, StringTable.ErrMsgRequiredField);
                return null;
            }

            DateTime oVal;
            var id = GetId(cxtBinding);
            string format = ODateTime.FormatString(id);

            if (val.AttemptedValue.StartsWith("/Date(", StringComparison.OrdinalIgnoreCase))
            {
                long iVal;
                if (long.TryParse(val.AttemptedValue.Substring(6).TrimEnd(new[] { ')', '/' }), out iVal))
                {
                    oVal = new DateTime(1970, 1, 1);
                    return oVal.AddMilliseconds(iVal);
                }
            }

            if (DateTime.TryParseExact(val.AttemptedValue, format, null, DateTimeStyles.None, out oVal))
                return oVal;
            else if (DateTime.TryParseExact(val.AttemptedValue, ODateTime.FormatString(id, true, true), null,
                DateTimeStyles.None, out oVal))
                return oVal;
            else if (DateTime.TryParseExact(val.AttemptedValue, ODateTime.FormatString(id, true), null,
                DateTimeStyles.None, out oVal))
                return oVal;
            else
            {
                cxtBinding.ModelState.AddModelError(cxtBinding.ModelName, StringTable.ErrMsgInvalidDateTimeFormat);
                cxtBinding.ModelState.AddModelError(key, StringTable.ErrMsgInvalidDateTimeFormat);
                return null;
            }
        }

        #endregion
        #region GetFloatValue
        /// <summary>convert cxtBinding to float</summary>
        private double? GetFloatValue(ModelBindingContext cxtBinding)
        {
            var val = cxtBinding.ValueProvider.GetValue(cxtBinding.ModelName);
            if (val == null) val = cxtBinding.ValueProvider.GetValue(cxtBinding.ModelName + ".Text");

            if (val == null || string.IsNullOrWhiteSpace(val.AttemptedValue))
            {
                if (cxtBinding.ModelMetadata.IsRequired)
                    cxtBinding.ModelState.AddModelError(cxtBinding.ModelName, StringTable.ErrMsgRequiredField);

                return null;
            }
            else
            {
                double oVal; var fInfo = OFloat.FormatInfo(GetId(cxtBinding));

                if (double.TryParse(val.AttemptedValue, NumberStyles.Number, fInfo, out oVal)) return oVal;
                else
                {
                    cxtBinding.ModelState.AddModelError(cxtBinding.ModelName, StringTable.ErrMsgInvalidNumberFormat);
                    return null;
                }
            }
        }

        #endregion
        #region GetMoneyValue
        /// <summary>convert cxtBinding to decimal</summary>
        private decimal? GetMoneyValue(ModelBindingContext cxtBinding)
        {
            var val = cxtBinding.ValueProvider.GetValue(cxtBinding.ModelName);
            if (val == null) val = cxtBinding.ValueProvider.GetValue(cxtBinding.ModelName + ".Text");

            if (val == null || string.IsNullOrWhiteSpace(val.AttemptedValue))
            {
                if (cxtBinding.ModelMetadata.IsRequired)
                    cxtBinding.ModelState.AddModelError(cxtBinding.ModelName, StringTable.ErrMsgRequiredField);

                return null;
            }
            else
            {
                decimal oVal; var fInfo = OMoney.FormatInfo(GetId(cxtBinding));

                if (decimal.TryParse(val.AttemptedValue, NumberStyles.Number, fInfo, out oVal)) return oVal;
                else
                {
                    cxtBinding.ModelState.AddModelError(cxtBinding.ModelName, StringTable.ErrMsgInvalidNumberFormat);
                    return null;
                }
            }
        }

        #endregion
        #region GetMoneyMcValue
        /// <summary>convert cxtBinding to decimal</summary>
        private decimal? GetMoneyMcValue(ModelBindingContext cxtBinding)
        {
            var val = cxtBinding.ValueProvider.GetValue(cxtBinding.ModelName);
            if (val == null) val = cxtBinding.ValueProvider.GetValue(cxtBinding.ModelName + ".Text");

            if (val == null || string.IsNullOrWhiteSpace(val.AttemptedValue))
            {
                if (cxtBinding.ModelMetadata.IsRequired)
                    cxtBinding.ModelState.AddModelError(cxtBinding.ModelName, StringTable.ErrMsgRequiredField);

                return null;
            }
            else
            {
                var ret = OMoneyMc.TryParse(val.AttemptedValue);

                if (ret != null) return ret;
                else
                {
                    cxtBinding.ModelState.AddModelError(cxtBinding.ModelName, StringTable.ErrMsgInvalidNumberFormat);
                    return null;
                }
            }
        }

        #endregion
        #region GetIntValue
        /// <summary>convert cxtBinding to int</summary>
        private int? GetIntValue(ModelBindingContext cxtBinding)
        {
            var val = cxtBinding.ValueProvider.GetValue(cxtBinding.ModelName);
            if (val == null) val = cxtBinding.ValueProvider.GetValue(cxtBinding.ModelName + ".Text");

            if (val == null || string.IsNullOrWhiteSpace(val.AttemptedValue))
            {
                if (cxtBinding.ModelMetadata.IsRequired)
                    cxtBinding.ModelState.AddModelError(cxtBinding.ModelName, StringTable.ErrMsgRequiredField);

                return null;
            }
            else
            {
                int oVal; var fInfo = OInt.FormatInfo(GetId(cxtBinding));

                if (int.TryParse(val.AttemptedValue, NumberStyles.Number, fInfo, out oVal)) return oVal;
                else
                {
                    cxtBinding.ModelState.AddModelError(cxtBinding.ModelName, StringTable.ErrMsgInvalidNumberFormat);
                    return null;
                }
            }
        }

        #endregion
        #region GetBoolValue
        /// <summary>convert cxtBinding to bool</summary>
        private bool GetBoolValue(ModelBindingContext cxtBinding, string key)
        {
            if (!string.IsNullOrEmpty(cxtBinding.ModelName)) key = cxtBinding.ModelName + '.' + key;
            var val = cxtBinding.ValueProvider.GetValue(key);

            if (val == null) return false;
            else if (val.AttemptedValue.EqualsIgCase("on")) return true;
            else return false;
        }

        #endregion
        #region GetValue
        /// <summary>extract data from model by key</summary>
        private string GetValue(ModelBindingContext cxtBinding, string key)
        {
            if (!string.IsNullOrEmpty(cxtBinding.ModelName)) key = cxtBinding.ModelName + '.' + key;
            var val = cxtBinding.ValueProvider.GetValue(key);

            if (val == null) return string.Empty;
            else return val.AttemptedValue;
        }

        #endregion
        #region GetId
        /// <summary>extract model id from cxtBinding</summary>
        private int GetId(ModelBindingContext cxtBinding)
        {
            var val = cxtBinding.ValueProvider.GetValue("Id");
            int oVal;
            if (val != null && int.TryParse(val.AttemptedValue, out oVal)) return oVal;
            else return 0;
        }

        #endregion
    }

    #endregion

    #region ModModelBinderAttribute

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false, Inherited = false)]
    public class ModModelBinderAttribute : CustomModelBinderAttribute
    {
        public override IModelBinder GetBinder()
        {
            return new ModModelBinder();
        }
    }

    #endregion

    #region ODateTime
    /// <summary>wrapper class for handling datetime format in multi-language</summary>
    [TypeConverter(typeof(ODateTime.Converter))]
    [ModModelBinder]
    public class ODateTime : IFormattable
    {
        #region JsonConverter
        public class JsonConverter : JsonConverter<ODateTime>
        {
            public override void WriteJson(JsonWriter writer, ODateTime value, JsonSerializer serializer)
            {
                writer.WriteValue(value == null || value.Value == null ? "" : value.ToString(true));
            }

            public override ODateTime ReadJson(JsonReader reader, Type objectType, ODateTime existingValue, bool hasExistingValue, JsonSerializer serializer)
            {
                var s = (string)reader.Value;

                if (s.IsNullOrEmpty()) return null;

                return TryParse(s);
            }
        }
        #endregion
        #region Converter
        /// <summary>convert from string to OFloat</summary>
        public class Converter : TypeConverter
        {
            public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
            {
                if (sourceType == typeof(string)) return true;
                return base.CanConvertFrom(context, sourceType);
            }

            public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
            {
                if (value is string)
                {
                    var txt = (string)(value ?? "");
                    var val = TryParse(txt);
                    if (val != null || txt.IsNullOrEmpty()) return val;
                }
                return base.ConvertFrom(context, culture, value);
            }

            public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
            {
                var cxt = HttpContext.Current;

                //if (cxt != null && cxt.Request.RawUrl.StartsWith("/api/cpos/")) return false;

                return base.CanConvertTo(context, destinationType);
            }

            public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
            {
                if (destinationType == typeof(string))
                    return ((ODateTime)value).ToString();

                return base.ConvertTo(context, culture, value, destinationType);
            }
        }
        #endregion

        public DateTime? Value { get; set; }

        #region ODateTime

        public ODateTime(DateTime value)
        {
            Value = value;
        }

        #endregion

        #region FormatString

        public static string FormatString(int? id = null, bool fullStyle = false, bool shortTime = false)
        {
            var setting = SettingsBiz.GetSettings().Display.Others;
            DateTime oVal;
            string format = fullStyle ? setting.GetDateTimeFormat(shortTime) : setting.GetDateFormat();

            return TranslateFormat(format);
        }

        #endregion

        #region GetMonthYearText

        public string GetMonthYearText()
        {
            var setting = SettingsBiz.GetSettings().Display.Others;

            return Value == null ? "" : Value.Value.ToString(TranslateFormat(setting.GetMonthYearFormat()));
        }

        #endregion

        #region TranslateFormat

        public static string TranslateFormat(string format)
        {
            if (format.IndexOf('\\') == -1) format = format.Replace("/", "\\/");
            else format = format.Replace(@"\", @"\\");

            return format;
        }

        #endregion

        #region Text
        /// <summary>Display text</summary>
        public string Text
        {
            get { return Value == null ? string.Empty : ToString(); }
        }

        #endregion

        #region TextF
        /// <summary>Display text in full style</summary>
        public string TextF
        {
            get { return Value == null ? string.Empty : Value.Value.ToString(FormatString(null, true)); }
        }

        #endregion

        #region ToString

        public string ToString(string format, IFormatProvider formatProvider)
        {
            return ToString();
        }

        public override string ToString()
        {
            return ToString(false);

        }

        public string ToString(bool fullStyle)
        {
            return Value == null ? "" : Value.Value.ToString(FormatString(fullStyle: fullStyle));
        }

        #endregion

        #region Equals

        public override bool Equals(object obj)
        {
            if (Value == null) return obj == null;
            else return Value.Equals(obj);
        }

        #endregion

        #region GetHashCode

        public override int GetHashCode()
        {
            return Value == null ? 0 : Value.GetHashCode();
        }

        #endregion

        #region implicit operator

        public static implicit operator DateTime?(ODateTime obj)
        {
            return obj == null ? (DateTime?)null : obj.Value;
        }

        public static implicit operator ODateTime(DateTime? value)
        {
            return value == null ? null : new ODateTime(value.Value);
        }

        #endregion

        #region compare operator

        public static bool operator >(ODateTime a, ODateTime b)
        {
            if ((object)a == null || (object)b == null || a.Value == null || b.Value == null) return false;
            else return a.Value > b.Value;
        }

        public static bool operator >=(ODateTime a, ODateTime b)
        {
            if ((object)a == null || (object)b == null || a.Value == null || b.Value == null) return false;
            else return a.Value >= b.Value;
        }

        public static bool operator <(ODateTime a, ODateTime b)
        {
            if ((object)a == null || (object)b == null || a.Value == null || b.Value == null) return false;
            else return a.Value < b.Value;
        }

        public static bool operator <=(ODateTime a, ODateTime b)
        {
            if ((object)a == null || (object)b == null || a.Value == null || b.Value == null) return false;
            else return a.Value <= b.Value;
        }

        public static bool operator ==(ODateTime a, ODateTime b)
        {
            if ((object)a == null || (object)b == null || a.Value == null || b.Value == null)
            {
                if ((object)a == null && (object)b == null) return true;
                if ((object)a == null ^ (object)b == null) return false;
                if (a.Value == null && b.Value == null) return true;
                return false;
            }
            else return a.Value == b.Value;
        }

        public static bool operator !=(ODateTime a, ODateTime b)
        {
            return !(a == b);
        }

        #endregion

        #region TryParse

        public static ODateTime TryParse(string value, int? id = null)
        {
            if (string.IsNullOrEmpty(value)) return null;

            DateTime oVal;
            string fInfo0 = FormatString(id, false), fInfo1 = FormatString(id, true), fInfo2 = FormatString(id, true, true);

            if (DateTime.TryParseExact(value, new[] { fInfo0, fInfo1, fInfo2, "O" }, null, DateTimeStyles.None, out oVal)) return new ODateTime(oVal);
            else return null;
        }

        #endregion

        #region GetValue

        public static ODateTime GetValue(string key, int?id = null)
        {
            var value = HttpContext.Current.Request[key];

            if (string.IsNullOrEmpty(value)) return null;

            return TryParse(value, id);
        }

        #endregion
    }

    #endregion

    #region OColor
    [ModModelBinder]
    public class OColor
    {
        public string Value { get; set; }

        [Display(Name = "Use transparent")]
        public bool IsTransparent { get; set; }

        public OColor(string value)
        {
            Value = value;
            IsTransparent = value.EqualsIgCase("transparent");
            if (IsTransparent) Value = "#FFFFFF";
        }

        public override string ToString()
        {
            return (string)this;
        }

        public static implicit operator string(OColor obj)
        {
            return obj == null ? null : (obj.IsTransparent ? "transparent" : obj.Value);
        }

        public static implicit operator OColor(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : new OColor(value);
        }
    }
    #endregion

    #region OFloat
    /// <summary>wrapper class for handling float format in multi-language</summary>
    [TypeConverter(typeof(OFloat.Converter))]
    [ModModelBinder]
    public class OFloat : IFormattable, IComparable
    {
        #region JsonConverter
        public class JsonConverter : JsonConverter<OFloat>
        {
            public override void WriteJson(JsonWriter writer, OFloat value, JsonSerializer serializer)
            {
                writer.WriteValue(value == null || value.Value == null ? "" : string.Format("{0}@{1}", value.Text, value.Value.Value.ToString(CultureInfo.InvariantCulture)));
            }

            public override OFloat ReadJson(JsonReader reader, Type objectType, OFloat existingValue, bool hasExistingValue, JsonSerializer serializer)
            {
                var s = (string)reader.Value;

                if (s.IsNullOrEmpty()) return new OFloat();

                var idx = s.IndexOf('@');

                if (idx == -1) return TryParse(s);

                return new OFloat(decimal.Parse(s.Substring(idx + 1), CultureInfo.InvariantCulture));
            }
        }
        #endregion
        #region Converter
        /// <summary>convert from string to OFloat</summary>
        public class Converter : TypeConverter
        {
            public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
            {
                if (sourceType == typeof(string)) return true;
                return base.CanConvertFrom(context, sourceType);
            }

            public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
            {
                if (value is string)
                {
                    var txt = ((string)(value ?? "")).TrimEnd('%');
                    var val = TryParse(txt);
                    if (val != null || txt.IsNullOrEmpty()) return val;
                }
                return base.ConvertFrom(context, culture, value);
            }

            public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
            {
                var cxt = HttpContext.Current;

                //if (cxt != null && cxt.Request.RawUrl.StartsWith("/api/cpos/")) return false;

                return base.CanConvertTo(context, destinationType);
            }

            public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
            {
                if (destinationType == typeof(string))
                    return ((OFloat)value).ToString();

                return base.ConvertTo(context, culture, value, destinationType);
            }
        }
        #endregion

        internal int DecimalPlaces = 2;
        public double? Value { get; set; }

        #region OFloat

        public OFloat(double value)
        {
            Value = value;
        }

        public OFloat(double? value = null)
        {
            Value = value;
        }

        public OFloat(decimal value)
        {
            Value = (double)value;
        }

        public OFloat(decimal? value)
        {
            Value = (double?)value;
        }

        public OFloat(int value)
        {
            Value = (double)value;
        }

        #endregion

        #region FormatInfo

        public static NumberFormatInfo FormatInfo(int? id = null, int? decimalPlaces = null)
        {
            var lang = LanguageMgr.OptLang(id ?? (QParam.GetId() ?? 0));
            var finfo = new NumberFormatInfo()
            {
                NumberGroupSeparator = lang.ThousandsSeperator,
                NumberDecimalSeparator = lang.DecimalSeperator,
                NumberDecimalDigits = decimalPlaces ?? 2,
            };

            return finfo;
        }

        #endregion

        #region Text

        public string Text
        {
            get { return Value == null ? string.Empty : ToString(); }
        }

        #endregion

        #region ToString

        public string ToString(string format, IFormatProvider formatProvider)
        {
            return ToString();
        }

        public override string ToString()
        {
            return Value == null ? "" : Value.Value.ToString("N", FormatInfo(null, DecimalPlaces));
        }

        #endregion

        #region Equals

        public override bool Equals(object obj)
        {
            if (Value == null) return obj == null;
            else return Value.Equals(obj);
        }

        #endregion

        #region GetHashCode

        public override int GetHashCode()
        {
            return Value == null ? 0 : Value.GetHashCode();
        }

        #endregion

        #region implicit operator

        public static implicit operator double?(OFloat obj)
        {
            return obj == null ? null : obj.Value;
        }

        public static implicit operator double(OFloat obj)
        {
            return obj == null ? 0 : obj.Value ?? 0;
        }

        public static implicit operator float?(OFloat obj)
        {
            return obj == null ? null : (float?)obj.Value;
        }

        public static implicit operator float(OFloat obj)
        {
            return obj == null ? 0 : ((float?)obj.Value) ?? 0;
        }

        public static implicit operator decimal?(OFloat obj)
        {
            return obj == null ? null : (decimal?)obj.Value;
        }

        public static implicit operator decimal(OFloat obj)
        {
            return obj == null ? 0m : (decimal?)obj.Value ?? 0m;
        }

        public static implicit operator OFloat(float? value)
        {
            return value == null ? null : new OFloat(value.Value);
        }

        public static implicit operator OFloat(double? value)
        {
            return value == null ? null : new OFloat(value.Value);
        }

        public static implicit operator OFloat(decimal? value)
        {
            return value == null ? null : new OFloat(value.Value);
        }

        #endregion

        #region compare operator

        public static bool operator >(OFloat a, OFloat b)
        {
            if ((object)a == null || (object)b == null || a.Value == null || b.Value == null) return false;
            else return a.Value > b.Value;
        }

        public static bool operator >=(OFloat a, OFloat b)
        {
            if ((object)a == null || (object)b == null || a.Value == null || b.Value == null) return false;
            else return a.Value >= b.Value;
        }

        public static bool operator <(OFloat a, OFloat b)
        {
            if ((object)a == null || (object)b == null || a.Value == null || b.Value == null) return false;
            else return a.Value < b.Value;
        }

        public static bool operator <=(OFloat a, OFloat b)
        {
            if ((object)a == null || (object)b == null || a.Value == null || b.Value == null) return false;
            else return a.Value <= b.Value;
        }

        public static bool operator ==(OFloat a, OFloat b)
        {
            if ((object)a == null || (object)b == null || a.Value == null || b.Value == null)
            {
                if ((object)a == null && (object)b == null) return true;
                if ((object)a == null ^ (object)b == null) return false;
                if (a.Value == null && b.Value == null) return true;
                return false;
            }
            else return a.Value == b.Value;
        }

        public static bool operator !=(OFloat a, OFloat b)
        {
            return !(a == b);
        }

        #endregion

        #region CompareTo
        public int CompareTo(object obj)
        {
            if (obj is OFloat)
            {
                var target = obj as OFloat;
                if (this == target) return 0;
                if (this < target) return -1;
                else return 1;
            }

            return -1;
        }
        #endregion

        #region TryParse

        public static OFloat TryParse(string value, int? id = null)
        {
            if (string.IsNullOrEmpty(value)) return null;

            double oVal;
            var fInfo = FormatInfo(id ?? 0);

            if (double.TryParse(value, NumberStyles.Number, fInfo, out oVal)) return new OFloat(oVal);
            else return null;
        }

        #endregion
    }

    #endregion

    #region OInt
    /// <summary>wrapper class for handling interger format in multi-language</summary>
    [ModModelBinder]
    public class OInt : IFormattable
    {
        public int? Value { get; set; }

        #region OInt

        public OInt(int value)
        {
            Value = value;
        }

        #endregion

        #region FormatInfo

        public static NumberFormatInfo FormatInfo(int? id = null)
        {
            var lang = LanguageMgr.OptLang(id ?? (QParam.GetId() ?? 0));
            var finfo = new NumberFormatInfo()
            {
                NumberGroupSeparator = lang.ThousandsSeperator,
                NumberDecimalSeparator = lang.DecimalSeperator
            };

            return finfo;
        }

        #endregion

        #region Text

        public string Text
        {
            get { return Value == null ? string.Empty : ToString(); }
        }

        #endregion

        #region ToString

        public string ToString(string format, IFormatProvider formatProvider)
        {
            return ToString();
        }

        public override string ToString()
        {
            return Value == null ? "" : Value.Value.ToString(FormatInfo());
        }

        #endregion

        #region Equals

        public override bool Equals(object obj)
        {
            if (Value == null) return obj == null;
            else return Value.Equals(obj);
        }

        #endregion

        #region GetHashCode

        public override int GetHashCode()
        {
            return Value == null ? 0 : Value.GetHashCode();
        }

        #endregion

        #region implicit operator

        public static implicit operator int?(OInt obj)
        {
            return obj == null ? (int?)null : obj.Value;
        }

        public static implicit operator int(OInt obj)
        {
            return obj == null ? 0 : obj.Value ?? 0;
        }

        public static implicit operator OInt(int? value)
        {
            return value == null ? null : new OInt(value.Value);
        }

        #endregion

        #region compare operator

        public static bool operator >(OInt a, OInt b)
        {
            if ((object)a == null || (object)b == null || a.Value == null || b.Value == null) return false;
            else return a.Value > b.Value;
        }

        public static bool operator >=(OInt a, OInt b)
        {
            if ((object)a == null || (object)b == null || a.Value == null || b.Value == null) return false;
            else return a.Value >= b.Value;
        }

        public static bool operator <(OInt a, OInt b)
        {
            if ((object)a == null || (object)b == null || a.Value == null || b.Value == null) return false;
            else return a.Value < b.Value;
        }

        public static bool operator <=(OInt a, OInt b)
        {
            if ((object)a == null || (object)b == null || a.Value == null || b.Value == null) return false;
            else return a.Value <= b.Value;
        }

        public static bool operator ==(OInt a, OInt b)
        {
            if ((object)a == null || (object)b == null || a.Value == null || b.Value == null)
            {
                if ((object)a == null && (object)b == null) return true;
                if ((object)a == null ^ (object)b == null) return false;
                if (a.Value == null && b.Value == null) return true;
                return false;
            }
            else return a.Value == b.Value;
        }

        public static bool operator !=(OInt a, OInt b)
        {
            return !(a == b);
        }

        #endregion

    }

    #endregion

    #region OMoney
    /// <summary>wrapper class for handling decimal format in multi-language</summary>
    [TypeConverter(typeof(OMoney.Converter))]
    [ModModelBinder]
    public class OMoney : IFormattable, IComparable
    {
        #region JsonConverter
        public class JsonConverter : JsonConverter<OMoney>
        {
            public override void WriteJson(JsonWriter writer, OMoney value, JsonSerializer serializer)
            {
                writer.WriteValue(value == null || value.Value == null ? "" : string.Format("{0}@{1}", value.Text, ((double)value.Value.Value).ToString(CultureInfo.InvariantCulture)));
            }

            public override OMoney ReadJson(JsonReader reader, Type objectType, OMoney existingValue, bool hasExistingValue, JsonSerializer serializer)
            {
                var s = (string)reader.Value;

                if (s.IsNullOrEmpty()) return new OMoney();

                var idx = s.IndexOf('@');

                if (idx == -1) return TryParse(s);

                return new OMoney(decimal.Parse(s.Substring(idx + 1), CultureInfo.InvariantCulture));
            }
        } 
        #endregion
        #region Converter
        /// <summary>convert from string to OMoney</summary>
        public class Converter : TypeConverter
        {
            public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
            {
                if (sourceType == typeof(string)) return true;
                return base.CanConvertFrom(context, sourceType);
            }

            public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
            {
                if (value is string)
                {
                    decimal val;
                    if (decimal.TryParse((string)value, NumberStyles.Number, FormatInfo(), out val))
                        return new OMoney(val);
                }
                return base.ConvertFrom(context, culture, value);
            }

            public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
            {
                var cxt = HttpContext.Current;

                //if (cxt != null && cxt.Request.RawUrl.StartsWith("/api/cpos/")) return false;
                
                return base.CanConvertTo(context, destinationType);
            }

            public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
            {
                if (destinationType == typeof(string))
                    return ((OMoney)value).ToString();

                return base.ConvertTo(context, culture, value, destinationType);
            }
        }

        #endregion

        public decimal? Value { get; set; }

        [ScriptIgnore(ApplyToOverrides = true)]
        public bool ForceFullMode = false;

        #region OMoney

        public OMoney(decimal value)
        {
            Value = value;
        }

        public OMoney(decimal? value = null)
        {
            Value = value;
        }

        public OMoney(float value)
        {
            Value = (decimal)value;
        }

        public OMoney(int value)
        {
            Value = (decimal)value;
        }

        #endregion

        #region FormatInfo

        public static NumberFormatInfo FormatInfo(int? id = null)
        {
            var lang = LanguageMgr.OptLang(id ?? (QParam.GetId() ?? 0));
            var finfo = new NumberFormatInfo()
            {
                NumberGroupSeparator = lang.ThousandsSeperator,
                NumberDecimalSeparator = lang.DecimalSeperator
            };

            return finfo;
        }

        #endregion

        #region Text

        public string Text
        {
            get { return Value == null ? string.Empty : ToString(); }
        }

        #endregion

        #region ToString

        public string ToString(string format, IFormatProvider formatProvider)
        {
            return ToString();
        }

        public override string ToString()
        {
            var format = ForceFullMode ? "n" : SettingsBiz.GetSettings().Display.Others.GetPriceFormat();
            return Value == null ? "" : Value.Value.ToString(format, FormatInfo());
        }

        #endregion

        #region Equals

        public override bool Equals(object obj)
        {
            if (Value == null) return obj == null;
            else return Value.Equals(obj);
        }

        #endregion

        #region GetHashCode

        public override int GetHashCode()
        {
            return Value == null ? 0 : Value.GetHashCode();
        }

        #endregion

        #region implicit operator

        public static implicit operator decimal?(OMoney obj)
        {
            return obj == null ? null : obj.Value;
        }

        public static implicit operator decimal(OMoney obj)
        {
            return obj == null ? 0 : obj.Value ?? 0;
        }

        public static implicit operator OMoney(decimal? value)
        {
            return value == null ? null : new OMoney(value.Value);
        }

        public static implicit operator OMoney(float? value)
        {
            return value == null ? null : new OMoney((decimal)value.Value);
        }

        public static implicit operator OMoney(int? value)
        {
            return value == null ? null : new OMoney(value.Value);
        }

        #endregion

        #region compare operator

        public static bool operator >(OMoney a, OMoney b)
        {
            if ((object)a == null || (object)b == null || a.Value == null || b.Value == null) return false;
            else return a.Value > b.Value;
        }

        public static bool operator >=(OMoney a, OMoney b)
        {
            if ((object)a == null || (object)b == null || a.Value == null || b.Value == null) return false;
            else return a.Value >= b.Value;
        }

        public static bool operator <(OMoney a, OMoney b)
        {
            if ((object)a == null || (object)b == null || a.Value == null || b.Value == null) return false;
            else return a.Value < b.Value;
        }

        public static bool operator <=(OMoney a, OMoney b)
        {
            if ((object)a == null || (object)b == null || a.Value == null || b.Value == null) return false;
            else return a.Value <= b.Value;
        }

        public static bool operator ==(OMoney a, OMoney b)
        {
            if ((object)a == null || (object)b == null || a.Value == null || b.Value == null)
            {
                if ((object)a == null && (object)b == null) return true;
                if ((object)a == null ^ (object)b == null) return false;
                if (a.Value == null && b.Value == null) return true;
                return false;
            }
            else return a.Value == b.Value;
        }

        public static bool operator !=(OMoney a, OMoney b)
        {
            return !(a == b);
        }

        #endregion

        #region CompareTo
        public int CompareTo(object obj)
        {
            if (obj is OMoney)
            {
                var target = obj as OMoney;
                if (this == target) return 0;
                if (this < target) return -1;
                else return 1;
            }

            return -1;
        }
        #endregion

        #region TryParse

        public static OMoney TryParse(string value, int? id = null)
        {
            if (string.IsNullOrEmpty(value)) return null;

            decimal oVal;
            var fInfo = FormatInfo(id ?? 0);

            if (decimal.TryParse(value, NumberStyles.Number, fInfo, out oVal)) return new OMoney(oVal);
            else return null;
        }

        #endregion
    }

    #endregion

    #region OMoneyMc
    /// <summary>wrapper class for handling decimal format in multi-language in FE</summary>
    [TypeConverter(typeof(OMoneyMc.Converter))]
    [ModModelBinder]
    public class OMoneyMc : IFormattable, IComparable
    {
        #region Setting

        public class Setting
        {
            public Domain.Currency Currency { get; set; }
            public bool ShowUnit { get; set; }
            public bool ShowOriginal { get; set; }
            public bool ShowInCart { get; set; }
            public bool Roundable { get; set; }
            public bool SkipRate { get; set; }

            public bool EnableRounding
            {
                get { return Roundable && SettingsBiz.GetSettings().Display.Product.PriceRound; }
            }

            public Setting(Currency currency)
            {
                Currency = new Currency(currency ?? CurrencyMgr.FocusCurrency);
                ShowUnit = true;
                ShowOriginal = false;
                ShowInCart = false;
                Roundable = false;
                SkipRate = false;
            }

            public Setting() : this(null) { }
        }

        #endregion
        #region JsonConverter
        public class JsonConverter : JsonConverter<OMoneyMc>
        {
            public override void WriteJson(JsonWriter writer, OMoneyMc value, JsonSerializer serializer)
            {
                writer.WriteValue(value == null || value.Value == null ? "" : string.Format("{0}@{1}", value.Text, ((double)value.Value.Value).ToString(CultureInfo.InvariantCulture)));
            }

            public override OMoneyMc ReadJson(JsonReader reader, Type objectType, OMoneyMc existingValue, bool hasExistingValue, JsonSerializer serializer)
            {
                var s = (string)reader.Value;

                if (s.IsNullOrEmpty()) return new OMoneyMc();

                var idx = s.IndexOf('@');

                if (idx == -1) return TryParse(s);

                return new OMoneyMc(decimal.Parse(s.Substring(idx + 1), CultureInfo.InvariantCulture));
            }
        }
        #endregion
        #region Converter
        /// <summary>convert from string to OMoneyFe</summary>
        public class Converter : TypeConverter
        {
            public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
            {
                if (sourceType == typeof(string)) return true;
                return base.CanConvertFrom(context, sourceType);
            }

            public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
            {
                if (value is string)
                {
                    decimal val;
                    if (decimal.TryParse((string)value, NumberStyles.Number, FormatInfo(), out val))
                        return new OMoneyMc(val);
                }
                return base.ConvertFrom(context, culture, value);
            }

            public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
            {
                var cxt = HttpContext.Current;

                //if (cxt != null && cxt.Request.RawUrl.StartsWith("/api/cpos/")) return false;

                return base.CanConvertTo(context, destinationType);
            }

            public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
            {
                if (destinationType == typeof(string))
                    return ((OMoneyMc)value).ToString(true);

                return base.ConvertTo(context, culture, value, destinationType);
            }
        }

        #endregion

        private readonly Setting _setting;

        public decimal? Origin { get; set; }

        #region Value

        public decimal? Value
        {
            get
            {
                if (Origin == null) return null;

                var price = Origin.Value * (_setting.SkipRate ? 1 : _setting.Currency.Rate);

                if (_setting.EnableRounding)
                    price = Math.Ceiling(Math.Round(price, _setting.Currency.DecimalPlaces));

                return ((decimal?)price).NormalizePriceInc(0, true);
            }
        }

        #endregion

        #region OMoneyMc

        public OMoneyMc()
        {
            _setting = new Setting();
        }

        //public OMoneyMc(string value)
        //{
        //    _setting = new Setting();

        //    if (!string.IsNullOrEmpty(value))
        //    {
        //        decimal tmpValue = 0;
        //        decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out tmpValue);
        //        Origin = tmpValue;
        //    }
        //}

        public OMoneyMc(decimal? value = null, Setting setting = null)
        {
            Origin = value;
            _setting = setting ?? new Setting();
        }

        public OMoneyMc(decimal value, Setting setting = null)
            : this((decimal?)value, setting)
        {
        }

        public OMoneyMc(float value, Setting setting = null)
            : this((decimal?)value, setting)
        {
        }

        public OMoneyMc(int value, Setting setting = null)
            : this((decimal?)value, setting)
        {
        }

        #endregion

        #region FormatInfo

        public static NumberFormatInfo FormatInfo(Currency currency = null)
        {
            currency = currency ?? CurrencyMgr.FocusCurrency;

            var finfo = new NumberFormatInfo()
            {
                NumberGroupSeparator = currency.ThousandSep,
                NumberDecimalSeparator = currency.DecimalSep,
                NumberDecimalDigits = currency.DecimalPlaces,
            };

            return finfo;
        }

        #endregion

        #region Text

        public string Text
        {
            get { return Value == null ? string.Empty : ToString(); }
        }

        #endregion

        #region ToString

        public string ToString(string format, IFormatProvider formatProvider)
        {
            return ToString();
        }

        public override string ToString()
        {
            return ToString(false);
        }

        public string ToString(bool noUnit)
        {
            if (Value == null) return string.Empty;

            if (noUnit) return Value.Value.ToString("N", FormatInfo(_setting.Currency));

            if (!_setting.EnableRounding || _setting.ShowInCart || _setting.ShowOriginal)
                return string.Format(_setting.Currency.Location ? "{1} {0}" : "{0} {1}",
                    Value.Value.ToString("N", FormatInfo(_setting.Currency)),
                    _setting.ShowUnit ? _setting.Currency.Token : "").Trim();
            else
            {
                var finfo = FormatInfo(_setting.Currency);
                finfo.NumberDecimalDigits = 0;
                return string.Format(_setting.Currency.Location ? "{1} {0}{2}-" : "{0}{2}- {1}",
                    Value.Value.ToString("N", finfo),
                    _setting.ShowUnit ? _setting.Currency.Token : "", _setting.Currency.DecimalSep);
            }

            //var cval = (Value == null ? (decimal?)null : (Value.Value * _setting.Currency.Rate));
            //return cval == null ? "" : string.Format(_setting.Currency.Location ? "{1} {0}" : "{0} {1}",
            //        cval.Value.ToString("n", FormatInfo(_setting.Currency)), _setting.Currency.Token);
        }

        #endregion

        #region Equals

        public override bool Equals(object obj)
        {
            if (Value == null) return obj == null;
            else return Value.Equals(obj);
        }

        #endregion

        #region GetHashCode

        public override int GetHashCode()
        {
            return Value == null ? 0 : Value.GetHashCode();
        }

        #endregion

        #region implicit operator

        public static implicit operator decimal?(OMoneyMc obj)
        {
            return obj == null ? null : obj.Value;
        }

        public static implicit operator decimal(OMoneyMc obj)
        {
            return obj == null ? 0 : obj.Value ?? 0;
        }

        public static implicit operator OMoneyMc(decimal? value)
        {
            return value == null ? null : new OMoneyMc(value.Value);
        }

        public static implicit operator OMoneyMc(float? value)
        {
            return value == null ? null : new OMoneyMc((decimal)value.Value);
        }

        public static implicit operator OMoneyMc(int? value)
        {
            return value == null ? null : new OMoneyMc(value.Value);
        }

        public static implicit operator OMoney(OMoneyMc value)
        {
            return value == null ? null : new OMoney(value.Value);
        }

        #endregion

        #region compare operator

        public static bool operator >(OMoneyMc a, OMoneyMc b)
        {
            if ((object)a == null || (object)b == null || a.Value == null || b.Value == null) return false;
            else return a.Value > b.Value;
        }

        public static bool operator >=(OMoneyMc a, OMoneyMc b)
        {
            if ((object)a == null || (object)b == null || a.Value == null || b.Value == null) return false;
            else return a.Value >= b.Value;
        }

        public static bool operator <(OMoneyMc a, OMoneyMc b)
        {
            if ((object)a == null || (object)b == null || a.Value == null || b.Value == null) return false;
            else return a.Value < b.Value;
        }

        public static bool operator <=(OMoneyMc a, OMoneyMc b)
        {
            if ((object)a == null || (object)b == null || a.Value == null || b.Value == null) return false;
            else return a.Value <= b.Value;
        }

        public static bool operator ==(OMoneyMc a, OMoneyMc b)
        {
            if ((object)a == null || (object)b == null || a.Value == null || b.Value == null)
            {
                if ((object)a == null && (object)b == null) return true;
                if ((object)a == null ^ (object)b == null) return false;
                if (a.Value == null && b.Value == null) return true;
                return false;
            }
            else return a.Value == b.Value;
        }

        public static bool operator !=(OMoneyMc a, OMoneyMc b)
        {
            return !(a == b);
        }

        #endregion

        #region CompareTo
        public int CompareTo(object obj)
        {
            if (obj is OMoneyMc)
            {
                var target = obj as OMoneyMc;
                if (this == target) return 0;
                if (this < target) return -1;
                else return 1;
            }

            return -1;
        }
        #endregion

        #region TryParse

        public static OMoneyMc TryParse(string value)
        {
            if (string.IsNullOrEmpty(value)) return null;

            var currency = CurrencyMgr.FocusCurrency;
            var fInfo = FormatInfo(currency);
            decimal oVal;

            if (!currency.Token.IsNullOrEmpty())
            {
                if (currency.Location &&
                    value.StartsWith(currency.Token, StringComparison.OrdinalIgnoreCase))
                    value = value.Substring(currency.Token.Length).Trim();

                if (!currency.Location &&
                    value.EndsWith(currency.Token, StringComparison.OrdinalIgnoreCase))
                    value = value.Substring(0, value.Length - currency.Token.Length).Trim();

                if (string.IsNullOrEmpty(value)) return null;
            }

            if (value.EndsWith("-") && value.EndsWith(currency.DecimalSep + "-"))
            {
                value = value.Substring(0, value.Length - 2).Trim();
            }

            if (decimal.TryParse(value, NumberStyles.Number, fInfo, out oVal)) return new OMoneyMc(oVal);
            else return null;
        }

        #endregion
    }
    #endregion
}