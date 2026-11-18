using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Web;
using System.Web.Mvc;
using Claw.Core.Interfaces;
using Claw.Core.DAL;
using Claw.Core;
using Claw.Controls.Modules;
using System.Web.UI.WebControls;
using Claw.Core.Logs;
using Claw.Modules.Eshop.Domain;
using NHibernate;
using AutoMapper;
using Ninject;
using NPOI.SS.Formula.Functions;

namespace Claw.Modules.Eshop.DAL
{
    #region IDB

    public interface IDB
    {
        IDataService DataService { get; }
    }

    public class DefaultDb : IDB
    {
        public DefaultDb()
        {
            DataService = (ClawContext.Current != null ? ClawContext.Current.DataService : null) ?? new DataService(IoC.Kernel.Get<ISession>());
        }

        public DefaultDb(IDataService dataService)
        {
            DataService = dataService;
        }

        public DefaultDb(ISession session)
        {
            DataService = new DataService(session);
        }

        public IDataService DataService { get; private set; }
    }

    public class SeparatedDb : IDB
    {
        public SeparatedDb()
        {
            DataService = new DataService(IoC.Kernel.Get<ISessionFactory>().OpenSession());
        }

        public IDataService DataService { get; private set; }
    }

    #endregion

    #region IDalMgr

    public interface IDalMgr
    {
    } 

    #endregion

    #region Singleton
    public class Singleton<TIf>
    {
        private static TIf _inst;

        public Singleton(Type type)
        {
            _inst = (TIf)Activator.CreateInstance(type);
        }

        public static TIf Default { get { return _inst; } }
    } 
    #endregion

    #region BaseMgr

    public class BaseMgr
    {
        public static volatile bool Debug;
        protected static readonly object Sync = new object();
        protected static readonly Dictionary<string, Type> SubTypes = new Dictionary<string, Type>();
 
        #region GetSiteId

        public static int GetSiteId(IDB db)
        {
            var site = db.DataService == null ? null : db.DataService.GetCurrentSite();
            return site == null ? 0 : site.Id;
        }

        #endregion

        #region Exists

        public static bool Exists<T>(Func<T, int> idExpr, int idVal, Predicate<T> cond,
            params KeyValuePair[] filters)
            where T : class 
        {
            var ds = (ClawContext.Current != null ? ClawContext.Current.DataService : null);
            if (ds == null) return false;
            else return Exists(ds, idExpr, idVal, cond, filters);
        }

        public static bool Exists<T>(Func<T, long> idExpr, long idVal, Predicate<T> cond,
            params KeyValuePair[] filters)
            where T : class
        {
            var ds = (ClawContext.Current != null ? ClawContext.Current.DataService : null);
            if (ds == null) return false;
            else return Exists(ds, idExpr, idVal, cond, filters);
        }

        public static bool Exists<T>(IDataService db, Func<T, long> idExpr, long idVal, Predicate<T> cond,
            params KeyValuePair[] filters)
            where T : class
        {
            int total;
            var list = db.GetList<T>(db.GetCurrentSite().Id, 1, int.MaxValue, out total, filters);

            var obj = cond == null
                ? list.FirstOrDefault(x => idExpr(x) != idVal)
                : list.FirstOrDefault(x => cond(x) && idExpr(x) != idVal);

            return obj != null;
        }

        public static bool Exists<T>(IDataService db, Func<T, int> idExpr, int idVal, Predicate<T> cond,
            params KeyValuePair[] filters)
           where T : class 
        {
            int total;
            var list = db.GetList<T>(db.GetCurrentSite().Id, 1, int.MaxValue, out total, filters);
            
            var obj = cond == null
                ? list.FirstOrDefault(x => idExpr(x) != idVal)
                : list.FirstOrDefault(x => cond(x) && idExpr(x) != idVal);

            return obj != null;
        }

        #endregion

        #region Get

        public static T Get<T>(object id) where T : class
        {
            var ds = (ClawContext.Current != null ? ClawContext.Current.DataService : null);
            if (ds == null) return null;
            else return ds.GetObject<T>(id);
        }

        public static T Get<T>(int id) where T : class
        {
            var ds = (ClawContext.Current != null ? ClawContext.Current.DataService : null);
            if (ds == null) return null;
            else return id == 0 ? null : ds.GetObject<T>(id);
        }

