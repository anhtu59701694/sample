using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Threading;
using Claw.Modules.Eshop.Domain;
using Claw.Modules.Eshop.Models;
using NHibernate;

namespace Claw.Modules.Eshop.DAL
{
    public class DelayQueueMgr : BaseMgr
    {
        private static Timer _timer = new Timer(DoJob, null, 1000, 1000);
        private static volatile bool _dirty = true, _running = false;
        private static DateTime _lastRun = DateTime.Now;
        public static volatile bool Enable = true;

        #region DoJob

        public static void DoJob(object data)
        {
            if (!Enable) return;

            if (_running || (!_dirty && (DateTime.Now - _lastRun).TotalMinutes < 1)) return;

            _running = true;

            try
            {
                IDB db = new SeparatedDb();
                var settings = Helper.SettingsBiz.GetSettings(false, db);
                var lst = settings.Hidden.PowerOffice ? db.DataService.GetAll<DelayQueue>() : new List<DelayQueue>();

                foreach (var itm in lst)
                {
                    try
                    {
                        var func = GetExecMethod(itm.Type);

                        func.Invoke(null, new object[] {db, itm});

                        db.DataService.Delete(itm);
                    }
                    catch (Exception err)
                    {
                        LogErr(err);
                    }
                }
            }
            catch (Exception er)
            {
                LogErr(er);
            }
            finally
            {
                _dirty = _running = false;
                _lastRun = DateTime.Now;
            }
        }

        #endregion

        #region Add

        public static bool Add(ISession session, Type type, int refId, string data)
        {
            if (type == null) return false;

            var obj = new DelayQueue()
            {
                Type = type.FullName + "," + type.Assembly.GetName().Name,
                RefId = refId,
                Data = data,
                CreatedDate = DateTime.Now,
            };

            if (GetExecMethod(obj.Type) == null)
            {
                if (Debugger.IsAttached) Debugger.Break();
                return false;
            }

            session.SaveOrUpdate(obj);
            _dirty = true;

            return true;
        }

        #endregion

        #region GetExecMethod

        public static MethodInfo GetExecMethod(string typeName)
        {
            if (string.IsNullOrEmpty(typeName)) return null;

            var type = Type.GetType(typeName, false, true);

            var ret = type == null ? null : type.GetMethod("DoDelayJob");

            if (ret != null)
            {
                var args = ret.GetParameters();
                if (!ret.IsStatic || args.Length != 2 || args[0].ParameterType != typeof (IDB) ||
                    args[1].ParameterType != typeof (DelayQueue)) return null;
            }

            return ret;
        }

        #endregion
    }
}