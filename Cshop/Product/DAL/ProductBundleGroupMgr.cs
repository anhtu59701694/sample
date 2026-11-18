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

namespace Claw.Modules.Eshop.DAL { internal class ProductBundleGroupMgr : Product.DAL.ProductBundleGroupMgr { } }

namespace Claw.Modules.Eshop.Product.DAL
{
    public class ProductBundleGroupMgr : BaseMgr<ProductBundleGroupModel, ProductBundleGroup>
    {
        static ProductBundleGroupMgr() { Init(typeof(ProductBundleGroupMgr)); }

        #region GetAll

        public static List<ProductBundleGroupModel> GetAll(IDB db, int? productId, int pageIndex, int pageSize, out int totalRows,
            SortDirection sortDirection = SortDirection.Descending, string sortField = "Id")
        {
            var args = new List<System.Tuple<string, string>>();
            var hSql = string.Format(@"
            select group from {0} as group
            where group.Product.Id = {1} ", typeof(ProductBundleGroup).FullName, productId);

            hSql += string.Format(" order by {0} {1}", ("group.") + sortField, GetDir(sortDirection));

            var ret = GetList<ProductBundleGroup>(db, hSql, pageIndex, pageSize, out totalRows, args.ToArray());

            ret.Action(m => m.CalculateStats(db));

            return Mapper.Map<List<ProductBundleGroup>, List<ProductBundleGroupModel>>(ret);
        }

        public static List<ProductBundleGroupModel> GetAll(IDB db, int? productId)
        {
            int total;
            return GetAll(db, productId, 1, Int32.MaxValue, out total, SortDirection.Ascending);
        }

        #endregion

        #region SaveorUpdate

        public static bool SaveorUpdate(IDB db, ProductBundleGroupModel model, bool useTransaction = true)
        {
            if (model == null) return false;

            var obj = GetDom(db, model.Id) ?? new ProductBundleGroup();

            obj.Name = model.Name;
            obj.ShowVariant = model.ShowVariant;

            obj.Product = ProductMgr.GetDom(db, model.ProductId ?? 0);

            if (useTransaction ? obj.SaveorUpdate(db.DataService) : SaveOrUpdate(obj, db.GetSession(), false))
            {
                model.Id = obj.Id;
                new ProductBundleGroupMgr().OnUpdate(obj); return true;
            }
            else return false;
        }

        #endregion

        #region OnUpdate
        protected override void OnUpdate(ProductBundleGroup obj)
        {
            ProductScope.Invalidate();
        }
        #endregion

        #region OnDelete
        protected override void OnDelete(ProductBundleGroup obj)
        {
            ProductScope.Invalidate();
        }
        #endregion
    }
}