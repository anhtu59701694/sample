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
using NHibernate.Criterion;

namespace Claw.Modules.Eshop.DAL
{
    public class CategoryMgr : BaseMgr<CategoryModel, Category>
    {
        public static volatile Func<IDB, string, string, List<System.Tuple<string, string>>, System.Tuple<string, string>> FuncHsqlList = null;

        static CategoryMgr() { Init(typeof(CategoryMgr)); }

        #region GetAll

        public static List<CategoryModel> GetAll(IDB db, string langCode, int? categoryid, bool? visible,
            int pageIndex, int pageSize, out int totalRows,
            SortDirection sortDirection = SortDirection.Descending, string sortField = "Id",
            string keyword = null, bool? visibility = null, bool admin = true, bool? visibilityPos = null)
        {
            var ret = GetAllRaw(db, langCode, categoryid, visible, pageIndex, pageSize, out totalRows,
                sortDirection, sortField, keyword, visibility, admin, visibilityPos);

            return Mapper.Map<List<Category>, List<CategoryModel>>(ret);
        }

        public static List<Category> GetAllRaw(IDB db, string langCode, int? categoryid, bool? visible,
            int pageIndex, int pageSize, out int totalRows,
            SortDirection sortDirection = SortDirection.Descending, string sortField = "Id",
            string keyword = null, bool? visibility = null, bool admin = true, bool? visibilityPos = null)
        {
            if (admin) return GetAllRawEx(db, langCode, categoryid, visible, pageIndex, pageSize, out totalRows, sortDirection, sortField, keyword, visibility, true, visibilityPos);

            var lst = Helper.DataCache<Helper.CategoryScope, Tuple2<int, List<Category>>>.Get(
                string.Format("DAL.CategoryMgr.GetAllRaw:{0}:{1}:{2}:{3}:{4}:{5}:{6}:{7}:{8}:{9}",
                langCode, categoryid, visible, pageIndex, pageSize, sortDirection, sortField, keyword, visibility, visibilityPos), () =>
                {
                    int total;
                    var ret = GetAllRawEx(db, langCode, categoryid, visible, pageIndex, pageSize, out total, sortDirection, sortField, keyword, visibility, false, visibilityPos);
                    var langs0 = db.DataService.GetAll<CategoryLang>();
                    var langs1 = ret.Sum(x => x.Languages.Count);
                    return new Tuple2<int, List<Category>>(total, ret);
                }, 120 * 60, true);

            totalRows = lst.Item1;
            return lst.Item2;
        }

