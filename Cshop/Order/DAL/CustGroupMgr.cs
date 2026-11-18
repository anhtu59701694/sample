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
using Claw.Modules.Eshop.DAL.Interfaces;
using Claw.Modules.Eshop.Domain.Interfaces;

namespace Claw.Modules.Eshop.DAL
{
    public class CustGroupMgr : BaseMgr<CustGroupModel, CustGroup>, ICustGroupMgr
    {
        static CustGroupMgr() { Init(typeof(CustGroupMgr)); } 

        #region GetAll

        public static List<CustGroupModel> GetAll(IDB db, int pageIndex, int pageSize, out int totalRows,
            SortDirection sortDirection = SortDirection.Descending, string sortField = "Id", bool bindStat = false)
        {
            var hSql = string.Format(@"
            select grp from {0} as grp ", typeof(CustGroup).FullName);

            hSql += string.Format(" order by {0} {1}", "grp." + sortField, GetDir(sortDirection));

            var ret = GetList<CustGroup>(db, hSql, pageIndex, pageSize, out totalRows);

            if (bindStat)
            {
                CustomerMgr.BindStats(db, ret);
                Singleton<IDiscountMgr>.Default.BindStats(db, ret.Cast<ICustGroup>().ToList());
            }

            return Mapper.Map<List<CustGroup>, List<CustGroupModel>>(ret);
        }

        public static List<CustGroupModel> GetAll(IDB db)
        {
            int total;
            return GetAll(db, 1, Int32.MaxValue, out total, SortDirection.Ascending, "Name");
        }

        #endregion

        #region SaveorUpdate

        public static bool SaveorUpdate(IDB db, CustGroupModel model)
        {
            if (model == null) return false;

            var obj = GetDom(db, model.Id) ?? new CustGroup();

            obj.Name = model.Name;
            obj.Price4Login = model.Price4Login;
            obj.Discount = model.Discount;

            if (model.Id == 0) obj.CustomerNum = obj.DiscountNum = 0;

            #region Categories

            var oldKey = obj.Id == 0 ? "" : string.Format("DR={0},CatR={1}", (double?)obj.DiscountRate, obj.CategoryCustGroups.OrderBy(m => m.Category.Id).Select(m => m.Category.Id).ToString(";"));
            if (model.Categories != null || model.AnyCategory != null)
            {
                obj.AnyCategory = model.AnyCategory ?? false;
                obj.DiscountRate = (model.DiscountRate/100m).NullIf(m => m == 0m);

                if (model.Categories != null) 
                {   
                    foreach (var cate in model.Categories.Where(m=>!m.IsDataModelEmpty))
                    {
                        var cateid = cate.CategoryId ?? 0;
                        var dItmCate = obj.CategoryCustGroups.FirstOrDefault(x => x.Category.Id == cateid);
                        if (dItmCate == null) 
                            obj.CategoryCustGroups.Add(new CategoryCustGroup() { Group = obj, Category = CategoryMgr.GetDom(db, cateid) });
                    }

                    obj.CategoryCustGroups.RemoveAll(m => m.Id != 0 && model.Categories.All(z => z.IsDataModelEmpty || (z.CategoryId ?? 0) != m.Category.Id));
                }
                else obj.CategoryCustGroups.Clear();
            }

            var newKey = obj.Id == 0 ? "" : string.Format("DR={0},CatR={1}", (double?)obj.DiscountRate, obj.CategoryCustGroups.OrderBy(m => m.Category.Id).Select(m => m.Category.Id).ToString(";"));            
            
            #endregion

            if (obj.SaveorUpdate(db.DataService))
            {
                if (model.Id == 0) model.Customers = model.Discounts = 0;

                model.Id = obj.Id;
                if (!newKey.EqualsIgCase(oldKey)) CustomerScope.Invalidate();
                new CustGroupMgr().OnUpdate(obj);

                return true;
            }
            else return false;
        }

        #endregion

        #region OnUpdate
        protected override void OnUpdate(CustGroup obj)
        {
            CombineScope.Invalidate();
        }
        #endregion

        #region OnDelete
        protected override void OnDelete(CustGroup obj)
        {
            CombineScope.Invalidate();
        }
        #endregion

        #region ICustGroupMgr

        ICustGroup ICustGroupMgr.GetDom(IDB db, int id, ISession session)
        {
            return CustGroupMgr.GetDom(db, id, session);
        }

        List<ICustGroup> ICustGroupMgr.GetAll(IDB db)
        {
            return db.DataService.GetAll<CustGroup>().Cast<ICustGroup>().ToList();
        } 

        #endregion
    }
}