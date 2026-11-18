using AutoMapper;
using Claw.Controls.Modules;
using Claw.Core;
using Claw.Core.DAL;
using Claw.Core.Domain;
using Claw.Core.Logs;
using Claw.Modules.Eshop.Controllers.Admin;
using Claw.Modules.Eshop.DAL;
using Claw.Modules.Eshop.DAL.Interfaces;
using Claw.Modules.Eshop.Domain;
using Claw.Modules.Eshop.Domain.Interfaces;
using Claw.Modules.Eshop.Helper;
using Claw.Modules.Eshop.Models;
using Kendo.Mvc.Extensions;
using Kendo.Mvc.Infrastructure;
using NHibernate;
using NHibernate.Mapping;
using Ninject;
using NPOI.OpenXmlFormats.Spreadsheet;
using NPOI.Util;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Web;
using System.Web.Hosting;
using System.Web.Mvc;
using System.Web.Services.Description;
using System.Web.UI.WebControls;

namespace Claw.Modules.Eshop.DAL { internal class ProductMgr : Product.DAL.ProductMgr { } }

namespace Claw.Modules.Eshop.Product.DAL
{
    public class ProductMgr : BaseMgr<ProductModel, Domain.Product>
    {
        public static volatile Func<IDB, ProductBase, VariantBase, int, bool> FuncUpdateQuantity = null;
        public static volatile Func<IDB, ProductBase, VariantBase, int, ProdQtyLogModel, bool> FuncUpdateQuantity2 = null;
        public static volatile Func<IDB, string, string, List<System.Tuple<string, string>>, System.Tuple<string, string>> FuncHsqlList = null;
        public static volatile Func<ProductBase, Language, string> FuncGetCategoryAlias = null;

        static ProductMgr() { Init(typeof(ProductMgr)); }

        #region GetProductByCondition

        public static List<ProductModel> GetProductByCondition(IDB db, string langCode, int? categoryid, bool? visible,
          int pageIndex, int pageSize, out int totalRows, SortDirection sortDirection = SortDirection.Descending,
          string sortField = "Id", string keyword = null, string exceptIds = null, int? type = null,
          int? statusid = null, int? manufacturerId = null, bool? visibility = null, string name = null,
          string sku = null, bool? featured = null, bool? backorder = null, int? minQty = null, string exceptVariantIds = null,
          bool? emailMarketing = null, bool? prisguide = null, bool? prisjakt = null, int? techSpecValId = null, bool? recurring = null)
        {
            var ret = GetProductByCondition(db, langCode, categoryid, visible, pageIndex, pageSize, out totalRows, sortDirection,
                sortField, keyword, exceptIds, type, statusid, manufacturerId, visibility, name, sku, featured, backorder, true, minQty,
                exceptVariantIds, emailMarketing, prisguide, prisjakt, techSpecValId, recurring);

            return Mapper.Map<List<Domain.Product>, List<ProductModel>>(ret);
        }

