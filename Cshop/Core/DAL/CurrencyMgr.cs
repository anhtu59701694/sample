using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using AutoMapper;
using Claw.Controls.Modules;
using Claw.Core;
using Claw.Modules.Eshop.Helper;
using NHibernate;
using System.Web.UI.WebControls;
using Claw.Core.DAL;
using Claw.Modules.Eshop.Controllers.Admin;
using Claw.Modules.Eshop.Domain;
using Claw.Modules.Eshop.Models;

namespace Claw.Modules.Eshop.DAL
{
    public class CurrencyMgr : BaseMgr<CurrencyModel, Currency>
    {
        #region Default

        public static Currency Default
        {
            get
            {
                return Helper.DataCache<CurrencyScope, Currency>.Get("CurrencyMgr.Default", () =>
                {
                    var settings = SettingsBiz.GetSettings();
                    var fmt = OMoney.FormatInfo();
                    return new Currency()
                    {
                        Code = settings.General.Currency.IsNullOrEmpty() ? "NOK" : settings.General.Currency,
                        Token = "",
                        Name = "Default",
                        Rate = 1.0m,
                        Active = true,
                        DecimalPlaces = fmt.NumberDecimalDigits,
                        DecimalSep = fmt.NumberDecimalSeparator,
                        ThousandSep = fmt.NumberGroupSeparator,
                    };
                }, 15*60);
            }
        }

        #endregion

        public static readonly Dictionary<string, string> Items = new Dictionary<string, string>();

