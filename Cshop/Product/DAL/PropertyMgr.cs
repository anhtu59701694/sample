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
    public class PropertyMgr : BaseMgr<PropertyModel, Property>
    {
        static PropertyMgr() { Init(typeof(PropertyMgr)); }

        #region GetAll

        public static List<PropertyModel> GetAll(IDB db, string langCode, bool? visible, int pageIndex, int pageSize, out int totalRows, 
            SortDirection sortDirection = SortDirection.Descending, string sortField = "Id", string keyword = null)
        {
            var args = new List<System.Tuple<string, string>>();
            var hSql = string.Format(@"
            select prop from {0} as prop join prop.Languages as lang
            where lang.LangCode = '{1}' ", typeof(Property).FullName, langCode);

            if (visible != null) hSql += " and lang.Visible = " + (visible.Value ? 1 : 0);

            if (!string.IsNullOrEmpty(keyword))
            {
                hSql += string.Format(@" and lang.Name like :keyword ");
                args.Add(new System.Tuple<string, string>("keyword", "%" + keyword + "%"));
            }

            #region Sorting

            if (sortField == "DisplayOrder")
                hSql += string.Format(" order by prop.{0} {1}, prop.Id desc", sortField, GetDir(sortDirection));
            else
                hSql += string.Format(" order by {0} {1}", (sortField == "Name" ? "lang." : "prop.") + sortField, GetDir(sortDirection));

            #endregion

            var ret = GetList<Property>(db, hSql, pageIndex, pageSize, out totalRows, args.ToArray());

            return Mapper.Map<List<Property>, List<PropertyModel>>(ret);
        }

        public static List<PropertyModel> GetPropertyByName(IDB db, string variantName, string langCode)
        {
            var hSql = string.Format(@"
            select distinct prop from {0} as prop join prop.Languages as lang
            where lang.LangCode = '{1}' and  lang.Name='{2}'", typeof(Property).FullName, "nb-NO", variantName);
            var ret = GetList<Property>(db, hSql);
            return Mapper.Map<List<Property>, List<PropertyModel>>(ret);
        }

        public static List<PropertyModel> GetAll(IDB db, string langCode, bool? active, bool? visible, params int[] variantIds)
        {
            if (variantIds.Length == 0) return new List<PropertyModel>();

            var hSql = string.Format(@"
            select distinct prop from {0} as varPVal 
                join varPVal.PropVal as pval 
                join pval.Property as prop
                join prop.Languages as lang 
            where lang.LangCode = '{1}' and varPVal.Variant.Id in ({2})",
            typeof(VariantPropVal).FullName, langCode, variantIds.ToString(","));

            if (visible != null) hSql += " and lang.Visible = " + (visible.Value ? 1 : 0);
            if (active != null)
                hSql += string.Format(" and pval.Active = {0} and and prop.Active = {0} ", (active.Value ? 1 : 0));

            var ret = GetList<Property>(db, hSql);

            return Mapper.Map<List<Property>, List<PropertyModel>>(ret);
        }

        public static List<PropertyModel> GetAll(IDB db, bool? active = null)
        {
            int total;
            return GetAll(db, LanguageMgr.FocusLangCode, null, 1, Int32.MaxValue, out total,
                SortDirection.Ascending, "Id")
                .Where(x => active == null || x.Active == active)
                .OrderBy(x => x.DisplayOrder)
                .ThenBy(x => x.Id).ToList();
        }

        #endregion

        #region SaveorUpdate
        public static bool SaveorUpdate(IDB db, PropertyModel model)
        {
            if (model == null) return false;

            var obj = GetDom(db, model.Id);

            #region Property

            if (obj != null)
            {
                obj.Code = model.Code;
                obj.DisplayOrder = model.DisplayOrder;
                obj.Active = model.Active ?? false;
                obj.Icon = model.Icon;
                obj.ModifiedDate = DateTime.Now;
            }
            else
                obj = new Property()
                {
                    Code = model.Code,
                    DisplayOrder = model.DisplayOrder,
                    Active = model.Active ?? false,
                    Icon = model.Icon,
                    ModifiedDate = DateTime.Now,
                    CreatedDate = DateTime.Now
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
                        obj.Languages.Add(ditm = new PropertyLang()
                        {
                            Property = obj, LangCode = item.LangCode,
                        });

                    ditm.Name = item.Name;
                    ditm.Visible = item.Visible;
                }

                obj.Languages.RemoveAll(x => model.Languages.All(w => w.Id != x.Id));
            }
            #endregion
            try
            {
                obj.SaveorUpdate(db.DataService);
                return true;
            }
            catch (Exception ex)
            {
                return false;
                throw;
            }
            //if (db.DataService.SaveorUpdate(obj))
            //{
            //    model.Id = obj.Id;
            //    new PropertyMgr().OnUpdate(obj); return true;
            //}
            //else return false;
        }
        #endregion

        #region OnUpdate
        protected override void OnUpdate(Property obj)
        {
            ProductScope.Invalidate();
        }
        #endregion

        #region OnDelete
        protected override void OnDelete(Property obj)
        {
            ProductScope.Invalidate();
        }
        #endregion
    }
}