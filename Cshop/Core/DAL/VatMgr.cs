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
    public class VatMgr : BaseMgr<VatModel, Vat>
    {
        static VatMgr() { Init(typeof(VatMgr)); }

        #region GetAll

        public static List<VatModel> GetAll(IDB db, bool? active, int pageIndex, int pageSize, out int totalRows,
            SortDirection sortDirection = SortDirection.Descending, string sortField = "Id")
        {
            var hSql = string.Format(@"
            select vat from {0} as vat", typeof(Vat).FullName);

            if (active != null) hSql += " where vat.Active = " + (active.Value ? 1 : 0);

            switch (sortField)
            {
                case "ValTxt":
                    sortField = "Value";
                    break;
            }

            hSql += string.Format(" order by {0} {1}", "vat." + sortField, GetDir(sortDirection));

            var ret = GetList<Vat>(db, hSql, pageIndex, pageSize, out totalRows);
            return Mapper.Map<List<Vat>, List<VatModel>>(ret);
        }

        public static List<VatModel> GetAll(IDB db, bool? active = null)
        {
            int total;
            return GetAll(db, active, 1, Int32.MaxValue, out total,
                SortDirection.Ascending, "Value");
        }

        #endregion

        #region SaveorUpdate
        public static bool SaveorUpdate(IDB db, VatModel model)
        {
            if (model == null) return false;

            var obj = GetDom(db, model.Id);

            #region Vat

            if (obj != null)
            {
                obj.Name = model.Name;
                obj.Value = (decimal)model.Value;
                obj.TakeAway = model.TakeAway;
                obj.Active = model.Active;
            }
            else obj = new Vat()
            {
                Name = model.Name,
                Value = (decimal)model.Value,
                TakeAway = model.TakeAway,
                Active = model.Active,
            };

            #endregion

            if (obj.SaveorUpdate(db.DataService))
            {
                model.Id = obj.Id;
                new VatMgr().OnUpdate(obj);

                var settings = SettingsBiz.GetSettings();

                if (model.Default && settings.General.DefaultTaxId != obj.Id)
                {
                    settings = SettingsBiz.GetSettings(true);
                    settings.General.DefaultTaxId = obj.Id;
                    SettingMgr.Save(settings);
                }
                else if (!model.Default && settings.General.DefaultTaxId == obj.Id)
                {
                    settings = SettingsBiz.GetSettings(true);
                    settings.General.DefaultTaxId = 0;
                    SettingMgr.Save(settings);
                }

                return true;
            }
            else return false;
        }
        #endregion

        #region Update

        public static bool Update(IDB db, int id, bool? defVal = null, bool? takeAway = null)
        {
            if (defVal == null && takeAway == null) return false;

            var obj = GetDom(db, id);

            if (obj == null) return false;

            if (takeAway != null) obj.TakeAway = takeAway.Value;

            if (obj.SaveorUpdate(db.DataService))
            {
                new VatMgr().OnUpdate(obj);
                var settings = SettingsBiz.GetSettings();

                if (defVal == true && settings.General.DefaultTaxId != obj.Id)
                {
                    settings = SettingsBiz.GetSettings(true);
                    settings.General.DefaultTaxId = obj.Id;
                    SettingMgr.Save(settings);
                }
                else if (defVal != null && !defVal.Value && settings.General.DefaultTaxId == obj.Id)
                {
                    settings = SettingsBiz.GetSettings(true);
                    settings.General.DefaultTaxId = 0;
                    SettingMgr.Save(settings);
                }

                return true;
            }
            else return false;
        }

        #endregion

        #region OnUpdate
        protected override void OnUpdate(Vat obj)
        {
        }
        #endregion

        #region OnDelete
        protected override void OnDelete(Vat obj)
        {
        }
        #endregion
    }
}