        #endregion

        #region GetDir

        public static string GetDir(SortDirection dir)
        {
            return dir == SortDirection.Ascending ? "asc" : "desc";
        }

        #endregion

        #region TranslateKWord

        public static string TranslateKWord(string value)
        {
            if (string.IsNullOrEmpty(value)) return value;

            var ret = new StringBuilder();

            foreach (var c in value.Trim())
            {
                if (c < 256)
                {
                    if (c == '\'') ret.Append("''");
                    else ret.Append(c);
                }
                else ret.Append('_');
            }

            return ret.ToString();
        }

        #endregion

        #region GetPrefix

        public static List<T> GetPrefix<T>(IDB db, string field, string prefix)
        {
            var hQueryString = string.Format("from {0} where {1} like :pattern", typeof(T).FullName, field);
            return GetList<T>(db, hQueryString, new[] { new System.Tuple<string, string>("pattern", prefix + "%") });
        }

        public static List<System.Tuple<int,string>> GetPrefix<T1,T2>(IDB db, string field, string prefix)
        {
            PropertyInfo propId1 = typeof(T1).GetProperty("Id"), propF1 = typeof(T1).GetProperty(field);
            PropertyInfo propId2 = typeof(T2).GetProperty("Id"), propF2 = typeof(T2).GetProperty(field);
            
            var lst1 = GetPrefix<T1>(db, field, prefix);
            var lst2 = typeof (T1) == typeof (T2) ? new List<T2>() : GetPrefix<T2>(db, field, prefix);

            return lst1.Select(x => new {Id = propId1.GetValue(x), Value = propF1.GetValue(x)})
                .Union(lst2.Select(x => new {Id = propId2.GetValue(x), Value = propF2.GetValue(x)}))
                .Select(x => new System.Tuple<int, string>((int) x.Id, (string) x.Value))
                .ToList();
        } 

        #endregion

        #region GetUniqueFieldHint

        public static string GetUniqueFieldHint<TDom1,TDom2>(IDB db, string name, string value, int? id, string separator = "-")
        {
            if (id == null) return null;

            var i = 1;
            var lst = GetPrefix<TDom1, TDom2>(db, name, value + separator)
                .Select(m => new {Id = m.Item1, Value = m.Item2})
                .Where(m => m.Id != id).ToList();

            while (true)
            {
                var key = value + separator + i;
                if (lst.Any(x => x.Value.EqualsIgCase(key))) i++;
                else break;
            }

            return value + separator + i;
        }

        #endregion

        #region Unique

        public static string Unique<TDom1, TDom2>(IDB db, string name, TDom1 obj)
        {
            var propName = typeof(TDom1).GetProperty(name);
            var propId = typeof(TDom1).GetProperty("Id");
            var nameVal = propName.GetValue(obj);

            if (nameVal == null || (string)nameVal == "") return (string)nameVal;

            return GetUniqueFieldHint<TDom1, TDom2>(db, name, (string)nameVal, (int)propId.GetValue(obj));
        }

        public static string Unique<TDom>(IDB db, string name, TDom obj)
        {
            return Unique<TDom, TDom>(db, name, obj);
        }

        #endregion

        #region GetList

        public static List<T> GetList<T>(IDB db, List<int> ids)
        {
            if (ids.SafeCount() == 0) return new List<T>();

            var hSql = string.Format(@"from {0} where Id in ({1})  ", typeof(T).FullName, ids.ToString(","));

            return GetList<T>(db, hSql);
        }

        public static List<T> GetList<T>(IDB db, List<int> ids, SortDirection sortDirection, string sortField,
            string objAlias = "obj", string langCode = null, string langAlias = "lang")
        {
            if (ids.SafeCount() == 0) return new List<T>();

            var hSql = string.Format(@"select {1} from {0} as {1} ", typeof (T).FullName, objAlias);

            if (langCode != null)
                hSql += string.Format(@" join {0}.Languages as {1} ", objAlias, langAlias);

            hSql += string.Format(@" where {0}.Id in ({1}) ", objAlias, ids.ToString(","));

            if (langCode != null)
                hSql += string.Format(@" and {0}.LangCode = '{1}' ", langAlias, langCode);
            
            hSql += string.Format(@" order by {0} {1}",
                (sortField == "Name" ? langAlias : objAlias) + "." + sortField, GetDir(sortDirection));

            return GetList<T>(db, hSql);
        }