        #region CurrencyMgr
        static CurrencyMgr()
        {
            Init(typeof(CurrencyMgr));

            #region Items
            var items = new[]
            {
                new {Code = "AED", Name = "United Arab Emirates Dirham"},
                new {Code = "AFN", Name = "Afghanistan Afghani"}, new {Code = "ALL", Name = "Albania Lek"},
                new {Code = "AMD", Name = "Armenia Dram"}, new {Code = "ANG", Name = "Netherlands Antilles Guilder"},
                new {Code = "AOA", Name = "Angola Kwanza"}, new {Code = "ARS", Name = "Argentina Peso"},
                new {Code = "AUD", Name = "Australia Dollar"}, new {Code = "AWG", Name = "Aruba Guilder"},
                new {Code = "AZN", Name = "Azerbaijan New Manat"},
                new {Code = "BAM", Name = "Bosnia and Herzegovina Convertible Marka"},
                new {Code = "BBD", Name = "Barbados Dollar"}, new {Code = "BDT", Name = "Bangladesh Taka"},
                new {Code = "BGN", Name = "Bulgaria Lev"}, new {Code = "BHD", Name = "Bahrain Dinar"},
                new {Code = "BIF", Name = "Burundi Franc"}, new {Code = "BMD", Name = "Bermuda Dollar"},
                new {Code = "BND", Name = "Brunei Darussalam Dollar"}, new {Code = "BOB", Name = "Bolivia Bolíviano"},
                new {Code = "BRL", Name = "Brazil Real"}, new {Code = "BSD", Name = "Bahamas Dollar"},
                new {Code = "BTN", Name = "Bhutan Ngultrum"}, new {Code = "BWP", Name = "Botswana Pula"},
                new {Code = "BYR", Name = "Belarus Ruble"}, new {Code = "BZD", Name = "Belize Dollar"},
                new {Code = "CAD", Name = "Canada Dollar"}, new {Code = "CDF", Name = "Congo/Kinshasa Franc"},
                new {Code = "CHF", Name = "Switzerland Franc"}, new {Code = "CLP", Name = "Chile Peso"},
                new {Code = "CNY", Name = "China Yuan Renminbi"}, new {Code = "COP", Name = "Colombia Peso"},
                new {Code = "CRC", Name = "Costa Rica Colon"}, new {Code = "CUC", Name = "Cuba Convertible Peso"},
                new {Code = "CUP", Name = "Cuba Peso"}, new {Code = "CVE", Name = "Cape Verde Escudo"},
                new {Code = "CZK", Name = "Czech Republic Koruna"}, new {Code = "DJF", Name = "Djibouti Franc"},
                new {Code = "DKK", Name = "Denmark Krone"}, new {Code = "DOP", Name = "Dominican Republic Peso"},
                new {Code = "DZD", Name = "Algeria Dinar"}, new {Code = "EGP", Name = "Egypt Pound"},
                new {Code = "ERN", Name = "Eritrea Nakfa"}, new {Code = "ETB", Name = "Ethiopia Birr"},
                new {Code = "EUR", Name = "Euro Member Countries"}, new {Code = "FJD", Name = "Fiji Dollar"},
                new {Code = "FKP", Name = "Falkland Islands (Malvinas) Pound"},
                new {Code = "GBP", Name = "United Kingdom Pound"}, new {Code = "GEL", Name = "Georgia Lari"},
                new {Code = "GGP", Name = "Guernsey Pound"}, new {Code = "GHS", Name = "Ghana Cedi"},
                new {Code = "GIP", Name = "Gibraltar Pound"}, new {Code = "GMD", Name = "Gambia Dalasi"},
                new {Code = "GNF", Name = "Guinea Franc"}, new {Code = "GTQ", Name = "Guatemala Quetzal"},
                new {Code = "GYD", Name = "Guyana Dollar"}, new {Code = "HKD", Name = "Hong Kong Dollar"},
                new {Code = "HNL", Name = "Honduras Lempira"}, new {Code = "HRK", Name = "Croatia Kuna"},
                new {Code = "HTG", Name = "Haiti Gourde"}, new {Code = "HUF", Name = "Hungary Forint"},
                new {Code = "IDR", Name = "Indonesia Rupiah"}, new {Code = "ILS", Name = "Israel Shekel"},
                new {Code = "IMP", Name = "Isle of Man Pound"}, new {Code = "INR", Name = "India Rupee"},
                new {Code = "IQD", Name = "Iraq Dinar"}, new {Code = "IRR", Name = "Iran Rial"},
                new {Code = "ISK", Name = "Iceland Krona"}, new {Code = "JEP", Name = "Jersey Pound"},
                new {Code = "JMD", Name = "Jamaica Dollar"}, new {Code = "JOD", Name = "Jordan Dinar"},
                new {Code = "JPY", Name = "Japan Yen"}, new {Code = "KES", Name = "Kenya Shilling"},
                new {Code = "KGS", Name = "Kyrgyzstan Som"}, new {Code = "KHR", Name = "Cambodia Riel"},
                new {Code = "KMF", Name = "Comoros Franc"}, new {Code = "KPW", Name = "Korea (North) Won"},
                new {Code = "KRW", Name = "Korea (South) Won"}, new {Code = "KWD", Name = "Kuwait Dinar"},
                new {Code = "KYD", Name = "Cayman Islands Dollar"}, new {Code = "KZT", Name = "Kazakhstan Tenge"},
                new {Code = "LAK", Name = "Laos Kip"}, new {Code = "LBP", Name = "Lebanon Pound"},
                new {Code = "LKR", Name = "Sri Lanka Rupee"}, new {Code = "LRD", Name = "Liberia Dollar"},
                new {Code = "LSL", Name = "Lesotho Loti"}, new {Code = "LYD", Name = "Libya Dinar"},
                new {Code = "MAD", Name = "Morocco Dirham"}, new {Code = "MDL", Name = "Moldova Leu"},
                new {Code = "MGA", Name = "Madagascar Ariary"}, new {Code = "MKD", Name = "Macedonia Denar"},
                new {Code = "MMK", Name = "Myanmar (Burma) Kyat"}, new {Code = "MNT", Name = "Mongolia Tughrik"},
                new {Code = "MOP", Name = "Macau Pataca"}, new {Code = "MRO", Name = "Mauritania Ouguiya"},
                new {Code = "MUR", Name = "Mauritius Rupee"},
                new {Code = "MVR", Name = "Maldives (Maldive Islands) Rufiyaa"},
                new {Code = "MWK", Name = "Malawi Kwacha"}, new {Code = "MXN", Name = "Mexico Peso"},
                new {Code = "MYR", Name = "Malaysia Ringgit"}, new {Code = "MZN", Name = "Mozambique Metical"},
                new {Code = "NAD", Name = "Namibia Dollar"}, new {Code = "NGN", Name = "Nigeria Naira"},
                new {Code = "NIO", Name = "Nicaragua Cordoba"}, new {Code = "NOK", Name = "Norway Krone"},
                new {Code = "NPR", Name = "Nepal Rupee"}, new {Code = "NZD", Name = "New Zealand Dollar"},
                new {Code = "OMR", Name = "Oman Rial"}, new {Code = "PAB", Name = "Panama Balboa"},
                new {Code = "PEN", Name = "Peru Sol"}, new {Code = "PGK", Name = "Papua New Guinea Kina"},
                new {Code = "PHP", Name = "Philippines Peso"}, new {Code = "PKR", Name = "Pakistan Rupee"},
                new {Code = "PLN", Name = "Poland Zloty"}, new {Code = "PYG", Name = "Paraguay Guarani"},
                new {Code = "QAR", Name = "Qatar Riyal"}, new {Code = "RON", Name = "Romania New Leu"},
                new {Code = "RSD", Name = "Serbia Dinar"}, new {Code = "RUB", Name = "Russia Ruble"},
                new {Code = "RWF", Name = "Rwanda Franc"}, new {Code = "SAR", Name = "Saudi Arabia Riyal"},
                new {Code = "SBD", Name = "Solomon Islands Dollar"}, new {Code = "SCR", Name = "Seychelles Rupee"},
                new {Code = "SDG", Name = "Sudan Pound"}, new {Code = "SEK", Name = "Sweden Krona"},
                new {Code = "SGD", Name = "Singapore Dollar"}, new {Code = "SHP", Name = "Saint Helena Pound"},
                new {Code = "SLL", Name = "Sierra Leone Leone"}, new {Code = "SOS", Name = "Somalia Shilling"},
                new {Code = "SPL", Name = "Seborga Luigino"}, new {Code = "SRD", Name = "Suriname Dollar"},
                new {Code = "STD", Name = "São Tomé and Príncipe Dobra"}, new {Code = "SVC", Name = "El Salvador Colon"},
                new {Code = "SYP", Name = "Syria Pound"}, new {Code = "SZL", Name = "Swaziland Lilangeni"},
                new {Code = "THB", Name = "Thailand Baht"}, new {Code = "TJS", Name = "Tajikistan Somoni"},
                new {Code = "TMT", Name = "Turkmenistan Manat"}, new {Code = "TND", Name = "Tunisia Dinar"},
                new {Code = "TOP", Name = "Tonga Pa'anga"}, new {Code = "TRY", Name = "Turkey Lira"},
                new {Code = "TTD", Name = "Trinidad and Tobago Dollar"}, new {Code = "TVD", Name = "Tuvalu Dollar"},
                new {Code = "TWD", Name = "Taiwan New Dollar"}, new {Code = "TZS", Name = "Tanzania Shilling"},
                new {Code = "UAH", Name = "Ukraine Hryvnia"}, new {Code = "UGX", Name = "Uganda Shilling"},
                new {Code = "USD", Name = "United States Dollar"}, new {Code = "UYU", Name = "Uruguay Peso"},
                new {Code = "UZS", Name = "Uzbekistan Som"}, new {Code = "VEF", Name = "Venezuela Bolivar"},
                new {Code = "VND", Name = "Viet Nam Dong"}, new {Code = "VUV", Name = "Vanuatu Vatu"},
                new {Code = "WST", Name = "Samoa Tala"},
                new {Code = "XAF", Name = "Communauté Financière Africaine (BEAC) CFA Franc"},
                new {Code = "XCD", Name = "East Caribbean Dollar"},
                new {Code = "XDR", Name = "International Monetary Fund (IMF) Special Drawing Rights"},
                new {Code = "XOF", Name = "Communauté Financière Africaine (BCEAO) Franc"},
                new {Code = "XPF", Name = "Comptoirs Français du Pacifique (CFP) Franc"},
                new {Code = "YER", Name = "Yemen Rial"}, new {Code = "ZAR", Name = "South Africa Rand"},
                new {Code = "ZMW", Name = "Zambia Kwacha"},
                new {Code = "ZWD", Name = "Zimbabwe Dollar"}
            };
            #endregion

            var codeDic = Enum.GetNames(typeof(CurrencyCodeType)).ToDictionary(x => x.Trim().ToUpper(), x => x);

            Items = items.Where(x => codeDic.ContainsKey(x.Code.Trim().ToUpper())).ToDictionary(x => x.Code.Trim().ToUpper(), x => x.Name);
        } 
        #endregion

