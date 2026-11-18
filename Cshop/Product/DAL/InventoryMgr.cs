using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using AutoMapper;
using System.Web.UI.WebControls;
using Claw.Modules.Eshop.Domain;
using Claw.Modules.Eshop.Models;

namespace Claw.Modules.Eshop.DAL
{
    public class InventoryMgr : BaseMgr<InventoryModel, Inventory>
    {
        #region InventoryMgr

        static InventoryMgr()
        {
            Init(typeof (InventoryMgr));

            Mapper.CreateMap<Inventory, InventoryModel>();
            Mapper.CreateMap<InventoryProduct, InventoryProductModel>()
                .ForMember(m => m.ProductSku, conf => conf.MapFrom(m => m.Product.SKU))
                .ForMember(m => m.VariantSku, conf => conf.MapFrom(m => m.Variant.SafeProp(x => x.SKU)))
                .ForMember(m => m.ProductName, conf => conf.MapFrom(m => m.Product.GetName(null)))
                .ForMember(m => m.VariantName, conf => conf.MapFrom(m => m.Variant.GetPropDesc()))
                .ForMember(m => m.ProductStatusTxt, conf => conf.MapFrom(m => m.ProductStatus.GetName()))
                .ForMember(m => m.OldQuantity, conf => conf.MapFrom(m => m.OldQuantity ?? m.Quantity));
        }

        #endregion

        #region GetAll

        public static List<InventoryModel> GetAll(IDB db, int pageIndex, int pageSize, out int totalRows,
            SortDirection sortDirection = SortDirection.Descending, string sortField = "Id", string keyword = null)
        {
            var hSql = string.Format(@"
            select inv from {0} as inv
            where 1 = 1 ", typeof(Inventory).FullName);

            if (!string.IsNullOrEmpty(keyword))
                hSql += string.Format(@" and inv.UserName like '%{0}%'", TranslateKWord(keyword));

            hSql += string.Format(" order by {0} {1}", "inv." + sortField, GetDir(sortDirection));

            var ret = GetList<Inventory>(db, hSql, pageIndex, pageSize, out totalRows);

            return Mapper.Map<List<Inventory>, List<InventoryModel>>(ret);
        }

        #endregion

        #region GetDetail

        public static List<InventoryProductModel> GetDetail(IDB db, int inventoryId, int pageIndex, int pageSize, out int totalRows,
            SortDirection sortDirection = SortDirection.Descending, string sortField = "Id")
        {
            var hSql = string.Format(@"
            select invpro from {0} as invpro
            where invpro.Inventory.Id = {1} ", typeof (InventoryProduct).FullName, inventoryId);

            hSql += string.Format(" order by {0} {1}", "invpro." + sortField, GetDir(sortDirection));

            var ret = GetList<InventoryProduct>(db, hSql, pageIndex, pageSize, out totalRows);

            return Mapper.Map<List<InventoryProduct>, List<InventoryProductModel>>(ret);
        }

        #endregion

        #region OnUpdate
        protected override void OnUpdate(Inventory obj)
        {
        }
        #endregion

        #region OnDelete
        protected override void OnDelete(Inventory obj)
        {
        }
        #endregion
    }
}