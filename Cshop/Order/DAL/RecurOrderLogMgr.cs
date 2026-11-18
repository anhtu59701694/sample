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
    public class RecurOrderLogMgr : BaseMgr<RecurOrderLogModel, RecurOrderLog>
    {
        static RecurOrderLogMgr() { Init(typeof(RecurOrderLogMgr)); }

        #region GetAll

        public static List<RecurOrderLogModel> GetAll(IDB db, int recurringid, int pageIndex, int pageSize, out int totalRows,
            SortDirection sortDirection = SortDirection.Descending, string sortField = "Id", bool? isPrivate = null)
        {
            var hSql = string.Format(@"
            select rlog from {0} as rlog 
            where rlog.Recurring.Id = {1}", typeof(RecurOrderLog).FullName, recurringid);

            #region Sorting

            switch (sortField)
            {
                case "CreatedDate.Text":
                    sortField = "CreatedDate";
                    break;
            }

            hSql += string.Format(" order by {0} {1}", "rlog." + sortField, GetDir(sortDirection));

            #endregion

            var ret = GetList<RecurOrderLog>(db, hSql, pageIndex, pageSize, out totalRows);

            return Mapper.Map<List<RecurOrderLog>, List<RecurOrderLogModel>>(ret);
        }

        #endregion

        #region SaveorUpdate
        public static bool SaveorUpdate(IDB db, RecurOrderLogModel model, ISession session = null,
            HttpRequestBase request = null, string uernameLog = null)
        {
            if (model == null) return false;

            var obj = GetDom(db, model.Id) ?? new RecurOrderLog() { CreatedDate = DateTime.Now };
            var audit = request == null ? null
                : string.Format("UserName: {0}, Host:{1}, Ip: {2}, UserAgent: {3}", uernameLog ?? CurrentUser.Username,
                    request.UserHostName.ResolveHostName(), request.UserHostAddress, request.UserAgent);

            #region RecurOrderLog

            obj.Content = model.Content;
            obj.AuditInfo = audit;
            obj.Recurring = RecurOrderMgr.GetDom(db, model.RecurringId ?? 0);

            #endregion

            if (session != null) session.SaveOrUpdate(obj);
            else if (!obj.SaveorUpdate(db.DataService)) return false;

            model.Id = obj.Id;
            new RecurOrderLogMgr().OnUpdate(obj);

            return true;
        }
        #endregion

        #region OnUpdate
        protected override void OnUpdate(RecurOrderLog obj)
        {
        }
        #endregion

        #region OnDelete
        protected override void OnDelete(RecurOrderLog obj)
        {
        }
        #endregion
    }
}