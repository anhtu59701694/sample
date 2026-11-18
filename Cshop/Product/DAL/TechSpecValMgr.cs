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
    public class TechSpecValMgr : BaseMgr<TechSpecValModel, TechSpecVal>
    {
        static TechSpecValMgr() { Init(typeof(TechSpecValMgr)); }

        #region GetAll

        public static List<TechSpecValModel> GetAll(IDB db, int? techSpecId, int pageIndex, int pageSize, out int totalRows, 
            SortDirection sortDirection = SortDirection.Descending, string sortField = "Id")
        {
            var args = new List<System.Tuple<string, string>>();
            var hSql = string.Format(@"
            select teVal from {0} as teVal
            where 1 = 1 ", typeof(TechSpecVal).FullName);

            if (techSpecId != null)
                hSql += " and teVal.TechSpec.Id = " + techSpecId;

            #region Sorting

            hSql += string.Format(" order by {0} {1}", "teVal." + sortField, GetDir(sortDirection));

            #endregion

            var ret = GetList<TechSpecVal>(db, hSql, pageIndex, pageSize, out totalRows, args.ToArray());

            return Mapper.Map<List<TechSpecVal>, List<TechSpecValModel>>(ret);
        }

        public static List<TechSpecValModel> GetAll(IDB db, int? techSpecId = null)
        {
            int total;
            return GetAll(db, techSpecId, 1, Int32.MaxValue, out total,
                SortDirection.Ascending, "Id");
        }

        #endregion

        #region SaveorUpdate
        public static bool SaveorUpdate(IDB db, TechSpecValModel model, bool autoCommit = true)
        {
            if (model == null) return false;

            var obj = GetDom(db, model.Id) ?? new TechSpecVal() { TechSpec = TechSpecMgr.GetDom(db,model.TechSpecId ?? 0) };

            obj.Value = model.Value;
            obj.Description = model.Description;
            obj.DisplayOrder = model.DisplayOrder;

            if (SaveOrUpdate(obj, db.GetSession(), autoCommit))
            {
                model.Id = obj.Id;
                new TechSpecValMgr().OnUpdate(obj); return true;
            }
            else return false;
        }
        #endregion

        #region OnUpdate
        protected override void OnUpdate(TechSpecVal obj)
        {
            ProductScope.Invalidate();
        }
        #endregion

        #region OnDelete
        protected override void OnDelete(TechSpecVal obj)
        {
            ProductScope.Invalidate();
        }
        #endregion
    }
}