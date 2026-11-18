using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using AutoMapper;
using Claw.Controls.Modules;
using Claw.Core;
using NHibernate;
using System.Web.UI.WebControls;
using Claw.Core.DAL;
using Claw.Modules.Eshop.Controllers.Admin;
using Claw.Modules.Eshop.Domain;
using Claw.Modules.Eshop.Models;

namespace Claw.Modules.Eshop.DAL
{
    public class TaskMgr : BaseMgr<TaskModel, Task>
    {
        static TaskMgr() { Init(typeof(TaskMgr)); }

        #region GetAll

        public static List<TaskModel> GetAll(IDB db, int pageIndex, int pageSize, out int totalRows,
            SortDirection sortDirection = SortDirection.Descending, string sortField = "Id")
        {
            var hSql = string.Format(@"
            select tsk from {0} as tsk ", typeof(Task).FullName);

            hSql += string.Format(" order by {0} {1}", "tsk." + sortField, GetDir(sortDirection));

            var ret = GetList<Task>(db, hSql, pageIndex, pageSize, out totalRows);

            return Mapper.Map<List<Task>, List<TaskModel>>(ret);
        }

        public static List<TaskModel> GetAll(IDB db)
        {
            int total;
            return GetAll(db, 1, Int32.MaxValue, out total);
        }

        #endregion

        #region SaveorUpdate
        public static bool SaveorUpdate(IDB db, TaskModel model)
        {
            if (model == null) return false;

            var obj = GetDom(db, model.Id) ?? new Task();

            obj.Name = model.Name;
            obj.Active = model.Active;
            obj.Interval = model.Interval;
            obj.ExeClassInfo = model.ExeClassInfo;
            obj.LastRunMsg = model.LastRunMsg;
            obj.LastRunTime = model.LastRunTime;

            if (obj.SaveorUpdate(db.DataService))
            {
                model.Id = obj.Id;
                new TaskMgr().OnUpdate(obj); return true;
            }
            else return false;
        }
        #endregion

        #region Update

        public static bool Update(IDB db, TaskModel model)
        {
            if (model == null) return false;

            var obj = GetDom(db, model.Id);

            if (obj == null) return false;

            obj.LastRunMsg = model.LastRunMsg;
            obj.LastRunTime = model.LastRunTime;
            obj.LastDoneTime = model.LastDoneTime;

            if (obj.Update(db.DataService))
            {
                new TaskMgr().OnUpdate(obj);
                return true;
            }
            else return false;
        }

        #endregion

        #region OnUpdate
        protected override void OnUpdate(Task obj)
        {
        }
        #endregion

        #region OnDelete
        protected override void OnDelete(Task obj)
        {
        }
        #endregion
    }
}