        static protected List<Domain.Product> GetProductByCondition(IDB db, string langCode, int? categoryid, bool? visible,
         int pageIndex, int pageSize, out int totalRows, SortDirection sortDirection = SortDirection.Descending,
         string sortField = "Id", string keyword = null, string exceptIds = null, int? type = null,
         int? statusid = null, int? manufacturerId = null, bool? visibility = null, string name = null,
         string sku = null, bool? featured = null, bool? backorder = null, bool admin = true,
         int? minQty = null, string exceptVariantIds = null, bool? emailMarketing = null,
          bool? prisguide = null, bool? prisjakt = null, int? techSpecValId = null, bool? recurring = null)
        {
            var args = new List<System.Tuple<string, string>>();
            var hSql = string.Format(@"
            select distinct prod,lang.Name from {0} as prod join prod.Languages as lang ", typeof(Domain.Product).FullName);

            if (!admin && visibility == true)//2017.02.20
                hSql += " join prod.CategoryProducts as cateProd";
            else if (categoryid != null) hSql += " join prod.CategoryProducts as cateProd";
            //bool beSide = ClawContext.Current.RawUrl.ToLower().Contains("admin") ? true : false;
            //if (beSide)
            //    hSql += string.Format(@" where 1 = '{0}' ", 1);
            //else

            if (techSpecValId != null) hSql += " join prod.ProductTechSVals as prodTSVal ";

            hSql += string.Format(@" where lang.LangCode = '{0}' and prod.Quantity > 0 ", langCode);

            if (categoryid != null)
                if (categoryid >= 0 && (statusid ?? 0) >= 0) hSql += " and cateProd.Category.Id = " + categoryid;
                else
                {
                    var subcates = new List<Category>();
                    var cate = CategoryMgr.GetDom(db, Math.Abs(categoryid ?? 0));
                    cate.GetDescendants(subcates, db);
                    if (statusid < 0 || !admin) subcates.Add(cate);
                    if (subcates.Count == 0) subcates.Add(null);
                    var lstcateids = subcates
                        .Where(x => x == null || admin || ((x.Visibility & 1) == 1 && x.Visible))
                        .Select(x => x == null ? 0 : x.Id).ToList();
                    if (lstcateids.Count == 0) lstcateids.Add(0);
                    hSql += " and cateProd.Category.Id in (" + lstcateids.ToString(",") + ")";
                }

            if (visibility != null) hSql += " and prod.Visibility IN " + (visibility.Value ? "(1,3)" : "(0,2)");

            if (manufacturerId != null) hSql += " and prod.Manufacturer.Id = " + manufacturerId;

            if (visible != null) hSql += " and lang.Visible = " + (visible.Value ? 1 : 0);

            if (type != null) hSql += " and prod.Type = " + type;

            if (!string.IsNullOrEmpty(sku))
                hSql += string.Format(@" and prod.SKU like '%{0}%'", TranslateKWord(sku));

            if (!string.IsNullOrEmpty(name))
            {
                hSql += string.Format(@" and lang.Name like :name");
                args.Add(new System.Tuple<string, string>("name", "%" + name + "%"));
            }

            if (!string.IsNullOrEmpty(keyword))
            {
                hSql += string.Format(@" and (lang.Name like :keyword or prod.SKU like :keyword)");
                args.Add(new System.Tuple<string, string>("keyword", "%" + keyword + "%"));
            }

            #region exceptIds
            while (!string.IsNullOrEmpty(exceptIds))
            {
                if (!exceptVariantIds.IsNullOrEmpty())
                {
                    var excVars = GetList<Variant>(db, string.Format("from {0} as vari where vari.Id in ({1})",
                        typeof(Variant).FullName, exceptVariantIds));
                    var incIds = excVars.GroupBy(x => x.Product.Id,
                        (key, lst) =>
                        {
                            var items = lst.ToList();
                            return new { items[0].Product, Items = items };
                        })
                        .Where(x => x.Items.Count < x.Product.Variants.Count(m => m.Active && m.Quantity >= (minQty ?? 0)))
                        .Select(x => x.Product.Id.ToString()).ToList();

                    exceptIds = exceptIds.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                        .Where(x => !incIds.Contains(x)).ToString(",");

                    if (exceptIds.IsNullOrEmpty()) break;
                }
                hSql += string.Format(" and prod.Id not in ({0})", exceptIds);
                break;
            }
            #endregion

            if (statusid != null) hSql += " and prod.ProductStatus.Id = " + Math.Abs(statusid ?? 0);

            if (featured != null)
                if (featured.Value)
                    hSql += string.Format(" and prod.Flags >= {0} and (prod.Flags&{0}) <> 0 ", ProductFlags.Featured.Val());
                else hSql += string.Format(" and (prod.Flags&{0}) = 0 ", ProductFlags.Featured.Val());

            if (prisguide != null)
                if (prisguide.Value)
                    hSql += string.Format(" and prod.Flags > {1} and (prod.Flags&{0}) <> 0 ", ProductFlags.Prisguide.Val(), ProductFlags.Featured.Val());
                else hSql += string.Format(" and (prod.Flags&{0}) = 0 ", ProductFlags.Prisguide.Val());

            if (prisjakt != null)
                if (prisjakt.Value)
                    hSql += string.Format(" and prod.Flags > {1} and (prod.Flags&{0}) <> 0 ", ProductFlags.Prisjakt.Val(), ProductFlags.Featured.Val());
                else hSql += string.Format(" and (prod.Flags&{0}) = 0 ", ProductFlags.Prisjakt.Val());

            if (emailMarketing != null)
                if (emailMarketing.Value)
                    hSql += string.Format(" and prod.Flags >= {0} and (prod.Flags&{0}) <> 0 ", ProductFlags.EmailMarketing.Val());
                else hSql += string.Format(" and (prod.Flags&{0}) = 0 ", ProductFlags.EmailMarketing.Val());

            if (recurring != null)
                if (recurring.Value)
                    hSql += string.Format(" and prod.Flags >= {0} and (prod.Flags&{0}) <> 0 ", ProductFlags.RecurringOrder.Val());
                else hSql += string.Format(" and (prod.Flags&{0}) = 0 ", ProductFlags.RecurringOrder.Val());

            if (backorder != null)
                hSql += " and prod.BackOrderQty " + (backorder.Value ? " > 0" : " = 0");

            if (minQty != null) hSql += " and prod.Quantity >= " + minQty;

            if (!admin && visibility == true)
            {
                hSql += string.Format(" and prod.PublishedDate <= '{0:yyyy-MM-dd}' ", DateTime.Now);
                hSql += " and cateProd.Category.Visibility in (1,3) and cateProd.Category.VisibilityUpPath in (1,3) ";
                hSql += " and prod.ProductStatus.ShowFrontEnd = 1 ";
                var hidden = SettingsBiz.GetSettings().Hidden;
                if (hidden.ShowZeroQtyProduct)
                {
                    if (!hidden.HideZeroQtyProdStatuses.IsNullOrEmpty())
                        hSql += string.Format(" and (prod.Quantity > 0 or prod.ProductStatus.Id not in ({0}) )", hidden.HideZeroQtyProdStatuses);
                }
                else hSql += " and prod.Quantity > 0 ";
            }

            if (techSpecValId != null) hSql += " and prodTSVal.TechSpecVal.Id = " + techSpecValId;

            #region Sorting

            switch (sortField)
            {
                case "CreatedDate.Text":
                    sortField = "CreatedDate";
                    break;
                case "PublishedDate.Text":
                    sortField = "PublishedDate";
                    break;
                case "PriceInc":
                    sortField = "Price";
                    break;
                case "ProductStatusName":
                    sortField = "ProductStatus";
                    break;
            }

            //if (sortField == "DisplayOrder")
            //    hSql += string.Format(" order by prod.{0} {1}, prod.Id desc", sortField, GetDir(sortDirection));
            //else
            hSql += string.Format(" order by {0} {1}", (sortField == "Name" ? "lang." : "prod.") + sortField, GetDir(sortDirection));

            #endregion

            int nTries = 1;
        LB_LOOP:
            var ret = GetList<object[]>(db, hSql, pageIndex, pageSize, out totalRows, "select count(distinct prod) {0}", args.ToArray())
                .Select(x => x[0]).Cast<Domain.Product>().ToList();

            if (sortField.EqualsIgCase("TotalSales") && ret.ComputeTotalSales(db) > 0 && nTries-- > 0) goto LB_LOOP;

            return ret;
        }

        static protected List<Domain.Product> GetProductByCondition(IDB db, string langCode, int? categoryid, bool? visible, DateTime? startDate,
            DateTime? endDate,
           int pageIndex, int pageSize, out int totalRows, SortDirection sortDirection = SortDirection.Descending,
           string sortField = "Id", string keyword = null, string exceptIds = null, int? type = null,
           int? statusid = null, int? manufacturerId = null, bool? visibility = null, string name = null,
           string sku = null, bool? featured = null, bool? backorder = null, bool admin = true,
           int? minQty = null, string exceptVariantIds = null, bool? emailMarketing = null,
            bool? prisguide = null, bool? prisjakt = null, int? techSpecValId = null, bool? recurring = null)
        {
            var args = new List<System.Tuple<string, string>>();
            var hSql = string.Format(@"
            select distinct prod,lang.Name from {0} as prod join prod.Languages as lang ", typeof(Domain.Product).FullName);

            if (!admin && visibility == true)//2017.02.20
                hSql += " join prod.CategoryProducts as cateProd";
            else if (categoryid != null) hSql += " join prod.CategoryProducts as cateProd";
            //bool beSide = ClawContext.Current.RawUrl.ToLower().Contains("admin") ? true : false;
            //if (beSide)
            //    hSql += string.Format(@" where 1 = '{0}' ", 1);
            //else

            if (techSpecValId != null) hSql += " join prod.ProductTechSVals as prodTSVal ";

            hSql += string.Format(@" where lang.LangCode = '{0}' and prod.Quantity > 0 ", langCode);

            if (categoryid != null)
                if (categoryid >= 0 && (statusid ?? 0) >= 0) hSql += " and cateProd.Category.Id = " + categoryid;
                else
                {
                    var subcates = new List<Category>();
                    var cate = CategoryMgr.GetDom(db, Math.Abs(categoryid ?? 0));
                    cate.GetDescendants(subcates);
                    if (statusid < 0 || !admin) subcates.Add(cate);
                    if (subcates.Count == 0) subcates.Add(null);
                    var lstcateids = subcates
                        .Where(x => x == null || admin || ((x.Visibility & 1) == 1 && x.Visible))
                        .Select(x => x == null ? 0 : x.Id).ToList();
                    if (lstcateids.Count == 0) lstcateids.Add(0);
                    hSql += " and cateProd.Category.Id in (" + lstcateids.ToString(",") + ")";
                }

            if (visibility != null) hSql += " and prod.Visibility IN " + (visibility.Value ? "(1,3)" : "(0,2)");

            if (manufacturerId != null) hSql += " and prod.Manufacturer.Id = " + manufacturerId;

            if (visible != null) hSql += " and lang.Visible = " + (visible.Value ? 1 : 0);

            if (type != null) hSql += " and prod.Type = " + type;

            if (!string.IsNullOrEmpty(sku))
                hSql += string.Format(@" and prod.SKU like '%{0}%'", TranslateKWord(sku));

            if (!string.IsNullOrEmpty(name))
            {
                hSql += string.Format(@" and lang.Name like :name");
                args.Add(new System.Tuple<string, string>("name", "%" + name + "%"));
            }

            if (!string.IsNullOrEmpty(keyword))
            {
                hSql += string.Format(@" and (lang.Name like :keyword or prod.SKU like :keyword)");
                args.Add(new System.Tuple<string, string>("keyword", "%" + keyword + "%"));
            }

            #region exceptIds
            while (!string.IsNullOrEmpty(exceptIds))
            {
                if (!exceptVariantIds.IsNullOrEmpty())
                {
                    var excVars = GetList<Variant>(db, string.Format("from {0} as vari where vari.Id in ({1})",
                        typeof(Variant).FullName, exceptVariantIds));
                    var incIds = excVars.GroupBy(x => x.Product.Id,
                        (key, lst) =>
                        {
                            var items = lst.ToList();
                            return new { items[0].Product, Items = items };
                        })
                        .Where(x => x.Items.Count < x.Product.Variants.Count(m => m.Active && m.Quantity >= (minQty ?? 0)))
                        .Select(x => x.Product.Id.ToString()).ToList();

                    exceptIds = exceptIds.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                        .Where(x => !incIds.Contains(x)).ToString(",");

                    if (exceptIds.IsNullOrEmpty()) break;
                }
                hSql += string.Format(" and prod.Id not in ({0})", exceptIds);
                break;
            }
            #endregion

            if (statusid != null) hSql += " and prod.ProductStatus.Id = " + Math.Abs(statusid ?? 0);

            if (featured != null)
                if (featured.Value)
                    hSql += string.Format(" and prod.Flags >= {0} and (prod.Flags&{0}) <> 0 ", ProductFlags.Featured.Val());
                else hSql += string.Format(" and (prod.Flags&{0}) = 0 ", ProductFlags.Featured.Val());

            if (prisguide != null)
                if (prisguide.Value)
                    hSql += string.Format(" and prod.Flags > {1} and (prod.Flags&{0}) <> 0 ", ProductFlags.Prisguide.Val(), ProductFlags.Featured.Val());
                else hSql += string.Format(" and (prod.Flags&{0}) = 0 ", ProductFlags.Prisguide.Val());

            if (prisjakt != null)
                if (prisjakt.Value)
                    hSql += string.Format(" and prod.Flags > {1} and (prod.Flags&{0}) <> 0 ", ProductFlags.Prisjakt.Val(), ProductFlags.Featured.Val());
                else hSql += string.Format(" and (prod.Flags&{0}) = 0 ", ProductFlags.Prisjakt.Val());

            if (emailMarketing != null)
                if (emailMarketing.Value)
                    hSql += string.Format(" and prod.Flags >= {0} and (prod.Flags&{0}) <> 0 ", ProductFlags.EmailMarketing.Val());
                else hSql += string.Format(" and (prod.Flags&{0}) = 0 ", ProductFlags.EmailMarketing.Val());

            if (recurring != null)
                if (recurring.Value)
                    hSql += string.Format(" and prod.Flags >= {0} and (prod.Flags&{0}) <> 0 ", ProductFlags.RecurringOrder.Val());
                else hSql += string.Format(" and (prod.Flags&{0}) = 0 ", ProductFlags.RecurringOrder.Val());

            if (backorder != null)
                hSql += " and prod.BackOrderQty " + (backorder.Value ? " > 0" : " = 0");

            if (minQty != null) hSql += " and prod.Quantity >= " + minQty;

            if (!admin && visibility == true)
            {
                hSql += string.Format(" and prod.PublishedDate <= '{0:yyyy-MM-dd}' ", DateTime.Now);
                hSql += " and cateProd.Category.Visibility in (1,3) and cateProd.Category.VisibilityUpPath in (1,3) ";
                hSql += " and prod.ProductStatus.ShowFrontEnd = 1 ";
                var hidden = SettingsBiz.GetSettings().Hidden;
                if (hidden.ShowZeroQtyProduct)
                {
                    if (!hidden.HideZeroQtyProdStatuses.IsNullOrEmpty())
                        hSql += string.Format(" and (prod.Quantity > 0 or prod.ProductStatus.Id not in ({0}) )", hidden.HideZeroQtyProdStatuses);
                }
                else hSql += " and prod.Quantity > 0 ";
            }
            if (startDate != null)
            {
                hSql += string.Format(" and prod.PublishedDate >= '{0:yyyy-MM-dd}'", startDate.Value.Date);
            }
            if (endDate != null)
            {
                hSql += string.Format(" and prod.PublishedDate <= '{0:yyyy-MM-dd}'", endDate.Value.Date);
            }

            if (techSpecValId != null) hSql += " and prodTSVal.TechSpecVal.Id = " + techSpecValId;

            #region Sorting

            switch (sortField)
            {
                case "CreatedDate.Text":
                    sortField = "CreatedDate";
                    break;
                case "PublishedDate.Text":
                    sortField = "PublishedDate";
                    break;
                case "PriceInc":
                    sortField = "Price";
                    break;
                case "ProductStatusName":
                    sortField = "ProductStatus";
                    break;
            }

            //if (sortField == "DisplayOrder")
            //    hSql += string.Format(" order by prod.{0} {1}, prod.Id desc", sortField, GetDir(sortDirection));
            //else
            hSql += string.Format(" order by {0} {1}", (sortField == "Name" ? "lang." : "prod.") + sortField, GetDir(sortDirection));

            #endregion

            int nTries = 1;



        LB_LOOP:
            var ret = GetList<object[]>(db, hSql, pageIndex, pageSize, out totalRows, "select count(distinct prod) {0}", args.ToArray())
                .Select(x => x[0]).Cast<Domain.Product>().ToList();

            if (sortField.EqualsIgCase("TotalSales") && ret.ComputeTotalSales(db) > 0 && nTries-- > 0) goto LB_LOOP;

            return ret;
        }

        public static List<ProductModel> GetProductByCondition(IDB db, ODateTime startDate, ODateTime endDate, int? id, int? categoryid = null, bool? visible = null)
        {
            int total;
            return GetProductByCondition(db, startDate, endDate, LanguageMgr.OptLangCode(id ?? 0), categoryid, visible, 1, Int32.MaxValue, out total,
                SortDirection.Ascending);
        }
        public static List<ProductModel> GetProductByCondition(IDB db, ODateTime startDate,
            ODateTime endDate, string langCode, int? categoryid, bool? visible,
           int pageIndex, int pageSize, out int totalRows, SortDirection sortDirection = SortDirection.Descending,
           string sortField = "Id", string keyword = null, string exceptIds = null, int? type = null,
           int? statusid = null, int? manufacturerId = null, bool? visibility = null, string name = null,
           string sku = null, bool? featured = null, bool? backorder = null, int? minQty = null, string exceptVariantIds = null,
           bool? emailMarketing = null, bool? prisguide = null, bool? prisjakt = null, int? techSpecValId = null, bool? recurring = null)
        {
            var ret = GetProductByCondition(db, langCode, categoryid, visible, startDate, endDate, pageIndex, pageSize, out totalRows, sortDirection,
                sortField, keyword, exceptIds, type, statusid, manufacturerId, visibility, name, sku, featured, backorder, true, minQty,
                exceptVariantIds, emailMarketing, prisguide, prisjakt, techSpecValId, recurring);

            return Mapper.Map<List<Domain.Product>, List<ProductModel>>(ret);
        }
        
        #endregion

        #region GetAll
        /// <summary>
        /// categoryid{ positive : direct list, negative: indirect list}
        /// </summary>
        static protected List<System.Tuple<Domain.Product, Variant>> GetAllRaw(IDB db, string langCode, int? categoryid, bool? visible,
           int pageIndex, int pageSize, out int totalRows, SortDirection sortDirection = SortDirection.Descending,
           string sortField = "Id", string keyword = null, string exceptIds = null, int? type = null,
           int? statusid = null, int? manufacturerId = null, bool? visibility = null, string name = null,
           string sku = null, bool? featured = null, bool? backorder = null, bool admin = true,
           int? minQty = null, string exceptVariantIds = null, bool? emailMarketing = null,
           bool? prisguide = null, bool? prisjakt = null, int? techSpecValId = null, bool? recurring = null,
           decimal? priceFrom = null, decimal? priceTo = null, bool? visibilityPos = null, string barCode = null, bool showVariant = false,
           int? id = null, bool? selfDiscount = null, bool? priceCheck = null, bool? fbMarketing = null,
           Predicate<ProductBase> fFilter = null, bool? googleShopping = null, string location = null,
           bool notifyMe = false, bool? download = null, bool? cgroupDiscount = null, bool? hasQtyOnly = null, bool? noCateOnly = null, bool? searchVariSku = null)
        {
            showVariant = (showVariant && sortField != null) || (!barCode.IsNullOrEmpty() || !sku.IsNullOrEmpty());

            var args = new List<System.Tuple<string, string>>();
            var selVari = showVariant ? ",vari" : ",0";
            var hSql = string.Format(@"
            select {1} from {0} as prod join prod.Languages as lang ", typeof(Domain.Product).FullName,
                   sortField == null && noCateOnly == null
                   ? "prod.Id, 0, case when 0 = 0 then '00000000-0000-0000-0000-000000000000' else NEWID() end"
                   : "distinct prod " + selVari + ",lang.Name {0}");

            if (!admin && visibility == true)//2017.02.20
                hSql += " join prod.CategoryProducts as cateProd";
            else if (categoryid != null || noCateOnly != null)
                hSql += string.Format(" {0} join prod.CategoryProducts as cateProd", noCateOnly == true ? "left" : "");
            //bool beSide = ClawContext.Current.RawUrl.ToLower().Contains("admin") ? true : false;
            //if (beSide)
            //    hSql += string.Format(@" where 1 = '{0}' ", 1);
            //else

            if (techSpecValId != null) hSql += " join prod.ProductTechSVals as prodTSVal ";

            if (showVariant) hSql += " left join prod.Variants as vari ";

            hSql += string.Format(@" where lang.LangCode = '{0}' ", langCode);

            if (categoryid != null)
                if (categoryid >= 0 && (statusid ?? 0) >= 0) hSql += " and cateProd.Category.Id = " + categoryid;
                else
                {                                      
                    var subcates = new List<Category>();
                    var cate = CategoryMgr.GetDom(db, Math.Abs(categoryid ?? 0));
                    cate.GetDescendants(subcates);
                    if (statusid < 0 || !admin) subcates.Add(cate);
                    if (subcates.Count == 0) subcates.Add(null);
                    var lstcateids = subcates
                        .Where(x => x == null || admin || ((x.Visibility & 1) == 1 && x.Visible))
                        .Select(x => x == null ? 0 : x.Id).ToList();
                    if (lstcateids.Count == 0) lstcateids.Add(0);
                    hSql += " and cateProd.Category.Id in (" + lstcateids.ToString(",") + ")";
                }

            if (visibility != null)
            {
                var manualShippingSkus = SettingsBiz.GetSettings().Hidden.ManualShippingProductSkus;

                if (!manualShippingSkus.IsNullOrEmpty() && visibility == true && minQty > 0)
                    hSql += string.Format(" and (prod.Visibility IN (1,3) or prod.SKU in ({0}))", 
                        manualShippingSkus.Split(new[]{';'}, StringSplitOptions.RemoveEmptyEntries)
                        .Select(m => string.Format("'{0}'", m.Replace("'",""))).ToString(", "));
                else hSql += " and prod.Visibility IN " + (visibility.Value ? "(1,3)" : "(0,2)");
            }

            if (visibilityPos != null)
            {
                hSql += " and prod.Visibility IN " + (visibilityPos.Value ? "(2,3)" : "(0,1)");
                if (showVariant) hSql += string.Format(" and (vari is null or vari.Active  = {0})", visibilityPos.Value ? 1 : 0);
            }

            if (showVariant) hSql += " and (vari is null or vari.Deleted = 0)";

            if (selfDiscount != null) hSql += " and prod.SelfDiscActive = " + (selfDiscount.Value ? 1 : 0);
 
            if (priceCheck != null) hSql += " and prod.PriceCkActive = " + (priceCheck.Value ? 1 : 0);

            if (manufacturerId != null) hSql += " and prod.Manufacturer.Id = " + manufacturerId;

            if (visible != null) hSql += " and lang.Visible = " + (visible.Value ? 1 : 0);

            if (type != null) hSql += " and prod.Type = " + type;

            if (!string.IsNullOrEmpty(sku)) 
            {
                hSql += string.Format(@" and (prod.SKU like :sku " + (showVariant ? " or vari.SKU like :sku " : "") + ")");
                args.Add(new System.Tuple<string, string>("sku", sku.Trim()));
            }

            if (!string.IsNullOrEmpty(barCode))
            {
                barCode = barCode.Trim();
                var barLen = SettingsBiz.GetSettings().Display.BarCode.CharLength + 1;
                if (barCode.Length < barLen) barCode = barCode.PadLeft(barLen, '0');
                hSql += " and (prod.BarCode = :barCode " + (showVariant ? " or vari.BarCode = :barCode " : "") + ") ";
                args.Add(new System.Tuple<string, string>("barCode", barCode));
            }

            if (!string.IsNullOrEmpty(name))
            {
                hSql += string.Format(@" and lang.Name like :name");
                args.Add(new System.Tuple<string, string>("name", fFilter != null ? name : "%" + name + "%"));
            }

            if (!string.IsNullOrEmpty(location))
            {
                hSql += string.Format(@" and prod.Location like :location");
                args.Add(new System.Tuple<string, string>("location", "%" + location + "%"));
            }

            if (!string.IsNullOrEmpty(keyword))
            {
                hSql += " and (lang.Name like :keyword or prod.SKU like :keyword or prod.BarCode like :keyword "
                        + (showVariant
                            ? " or vari.BarCode like :keyword  or vari.SKU like :keyword "
                            : ((searchVariSku ?? false) ? string.Format(" or prod.Id in (select Product.Id from {0} where SKU like :keyword2 group by Product.Id) ", typeof(Domain.Variant).FullName) : ""))
                        + ")";

                args.Add(new System.Tuple<string, string>("keyword", fFilter != null ? keyword : "%" + keyword + "%"));
                if (!showVariant && (searchVariSku ?? false))
                    args.Add(new System.Tuple<string, string>("keyword2", fFilter != null ? keyword : keyword + "%"));
            }

            #region exceptIds
            while (!string.IsNullOrEmpty(exceptIds))
            {
                if (!exceptVariantIds.IsNullOrEmpty())
                {
                    var excVars = GetList<Variant>(db, string.Format("from {0} as vari where vari.Id in ({1})",
                        typeof(Variant).FullName, exceptVariantIds));
                    var incIds = excVars.GroupBy(x => x.Product.Id,
                        (key, lst) =>
                        {
                            var items = lst.ToList();
                            return new { items[0].Product, Items = items };
                        })
                        .Where(x => x.Items.Count < x.Product.Variants.Count(m => m.Active && m.Quantity >= (minQty ?? 0)))
                        .Select(x => x.Product.Id.ToString()).ToList();

                    exceptIds = exceptIds.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                        .Where(x => !incIds.Contains(x)).ToString(",");

                    if (exceptIds.IsNullOrEmpty()) break;
                }
                hSql += string.Format(" and prod.Id not in ({0})", exceptIds);
                break;
            }
            #endregion

            if (statusid != null) hSql += " and prod.ProductStatus.Id = " + Math.Abs(statusid ?? 0);

            if (id != null) hSql += " and prod.Id = " + id;

            if (featured != null)
                if (featured.Value)
                    hSql += string.Format(" and prod.Flags >= {0} and (prod.Flags&{0}) <> 0 ", ProductFlags.Featured.Val());
                else hSql += string.Format(" and (prod.Flags&{0}) = 0 ", ProductFlags.Featured.Val());

            if (prisguide != null)
                if (prisguide.Value)
                    hSql += string.Format(" and prod.Flags > {1} and (prod.Flags&{0}) <> 0 ", ProductFlags.Prisguide.Val(), ProductFlags.Featured.Val());
                else hSql += string.Format(" and (prod.Flags&{0}) = 0 ", ProductFlags.Prisguide.Val());

            if (prisjakt != null)
                if (prisjakt.Value)
                    hSql += string.Format(" and prod.Flags > {1} and (prod.Flags&{0}) <> 0 ", ProductFlags.Prisjakt.Val(), ProductFlags.Featured.Val());
                else hSql += string.Format(" and (prod.Flags&{0}) = 0 ", ProductFlags.Prisjakt.Val());

            if (fbMarketing != null)
                if (fbMarketing.Value)
                    hSql += string.Format(" and prod.Flags > {1} and (prod.Flags&{0}) <> 0 ", ProductFlags.FBMarketing.Val(), ProductFlags.Featured.Val());
                else hSql += string.Format(" and (prod.Flags&{0}) = 0 ", ProductFlags.FBMarketing.Val());

            if (googleShopping != null)
                if (googleShopping.Value)
                    hSql += string.Format(" and prod.Flags > {1} and (prod.Flags&{0}) <> 0 ", ProductFlags.GoogleShopping.Val(), ProductFlags.Featured.Val());
                else hSql += string.Format(" and (prod.Flags&{0}) = 0 ", ProductFlags.GoogleShopping.Val());

            if (emailMarketing != null)
                if (emailMarketing.Value)
                    hSql += string.Format(" and prod.Flags >= {0} and (prod.Flags&{0}) <> 0 ", ProductFlags.EmailMarketing.Val());
                else hSql += string.Format(" and (prod.Flags&{0}) = 0 ", ProductFlags.EmailMarketing.Val());

            if (cgroupDiscount != null)
                hSql += cgroupDiscount.Value
                    ? string.Format(" and prod.Flags >= {0} and (prod.Flags&{0}) <> 0 ", ProductFlags.CGroupDiscount.Val())
                    : string.Format(" and (prod.Flags&{0}) = 0 ", ProductFlags.CGroupDiscount.Val());

            if (notifyMe)
                hSql += " and prod.EmailMUsers > 0 ";

            if (download != null)
                hSql += " and prod.ShippingType " + (download.Value ? (" = " + ShippingType.Download.Val()) : (" <> " + ShippingType.Download.Val()));

            if (hasQtyOnly != null)
            {
                hSql += string.Format(" and prod.Quantity {0} 0 ", hasQtyOnly.Value ? ">" : "<=");
                if (showVariant) hSql += string.Format(" and (vari is null or vari.Quantity {0} 0)", hasQtyOnly.Value ? ">" : "<=");
            }

            if (noCateOnly != null)
                hSql += string.Format(" and cateProd.Id {0} null ", noCateOnly.Value ? "is" : "is not");

            if (recurring != null)
                if (recurring.Value)
                    hSql += string.Format(" and prod.Flags >= {0} and (prod.Flags&{0}) <> 0 ", ProductFlags.RecurringOrder.Val());
                else hSql += string.Format(" and (prod.Flags&{0}) = 0 ", ProductFlags.RecurringOrder.Val());

            if (backorder != null)
                hSql += " and prod.BackOrderQty " + (backorder.Value ? " > 0" : " = 0");

            if (minQty != null)
            {
                hSql += visibility == true
                    ? string.Format(" and (prod.Quantity >= {0} or prod.AllowBackorder = 1) ", minQty)
                    : string.Format(" and prod.Quantity >= {0} ", minQty);
            }

            if (!admin && visibility == true)
            {
                if (visibility == true)
                {
                    hSql += string.Format(" and prod.PublishedDate <= '{0:yyyy-MM-dd}' ", DateTime.Now);
                    hSql += " and cateProd.Category.Visibility in (1,3) and cateProd.Category.VisibilityUpPath in (1,3) ";
                    hSql += " and prod.ProductStatus.ShowFrontEnd = 1 ";
                    var hidden = SettingsBiz.GetSettings().Hidden;
                    if (hidden.ShowZeroQtyProduct)
                    {
                        if (!hidden.HideZeroQtyProdStatuses.IsNullOrEmpty())
                            hSql += string.Format(" and (prod.Quantity > 0 or prod.ProductStatus.Id not in ({0}) )", hidden.HideZeroQtyProdStatuses);
                    }
                    else hSql += " and prod.Quantity > 0 ";
                }
                //else hSql += " and cateProd.Category.Visibility in (2,3) and cateProd.Category.VisibilityUpPath in (2,3) ";
            }

            if (techSpecValId != null) hSql += " and prodTSVal.TechSpecVal.Id = " + techSpecValId;

            if (priceFrom != null) hSql += " and case when isnull(prod.SalePrice, 0) = 0.0 then prod.Price else prod.SalePrice end  * (1+prod.Vat.Value) >= " + priceFrom.Value.ToString(CultureInfo.InvariantCulture);

            if (priceTo != null) hSql += " and case when isnull(prod.SalePrice, 0) = 0.0 then prod.Price else prod.SalePrice end * (1+prod.Vat.Value) <= " + priceTo.Value.ToString(CultureInfo.InvariantCulture);

            #region Sorting

            switch (sortField)
            {
                case "CreatedDate.Text":
                    sortField = "CreatedDate";
                    break;
                case "PublishedDate.Text":
                    sortField = "PublishedDate";
                    break;
                case "PriceInc":
                    sortField = "Price";
                    break;
                case "ProductStatusName":
                    sortField = "ProductStatus";
                    break;
            }

            //if (sortField == "DisplayOrder")
            //    hSql += string.Format(" order by prod.{0} {1}, prod.Id desc", sortField, GetDir(sortDirection));
            //else
            if (sortField == null) hSql += " group by prod.Id order by NEWID() ";
            else if (sortField == "IsAccessory")
                hSql = string.Format(hSql, string.Format(", prod.Flags&{0}", ProductFlags.Accessory.Val())) 
                     + string.Format(" order by prod.Flags&{0} {1}, prod.Id desc", ProductFlags.Accessory.Val(), GetDir(sortDirection));
            else hSql = string.Format(hSql, "") + string.Format(" order by {0} {1}", (sortField == "Name" ? "lang." : "prod.") + sortField, GetDir(sortDirection));

            #endregion

            if (FuncHsqlList != null) hSql = FuncHsqlList(db, hSql, null, null)?.Item1;

            int nTries = 1;
        LB_LOOP:
            List<System.Tuple<Domain.Product, Variant>> ret;
            if (sortField == null && fFilter == null)
            {
                var rand = new Random();
                var ids = GetList<object[]>(db, hSql, pageIndex, pageSize, out totalRows, null, args.ToArray())
                .Select(x => x[0]).Cast<int>().ToList();
                ret = ids.Count == 0
                    ? new List<System.Tuple<Domain.Product, Variant>>() 
                    : GetList<Domain.Product>(db, ids, sortDirection, "Id")
                    .OrderBy(x => rand.NextDouble())
                    .Select(x => new System.Tuple<Domain.Product, Variant>(x, null)).ToList();
            }
            else { 
                ret = GetList<object[]>(db, hSql, fFilter != null ? 1 : pageIndex, fFilter != null ? int.MaxValue : pageSize, out totalRows, "select count(distinct prod.Id) " + (showVariant ? " + count(distinct vari.Id) - count(distinct vari.Product.Id)" : "") + " {0}", args.ToArray())
                        .Select(x => new System.Tuple<Domain.Product, Variant>((Domain.Product)x[0], showVariant ? (Variant)x[1] : null)).ToList();

                if (fFilter != null)
                {
                    ret = ret.Where(m => fFilter(m.Item1)).ToList();
                    totalRows = ret.Count;
                    ret = ret.GetPage(pageIndex, pageSize);
                }
            }

            if (sortField.EqualsIgCase("TotalSales") && ret.Select(x => x.Item1).ToList().ComputeTotalSales(db) > 0 && nTries-- > 0) goto LB_LOOP;

            return ret;
        }

        public static List<ProductModel> GetAll(IDB db, string langCode, int? categoryid, bool? visible,
            int pageIndex, int pageSize, out int totalRows, SortDirection sortDirection = SortDirection.Descending,
            string sortField = "Id", string keyword = null, string exceptIds = null, int? type = null,
            int? statusid = null, int? manufacturerId = null, bool? visibility = null, string name = null,
            string sku = null, bool? featured = null, bool? backorder = null, int? minQty = null, string exceptVariantIds = null,
            bool? emailMarketing = null, bool? prisguide = null, bool? prisjakt = null, int? techSpecValId = null, bool? recurring = null,
            bool? selfDiscount = null, string barCode = null, bool? priceCheck = null, bool? fbMarketing = null, bool admin = true,
            Predicate<ProductBase> fFilter = null, bool? googleShopping = null, bool bindShopping = false, string location = null,
            bool notifyMe = false, bool? download = null, bool? cgroupDiscount = null, bool? hasQtyOnly = null, bool? noCateOnly = null, bool? searchVariSku = null)
        {
            var ret = GetAllRaw(db, langCode, categoryid, visible, pageIndex, pageSize, out totalRows, sortDirection,
                sortField, keyword, exceptIds, type, statusid, manufacturerId, visibility, name, sku, featured, backorder, admin, minQty,
                exceptVariantIds, emailMarketing, prisguide, prisjakt, techSpecValId, recurring, selfDiscount: selfDiscount, barCode: barCode,
                priceCheck: priceCheck, fbMarketing: fbMarketing, fFilter: fFilter, googleShopping: googleShopping, location: location,
                notifyMe: notifyMe, download: download, cgroupDiscount: cgroupDiscount, hasQtyOnly: hasQtyOnly, noCateOnly: noCateOnly, searchVariSku: searchVariSku);

            if (bindShopping) ShoppingProductMgr.BindStats(db, ret.Select(x => x.Item1 as ProductBase).ToList());

            return Mapper.Map<List<Domain.Product>, List<ProductModel>>(ret.Select(x => x.Item1).ToList());
        }

        public static List<ProductModel> GetAll(IDB db, int? id, int? categoryid = null, bool? visible = null)
        {
            int total;
            return GetAll(db, LanguageMgr.OptLangCode(id ?? 0), categoryid, visible, 1, Int32.MaxValue, out total,
                SortDirection.Ascending, "Name");
        }

        #endregion

        #region ComputeTotalSales

        public static void ComputeTotalSales(IDB db, int? categoryid = null, int? statusid = null)
        {
            var hSql = string.Format(@"select prod from {0} as prod ", typeof(Domain.Product).FullName);

            if (categoryid != null) hSql += " join prod.CategoryProducts as cateProd";

            hSql += " where prod.TotalSales is null ";

            if (categoryid != null) hSql += " and cateProd.Category.Id = " + categoryid;

            if (statusid != null) hSql += " and prod.ProductStatus.Id = " + statusid;

            var ret = GetList<Domain.Product>(db, hSql);

            ret.ComputeTotalSales(db);
        }

        #endregion

        #region SaveorUpdate

        public static bool SaveorUpdate(IDB db, ProductModel model, bool isFrontEnd = false,
            HttpRequestBase request = null, bool useTransaction = true, bool importing = false)
        {
            if (model == null || model.Id == 1) return false;
            if (model.Type == ProductType.Bundle && model.Variants != null && model.Variants.Count > 0) return false;

            #region Fields

            var settings = SettingsBiz.GetSettings();
            var tmpVar = new Variant();
            var monFields = new[]
            {
                Expr(m => m.Price), Expr(m => m.SalePrice), Expr(m => m.CostPrice)
            }.ToFieldName();
            var monVarFields = new[]
            {
                Expr(tmpVar, m => m.Price), Expr(tmpVar, m => m.SalePrice),
                Expr(tmpVar, m => m.CostPrice), Expr(tmpVar, m => m.Quantity)
            }.ToFieldName();

            var obj = GetDom(db, model.Id) ?? new Domain.Product() { CreatedDate = DateTime.Now, Type = (byte)model.Type, Quantity = model.Quantity, Quantity1 = model.Quantity };
            var oldQty = obj.Quantity;
            var deltaQty = model.Quantity - obj.Quantity;
            var status = StatusMgr.GetDom(db, model.ProductStatusId ?? 0);
            var statusChange = status.GetId(0) != obj.ProductStatus.GetId(0);
            var logAction = LogActionType.Product | (obj.Id == 0 ? LogActionType.Add : LogActionType.Edit);
            var variChanges = new List<ActionLogMgr.ChangeInfo<VariantModel>>();
            var objChanges = new List<object>();

            #endregion

            if (obj.Variants.SafeCount() == 0) monFields.Add(Expr(m => m.Quantity).ToFieldName());

            var originVals = obj.Id == 0 ? null : obj.Extract(monFields);

            #region Product

            obj.Vat = VatMgr.GetDom(db, model.VatId ?? 0);
            obj.ProductStatus = status;

            //if (obj.ProductStatus.DescInstock && obj.BackOrderQty > 0 && deltaQty > 0)
            //{
            //    obj.Quantity += Math.Max(0, deltaQty - obj.BackOrderQty);
            //    obj.BackOrderQty = Math.Max(0, obj.BackOrderQty - deltaQty);
            //}
            //else obj.Quantity = model.Quantity;

            if (obj.Quantity < 0)
            {
                obj.BackOrderQty += -1*obj.Quantity;
                obj.Quantity = 0;
            }

            obj.Alias = model.Alias;
            //obj.Type = (byte) model.Type;
            obj.SKU = model.SKU;
            obj.BarCode = model.BarCode;
            obj.NvitCode = model.NvitCode;
            obj.Materials = model.Materials;
            obj.Price = obj.Vat.NormalizePrice(model.Price, model.PriceInc) ?? 0;
            obj.SalePrice = obj.Vat.NormalizePrice(model.SalePrice, model.SalePriceInc) ?? 0;
            obj.CostPrice = obj.Vat.NormalizePrice(model.CostPrice, model.CostPriceInc);
            obj.TechSpecCostExtra = model.TechSpecCostExtra;
            obj.DepositAmount = model.DepositAmount;
            obj.BundleDiscountRate = (model.BundleDiscProfit ? -1 : 1)*(model.BundleDiscountRate/100m);
            obj.Weight = model.Weight;
            obj.WUnit = model.WUnit;
            obj.ShippingType = model.ShippingType ?? 0;
            obj.ShippingCostExtra = (model.ShippingType ?? 0).InList(ShippingType.RequireShipping) ? model.ShippingCostExtra : null;
            obj.AttachFile = model.AttachFile;
            obj.VideoUrls = model.VideoUrls;
            obj.Location = model.Location;
            obj.MinQuantity = model.MinQuantity;
            //obj.Flags = model.Flags; no update
            obj.Flags = (obj.Flags & ~(int)(ProductFlags.ModelMask | ProductFlags.HideZeroQtyProd | ProductFlags.Price4LoginOnly | ProductFlags.HideSku | ProductFlags.CGroupDiscount | ProductFlags.SerialNumber | ProductFlags.BundleHideSubItems | ProductFlags.BundleNeedSelectAll | ProductFlags.BundleShowProdInStock | ProductFlags.BundleFetchDescFromProds | ProductFlags.Controlled | ProductFlags.Aassortment)) 
                | (model.EmailMarketing ? (int)ProductFlags.EmailMarketing : 0)
                | (model.HideZeroQtyProd ? (int)ProductFlags.HideZeroQtyProd : 0)
                | (model.Price4LoginOnly ? (int)ProductFlags.Price4LoginOnly : 0)
                | (model.VisibleSku ? 0 : (int)ProductFlags.HideSku)
                | (model.RegSerialNumber ? (int)ProductFlags.SerialNumber : 0)
                | (model.BundleHideSubItems ? (int)ProductFlags.BundleHideSubItems : 0)
                | (model.BundleNeedSelectAll ? (int)ProductFlags.BundleNeedSelectAll : 0)
                | (model.BundleShowProdInStock ? (int)ProductFlags.BundleShowProdInStock : 0)
                | (model.BundleFetchDescFromProducts ? (int)ProductFlags.BundleFetchDescFromProds : 0)
                | (model.Controlled ? (int)ProductFlags.Controlled : 0)
                | (model.IsAassortment && model.AassortmentThreshold != null ? (int)ProductFlags.Aassortment : 0);
            obj.HidePrice = model.HidePrice;
            obj.AassortmentThreshold = model.AassortmentThreshold;

            if (!importing)
            {
                obj.SelfDiscActive = model.SelfDiscActive;
                obj.SelfDiscMinQty = model.SelfDiscMinQty ?? 0;
                obj.SelfDiscValue = model.SelfDiscType ? (decimal) model.SelfDiscValue : -1*model.SelfDiscValue/100.0m;

                obj.PriceCkUrl = model.PriceCkUrl;
                obj.PriceCkActive = model.PriceCkActive;
                obj.PriceCkVariant = model.PriceCkVariant;
                obj.PriceCkMinVal = model.PriceCkMinVal;
                obj.PriceCkDateFrom = model.PriceCkDateFrom;
                obj.PriceCkDateTo = model.PriceCkDateTo;
            }

            if (model.RecurringOrder) obj.Flags |= (int) ProductFlags.RecurringOrder;
            else
            {
                obj.Flags = obj.Flags & (~(int)ProductFlags.RecurringOrder);
                model.RecurringDiscounts = null;
            }

            obj.AllowBackorder = model.AllowBackorder;
            obj.BackorrderMsg = model.BackorrderMsg;
            obj.NotifyQtyThreshold = model.NotifyQtyThreshold;
            obj.Visibility = (byte)model.Visibility;
            obj.ModifiedDate = DateTime.Now;
            obj.PublishedDate = ((DateTime?)model.PublishedDate) ?? DateTime.Now;
            obj.BackDate = ((DateTime?)model.BackDate).HasValue ? (DateTime?)model.BackDate : null;
            obj.DisplayOrder = model.DisplayOrder;
            obj.Supplier = SupplierMgr.Resolve(db, model.SupplierId, model.SupplierName, useTransaction);
            obj.SecondVat = VatMgr.GetDom(db, model.SecondVatId ?? 0);
            obj.Manufacturer = ManufacturerMgr.GetDom(db, model.ManufacturerId ?? 0);
            obj.OriginCountry = CountryMgr.GetDom(db, model.OriginCountryId ?? 0);

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
                        obj.Languages.Add(ditm = new ProductLang()
                        {
                            Product = obj,
                            LangCode = item.LangCode,
                        });
                    }

                    ditm.Alias = item.Alias;
                    ditm.Name = item.Name;
                    ditm.Tags = item.Tags;
                    ditm.Description = (item.Description ?? string.Empty).Replace("\r\n", "").Replace("\n", "");
                    ditm.ExtraInfo = item.ExtraInfo;
                    ditm.ExtraInfo2 = item.ExtraInfo2;
                    ditm.ExtraInfo3 = item.ExtraInfo3;
                    ditm.ExtraInfo4 = item.ExtraInfo4;
                    ditm.PageTitle = item.PageTitle;
                    ditm.MetaDesc = item.MetaDesc;
                    ditm.Visible = item.Visible;
                }

                obj.Languages.RemoveAll(x => model.Languages.All(w => w.Id != x.Id));
            }

