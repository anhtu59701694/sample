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
    public class SupplierMgr : BaseMgr<SupplierModel, Supplier>
    {
        static SupplierMgr() { Init(typeof(SupplierMgr)); }

        #region GetAll

        public static List<SupplierModel> GetAll(IDB db, int pageIndex, int pageSize, out int totalRows,
            SortDirection sortDirection = SortDirection.Descending, string sortField = "Id", string kw = "")
        {
            var hSql = string.Format(@"select supp from {0} as supp", typeof(Supplier).FullName);

            if (!string.IsNullOrEmpty(kw))
            {
                kw = TranslateKWord(kw);
                hSql = string.Format("{0} where (supp.Name like '%{1}%' or supp.Information like '%{1}%')", hSql, kw);
            }

            hSql += string.Format(" order by {0} {1}", "supp." + sortField, GetDir(sortDirection));

            var ret = GetList<Supplier>(db, hSql, pageIndex, pageSize, out totalRows);

            return Mapper.Map<List<Supplier>, List<SupplierModel>>(ret);
        }

        public static List<SupplierModel> GetAll(IDB db)
        {
            int total;
            return GetAll(db, 1, Int32.MaxValue, out total,
                SortDirection.Ascending, "Name");
        }

        #endregion

        #region Resolve

        public static Supplier Resolve(IDB db, int? id, string name, bool useTransaction = true)
        {
            var obj = GetDom(db, id ?? 0);

            if (obj != null) return obj;

            if (name.IsNullOrEmpty()) return null;

            obj = db.DataService.GetObject<Supplier>(0, "Name", name);

            if (obj != null) return obj;

            obj = new Supplier() { Name = name, Active = true };

            if (useTransaction)
            {
                if (!obj.SaveorUpdate(db.DataService)) return null;
            }
            else db.GetSession().SaveOrUpdate(obj);

            return obj;
        }

        #endregion

        #region SaveorUpdate

        public static bool SaveorUpdate(IDB db, SupplierModel model)
        {
            if (model == null) return false;

            var obj = GetDom(db, model.Id);
            obj = obj ?? new Supplier();

            obj.Name = model.Name.Trim();
            obj.Information = model.Information;
            obj.Active = model.Active;

            if (model.Id > 0)
                return obj.Update(db.DataService);

            return db.DataService.Save(obj);
        }

        #endregion

        #region OnUpdate
        protected override void OnUpdate(Supplier obj)
        {
        }
        #endregion

        #region OnDelete
        protected override void OnDelete(Supplier obj)
        {
        }
        #endregion
    }
}