        public static List<T> GetList<T>(string field, object value) where T : class
        {
            var ds = (ClawContext.Current != null ? ClawContext.Current.DataService : null);
            if (ds == null) return new List<T>();
            else return ds.GetList<T>(0, field, value);
        }

        public static List<T> GetList<T>(IDB db, string hQueryString)
        {
            int totalRows;
            return GetList<T>(db, hQueryString, 1, int.MaxValue, out totalRows, null, null);
        }
        public static List<T> GetList<T>(IDB db, string hQueryString, System.Tuple<string, string>[] args)
        {
            int totalRows;
            return GetList<T>(db, hQueryString, 1, int.MaxValue, out totalRows, null, args);
        }

        public static List<T> GetList<T>(IDB db, string hQueryString, int pageIndex, int pageSize, out int totalRows,
             System.Tuple<string, string>[] args)
        {
            return GetList<T>(db, hQueryString, pageIndex, pageSize, out totalRows, "select count(*) {0}", args);
        }


        public static List<T> GetList<T>(IDB db, string hQueryString, int pageIndex, int pageSize, out int totalRows,
            string countSql = "select count(*) {0}", System.Tuple<string, string>[] args = null)
        {
            return GetList<T>(db, hQueryString, pageIndex, pageSize, out totalRows, null, countSql, args);
        }

        public static List<T> GetList<T>(IDB db, string hQueryString, int pageIndex, int pageSize, out int totalRows, Func<IQuery, IQuery> funcFilter,
            string countSql = "select count(*) {0}", System.Tuple<string, string>[] args = null)
        {
            try
            {
                var stime = DateTime.Now;
                var session = ((DataService) db.DataService).Session;
                totalRows = 0;

                var querySearch = session.CreateQuery(hQueryString)
                    .SetCacheable(true)
                    .SetFirstResult((pageIndex - 1)*pageSize)
                    .SetMaxResults(pageSize);

                if (funcFilter != null) querySearch = funcFilter(querySearch);

                var multiQuery = session.CreateMultiQuery();
                var qSearch = multiQuery.Add<T>("search", querySearch);

                if (args != null) args.Action(x => qSearch.SetString(x.Item1, x.Item2));

                if (countSql != null && pageSize != int.MaxValue)
                {
                    int index = hQueryString.IndexOf("from");
                    int iOrderby = hQueryString.IndexOf("order by");
                    if (iOrderby == -1) iOrderby = hQueryString.Length;
                    string hQueryTotal = hQueryString.Substring(index, iOrderby - index);
                    hQueryTotal = string.Format(countSql, hQueryTotal);

                    var queryTotal = session.CreateQuery(hQueryTotal)
                        .SetCacheable(true);

                    if (args != null) args.Action(x => queryTotal.SetString(x.Item1, x.Item2));

                    multiQuery.Add("totalRows", queryTotal);

                    var t = multiQuery.GetResult("totalRows");
                    if (t is ArrayList)
                    {
                        long tmpTotal = (long) (t as ArrayList)[0];
                        totalRows = (int) tmpTotal;
                    }
                }

                var lst = multiQuery.GetResult("search");
                var ret = (lst as List<T>) ?? ((ArrayList) lst).Cast<T>().ToList();

                if (ret != null && (pageSize == int.MaxValue || countSql == null)) totalRows = ret.Count;

                if (Debug && (DateTime.Now - stime).TotalSeconds > 1)
                    LogHelper.EventLog($"BaseMgr.GetList: Url = {HttpContext.Current?.Request?.RawUrl}, duration = {(DateTime.Now - stime).TotalSeconds}s\r\n{hQueryString}\r\n{GetSqlString(hQueryString,session)}");

                return ret;
            }
            catch (Exception err)
            {
                LogHelper.Log4net(err,
                    string.Format("{0}\r\n{1}", HttpContext.Current == null ? null : HttpContext.Current.Request.Url, hQueryString ?? ""));

                throw err;
            }
        }

        #endregion

        #region Delete

        protected static bool Delete<T>(IDB db, Func<ISession, T, bool> fAction, params T[] objects)
            where T : class
        {
            return Delete<T>(db, true, fAction, objects);
        }