            #endregion

            #region Variants

            var newVariants = new List<VariantBase>();
            if (model.Variants != null && !isFrontEnd)
            {
                foreach (var vari in model.Variants)
                {
                    #region Variant

                    var gpid = vari.Id;
                    var dVar = (gpid == 0 ? null : obj.Variants.FirstOrDefault(x => x.Id == gpid));
                    if (dVar == null)
                        obj.Variants.Add(dVar = new Variant() {Product = obj, Quantity = vari.Quantity, Quantity1 = vari.Quantity });

                    var oriVarDic = dVar.Id == 0 ? null : dVar.Extract(monVarFields);
                    var deltaVarQty = vari.Quantity - dVar.Quantity;
                    var varStatus = StatusMgr.GetDom(db, vari.ProductStatusId ?? 0);
                    var oriActive = dVar.Active;

                    dVar.SKU = vari.SKU;
                    dVar.BarCode = vari.BarCode;
                    //dVar.NvitCode = vari.NvitCode;
                    dVar.Price = obj.Vat.NormalizePrice(vari.Price, vari.PriceInc) ?? 0;
                    dVar.CostPrice = obj.Vat.NormalizePrice(vari.CostPrice, vari.CostPriceInc);
                    dVar.SalePrice = obj.Vat.NormalizePrice(vari.SalePrice, vari.SalePriceInc) ?? 0;
                    dVar.Weight = settings.Hidden.ManageVariantWeight ? vari.Weight : null;
                    dVar.Image = vari.Image;
                    dVar.Location = vari.Location;
                    dVar.Active = vari.Active ?? false;
                    dVar.DisplayOrder = vari.DisplayOrder;
                    dVar.ProductStatus = varStatus;
                    dVar.BackDate = ((DateTime?)vari.BackDate).HasValue ? (DateTime?)vari.BackDate : null;
                    dVar.PriceCkUrl = vari.PriceCkUrl;
                    dVar.PriceCkMinVal = obj.ExcVat(vari.PriceCkMinVal);
                    dVar.ISeq = vari.Iseq;

                    if (dVar.Quantity < 0)
                    {
                        dVar.BackOrderQty += -1 * dVar.Quantity;
                        dVar.Quantity = 0;
                    }

                    UpdateVariantStatus(db, dVar, dVar.Quantity);

                    if (!dVar.Active && dVar.Quantity > 0 && 0 != (obj.Flags & (int)ProductFlags.HideZeroQtyProd) &&
                        settings.Hidden.ProductQtyAutoWebVisibilty) dVar.Active = true;

                    if (dVar.Id != 0 && dVar.Active != oriActive) 
                        dVar.ProductBundles.Action(m => { m.Bundle.CalculateQty(); objChanges.Add(m.Bundle); });

                    if (dVar.Id == 0) newVariants.Add(dVar);

                    #region GetChanges

                    var logVarAct = LogActionType.Product | LogActionType.ProdVariant | (dVar.Id == 0 ? LogActionType.Add : LogActionType.Edit);
                    var finalVarDic = dVar.Id == 0 ? null : dVar.Extract(monVarFields);
                    var changes = oriVarDic.GetChanges(finalVarDic);
                    if (changes.Count != 0)
                        variChanges.Add(new ActionLogMgr.ChangeInfo<VariantModel>()
                        {
                            Action = logVarAct, Changes = changes, Id = dVar.Id, Model = vari,
                            LogTemplate = string.Format("Updated '{{0}}' of '{0}', former value: '{{1}}'", obj.GetName(dVar))
                        });

                    #endregion

                    #endregion

                    #region VariantPropVals

                    foreach (VariantPropValModel vprop in vari.Properties)
                    {
                        var vpid = vprop.Id;
                        var dprop = (vpid == 0 ? null : dVar.VariantPropVals.FirstOrDefault(x => x.Id == vpid));
                        if (dprop == null) dVar.VariantPropVals.Add(dprop = new VariantPropVal() { Variant = dVar });
                        dprop.PropVal = PropValMgr.GetDom(db, vprop.PropValId ?? 0);
                    }

                    var mVar = vari;
                    dVar.VariantPropVals.RemoveAll(x => mVar.Properties.All(w => w.Id != x.Id));

                    if (dVar.VariantPropVals.Count == 0) return false;

                    #endregion

                    if (settings.Hidden.VariantMultiImage)
                    {
                        #region Languages

                        foreach (VariantLangModel vlang in vari.Languages)
                        {
                            var vlid = vlang.Id;
                            var dvlang = (vlid == 0 ? null : dVar.Languages.FirstOrDefault(x => x.Id == vlid));
                            if (dvlang == null)
                                dVar.Languages.Add(
                                    dvlang = new VariantLang() { Variant = dVar, LangCode = vlang.LangCode });

                            dvlang.Description = vlang.DescriptionV;
                        }

                        dVar.Languages.RemoveAll(x => mVar.Languages.All(w => w.Id != x.Id));

                        #endregion

                        #region Images

                        foreach (VariantImageModel vimg in vari.Images)
                        {
                            var vlkey = vimg.FileName;
                            var dvimg = (vlkey.IsNullOrEmpty()
                                ? null
                                : dVar.Images.FirstOrDefault(x => x.FileName.EqualsIgCase(vlkey)));
                            if (dvimg == null) dVar.Images.Add(dvimg = new VariantImage() { Variant = dVar });

                            dvimg.FileName = vimg.FileName;
                            dvimg.DisplayOrder = vimg.DisplayOrder;
                        }

                        dVar.Images.RemoveAll(x => mVar.Images.All(w => !w.FileName.EqualsIgCase(x.FileName)));
                        dVar.Image = dVar.Images.OrderBy(x => x.DisplayOrder).ThenByDescending(x => x.Id)
                            .Select(x => x.FileName).FirstOrDefault();

                        #endregion
                    }
                }

                foreach (var itm in obj.Variants.Where(x => !x.Deleted && model.Variants.All(w => w.Id != x.Id)).ToList())
                {
                    if (itm.ProductBundles.Count != 0) continue;

                    itm.Deleted = true;
                    itm.ProductBundles.Action(m => { m.Bundle.CalculateQty(); objChanges.Add(m.Bundle); });

                    if (Singleton<IOrderMgr>.Default.AnyOrder(db, itm) || itm.BongerProducts.Any() || itm.ProductBundles.Any())
                    {
                        itm.SKU = itm.BarCode = null;
                    }
                    else
                    {
                        var tmpItm = itm;
                        obj.QtyLogs.RemoveAll(m => m.Variant != null && m.Variant.Id == tmpItm.Id);
                        obj.DiscountProducts.RemoveAll(m => tmpItm.DiscountProducts.Any(z => z.Id == m.Id));
                        obj.Variants.Remove(itm);
                    }
                }

                if (obj.Variants.Count != 0 && obj.Variants.All(x => x.Deleted || !x.Active))
                    obj.Visibility = 0;
            }

