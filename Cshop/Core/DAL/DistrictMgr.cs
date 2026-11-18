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
    public class DistrictMgr : BaseMgr<DistrictModel, District>
    {
        static DistrictMgr() { Init(typeof(DistrictMgr)); }

        #region GetAll

        public static List<DistrictModel> GetAll(IDB db, int? cityid, int pageIndex, int pageSize, out int totalRows,
            SortDirection sortDirection = SortDirection.Descending, string sortField = "Id")
        {
            var hSql = string.Format(@"
            select district from {0} as district", typeof(District).FullName);

            if (cityid != null) hSql += " where district.City.Id = " + cityid;

            hSql += string.Format(" order by {0} {1}", "district." + sortField, GetDir(sortDirection));

            var ret = GetList<District>(db, hSql, pageIndex, pageSize, out totalRows);

            return Mapper.Map<List<District>, List<DistrictModel>>(ret);
        }

        public static List<DistrictModel> GetAll(IDB db, int? cityid=null)
        {
            int total;
            return GetAll(db, cityid, 1, Int32.MaxValue, out total,
                SortDirection.Ascending, "Name");
        }

        #endregion

        #region SaveorUpdate
        public static bool SaveorUpdate(IDB db, DistrictModel model)
        {
            if (model == null) return false;

            var obj = GetDom(db, model.Id);

            #region District

            if (obj != null)
            {
                obj.Name = model.Name;
            }
            else
                obj = new District()
                {
                    Name = model.Name,
                };

            obj.City = CityMgr.GetDom(db, model.CityId ?? 0);

            #endregion

            if (obj.SaveorUpdate(db.DataService))
            {
                model.Id = obj.Id;
                new DistrictMgr().OnUpdate(obj);

                return true;
            }
            else return false;
        }
        #endregion

        #region OnUpdate
        protected override void OnUpdate(District obj)
        {
        }
        #endregion

        #region OnDelete
        protected override void OnDelete(District obj)
        {
        }
        #endregion
    }
}