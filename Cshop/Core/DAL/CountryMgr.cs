using System.Collections.Generic;
using System.Linq;
using System.Web;
using Claw.Controls.Modules;
using Claw.Core;
using Claw.Core.DAL;
using Claw.Modules.Eshop.Controllers.Admin;
using Claw.Modules.Eshop.Domain;
using Claw.Modules.Eshop.Helper;
using NHibernate;

namespace Claw.Modules.Eshop.DAL
{
    public partial class CountryMgr : BaseMgr
    {
        #region GetAll

        public static List<Country> GetAll(IDB db)
        {
            var hSql = string.Format(@"
            select country from {0} as country ", typeof(Country).FullName);

            int total;
            return GetList<Country>(db, hSql, 1, int.MaxValue, out total);
        }

        public static List<Country> GetAll(IDB db, int? id)
        {
            return Helper.DataCache<Helper.DefaultScope, List<Country>>.Get(string.Format("CountryMgr.GetAll:{0}", id),
                () => GetAll(db).OrderBy(x => x.Name).ToList(), 30);
        }

        #endregion

        #region GetDom
        public static Country GetDom(IDB db, int? id)
        {
            if (id <= 0) return null;

            return db.DataService.GetObject<Country>(id ?? 0);
        }

        public static Country Get(IDB db, string code)
        {
            if (code.IsNullOrEmpty()) return null;

            var obj = db.DataService.GetObject<Country>(0, "Code", code);

            return obj == null ? null : obj;
        }

        #endregion

        #region IsVatEnable

        public static bool IsVatEnable(string code)
        {
            return IsVatEnable(new DefaultDb(), null, code);
        }

        public static bool IsVatEnable(IDB db, int id)
        {
            return IsVatEnable(db, id, null);
        }

        public static bool IsVatEnable(IDB db, string code)
        {
            return IsVatEnable(db, null, code);
        }

        private static bool IsVatEnable(IDB db, int? id, string code)
        {
            if (id <= 0 && code.IsNullOrEmpty()) return true;

            var lst = Eshop.Helper.DataCache<Eshop.Helper.CombineScope, List<Eshop.Domain.VatCountry>>.Get(string.Format("CountryMgr.IsVatEnable"),
                () => db.DataService.GetList<Eshop.Domain.VatCountry>(string.Format("from {0}", typeof(Eshop.Domain.VatCountry).FullName)), 15);

            return lst.Count == 0 ||
                   lst.Any(x => x.Active && (x.Country.Id == id || x.Country.Code.EqualsIgCase(code)));
        }
        #endregion
    }
}