            obj.VariantNum = obj.Variants.Count(x => !x.Deleted);
            obj.PriceCkVariant &= obj.Variants.Count != 0;

            if (obj.VariantNum > 0)
            {
                obj.Quantity = obj.Variants.Sum(x => x.Deleted ? 0 : x.Quantity);
                obj.BackOrderQty = Math.Max(0, obj.Variants.Sum(x => x.Deleted ? 0 : x.BackOrderQty));
                obj.CalculateSalePrice(settings);
            }

            UpdateProductStatus(db, obj, obj.Quantity, obj.Variants.Any());

            if (0 != (obj.Flags & (int) ProductFlags.HideZeroQtyProd))
            {
                if (obj.Quantity <= 0 || (obj.VariantNum != 0 && obj.Variants.All(x => x.Deleted || !x.Active || x.Quantity <= 0))) 
                    obj.Visibility = (byte)(obj.Visibility & ~(byte)VisibilityType.Web);
                else if (settings.Hidden.ProductQtyAutoWebVisibilty 
                   && (obj.VariantNum == 0 || obj.Variants.Any(x => !x.Deleted && x.Active && x.Quantity > 0))) 
                   obj.Visibility |= (byte)VisibilityType.Web;
            }

            #endregion

            #region Images

            if (model.Images != null)
            {
                foreach (var image in model.Images)
                {
                    var gpid = image.Id;
                    var dImg = (gpid == 0 ? null : obj.Images.FirstOrDefault(x => x.Id == gpid));
                    if (dImg != null)
                    {
                        dImg.FileName = image.FileName;
                        dImg.DisplayOrder = image.DisplayOrder;
                    }
                    else
                        obj.Images.Add(dImg = new Domain.Image()
                        {
                            Product = obj,
                            FileName = image.FileName,
                            DisplayOrder = image.DisplayOrder,
                        });

                    #region PhotoLang

                    foreach (ImageLangModel photoLang in image.Languages)
                    {
                        var plid = photoLang.Id;
                        var dlang = (plid == 0 ? null : dImg.Languages.FirstOrDefault(x => x.Id == plid));
                        if (dlang != null) dlang.Title = photoLang.Title;
                        else
                            dImg.Languages.Add(new ImageLang()
                            {
                                Image = dImg,
                                Title = photoLang.Title,
                                LangCode = photoLang.LangCode,
                            });
                    }

                    var mImg = image;
                    dImg.Languages.RemoveAll(x => mImg.Languages.All(w => w.Id != x.Id));

                    #endregion
                }

                obj.Images.RemoveAll(x => model.Images.All(w => w.Id != x.Id));
                obj.Image = model.Image == null
                    ? obj.Images.FirstOrDefault()
                    : obj.Images.MatchOrFirst(m => m.FileName == model.Image.FileName);
            }

            #endregion

            #region Bundlings

