using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Web;
using AutoMapper;
using Claw.Controls.Modules;
using Claw.Core;
using Claw.Modules.Eshop.DAL.Interfaces;
using Claw.Modules.Eshop.Helper;
using NHibernate;
using System.Web.UI.WebControls;
using Claw.Core.DAL;
using Claw.Modules.Eshop.Controllers.Admin;
using Claw.Modules.Eshop.Domain;
using Claw.Modules.Eshop.Models;
using System.Web.Mvc;
using Claw.Modules.Eshop.Models;

namespace Claw.Modules.Eshop.DAL
{
    public class ActionLogMgr : BaseMgr<ActionLogModel, ActionLog>
    {
        static ActionLogMgr() { Init(typeof(ActionLogMgr)); }

        #region ChangeInfo

        public class ChangeInfo<TMod>
        {
            public int? Id { get; set; }
            public TMod Model { get; set; }
            public Dictionary<string, string> Changes { get; set; }
            public string LogTemplate { get; set; }
            public LogActionType Action { get; set; }
        }
 
        #endregion

        #region GetAll

        public static List<ActionLogModel> GetAll(IDB db, int pageIndex, int pageSize, out int totalRows,
            SortDirection sortDirection = SortDirection.Descending, string sortField = "Id", int? action = null, int? refId = null)
        {
            var hSql = string.Format(@"
            select alog from {0} as alog 
            where 1 = 1 ", typeof(ActionLog).FullName);

            if (action != null) hSql += " and alog.Action = " + action;

            if (refId != null) hSql += " and alog.RefId = " + refId;

            #region Sorting

            switch (sortField)
            {
                case "CreatedDate":
                case "CreatedDate.Text":
                    sortField = "Id";
                    break;
            }

            hSql += string.Format(" order by {0} {1}", "alog." + sortField, GetDir(sortDirection));

            #endregion

            var ret = GetList<ActionLog>(db, hSql, pageIndex, pageSize, out totalRows);

            return Mapper.Map<List<ActionLog>, List<ActionLogModel>>(ret);
        }

        #endregion

        #region SaveorUpdate
        public static bool SaveorUpdate(IDB db, ActionLogModel model, HttpRequestBase request,
            ISession session = null, string userAgent = null, HttpRequest requestEx = null)
        {
            if (model == null || /*request == null ||*/
                !typeof (LogActionType).IsEnumDefined(model.Action & 0x1F)) return false;

            var obj = GetDom(db, model.Id) ?? new ActionLog() { CreatedDate = DateTime.Now };

            #region ActionLog

            obj.RefId = model.RefId;
            obj.Action = model.Action;
            obj.Activity = model.Activity;

            if (request != null || requestEx != null)
            {
                obj.UserIp = request != null ? request.UserHostAddress : requestEx.UserHostAddress;
                obj.UserName = CurrentUser.Username;
                obj.UserHost = (request != null ? request.UserHostName : requestEx.UserHostName).ResolveHostName();
                obj.UserAgent = request != null ? request.UserAgent : requestEx.UserAgent;
            }
            else obj.UserIp = obj.UserName = obj.UserHost = obj.UserAgent = "";

            obj.UserAgent = userAgent ?? obj.UserAgent;

            if (obj.Action.InList(LogActionType.CustProduct) && obj.RefId != null)
            {            
                bool showPrice;
                decimal? discountRate;
                Dictionary<int, decimal> cateDiscountRates;
                var userId = Singleton<ISessionHelper>.Default.GetUserId(out showPrice, out discountRate, out cateDiscountRates);
                obj.UserName = Singleton<ICustomerMgr>.Default.GetDom(db, userId ?? 0, session).SafeProp(m => m.Email).IfNull(obj.UserName);
            }

            #endregion

            if (session != null) session.SaveOrUpdate(obj);
            else if (!obj.SaveorUpdate(db.DataService)) return false;

            model.Id = obj.Id;
            new ActionLogMgr().OnUpdate(obj);

            return true;
        }
        #endregion

        #region Add

        public static void Add<TMod>(IDB db, int? id, TMod model, LogActionType action,
            Dictionary<string, string> changes, string template, HttpRequestBase request,
            ISession session = null, string userAgent = null)
        {
            if (changes == null || changes.Count == 0 || template.IsNullOrEmpty()) return;

            var data = changes.Select(x => template.Replace("{0}", x.Key.GetDisplayName(model))
                .Replace("{1}", (x.Value ?? "")))
                .ToString(". ");

            SaveorUpdate(db, new ActionLogModel()
            {
                RefId = id,
                Action = (int) action,
                Activity = data,
            }, request, session, userAgent);
        }

        public static void Add<TMod>(IDB db, List<ChangeInfo<TMod>> changes, HttpRequestBase request,
            ISession session = null, string userAgent = null)
        {
            if (changes == null || changes.Count == 0) return;

            foreach (var info in changes)
            {
                Add(db, info.Id, info.Model, info.Action, info.Changes, info.LogTemplate, request, session, userAgent);
            }
        }

        #endregion

        #region OnUpdate
        protected override void OnUpdate(ActionLog obj)
        {
        }
        #endregion

        #region OnDelete
        protected override void OnDelete(ActionLog obj)
        {
        }
        #endregion
    }
}