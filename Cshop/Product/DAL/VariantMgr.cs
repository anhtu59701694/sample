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
using Claw.Modules.Eshop.DAL;

namespace Claw.Modules.Eshop.DAL{internal class VariantMgr : Product.DAL.VariantMgr {}}

namespace Claw.Modules.Eshop.Product.DAL
{
    public class VariantMgr : BaseMgr<VariantModel, Variant>
    {
        static VariantMgr() { Init(typeof(VariantMgr)); }

        #region GetAll

        public static List<VariantModel> GetAll(IDB db, string langCode, int productid,  
            int pageIndex, int pageSize, out int totalRows,
            SortDirection sortDirection = SortDirection.Descending, string sortField = "Id",
            string exceptIds = null, int? minQty = null, bool? active = null, Predicate<VariantBase> fFilter = null)
        {
            var hSql = string.Format(@"
            select vari from {0} as vari
            where vari.Product.Id = '{1}' ", typeof(Variant).FullName, productid);

            if (!string.IsNullOrEmpty(exceptIds)) hSql += string.Format(" and vari.Id not in ({0})", exceptIds);

            if (minQty != null && (!(active ?? false) || !ProductMgr.GetDom(db, productid).SafeProp(m => m.AllowBackorder))) 
                hSql += " and vari.Quantity >= " + minQty;

            if (active != null) hSql += " and vari.Active = " + (active.Value ? 1 : 0);

            #region Sorting

            if (sortField == "DisplayOrder")
                hSql += string.Format(" order by vari.{0} {1}, vari.Id desc", sortField, GetDir(sortDirection));
            else
                hSql += string.Format(" order by {0} {1}", "vari." + sortField, GetDir(sortDirection));

            #endregion

            var ret = GetList<Variant>(db, hSql, fFilter != null ? 1 : pageIndex, fFilter != null ? int.MaxValue : pageSize, out totalRows);

            if (fFilter != null)
            {
                ret = ret.Where(m => fFilter(m)).ToList();
                totalRows = ret.Count;
                ret = ret.GetPage(pageIndex, pageSize);
            }

            return Mapper.Map<List<Variant>, List<VariantModel>>(ret);
        }

        #endregion

        #region SaveorUpdate
        public static bool SaveorUpdate(IDB db, VariantModel model, bool useTransaction = true)
        {
            var product = ProductMgr.GetDom(db, model == null ? 0 : model.ProductId);

            if (model == null || product == null) return false;

            var settings = SettingsBiz.GetSettings();
            var obj = GetDom(db, model.Id) ?? new Variant();

            #region Variant

            obj.Product = product;

            obj.SKU = model.SKU;
            obj.BarCode = model.BarCode;
            obj.Quantity = model.Quantity;
            obj.DisplayOrder = model.DisplayOrder;
            obj.Price = obj.Product.Vat.NormalizePrice(model.Price, model.PriceInc) ?? 0;
            obj.SalePrice = obj.Product.Vat.NormalizePrice(model.SalePrice, model.SalePriceInc) ?? 0;
            obj.CostPrice = obj.Product.Vat.NormalizePrice(model.CostPrice, model.CostPriceInc);
            obj.Weight = (decimal?)model.Weight;
            obj.Image = model.Image;
            obj.Active = model.Active ?? false;

            obj.Location = model.Location;

            obj.ProductStatus = StatusMgr.GetDom(db, model.ProductStatusId??0);
            obj.BackDate = ((DateTime?)model.BackDate).HasValue ? (DateTime?)model.BackDate : null;
            if (obj.Id == 0)
            {
                obj.Product.Variants.Add(obj);
                obj.Product.VariantNum = obj.Product.Variants.Count(x => !x.Deleted);
            }

            obj.Product.Quantity = obj.Product.Variants.Sum(x => x.Deleted ? 0 : x.Quantity);
            obj.Product.BackOrderQty = Math.Max(0, obj.Product.Variants.Sum(x => x.Deleted ? 0 : x.BackOrderQty));

            #endregion

            #region VariantPropVals

            if (model.Properties != null)
            {
                foreach (VariantPropValModel vprop in model.Properties)
                {
                    var vpid = vprop.Id;
                    var vpValId = vprop.PropValId;
                    var dprop = (vpid == 0 ? null : obj.VariantPropVals.FirstOrDefault(x => x.Id == vpid || x.PropVal.Id == vpValId));
                    if (dprop == null) obj.VariantPropVals.Add(dprop = new VariantPropVal() {Variant = obj});
                    dprop.PropVal = PropValMgr.GetDom(db, vprop.PropValId ?? 0);
                }

                obj.VariantPropVals.RemoveAll(x => model.Properties.All(w => w.Id != x.Id));

                if (obj.VariantPropVals.Count == 0) return false;
            }

            #endregion

            if (settings.Hidden.VariantMultiImage)
            {
                #region Languages

                if (model.Languages != null)
                {
                    foreach (VariantLangModel vlang in model.Languages)
                    {
                        var vlid = vlang.Id;
                        var dvlang = (vlid == 0 ? null : obj.Languages.FirstOrDefault(x => x.Id == vlid));
                        if (dvlang == null)
                            obj.Languages.Add(
                                dvlang = new VariantLang() {Variant = obj, LangCode = vlang.LangCode});

                        dvlang.Description = vlang.DescriptionV;
                    }

                    obj.Languages.RemoveAll(x => model.Languages.All(w => w.Id != x.Id));
                }

                #endregion

                #region Images

                if (model.Images != null)
                {
                    foreach (VariantImageModel vimg in model.Images)
                    {
                        var vlkey = vimg.FileName;
                        var dvimg = (vlkey.IsNullOrEmpty()
                            ? null
                            : obj.Images.FirstOrDefault(x => x.FileName.EqualsIgCase(vlkey)));
                        if (dvimg == null) obj.Images.Add(dvimg = new VariantImage() {Variant = obj});

                        dvimg.FileName = vimg.FileName;
                        dvimg.DisplayOrder = vimg.DisplayOrder;
                    }

                    obj.Images.RemoveAll(x => model.Images.All(w => !w.FileName.EqualsIgCase(x.FileName)));
                    obj.Image = obj.Images.OrderBy(x => x.DisplayOrder).ThenByDescending(x => x.Id)
                        .Select(x => x.FileName).FirstOrDefault();
                }

                #endregion
            }

            var ret = true;
            if (useTransaction) ret = obj.SaveorUpdate(db.DataService);
            else db.GetSession().SaveOrUpdate(obj);

            if (ret)
            {
                model.Id = obj.Id;
                new VariantMgr().OnUpdate(obj); return true;
            }
            else return false;
        } 
        #endregion

        #region OnUpdate
        protected override void OnUpdate(Variant obj)
        {
            ProductScope.Invalidate();
        }
        #endregion

        #region OnDelete
        protected override void OnDelete(Variant obj)
        {
            ProductScope.Invalidate();
        }
        #endregion
    }
}