        private static List<Category> GetAllRawEx(IDB db, string langCode, int? categoryid, bool? visible,
            int pageIndex, int pageSize, out int totalRows,
            SortDirection sortDirection = SortDirection.Descending, string sortField = "Id",
            string keyword = null, bool? visibility = null, bool admin = true, bool? visibilityPos = null)
        {
            if (keyword != null && categoryid > 0) keyword = null;

            var hSql = string.Format(@"
            select cate from {0} as cate join cate.Languages as lang
            where lang.LangCode = '{1}' ", typeof(Category).FullName, langCode);

            if (categoryid != null)
                if (categoryid == -1) hSql += " and cate.Parent is null";
                else hSql += " and cate.Parent.Id = " + categoryid;

            if (visible != null) hSql += " and lang.Visible = " + (visible.Value ? 1 : 0);

            if (visibility != null) hSql += " and cate.Visibility IN " + (visibility.Value ? "(1,3)" : "(0,2)");

            if (visibilityPos != null) hSql += " and cate.Visibility IN " + (visibilityPos.Value ? "(2,3)" : "(0,1)");

            if (!string.IsNullOrEmpty(keyword))
            {
                //hSql += string.Format(@" and lang.Name like '%{0}%'", TranslateKWord(keyword));
                List<int> Ids = ParentCateIds(db, keyword);
                string inExp = Ids.Count > 0 ? string.Format("({0})", string.Join(",", Ids.ToArray())) : "(-1)";
                hSql += " and cate.Id IN " + inExp;
            }

            if (!admin) hSql += string.Format(" and cate.PublishedDate <= '{0:yyyy-MM-dd}' ", DateTime.Now);

            #region Sorting

            switch (sortField)
            {
                case "CreatedDate.Text":
                    sortField = "CreatedDate";
                    break;
                case "PublishedDate.Text":
                    sortField = "PublishedDate";
                    break;
            }

            if (sortField == "DisplayOrder")
                hSql += string.Format(" order by cate.{0} {1}, cate.Id desc", sortField, GetDir(sortDirection));
            else
                hSql += string.Format(" order by {0} {1}", (sortField == "Name" ? "lang." : "cate.") + sortField, GetDir(sortDirection));

            #endregion

            if (FuncHsqlList != null) hSql = FuncHsqlList(db, hSql, null, null)?.Item1;

            return GetList<Category>(db, hSql, pageIndex, pageSize, out totalRows);
        }

        private static List<int> ParentCateIds(IDB db, string keyWord)
        {
            List<int> result = new List<int>();
            var args = new List<System.Tuple<string, string>>();
            var hSql = string.Format(@"
            select cate from {0} as cate join cate.Languages as lang
            where lang.Name like :keyword ", typeof(Category).FullName);
            //where lang.LangCode = '{1}' and lang.Name like '%{2}%'", typeof(Category).FullName, langCode, TranslateKWord(keyWord));
            args.Add(new System.Tuple<string, string>("keyword", "%" + keyWord + "%"));
            int totalRows = 0;
            List<Category> lstCate = GetList<Category>(db, hSql, 1, int.MaxValue, out totalRows, args.ToArray());
            if (totalRows > 0)
            {
                for (int i = 0; i < totalRows; i++)
                {
                    var cate = lstCate[i];
                    while (cate.Parent != null)
                    {
                        cate = cate.Parent;
                    }
                    result.Add(cate.Id);
                }
            }
            return result;
        }

        public static List<CategoryModel> GetAll(IDB db, int? id, int? categoryid = null, bool? visible = null, bool? visibility = null, bool admin = true)
        {
            int total;
            return GetAll(db, LanguageMgr.OptLangCode(id ?? 0), categoryid, visible, 1, Int32.MaxValue, out total,
                SortDirection.Ascending, "Name", null, visibility, admin);
        }

        public static List<Category> GetAllEx(IDB db, int? categoryid = null, bool? visible = null, bool? visibility = null, bool admin = false)
        {
            int total;
            return GetAllRaw(db, LanguageMgr.OptLangCode(0), categoryid, visible, 1, Int32.MaxValue, out total,
                SortDirection.Ascending, "Name", null, visibility, admin);
        }

        public static Dictionary<int, System.Tuple<Category, List<Category>>> GetDictionary(IDB db, int? categoryid = null, bool? visible = null, bool? visibility = null, bool admin = false)
        {
            if (admin) return GetAllEx(db, categoryid, visible, visibility, true).ToDictionary(x => x.Id, x => new System.Tuple<Category, List<Category>>(x, x.Children.ToList()));

            return Helper.DataCache<Helper.CategoryScope, Dictionary<int, System.Tuple<Category, List<Category>>>>.Get(
                string.Format("DAL.CategoryMgr.GetDictionary:{0}:{1}:{2}:{3}", LanguageMgr.OptLangCode(0), categoryid, visible, visibility),
                () =>
                {
                    var ret = GetAllEx(db, categoryid, visible, visibility, false);
                    var parDic = ret.Where(x => x.Parent != null)
                        .GroupBy(x => x.Parent.Id)
                        .ToDictionary(x => x.Key, x => x.ToList());
                    return ret.ToDictionary(x => x.Id,
                        x => new System.Tuple<Category, List<Category>>(x, parDic.SafeGet(x.Id) ?? new List<Category>()));
                }, 120 * 60, true);
        }

        #endregion

        #region Get
        public static Category Get(IDB db, string alias)
        {
            if (alias.IsNullOrEmpty()) return null;

            var obj = db.DataService.GetObject<CategoryLang>(0, "Alias", alias);

            return obj == null ? null : obj.Category;
        }

        #endregion

        #region SaveorUpdate
        public static bool SaveorUpdate(IDB db, CategoryModel model, bool useTransaction = true)
        {
            var updateLuceneIndex = false;
            if (model == null) return false;

            var obj = GetDom(db, model.Id) ?? new Category() { CreatedDate = DateTime.Now, VisibilityUpPath = 3 };

            if (obj.Id == 0 || (obj.Id > 0 && obj.Visibility != (byte)model.Visibility))
                updateLuceneIndex = true;

            #region Category

            obj.Alias = model.Alias;
            obj.Image = model.Image;
            obj.Banner0 = model.Banner0;
            obj.Banner1 = model.Banner1;
            obj.Banner2 = model.Banner2;
            obj.BannerUrl0 = model.BannerUrl0;
            obj.BannerUrl1 = model.BannerUrl1;
            obj.BannerUrl2 = model.BannerUrl2;
            obj.Visibility = (byte)model.Visibility;
            obj.DisplayOrder = model.DisplayOrder;
            obj.IncSubCate = model.IncSubCate;
            obj.PagingSz = model.PagingSz;
            obj.Sorting = model.Sorting;
            obj.ModifiedDate = DateTime.Now;
            obj.PublishedDate = ((DateTime?)model.PublishedDate) ?? DateTime.Now;

            var parent = GetDom(db, model.ParentId ?? 0);
            if (obj.Parent.GetId(0) != parent.GetId(0))
            {
                if (obj.Parent != null)
                    obj.TravelUp(x => x.Languages.Action(w => { w.ProductsSub = w.SubCates = w.ProductsBundleSub = null; w.StatusStats = null; }));

                if (null != (obj.Parent = parent))
                    parent.TravelUp(x => x.Languages.Action(w => { w.ProductsSub = w.SubCates = w.ProductsBundleSub = null; w.StatusStats = null; }), true);
            }

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
                        obj.Languages.Add(ditm = new CategoryLang()
                        {
                            Category = obj,
                            LangCode = item.LangCode,
                        });
                    }

                    ditm.Alias = item.Alias;

                    if (ditm.Alias.IsNullOrEmpty())
                    {
                        ditm.Alias = item.Name.GenerateAlias();
                        if (Exists<CategoryLang>(x => x.Id, ditm.Id, null, new KeyValuePair() { Key = "Alias", Value = ditm.Alias }))
                            ditm.Alias = Unique(db, "Alias", ditm);
                    }

                    ditm.Name = item.Name;
                    ditm.Description = item.Description;
                    ditm.PageTitle = item.PageTitle;
                    ditm.MetaDesc = item.MetaDesc;
                    ditm.Visible = item.Visible;
                }

                obj.Languages.RemoveAll(x => model.Languages.All(w => w.Id != x.Id));

                if (obj.Alias.IsNullOrEmpty())
                    obj.Alias = obj.Languages.FirstOrDefault().SafeProp(x => x.Alias);
            }
            #endregion

