using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Claw.Core;
using Claw.Modules.Eshop.Controllers.Admin;
using Claw.Modules.Eshop.DAL;
using Claw.Modules.Eshop.DAL.Interfaces;
using Claw.Modules.Eshop.Helper;
using Claw.Modules.Eshop.Models;
using Claw.Modules.Eshop;
using Claw.Modules.Eshop.Domain.Interfaces;

namespace Claw.Modules.Eshop.Domain
{
    public partial class ProductBase : Tuple<CategoryProduct, Image, ProductLang, VariantBase, ProductBundle, ProductBundle, IDom, IDom, IDom, IDom, IDom, IDom, ProductAccessory, ProductAccessory, ProductEmailMUser, ProductExtraInformation, ProductTechSVal, ProductRecurDisc, IDom>, IProduct
    {
        public virtual bool Visible
        {
            get
            {
                var lang = ClawContext.Current.CurrentLanguage.LanguageCode;

                var ret = LanguagesFe.FirstOrDefault(x => x.LangCode.EqualsIgCase(lang));
                return ret != null && ret.Visible;
            }
        }

        public virtual string Name
        {
            get
            {
                var lang = ClawContext.Current.CurrentLanguage.LanguageCode;

                var ret = LanguagesFe.MatchOrFirst(x => x.LangCode.EqualsIgCase(lang));
                return ret == null ? "" : ret.Name;
            }
        }
        public virtual string AliasLang
        {
            get
            {
                var lang = ClawContext.Current.CurrentLanguage.LanguageCode;
                var ret = LanguagesFe.MatchOrFirst(x => x.LangCode.EqualsIgCase(lang));
                return ret == null ? "" : ret.Alias;

            }
        }
        public virtual string Tags
        {
            get
            {
                var lang = ClawContext.Current.CurrentLanguage.LanguageCode;

                var ret = LanguagesFe.MatchOrFirst(x => x.LangCode.EqualsIgCase(lang));
                return ret == null ? "" : ret.Tags;
            }
        }
        public virtual string ExtraInfo
        {
            get
            {
                var lang = ClawContext.Current.CurrentLanguage.LanguageCode;
                var ret = LanguagesFe.MatchOrFirst(x => x.LangCode.EqualsIgCase(lang));
                return ret == null ? "" : ret.ExtraInfo;
            }
        }

        public virtual string ExtraInfo2
        {
            get
            {
                var lang = ClawContext.Current.CurrentLanguage.LanguageCode;
                var ret = LanguagesFe.MatchOrFirst(x => x.LangCode.EqualsIgCase(lang));
                return ret == null ? "" : ret.ExtraInfo2;
            }
        }

        public virtual string ExtraInfo3
        {
            get
            {
                var lang = ClawContext.Current.CurrentLanguage.LanguageCode;
                var ret = LanguagesFe.MatchOrFirst(x => x.LangCode.EqualsIgCase(lang));
                return ret == null ? "" : ret.ExtraInfo3;
            }
        }

        public virtual string ExtraInfo4
        {
            get
            {
                var lang = ClawContext.Current.CurrentLanguage.LanguageCode;
                var ret = LanguagesFe.MatchOrFirst(x => x.LangCode.EqualsIgCase(lang));
                return ret == null ? "" : ret.ExtraInfo4;
            }
        }

        public virtual string Description
        {
            get
            {
                var lang = ClawContext.Current.CurrentLanguage.LanguageCode;
                var ret = LanguagesFe.MatchOrFirst(x => x.LangCode.EqualsIgCase(lang));
                return ret == null ? "" : ret.Description;
            }
        }
        public virtual string PageTitle
        {
            get
            {
                var lang = ClawContext.Current.CurrentLanguage.LanguageCode;
                var ret = LanguagesFe.MatchOrFirst(x => x.LangCode.EqualsIgCase(lang));
                return ret == null ? "" : ret.PageTitle;
            }
        }
        public virtual string MetaDesc
        {
            get
            {
                var lang = ClawContext.Current.CurrentLanguage.LanguageCode;
                var ret = LanguagesFe.MatchOrFirst(x => x.LangCode.EqualsIgCase(lang));
                return ret == null ? "" : ret.MetaDesc;
            }
        }

