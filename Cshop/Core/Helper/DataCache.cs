using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Web;
using Claw.Core;
using Claw.Core.DAL;
using Claw.Core.Logs;
using Claw.Modules.Eshop.DAL;
using NHibernate;
using Ninject;

namespace Claw.Modules.Eshop.Helper
{
    #region DataCacheBase

    public class DataCacheBase
    {
        protected static object SyncBase = new object();
        protected static Dictionary<string, Action<IDB>> RefreshFuncs = new Dictionary<string, Action<IDB>>();
        private static System.Threading.Tasks.Task _task = null;
        private static readonly Timer Timer = new Timer(DoRefresh, null, 1000, 1000);

        static void DoRefresh(object obj)
        {
            if (_task == null || _task.IsCompleted || _task.IsCanceled || _task.IsFaulted)
            {
                _task = System.Threading.Tasks.Task.Run(() =>
                {
                    var session = IoC.Kernel == null ? null : IoC.Kernel.Get<ISession>();
                    var db = session == null ? null : new DefaultDb(new DataService(session));

                    if (db != null) RefreshFuncs.Action(x => x.Value(db));
                });
            }
        }
    }

    #endregion

    #region DataCache

    public class DataCache<TScope, TItm> : DataCacheBase
        where TItm : class
        where TScope : CacheScope
    {
        #region Cell

        public class Cell
        {
            public TItm Data { get; set; }
            public DateTime Time { get; set; }
            public DateTime Timestamp { get; set; }
            public DateTime AddedTime { get; set; }
        }

        #endregion

        #region CellEx

        public class CellEx : Cell
        {
            public Func<IDB, TItm> DataFunc { get; set; }

            public int Seconds { get { return (int)(Time - AddedTime).TotalSeconds; } }

            public int MinSeconds { get { return (int)(Timestamp - AddedTime).TotalSeconds; } }
        }
 
        #endregion

        private static readonly object Sync = new object();
        private static readonly Dictionary<string, Cell> Items = new Dictionary<string, Cell>();
        private static readonly TScope Scope = Activator.CreateInstance<TScope>();
        private static readonly Timer Timer = new Timer(DoClean, null, 15 * 60 * 1000, 15 * 60 * 1000);

        #region Clear
        public static void Clear()
        {
            lock (Sync)
            {
                Items.Clear();
            }
        }
        #endregion

        #region Remove

        public static void Remove(string key)
        {
            if (string.IsNullOrEmpty(key)) return;

            lock (Sync)
            {
                if (Items.ContainsKey(key)) Items.Remove(key);
            }
        }

        #endregion

        #region Get

        public static TItm Get(string key, Func<TItm> fData, int seconds, bool forced = false, int minSeconds = 1, bool refresh = false)
        {
            if (seconds <= 2 || (!forced && !Cacheable)) return fData();

            Cell ret;

            lock (Sync)
            {
                if (!refresh && Items.TryGetValue(key, out ret) && ret.Time >= DateTime.Now &&
                    ret.Timestamp >= Scope.LastChange) return ret.Data;
            }

            ret = new Cell()
            {
                Data = fData(),
                AddedTime =  DateTime.Now
            };

            ret.Time = ret.AddedTime.AddSeconds(seconds);
            ret.Timestamp = ret.AddedTime.AddSeconds(minSeconds);

            lock (Sync) { Items[key] = ret; }

            return ret.Data;
        }

        public static TItm Get(IDB db, string key, Func<IDB, TItm> fData, int seconds, bool forced = false, int minSeconds = 1)
        {
            if (seconds <= 2 || (!forced && !Cacheable)) return fData(db);

            Cell ret;

            lock (Sync)
            {
                if (Items.TryGetValue(key, out ret) && ret.Time >= DateTime.Now &&
                    ret.Timestamp >= Scope.LastChange) return ret.Data;
            }

            ret = new CellEx()
            {
                Data = fData(db),
                AddedTime = DateTime.Now,
                DataFunc = fData,
            };

            ret.Time = ret.AddedTime.AddSeconds(seconds);
            ret.Timestamp = ret.AddedTime.AddSeconds(minSeconds);

            lock (Sync) { Items[key] = ret; }
            
            lock (SyncBase)
            {
                var type = typeof(DataCache<TScope, TItm>);
                if (!RefreshFuncs.ContainsKey(type.FullName)) RefreshFuncs.Add(type.FullName, DoRefresh);
            }

            //if (seconds < 120 && Debugger.IsAttached) Debugger.Break();

            return ret.Data;
        }

        #endregion

        #region Cacheable

        private static bool Cacheable
        {
            get
            {
                if (HttpContext.Current == null) return false;

                const StringComparison fcmp = StringComparison.OrdinalIgnoreCase;
                var path = HttpContext.Current.Request.Url.AbsolutePath;
                return !path.StartsWith("/admin/", fcmp) && !path.StartsWith("/AdminCP/", fcmp) &&
                       !path.StartsWith("/BackEnd/", fcmp);
            }
        }

        #endregion

        #region DoClean

        static void DoClean(object state)
        {
            try
            {
                lock (Sync)
                {
                    if (Items.Count == 0) return;

                    var chktime = DateTime.Now.AddMinutes(-30);
                    var remkeys = Items.Take(100, x =>
                        (x.Value as CellEx) == null && x.Value.Time < chktime)
                        .Select(x => x.Key).ToList();

                    foreach (var key in remkeys) Items.Remove(key);
                }
            }
            catch (Exception er)
            {
                if(Debugger.IsAttached)Debugger.Break();
            }
        }

        #endregion

        #region DoRefresh

        static void DoRefresh(IDB db)
        {
            try
            {
                var chkTime = DateTime.Now.AddMilliseconds(10000);
                var lastChange = Scope.LastChange;
                List<string> keys;
                 
                lock (Sync) keys = Items
                    .Where(x => { var itm = x.Value as CellEx; return itm != null && itm.Seconds >= 120 && (itm.Time < chkTime || itm.AddedTime < lastChange); })
                    .Select(x => x.Key).ToList();

                foreach (var key in keys)
                {
                    Cell cell;
                    lock (Sync) Items.TryGetValue(key, out cell);
                    var itm = cell as CellEx;
                    if (itm != null)
                    {
                        var newItm = new CellEx()
                        {
                            Data = itm.DataFunc(db),
                            AddedTime = DateTime.Now,
                            DataFunc = itm.DataFunc,
                        };

                        newItm.Time = newItm.AddedTime.AddSeconds(itm.Seconds);
                        newItm.Timestamp = newItm.AddedTime.AddSeconds(itm.MinSeconds);

                        lock (Sync) Items[key] = newItm;
                    }
                }
            }
            catch (Exception e)
            {
                if (Debugger.IsAttached) Debugger.Break();
            }
        }
 
        #endregion
    }

