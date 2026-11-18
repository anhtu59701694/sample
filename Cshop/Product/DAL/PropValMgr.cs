using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Script.Serialization;
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
using Claw.Modules.Eshop.DAL;

namespace Claw.Modules.Eshop.DAL { internal class PropValMgr : Product.DAL.PropValMgr{} }

namespace Claw.Modules.Eshop.Product.DAL
{
    public class PropValMgr : BaseMgr<PropValModel, PropVal>
    {
        static PropValMgr() { Init(typeof(PropValMgr)); }

        #region GetAll

        public static List<PropValModel> GetAll(IDB db, string langCode, int? propertyid, bool? visible, int pageIndex, int pageSize, out int totalRows,
            SortDirection sortDirection = SortDirection.Descending, string sortField = "Id", string keyword = null)
        {
            var args = new List<System.Tuple<string, string>>();
            var hSql = string.Format(@"
            select pval from {0} as pval join pval.Languages as lang
            where lang.LangCode = '{1}' ", typeof(PropVal).FullName, langCode);

            if (visible != null) hSql += " and lang.Visible = " + (visible.Value ? 1 : 0);

            if (propertyid != null) hSql += " and pval.Property.Id = " + propertyid;

            if (!string.IsNullOrEmpty(keyword))
            {
                hSql += string.Format(@" and lang.Name like :keyword ");
                args.Add(new System.Tuple<string, string>("keyword", "%" + keyword + "%"));
            }

            #region Sorting

            if (sortField == "DisplayOrder")
                hSql += string.Format(" order by pval.{0} {1}, pval.Id desc", sortField, GetDir(sortDirection));
            else
                hSql += string.Format(" order by {0} {1}", (sortField == "Name" ? "lang." : "pval.") + sortField, GetDir(sortDirection));

            #endregion

            var ret = GetList<PropVal>(db, hSql, pageIndex, pageSize, out totalRows, args.ToArray());

            return Mapper.Map<List<PropVal>, List<PropValModel>>(ret);
        }

        public static List<PropValModel> GetAll(IDB db, string langCode, bool? active, bool? visible, params int[] variantIds)
        {
            if (variantIds.Length == 0) return new List<PropValModel>();

            var hSql = string.Format(@"
            select distinct pval from {0} as varPVal 
                join varPVal.PropVal as pval 
                join pval.Languages as lang 
            where lang.LangCode = '{1}' and varPVal.Variant.Id in ({2})",
            typeof(VariantPropVal).FullName, langCode, variantIds.ToString(","));

            if (visible != null) hSql += " and lang.Visible = " + (visible.Value ? 1 : 0);
            if (active != null) hSql += " and pval.Active = {0} " + (active.Value ? 1 : 0);

            var ret = GetList<PropVal>(db, hSql);

            return Mapper.Map<List<PropVal>, List<PropValModel>>(ret);
        }

        public static List<PropValModel> GetAll(IDB db)
        {
            int total;
            return GetAll(db, LanguageMgr.FocusLangCode, null, null, 1, Int32.MaxValue, out total,
                SortDirection.Ascending, "Id")
                .OrderBy(x => x.DisplayOrder)
                .ThenBy(x => x.Id).ToList();
        }

        #endregion

        #region SaveorUpdate
        public static bool SaveorUpdate(IDB db, PropValModel model, bool useTransaction = true)
        {
            if (model == null) return false;

            var obj = GetDom(db, model.Id);

            #region PropVal

            if (obj != null)
            {
                obj.DisplayOrder = model.DisplayOrder;
                obj.Active = model.Active ?? false;
                obj.Icon = model.Icon;
                obj.ModifiedDate = DateTime.Now;
            }
            else
                obj = new PropVal()
                {
                    DisplayOrder = model.DisplayOrder,
                    Active = model.Active ?? false,
                    Icon = model.Icon,
                    ModifiedDate = DateTime.Now,
                    CreatedDate = DateTime.Now
                };

            obj.Code = model.Code;

            obj.Code = obj.Id == 0 && Exists<PropVal>(x => x.Id, obj.Id, null, new KeyValuePair() {Key = "Code", Value = obj.Code})
                ? Unique(db, "Code", obj)
                : obj.Code;

            obj.Property = PropertyMgr.GetDom(db, model.PropertyId);

            #endregion

            #region Languages
            if (model.Languages != null)
            {
                foreach (var item in model.Languages)
                {
                    var lid = item.Id;
                    var ditm = (lid == 0 ? null : obj.Languages.FirstOrDefault(x => x.Id == lid));
                    if (ditm == null)
                        obj.Languages.Add(ditm = new PropValLang()
                        {
                            PropVal = obj, LangCode = item.LangCode,
                        });

                    ditm.Name = item.Name;
                    //ditm.Visible = item.Visible;
                }

                obj.Languages.RemoveAll(x => model.Languages.All(w => w.Id != x.Id));
            }
            #endregion

            if (useTransaction ? obj.SaveorUpdate(db.DataService) : SaveOrUpdate(obj, db.GetSession(), false))
            {
                model.Id = obj.Id;
                new PropValMgr().OnUpdate(obj); return true;
            }
            else return false;
        }
        #endregion

        #region OnUpdate
        protected override void OnUpdate(PropVal obj)
        {
            ProductScope.Invalidate();
        }
        #endregion

        #region OnDelete
        protected override void OnDelete(PropVal obj)
        {
            ProductScope.Invalidate();
        }
        #endregion
    }
}