        public virtual bool CanSelfDiscount 
        {
            get { return CanApplySelfDiscount(); }
        }

        public virtual bool CanApplySelfDiscount(bool skipSelfDisc = false, bool skipCustDisc = false)
        {
            return (!skipSelfDisc && SelfDiscountAvailable && ShoppingCart.Count(Settings.Hidden.ProdSelfDiscSumAll ? (int?)null : Id) >= SelfDiscMinQty) || (!skipCustDisc && GetCustDiscountRate() > 0); 
        }

        public virtual bool SelfDiscountApplied
        {
            get { return Math.Abs(SalePriceSelfDisc - (SalePrice ?? 0)) >= 0.1m; }
        }

        public virtual bool SelfDiscountAvailable
        {
            get { return SelfDiscActive && SelfDiscValue != 0 && SelfDiscMinQty > 1 && Settings.Hidden.ProdSelfDiscount; }
        }

        public virtual decimal GetCustDiscountRate() 
        {
            return GetCustDiscountRate(null);
        }

        public virtual decimal GetCustDiscountRate(object userInfo)
        {
            if (HttpContext.Current != null && Settings.Hidden.EnableCustomerDiscount)
            {
                const string keyDic = "Claw-Cshop-CustomerDiscount";
                var dic = (Dictionary<int, decimal>)(HttpContext.Current.Items[keyDic] ?? (HttpContext.Current.Items[keyDic] = new Dictionary<int, decimal>()));

                if (dic.ContainsKey(Id)) return dic[Id];

                DateTime? modifiedDate;
                bool showPrice;
                decimal? discountRate;
                List<ICustGroup> groups;
                Dictionary<int, decimal> cateDiscountRates;

                if (DAL.Singleton<Helper.ISessionHelper>.Default.GetUserId(userInfo, out showPrice, out discountRate, out cateDiscountRates, out groups, out modifiedDate, false) > 0)
                {
                    var groupDiscRate = groups == null
                        ? 0
                        : groups.Where(m => m.DiscountRate > 0 && m.AnyCategory != null &&
                            (m.AnyCategory.Value || m.GetDiscountCategories().Any(z => this.CategoryProducts.Any(w => w.Category.Id == z.Id))))
                            .Select(m => m.DiscountRate).OrderByDescending(m => m).FirstOrDefault() ?? 0;

                    if (discountRate > 0) return dic[Id] = Math.Max(groupDiscRate, discountRate ?? 0);

                    var cateDiscountRate = 0m;
                    if (cateDiscountRates != null && cateDiscountRates.Count != 0)
                        cateDiscountRate = CategoryProducts
                            .Where(m => cateDiscountRates.ContainsKey(m.Category.Id))
                            .Select(m => cateDiscountRates[m.Category.Id]).OrderByDescending(m => m).FirstOrDefault();

                    return dic[Id] = Math.Max(groupDiscRate, cateDiscountRate);
                }
            }

            return 0;
        }

        public virtual decimal SelfDiscountPrice { get { return GetSelfDiscountPrice(); } }

        public virtual decimal GetSelfDiscountPrice(bool skipCustDisc = false)
        {
            var settings = Settings.Hidden;
            var combinedPrice = SalePrice > 0 ? Math.Min(SalePrice ?? 0, Price) : Price;
            var combinedPriceCust = settings.CustomerDiscountUseSalePrice ? combinedPrice : Price;
            var combinedPriceSelf = settings.CustomerDiscountUseSalePrice ? combinedPrice : Price;

            decimal newSalePriceSelf = combinedPrice, newSalePriceCust = combinedPrice;

            if (CanApplySelfDiscount(false, true))
                newSalePriceSelf = Math.Max(0m, combinedPriceSelf - (SelfDiscValue > 0 ? (this.ExcVat(SelfDiscValue) ?? 0) : Math.Abs(SelfDiscValue * combinedPrice)));

            if (!skipCustDisc && CanApplySelfDiscount(true))
                newSalePriceCust = Math.Max(0m, combinedPriceCust * (1 - GetCustDiscountRate()));

            newSalePriceSelf = newSalePriceSelf > 0 && newSalePriceSelf < Price ? newSalePriceSelf : 0;
            newSalePriceCust = newSalePriceCust > 0 && newSalePriceCust < Price ? newSalePriceCust : 0;

            return newSalePriceSelf > 0 && newSalePriceCust > 0 ? Math.Min(newSalePriceSelf, newSalePriceCust) : Math.Max(newSalePriceSelf, newSalePriceCust);
        }