            if (model.Bundlings != null)
            {
                foreach (var bundl in model.Bundlings)
                {
                    int gpid = bundl.Id, mProdId = bundl.ProductId ?? 0, mVariantId = bundl.VariantId ?? 0;
                    var dBund = (gpid == 0 ? null : obj.ProductBundles.FirstOrDefault(x => x.Id == gpid || (mProdId == x.Product.GetId(0) && mVariantId == x.Variant.GetId(0))));
                    if (dBund == null)
                        obj.ProductBundles.Add(dBund = new ProductBundle() { Bundle = obj });

                    dBund.Quantity = bundl.Quantity;
                    dBund.Product = GetDom(db, bundl.ProductId ?? 0);
                    dBund.Variant = VariantMgr.GetDom(db, bundl.VariantId ?? 0);
                }

                foreach (var itm in obj.ProductBundles.Where(x => model.Bundlings.All(w => w.Id != x.Id)).ToList())
                {
                    itm.Product.BundlingRefs.Remove(itm);
                    obj.ProductBundles.Remove(itm);
                }

                obj.CalculateQty(); obj.CalculatePriceWeight();
            }

            obj.BundleNum = obj.ProductBundles.Count;

            #endregion

            #region Categories

            if (model.CategoryIds != null)
            {
                var arrIds = model.CategoryIds.Trim().Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                var cates = arrIds.Convert(x => db.DataService.GetObject<Category>(int.Parse(x)));

                foreach (var cate in cates)
                {
                    var cateid = cate.Id;
                    var dItmCate = obj.CategoryProducts.FirstOrDefault(x => x.Category.Id == cateid);
                    if (dItmCate == null)
                        obj.CategoryProducts.Add(dItmCate = new CategoryProduct() { Product = obj, Category = cate });
                }

                obj.ClearStatistics();
                obj.CategoryProducts.RemoveAll(x => cates.All(c => c.Id != x.Category.Id));
            }
            else
            {
                if (statusChange) obj.ClearStatistics();
            }

            #endregion

            #region Accessories

            if (model.Accessories != null)
            {
                var tmpAccessories = obj.GetAccessories(true);
                foreach (var acce in model.Accessories)
                {
                    var acid = acce.Id;
                    var dAcce = (acid == 0 ? null : tmpAccessories.FirstOrDefault(x => x.Id == acid));
                    if (dAcce == null)
                        tmpAccessories.Add(dAcce = new ProductAccessory() { Product = obj });

                    if (dAcce.BundleGroup.SafeProp(z => z.Id) != (acce.BundleGroupId ?? 0))
                    {
                        dAcce.BundleGroup.ResetStats();
                        if (dAcce.BundleGroup != null) objChanges.Add(dAcce.BundleGroup);
                        dAcce.BundleGroup = ProductBundleGroupMgr.GetDom(db, acce.BundleGroupId ?? 0);
                        dAcce.BundleGroup.ResetStats();
                    }

                    if (dAcce.Id >= 0)
                    {
                        dAcce.Accessory = GetDom(db, acce.AccessoryId ?? 0);
                        dAcce.AcceVariant = Eshop.DAL.VariantMgr.GetDom(db, acce.AcceVariantId ?? 0);
                    }
                }

                foreach (var acce in tmpAccessories
                    .Where(x => model.Accessories.All(w => w.Id != x.Id)).ToList())
                {
                    var accId = acce.Id;
                    tmpAccessories.Remove(acce);
                    if (accId != 0)
                    {
                        if (accId > 0)
                        {
                            obj.Accessories.Remove(acce);
                            acce.BundleGroup.ResetStats();
                        }
                        else obj.AccessoryRefs.RemoveAll(x => x.Id == Math.Abs(accId));
                    }
                }

                obj.Accessories.AddRange(tmpAccessories.Where(x => x.Id == 0));
            }

            #endregion

            #region SerialNumbers

            if (model.SerialNumbers != null && settings.Hidden.ProducSerialNumber)
            {
                foreach (var item in model.SerialNumbers)
                {
                    var lid = item.Id;
                    var lval = item.SerialNumber;
                    var dItm = (lid == 0 && lval.IsNullOrEmpty() ? null : obj.ProductSerials.FirstOrDefault(x => (x.Id == lid && lid > 0) || x.SerialNumber.EqualsIgCase(lval)));
                    if (dItm == null) obj.ProductSerials.Add(dItm = new ProductSerial()
                    {
                        Product = obj, SerialNumber = item.SerialNumber, Sold = false
                    });
                }

                obj.ProductSerials.RemoveAll(x => x.Id > 0 && model.SerialNumbers.All(w => !w.SerialNumber.EqualsIgCase(x.SerialNumber)));
            }

            #endregion

            #region ExtraInformation

            if (model.ExtraInformation != null)
            {
                //model.ExtraInformation.Where(p => p.Id < 0).ToList().ForEach(p => p.Id = 0);
                foreach (var item in model.ExtraInformation)
                {
                    item.Id = item.Id < 0 ? 0 : item.Id;
                    var lid = item.Id;
                    var ditm = (lid == 0 ? null : obj.ExtraInformation.FirstOrDefault(x => x.Id == lid));
                    if (ditm == null)
                    {
                        obj.ExtraInformation.Add(ditm = new ProductExtraInformation() { Product = obj });
                    }

                    ditm.FieldName = item.FieldName;
                    ditm.FieldValue = item.FieldValue;
                    ditm.ProductId = item.ProductId;
                }
                obj.ExtraInformation.RemoveAll(x => model.ExtraInformation.All(w => w.Id != x.Id));
            }

            #endregion

            #region TechSVals

            if (model.TechSVals != null)
            {
                foreach (var item in model.TechSVals)
                {
                    var lid = item.TechSpecValId;
                    var ditm = (lid == 0 ? null : obj.ProductTechSVals.FirstOrDefault(x => x.TechSpecVal.Id == lid));
                    if (ditm == null)
                        obj.ProductTechSVals.Add(ditm = new ProductTechSVal() { Product = obj, TechSpecVal = TechSpecValMgr.GetDom(db, item.TechSpecValId ?? 0) });
                }

                obj.ProductTechSVals.RemoveAll(x => model.TechSVals.All(w => w.TechSpecValId != x.TechSpecVal.Id));
            }

            #endregion

            #region RecurringDiscounts

            List<IPaymentMethod> payments;
            if (model.RecurringDiscounts != null && settings.Hidden.RecurringOrder &&
                (payments = Singleton<IPaymentMethodMgr>.Default.GetAll(db, RecurringOrderMode.SpecificProducts)).SafeCount() != 0)
            {
                var paymentIds = payments.Select(x => x.Id).ToList();
                foreach (var item in model.RecurringDiscounts.Where(x => !x.IsDataModelEmpty))
                {
                    int? lid = item.Type, lidRE = item.RecurEach;
                    var ditm = (lid == 0 ? null : obj.ProductRecurDiscs.FirstOrDefault(x => x.Type == lid && x.RecurEach == lidRE));
                    if (ditm == null)
                        obj.ProductRecurDiscs.Add(ditm = new ProductRecurDisc() { Product = obj, Type = item.Type ?? 0, RecurEach = item.RecurEach ?? 0 });

                    var oVal = ditm.Value;
                    ditm.Value = item.Value / 100m;
                    ditm.DefOpt = item.DefOpt;
                    ditm.Discount = Singleton<IDiscountMgr>.Default.GetDom(db, item.DiscountId ?? 0);
                    if (ditm.Discount == null || ditm.Discount.BonusValue != ditm.Value)
                        ditm.Discount = Singleton<IDiscountMgr>.Default.SaveOrUpdate(db, paymentIds,
                            (RecurringType)ditm.Type, ditm.Value, oVal, obj.Id);
                }

                foreach (var itm in obj.ProductRecurDiscs.Where(x => (x.Value == 0 && !x.DefOpt) || model.RecurringDiscounts.All(w => w.Type != x.Type || w.RecurEach != x.RecurEach || w.IsDataModelEmpty)).ToList())
                {
                    itm.DefOpt = false;

                    if (itm.Discount != null)
                        Singleton<IDiscountMgr>.Default.SaveOrUpdate(db, paymentIds, (RecurringType)itm.Type, 0, itm.Value, itm.Product.Id);

                    obj.ProductRecurDiscs.Remove(itm);
                }
            }

            #endregion

            #region CGroupDiscounts

            if (model.CGroupDiscounts != null && settings.Hidden.EnableCustomerDiscount)
            {
                Func<bool, decimal, decimal> fRate = (type, rate) => type ? rate : rate/-100.0m;
                bool cgd0 = false, cgd1 = false, cgd2 = false, cgd3 = false, cgd4 = false;
                foreach (var item in model.CGroupDiscounts.Where(x => !x.IsDataModelEmpty))
                {
                    if (item.GroupId == obj.CGroupDisc0Id) { obj.CGroupDisc0Rate = fRate(item.Type, item.Rate); cgd0 = true; }
                    else if (item.GroupId == obj.CGroupDisc1Id) { obj.CGroupDisc1Rate = fRate(item.Type, item.Rate); cgd1 = true; }
                    else if (item.GroupId == obj.CGroupDisc2Id) { obj.CGroupDisc2Rate = fRate(item.Type, item.Rate); cgd2 = true; }
                    else if (item.GroupId == obj.CGroupDisc3Id) { /*obj.CGroupDisc3Rate = fRate(item.Type, item.Rate);*/ cgd3 = true; }
                    else if (item.GroupId == obj.CGroupDisc4Id) { obj.CGroupDisc4Rate = fRate(item.Type, item.Rate); cgd4 = true; }
                    else
                    {
                        if (obj.CGroupDisc0Id == null) { obj.CGroupDisc0Id = item.GroupId; obj.CGroupDisc0Rate = fRate(item.Type, item.Rate); cgd0 = true; }
                        else if (obj.CGroupDisc1Id == null) { obj.CGroupDisc1Id = item.GroupId; obj.CGroupDisc1Rate = fRate(item.Type, item.Rate); cgd1 = true; }
                        else if (obj.CGroupDisc2Id == null) { obj.CGroupDisc2Id = item.GroupId; obj.CGroupDisc2Rate = fRate(item.Type, item.Rate); cgd2 = true; }
                        //else if (obj.CGroupDisc3Id == null) { obj.CGroupDisc3Id = item.GroupId; obj.CGroupDisc3Rate = fRate(item.Type, item.Rate); cgd3 = true; }  
                        else if (obj.CGroupDisc4Id == null) { obj.CGroupDisc4Id = item.GroupId; obj.CGroupDisc4Rate = fRate(item.Type, item.Rate); cgd4 = true; }
                    }
                }

                if (!cgd0) { obj.CGroupDisc0Id = null; obj.CGroupDisc0Rate = null; } 
                if (!cgd1) { obj.CGroupDisc1Id = null; obj.CGroupDisc1Rate = null; } 
                if (!cgd2) { obj.CGroupDisc2Id = null; obj.CGroupDisc2Rate = null; }
                //if (!cgd3) { obj.CGroupDisc3Id = null; obj.CGroupDisc3Rate = null; }
                if (!cgd4) { obj.CGroupDisc4Id = null; obj.CGroupDisc4Rate = null; }

                obj.UpdateCGroupDiscountFlag();
            }

            #endregion

            var finalVals = obj.Id == 0 ? null : obj.Extract(monFields);
            var firstCreated = obj.Id == 0;

            #region SaveOrUpdate

            if (SaveOrUpdate(db, useTransaction, (sess, prod) =>
            {
                #region Price4LoginCustGroups

                if (model.Price4LoginCustGroups != null && settings.Hidden.Price4LoginOnly)
                {
                    foreach (var item in model.Price4LoginCustGroups.Where(x => x.CustGroupId != 0))
                    {
                        var custGroup = Singleton<ICustGroupMgr>.Default.GetDom(db, item.CustGroupId);
                        if (custGroup != null && custGroup.Price4Login != item.Selected)
                        {
                            custGroup.Price4Login = item.Selected;
                            sess.Update(custGroup);
                        }
                    }
                }

                #endregion

                objChanges.Action(m => sess.Update(m));

                var changes = originVals.GetChanges(finalVals);
                if (changes.Count != 0)
                {
                    if (changes.Any(m => m.Key.InList(monFields.ToArray()))) obj.BundlingRefs.Action(m => m.Bundle.CalculatePriceWeight());

                    var logTempl = string.Format("Updated '{{0}}' of '{0}', former value: '{{1}}'", obj.GetName());
                    ActionLogMgr.Add(db, obj.Id, model, logAction, changes, logTempl, request, sess);
                }

                variChanges.Where(m => m.Changes.Any(z => z.Key.InList(monFields.ToArray())))
                    .Select(m => sess.Get<VariantBase>(m.Id))
                    .Where(m => m != null && !m.Deleted && m.Active)
                    .Action(m => m.ProductBundles.Action(z => z.Bundle.CalculatePriceWeight()));

                ActionLogMgr.Add(db, variChanges, request, sess);

                if (!obj.Type.InList(ProductType.Bundle)) 
                {
                    if (firstCreated && obj.VariantNum == 0) ProdQtyLogMgr.SaveorUpdate(db,
                        new ProdQtyLogModel() { Reason = (int)(importing ? ProdQtyLogReason.Imported : ProdQtyLogReason.FirstCreation), Quantity = prod.Quantity, ProductId = prod.Id }, false);
                    else if (newVariants.SafeCount() != 0)
                        foreach (var itm in newVariants) { 
                            sess.SaveOrUpdate(itm);
                            ProdQtyLogMgr.SaveorUpdate(db,
                            new ProdQtyLogModel() { Reason = (int)ProdQtyLogReason.FirstCreation, Quantity = itm.Quantity, ProductId = prod.Id, VariantId = itm.Id }, false);
                        }
                    sess.SaveOrUpdate(prod);
                }

                return true;
            }, obj))
            {
                model.Id = obj.Id;
                new ProductMgr().OnUpdate(obj);

                return true;
            }
            else return false;

            #endregion
        }

        #endregion

        #region UpdateBundleQty

        public static bool UpdateBundleQty(IDB db, Domain.ProductBase product, int delta, bool changeStatus, Func<int?> funcOrderId, 
            List<System.Tuple<ProductBase, VariantBase, int>> odData = null)
        {
            if (delta == 0) return true;

            if (product == null) return false;

            foreach (var itm in odData != null 
                ? odData.Select(m => new {Product = m.Item1, Variant = m.Item2, Quantity = m.Item3}) 
                : product.ProductBundles/*.Where(m => m.Product.ValidToShow())*/.Select(m => new { m.Product, m.Variant, m.Quantity }))
            {
                UpdateQuantity(db, itm.Product, itm.Variant, itm.Quantity * delta, false, false, changeStatus, false,
                    new ProdQtyLogModel() { Reason = (int)ProdQtyLogReason.Bundled, RefIdFunc = funcOrderId, Note = string.Format("BundleId: {0}", product.Id) }, noBundle: true);
            }

            return true;
        } 

        #endregion

        #region UpdateProductStatus