        protected static bool Delete<T>(IDB db, bool transaction, Func<ISession, T, bool> fAction, params T[] objects)
            where T : class
        {
            ITransaction tran = null;
            try
            {
                var nAct = 0;
                var session = ((DataService)db.DataService).Session;
                tran = session.BeginTransaction();

                foreach (var obj in objects)
                {
                    if (fAction != null && !fAction(session, obj))
                    {
                        if (transaction) break;
                        else continue;
                    }
                    else nAct++;
                    session.Delete(obj);
                    if (!transaction) { tran.Commit(); tran = session.BeginTransaction(); }
                }

                if (nAct != objects.Length)
                {
                    if (transaction) tran.Rollback();
                    return false;
                }

                if (transaction) tran.Commit();

                return true;
            }
            catch (Exception exp)
            {
                if (tran != null)
                    tran.Rollback();

                LogErr(exp, ClawExceptionType.DeleteObject, "Delete domain object");
            }
            return false;
        }

        #endregion

        #region SaveOrUpdate

        public static bool SaveOrUpdate<T>(IDB db, Func<ISession, T, bool> fAction, params T[] objects)
            where T : class
        {
            return SaveOrUpdate(db, null, fAction, objects);
        }

        public static bool SaveOrUpdate<T>(IDB db, bool useTransaction, Func<ISession, T, bool> fAction, params T[] objects)
            where T : class
        {
            return SaveOrUpdate(db, useTransaction, null, fAction, objects);
        }

        public static bool SaveOrUpdate<T>(IDB db, Func<ISession, T, bool> fActionBefore,
            Func<ISession, T, bool> fActionAfter, params T[] objects)
            where T : class
        {
            return SaveOrUpdate(db, true, fActionBefore, fActionAfter, objects);
        }

        public static bool SaveOrUpdate<T>(IDB db, bool useTransaction, Func<ISession, T, bool> fActionBefore, Func<ISession, T, bool> fActionAfter, 
            params T[] objects)
            where T : class
        {
            ITransaction tran = null;
            try
            {
                int nAct = 0, step = fActionBefore != null ^ fActionAfter != null ? 2 : 1;
                var session = ((DataService)db.DataService).Session;
                if (useTransaction) tran = session.BeginTransaction();

                foreach (var obj in objects)
                {
                    if (fActionBefore != null)
                        if (!fActionBefore(session, obj)) break;
                        else nAct += step;

                    session.SaveOrUpdate(obj);

                    if (fActionAfter != null)
                        if (!fActionAfter(session, obj)) break;
                        else nAct += step;
                }

                if (nAct != objects.Length * 2)
                {
                    if (tran != null) tran.Rollback();
                    return false;
                }

                if (tran != null) tran.Commit();
                return true;
            }
            catch (Exception exp)
            {
                if (tran != null)
                    tran.Rollback();

                LogErr(exp, ClawExceptionType.DeleteObject, "SaveOrUpdate domain object");
            }
            return false;
        }

        public static bool SaveOrUpdate(object obj, ISession session, bool autocommit = true)
        {
            var trans = autocommit ? session.BeginTransaction() : null;
            try
            {
                session.SaveOrUpdate(obj);
                if (trans != null) trans.Commit();
                return true;
            }
            catch (Exception e)
            {
                if (trans != null) trans.Rollback(); 
                LogErr(e); 
                return false;
            }
        }

        public static bool SaveOrUpdate<T>(IDB db, List<T> objects)
        {
            if (db == null || objects == null) return false;

            var session = db.GetSession();
            var trans = session.BeginTransaction();

            try
            {
                session.SetBatchSize(100);
                objects.Action(x => session.SaveOrUpdate(x));
                trans.Commit();

                return true;
            }
            catch (Exception err)
            {
                trans.Rollback(); 
                LogErr(err); 
                return false;
            }
        }

        #endregion

        #region Update        
        public static bool Update<T>(IDB db, List<T> objects, bool useTransaction = true) 
        {
            return Update(db, false, objects, useTransaction);
        }
        public static bool Update<T>(IDB db, bool intervalFlush, List<T> objects, bool useTransaction = true)
        {
            if (db == null || objects == null) return false;

            int n = 0, count = objects.Count;
            var session = db.GetSession();
            var trans = useTransaction ? session.BeginTransaction() : null;

            try
            {
                session.SetBatchSize(100);

                objects.Action(x =>
                {
                    session.Update(x);

                    if ((++n % 100) == 0 && intervalFlush)
                    {
                        session.Flush();
                        LogHelper.EventLog($"BaseMgr.Update: Flush, n = {n}/{count}");
                    }
                });

                if (trans != null) trans.Commit();

                return true;
            }
            catch (Exception err)
            {
                if (trans != null) trans.Rollback();
                LogErr(err); 
                return false;
            }
        } 