        public virtual decimal SelfDiscountPriceInc
        {
            get { return this.IncVat(SelfDiscountPrice) ?? 0; }
        }

        private decimal SalePriceSelfDisc
        {
            get { return CanSelfDiscount ? SelfDiscountPrice : (SalePrice ?? 0); }
        }

        private decimal SalePriceSelfDiscInc
        {
            get { return this.IncVat(SalePriceSelfDisc) ?? 0; }
        }

        public virtual System.Tuple<int, decimal> CGroupDiscRate(VariantBase variant = null)
        {
            var flagCgDiscNoVari = (int)ProductFlags.CGroupDiscountNoVariant;
            var cposApi = HttpContext.Current?.Items.Contains("Cpos-Api") == true;
            var groups = CustGroups;

            if (groups == null || groups.Count == 0 || cposApi) return null;

            var discByGroup = Settings.Hidden.CustomerDiscountByGroup;
            var lst = groups.Where(m => !discByGroup || m.Discount).Join(new[]
            {
                new { Id = CGroupDisc0Id, Rate = CGroupDisc0Rate }, new { Id = CGroupDisc1Id, Rate = CGroupDisc1Rate }, 
                new { Id = CGroupDisc2Id, Rate = CGroupDisc2Rate },
                new {
                    Id = (variant == null ? null : variant.CGroupDisc3Id) ?? CGroupDisc3Id,
                    Rate = (variant == null ? null : variant.CGroupDisc3Rate) ?? (variant == null || 0 == (Flags & flagCgDiscNoVari) ? CGroupDisc3Rate : null)
                },
                new { Id = CGroupDisc4Id, Rate = CGroupDisc4Rate },
            }.Where(m => m.Id != null && m.Rate != null && m.Rate != 0), o => o.Id, i => i.Id, (o, i) => new System.Tuple<int, decimal>(o.Id, i.Rate ?? 0)).ToList();

            var combinedPrice = SalePrice > 0 && Settings.Hidden.CustomerDiscountUseSalePrice ? Math.Min(SalePrice ?? 0, Price) : Price;

            return lst.Select(m => new
            {
                Item = m, Value = combinedPrice - (m.Item2 >= 0 ? (this.ExcVat(m.Item2) ?? 0) : combinedPrice*Math.Abs(m.Item2))
            })
            .OrderBy(m => m.Value)
            .FirstOrDefault()
            .SafeProp(m => m.Item);
        }

        private decimal? SalePriceCGroupDisc
        {
            get
            {
                var settings = Settings.Hidden;

                if (!settings.EnableCustomerDiscount) return null;

                var cgDiscRate = CGroupDiscRate();

                var newSalePriceSelf = GetSelfDiscountPrice(true);

                var combinedPrice = settings.CustomerDiscountUseSalePrice && SalePrice > 0 ? Math.Min(SalePrice ?? 0, Price) : Price;
                var combinedPriceCgDisc = settings.CustomerDiscountUseSalePrice ? combinedPrice : Price;

                var newSalePriceCgDisc = cgDiscRate == null ? 0 : combinedPriceCgDisc - (cgDiscRate.Item2 >= 0 ? (this.ExcVat(cgDiscRate.Item2) ?? 0) : combinedPriceCgDisc * Math.Abs(cgDiscRate.Item2));
                
                var newSalePrice = newSalePriceSelf > 0 && newSalePriceCgDisc > 0
                    ? Math.Min(newSalePriceSelf, newSalePriceCgDisc)
                    : Math.Max(newSalePriceSelf, newSalePriceCgDisc);

                return newSalePriceCgDisc > 0 && newSalePrice > 0 && newSalePrice < combinedPrice ? newSalePrice : (decimal?)null;
            }
        }

