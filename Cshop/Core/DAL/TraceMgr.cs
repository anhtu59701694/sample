using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using System.Web;
using AutoMapper;
using Claw.Controls.Modules;
using Claw.Core;
using NHibernate;
using System.Web.UI.WebControls;
using Claw.Core.DAL;
using Claw.Modules.Eshop.Controllers.Admin;
using Claw.Modules.Eshop.Models;
using System.Web.Mvc;
using Ninject;
using Quartz;
using Trace = Claw.Modules.Eshop.Domain.Trace;

namespace Claw.Modules.Eshop.DAL
{
    public class TraceMgr : BaseMgr<TraceModel, Trace>
    {
        public static bool Enabled { get; set; }

        static TraceMgr()
        {
            Init(typeof(TraceMgr));
            Enabled = false;
        }

        #region GetAll

        public static List<TraceModel> GetAll(IDB db, int pageIndex, int pageSize, out int totalRows,
            SortDirection sortDirection = SortDirection.Descending, string sortField = "Id")
        {
            var hSql = string.Format(@"
            select atrc from {0} as atrc 
            ", typeof(Trace).FullName);

            #region Sorting

            switch (sortField)
            {
                case "CreatedDate":
                case "CreatedDate.Text":
                    sortField = "Id";
                    break;
            }

            hSql += string.Format(" order by {0} {1}", "atrc." + sortField, GetDir(sortDirection));

            #endregion

            var ret = GetList<Trace>(db, hSql, pageIndex, pageSize, out totalRows);

            return Mapper.Map<List<Trace>, List<TraceModel>>(ret);
        }

        #endregion

        #region Log

        public static void Log(string format, params object[] args)
        {
            if (!Enabled || format.IsNullOrEmpty() || args == null) return;

            Task.Run(() =>
            {
                try
                {
                    var session = IoC.Kernel.Get<ISession>();
                    session.SaveOrUpdate(new Trace()
                    {
                        CreatedDate = DateTime.Now,
                        Data = string.Format(format, args)
                    });
                }
                catch (Exception e)
                {
                    if (Debugger.IsAttached) Debugger.Break();
                    else LogErr(e);
                }
            });
        }

        #endregion

        #region SaveorUpdate

        public static bool SaveorUpdate(IDB db, TraceModel model)
        {
            if (model == null) return false;

            var obj = GetDom(db, model.Id) ?? new Trace() { CreatedDate = DateTime.Now };

            obj.Data = model.Data;

            if (obj.SaveorUpdate(db.DataService))
            {
                model.Id = obj.Id;
                new TraceMgr().OnUpdate(obj);

                return true;
            }
            else return false;
        }

        #endregion

        #region OnUpdate
        protected override void OnUpdate(Trace obj)
        {
        }
        #endregion

        #region OnDelete
        protected override void OnDelete(Trace obj)
        {
        }
        #endregion
    }
}