        #endregion

        #region Expr
        public static LambdaExpression Expr<TDom, TProp>(TDom obj, Expression<Func<TDom, TProp>> expression)
        {
            return expression;
        }
        #endregion

        #region CurrentUser
        public static Claw.Core.Domain.User CurrentUser
        {
            get { return ClawContext.Current.User ?? new Claw.Core.Domain.User(); }
        }
        #endregion

        #region GetSqlString
        public static string GetSqlString(string hsql, NHibernate.ISession session)
        {
            var sfi = session.GetSessionImplementation()?.Factory;
            var mf = new Dictionary<string, NHibernate.IFilter>();

            sfi?.DefinedFilterNames?.Action(m => mf.Add(m, session.EnableFilter(m)));

            var qt = sfi?.Settings?.QueryTranslatorFactory?.CreateQueryTranslators(hsql, null, false, mf, sfi);

            return qt?.FirstOrDefault()?.SQLString;
        }
        #endregion

        #region LogErr

        public static void LogErr(Exception err, ClawExceptionType type = ClawExceptionType.DataProvider,
            string section = null)
        {
            try
            {
                if (Debugger.IsAttached) Debugger.Break();
                LogHelper.Log4net(err, type, section ?? "");
            }
            catch (Exception ex) { }
        }

        #endregion
    } 

    #endregion

    #region BaseMgr<TMod, TDom>