        public virtual bool HasSalePrice
        {
            get
            {
                var coupon = ShoppingCart.SafeProp(m => m.Coupon);

                if (coupon != null && coupon.Exclusive && coupon.DiscountType == 1 /*MoneyTotal*/) 
                    return false;

                var salePrice = SalePriceCGroupDisc;
                
                if (salePrice > 0 && (this.IncVat(salePrice) ?? 0).ToCeilling() < PriceInc.ToCeilling()) return true;
                
                salePrice = SalePriceSelfDisc;
                
                return salePrice > 0 && (this.IncVat(salePrice) ?? 0).ToCeilling() < PriceInc.ToCeilling();
            }
        }

        public virtual bool HasMembershipPrice
        {
            get { return Singleton<IMembershipMgr>.Default.GetDiscountRate(ShoppingCart) > 0m; }
        }

        public virtual decimal NonMemberPriceFE
        {
            get
            {
                var salePrice0 = SalePriceCGroupDisc ?? 0;
                var salePrice1 = HasSalePrice ? SalePriceSelfDisc : 0;
                return salePrice0 > 0 && salePrice1 > 0
                    ? Math.Min(Math.Min(salePrice0, salePrice1), Price)
                    : (salePrice0 > 0 || salePrice1 > 0 ? Math.Min(Math.Max(salePrice0, salePrice1), Price) : Price);
            }
        }

        public virtual decimal NonMemberPriceFEInc
        {
            get
            {
                return (decimal)this.IncVat(NonMemberPriceFE);
            }
        }

        public virtual decimal PriceFE
        {
            get
            {
                var coupon = ShoppingCart.SafeProp(m => m.Coupon);

                if (coupon != null && coupon.Exclusive && coupon.DiscountType == 1 /*MoneyTotal*/) return Price;

                var saleprice0 = NonMemberPriceFE;
                var combinedPrice = SalePrice > 0 && Settings.Hidden.CustomerDiscountUseSalePrice ? Math.Min(SalePrice ?? 0, Price) : Price;
                var saleprice1 = combinedPrice*(1 - Singleton<IMembershipMgr>.Default.GetDiscountRate(ShoppingCart));

                return Math.Min(saleprice0, saleprice1); 
            }
        }

        public virtual decimal PriceFEInc
        {
            get
            {
                return (decimal)this.IncVat(PriceFE);
            }
        }

        public virtual decimal? PriceFEInc2
        {
            get
            {
                return SalePrice2 > 0 && SalePrice2 < Price ? SalePriceInc2 : (decimal?)null;
            }
        }

        public virtual decimal? TechSpecCostExtraInc { get { return this.IncVat(TechSpecCostExtra); } }

        public virtual decimal PercentDiscount
        {
            get
            {
                return HasSalePrice || HasMembershipPrice ? Eshop.Product.Helper.EShopHelper.PercentDiscount(PriceInc, PriceFEInc) : 0;
            }
        }

        public virtual decimal PriceInc
        {
            get { return (decimal)this.IncVat(Price); }
        }

        public virtual decimal SalePriceInc
        {
            get { return (decimal)this.IncVat(SalePrice ?? 0); }
        }
        public virtual decimal SalePriceInc2
        {
            get { return (decimal)this.IncVat(SalePrice2 ?? 0); }
        }

        public virtual decimal CostPriceInc
        {
            get { return (decimal)this.IncVat(CostPrice ?? 0); }
        }

        public virtual int ProductStatusId
        {
            get { return ProductStatus.Id; }
        }

        public virtual bool ValidToShow()
        {
            var ctx = ClawContext.Current;
            var langCode = ctx != null ? ctx.CurrentLanguage.LanguageCode : LanguageMgr.FocusLangCode;

            return ValidToShow(langCode);
        }

        public virtual bool ValidToShow(string langCode)
        {
            int errQty;
            return ValidToShow(langCode, 0, out errQty);
        }

        public virtual bool ValidToShow(string langCode, int minQty, out int errQty)
        {
            return ValidToShow(langCode, minQty, out errQty, null, null, null);
        }

