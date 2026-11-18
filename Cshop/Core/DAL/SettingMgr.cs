using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Web.Routing;
using System.Web;
using Claw.Core;
using Claw.Core.Domain;
using Claw.Core.Interfaces;
using Claw.Modules.Eshop.Helper;
using System.Web.Mvc;

namespace Claw.Modules.Eshop.DAL
{
    public class SettingMgr : BaseMgr
    {
        #region Load

        public static bool Load(SettingsBiz obj, IDB db = null)
        {
            var ds = (db != null ? db.DataService : null) ?? (ClawContext.Current != null ? ClawContext.Current.DataService : null);
            if (ds == null) return false;

            int total;
            var dicDb = ds.GetList<Domain.Setting>(0, 1, int.MaxValue, out total)
                .ToRouteValueDictionary(m => m.Name, m => m);

            var dicVal = obj.ToDictionary(true, false);

            var ogrinCulture = Thread.CurrentThread.CurrentCulture;
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;

            foreach (var vEntry in dicVal)
            {
                object dbVal;
                if (dicDb.TryGetValue(vEntry.Key, out dbVal))
                {
                    var propData = (Tuple<object, PropertyInfo>)vEntry.Value;
                    
                    if(!propData.Item2.CanWrite)continue;
                    
                    var propType = propData.Item2.PropertyType;
                    var setting = (Domain.Setting)dbVal;
                    var propVal = propType.IsEnum
                        ? Enum.Parse(propType, setting.Value)
                        : Convert.ChangeType(setting.Value, propData.Item2.PropertyType);
                    propData.Item2.SetValue(propData.Item1, propVal);
                }
            }

            Thread.CurrentThread.CurrentCulture = ogrinCulture;

            return true;
        }

        #endregion

        #region Save

        public static bool Save(SettingsBiz obj)
        {
            var ds = (ClawContext.Current != null ? ClawContext.Current.DataService : null);
            if (ds == null) return false;

            int total;
            var dicDb = ds.GetList<Domain.Setting>(0, 1, int.MaxValue, out total)
                .ToRouteValueDictionary(m => m.Name, m => m);

            var dicVal = obj.ToDictionary(true);

            var ogrinCulture = Thread.CurrentThread.CurrentCulture;
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;

            foreach (var vEntry in dicVal)
            {
                object dbVal;
                if (dicDb.TryGetValue(vEntry.Key, out dbVal))
                    ((Domain.Setting)dbVal).Value = (vEntry.Value ?? "").ToString();
                else
                {
                    dicDb.Add(vEntry.Key, dbVal = new Domain.Setting()
                    {
                        Name = vEntry.Key,
                        Value = (vEntry.Value ?? "").ToString()
                    });

                    ds.Save(dbVal);
                }
            }

            Thread.CurrentThread.CurrentCulture = ogrinCulture;

            if (ds.Save(dicDb.Values))
            {
                CurrencyScope.Invalidate();
                Helper.DefaultScope.Invalidate();
                return true;
            }
            else return false;
        }

        #endregion

        #region Get all country
        public static IEnumerable<SelectListItem> GetCountryList(string CurentLanguage)
        {
            if (string.IsNullOrEmpty(CurentLanguage)) CurentLanguage = "en-US";

            RegionInfo country = new RegionInfo(new CultureInfo(CurentLanguage, false).LCID);
            IList<SelectListItem> items = new List<SelectListItem>();

            //To get the Country Names from the CultureInfo installed in windows
            foreach (CultureInfo cul in CultureInfo.GetCultures(CultureTypes.SpecificCultures))
            {
                country = new RegionInfo(new CultureInfo(cul.Name, false).LCID);
                items.Add(new SelectListItem() { Text = country.DisplayName, Value = country.Name });
            }

            var result = (from firstEntry in items
                          select new
                          {
                              Text = firstEntry.Text,
                              Value = firstEntry.Value
                          }).OrderBy(x => x.Text).Distinct().Select(x => new SelectListItem
                          {
                              Text = x.Text,
                              Value = x.Value
                          });
            return result;

        }

        #endregion
    }
}