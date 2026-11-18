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
    public class OrderNoteMgr : BaseMgr<OrderNoteModel, OrderNote>
    {
        static OrderNoteMgr() { Init(typeof(OrderNoteMgr)); }

        #region GetAll

        public static List<OrderNoteModel> GetAll(IDB db, int orderid, int pageIndex, int pageSize, out int totalRows,
            SortDirection sortDirection = SortDirection.Descending, string sortField = "Id", bool? isPrivate = null)
        {
            var hSql = string.Format(@"
            select note from {0} as note 
            where note.Order.Id = {1}", typeof(OrderNote).FullName, orderid);

            if (isPrivate != null) hSql += " and note.Private = " + (isPrivate.Value ? 1 : 0);

            #region Sorting

            switch (sortField)
            {
                case "CreatedDate.Text":
                    sortField = "CreatedDate";
                    break;
            }

            hSql += string.Format(" order by {0} {1}", "note." + sortField, GetDir(sortDirection));

            #endregion

            var ret = GetList<OrderNote>(db, hSql, pageIndex, pageSize, out totalRows);

            return Mapper.Map<List<OrderNote>, List<OrderNoteModel>>(ret);
        }

        #endregion

        #region SaveorUpdate
        public static bool SaveorUpdate(IDB db, OrderNoteModel model, ISession session = null, HttpRequestBase request = null)
        {
            return SaveorUpdate(db, model, session, request, null);
        }

        public static bool SaveorUpdate(IDB db, OrderNoteModel model, ISession session,
            HttpRequestBase request, string userName)
        {
            if (model == null) return false;

            var obj = GetDom(db, model.Id) ?? new OrderNote() { CreatedDate = DateTime.Now };
            var audit = request == null ? null
                : string.Format("UserName: {0}, Host:{1}, Ip: {2}, UserAgent: {3}", userName ?? CurrentUser.Username,
                    request.UserHostName.ResolveHostName(), request.UserHostAddress, request.UserAgent);

            #region OrderNote

            obj.Subject = model.Subject;
            obj.Body = model.Body;
            obj.AuditInfo = audit;
            obj.Private = model.Private;
            obj.Flags = model.Flags;
            obj.OrderStatus = model.OrderStatus;
            obj.Order = OrderMgr.GetDom(db, model.OrderId ?? 0);

            #endregion

            if (session != null) session.SaveOrUpdate(obj);
            else if (!SaveOrUpdate(db, new[] { obj }.ToList())) return false;
                     
            model.Id = obj.Id;
            new OrderNoteMgr().OnUpdate(obj);

            return true;
        }
        #endregion

        #region OnUpdate
        protected override void OnUpdate(OrderNote obj)
        {
        }
        #endregion

        #region OnDelete
        protected override void OnDelete(OrderNote obj)
        {
        }
        #endregion
    }
}