        public virtual bool ValidToShow(string langCode, int minQty, out int errQty, List<System.Tuple<int, string>> items, 
            List<System.Tuple<int, int, int, string>> errQtyProds, Dictionary<string, int> dicQty)
        {
            if (langCode == null)
            {
                var ctx = ClawContext.Current;
                langCode = ctx != null ? ctx.CurrentLanguage.LanguageCode : LanguageMgr.FocusLangCode;
            }

            if (AllowBackorder || minQty == 0) errQty = 0;
            else
            {
                if (items.SafeCount() != 0 && dicQty != null && Type.InList(ProductType.Bundle) && Accessories.Any(m => m.BundleGroup != null))
                {
                    var tmpErrQty = 0;
                    items.Action(z =>
                    {
                        var dataExp = (IDictionary<string, object>)z.Item2.ToExpando();
                        var bundlevariantIds = dataExp != null
                            ? ((string)dataExp.SafeGet("bundlevariantids") ?? "").Split(',').Select(m => m.TryParseInt() ?? 0).Where(m => m > 0).ToList()
                            : new List<int>();
                        var bundleAccessoryIds = dataExp != null
                            ? ((string)dataExp.SafeGet("bundleaccessoryids") ?? "").Split(',').Where(m => !m.IsNullOrEmpty()).Select(m => m.IndexOf('-') == -1 ? new[] { int.Parse(m) } : m.Split('-').Select(int.Parse).ToArray()).ToList()
                            : new List<int[]>();
                        var key = string.Format("{0}:{1}", Id, bundlevariantIds.OrderBy(m => m).ToString("-"));
                        var maxQty = this.GetMaxQuantity(bundlevariantIds, bundleAccessoryIds, dicQty);

                        if (!dicQty.ContainsKey(key)) dicQty.Add(key, maxQty.Item1);

                        var qty = dicQty[key];

                        qty = Math.Min(qty, maxQty.Item2);
                        if (errQtyProds != null && z.Item1 > qty)
                        {
                            tmpErrQty += z.Item1 - qty;
                            errQtyProds.Add(new System.Tuple<int, int, int, string>(Id, 0, z.Item1 - qty, z.Item2));
                        }
                        dicQty[key] = dicQty[key] - Math.Min(qty, z.Item1);
                        maxQty.Item3.Action(m => m(Math.Min(qty, z.Item1)));
                    });
                    errQty = tmpErrQty;
                }
                else errQty = minQty - Quantity - (DropshipQty ?? 0);
            }

            if (0 != (Visibility & (byte) VisibilityType.Web) && DateTime.Now >= PublishedDate && ProductStatus.ShowFrontEnd)
            {
                var hidden = Settings.Hidden;
                if ((Quantity + (DropshipQty ?? 0)) >= (minQty > 0 ? 0 : 1) || AllowBackorder || (minQty == 0 && hidden.ShowZeroQtyProduct && !(ProductStatus.GetId(0) ?? 0).InList(hidden.HideZeroQtyProdStatusIds())))
                {
                    if (Type.InList(ProductType.Bundle) && (ProductBundles.Count == 0 || ProductBundles.Any(m => !m.Product.ValidToShow() || (m.Variant != null && !m.Variant.ValidToShow()))))
                        return false;

                    return LanguagesFe.Any(c => c.LangCode.EqualsIgCase(langCode) && c.Visible);
                }
            }

            return false;
        }

        public virtual string GetImageFrist
        {
            get
            {
                var fileName = "/Claw/Sites/Default/Modules/Eshop/Images/Product/no-img.jpg";
                if (Image != null)
                {
                    fileName = Image.ValidNHibernateProxy().FileName;
                }
                return ModuleUrl.ImagePath(fileName).TrimStart('~');
            }
        }

        public virtual string GetImageThumbFrist
        {
            get
            {
                var fileName = "/Claw/Sites/Default/Modules/Eshop/Images/Product/no-thumb-img.jpg";
                if (Image != null)
                {
                    fileName = Image.ValidNHibernateProxy().FileName;
                }
                return ModuleUrl.ImageThumbPath(fileName).TrimStart('~');
            }
        }

        public virtual string GetAttachment
        {
            get
            {
                return !string.IsNullOrEmpty(AttachFile) ? ModuleUrl.AttachmentPath(AttachFile).TrimStart('~') : "";
            }
        }

        public virtual bool IsFeatured { get { return (Flags & (int)ProductFlags.Featured) != 0; } }

        public virtual bool IsAccessory { get { return (Flags & (int)ProductFlags.Accessory) != 0; } }
    }
}