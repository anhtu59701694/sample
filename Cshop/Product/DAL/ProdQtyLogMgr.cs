using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using AutoMapper;
using Claw.Controls.Modules;
using Claw.Core;
using NHibernate;
using System.Web.UI.WebControls;
using Claw.Core.DAL;
using Claw.Modules.Eshop.Controllers.Admin;
using Claw.Modules.Eshop.Domain;
using Claw.Modules.Eshop.Models;

namespace Claw.Modules.Eshop.DAL
{
    public class ProdQtyLogMgr : BaseMgr<ProdQtyLogModel, ProdQtyLog>
    {
        static ProdQtyLogMgr() { Init(typeof(ProdQtyLogMgr)); }

        #region GetAll

        public static List<ProdQtyLogModel> GetAll(IDB db, int? productid, int? variantid, 
            int pageIndex, int pageSize, out int totalRows,
            SortDirection sortDirection = SortDirection.Descending, string sortField = "Id", bool? serialNumber = null)
        {
            var args = new List<System.Tuple<string, string>>();
            var hSql = string.Format(@"
            select pqtylog from {0} as pqtylog left join pqtylog.Variant as vari
            where pqtylog.Product.Id = {1}
            and (pqtylog.Variant.Id is null or vari.Product is not null)", typeof(ProdQtyLog).FullName, productid ?? 0);

            if (variantid != null) hSql += " and pqtylog.Variant.Id = " + variantid;

            if (serialNumber != null)
            {
                args.Add(new System.Tuple<string, string>("serialNumber", "%" + StringTable.SerialNumbers + ":%"));
                if (serialNumber.Value) hSql += string.Format(@" and pqtylog.Reason in ({0}, {1}) and pqtylog.Note like :serialNumber ", ProdQtyLogReason.Ordered.Val(), ProdQtyLogReason.Bundled.Val());
                else hSql += string.Format(@" and (pqtylog.Reason not in ({0}, {1}) or pqtylog.Note not like :serialNumber) ", ProdQtyLogReason.Ordered.Val(), ProdQtyLogReason.Bundled.Val());
            }

            hSql += string.Format(" order by {0} {1}", ("pqtylog.") + sortField, GetDir(sortDirection));

            var ret = GetList<ProdQtyLog>(db, hSql, pageIndex, pageSize, out totalRows, args.ToArray());

            return Mapper.Map<List<ProdQtyLog>, List<ProdQtyLogModel>>(ret);
        }

        public static List<ProdQtyLog> GetAll(IDB db, int refId, ProdQtyLogReason reason)
        {
            return GetList<ProdQtyLog>(db, string.Format("from {0} where RefId = {1} and Reason = {2} ", typeof(ProdQtyLog).FullName, refId, reason.Val()));
        }

        #endregion

        #region SaveorUpdate

        public static bool SaveorUpdate(IDB db, ProdQtyLogModel model, bool autoSave)
        {
            if (model == null || !typeof (ProdQtyLogReason).IsEnumDefined(model.Reason ?? 0)) return false;

            var obj = GetDom(db, model.Id) ?? new ProdQtyLog() {CreatedDate = DateTime.Now};

            obj.Note = model.Note;
            obj.Quantity = model.Quantity ?? 0;
            obj.Reason = model.Reason ?? 0;
            obj.RefId = model.RefId;
            obj.RefIdFunc = model.RefIdFunc;
            obj.QtyPos = model.QtyPos;

            obj.Product = Product.DAL.ProductMgr.GetDom(db, model.ProductId ?? 0);
            obj.Variant = Product.DAL.VariantMgr.GetDom(db, model.VariantId ?? 0);

            obj.StockQty = obj.Variant != null 
                ? obj.Variant.SafeProp(m => m.Quantity > 0 ? m.Quantity : -1 * m.BackOrderQty) 
                : obj.Product.SafeProp(m => m.Quantity > 0 ? m.Quantity : -1 * m.BackOrderQty);
            obj.StockQty1 = obj.Variant != null ? obj.Variant.Quantity1 : obj.Product.Quantity1;
            obj.StockQty2 = obj.Variant != null ? obj.Variant.Quantity2 : obj.Product.Quantity2;

            if (autoSave)
            {
                if (obj.SaveorUpdate(db.DataService))
                {
                    model.Id = obj.Id;
                    new ProdQtyLogMgr().OnUpdate(obj);
                    return true;
                }
                else return false;
            }
            else
            {
                if (obj.Product.QtyLogs == null) obj.Product.QtyLogs = new List<ProdQtyLog>();

                obj.Product.QtyLogs.Add(obj);

                return true;
            }
        }

        #endregion

        #region OnUpdate
        protected override void OnUpdate(ProdQtyLog obj)
        {
        }
        #endregion

        #region OnDelete
        protected override void OnDelete(ProdQtyLog obj)
        {
        }
        #endregion
    }
}