    public class BaseMgr<TMod, TDom> : BaseMgr
        where TMod : class
        where TDom : class
    {
        public static event Action<TDom> Updated;
        public static event Action<TDom> Deleted;
 
        #region Init

        protected static void Init(Type type)
        {
            lock (Sync)
            {
                SubTypes[typeof (TMod).FullName] = SubTypes[typeof (TDom).FullName] = type;
                type.GetInterfaces()
                    .Where(x => x.GetInterface(typeof (IDalMgr).Name) != null)
                    .Action(x => Activator.CreateInstance(typeof (Singleton<>).MakeGenericType(x), type));
            }
        }

        #endregion

        #region Get

        public static TMod Get(IDB db, int id)
        {
            if (id <= 0) return null;

            var obj = db.DataService.GetObject<TDom>(id);
            return obj == null ? null : Mapper.Map<TDom, TMod>(obj);
        }

        public static TDom GetDom(IDB db, int id, ISession session = null)
        {
            if (id <= 0) return null;

            if (session != null) return session.Get<TDom>(id);
            else return db.DataService.GetObject<TDom>(id);
        }

        public static TDom GetDom(ISession session, int id)
        {
            if (id <= 0) return null;

            return session.Get<TDom>(id);
        }

        #endregion

        #region GetList

        public static List<TMod> GetList(IDB db, int[] ids)
        {
            if (ids.SafeCount() == 0) return new List<TMod>();

            var lst = db.DataService.GetList<TDom>(ids);

            return Mapper.Map<List<TDom>, List<TMod>>(lst);
        }

        #endregion

        #region GetAllDom

        public static List<TDom> GetAllDom(IDB db)
        {
            return GetList<TDom>(db, string.Format("select dom from {0} as dom ", typeof (TDom).FullName));
        } 

        #endregion

        #region Enable

        public static bool Enable(IDB db, int id, bool active)
        {
            var obj = db.DataService.GetObject<TDom>(id);

            if (obj != null)
            {
                var oUpd = obj as IStatus;

                if (oUpd == null) return false;

                oUpd.Active = active;

                if (obj.Update(db.DataService))
                {
                    new BaseMgr<TMod, TDom>().OnUpdate(obj);
                    return true;
                }
                else return false;
            }

            return true;
        }

        #endregion

        #region Delete

        public static bool Delete(IDB db, Func<ISession, TDom, bool> beforeDel, Action<TDom[]> okDel, params int[] ids)
        {
            return Delete(db,true, beforeDel, okDel,ids);
        }

        public static bool Delete(IDB db, bool transaction, Func<ISession, TDom, bool> beforeDel, Action<TDom[]> okDel, params int[] ids)
        {
            var failedLst = new List<TDom>();

            Func<ISession, TDom, bool> fAction = (session, obj) =>
            {
                if (obj == null) return false;
                if (beforeDel == null) return true;
                if (beforeDel(session, obj)) return true;
   
                failedLst.Add(obj); return false;
            };

            var objects = ids.Select(x => db.DataService.GetObject<TDom>(x)).ToArray();
            var retCode = Delete<TDom>(db, transaction, fAction, objects);

            if (retCode || !transaction)
            {
                objects = objects.Where(x => !failedLst.Contains(x)).ToArray();

                if (okDel != null) okDel(objects);

                foreach (var obj in objects)
                    new BaseMgr<TMod, TDom>().OnDelete(obj);
            }

            return retCode;
        }

        #endregion

        #region SaveOrUpdate

        public static bool SaveOrUpdate(IDB db, int id, Action<TDom> fCopy)
        {
            var obj = GetDom(db, id);

            if (obj != null)
            {
                fCopy(obj);
                if (obj.SaveorUpdate(db.DataService))
                {
                    new BaseMgr<TMod, TDom>().OnUpdate(obj);
                    return true;
                }
            }
            return false;
        }

        public static bool SaveOrUpdate(IDB db, TDom obj)
        {
            if (obj != null)
            {
                if (obj.SaveorUpdate(db.DataService))
                {
                    new BaseMgr<TMod, TDom>().OnUpdate(obj);
                    return true;
                }
            }
            return false;
        }

        #endregion

        #region Update

        public static bool Update<TProp>(IDB db, int id, Expression<Func<TDom, TProp>> expression, TProp value)
        {
            var obj = GetDom(db, id);

            if (obj != null)
            {
                var type = typeof(TDom);
                var prop = type.GetProperty(expression.GetFieldName());

                if (prop == null) return false;

                prop.SetValue(obj, value);

                if (obj.Update(db.DataService))
                {
                    new BaseMgr<TMod, TDom>().OnUpdate(obj);
                    return true;
                }
                else return false;
            }

            return true;
        }

        #endregion

        #region Action

        public static bool Action(IDB db, bool transaction, Func<ISession, TDom, bool> beforeDel, params long[] ids)
        {
            var failedLst = new List<TDom>();

            Func<ISession, TDom, bool> fAction = (session, obj) =>
            {
                if (obj == null) return false;
                if (beforeDel == null) return true;
                if (beforeDel(session, obj)) return true;

                failedLst.Add(obj); return false;
            };

            var objects = ids.Select(x => db.DataService.GetObject<TDom>(x)).ToArray();
            var retCode = Action(db, transaction, fAction, objects);

            if (retCode)
            {
                objects = objects.Where(x => !failedLst.Contains(x)).ToArray();

                foreach (var obj in objects)
                    new BaseMgr<TMod, TDom>().OnUpdate(obj);
            }

            return retCode;
        }

        public static bool Action(IDB db, bool transaction, Func<ISession, TDom, bool> beforeDel, params int[] ids)
        {
            var failedLst = new List<TDom>();

            Func<ISession, TDom, bool> fAction = (session, obj) =>
            {
                if (obj == null) return false;
                if (beforeDel == null) return true;
                if (beforeDel(session, obj)) return true;

                failedLst.Add(obj); return false;
            };

            var objects = ids.Select(x => db.DataService.GetObject<TDom>(x)).ToArray();
            var retCode = Action(db, transaction, fAction, objects);

            if (retCode)
            {
                objects = objects.Where(x => !failedLst.Contains(x)).ToArray();

                foreach (var obj in objects)
                    new BaseMgr<TMod, TDom>().OnUpdate(obj);
            }

            return retCode;
        }

        protected static bool Action(IDB db, bool transaction, Func<ISession, TDom, bool> fAction, params TDom[] objects)
        {
            ITransaction tran = null;
            try
            {
                var nAct = 0;
                var session = ((DataService)db.DataService).Session;
                tran = session.BeginTransaction();

                foreach (var obj in objects)
                {
                    if (!fAction(session, obj))
                    {
                        if (transaction) break;
                        else { tran.Rollback(); tran = session.BeginTransaction(); continue; }
                    }
                    else nAct++;
                   
                    if (!transaction) { tran.Commit(); tran = session.BeginTransaction(); }
                }

                if (nAct != objects.Length)
                {
                    tran.Rollback();
                    return false;
                }

                if (transaction) tran.Commit();

                return true;
            }
            catch (Exception exp)
            {
                if (tran != null) tran.Rollback();

                LogErr(exp, ClawExceptionType.UpdateObject, "Update domain object");
            }
            return false;
        }
        
        #endregion

        #region Evict

        public static void Evict(IDB db, int id)
        {
            var obj = GetDom(db, id);
            if (obj != null) ((DataService)db.DataService).Session.Evict(obj);
        }

        #endregion

        #region Expr
        public static LambdaExpression Expr<TProp>(Expression<Func<TDom, TProp>> expression)
        {
            return expression;
        }
        public static LambdaExpression Expr2<TProp>(Expression<Func<TMod, TProp>> expression)
        {
            return expression;
        }
        #endregion

        #region OnUpdate

        protected virtual void OnUpdate(TDom obj)
        {
            Type type;
            lock (Sync) SubTypes.TryGetValue(typeof (TMod).FullName, out type);

            if (type != null)
            {
                var instMgr = ((BaseMgr<TMod, TDom>) Activator.CreateInstance(type));
                if (instMgr.GetType() != typeof (BaseMgr<TMod, TDom>))
                {
                    instMgr.OnUpdate(obj);
                    if (Updated != null) Updated(obj);
                }
            }
        }

        #endregion

        #region OnDelete

        protected virtual void OnDelete(TDom obj)
        {
            Type type;
            lock (Sync) SubTypes.TryGetValue(typeof (TMod).FullName, out type);

            if (type != null)
            {
                var instMgr = ((BaseMgr<TMod, TDom>) Activator.CreateInstance(type));
                if (instMgr.GetType() != typeof (BaseMgr<TMod, TDom>))
                {
                    instMgr.OnDelete(obj);
                    if (Deleted != null) Deleted(obj);
                }
            }
        }

        #endregion
    } 