        #region GetAll

        public static List<CurrencyModel> GetAll(IDB db, bool? active, int pageIndex, int pageSize, out int totalRows,
            SortDirection sortDirection = SortDirection.Descending, string sortField = "Id")
        {
            var hSql = string.Format(@"
            select curr from {0} as curr ", typeof(Currency).FullName);

            if (active != null) hSql += " where curr.Active = " + (active.Value ? 1 : 0);

            hSql += string.Format(" order by {0} {1}", "curr." + sortField, GetDir(sortDirection));

            var ret = GetList<Currency>(db, hSql, pageIndex, pageSize, out totalRows);

            return Mapper.Map<List<Currency>, List<CurrencyModel>>(ret);
        }

        static System.Tuple<List<CurrencyModel>, Dictionary<string, Currency>> GetAllRaw(IDB db = null, bool? active = true, bool refeshCache = false)
        {
            return Helper.DataCache<CurrencyScope, System.Tuple<List<CurrencyModel>, Dictionary<string,Currency>>>.Get(
                string.Format("CurrencyMgr.GetAll:{0}", active), () =>
                {
                    int total;
                    var lst = GetAll(db ?? new DefaultDb(), active, 1, Int32.MaxValue, out total, SortDirection.Ascending, "DisplayOrder");
                    return new System.Tuple<List<CurrencyModel>, Dictionary<string, Currency>>(lst, lst.ToDictionary(x => x.Code.ToLower(), x => x.GetRaw()));
                }, 15 * 60, refresh: refeshCache);
        }

        public static List<CurrencyModel> GetAll(IDB db = null, bool? active = true, bool requireDef = false)
        {
            var ret = GetAllRaw(db, active).Item1;

            if (requireDef)
            {
                var settings = SettingsBiz.GetSettings();
                if (settings.Hidden.SingleCurrency)
                {
                    var defVal = Default;
                    if (ret.All(x => !x.Code.EqualsIgCase(defVal.Code)))
                        ret.Add(Mapper.Map<Currency, CurrencyModel>(defVal));
                }
            }

            return ret;
        }
        
        #endregion

        #region Get

        public static Currency Get(string code)
        {
            return Get(code, null);
        }

        public static Currency Get(string code, ISession session)
        {
            if (code.IsNullOrEmpty()) return null;

            var refeshCache = false;

            LB_LOOP:
            Currency oVal;
            GetAllRaw(refeshCache: refeshCache, db: session == null ? null : new DefaultDb(session)).Item2.TryGetValue(code.ToLower(), out oVal);

            if (oVal != null && oVal.Id == 0 && !refeshCache)
            {
                refeshCache = true;
                goto LB_LOOP;
            }

            if (oVal == null && code.EqualsIgCase(Default.Code))
                return Default;

            return oVal;
        }

        #endregion

        #region GetOrDefault
        public static Currency GetOrDefault(string code)
        {
            return Get(code) ?? Default;
        }
        #endregion

        #region FocusCurrencyCode

        public static string FocusCurrencyCode
        {
            get
            {
                var curCxt = HttpContext.Current;
                if (curCxt != null)
                {
                    var settings = SettingsBiz.GetSettings();
                    var code = settings.Hidden.SingleCurrency
                        ? Default.Code
                        : curCxt.Request.QueryString[CurrencyModel.CurCode];

                    if (string.IsNullOrEmpty(code))
                    {
                        var cookie = curCxt.Request.Cookies[CurrencyModel.CurrencyCode];
                        if (cookie != null) code = cookie.Value;
                    }

                    if (string.IsNullOrEmpty(code))
                        code = (curCxt.GetRouteValue(CurrencyModel.CurrencyCode) ?? "").ToString();

                    if (!string.IsNullOrEmpty(code) && Get(code) != null)
                        return code.Trim().ToLower();
                }

                return Default.Code.ToLower();
            }
        }

        #endregion

        #region FocusCurrency

        public static Currency FocusCurrency
        {
            get
            {
                var cxt = HttpContext.Current;
                var key = "CurrencyMgr.FocusCurrency";
                if (cxt != null && cxt.Items.Contains(key)) return (Currency)cxt.Items[key];
                
                var ret = GetOrDefault(FocusCurrencyCode);
                if (cxt != null) cxt.Items[key] = ret;
                
                return ret;
            }
        }

        #endregion

        #region SaveorUpdate

        public static bool SaveorUpdate(IDB db, CurrencyModel model)
        {
            if (model == null) return false;

            CurrencyCodeType cVal;
            if (!Enum.TryParse(model.Code, true, out cVal)) return false;

            var obj = GetDom(db, model.Id) ?? new Currency();

            #region Currency

            obj.Name = model.Name;
            obj.Code = model.Code;
            obj.Rate = model.Rate;
            obj.Token = model.Token;
            obj.Location = model.Location;
            obj.DecimalSep = model.DecimalSep;
            obj.ThousandSep = model.ThousandSep.DecodeBase64();
            obj.DecimalPlaces = model.DecimalPlaces;
            obj.Active = model.Active;
            obj.DisplayOrder = model.DisplayOrder;

            #endregion

            if (obj.SaveorUpdate(db.DataService))
            {
                model.Id = obj.Id;
                new CurrencyMgr().OnUpdate(obj);

                var settings = SettingsBiz.GetSettings();

                if (model.Default && !settings.General.Currency.EqualsIgCase(obj.Code))
                {
                    settings = SettingsBiz.GetSettings(true);
                    settings.General.Currency = obj.Code;
                    SettingMgr.Save(settings);
                }
                else if (!model.Default && settings.General.Currency.EqualsIgCase(obj.Code))
                {
                    settings = SettingsBiz.GetSettings(true);
                    settings.General.Currency = "";
                    SettingMgr.Save(settings);
                }

                return true;
            }
            else return false;
        }

        #endregion

        #region Update

        public static bool Update(IDB db, int id, bool defVal)
        {
            var obj = GetDom(db, id);

            if (obj == null) return false;
            obj.Active = true;
            new CurrencyMgr().OnUpdate(obj);
            var settings = SettingsBiz.GetSettings();

            if (obj.IsDefault() && !defVal)
            {
                settings = SettingsBiz.GetSettings(true);
                settings.General.Currency = "";
                SettingMgr.Save(settings);
            }
            else if (!obj.IsDefault() && defVal)
            {
                settings = SettingsBiz.GetSettings(true);
                settings.General.Currency = obj.Code;
                SettingMgr.Save(settings);
            }

            return true;
        }

        #endregion

        #region UpdateRef
        public static void UpdateRef(ISession session, string code)
        {
            var obj = Get(code, session);

            if (obj == null || obj.AnyOrder == true || obj.Id == 0) return;

            obj.AnyOrder = true;

            session.Update(obj);

            new CurrencyMgr().OnUpdate(obj);
        } 
        #endregion

        #region OnUpdate
        protected override void OnUpdate(Currency obj)
        {
            CurrencyScope.Invalidate();
        }
        #endregion

        #region OnDelete
        protected override void OnDelete(Currency obj)
        {
            CurrencyScope.Invalidate();
        }
        #endregion

        #region CheckDefault

        public static bool CheckDefault(IDB db, int id)
        {
            var obj = GetDom(db, id);
            if (obj == null) return false;
            return obj.IsDefault();
        }

        #endregion
    }
}