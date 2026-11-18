using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Web;
using System.Web.UI.WebControls;
using AutoMapper;
using Claw.Modules.Eshop.Domain;
using Claw.Modules.Eshop.Helper;
using Claw.Modules.Eshop.Models;
using Claw.Modules.Eshop.DAL;

namespace Claw.Modules.Eshop.DAL { internal class ManufacturerMgr : Product.DAL.ManufacturerMgr {} }

namespace Claw.Modules.Eshop.Product.DAL
{
    public class ManufacturerMgr : BaseMgr<ManufacturerModel, Manufacturer>
    {
        static ManufacturerMgr() { Init(typeof(ManufacturerMgr)); }

        #region GetAll

        public static List<ManufacturerModel> GetAll(IDB db, bool? active, string langCode,
            int pageIndex, int pageSize, out int totalRows,
            SortDirection sortDirection = SortDirection.Descending, string sortField = "Id", string keyword = null)
        {
            var ret = GetAllRaw(db, active, langCode, pageIndex, pageSize, out totalRows, sortDirection,
                sortField, keyword);

            return Mapper.Map<List<Manufacturer>, List<ManufacturerModel>>(ret);
        }

        public static List<Manufacturer> GetAllRaw(IDB db, bool? active, string langCode,
            int pageIndex, int pageSize, out int totalRows,
            SortDirection sortDirection = SortDirection.Descending, string sortField = "Id", string keyword = null)
        {
            var args = new List<System.Tuple<string, string>>();
            var hSql = string.Format(@"
            select manufacturer from {0} as manufacturer join manufacturer.Languages as lang
            where lang.LangCode = '{1}' ", typeof(Manufacturer).FullName, langCode);

            if (!string.IsNullOrEmpty(keyword))
            {
                hSql += string.Format(@" and lang.Name like :keyword ");
                args.Add(new System.Tuple<string, string>("keyword", "%" + keyword + "%"));
            }

            if (active != null) hSql += " and manufacturer.Active = " + (active.Value ? 1 : 0);


            hSql += string.Format(" order by {0} {1}", (sortField == "Name" ? "lang." : "manufacturer.") + sortField, GetDir(sortDirection));

            return GetList<Manufacturer>(db, hSql, pageIndex, pageSize, out totalRows, args.ToArray());
        }

        public static List<ManufacturerModel> GetAll(IDB db, int? id, bool? active = null)
        {
            int total;
            return GetAll(db, active, LanguageMgr.OptLangCode(id ?? 0), 1, Int32.MaxValue, out total,
                SortDirection.Ascending, "Name");
        }

        #endregion

        #region SaveorUpdate
        public static bool SaveorUpdate(IDB db, ManufacturerModel model)
        {
            if (model == null) return false;

            var obj = GetDom(db, model.Id);

            #region Manufacturer

            if (obj != null)
            {
                obj.Alias = model.Alias;
                obj.Active = model.Active;
                obj.DisplayOrder = model.DisplayOrder;
                obj.Logo = model.Logo;
                obj.Url = model.Url;
            }
            else obj = new Manufacturer()
            {
                Alias = model.Alias,
                Active = model.Active,
                DisplayOrder = model.DisplayOrder,
                Logo = model.Logo,
                Url = model.Url


            };

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
                        obj.Languages.Add(ditm = new ManufacturerLang()
                        {
                            Manufacturer = obj,
                            LangCode = item.LangCode,
                        });
                    }

                    ditm.Alias = item.Alias;
                    ditm.Name = item.Name;
                    ditm.Description = item.Description;
                    ditm.PageTitle = item.PageTitle;
                    ditm.MetaDesc = item.MetaDesc;
                    ditm.Visible = item.Visible;
                }

                obj.Languages.RemoveAll(x => model.Languages.All(w => w.Id != x.Id));
            }
            #endregion

            if (obj.SaveorUpdate(db.DataService))
            {
                model.Id = obj.Id;
                new ManufacturerMgr().OnUpdate(obj); return true;
            }
            else return false;
        }
        #endregion

        #region OnUpdate
        protected override void OnUpdate(Manufacturer obj)
        {
            CombineScope.Invalidate();
        }
        #endregion

        #region OnDelete
        protected override void OnDelete(Manufacturer obj)
        {
            CombineScope.Invalidate();
        }
        #endregion
    }
}