    #endregion

    #region AsyncDataCache
    public static class AsyncDataCache<T>
    {
        #region Cell
        public class Cell
        {
            public T Data { get; set; }
            public DateTime Time { get; set; }
            public Func<T> FData { get; set; }
            public string Key { get; set; }
            public int Seconds { get; set; }
        } 
        #endregion

        private static readonly object Sync = new object();
        private static readonly Dictionary<string, Cell> Items = new Dictionary<string, Cell>();
        private static DateTime LastCheck = DateTime.Now;

        #region Clear
        public static void Clear()
        {
            lock (Sync)
            {
                Items.Clear();
            }
        }
        #endregion

        #region Get

        public static T Get(string key, Func<T> fData, int seconds)
        {
            Cell ret = null;

            lock (Sync)
            {
                if (Items.TryGetValue(key, out ret) && ret.Time >= DateTime.Now) return ret.Data;
            }

            if (ret == null)
            {
                ret = new Cell()
                {
                    Data = fData(),
                    Key = key,
                    Seconds = seconds,
                    FData = fData,
                    Time = DateTime.Now.AddSeconds(seconds),
                };

                lock (Sync) { Items[key] = ret; }
            }
            else
            {
                ret.Key = key;
                ret.FData = fData;
                ret.Seconds = seconds;
                ret.Time = DateTime.Now.AddSeconds(seconds - 15);

                ThreadPool.QueueUserWorkItem(DoJob, ret);
            }

            return ret.Data;
        }

        #endregion

        #region DoJob
        private static void DoJob(object data)
        {
            try
            {
                var cell = (Cell)data;
                cell.Data = cell.FData();
                cell.Time = DateTime.Now.AddSeconds(cell.Seconds);
                lock (Sync)
                {
                    Items[cell.Key] = cell;

                    if ((DateTime.Now - LastCheck).TotalMinutes > 15)
                    {
                        var chkTime = (LastCheck = DateTime.Now).AddHours(-24);
                        foreach (var itm in Items.Where(x => x.Value.Time < chkTime).ToList())
                        {
                            Items.Remove(itm.Key);
                        }
                    }
                }
            }
            catch (Exception err)
            {
                LogHelper.Log(err, err.Message);
            }
        } 
        #endregion
    } 
    #endregion
}