        static protected bool UpdateProductStatus(IDB db, Domain.ProductBase product, int oldQty, bool variProd)
        {
            var instock = variProd && product.Variants.Any(x => !x.Deleted && (x.Quantity + (x.DropshipQty ?? 0)) > 0 && x.ProductStatus.Code.InList("instock", "new"));
            var outstock = variProd && !instock && product.Variants.All(x => !x.Deleted && x.ProductStatus.Code.InList("outstock"));

            if (((product.Quantity + (product.DropshipQty ?? 0)) == 0 || outstock) && product.ProductStatus.Code.EqualsIgCase("instock"))
            {
                var newStatus = StatusMgr.GetDom(db, "outstock");
                product.ProductStatus = newStatus ?? product.ProductStatus;
                product.ClearStatistics();
                return true;
            }
            else if (((product.Quantity + (product.DropshipQty ?? 0)) > 0 || instock) && product.ProductStatus.Code.EqualsIgCase("outstock"))
            {
                var newStatus = StatusMgr.GetDom(db, "instock");
                product.ProductStatus = newStatus ?? product.ProductStatus;
                product.ClearStatistics();
                return true;
            }

            return false;
        } 

        #endregion

        #region UpdateVariantStatus

        protected static void UpdateVariantStatus(IDB db, VariantBase variant, int oldQty)
        {
            if ((variant.Quantity + (variant.DropshipQty ?? 0)) == 0 && variant.ProductStatus.Code.EqualsIgCase("instock"))
            {
                var newStatus = StatusMgr.GetDom(db, "outstock");
                variant.ProductStatus = newStatus ?? variant.ProductStatus;
            }
            else if ((variant.Quantity + (variant.DropshipQty ?? 0)) > 0 && variant.ProductStatus.Code.EqualsIgCase("outstock"))
            {
                var newStatus = StatusMgr.GetDom(db, "instock");
                variant.ProductStatus = newStatus ?? variant.ProductStatus;
            }
        }

        #endregion

        #region UpdateQuantity
        
        public static bool UpdateQuantityRaw(IDB db, Domain.ProductBase product, VariantBase variant, int delta, QtyPos? qtyPos,
            bool autoSave = false, bool force = false, bool changeStatus = false, bool reCalc = false,
            ProdQtyLogModel logModel = null, StringBuilder errMsg = null, decimal? costPrice = null, 
            List<System.Tuple<ProductBase, VariantBase, int>> bundleData = null, bool noBundle = false)
        {
            if (product == null) return false;
            if (product.Id == 1 || (delta == 0 && !reCalc)) return true;

            Func<int, bool, bool> fProdStatus = (oldQty, variProd) => UpdateProductStatus(db, product, oldQty, variProd);
            
            #region fFireUpdate
            Action<int, byte> fFireUpdate = (id, oVisibility) => System.Threading.Tasks.Task.Run(async () =>
            {
                await System.Threading.Tasks.Task.Delay(10000);
                ISession session = null;
                var retries = 30;
            LB_LOOP:
                try
                {
                    session = IoC.Kernel.Get<ISession>();
                    var prod = session.Get<Domain.Product>(id);
                    if (oVisibility != prod.Visibility)
                    {
                        var lang = LanguageMgr.GetDefaultDBLang(new DataService(session)).SafeProp(m => m.ShortLangCode).IfNull("no");
                        var query = string.Format("{0}={1}", CurrencyModel.CurCode, "");
                        var request = new SimpleWorkerRequest(lang, query, new StreamWriter(new MemoryStream()));
                        HttpContext.Current = new HttpContext(request);
                        HttpContext.Current.Request.RequestContext.RouteData.Values.Add("lang", lang);
                        var controller = new ProductController(db.DataService);
                        controller.ControllerContext = new ControllerContext(HttpContext.Current.Request.RequestContext, controller);
                        ClawContext.Current.InitRequestData();

                        new ProductMgr().OnUpdate(prod);
                    }
                }
                catch (IOException ex) {
                    if (--retries > 0)
                    {
                        await System.Threading.Tasks.Task.Delay(10000 + 100 * new Random().Next(0, 100));
                        goto LB_LOOP;
                    }
                    else LogErr(ex);
                }
                catch (Exception ex) { LogErr(ex); }
                finally {
                    if (session != null && session.IsOpen) { session.Close(); session = null; }
                }
            });
            #endregion

            var settings = SettingsBiz.GetSettings();
            var oldVisibility = variant != null ? variant.Product.Visibility : product.Visibility;

            if (variant != null)
            {
                #region Variant
                
                if (!variant.ProductStatus.DescInstock && !force) return true;

                #region FixQty
                if (variant.BackOrderQty > 0 && variant.Quantity > 0)
                {
                    var min1 = Math.Min(variant.BackOrderQty, variant.Quantity1);
                    variant.BackOrderQty -= min1;
                    variant.Quantity1 -= min1;

                    if (settings.Hidden.Storage2Enabled && variant.Quantity2 > 0)
                    {
                        var min2 = Math.Min(variant.BackOrderQty, variant.Quantity2);
                        variant.BackOrderQty -= min2;
                        variant.Quantity2 -= min2;
                    }

                    variant.Quantity = variant.Quantity1 + (settings.Hidden.Storage2Enabled ? variant.Quantity2 : 0);
                }
                #endregion

                var oldQty = variant.Quantity;
                var oldBackQty = variant.BackOrderQty;

                if (qtyPos != null)
                {
                    #region QtyPos

                    if (delta < 0) switch (qtyPos.Value)
                    {
                        case QtyPos.Qty1:
                            variant.Quantity1 = Math.Max(0, variant.Quantity1 + delta);
                            break;
                        case QtyPos.Qty2:
                            variant.Quantity2 = Math.Max(0, variant.Quantity2 + delta);
                            break;
                    }
                    else switch (qtyPos.Value)
                    {
                        case QtyPos.Qty1:
                            variant.Quantity1 = Math.Max(0, variant.Quantity1 + Math.Max(0, delta - variant.BackOrderQty));
                            break;
                        case QtyPos.Qty2:
                            variant.Quantity2 = Math.Max(0, variant.Quantity2 + Math.Max(0, delta - variant.BackOrderQty));
                            break;
                    }

                    variant.Quantity = variant.Quantity1 + (settings.Hidden.Storage2Enabled ? variant.Quantity2 : 0);

                    if (/*variant.Quantity <= 0 && */((oldQty + delta < 0 && delta < 0) || (variant.BackOrderQty > 0 && delta > 0)))
                        variant.BackOrderQty = Math.Max(0, variant.BackOrderQty - (delta < 0 ? oldQty + delta : delta));

                    #endregion
                }
                else
                {
                    if (delta < 0)
                    {
                        int tmpDelta = delta, oldQty1 = variant.Quantity1;
                        if (variant.Quantity1 > 0)
                        {
                            variant.Quantity1 = Math.Max(0, variant.Quantity1 + tmpDelta);
                            tmpDelta += oldQty1 - variant.Quantity1;
                        }
                        if (tmpDelta < 0 && variant.Quantity2 > 0 && settings.Hidden.Storage2Enabled)
                        {
                            variant.Quantity2 = Math.Max(0, variant.Quantity2 + tmpDelta);
                        }
                    }
                    else variant.Quantity1 = Math.Max(0, variant.Quantity1 + Math.Max(0, delta - variant.BackOrderQty));

                    variant.Quantity = variant.Quantity1 + (settings.Hidden.Storage2Enabled ? variant.Quantity2 : 0);

                    if ((oldQty + delta < 0 && delta < 0) || (variant.BackOrderQty > 0 && delta > 0))
                        variant.BackOrderQty = Math.Max(0, variant.BackOrderQty - (delta < 0 ? oldQty + delta : delta));
                }

                if (variant.Quantity > oldQty && costPrice > 0)
                    variant.CostPrice = variant.CostPrice > 0 ? (oldQty * variant.CostPrice + (variant.Quantity - oldQty) * costPrice) / variant.Quantity : costPrice;

                var oldProdQty = variant.Product.Quantity;
                variant.Product.Quantity = variant.Product.Variants.Sum(x => x.Deleted ? 0 : x.Quantity);
                variant.Product.Quantity1 = variant.Product.Variants.Sum(x => x.Deleted ? 0 : x.Quantity1);
                variant.Product.Quantity2 = variant.Product.Variants.Sum(x => x.Deleted ? 0 : x.Quantity2);
                variant.Product.BackOrderQty = Math.Max(0, variant.Product.Variants.Sum(x => x.Deleted ? 0 : x.BackOrderQty));
                if (delta != 0 && logModel != null)
                {
                    logModel.Quantity = delta;
                    logModel.ProductId = product.Id;
                    logModel.VariantId = variant.Id;
                    ProdQtyLogMgr.SaveorUpdate(db, logModel, autoSave);
                }

                if (changeStatus)
                {
                    UpdateVariantStatus(db, variant, oldQty);

                    if (fProdStatus(oldProdQty, true) && autoSave) variant.Product.Update(db.DataService);
                }

                if (0 != (product.Flags & (int) ProductFlags.HideZeroQtyProd))
                {
                    if (variant.Quantity <= 0) 
                    { 
                        variant.Active = false;
                        if (variant.Product.Variants.All(x => x.Deleted || !x.Active || x.Quantity <= 0)) 
                            variant.Product.Visibility = (byte)(variant.Product.Visibility & ~(byte)VisibilityType.Web);
                    }
                    else if (settings.Hidden.ProductQtyAutoWebVisibilty)
                    {
                        variant.Active = true;
                        if (variant.Product.Variants.Any(x => !x.Deleted && x.Active && x.Quantity > 0))
                            variant.Product.Visibility |= (byte)VisibilityType.Web;
                    }
                }

                var objChanges = new List<object>();
                if ((oldQty != variant.Quantity || oldBackQty != variant.BackOrderQty) && !noBundle)
                {
                    variant.ProductBundles.Action(m => { m.Bundle.CalculateQty(); objChanges.Add(m.Bundle); });
                    variant.Product.BundlingRefs
                        .Where(m => m.Bundle.ProductBundles.Any(z => z.Variant == null && z.Product.Id == variant.Product.Id))
                        .Action(m => { m.Bundle.CalculateQty(); objChanges.Add(m.Bundle); });
                }

                if (delta > 0 && oldBackQty > 0)
                {
                    var addQty = delta;
                    Singleton<IOrderMgr>.Default.GetDependOrders(db, product.Id).OrderBy(m => m.Id).Where(z => addQty > 0)
                        .Action(m =>
                        {
                            m.GetOrderDetailsEx()
                                .Where(z => addQty > 0 && z.StockQty < 0 && z.GetProduct().GetId() == product.Id && z.GetVariant().GetId() == variant.Id)
                                .Action(z =>
                                {
                                    var tmp = Math.Min(addQty, Math.Min(z.Quantity, -1*(z.StockQty ?? 0)));
                                    addQty -= tmp;
                                    z.StockQty += tmp;
                                    objChanges.Add(z);
                                });

                            m.GetOrderDetailBundles()
                                .Where(z => addQty > 0 && z.StockQty < 0 && z.GetProduct().GetId() == product.Id && z.GetVariant().GetId() == variant.Id)
                                .Action(z =>
                                {
                                    var tmp = Math.Min(addQty, Math.Min(z.Quantity, -1 * (z.StockQty ?? 0)));
                                    addQty -= tmp;
                                    z.StockQty += tmp;

                                    var odetail = z.GetOrderDetail();
                                    if (odetail != null && odetail.StockQty < 0)
                                    {
                                        odetail.StockQty = odetail.GetOrderDetailBundles().Min(w => w.StockQty >= 0 || w.Quantity == 0 ? 0 : w.StockQty / w.Quantity);
                                        objChanges.Add(odetail);
                                    }

                                    objChanges.Add(z);
                                });
                        });
                }

                if (FuncUpdateQuantity != null) FuncUpdateQuantity(db, variant.Product, variant, oldQty);
                if (FuncUpdateQuantity2 != null) FuncUpdateQuantity2(db, variant.Product, variant, oldQty, logModel);

                variant.Product.CalculateSalePrice(settings);

                ProductScope.Invalidate();

                if (oldVisibility != variant.Product.Visibility) fFireUpdate(variant.Product.Id, oldVisibility);

                if (autoSave) return variant.Update(db.DataService) && variant.Product.Update(db.DataService) && (objChanges.Count == 0 || objChanges.Update(db.DataService)); 
                
                #endregion
            }
            else
            {
                #region Product

                if (!product.ProductStatus.DescInstock && !force) return true;

                if (product.VariantNum > 0 ||
                    (product.VariantNum == null && product.Variants.Any(x => !x.Deleted))) return false;

                #region FixQty
                if (product.BackOrderQty > 0 && product.Quantity > 0)
                {
                    var min1 = Math.Min(product.BackOrderQty, product.Quantity1);
                    product.BackOrderQty -= min1;
                    product.Quantity1 -= min1;

                    if (settings.Hidden.Storage2Enabled && product.Quantity2 > 0)
                    {
                        var min2 = Math.Min(product.BackOrderQty, product.Quantity2);
                        product.BackOrderQty -= min2;
                        product.Quantity2 -= min2;
                    }

                    product.Quantity = product.Quantity1 + (settings.Hidden.Storage2Enabled ? product.Quantity2 : 0);
                }
                #endregion

                var oldQty = product.Quantity;
                var oldBackQty = product.BackOrderQty;
                var bundleOrdered = product.Type.InList(ProductType.Bundle) && logModel != null && (logModel.Reason ?? 0).InList(ProdQtyLogReason.Ordered, ProdQtyLogReason.Credited, ProdQtyLogReason.Cancelled);
                var bundleRecalc = product.Type.InList(ProductType.Bundle) && reCalc;

                if (bundleRecalc)
                {
                    var showProdInStock = 0 != (product.Flags & (int)ProductFlags.BundleShowProdInStock);
                    var lst = product.ProductBundles.Where(m => !m.Product.Deleted && (m.Variant == null || (!m.Variant.Deleted /*&& m.Variant.Active*/))).ToList();
                    product.Quantity = lst.Count == 0 ? 0 : lst.Min(m => m.Quantity == 0 || m.Product.AllowBackorder ? (m.Product.AllowBackorder ? 10000 : 0) : (m.Variant != null ? m.Variant.Quantity : m.Product.Quantity) / m.Quantity);
                    product.BackOrderQty = lst.Count == 0 ? 0 : lst.Max(m => m.Quantity == 0 ? 0 : (m.Variant != null ? m.Variant.BackOrderQty : m.Product.BackOrderQty) / m.Quantity);
                    product.AllowBackorder = lst.All(m => m.Product.AllowBackorder) && product.Accessories.Where(m => m.BundleGroup != null).All(m => m.Accessory.AllowBackorder);

                    if (0 != (product.Flags & (int) ProductFlags.BundleNeedSelectAll) &&
                        product.Accessories.Where(m => m.BundleGroup != null).GroupBy(m => m.BundleGroup.Id)
                        .Any(m => m.All(z => (showProdInStock || !z.Accessory.AllowBackorder) && z.Accessory.Quantity <= 0)))
                    {
                        product.Quantity = 0;
                    }

                    product.Quantity1 = product.Quantity;
                    product.Quantity2 = 0;
                }
                else 
                {
                    if (qtyPos != null)
                    {
                        #region QtyPos

                        if (delta < 0) switch (qtyPos.Value)
                        {
                            case QtyPos.Qty1:
                                product.Quantity1 = Math.Max(0, product.Quantity1 + delta);
                                break;
                            case QtyPos.Qty2:
                                product.Quantity2 = Math.Max(0, product.Quantity2 + delta);
                                break;
                        }
                        else switch (qtyPos.Value)
                        {
                            case QtyPos.Qty1:
                                product.Quantity1 = Math.Max(0, product.Quantity1 + Math.Max(0, delta - product.BackOrderQty));
                                break;
                            case QtyPos.Qty2:
                                product.Quantity2 = Math.Max(0, product.Quantity2 + Math.Max(0, delta - product.BackOrderQty));
                                break;
                        }

                        product.Quantity = product.Quantity1 + (settings.Hidden.Storage2Enabled ? product.Quantity2 : 0);

                        if (product.Quantity <= 0 && ((oldQty + delta < 0 && delta < 0) || (product.BackOrderQty > 0 && delta > 0)))
                            product.BackOrderQty = Math.Max(0, product.BackOrderQty - (delta < 0 ? oldQty + delta : delta));

                        #endregion
                    }
                    else
                    {
                        if (delta < 0)
                        {
                            int tmpDelta = delta, oldQty1 = product.Quantity1;
                            if (product.Quantity1 > 0)
                            {
                                product.Quantity1 = Math.Max(0, product.Quantity1 + tmpDelta);
                                tmpDelta += oldQty1 - product.Quantity1;
                            }
                            if (tmpDelta < 0 && product.Quantity2 > 0 && settings.Hidden.Storage2Enabled)
                            {
                                product.Quantity2 = Math.Max(0, product.Quantity2 + tmpDelta);
                            }
                        }
                        else
                        {
                            product.Quantity1 = Math.Max(0, product.Quantity1 + Math.Max(0, delta - product.BackOrderQty));
                        }

                        if ((oldQty + delta < 0 && delta < 0) || (product.BackOrderQty > 0 && delta > 0))
                            product.BackOrderQty = Math.Max(0, product.BackOrderQty - (delta < 0 ? oldQty + delta : delta));
                    }
                }

                product.Quantity = product.Quantity1 + (settings.Hidden.Storage2Enabled ? product.Quantity2 : 0);

                if (product.Quantity > oldQty && costPrice > 0)
                    product.CostPrice = product.CostPrice > 0 ? (oldQty * product.CostPrice + (product.Quantity - oldQty) * costPrice) / product.Quantity : costPrice;

                if (delta != 0 && logModel != null && !bundleRecalc)
                {
                    logModel.Quantity = delta;
                    logModel.ProductId = product.Id;
                    ProdQtyLogMgr.SaveorUpdate(db, logModel, autoSave);
                }

                if (changeStatus) fProdStatus(oldQty, false);

                if (0 != (product.Flags & (int) ProductFlags.HideZeroQtyProd))
                {
                    if(product.Quantity <= 0) product.Visibility = (byte)(product.Visibility & ~(byte)VisibilityType.Web);
                    else if (settings.Hidden.ProductQtyAutoWebVisibilty) product.Visibility |= (byte)VisibilityType.Web;
                }

                var objChanges = new List<object>();
                if ((oldQty != product.Quantity || oldBackQty != product.BackOrderQty) && !noBundle)
                {
                    product.BundlingRefs.Action(m => { m.Bundle.CalculateQty(); objChanges.Add(m.Bundle); });
                    product.AccessoryRefs.Where(m => m.BundleGroup != null && m.Product.Type.InList(ProductType.Bundle))
                        .Action(m => { m.Product.CalculateQty(); objChanges.Add(m.Product); });
                }

                if (bundleOrdered && product.Id != 0) UpdateBundleQty(db, product, delta, true, logModel.RefIdFunc ?? (() => logModel.RefId), bundleData);

                if (delta > 0 && oldBackQty > 0 && !bundleRecalc)
                {
                    var addQty = delta;
                    Singleton<IOrderMgr>.Default.GetDependOrders(db, product.Id).OrderBy(m => m.Id).Where(z => addQty > 0)
                        .Action(m =>
                        {
                            m.GetOrderDetailsEx()
                                .Where(z => addQty > 0 && z.GetProduct().GetId() == product.Id && z.StockQty < 0)
                                .Action(z =>
                                {
                                    var tmp = Math.Min(addQty, Math.Min(z.Quantity, -1*(z.StockQty ?? 0)));
                                    addQty -= tmp;
                                    z.StockQty += tmp;
                                    objChanges.Add(z);
                                });

                            m.GetOrderDetailBundles()
                                .Where(z => addQty > 0 && z.GetProduct().GetId() == product.Id && z.StockQty < 0)
                                .Action(z =>
                                {
                                    var tmp = Math.Min(addQty, Math.Min(z.Quantity, -1 * (z.StockQty ?? 0)));
                                    addQty -= tmp;
                                    z.StockQty += tmp;

                                    var odetail = z.GetOrderDetail();
                                    if (odetail != null && odetail.StockQty < 0)
                                    {
                                        odetail.StockQty = odetail.GetOrderDetailBundles().Min(w => w.StockQty >= 0 || w.Quantity == 0 ? 0 : w.StockQty/w.Quantity);
                                        objChanges.Add(odetail);
                                    }

                                    objChanges.Add(z);
                                });
                        });
                }

                if (FuncUpdateQuantity != null) FuncUpdateQuantity(db, product, null, oldQty);
                if (FuncUpdateQuantity2 != null) FuncUpdateQuantity2(db, product, null, oldQty, logModel);

                ProductScope.Invalidate();

                if (oldVisibility != product.Visibility) fFireUpdate(product.Id, oldVisibility);

                if (autoSave) return product.Update(db.DataService) && (objChanges.Count == 0 || objChanges.Update(db.DataService)); 

                #endregion
            }

            return true;
        }

