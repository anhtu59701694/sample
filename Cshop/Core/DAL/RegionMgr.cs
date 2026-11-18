using System;
using System.Collections.Generic;
using System.Web.UI.WebControls;
using AutoMapper;
using Claw.Modules.Eshop.Models;
using Claw.Modules.Eshop.Domain;

namespace Claw.Modules.Eshop.DAL
{
    public class RegionMgr : BaseMgr<RegionModel, Region>
    {
        static RegionMgr() { Init(typeof(RegionMgr)); }

        #region GetAll

        public static List<RegionModel> GetAll(IDB db, int pageIndex, int pageSize, out int totalRows,
            SortDirection sortDirection = SortDirection.Descending, string sortField = "Id")
        {
            var hSql = string.Format(@"
            select region from {0} as region join region.Languages as lang", typeof(Region).FullName);

            hSql += string.Format(" order by {0} {1}", (sortField == "Name" ? "lang." : "region.") + sortField, GetDir(sortDirection));

            var ret = GetList<Region>(db, hSql, pageIndex, pageSize, out totalRows);

            return Mapper.Map<List<Region>, List<RegionModel>>(ret);
        }

        public static List<RegionModel> GetAll(IDB db)
        {
            int total;
            return GetAll(db, 1, Int32.MaxValue, out total,
                SortDirection.Ascending, "Name");
        }

        #endregion

        #region OnUpdate
        protected override void OnUpdate(Region obj)
        {
        }
        #endregion

        #region OnDelete
        protected override void OnDelete(Region obj)
        {
        }
        #endregion
    }
}