            #region Products

            if (model.Products != null)
            {
                foreach (var catPro in model.Products)
                {
                    #region CategoryProducts

                    var gpid = catPro.Id;
                    var dCPro = (gpid == 0 ? null : obj.CategoryProducts.FirstOrDefault(x => x.Id == gpid));
                    if (dCPro == null)
                        obj.CategoryProducts.Add(dCPro = new CategoryProduct() { Category = obj });

                    dCPro.Product = ProductMgr.GetDom(db, catPro.ProductId ?? 0);

                    #endregion
                }

                obj.Languages.Action(x =>
                {
                    x.Products = null;
                    x.ProductsBundle = null;
                    x.StatusStats = null;
                    x.Category.TravelUp(w => w.Languages.Action(k => { k.ProductsSub = k.ProductsBundleSub = null; k.StatusStats = null; }));
                });
                obj.CategoryProducts.RemoveAll(x => x.Product == null || model.Products.All(w => w.Id != x.Id));
            }

            #endregion

            if (useTransaction ? obj.SaveorUpdate(db.DataService) : SaveOrUpdate(obj, db.GetSession(), false))
            {
                model.Id = obj.Id;
                new CategoryMgr().OnUpdate(obj, updateLuceneIndex); return true;
            }
            else return false;
        }

        public static bool SaveOrUpdate(IDB db, int id, Action<Category> fCopy, bool updateLuceneIndex)
        {
            var obj = GetDom(db, id);

            if (obj != null)
            {
                fCopy(obj);
                if (obj.SaveorUpdate(db.DataService))
                {
                    new CategoryMgr().OnUpdate(obj, updateLuceneIndex);
                    return true;
                }
            }
            return false;
        }