        /// <summary>CApp</summary>
        public static bool UpdateQuantity(IDB db, Domain.ProductBase product, VariantBase variant, int delta,
            bool autoSave = false, bool force = false, bool changeStatus = false, bool reCalc = false,
            ProdQtyLogModel logModel = null, StringBuilder errMsg = null, decimal? costPrice = null,
            List<System.Tuple<ProductBase, VariantBase, int>> bundleData = null, bool noBundle = false)
        {
            return UpdateQuantityRaw(db, product, variant, delta, null, autoSave, force, changeStatus, reCalc, logModel, errMsg, costPrice, bundleData, noBundle);
        }

        public static bool UpdateQuantity(IDB db, Domain.Product product, Variant variant, int delta,
            ProdQtyLogModel logModel, bool autoSave = false, bool force = false, bool changeStatus = false,
            bool reCalc = false, StringBuilder errMsg = null, decimal? costPrice = null, QtyPos? qtyPos = null)
        {
            return UpdateQuantityRaw(db, product, variant, delta, qtyPos, autoSave, force, changeStatus, reCalc, logModel, errMsg, costPrice);
        }

        public static bool UpdateQuantity(IDB db, int productId, int? variantId, int delta, ProdQtyLogModel logModel, bool changeStatus = false,
            StringBuilder errMsg = null, decimal? costPrice = null, QtyPos? qtyPos = null)
        {
            var product = GetDom(db, productId);
            var variant = VariantMgr.GetDom(db, variantId ?? 0);

            if (variant != null && variant.Product.Id != productId) return false;

            return UpdateQuantity(db, product, variant, delta, logModel, true, false, changeStatus, false, errMsg, costPrice, qtyPos);
        }
        #endregion

        #region GenerateBarCode

        public static string GenerateBarCode(IDB db, int id, int? variantId)
        {
            var len = Math.Max(4, Math.Min(50, SettingsBiz.GetSettings().Display.BarCode.CharLength));
            var maxLoop = 1000;
            var barcode = "";
            var rand = new Random();
            var buf = new byte[len];

            while (--maxLoop > 0)
            {
                rand.NextBytes(buf);
                barcode = buf.Select(x => x.ToString("00")).ToString("").TrimStart('0').SubstringSafe(0, len);
                while (barcode.Length < len) barcode += '0';

                if (!Exists<Domain.Product>(db.DataService, m => m.Id, id, null, new[] { new KeyValuePair() { Key = "BarCode", Value = barcode } }) &&
                    !Exists<Domain.Variant>(db.DataService, m => m.Id, variantId ?? 0 , null, new[] { new KeyValuePair() { Key = "BarCode", Value = barcode } })) break;
            }

            return barcode;
        }

        #endregion

        #region Count

        public static int Count(IDB db, string langCode, params int[] categoryids)
        {
            var hSql = string.Format(@"
            select count(*) from {0} as prod join prod.Languages as lang  
                join prod.CategoryProducts as cateProd
            where lang.LangCode = '{1}' and cateProd.Category.Id in ({2})",
                typeof(Domain.Product).FullName, langCode, categoryids.ToString(","));

            var ret = GetList<object>(db, hSql);

            return ret.Count == 0 ? 0 : (int)Convert.ChangeType(ret[0], typeof(int));
        }

        #endregion

        #region Clone

        public static Domain.Product Clone(IDB db, int id)
        {
            var settings = SettingsBiz.GetSettings();
            var obj = GetDom(db, id);

            #region Product

            var newProd = new Domain.Product()
            {
                Alias = Unique(db, "Alias", obj),
                Type = obj.Type,
                SKU = Unique<Domain.Product, Variant>(db, "SKU", obj),
                BarCode = Unique<Domain.Product, Variant>(db, "BarCode", obj),
                Quantity = obj.Quantity,
                Quantity1 = obj.Quantity1,
                Quantity2 = obj.Quantity2,
                Vat = obj.Vat,
                SecondVat = obj.SecondVat,
                Price = obj.Price,
                SalePrice = obj.SalePrice ?? 0,
                CostPrice = obj.CostPrice,
                TechSpecCostExtra = obj.TechSpecCostExtra,
                DepositAmount = obj.DepositAmount,
                Weight = obj.Weight,
                WUnit = obj.WUnit,
                ShippingCostExtra = obj.ShippingCostExtra, 
                AttachFile = obj.AttachFile,
                VideoUrls = obj.VideoUrls,
                Location = obj.Location,
                MinQuantity = obj.MinQuantity,
                Flags = obj.Flags,
                AllowBackorder = obj.AllowBackorder,
                BackOrderQty = 0,
                BackorrderMsg = obj.BackorrderMsg,
                NotifyQtyThreshold = obj.NotifyQtyThreshold,
                Visibility = obj.Visibility,
                ModifiedDate = DateTime.Now,
                CreatedDate = DateTime.Now,
                PublishedDate = obj.PublishedDate,
                Manufacturer = obj.Manufacturer,
                ProductStatus = obj.ProductStatus,
                ShippingType = obj.ShippingType,
                Supplier = obj.Supplier,
                SelfDiscActive = obj.SelfDiscActive,
                SelfDiscMinQty = obj.SelfDiscMinQty,
                SelfDiscValue = obj.SelfDiscValue,

                PriceCkActive = obj.PriceCkActive,
                PriceCkDateFrom = obj.PriceCkDateFrom,
                PriceCkDateTo = obj.PriceCkDateTo,
                PriceCkMinVal = obj.PriceCkMinVal,
                PriceCkUrl = obj.PriceCkUrl,
                PriceCkVariant = obj.PriceCkVariant,

                CGroupDisc0Id = obj.CGroupDisc0Id,
                CGroupDisc0Rate = obj.CGroupDisc0Rate, 
                CGroupDisc1Id = obj.CGroupDisc1Id,
                CGroupDisc1Rate = obj.CGroupDisc1Rate, 
                CGroupDisc2Id = obj.CGroupDisc2Id,
                CGroupDisc2Rate = obj.CGroupDisc2Rate,
                //CGroupDisc3Id = obj.CGroupDisc3Id,
                //CGroupDisc3Rate = obj.CGroupDisc3Rate,  
                CGroupDisc4Id = obj.CGroupDisc4Id,
                CGroupDisc4Rate = obj.CGroupDisc4Rate,
                BundleDiscountRate = obj.BundleDiscountRate,
            };

            #endregion

            #region Languages

            foreach (var lang in obj.Languages)
            {
                newProd.Languages.Add(new ProductLang()
                {
                    Product = newProd,
                    Name = Unique(db, "Name", lang),
                    Tags = lang.Tags,
                    Description = lang.Description,
                    ExtraInfo = lang.ExtraInfo,
                    PageTitle = lang.PageTitle,
                    MetaDesc = lang.MetaDesc,
                    Visible = lang.Visible,
                    LangCode = lang.LangCode,
                    Alias = Unique(db, "Alias", lang),
                });
            }

            #endregion

            #region Variants

            foreach (var vari in obj.Variants.Where(x => !x.Deleted).Cast<Variant>())
            {
                #region Variant

                var dVar = new Variant()
                {
                    Product = newProd,
                    SKU = Unique<Variant, Domain.Product>(db, "SKU", vari),
                    BarCode = Unique<Variant, Domain.Product>(db, "BarCode", vari),
                    Price = vari.Price,
                    CostPrice = vari.CostPrice,
                    SalePrice = vari.SalePrice ?? 0,
                    Quantity = vari.Quantity,
                    Quantity1 = vari.Quantity1,
                    Quantity2 = vari.Quantity2,
                    BackOrderQty = 0,
                    Weight = vari.Weight,
                    Image = vari.Image,
                    Active = vari.Active,
                    DisplayOrder = vari.DisplayOrder,
                    ProductStatus = vari.ProductStatus,
                    Location = vari.Location,
                };

                newProd.Variants.Add(dVar);

                #endregion

                #region VariantPropVals

                foreach (VariantPropVal vprop in vari.VariantPropVals)
                {
                    dVar.VariantPropVals.Add(new VariantPropVal() { Variant = dVar, PropVal = vprop.PropVal });
                }

                #endregion

                if (settings.Hidden.VariantMultiImage)
                {
                    #region Languages

                    dVar.Languages.AddRange(vari.Languages.Select(x => new VariantLang()
                    {
                        Variant = dVar,
                        LangCode = x.LangCode,
                        Description = x.Description,
                    }));

                    #endregion

                    #region Images

                    dVar.Images.AddRange(vari.Images.Select(x => new VariantImage()
                    {
                        Variant = dVar,
                        FileName = x.FileName,
                        DisplayOrder = x.DisplayOrder,
                    }));

                    dVar.Image = dVar.Images.OrderBy(x => x.DisplayOrder).ThenByDescending(x => x.Id)
                        .Select(x => x.FileName).FirstOrDefault();

                    #endregion
                }
            }

            newProd.VariantNum = newProd.Variants.Count(x => !x.Deleted);

            #endregion

            #region Images

            foreach (var image in obj.Images)
            {
                var dImg = new Domain.Image()
                {
                    Product = newProd,
                    FileName = image.FileName,
                    DisplayOrder = image.DisplayOrder,
                };

                newProd.Images.Add(dImg);

                #region PhotoLang

                foreach (ImageLang photoLang in image.Languages)
                {
                    dImg.Languages.Add(new ImageLang()
                    {
                        Image = dImg,
                        Title = photoLang.Title,
                        LangCode = photoLang.LangCode,
                    });
                }

                #endregion
            }

            newProd.Image = obj.Image == null ? null : newProd.Images.MatchOrFirst(m => m.FileName == obj.Image.FileName);

            #endregion

            #region Bundlings

            foreach (var bundl in obj.ProductBundles)
            {
                newProd.ProductBundles.Add(new ProductBundle()
                {
                    Bundle = newProd,
                    Quantity = bundl.Quantity,
                    Product = bundl.Product,
                    Variant = bundl.Variant,
                });
            }

            #endregion

            #region BundleGroups

            foreach (var bgroup in obj.ProductBundleGroups)
            {
                newProd.ProductBundleGroups.Add(new ProductBundleGroup()
                {
                    Product = newProd,
                    Name = bgroup.Name,
                    ShowVariant = bgroup.ShowVariant,
                    Accessories = bgroup.Accessories,
                    MinPrice = bgroup.MinPrice,
                });
            }

            #endregion

            #region Categorys

            foreach (var catPro in obj.CategoryProducts)
            {
                newProd.CategoryProducts.Add(new CategoryProduct() { Category = catPro.Category, Product = newProd });
            }

            #endregion

            #region Accessories

            foreach (var itm in obj.Accessories)
            {
                newProd.Accessories.Add(new ProductAccessory()
                {
                    Product = newProd, Accessory = itm.Accessory, AcceVariant = itm.AcceVariant,
                    BundleGroup = itm.BundleGroup == null ? null : newProd.ProductBundleGroups.FirstOrDefault(m => m.Name.EqualsIgCase(itm.BundleGroup.Name))
                });
            }

            #endregion

            #region ExtraInformation

            foreach (var item in obj.ExtraInformation)
            {
                newProd.ExtraInformation.Add(new ProductExtraInformation()
                {
                    Product = newProd,
                    FieldName = item.FieldName,
                    FieldValue = item.FieldValue
                });
            }

            #endregion

            #region TechSVals

            foreach (var itm in obj.ProductTechSVals)
            {
                newProd.ProductTechSVals.Add(new ProductTechSVal()
                {
                    Product = newProd,
                    TechSpecVal = itm.TechSpecVal
                });
            }

            #endregion

            #region RecurDiscs

            foreach (var itm in obj.ProductRecurDiscs)
            {
                newProd.ProductRecurDiscs.Add(new ProductRecurDisc()
                {
                    Product = newProd, DefOpt = itm.DefOpt, RecurEach = itm.RecurEach,
                    Type = itm.Type, Value = itm.Value, Discount = itm.Discount,
                });
            }

            #endregion

            return newProd;
        }

