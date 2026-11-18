using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using AutoMapper;
using Claw.Controls.Modules;
using Claw.Core;
using Claw.Modules.Eshop.Helper;
using NHibernate;
using System.Web.UI.WebControls;
using Claw.Core.DAL;
using Claw.Modules.Eshop.Controllers.Admin;
using Claw.Modules.Eshop.Domain;
using Claw.Modules.Eshop.Models;

namespace Claw.Modules.Eshop.DAL
{
    public class InvoiceMgr : BaseMgr<InvoiceModel, Invoice>
    {
        static InvoiceMgr() { Init(typeof(InvoiceMgr)); }

        #region GetAll

        public static List<InvoiceModel> GetAll(IDB db, int pageIndex, int pageSize, out int totalRows,
            SortDirection sortDirection = SortDirection.Descending, string sortField = "Id")
        {
            var hSql = string.Format(@"
            select invoice from {0} as invoice ", typeof(Invoice).FullName);

            hSql += string.Format(" order by {0} {1}", "invoice." + sortField, GetDir(sortDirection));

            var ret = GetList<Invoice>(db, hSql, pageIndex, pageSize, out totalRows);

            return Mapper.Map<List<Invoice>, List<InvoiceModel>>(ret);
        }

        public static List<InvoiceModel> GetAll(IDB db, int? countryid = null)
        {
            int total;
            return GetAll(db, 1, Int32.MaxValue, out total);
        }

        #endregion

        #region SaveorUpdate
        public static bool SaveorUpdate(IDB db, InvoiceModel model, ISession session = null)
        {
            if (model == null) return false;

            var obj = GetDom(db, model.Id, session) ?? new Invoice() { CreatedDate = DateTime.Now };

            obj.Data = model.Data;
            obj.Order = OrderMgr.GetDom(db, model.OrderId ?? 0, session);

            if (session != null) session.SaveOrUpdate(obj);
            else if (!obj.SaveorUpdate(db.DataService)) return false;

            model.Id = obj.Id;
            new InvoiceMgr().OnUpdate(obj);

            return true;
        }
        #endregion

        #region OnUpdate
        protected override void OnUpdate(Invoice obj)
        {
        }
        #endregion

        #region OnDelete
        protected override void OnDelete(Invoice obj)
        {
        }
        #endregion
    }
}