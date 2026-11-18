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
    public class LocationMgr : BaseMgr<LocationModel, Location>
    {
        static LocationMgr() { Init(typeof(LocationMgr)); }

        #region GetAll

        public static List<LocationModel> GetAll(IDB db, int pageIndex, int pageSize, out int totalRows,
            SortDirection sortDirection = SortDirection.Descending, string sortField = "Id")
        {
            var hSql = string.Format(@"
            select Location from {0} as Location", typeof(Location).FullName);

            switch (sortField)
            {
                case "Name":
                    sortField = "Name";
                    break;
            }

            hSql += string.Format(" order by {0} {1}", "Location." + sortField, GetDir(sortDirection));

            var ret = GetList<Location>(db, hSql, pageIndex, pageSize, out totalRows);

            return Mapper.Map<List<Location>, List<LocationModel>>(ret);
        }

        public static List<LocationModel> GetAll(IDB db)
        {
            int total;
            return GetAll(db, 1, Int32.MaxValue, out total,
                SortDirection.Ascending, "Name");
        }

        #endregion

        #region SaveorUpdate
        public static bool SaveorUpdate(IDB db, LocationModel model)
        {
            if (model == null) return false;

            var obj = GetDom(db, model.Id);

            #region Location

            if (obj == null)
            {
                if (GetAll(db).Any(x => x.Name.EqualsIgCase(model.Name))) return true;
                else obj = new Location() {Name = model.Name};
            }

            #endregion

            if (obj.SaveorUpdate(db.DataService))
            {
                model.Id = obj.Id;
                new LocationMgr().OnUpdate(obj);
                return true;
            }
            else return false;
        }
        #endregion

        #region OnUpdate
        protected override void OnUpdate(Location obj)
        {
        }
        #endregion

        #region OnDelete
        protected override void OnDelete(Location obj)
        {
        }
        #endregion
    }
}