        #endregion

        #region ChangePrice
        /// <param name="increase">false: descrease</param>
        /// <param name="percent">false: fixed value</param>
        public static Category ChangePrice(IDB db, int id, bool increase, bool percent, decimal value)
        {
            var priceResolution = SettingsBiz.GetSettings().Display.Others.PriceResolution;
            var obj = GetDom(db, id);

            if (obj == null) return null;

            var dir = increase ? 1 : -1;
            foreach (var itm in obj.CategoryProducts)
            {
                var prod = itm.Product;
                var oldPrice = prod.Price;

                if (percent) prod.Price *= 1 + (dir * value);
                else prod.Price += dir * prod.ExcVat(value) ?? 0;

                prod.Price = Math.Max(0, prod.Price);
                prod.Price = prod.Vat.NormalizePrice(prod.Price, prod.IncVat(prod.Price)) ?? 0;
                if (Math.Abs(prod.Price - prod.SalePrice ?? 0) < priceResolution) prod.SalePrice = 0;

                if (oldPrice == 0) continue;

                var ratio = (prod.Price - oldPrice) / oldPrice;

                foreach (var vari in prod.Variants.Where(x => !x.Deleted && x.Price != null))
                {
                    vari.Price = Math.Max(0, (vari.Price * (1 + ratio)) ?? 0);
                    vari.Price = prod.Vat.NormalizePrice(vari.Price, prod.IncVat(vari.Price));
                    if (Math.Abs(vari.Price - vari.SalePrice ?? 0) < priceResolution) vari.SalePrice = 0;
                }
            }

            return obj;
        }

        #endregion

        #region Move

