using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Web.Routing;
using System.Web;
using Claw.Core;
using Claw.Core.DAL;
using Claw.Core.Domain;
using Claw.Core.Interfaces;
using Claw.Modules.Eshop.Helper;
using System.Web.Mvc;

namespace Claw.Modules.Eshop.DAL
{
    public class TaskSettingMgr : BaseMgr
    {
        #region Load

        public static int Load<T>(int taskid, T obj, IDB db = null)
            where T : class 
        {
            db = db ?? new DefaultDb();

            int total;
            var dicDb = db.DataService.GetList<Domain.TaskSetting>(0, 1, int.MaxValue, out total,
                new KeyValuePair() {Key = "Task.Id", Value = taskid})
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
                    var setting = (Domain.TaskSetting)dbVal;
                    var propVal = propType.IsEnum
                        ? Enum.Parse(propType, setting.Value)
                        : Convert.ChangeType(setting.Value, propData.Item2.PropertyType);
                    propData.Item2.SetValue(propData.Item1, propVal);
                }
            }

            Thread.CurrentThread.CurrentCulture = ogrinCulture;

            return dicDb.Count;
        }

        #endregion

        #region Save

        public static bool Save<T>(int taskid, T obj, IDB db = null)
            where T : class 
        {
            db = db ?? new DefaultDb();

            int total;
            var dicDb = db.DataService.GetList<Domain.TaskSetting>(0, 1, int.MaxValue, out total,
                new KeyValuePair() {Key = "Task.Id", Value = taskid})
                .ToRouteValueDictionary(m => m.Name, m => m);

            var dicVal = obj.ToDictionary(true);

            var ogrinCulture = Thread.CurrentThread.CurrentCulture;
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;

            foreach (var vEntry in dicVal)
            {
                object dbVal;
                if (dicDb.TryGetValue(vEntry.Key, out dbVal))
                    ((Domain.TaskSetting)dbVal).Value = (vEntry.Value ?? "").ToString();
                else
                {
                    dicDb.Add(vEntry.Key, dbVal = new Domain.TaskSetting()
                    {
                        Name = vEntry.Key,
                        Value = (vEntry.Value ?? "").ToString()
                    });

                    db.DataService.Save(dbVal);
                }
            }

            Thread.CurrentThread.CurrentCulture = ogrinCulture;

            return db.DataService.Save(dicDb.Values);
        }

        #endregion
    }
}