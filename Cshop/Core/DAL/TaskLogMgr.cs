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
using Claw.Modules.Eshop.Domain;

namespace Claw.Modules.Eshop.DAL
{
    public class TaskLogMgr : BaseMgr<TaskLogModel, TaskLog>
    {
        static TaskLogMgr() { Init(typeof(TaskLogMgr)); }

        #region GetAll

        public static List<TaskLogModel> GetAll(IDB db, int pageIndex, int pageSize, out int totalRows,
            SortDirection sortDirection = SortDirection.Descending, string sortField = "Id")
        {
            var hSql = string.Format(@"
            select tlog from {0} as tlog ", typeof(TaskLog).FullName);

            #region Sorting

            switch (sortField)
            {
                case "CreatedDate":
                case "CreatedDate.Text":
                    sortField = "Id";
                    break;
            }

            hSql += string.Format(" order by {0} {1}", "tlog." + sortField, GetDir(sortDirection));

            #endregion

            var ret = GetList<TaskLog>(db, hSql, pageIndex, pageSize, out totalRows);

            return Mapper.Map<List<TaskLog>, List<TaskLogModel>>(ret);
        }

        #endregion

        #region Log

        public static void Log(IDB db, int taskid, string format, params object[] args)
        {
            if (format.IsNullOrEmpty() || args == null) return;

            try
            {
                new TaskLog()
                {
                    Task = TaskMgr.GetDom(db, taskid),
                    CreatedDate = DateTime.Now,
                    Data = string.Format(format, args)
                }.SaveorUpdate((db ?? new DefaultDb()).DataService);
            }
            catch (Exception e)
            {
                if (Debugger.IsAttached) Debugger.Break();
                else LogErr(e);
            }
        }

        #endregion

        #region SaveorUpdate

        public static bool SaveorUpdate(IDB db, TaskLogModel model)
        {
            if (model == null) return false;

            var obj = GetDom(db, model.Id) ?? new TaskLog() { CreatedDate = DateTime.Now };

            obj.Task = TaskMgr.GetDom(db, model.TaskId);
            obj.Data = model.Data;

            if (obj.SaveorUpdate(db.DataService))
            {
                model.Id = obj.Id;
                new TaskLogMgr().OnUpdate(obj);

                return true;
            }
            else return false;
        }

        #endregion

        #region OnUpdate
        protected override void OnUpdate(TaskLog obj)
        {
        }
        #endregion

        #region OnDelete
        protected override void OnDelete(TaskLog obj)
        {
        }
        #endregion
    }
}