        public static bool Move(IDB db, int categoryid, int beforeId, int type)
        {
            if (MoveRaw(db, categoryid, beforeId, type))
            {
                Category obj = GetDom(db, categoryid);
                new CategoryMgr().OnUpdate(obj);
                return true;
            }
            return false;
        }
        /// <summary>
        /// type {1:first,2:last,3:after,4:before}
        /// </summary>
        static bool MoveRaw(IDB db, int categoryid, int beforeId, int type)
        {
            Category obj = GetDom(db, categoryid), before = GetDom(db, beforeId);

            if (obj == null || (beforeId > 0 && before == null)) return false;

            if (beforeId == 0 && type >= 1 && type <= 2) before = obj.Parent;

            #region Parent

            var oParent = obj.Parent;

            switch (type)
            {
                case 1:
                case 2:
                    if (before.GetId(0) != obj.Parent.GetId(0)) obj.Parent = before;
                    break;
                case 3:
                case 4:
                    if (before != null && before.Parent.GetId(0) != obj.Parent.GetId(0))
                        obj.Parent = before.Parent;
                    break;
            }

            if (obj.Parent.GetId(0) != oParent.GetId(0))
            {
                if (obj.Parent != null) obj.TravelUp(x => x.Languages.Action(w => { w.ProductsSub = w.SubCates = w.ProductsBundleSub = null; w.StatusStats = null; }));
                if (oParent != null) oParent.TravelUp(x => x.Languages.Action(w => { w.ProductsSub = w.SubCates = w.ProductsBundleSub = null; w.StatusStats = null; }), true);
            }

            #endregion

            var hSql = string.Format(@"
            select cate from {0} as cate join cate.Languages as lang
            where lang.LangCode = '{1}' and cate.Parent.Id {2} order by cate.DisplayOrder ",
            typeof(Category).FullName, LanguageMgr.FocusLangCode,
            obj.Parent == null ? " is null" : " = " + obj.Parent.Id);

            var lst = GetList<Category>(db, hSql);

            if (lst.Count == 0)
                if (type == 1 || type == 2) return obj.SaveorUpdate(db.DataService);
                else return false;

        LB_LOOP:
            #region Move
            switch (type)
            {
                case 1:
                    obj.DisplayOrder = lst.First().DisplayOrder - 1;
                    return obj.SaveorUpdate(db.DataService);
                case 2:
                    obj.DisplayOrder = lst.Last().DisplayOrder + 1;
                    return obj.SaveorUpdate(db.DataService);
                default:
                    {
                        if (before == null) return false;
                        if (type == 4)
                        {
                            var beLst = lst
                                .Where(x => x.DisplayOrder < before.DisplayOrder || (x.DisplayOrder == before.DisplayOrder && x.Id > before.Id))
                                .OrderBy(x => x.DisplayOrder)
                                .ThenByDescending(x => x.Id).ToList();

                            if (beLst.Count == 0) { type = 1; goto LB_LOOP; }
                            else before = beLst.Last();
                        }

                        var ret = new List<Category>() { obj };
                        var minVal = before.DisplayOrder + 1;

                        foreach (var itm in lst.Where(x => x.DisplayOrder >= before.DisplayOrder && x.Id != before.Id))
                        {
                            if (itm.DisplayOrder > minVal) break;
                            else
                            {
                                minVal = itm.DisplayOrder = Math.Max(itm.DisplayOrder + 1, minVal + 1); ret.Add(itm);
                            }
                        }

                        obj.DisplayOrder = before.DisplayOrder + 1;

                        return ret.Update(db.DataService);
                    }
            }
            #endregion
        }

        #endregion

        #region OnUpdate

        protected override void OnUpdate(Category obj)
        {
            this.OnUpdate(obj, true);
        }

        protected void OnUpdate(Category obj, bool updateLuceneIndex)
        {
            ITransaction trans = null;
            var db = new DefaultDb();
            try
            {
                var session = db.GetSession();
                if (session != null && obj != null)
                {
                    trans = session.Transaction.SafeProp(x => x.IsActive) ? null : session.BeginTransaction();
                    session.SetBatchSize(100);
                    obj.InvalidateUpPathVisibility(session);
                    if (trans != null) { session.Flush(); trans.Commit(); }
                }
            }
            catch (Exception err)
            {
                if (trans != null && trans.IsActive) trans.Rollback();
                LogErr(err);
            }
            CategoryScope.Invalidate();

            if (updateLuceneIndex)
            {
                //var listProductOfCategory = new List<Domain.Product>(db.DataService.GetAll<Domain.Product>().Where(x => x.CategoryProducts.Any(m => m.Category == obj)));
                if (obj != null && (0 != (obj.Visibility & (byte)VisibilityType.Web) ||
                    obj.CategoryProducts.Count < SettingsBiz.GetSettings().Hidden.CateEditMaxProducts))
                    LuceneIndexing.IndexingService.AddUpdateLuceneIndex(obj.CategoryProducts.Select(x => ProductMgr.GetDom(db, x.Product.Id)).Where(m => m != null).ToList());
            }
        }


        #endregion

        #region OnDelete
        protected override void OnDelete(Category obj)
        {
            if (obj != null)
            {
                obj.TravelUp(x => x.Languages.Action(w => { w.ProductsSub = w.SubCates = w.ProductsBundleSub = null; w.StatusStats.Clear(); }));
                if (obj.Parent != null) obj.Parent.Update(new DefaultDb().DataService);
            }

            CategoryScope.Invalidate();
        }
        #endregion

    }
}