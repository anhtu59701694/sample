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
    public class TechSpecMgr : BaseMgr<TechSpecModel, TechSpec>
    {
        static TechSpecMgr() { Init(typeof(TechSpecMgr)); }

        #region GetAll

        public static List<TechSpecModel> GetAll(IDB db, int pageIndex, int pageSize, out int totalRows, 
            SortDirection sortDirection = SortDirection.Descending, string sortField = "Id", string keyword = null)
        {
            var args = new List<System.Tuple<string, string>>();
            var hSql = string.Format(@"
            select tech from {0} as tech
            where 1 = 1 ", typeof(TechSpec).FullName);

            if (!string.IsNullOrEmpty(keyword))
            {
                hSql += string.Format(@" and tech.Name like :keyword ");
                args.Add(new System.Tuple<string, string>("keyword", "%" + keyword + "%"));
            }

            #region Sorting

            if (sortField == "DisplayOrder")
                hSql += string.Format(" order by tech.{0} {1}, tech.Id desc", sortField, GetDir(sortDirection));
            else
                hSql += string.Format(" order by {0} {1}", "tech." + sortField, GetDir(sortDirection));

            #endregion

            var ret = GetList<TechSpec>(db, hSql, pageIndex, pageSize, out totalRows, args.ToArray());

            return Mapper.Map<List<TechSpec>, List<TechSpecModel>>(ret);
        }

        public static List<TechSpecModel> GetAll(IDB db, bool? active = null)
        {
            int total;
            return GetAll(db, 1, Int32.MaxValue, out total,
                SortDirection.Ascending, "Name")
                .Where(x => active == null || x.Active == active)
                .OrderBy(x => x.DisplayOrder)
                .ThenBy(x => x.Id).ToList();
        }

        #endregion

        #region SaveorUpdate
        public static bool SaveorUpdate(IDB db, TechSpecModel model)
        {
            if (model == null) return false;

            var obj = GetDom(db, model.Id) ?? new TechSpec() { };

            obj.Name = model.Name;
            obj.Icon = model.Icon;
            obj.DisplayOrder = model.DisplayOrder;
            obj.Active = model.Active;
            obj.Featured = model.Featured;
            obj.Description = model.Description;

            if (obj.SaveorUpdate(db.DataService))
            {
                model.Id = obj.Id;
                new TechSpecMgr().OnUpdate(obj); return true;
            }
            else return false;
        }
        #endregion

        #region OnUpdate
        protected override void OnUpdate(TechSpec obj)
        {
            ProductScope.Invalidate();
        }
        #endregion

        #region OnDelete
        protected override void OnDelete(TechSpec obj)
        {
            ProductScope.Invalidate();
        }
        #endregion
    }
}