    #endregion

    #region Tuple2
    public class Tuple2<T1, T2>
    {
        public T1 Item1;
        public T2 Item2;

        public Tuple2(T1 item1, T2 item2)
        {
            Item1 = item1;
            Item2 = item2;
        }
    } 
    #endregion

    #region Tuple3

    public class Tuple3<T1, T2, T3> : Tuple2<T1, T2>
    {
        public T3 Item3;

        public Tuple3(T1 item1, T2 item2, T3 item3) : base(item1, item2)
        {
            Item3 = item3;
        }
    }

    #endregion

    #region Tuple4

    public class Tuple4<T1, T2, T3, T4> : Tuple3<T1, T2, T3>
    {
        public T4 Item4;

        public Tuple4(T1 item1, T2 item2, T3 item3, T4 item4)
            : base(item1, item2, item3)
        {
            Item4 = item4;
        }
    }

    #endregion

    #region Tuple5

    public class Tuple5<T1, T2, T3, T4, T5> : Tuple4<T1, T2, T3, T4>
    {
        public T5 Item5;

        public Tuple5(T1 item1, T2 item2, T3 item3, T4 item4, T5 item5)
            : base(item1, item2, item3, item4)
        {
            Item5 = item5;
        }
    }

    #endregion

    #region Tuple6

    public class Tuple6<T1, T2, T3, T4, T5, T6> : Tuple5<T1, T2, T3, T4, T5>
    {
        public T6 Item6;

        public Tuple6(T1 item1, T2 item2, T3 item3, T4 item4, T5 item5, T6 item6)
            : base(item1, item2, item3, item4, item5)
        {
            Item6 = item6;
        }
    }

    #endregion

    #region Tuple7

    public class Tuple7<T1, T2, T3, T4, T5, T6, T7> : Tuple6<T1, T2, T3, T4, T5, T6>
    {
        public T7 Item7;

        public Tuple7(T1 item1, T2 item2, T3 item3, T4 item4, T5 item5, T6 item6, T7 item7)
            : base(item1, item2, item3, item4, item5,item6)
        {
            Item7 = item7;
        }
    }

    #endregion
}