        #endregion

        #region Find

        public static List<Domain.Product> Find(IDB db, string name)
        {
            if (name.IsNullOrEmpty()) return null;

            var hSql = string.Format(@"
            select prod from {0} as prod join prod.Languages as lang
            where lang.Name like '{1}' ", typeof(Domain.Product).FullName, TranslateKWord(name));

            return GetList<Domain.Product>(db, hSql);
        }

        #endregion

        #region Get

        public static ProductModel Get(IDB db, string sku, string alias, string variantSku)
        {
            if (sku.IsNullOrEmpty() && alias.IsNullOrEmpty() && variantSku.IsNullOrEmpty()) return null;

            var args = new List<System.Tuple<string, string>>();
            var hSql = string.Format(@"
            select prod from {0} as prod join prod.Languages as lang {{0}}
            where 1 = 1 ", typeof(Domain.Product).FullName);

            if (!sku.IsNullOrEmpty()) hSql += " and prod.SKU = :sku ";

            if (!alias.IsNullOrEmpty()) hSql += " and (lang.Alias = :alias or prod.Alias = :alias) ";

            if (variantSku.IsNullOrEmpty()) hSql = string.Format(hSql, "");
            else hSql = string.Format(hSql, " join prod.Variants as vari ") + " and vari.SKU  = :variantsku ";

            args.Add(new System.Tuple<string, string>("alias", alias));
            args.Add(new System.Tuple<string, string>("sku", sku));
            args.Add(new System.Tuple<string, string>("variantsku", variantSku));

            var ret = GetList<Domain.Product>(db, hSql, args.ToArray());

            return ret.SafeCount() == 0 ? null : Mapper.Map<Domain.Product, ProductModel>(ret[0]);
        }

        #endregion

        #region Preload

        public static void Preload(IDB db)
        {
            try 
            {
                var lst0A = GetList<Domain.Product>(db, string.Format("select prod from {0} as prod ",
                    typeof(Domain.Product).FullName));

                var lst0B = GetList<Domain.ProductLang>(db, string.Format("select lang from {0} as lang ",
                    typeof(Domain.ProductLang).FullName));
            }
            catch (Exception err) { LogErr(err); }

            try 
            {
                var lst1A = GetList<Domain.PropVal>(db, string.Format("select pval from {0} as pval ",
                    typeof(Domain.PropVal).FullName));
                var lst1B = GetList<Domain.PropValLang>(db, string.Format("select lang from {0} as lang ",
                    typeof(Domain.PropValLang).FullName));
            }
            catch (Exception err) { LogErr(err); }

            try 
            {
                var lst2 = GetList<Domain.Variant>(db, string.Format("select vari from {0} as vari ",
                    typeof(Domain.Variant).FullName));
            }
            catch (Exception err) { LogErr(err); }

            try 
            {
                var lst3 = GetList<object>(db, string.Format("from {0} ", typeof(Domain.VariantPropVal).FullName));
            }
            catch (Exception err) { LogErr(err); }

            try 
            {
                var lst4 = GetList<Domain.CategoryProduct>(db, string.Format("from {0} ", typeof(Domain.CategoryProduct).FullName));
            }
            catch (Exception err) { LogErr(err); }

            try 
            {
                var lst5A = GetList<Domain.Category>(db, string.Format("select cate from {0} as cate ",
                    typeof(Domain.Category).FullName));
                var lst5B = GetList<Domain.CategoryLang>(db, string.Format("select lang from {0} as lang ",
                    typeof(Domain.CategoryLang).FullName));
            }
            catch (Exception err) { LogErr(err); }
        }

        #endregion

        #region ProcessAutoChangeStatus

        public static void ProcessAutoChangeStatus(IDB db, int days)
        {
            var hSql = string.Format(@"
            select prod from {0} as prod
            where prod.ProductStatus.Id in (1)
            and prod.CreatedDate < '{1:yyyy-MM-dd HH:mm:ss}'", typeof(Domain.Product).FullName, DateTime.Now.AddDays(-1 * days));

            var instock = StatusMgr.GetDom(db, 2);
            var ret = GetList<Domain.Product>(db, hSql);

            ret.Action(x => x.ProductStatus = instock);

            Update(db, ret);

            ret.Action(x => new ProductMgr().OnUpdate(x));
        }

        #endregion

        #region ProcessPriceCheck

        public static void ProcessPriceCheck(IDB db, int hourStart, int hourStop, int batchSize)
        {
            if (hourStart == hourStop || batchSize <= 0 || hourStart > DateTime.Now.Hour || hourStop <= DateTime.Now.Hour) return;

            var monFields = new[] { Expr(m => m.SalePrice) }.ToFieldName();
            var monVarFields = new[] { Expr(new Variant(), m => m.SalePrice) }.ToFieldName();

            var hSql = string.Format(@"
            from {0} as prod
            where prod.PriceCkActive = 1
            and (prod.PriceCkDateFrom is null or prod.PriceCkDateFrom <= '{2:yyyy-MM-dd}')  
            and (prod.PriceCkDateTo is null or prod.PriceCkDateTo >= '{2:yyyy-MM-dd}')
            and (prod.PriceCkTimestamp is null or prod.PriceCkTimestamp < '{1:yyyy-MM-dd HH:mm:ss}')
            order by prod.PriceCkTimestamp ", typeof(Domain.Product).FullName, DateTime.Now.AddDays(-0.9), DateTime.Now);

            int total;
            var lst = GetList<Domain.Product>(db, hSql, 1, batchSize, out total, null, null);
            var task = db.DataService.GetObject<Task>(GetSiteId(db), "Name", "PriceCheckJob") ?? new Task()
            {
                Name = "PriceCheckJob", Active = false, IsService = false, ExeClassInfo = "", Interval = 0
            };

            task.LastRunTime = DateTime.Now;
            task.LastRunMsg = null;

            foreach (var itm in lst)
            {
                try
                {
                    var changed = false;
                    var variChanges = new List<ActionLogMgr.ChangeInfo<VariantModel>>();

                    if (itm.Variants.Count != 0)
                    {
                        foreach (var vari in itm.Variants)
                        {
                            var minPrice = itm.PriceCheck(vari);
                            if (minPrice > 0 && minPrice < (vari.SalePrice > 0 ? vari.SalePriceInc : vari.PriceInc))
                            {
                                var oriVarDic = vari.Extract(monVarFields);

                                vari.SalePrice = itm.ExcVat(minPrice); changed = true;

                                var changes = oriVarDic.GetChanges(vari.Extract(monVarFields));
                                if (changes.Count != 0)
                                    variChanges.Add(new ActionLogMgr.ChangeInfo<VariantModel>()
                                    {
                                        Action = LogActionType.Product | LogActionType.ProdVariant | LogActionType.Edit,
                                        Changes = changes, Id = vari.Id, Model = new VariantModel(),
                                        LogTemplate = string.Format("Updated '{{0}}' of '{0}', former value: '{{1}}'", itm.GetName(vari, "nb-NO"))
                                    });
                            }
                        }

                        ActionLogMgr.Add(db, variChanges, null, null, "PRICE-CHECKER");
                    }
                    else
                    {
                        var minPrice = itm.PriceCheck();
                        if (minPrice > 0 && minPrice < (itm.SalePrice > 0 ? itm.SalePriceInc : itm.PriceInc))
                        {
                            var oriVarDic = itm.Extract(monFields);
                            
                            itm.SalePrice = itm.ExcVat(minPrice); changed = true;

                            var changes = oriVarDic.GetChanges(itm.Extract(monFields));
                            if (changes.Count != 0) 
                                ActionLogMgr.Add(db, itm.Id, new ProductModel(), LogActionType.Product | LogActionType.Edit,
                                    changes, string.Format("Updated '{{0}}' of '{0}', former value: '{{1}}'", itm.GetName(null, "nb-NO")), null, null, "PRICE-CHECKER");
                        }
                    }

                    itm.PriceCkTimestamp = DateTime.Now;
                    Update(db, new[] {itm}.ToList());
                    //new ProductMgr().OnUpdate(itm);
                }
                catch (Exception err)
                {
                    task.LastRunMsg = err.Message;
                    LogHelper.Log(new Exception(string.Format("Error: {0}, ProductId: {1}", err.Message, itm.Id), err));
                }
            }

            task.LastRunMsg = task.LastRunMsg ?? "Ok";
            task.LastDoneTime = DateTime.Now;
            task.SaveorUpdate(db.DataService);
        }

        #endregion

        #region ProcessAassortment

        public static void ProcessAassortment(IDB db, SettingsBiz settings)
        {
            var hSql = string.Format(@"
            select prod, vari 
            from {0} as prod left join prod.Variants as vari
            where prod.Flags >= {1} and 0 <> (prod.Flags & {1}) and prod.Type = {2}
            and ((vari is null and prod.Quantity <= prod.AassortmentThreshold) 
                or (vari is not null and vari.Active = 1 and vari.Deleted = 0 and vari.Quantity <= prod.AassortmentThreshold)
            )
             ", typeof(Domain.Product).FullName, ProductFlags.Aassortment.Val(), ProductType.Normal.Val());
                                           
            //LogHelper.EventLog($"ProcessAassortment: sql = {GetSqlString(hSql, db.GetSession())}");

            var lst = GetList<object[]>(db, hSql)
                .Select(m => new System.Tuple<Domain.Product, Domain.Variant>((Domain.Product)m[0], (Domain.Variant)m[1])).ToList();

            if (lst.Count == 0) return;

            #region SendEmail

            try
            {
                var lang = "no";
                var query = string.Format("{0}={1}", CurrencyModel.CurCode, settings.General.Currency);
                var request = new SimpleWorkerRequest(lang, query, new StreamWriter(new MemoryStream()));
                HttpContext.Current = new HttpContext(request);
                HttpContext.Current.Request.RequestContext.RouteData.Values.Add("lang", lang);
                HttpContext.Current.Request.RequestContext.RouteData.Values.Add(CurrencyModel.CurCode, settings.General.Currency);
                var controller = new ProductController(db.DataService);
                controller.ControllerContext = new ControllerContext(HttpContext.Current.Request.RequestContext, controller);
                ClawContext.Current.InitRequestData();

                var typeSendMailHelper = Type.GetType("Claw.Modules.Eshop.Helper.SendMailHelpers, Claw.Modules.Eshop.FrontEnd");
                var emailTempl = EmailTemplMgr.GetAll(db, (int)EmailTemplateType.ProductAassortment).FirstOrDefault();
                if (typeSendMailHelper != null&& emailTempl != null)
                {
                    var sendMailHelper = Activator.CreateInstance(typeSendMailHelper, controller);
                    sendMailHelper.SafeInvokeMethod<string>("EmailAassortment", db, lst, emailTempl);
                 }
                else LogErr(new Exception("ProductMgr.ProcessAassortment send email failed, typeSendMailHelper/emailTempl is null"));
            }
            catch (Exception err)
            {
                LogErr(err, section: "ProductMgr.ProcessAassortment");
            }
            finally { HttpContext.Current = null; }

            #endregion
        }

        #endregion

        #region OnUpdate
        protected override void OnUpdate(Domain.Product obj)
        {
            ProductScope.Invalidate();
            LuceneIndexing.IndexingService.AddUpdateLuceneIndex(obj);
        }
        #endregion

        #region OnDelete
        protected override void OnDelete(Domain.Product obj)
        {
            ProductScope.Invalidate();
            LuceneIndexing.IndexingService.ClearRecordIndex(obj);
        }
        #endregion
    }
}