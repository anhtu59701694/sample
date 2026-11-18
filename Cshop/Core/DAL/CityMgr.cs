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
    public class CityMgr : BaseMgr<CityModel, City>
    {
        static CityMgr() { Init(typeof(CityMgr)); }

        #region GetAll

        public static List<CityModel> GetAll(IDB db, int? countryid, int pageIndex, int pageSize, out int totalRows,
            SortDirection sortDirection = SortDirection.Descending, string sortField = "Id")
        {
            var hSql = string.Format(@"
            select city from {0} as city join city.Languages as lang ", typeof(City).FullName);

            if (countryid != null) hSql += " where city.Country.Id = " + countryid;

            hSql += string.Format(" order by {0} {1}", (sortField == "Name" ? "lang." : "city.") + sortField, GetDir(sortDirection));

            var ret = GetList<City>(db, hSql, pageIndex, pageSize, out totalRows);

            return Mapper.Map<List<City>, List<CityModel>>(ret);
        }

        public static List<CityModel> GetAll(IDB db, int? countryid = null)
        {
            int total;
            return GetAll(db, countryid, 1, Int32.MaxValue, out total,
                SortDirection.Ascending, "Name");
        }

        #endregion

        #region SaveorUpdate
        public static bool SaveorUpdate(IDB db, CityModel model)
        {
            if (model == null) return false;

            var obj = GetDom(db, model.Id);

            #region City

            if (obj != null)
            {
                obj.Code = model.Code;
            }
            else
                obj = new City()
                {
                    Code = model.Code,
                };

            obj.Country = CountryMgr.GetDom(db, model.CountryId ?? 0);

            #endregion

            #region Languages
            if (model.Languages != null)
            {
                foreach (var item in model.Languages)
                {
                    var lid = item.Id;
                    var ditm = (lid == 0 ? null : obj.Languages.FirstOrDefault(x => x.Id == lid));
                    if (ditm == null)
                    {
                        obj.Languages.Add(new CityLang()
                        {
                            City = obj,
                            Name = item.Name,
                            LangCode = item.LangCode,
                        });
                    }
                    else ditm.Name = item.Name;
                }

                obj.Languages.RemoveAll(x => model.Languages.All(w => w.Id != x.Id));
            }
            #endregion

            if (obj.SaveorUpdate(db.DataService))
            {
                model.Id = obj.Id;
                new CityMgr().OnUpdate(obj);

                return true;
            }
            else return false;
        }
        #endregion

        #region OnUpdate
        protected override void OnUpdate(City obj)
        {
        }
        #endregion

        #region OnDelete
        protected override void OnDelete(City obj)
        {
        }
        #endregion
    }
}