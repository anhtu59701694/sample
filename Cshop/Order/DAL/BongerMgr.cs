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
    public class BongerMgr : BaseMgr<BongerModel, Bonger>
    {
        static BongerMgr() { Init(typeof(BongerMgr)); }

        #region GetAll

        public static List<BongerModel> GetAll(IDB db, int pageIndex, int pageSize, out int totalRows,
            SortDirection sortDirection = SortDirection.Descending, string sortField = "Id", string keyword = null, byte? status = null)
        {
            var args = new List<System.Tuple<string, string>>();
            var hSql = string.Format(@"
            select bong from {0} as bong
            where 1 = 1 ", typeof(Bonger).FullName);

            if (status != null)
                hSql += " and bong.Status = " + status;

            if (!keyword.IsNullOrEmpty())
            {
                hSql += " and (bong.Name like :keyword or bong.Email like :keyword or bong.Phone like :keyword) ";
                args.Add(new System.Tuple<string, string>("keyword", "%" + keyword + "%"));
            }

            hSql += string.Format(" order by {0} {1}", "bong." + sortField, GetDir(sortDirection));

            var ret = GetList<Bonger>(db, hSql, pageIndex, pageSize, out totalRows,args.ToArray());

            return Mapper.Map<List<Bonger>, List<BongerModel>>(ret);
        }

        #endregion

        #region SaveorUpdate
        public static bool SaveorUpdate(IDB db, BongerModel model, ISession session = null)
        {
            if (model == null) return false;

            var obj = GetDom(db, model.Id, session) ?? new Bonger() { CreatedDate = DateTime.Now };

            #region Bonger

            obj.Name = model.Name;
            obj.Email = model.Email;
            obj.Phone = model.Phone;
            obj.Address = model.Address;
            obj.Postnr = model.Postnr;
            obj.Posted = model.Posted;
            obj.Status = model.Status ?? 0;
            obj.ModifiedDate = DateTime.Now;

            if (!model.Notes.IsNullOrEmpty())
                obj.Notes = string.Format("{0}: {1}", new ODateTime(obj.ModifiedDate).TextF, model.Notes) 
                    + (obj.Notes.IsNullOrEmpty() ? "" : "\r\n") + (obj.Notes ?? "");

            obj.Customer = CustomerMgr.GetDom(db, model.CustomerId ?? 0, session);
            obj.Country = CountryMgr.GetDom(db, model.CountryId ?? 0);

            #endregion

            if (model.Id == 0 && model.Products != null)
            {
                foreach (var itm in model.Products.Where(m => !m.IsDataModelEmpty))
                {
                    BongerProduct ditm;
                    obj.BongerProducts.Add(ditm = new BongerProduct()
                    {
                        Bonger = obj,
                        Product = Product.DAL.ProductMgr.GetDom(db, itm.ProductId ?? 0),
                        Variant = Product.DAL.VariantMgr.GetDom(db, itm.VariantId ?? 0),
                        ProductName = itm.ProductName,
                        Quantity = itm.Quantity,
                    });
                    if (ditm.ProductName.IsNullOrEmpty()) ditm.ProductName = ditm.Product.GetName(ditm.Variant);
                }
            }

            if (session != null) session.SaveOrUpdate(obj);
            else if (!obj.SaveorUpdate(db.DataService)) return false;

            model.Id = obj.Id;
            new BongerMgr().OnUpdate(obj);

            return true;
        }
        #endregion

        #region OnUpdate
        protected override void OnUpdate(Bonger obj)
        {
        }
        #endregion

        #region OnDelete
        protected override void OnDelete(Bonger obj)
        {
        }
        #endregion
    }
}