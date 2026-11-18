using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Web;
using System.Web.Mvc;
using AutoMapper;
using Claw.Core;
using Claw.Core.Domain;
using Claw.Modules.Eshop.Controllers.Admin;
using Claw.Modules.Eshop.DAL;
using Claw.Modules.Eshop.Domain;
using Claw.Modules.Eshop.Domain.Interfaces;
using Claw.Modules.Eshop.Helper;
using Claw.Modules.Eshop.Models;
using Kendo.Mvc.Infrastructure;
using Newtonsoft.Json;
using NHibernate;
using NHibernate.Loader.Custom;
using NHibernate.Mapping;
using Claw.Modules.Eshop.DAL.Interfaces;
using NPOI.SS.UserModel;
using Gma.DataStructures.StringSearch;

namespace Claw.Modules.Eshop
{
    public static class DomExtensions
    {
        #region ComputeVat
        /// <summary>
        ///  vat after discount/coupon
        /// </summary>
        public static decimal ComputeVat(this decimal orderTotal, decimal vatTotal, decimal subVatTotal, decimal paymentCost,
            decimal paymentVat, decimal shippingCost, decimal shippingVat, decimal totalDiscount, decimal totalCoupon)
        {
            var allSumTotal = orderTotal + vatTotal + paymentCost + shippingCost;
            var finalTotal = allSumTotal - totalDiscount - totalCoupon;
            var allSumVat = subVatTotal;//vatTotal + (paymentCost * paymentVat) / (1 + paymentVat) +
            //(shippingCost * shippingVat) / (1 + shippingVat);

            if (allSumTotal == 0) return 0;

            return (finalTotal * allSumVat) / allSumTotal;
        }
        #endregion

        #region GetVat

        public static decimal? GetVat(this Domain.ProductBase obj)
        {
            if (obj == null || obj.Vat == null) return null;

            return obj.Vat.Value;
        }

        #endregion

        #region IncVat

        public static decimal? IncVat(this Domain.ProductBase obj, decimal? value, decimal? vat)
        {
            if (obj == null || value == null) return null;

            return (1.0m + (vat ?? obj.Vat.Value)) * (value ?? 0m);
        }

        public static decimal? IncVat(this Domain.ProductBase obj, decimal? value)
        {
            if (obj == null || value == null) return null;

            return (1.0m + obj.Vat.Value) * (value ?? 0m);
        }

        public static OMoney IncVat(this ProductModel obj, OMoney value)
        {
            if (obj == null || value == null || obj.Vat == null) return null;

            if (value.Value == null) return new OMoney();

            return new OMoney((decimal)(1.0 + obj.Vat.Value) * (value.Value ?? 0m));
        }

        public static OMoney IncVat(this ProductExportModel obj, OMoney value)
        {
            if (obj == null || value == null) return null;

            if (value.Value == null) return new OMoney();

            return new OMoney((decimal)(1.0 + obj.Vat / 100d) * (value.Value ?? 0m));
        }

        public static decimal? IncVat(this VariantBase obj, decimal? value)
        {
            if (obj == null || value == null) return null;

            return (1.0m + obj.Product.Vat.Value) * (value ?? 0m);
        }

        #endregion

        #region ExcVat

        public static decimal? ExcVat(this Domain.ProductBase obj, decimal? value, decimal? vat)
        {
            if (obj == null || value == null) return null;

            return (value ?? 0m) / (1.0m + (vat ?? obj.Vat.Value));
        }

        public static decimal? ExcVat(this Domain.ProductBase obj, decimal? value)
        {
            if (obj == null || value == null) return null;

            return (value ?? 0m) / (1.0m + obj.Vat.Value);
        }

        public static OMoney ExcVat(this ProductModel obj, OMoney value)
        {
            if (obj == null || value == null || obj.Vat == null) return null;

            if (value.Value == null) return new OMoney();

            return new OMoney((value.Value ?? 0m) / (decimal)(1.0 + obj.Vat.Value));
        }

        public static OMoney ExcVat(this ProductExportModel obj, OMoney value)
        {
            if (obj == null || value == null) return null;

            if (value.Value == null) return new OMoney();

            return new OMoney((value.Value ?? 0m) / (decimal)(1.0 + obj.Vat / 100d));
        }

        public static decimal? ExcVat(this Vat obj, decimal? value)
        {
            if (value == null) return null;

            return (value ?? 0m) / (1.0m + obj.Value);
        }

        #endregion

        #region NormalizePrice

        public static decimal? NormalizePrice(this Vat vat, OMoney price, OMoney priceInc)
        {
            var setting = SettingsBiz.GetSettings().Display.Others;
            return setting.PriceFocusInc
                ? vat.ExcVat(((decimal?)priceInc).RoundEx(setting.PriceRound))
                : ((decimal?)price).RoundEx(setting.PriceRound);
        }

        #endregion

        #region ApplySalePriceToVariants

        public static void ApplySalePriceToVariants(this Domain.ProductBase obj, bool clear = false, decimal? earnRate = null)
        {
            obj.ApplySalePriceToVariantsEx(false, clear, earnRate);
        }

        public static void ApplySalePriceToVariantsEx(this Domain.ProductBase obj, bool firstDisc, bool clear = false, decimal? earnRate = null, bool checkQty = false, IDiscount discount = null)
        {
            if (obj == null || obj.Price == 0 || obj.Variants.Count == 0) return;

            var isScopePos = discount?.IsScopePos == true;
            if (clear) obj.Variants.Where(x => !x.Deleted).Action(x => 
            {
                if (isScopePos) x.SalePrice2 = 0;
                else
                {
                    x.SalePrice = 0/*x.SalePriceBk ?? 0*/; x.SalePriceBk = null;
                    x.CGroupDisc3Id = null; x.CGroupDisc3Rate = null;
                }
            });
            else
            {
                var priceResolution = SettingsBiz.GetSettings().Display.Others.PriceResolution;
                var ratio = earnRate ?? ((obj.Price - (isScopePos ? obj.SalePrice2 : obj.SalePrice)) / obj.Price);
                if (ratio > 0)
                {
                    foreach (var vari in obj.Variants.Where(x => !x.Deleted && (earnRate == null || x.CostPrice != null)))
                    {
                        if (!checkQty || (vari.Quantity > 0 || obj.Discount?.Apply2InstockOnly == false))
                        {
                            if (firstDisc) vari.SalePriceBk = vari.SalePrice;

                            var newSalePrice = earnRate != null ? vari.CostPrice * (1 + earnRate) : vari.Price * (1 - ratio);

                            if (isScopePos)
                            {
                                vari.SalePrice2 = obj.Vat.NormalizePrice(newSalePrice, obj.IncVat(newSalePrice));
                                if (((vari.Price ?? 0) - (vari.SalePrice2 ?? 0)) < priceResolution) vari.SalePrice2 = 0;
                            }
                            else
                            {
                                vari.SalePrice = obj.Vat.NormalizePrice(newSalePrice, obj.IncVat(newSalePrice));
                                if (((vari.Price ?? 0) - (vari.SalePrice ?? 0)) < priceResolution) vari.SalePrice = 0;
                            }
                        }
                        else 
                            if (isScopePos) vari.SalePrice2 = 0;
                            else { vari.SalePrice = 0/*vari.SalePriceBk ?? 0*/; vari.SalePriceBk = null; }
                    }
                }
            }
        }

        #endregion

        #region ToKg

        public static decimal ToKg(this decimal value, WeightUnit srcUnit)
        {
            if (srcUnit == WeightUnit.Kg) return value;

            switch (srcUnit)
            {
                case WeightUnit.Gram:
                    return value / 1000;
                case WeightUnit.Lbs:
                    return value * 0.453592m;
                default:
                    if (Debugger.IsAttached) Debugger.Break();
                    return 0;
            }
        }

        #endregion

        #region SubPrice

        public static decimal? SubPrice(this decimal value, bool money, decimal delta)
        {
            if (money) return value - delta;
            else return value * (1m - delta);
        }

        #endregion

        #region CombinedPrice

        public static decimal? CombinedPrice(this Domain.ProductBase obj, VariantBase variant = null)
        {
            if (variant != null) return variant.Price; 
            else return obj.Price;
        }

        public static decimal? CombinedCostPrice(this Domain.ProductBase obj, VariantBase variant = null)
        {
            if (variant != null) return variant.CostPrice; //return variant.CombinedCostPrice();
            else return obj.CostPrice;
        }

        public static decimal? CombinedCostPriceUseDefPrice(this Domain.ProductBase obj, VariantBase variant, decimal coef = 0.8m)
        {
            if (variant != null) return variant.CostPrice.NullIfZero() ?? (variant.Price * coef);
            else return obj.CostPrice ?? (obj.Price * coef);
        }

        public static decimal? CombinedSalePrice(this Domain.ProductBase obj, VariantBase variant = null)
        {
            return obj.CombinedSalePrice(variant, true);
        }

        public static decimal? CombinedSalePrice(this Domain.ProductBase obj, VariantBase variant, bool fallback)
        {
            if (variant != null) return fallback ? variant.CombinedSalePrice() : variant.SalePrice;
            else return fallback ? obj.SalePrice.Combine(obj.Price) : obj.SalePrice;
        }

        public static decimal? CombinedSalePriceInc(this Domain.ProductBase obj, VariantBase variant = null)
        {
            if (variant != null) return variant.CombinedSalePriceInc();
            else return obj.IncVat(obj.CombinedSalePrice());
        }

        public static decimal? CombinedCostPrice(this VariantBase obj)
        {
            return obj.CostPrice.Combine(obj.Product == null ? null : obj.Product.CostPrice);
        }

        public static decimal? CombinedPrice(this VariantBase obj)
        {
            return obj.Price.Combine(obj.Product == null ? (decimal?)null : obj.Product.Price);
        }

        public static decimal? CombinedPriceInc(this VariantBase obj)
        {
            return obj.IncVat(obj.CombinedPrice());
        }

        public static decimal? CombinedSalePrice(this VariantBase obj)
        {
            return obj.SalePrice.Combine(obj.Price)
                .Combine(obj.Product == null ? null : obj.Product.CombinedSalePrice());
        }

        public static decimal? CombinedSalePriceInc(this VariantBase obj)
        {
            return obj.IncVat(obj.CombinedSalePrice());
        }
        #endregion

        #region CombinedWeight

        public static decimal? CombinedWeight(this Domain.ProductBase obj, VariantBase variant = null)
        {
            if (variant == null) return obj.Weight;
            else return variant.Weight.Combine(obj.Weight);
        }

        #endregion

        #region CombinedSKU
        public static string CombinedSKU(this Domain.ProductBase obj, VariantBase variant = null)
        {
            return variant == null ? obj.SKU : variant.SKU;
        }
        #endregion

        #region CombinedBarCode
        public static string CombinedBarCode(this Domain.ProductBase obj, VariantBase variant = null)
        {
            return variant == null ? obj.BarCode : variant.BarCode;
        }
        #endregion

        #region CombinedNvitCode
        public static string CombinedNvitCode(this Domain.ProductBase obj, VariantBase variant = null)
        {
            return obj.NvitCode;
        }
        #endregion

        #region Filter
        public static IEnumerable<VariantPropVal> Filter(this IEnumerable<VariantPropVal> list, int propId)
        {
            if (propId == 0 || list == null) return new List<VariantPropVal>();

            return list.Select(k => k.Variant.VariantPropVals.FirstOrDefault(w => w.PropVal.Property.Id == propId))
                .Where(k => k != null);
        }
        #endregion

        #region ToPropertyTree

        public static List<PropValModel.Node> ToPropertyTree(this List<VariantPropVal> list, int index, List<Domain.Property> props)
        {
            if (list == null || index >= props.Count) return null;

            var lastDeep = index == props.Count - 1;

            return list.GroupBy(x => x.PropVal.Id).Select(x =>
            {
                var lst = x.ToList();
                var fItm = lst.First().PropVal;
                var propId = index + 1 < props.Count ? props[index + 1].Id : 0;
                var varPVals = x.Filter(propId).ToList();
                var pvVariant = lastDeep ? x.Filter(props[index].Id).FirstOrDefault() : null;

                return new
                {
                    PVal = fItm,
                    PValName = fItm.GetName(),
                    Children = ToPropertyTree(varPVals, index + 1, props),
                    VariantId = pvVariant != null ? pvVariant.Variant.Id : (int?)null,
                    Items = lst,
                    QtyStatuses = lst.GroupBy(z => z.Variant.ProductStatus.Id, (key, lst2) =>
                    {
                        var lst3 = lst2.ToList();
                        return new
                        {
                            StatusId = key,
                            lst3[0].Variant.ProductStatus.EnableBuy,
                            lst3[0].Variant.ProductStatus.ContactLink,
                            lst3[0].Variant.BackDate,
                            SumQty = !lst3[0].Variant.ProductStatus.ShowFrontEnd ? 0 : lst3.Sum(w => w.Variant.Quantity + (w.Variant.DropshipQty ?? 0))
                        };
                    }),
                };
            }).OrderBy(x => x.PVal.DisplayOrder).ThenBy(x => x.PValName)
            .Select(x => new PropValModel.Node()
            {
                Id = x.PVal.Id,
                Code = x.PVal.Code,
                Name = x.PValName,
                Icon = ModuleUrl.ImagePath(x.PVal.Icon).ResolveClientUrl(),
                Children = x.Children,
                VariantId = x.VariantId,
                PriceOffset = x.Items.OrderBy(z => z.Variant.PriceInc).Take(1).Select(z => z.Variant.PriceInc - z.Variant.Product.PriceInc).FirstOrDefault(),
                QtyStatuses = x.QtyStatuses.Select(z => new PropValModel.Node.QtyStatus()
                {
                    StatusId = z.StatusId,
                    SumQty = z.SumQty,
                    EnableBuy = (z.BackDate.HasValue) ? true: z.EnableBuy,
                    ContactLink = z.ContactLink,
                    BackDate = z.BackDate.HasValue,
                }).ToList()
            })
            .ToList();
        }

        #endregion

        #region GetName

        public static string GetName(this Category obj)
        {
            return obj.GetName(null);
        }

        public static string GetName(this Category obj, string langCode)
        {
            if (obj == null) return string.Empty;

            langCode = langCode ?? LanguageMgr.FocusLangCode;
            var gLang = obj.Languages.MatchOrFirst(m => m.LangCode.EqualsIgCase(langCode));

            return gLang == null ? string.Empty : gLang.Name;
        }

        public static string GetName(this Domain.ProductBase obj, VariantBase variant = null)
        {
            return obj.GetName(variant, null);
        }

        public static string GetName(this Domain.ProductBase obj, VariantBase variant, string langCode)
        {
            if (obj == null) return string.Empty;

            langCode = langCode ?? LanguageMgr.FocusLangCode;
            var gLang = obj.Languages.MatchOrFirst(m => m.LangCode.EqualsIgCase(langCode));
            var propDesc = variant == null ? "" : " [" + variant.GetPropDesc(langCode) + "]";
            return gLang == null ? string.Empty : gLang.Name + propDesc;

        }

        public static string GetName(this Status obj)
        {
            if (obj == null) return string.Empty;

            var langCode = LanguageMgr.FocusLangCode;
            var gLang = obj.Languages.MatchOrFirst(m => m.LangCode.EqualsIgCase(langCode));

            return gLang == null ? string.Empty : gLang.Name;
        }

        public static string GetName(this Domain.Property obj, string lang = null)
        {
            if (obj == null) return string.Empty;

            var langCode = lang ?? LanguageMgr.FocusLangCode;
            var gLang = obj.Languages.MatchOrFirst(m => m.LangCode.EqualsIgCase(langCode));

            return gLang == null ? string.Empty : gLang.Name;
        }

        public static string GetName(this PropVal obj, string lang = null)
        {
            if (obj == null) return string.Empty;

            var langCode = lang ?? LanguageMgr.FocusLangCode;
            var gLang = obj.Languages.MatchOrFirst(m => m.LangCode.EqualsIgCase(langCode));

            return gLang == null ? string.Empty : gLang.Name;
        }
        public static string GetName(this Supplier obj)
        {
            if (obj == null) return string.Empty;

            return obj.Name;
        }

        public static string GetName(this Manufacturer obj)
        {
            if (obj == null) return string.Empty;

            return obj.Name;
        }
        #endregion

        #region GetPropDesc

        public static string GetPropDesc(this VariantBase obj)
        {
            return obj.GetPropDesc(null);
        }

        public static string GetPropDesc(this VariantBase obj, string langCode)
        {
            if (obj == null) return string.Empty;

            var ret = new StringBuilder();
            langCode = langCode ?? LanguageMgr.FocusLangCode;

            foreach (var val in obj.VariantPropVals
                .OrderBy(x => x.PropVal.Property.DisplayOrder).ThenBy(x => x.Id))
            {
                if (ret.Length != 0) ret.Append(", ");
                ret.AppendFormat("{0}: {1}", val.PropVal.Property.GetName(langCode), val.PropVal.GetName(langCode));
            }

            return ret.ToString();
        }

        #endregion

        #region GetCategoryAlias
        public static string GetCategoryAlias(this Domain.ProductBase obj, Language lang = null)
        {
            if (obj == null) return string.Empty;

            if (ProductMgr.FuncGetCategoryAlias != null) 
                return ProductMgr.FuncGetCategoryAlias(obj, lang);

            var cate = obj.CategoryProducts.FirstOrDefault();
            
            var langCode = lang != null ? lang.LanguageCode : LanguageMgr.FocusLangCode;

            if (cate == null) return string.Empty;

            var catelang = cate.Category.LanguagesFe.MatchOrFirst(x => x.LangCode.EqualsIgCase(langCode));

            return catelang == null ? string.Empty : catelang.Alias;
        }
        #endregion
        #region GetImageThumbnail 
        public static string GetThumbnailImage(this Domain.ProductBase obj, Language lang = null)
        {
            if (obj == null) return string.Empty;
            return obj.Image.ImageThumbUrl;
        }
        #endregion

        #region GetProducts
        public static List<Domain.ProductBase> GetProducts(this Category obj, List<int> productIds)
        {
            var ret = new List<Domain.ProductBase>();

            if (obj == null || productIds == null || productIds.Count == 0) return ret;

            ret.AddRange(obj.CategoryProducts.Where(x => productIds.Contains(x.Product.Id)).Select(x => x.Product));

            foreach (var child in obj.Children)
            {
                ret.AddRange(child.GetProducts(productIds));
            }

            return ret;
        }
        #endregion

        #region GetImage

        public static ImageModel GetImage(this Domain.ProductBase obj, VariantBase variant)
        {
            if (obj == null) return null;

            var img = obj.Image;

            if (variant != null && !string.IsNullOrEmpty(variant.Image))
            {
                img = new Image() { FileName = variant.Image, Product = obj };
            }

            return img == null ? null : Mapper.Map<Image, ImageModel>(img);
        }

        #endregion

        #region GetBundleCount

        public static int GetBundleCount(this Domain.ProductBase obj)
        {
            if (obj == null) return 0;

            if (obj.BundleNum == null) obj.BundleNum = obj.ProductBundles.Count;

            return obj.BundleNum ?? 0;
        }

        #endregion

        #region GetImages

        public static List<ImageModel> GetImages(this Domain.ProductBase obj)
        {
            if (obj == null || obj.Images.Count == 0) return new List<ImageModel>();

            return Mapper.Map<List<Image>, List<ImageModel>>(obj.Images.ToList());
        }

        public static List<VariantImageModel> GetImages(this VariantBase obj)
        {
            if (obj == null || obj.Images.Count == 0) return new List<VariantImageModel>();

            return Mapper.Map<List<VariantImage>, List<VariantImageModel>>(obj.Images.OrderBy(x => x.DisplayOrder).ToList());
        }

        #endregion

        #region InitProductImages

        public static void InitProductImages(this CategoryModel obj, IDB db)
        {
            if (obj == null || obj.Products == null || obj.Products.Count == 0) return;

            foreach (var prod in obj.Products)
            {
                var product = ProductMgr.GetDom(db, prod.ProductId ?? 0);
                if (product == null) continue;
                prod.ProductName = product.GetName();
                prod.ProductImage = Mapper.Map<Image, ImageModel>(product.Image);
            }
        }

        #endregion

        #region GetProducts

        public static List<CategoryProductModel> GetProducts(this Category obj, IDB db)
        {
            if (obj == null /*|| obj.CategoryProducts.Count == 0*/) return new List<CategoryProductModel>();
            
            var lst = BaseMgr.GetList<object[]>(db, string.Format(@"
            select prod, img, lang.Name, cprod.Id
            from {0} as cprod join cprod.Product as prod
                left join prod.Image as img
                join prod.Languages as lang
            where lang.LangCode = '{1}' and cprod.Category.Id = {2} ", typeof(CategoryProduct).FullName, LanguageMgr.FocusLangCode, obj.Id))
            .Select(m => new { Product = (ProductBase)m[0], Img = (Image)m[1], Name = (string)m[2], Id = (int)m[3] }).ToList();

            lst.Where(x => !x.Product.Deleted).Select(x => x.Product).ToList().ComputeTotalSales(db);

            return lst.Select(m =>
            {
                var tmp = Mapper.Map<CategoryProduct, CategoryProductModel>(new CategoryProduct()
                {
                    Category = obj, Product = m.Product, Id = m.Id
                });

                tmp.ProductName = m.Name;
                tmp.ProductImage = m.Img == null ? null : Mapper.Map<Image, ImageModel>(m.Img);

                return tmp;
            }).ToList();
        }

        #endregion

        #region GetVariants

        public static List<VariantModel> GetVariants(this Domain.ProductBase obj)
        {
            if (obj == null || obj.Variants.Count == 0) return new List<VariantModel>();

            if(SettingsBiz.GetSettings().Hidden.ShoppingProduct)
                ShoppingProductMgr.BindStats(new DefaultDb(), obj.Variants.Where(x => !x.Deleted).ToList());

            return Mapper.Map<List<VariantBase>, List<VariantModel>>(obj.Variants.Where(x => !x.Deleted).ToList());
        }

        #endregion

        #region GetProperties

        public static object GetProperties(this List<VariantModel> list)
        {
            if (list == null || list.Count == 0) return new List<object>();

            return list.SelectMany(x => x.Properties)
                .Where(x => x.PropValId != null)
                .GroupBy(x => x.PropValId)
                .Select(x => x.First())
                .GroupBy(x => x.PropId)
                .Select(x => new
                {
                    Id = x.Key,
                    Name = x.First().Property.Name,
                    PropVals = x.Select(m => new { Id = m.PropValId, Name = m.PropValName }).ToList()
                }).ToList();
        }

        #endregion

        #region GetFirstVariant

        public static int? GetFirstVariant(this List<PropValModel.Node> tree)
        {
            if (tree == null || tree.Count == 0) return null;

            var node = tree[0];

            if (node.Children.SafeCount() == 0) return node.VariantId;
            else return node.GetFirstVariant();
        }

        public static int? GetFirstVariant(this PropValModel.Node node)
        {
            if (node == null) return null;

            if (node.Children.SafeCount() == 0) return node.VariantId;
            else return node.Children[0].GetFirstVariant();
        }

        #endregion

        #region GetBundlings

        public static List<ProductBundleModel> GetBundlings(this Domain.ProductBase obj)
        {
            if (obj == null || obj.ProductBundles.Count == 0) return new List<ProductBundleModel>();

            return Mapper.Map<List<ProductBundle>, List<ProductBundleModel>>(obj.ProductBundles.ToList());
        }

        #endregion

        #region GetAccessories
        public static List<ProductAccessory> GetAccessories(this Domain.ProductBase obj, bool traceId = false)
        {
            if (obj == null) return new List<ProductAccessory>();

            var isBundle = obj.Type.InList(ProductType.Bundle);
            var lst = obj.Accessories.ToList();

            if (!isBundle)
                lst.AddRange(obj.AccessoryRefs.Where(m => !m.Product.Type.InList(ProductType.Bundle)).
                    Select(x => new ProductAccessory()
                    {
                        Id = traceId ? -1*x.Id : 0,
                        Product = x.Accessory,
                        Accessory = x.Product
                    }));

            return isBundle
                ? lst.Distinct(x => string.Format("{0}-{1}", x.Accessory.Id, x.AcceVariant.GetId(0))).ToList()
                : lst.Distinct(x => x.Accessory.Id).ToList();
        }
        #endregion

        #region GetExtraInformation

        public static List<ProductExtraInformationModel> GetProductExtraInformation(this Domain.ProductBase obj)
        {
            if (obj == null || obj.ExtraInformation.Count == 0) return new List<ProductExtraInformationModel>();

            return Mapper.Map<List<ProductExtraInformation>, List<ProductExtraInformationModel>>(obj.ExtraInformation.ToList());
        }

        #endregion

        #region GetTechSVals
        public static List<ProductTechSValModel> GetTechSVals(this Domain.ProductBase obj, IDB db)
        {
            if (obj == null) return new List<ProductTechSValModel>();

            var lst = obj.ProductTechSVals.ToList();

            return TechSpecValMgr.GetAll(db)
                .Concat(TechSpecMgr.GetAll(db).Select(x => new TechSpecValModel()
                {
                    TechSpecId = x.Id,
                    TechSpecName = x.Name,
                    TechSpecDOrder = x.DisplayOrder,
                    TechSpecActive = x.Active,
                    Value = "",
                }))
                .OrderBy(x => x.TechSpecDOrder)
                .ThenBy(x => x.TechSpecName)
                .ThenBy(x => x.Id == 0 ? 1 : 0)
                .ThenBy(x => x.Value)
                .Select(x =>
                {
                    var itm = lst.Find(z => z.TechSpecVal.Id == x.Id);
                    return new ProductTechSValModel()
                    {
                        Id = itm != null ? itm.Id : 0,
                        Selected = itm != null,
                        ProductId = itm != null ? itm.Product.Id : 0,
                        TechSpecId = x.TechSpecId,
                        TechSpecName = x.TechSpecName,
                        TechSpecActive = x.TechSpecActive,
                        TechSpecValId = x.Id,
                        TechSpecValName = x.Value,
                    };
                }).ToList();
        }
        #endregion

        #region GetRecurringDiscounts
        public static List<ProductRecurDiscModel> GetRecurringDiscounts(this Domain.ProductBase obj, IDB db)
        {
            if (obj == null) return new List<ProductRecurDiscModel>();

            var setting = SettingsBiz.GetSettings();
            var lst = obj.ProductRecurDiscs.ToList();
            var disRecurs = (setting.Hidden.RecurringTypesDisabled ?? "").Split(new[] { ',', ';' },
                    StringSplitOptions.RemoveEmptyEntries)
                    .Select(x => (int) Enum.Parse(typeof (RecurringType), x)).ToList();
            var recurEaches = (setting.Hidden.RecurringTypesRecurEach ?? "").Split(new[] { ',', ';' },
                    StringSplitOptions.RemoveEmptyEntries)
                    .Select(x =>
                    {
                        var arr = x.Split(new[] {':'});
                        return new { Type = (int)Enum.Parse(typeof(RecurringType), arr[0]), Each = int.Parse(arr[1]) };
                    }).ToList();

            return typeof (RecurringType).LocalizeEnumValues<int>(x => !disRecurs.Contains(x))
                .Select(x => new { Type = int.Parse(x.Value), Each = 1 }).Concat(recurEaches)
                .Select(x =>
                {
                    var itm = lst.FirstOrDefault(z => z.Type == x.Type && z.RecurEach == x.Each);
                    return new ProductRecurDiscModel()
                    {
                        Type = x.Type,
                        RecurEach = x.Each,
                        Id =  itm == null ? 0 : itm.Id,
                        DefOpt = itm != null && itm.DefOpt,
                        Value = new OFloat(itm == null ? null : (double?) itm.Value*100),
                        DiscountId = itm == null || itm.Discount == null ? (int?)null : itm.Discount.Id, 
                        ProductId = obj.Id,
                    };
                }).ToList();
        }
        #endregion

        #region GetPropVals

        public static List<PropValModel> GetPropVals(this Domain.Property obj, bool? active = null)
        {
            if (obj == null || obj.PropVals.Count == 0) return new List<PropValModel>();

            return Mapper.Map<List<PropVal>, List<PropValModel>>(
                obj.PropVals.Where(x => active == null || x.Active == active)
                    .OrderBy(x => x.DisplayOrder).ThenBy(x => x.Id).ToList());
        }

        #endregion

        #region GetVariantPropVals

        public static void GetVariantPropVals(this VariantBase obj, List<VariantPropValModel> result,
            List<PropertyModel> properties = null, bool mergeable = false)
        {
            if (obj == null || (properties == null && obj.VariantPropVals.Count == 0))
                return;

            List<VariantPropValModel> ret;
            var lst = obj.VariantPropVals.ToList();
            if (properties != null)
            {
                ret = properties.Select(x =>
                {
                    var itm = lst.FirstOrDefault(m => m.PropVal.Property.Id == x.Id);
                    return new VariantPropValModel()
                    {
                        VariantId = obj.Id,
                        Property = x,
                        Id = (itm != null ? itm.Id : 0),
                        PropValId = (itm != null ? itm.PropVal.GetId() : (int?)null),
                        PropValName = (itm != null ? itm.PropVal.GetName() : null),
                    };
                }).ToList();
            }
            else ret = Mapper.Map<List<VariantPropVal>, List<VariantPropValModel>>(lst);

            if (!mergeable) { result.Clear(); result.AddRange(ret); }
            else
            {
                result.AddRange(ret.Where(x => result.All(m => m.PropId != x.PropId)).ToList());
            }
        }

        #endregion

        #region GetDescendantCount

        public static int GetDescendantCount(this Category obj)
        {
            if (obj == null) return 0;
            var result = new List<Category>();
            obj.GetDescendants(result);
            return result.Count;
        }

        #endregion

        #region GetDescendants

        public static void GetDescendants(this Category obj, List<Category> result, IDB db = null, Dictionary<int, System.Tuple<Category, List<Category>>> dicCate = null)
        {
            obj.GetDescendants(result, false, db, dicCate);
        }

        public static void GetDescendants(this Category obj, List<Category> result, bool nocache, IDB db = null, Dictionary<int, System.Tuple<Category, List<Category>>> dicCate = null)
        {
            if (dicCate == null) obj.GetDescendantsEx(result, db, nocache);
            else obj.GetDescendantsRaw(result, db, dicCate, nocache);
        }

        public static void GetDescendantsEx(this Category obj, List<Category> result, IDB db)
        {
            obj.GetDescendantsEx(result, db, false);
        }

        public static void GetDescendantsEx(this Category obj, List<Category> result, IDB db, bool nocache)
        {
            if (obj == null) return;

            Func<List<Category>> fData = () =>
            {
                var tmp = new List<Category>();
                var dicCate = CategoryMgr.GetDictionary(db = db ?? new DefaultDb());
                obj.GetDescendantsRaw(tmp, db, dicCate);
                return tmp;
            };

            var ret = nocache ? fData() : Helper.DataCache<CategoryScope, List<Category>>.Get(string.Format("Category.GetDescendants::{0}", obj.Id), fData, 15 * 60, true, 5);      
      
            result.AddRange(ret);
        }

        private static void GetDescendantsRaw(this Category obj, List<Category> result, IDB db,
            Dictionary<int, System.Tuple<Category, List<Category>>> dicCate)
        {
            obj.GetDescendantsRaw(result, db, dicCate, false);
        }

        private static void GetDescendantsRaw(this Category obj, List<Category> result, IDB db,
            Dictionary<int, System.Tuple<Category, List<Category>>> dicCate, bool nocache)
        {
            if (obj == null) return;

            var children = dicCate.SafeGet(obj.Id).SafeProp(x => x.Item2);

            if (children != null)
                foreach (var child in children)
                {
                    child.GetDescendantsRaw(result, db, dicCate, nocache);
                    result.Add(child);
                }
        }

        #endregion

        #region GetId

        public static int? GetId(this PropVal obj, int? defVal = null)
        {
            if (obj == null) return defVal;
            else return obj.Id;
        }

        public static int? GetId(this VariantBase obj, int? defVal = null)
        {
            if (obj == null) return defVal;
            else return obj.Id;
        }

        public static int? GetId(this Category obj, int? defVal = null)
        {
            if (obj == null) return defVal;
            else return obj.Id;
        }

        public static int? GetId(this Vat obj, int? defVal = null)
        {
            if (obj == null) return defVal;
            else return obj.Id;
        }

        public static int? GetId(this Domain.ProductBase obj, int? defVal = null)
        {
            if (obj == null) return defVal;
            else return obj.Id;
        }

        public static int? GetId(this Manufacturer obj, int? defVal = null)
        {
            if (obj == null) return defVal;
            else return obj.Id;
        }

        public static int? GetId(this Supplier obj, int? defVal = null)
        {
            if (obj == null) return defVal;
            else return obj.Id;
        }

        public static int? GetId(this Status obj, int? defVal = null)
        {
            if (obj == null) return defVal;
            else return obj.Id;
        }

        public static int? GetId(this Image obj, int? defVal = null)
        {
            if (obj == null) return defVal;
            else return obj.Id;
        }

        #endregion

        #region UpPathHasLang
        public static bool UpPathHasLang(this Category obj, string langCode)
        {
            if (obj == null) return false;

            if (obj.UpPathHasLang == null)
            {
                if (obj.Languages.Any(m => m.LangCode.EqualsIgCase(langCode)))
                {
                    obj.UpPathHasLang = (obj.Parent == null || obj.Parent.UpPathHasLang(langCode));
                }
                else obj.UpPathHasLang = false;
            }

            return obj.UpPathHasLang.Value;
        }
        #endregion

        #region Deep

        public static int Deep(this Category obj)
        {
            if (obj == null) return 0;

            if (obj.Deep == null)
                obj.Deep = (obj.Parent == null ? 0 : obj.Parent.Deep() + 1);

            return (int)obj.Deep;
        }

        #endregion

        #region HierOrder

        public static string HierOrder(this Category obj, bool alias = true)
        {
            if (obj == null) return string.Empty;

            if (obj.HierOrder == null)
                obj.HierOrder = (obj.Parent == null ? string.Empty : obj.Parent.HierOrder(alias) + "/")
                    + (alias ? obj.GetName().GenerateAlias() + "-" + obj.Id : obj.GetName());

            return obj.HierOrder;
        }

        #endregion

        #region GetHierOrderList

        public static List<Category> GetHierOrderList(this Category obj)
        {
            if (obj == null) return null;

            var ret = new List<Category>() { obj };

            while (obj.Parent != null)
            {
                ret.Add(obj.Parent);
                obj = obj.Parent;
            }

            ret.Reverse();

            return ret;
        }

        #endregion

        #region HasLang

        public static bool HasLang(this Category obj, string langCode, bool? visible = null)
        {
            if (obj == null) return false;

            return obj.Languages.Any(m => m.LangCode.EqualsIgCase(langCode) && (visible == null || m.Visible == visible));
        }

        public static bool HasLang(this Status obj, string langCode)
        {
            if (obj == null) return false;

            return obj.Languages.Any(m => m.LangCode.EqualsIgCase(langCode));
        }

        public static bool HasLang(this Domain.ProductBase obj, string langCode)
        {
            if (obj == null) return false;

            return obj.Languages.Any(m => m.LangCode.EqualsIgCase(langCode));
        }

        public static bool HasLang(this PropVal obj, string langCode)
        {
            if (obj == null) return false;

            return obj.Languages.Any(m => m.LangCode.EqualsIgCase(langCode));
        }

        #endregion

        #region AnyChild

        public static bool AnyChild(this Category obj)
        {
            if (obj == null) return false;

            return obj.Children.Count != 0;
        }

        #endregion

        #region GetChildrenCount

        public static int GetChildrenCount(this Category obj)
        {
            if (obj == null) return 0;

            var langCode = LanguageMgr.FocusLangCode;

            return obj.Children.Count(x => x.HasLang(langCode));
        }

        #endregion

        #region GetPropValCount

        public static int GetPropValCount(this Domain.Property obj)
        {
            if (obj == null) return 0;

            var langCode = LanguageMgr.FocusLangCode;

            return obj.PropVals.Count(x => x.HasLang(langCode));
        }

        #endregion

        #region GetVariantCount

        public static int GetVariantCount(this Domain.ProductBase obj)
        {
            if (obj == null) return 0;

            return obj.Variants.Count(x => !x.Deleted);
        }

        #endregion

        #region GetProductCount

        public static int GetProductCount(this Category obj, string lang = null, int status = 0,
            bool descendant = false, int type = -1)
        {
            if (type == -1)
                return obj.GetProducts(lang, status, descendant).Count();
            return obj.GetProducts(lang, status, descendant).Where(x => x.Value.Type == ((byte)type)).Count();
        }

        #endregion

        #region GetProducts

        public static Dictionary<int, Domain.ProductBase> GetProducts(this Category obj, string lang = null, int status = 0,
            bool descendant = false)
        {
            if (obj == null) return new Dictionary<int, Domain.ProductBase>();

            var langCode = lang ?? LanguageMgr.FocusLangCode;
            Dictionary<int, Domain.ProductBase> ret;

            if (status == 0)
                ret = obj.CategoryProducts
                    .Where(x => x.Product.HasLang(langCode))
                    .ToDictionary(x => x.Product.Id, x => (Domain.ProductBase)x.Product);
            else
                ret = obj.CategoryProducts
                    .Where(x => x.Product.HasLang(langCode) && x.Product.ProductStatus.Id == status)
                    .ToDictionary(x => x.Product.Id, x => (Domain.ProductBase)x.Product);

            if (descendant)
                foreach (var child in obj.Children)
                {
                    foreach (var pc in child.GetProducts(lang, status, true))
                    {
                        if (!ret.ContainsKey(pc.Key)) ret.Add(pc.Key, pc.Value);
                    }
                }

            return ret;
        }

        #endregion

        #region ClearStatistics

        public static void ClearStatistics(this Domain.ProductBase obj)
        {
            obj.CategoryProducts.SelectMany(x => x.Category.Languages).ToList().Action(x =>
            {
                x.Products = null;
                x.ProductsBundle = null;
                x.StatusStats = null;
                x.Category.TravelUp(w => w.Languages.Action(k => { k.ProductsSub = null; k.ProductsBundleSub = null; k.StatusStats = null; }));
            });
        }

        #endregion

        #region GetStatistics.Product

        public static ProductModel.StatInfo GetStatistics(this Domain.ProductBase obj)
        {
            if (obj == null || obj.Id == 0) return new ProductModel.StatInfo();

            return new ProductModel.StatInfo()
            {
                InStock = obj.Quantity,
                Variants = obj.VariantNum ?? 0,
                OrderLines = obj.OrderDetailNum ?? 0,
            };
        }

        #endregion

        #region GetStatistics.Category

        public static ExpandoObject GetStatistics(this Category obj, IDB db, HttpRequestBase request, List<StatusModel> statuses)
        {
            if (obj == null || obj.Id == 0 || obj.Languages.SafeCount() == 0) return new ExpandoObject();

            var langCode = LanguageMgr.FocusLangCode;
            var lang = obj.Languages.MatchOrFirst(x => x.LangCode.EqualsIgCase(langCode));
            var dirty = false;

            if (lang.SubCates == null)
            {
                lang.SubCates = obj.GetDescendantCount();
                dirty = true;
            }

            if (lang.Products == null)
            {
                lang.Products = obj.GetProductCount(null, 0, false, (int)ProductType.Normal);
                dirty = true;
            }
            if (lang.ProductsBundle == null)
            {
                lang.ProductsBundle = obj.GetProductCount(null, 0, false, (int)ProductType.Bundle);
                dirty = true;
            }

            if (lang.ProductsSub == null)
            {
                lang.ProductsSub = ProductMgr.GetAll(db, 0, -1 * obj.Id).Where(x => x.Type.InList(ProductType.Normal)).ToList().Count;
                dirty = true;
            }

            if (lang.ProductsBundleSub == null)
            {
                lang.ProductsBundleSub = ProductMgr.GetAll(db, 0, -1 * obj.Id).Where(x => x.Type.InList(ProductType.Bundle)).ToList().Count;
                dirty = true;
            }

            #region StatusStats
            if (lang.StatusStats == null)
            {
                lang.StatusStats = new StatusStatList();
            }

            foreach (var status in statuses)
            {
                var statId = status.Id;
                if (lang.StatusStats.All(x => x.Id != statId))
                {
                    lang.StatusStats.Add(new Status.Stat()
                    {
                        Id = statId,
                        Count = obj.GetProductCount(null, statId, true, (int)ProductType.Normal),
                        CountBundle = obj.GetProductCount(null, statId, true, (int)ProductType.Bundle)
                    });
                    dirty = true;
                }
            }
            #endregion

            if (dirty) lang.Update(db.DataService);

            dynamic info = new ExpandoObject();
            var dic = info as IDictionary<string, object>;

            info.Children = lang.SubCates ?? 0;
            info.ChildrenLink = request.ActionUrl("Index", "Category", new { @CategoryId = obj.Id });
            info.Products = lang.Products ?? 0;
            info.ProductsLink = request.ActionUrl("ListProduct", "Category", new { @CategoryId = obj.Id });
            info.ProductsSub = lang.ProductsSub ?? 0;
            info.ProductsSubLink = request.ActionUrl("ListProduct", "Category", new { @CategoryId = (obj.Id * -1) });

            info.ProductsBundle = lang.ProductsBundle ?? 0;
            info.ProductsBundleLink = request.ActionUrl("ListBundleProduct", "Category", new { @CategoryId = obj.Id });
            info.ProductsBundleSub = lang.ProductsBundleSub ?? 0;
            info.ProductsBundleSubLink = request.ActionUrl("ListBundleProduct", "Category", new { @CategoryId = (obj.Id * -1) });

            foreach (var status in statuses)
            {
                var statId = status.Id;
                dic["status_count_" + status.Id] = lang.StatusStats.First(x => x.Id == statId).Count;
                dic["status_link_" + status.Id] = request.ActionUrl("ListProduct", "Category", new { @CategoryId = obj.Id, @Status = -1 * status.Id });

                dic["status_countbundle_" + status.Id] = lang.StatusStats.First(x => x.Id == statId).CountBundle;
                dic["status_linkbundle_" + status.Id] = request.ActionUrl("ListBundleProduct", "Category", new { @CategoryId = obj.Id, @Status = -1 * status.Id });
            }

            return info;
        }

        #endregion

        #region ComputeTotalSales

        public static int ComputeTotalSales(this List<Domain.Product> list, IDB db)
        {
            if (list.SafeCount() == 0) return 0;

            return list.Cast<ProductBase>().ToList().ComputeTotalSales(db);
        }

        public static int ComputeTotalSales(this List<ProductBase> list, IDB db)
        {
            if (list.SafeCount() == 0) return 0;

            var nilLst = list.Where(x => x.TotalSales == null).ToList();

            if (nilLst.Count == 0) return 0;

            foreach (var arr in Singleton<IOrderMgr>.Default.GetProductTotalSales(db, nilLst.Select(x => x.Id).ToArray()))
            {
                var pair = arr;
                nilLst.Where(x => x.Id == pair[0]).Action(x => x.TotalSales = pair[1]);
            }

            nilLst.Where(x => x.TotalSales == null).Action(x => x.TotalSales = 0);

            BaseMgr.Update(db, nilLst);

            ProductScope.Invalidate();

            return nilLst.Count;
        }

        #endregion

        #region TravelUp

        public static void TravelUp(this Category obj, Action<Category> fAction, bool includeSelf = false)
        {
            if (obj == null) return;

            if (includeSelf) fAction(obj);

            if (obj.Parent != null)
            {
                fAction(obj.Parent);
                obj.Parent.TravelUp(fAction);
            }
        }

        #endregion

        #region TravelDown

        public static void TravelDown(this Category obj, Action<Category> fAction)
        {
            if (obj == null) return;

            foreach (var child in obj.Children)
            {
                fAction(child);
                child.TravelDown(fAction);
            }
        }

        #endregion

        #region ParentLangVisible

        public static bool ParentLangVisible(this Category obj)
        {
            if (obj == null) return false;
            else return obj.Parent == null || obj.Parent.HasLang(LanguageMgr.FocusLangCode, true);
        }

        #endregion

        #region AncestorLangVisible

        public static bool AncestorLangVisible(this Category obj)
        {
            if (obj == null) return false;
            else
            {
                if (obj.AncestorLangVisible == null)
                {
                    obj.AncestorLangVisible = obj.Parent == null ||
                                            (obj.Parent.HasLang(LanguageMgr.FocusLangCode, true) &&
                                             obj.Parent.AncestorLangVisible());
                }
                return obj.AncestorLangVisible.Value;
            }
        }

        #endregion

        #region IsDefault

        public static bool IsDefault(this Vat obj)
        {
            if (obj == null) return false;

            var defTaxId = SettingsBiz.GetSettings().General.DefaultTaxId;

            return obj.Id == defTaxId;
        }

        #endregion

        #region AnyBundleRef
        public static bool AnyBundleRef(this Domain.ProductBase obj)
        {
            if (obj == null || obj.Id == 0) return false;

            return obj.BundlingRefs.Any(x => x.Product.Id == obj.Id && x.Variant == null);
        }
        #endregion

        #region GetUpPathVisibility
        public static byte GetUpPathVisibility(this Category obj)
        {
            if (obj == null || obj.Parent == null) return 3;

            return (byte)(obj.Parent.Visibility & obj.Parent.GetUpPathVisibility());
        }
        #endregion

        #region InvalidateUpPathVisibility

        public static void InvalidateUpPathVisibility(this Category obj, ISession session)
        {
            if (obj == null) return;

            obj.VisibilityUpPath = obj.GetUpPathVisibility();

            if (obj.Children.SafeCount() != 0)
                obj.Children.Action(x =>
                {
                    x.VisibilityUpPath = x.GetUpPathVisibility();
                    session.Update(x);
                    x.InvalidateUpPathVisibility(session);
                });
        }

        #endregion

        #region ToExportModel

        public static ProductExportModel ToExportModel(this ProductModel obj, List<PropertyModel> properties, VariantModel variant = null)
        {
            if (obj == null) return null;

            var variantExp = variant == null ? null : new ProductExportModel.VariantModel(variant);

            var propVals = variant == null ? null : variant.GetRaw().VariantPropVals.ToList();
            var propExp = variant == null
                ? null
                : properties.Select(m => new ProductExportModel.PropertyModel()
                {
                    Id = m.Id,
                    Name = m.Name,
                    Value = propVals.FirstOrDefault(x => x.PropVal.Property.Id == m.Id).SafeProp(w => w.PropVal.GetName())
                }).ToList();

            return new ProductExportModel(obj, variantExp)
            {
                ProductStatusName = variantExp != null ? variantExp.ProductStatusName : obj.ProductStatusName,
                CategoryPath = obj.GetRaw().CategoryProducts
                        .Select(x => new { Val = x.Category.GetPath(" > "), Deep = x.Category.Deep() })
                        .OrderByDescending(x => x.Deep).Select(x => x.Val)
                        .FirstOrDefault(x => !x.IsNullOrEmpty()),
                Properties = propExp ?? properties.Select(m => new ProductExportModel.PropertyModel()
                {
                    Id = m.Id,
                    Name = m.Name,
                }).ToList(),
            };
        }

        #endregion

        #region GetPath

        public static string GetPath(this Domain.Category obj, string separator, Predicate<Category> fCond = null)
        {
            if (obj == null) return "";

            var ret = "";
            var lstCateName = new List<string>();
            while (obj != null)
            {
                if (fCond != null && !fCond(obj)) return null;
                //ret += (ret.Length == 0 ? "" : separator) + obj.GetName();
                lstCateName.Add(obj.GetName());
                obj = obj.Parent;
            }
            ret = string.Join(separator, lstCateName.ToArray().Reverse());
            return ret;
        }

        #endregion

        #region ToString

        public static string ToString(this ICell value, NumberFormatInfo formatInfo)
        {
            if (value == null) return null;

            switch (value.CellType)
            {
                case CellType.Numeric:
                    return value.NumericCellValue.ToString(formatInfo);
                default:
                    return value.ToString();
            }
        }

        #endregion

        #region PriceCheck

        public static decimal? PriceCheck(this ProductBase product, VariantBase variant = null)
        {
            if (product == null || !product.PriceCkActive) return null;

            var settings = SettingsBiz.GetSettings().Hidden;
            var url = variant == null || product.PriceCkVariant ? product.PriceCkUrl : variant.PriceCkUrl;
            var minVal = (variant == null ? null : variant.PriceCkMinVal) ?? product.PriceCkMinVal;

            if (url.IsNullOrEmpty() || settings.PriceCheckUrl.IsNullOrEmpty()) return null;

            var respAnonymous = new
            {
                status = "",
                items = new[] {new {price = 0m}},
                stats = new { finish_time = "", start_time = "" },
            };

            var respTxt = settings.PriceCheckUrl.Replace("{url}", WebUtility.UrlEncode(url)).RestApiInvoke();
            var ret = respTxt.IsNullOrEmpty() ? null : JsonConvert.DeserializeAnonymousType(respTxt, respAnonymous);

            var prisjaktPrice = ret == null || !ret.status.EqualsIgCase("ok") || ret.items.SafeCount() == 0 ? (decimal?) null : ret.items.Min(m => m.price);

            return prisjaktPrice == null || minVal == null ? prisjaktPrice : (prisjaktPrice < minVal ? null : prisjaktPrice); 
        }

        #endregion

        #region CalculateQty

        public static void CalculateQty(this ProductBase obj, ISession session = null)
        {
            if (obj == null || !obj.Type.InList(ProductType.Bundle)) return;

            var lst = obj.ProductBundles.Where(m => !m.Product.Deleted && (m.Variant == null || (!m.Variant.Deleted /*&& m.Variant.Active*/))).ToList();

            var newQty = lst.Count == 0 ? 0 : lst.Min(m => m.Quantity == 0 || m.Product.AllowBackorder ? (m.Product.AllowBackorder ? 10000 : 0) : (m.Variant != null ? m.Variant.Quantity : m.Product.Quantity) / m.Quantity);
            var newBackQty = lst.Count == 0 ? 0 : lst.Max(m => m.Quantity == 0 ? 0 : (m.Variant != null ? m.Variant.BackOrderQty : m.Product.BackOrderQty) / m.Quantity);
            var showProdInStock = 0 != (obj.Flags & (int)ProductFlags.BundleShowProdInStock);

            obj.AllowBackorder = lst.All(m => m.Product.AllowBackorder) && obj.Accessories.Where(m => m.BundleGroup != null).All(m => m.Accessory.AllowBackorder);

            if (0 != (obj.Flags & (int)ProductFlags.BundleNeedSelectAll) &&
                obj.Accessories.Where(m => m.BundleGroup != null).GroupBy(m => m.BundleGroup.Id)
                .Any(m => m.All(z => (showProdInStock || !z.Accessory.AllowBackorder) && z.Accessory.Quantity <= 0)))
            {
                newQty = 0;
            }

            if (obj.Quantity != newQty || obj.BackOrderQty != newBackQty)
            {
                ProductMgr.UpdateQuantity(new DefaultDb(), obj, null, 0, changeStatus: true, reCalc: true);
                if (session != null) session.SaveOrUpdate(obj);
            }

            obj.CalculatePriceWeight();
        } 

        #endregion

        #region CalculatePriceWeight

        public static bool CalculatePriceWeight(this ProductBase obj, bool updateWeight = true, bool updatePrice = true,
            List<int> variantIds = null, List<int[]> accessoryIds = null)
        {
            return obj.CalculatePriceWeight(updateWeight, updatePrice, variantIds, null, null, accessoryIds);
        }

        public static bool CalculatePriceWeight(this ProductBase obj, bool updateWeight, bool updatePrice, List<int> variantIds,
            Action<decimal, decimal?, decimal?> fPrice, Action<decimal> fWeight, List<int[]> accessoryIds)
        {
            if (obj == null || !obj.Type.InList(ProductType.Bundle)) return false;

            var ret = false;
            var lst = obj.ProductBundles.Where(m => !m.Product.Deleted && (m.Variant == null || (!m.Variant.Deleted && m.Variant.Active))).ToList();

            if (updatePrice) 
            {
                var variants = variantIds == null 
                    ? null 
                    : lst.Where(m => m.Variant == null && m.Product.VariantNum > 0)
                        .SelectMany(m => m.Product.VariantsFe.Where(z => z.Active)).ToList();
                var dicVar = variantIds == null 
                    ? null 
                    : variantIds.Join(variants, o => o, i => i.Id, (o, i) => i).ToList()
                        .Distinct(m => m.Id).ToDictionary(m => m.Product.Id, m => m);

                if (variantIds != null && variantIds.Count != dicVar.Count) return false;

                var profitMode = obj.BundleDiscountRate < 0;
                //var newPrice = lst.Sum(m => m.Quantity * m.Product.CombinedSalePrice(m.Variant)) ?? 0;
                var newCostPrice = lst.Sum(m => m.Quantity * m.Product.CombinedCostPriceUseDefPrice(m.Variant ?? (dicVar != null && dicVar.ContainsKey(m.Product.Id) ? dicVar[m.Product.Id] : null)));
                var newPrice = obj.ProductBundles.Sum(m => m.Quantity * ((m.Variant != null || (dicVar != null && dicVar.ContainsKey(m.Product.Id)))
                        ? (m.Variant ?? dicVar[m.Product.Id]).CombinedPrice() ?? 0
                        : (m.Product.VariantsFe.Any(z => z.Active) ? m.Product.VariantsFe.Where(z => z.Active).Min(z => z.CombinedPrice() ?? 0) : m.Product.Price)));

                if (Math.Abs(newPrice - obj.Price) > 0.0001m) ret = true;
                else newPrice = obj.Price;

                Func<decimal?, decimal, decimal, decimal?> calcPrice = (cost, price, rate) =>
                {
                    if (rate == 0) return null;

                    if (rate > 0) return (1 - rate)*price;

                    var tmp = (cost ?? 0)*(1 + Math.Abs(rate));
                    return tmp > price ? price : tmp;
                };

                var newSalePrice = calcPrice(newCostPrice, newPrice, obj.BundleDiscountRate ?? 0);

                if (accessoryIds.SafeCount() != 0)
                {
                    obj.Accessories.Where(m => m.BundleGroup != null && accessoryIds.Any(z => z[0] == m.Accessory.Id)).Action(m =>
                    {
                        var tmp = m.Accessory.Variants.Any() ? m.Accessory.Variants
                            .Where(v => accessoryIds.Any(z => z.Length > 1 && z[0] == v.Product.Id && z[1] == v.Id && (m.AcceVariant == null || m.AcceVariant.Id == v.Id))).ToList() : null;

                        if (tmp.SafeCount() != 0) tmp.Action(z =>
                        {
                            var tmpCost = z.CostPrice.NullIfZero() ?? (z.Price*0.8m);
                            newCostPrice += tmpCost;
                            newPrice += z.Price ?? z.Product.Price;
                            newSalePrice += calcPrice(tmpCost, z.Price ?? z.Product.Price,
                                obj.BundleDiscountRate ?? 0);
                        });
                        else if(m.AcceVariant == null && (m.Accessory.Variants.Count == 0 || !m.BundleGroup.ShowVariant))
                        {
                            var tmpCost = m.Accessory.CostPrice.NullIfZero() ?? (m.Accessory.Price*0.8m);
                            newCostPrice += tmpCost;
                            newPrice += m.Accessory.Price;
                            newSalePrice += calcPrice(tmpCost, m.Accessory.Price, obj.BundleDiscountRate ?? 0);
                        }
                    });
                }

                if (fPrice != null) fPrice(newPrice, newSalePrice, newCostPrice);
                else
                {
                    obj.Price = newPrice;
                    obj.SalePrice = newSalePrice;
                    obj.CostPrice = newCostPrice;
                }
            }

            if (updateWeight)
            {
                var defWUnit = (byte)SettingsBiz.GetSettings().Display.Others.MUnit;
                var newWeight = lst.Sum(m => m.Quantity * (m.Product.CombinedWeight(m.Variant) ?? 0).ToKg((WeightUnit)(m.Product.WUnit ?? defWUnit)));

                if (Math.Abs(newWeight - obj.Weight) > 0.0001m)
                {
                    if (fWeight != null) fWeight(newWeight);
                    else
                    {
                        obj.Weight = newWeight;
                        obj.WUnit = (byte) WeightUnit.Kg;
                    }
                    ret = true;
                }
            }

            return ret;
        }
        #endregion

        #region CalculateSalePrice
        public static bool CalculateSalePrice(this ProductBase obj) 
        {
            return obj.CalculateSalePrice(SettingsBiz.GetSettings());
        }
        public static bool CalculateSalePrice(this ProductBase obj, SettingsBiz settings)
        {
            if (obj == null || obj.Variants.Count == 0 || settings.Hidden.ProductVariMainPriceDisabled) return false;

            var lst = obj.Variants.Where(m => m.Active && !m.Deleted && m.ValidToShow()).ToList();

            if (lst.Count == 0) return false;

            obj.Price = lst.Min(m => m.Price) ?? 0;
            obj.CostPrice = lst.Any(m => m.CostPrice > 0) ? lst.Where(m => m.CostPrice > 0).Min(m => m.CostPrice) : null;
            obj.SalePrice = lst.Any(m => m.SalePrice > 0) ? lst.Where(m => m.SalePrice > 0).Min(m => m.SalePrice) : null;
            obj.SalePrice2 = lst.Any(m => m.SalePrice2 > 0) ? lst.Where(m => m.SalePrice2 > 0).Min(m => m.SalePrice2) : null;

            return true;
        }
        #endregion

        #region GetMaxQuantity

        public static System.Tuple<int, int, List<Action<int>>> GetMaxQuantity(this ProductBase obj, List<int> variantIds, List<int[]> accessoryIds, Dictionary<string, int> dicQty)
        {
            if (obj == null || !obj.Type.InList(ProductType.Bundle))
                return new System.Tuple<int, int, List<Action<int>>>(obj.SafeProp(m => m.Quantity + (m.DropshipQty ?? 0)), 0,  null); 

            var qty = obj.Quantity + (obj.DropshipQty ?? 0);
            var qty2 = qty;

            if (variantIds.SafeCount() != 0)
            {
                var lst = obj.ProductBundles.Where(m => !m.Product.Deleted && (m.Variant == null || (!m.Variant.Deleted && m.Variant.Active))).ToList();
                var variants = lst.Where(m => m.Variant == null && m.Product.VariantNum > 0)
                        .SelectMany(m => m.Product.VariantsFe.Where(w => w.Active)).ToList();
                var dicVar = variantIds.Join(variants, o => o, i => i.Id, (o, i) => i).ToList()
                    .Distinct(m => m.Id).ToDictionary(m => m.Product.Id, m => m);

                obj.ProductBundles.Where(m => m.Variant != null || dicVar.ContainsKey(m.Product.Id)).Action(m =>
                {
                    var vari = m.Variant ?? dicVar[m.Product.Id];
                    if (m.Quantity != 0 && !vari.Product.AllowBackorder) qty = Math.Min(qty, vari.Quantity / m.Quantity);
                });
            }

            var lstAct = new List<Action<int>>();
            if (accessoryIds.SafeCount() != 0)
                obj.Accessories.Where(m => accessoryIds.Any(w => w[0] == m.Accessory.Id)).Action(m =>
                {
                    if (m.Accessory.Variants.Any())
                    {
                        m.Accessory.Variants
                            .Where(v => accessoryIds.Any(x => x.Length > 1 && x[0] == v.Product.Id && x[1] == v.Id))
                            .Action(v =>
                            {
                                var key = string.Format("{0}:{1}", v.Product.Id, v.Id);
                                if (!dicQty.ContainsKey(key)) dicQty.Add(key, v.Quantity);
                                if (!v.Product.AllowBackorder) qty2 = Math.Min(qty2, dicQty[key]);
                                lstAct.Add(x => dicQty[key] = dicQty[key] - x);
                            });
                    }
                    else
                    {
                        var key = m.Accessory.Id.ToString();
                        if (!dicQty.ContainsKey(key)) dicQty.Add(key, m.Accessory.Quantity);
                        if (!m.Accessory.AllowBackorder) qty2 = Math.Min(qty2, dicQty[key]);
                        lstAct.Add(x => dicQty[key] = dicQty[key] - x);
                    }
                });

            return new System.Tuple<int, int, List<Action<int>>>(qty, qty2, lstAct);
        } 
        
        #endregion

        #region IsSerial

        public static bool IsSerial(this ProductBase obj)
        {
            if (obj == null) return false;

            if (obj.Type.InList(ProductType.Bundle))
               return obj.ProductBundles.Any(m => !m.Product.Deleted && 0 != (m.Product.Flags & (int) ProductFlags.SerialNumber)) ||
                   obj.Accessories.Any(m => m.BundleGroup != null && !m.Accessory.Deleted && 0 != (m.Accessory.Flags & (int)ProductFlags.SerialNumber));
            
            return 0 != (obj.Flags & (int) ProductFlags.SerialNumber);
        }
        #endregion

        #region CalculateStats

        public static void CalculateStats(this ProductBundleGroup obj, IDB db)
        {
            if (obj == null || (obj.Accessories != null && obj.MinPrice != null)) return;

            var lst = obj.Product.Accessories.Where(z => z.BundleGroup != null && z.BundleGroup.Id == obj.Id).ToList();

            obj.Accessories = obj.Accessories ?? lst.Count;
            obj.MinPrice = obj.MinPrice ?? (lst.Count == 0 ? 0 : lst.Min(z => z.Accessory.Price));

            obj.Update(db.DataService);
        } 

        #endregion

        #region ResetStats

        public static void ResetStats(this ProductBundleGroup obj, IDB db = null)
        {
            if (obj == null || (obj.Accessories == null && obj.MinPrice == null)) return;

            obj.Accessories = null;
            obj.MinPrice = null;

            if (db != null) obj.Update(db.DataService);
        }

        #endregion

        #region GetCombinedImages

        public static IList<Image> GetCombinedImages(this ProductBase obj, bool incAccessory = false)
        {
            if (obj == null) return new List<Image>();

            if (!obj.Type.InList(ProductType.Bundle) || 0 == (obj.Flags & (int)ProductFlags.BundleFetchDescFromProds)) 
                return obj.Images;

            var cxt = HttpContext.Current;
            var key = "product-combined-images-" + obj.Id;

            if (cxt != null && cxt.Items.Contains(key))
                return cxt.Items[key] as IList<Image>;

            var ret = /*obj.Images.Concat*/(obj.ProductBundles.SelectMany(m => m.Variant == null || m.Variant.Image.IsNullOrEmpty() ? m.Product.Images : new[] { new Image() { FileName = m.Variant.Image } })).ToList();

            if (incAccessory)
                ret.AddRange(obj.Accessories
                    .Where(m => m.BundleGroup != null && m.Product.ValidToShow() && (m.AcceVariant == null || m.AcceVariant.ValidToShow()))
                    .SelectMany(m => m.AcceVariant == null || m.AcceVariant.Image.IsNullOrEmpty() ? m.Accessory.Images : new[] { new Image() { FileName = m.AcceVariant.Image } }));

            ret = ret.Distinct(m => m.Id).ToList();

            if (cxt != null) cxt.Items[key] = ret;

            return ret;
        } 

        #endregion

        #region GetCombinedImage

        public static Image GetCombinedImage(this ProductBase obj, bool incAccessory = false)
        {
            if (obj == null) return null;

            if (!obj.Type.InList(ProductType.Bundle) || 0 == (obj.Flags & (int)ProductFlags.BundleFetchDescFromProds))
                return obj.Image;

            return obj.GetCombinedImages(incAccessory).FirstOrDefault();
        }

        #endregion

        #region RemoveKey2.ModelState  

        /// <summary>remove items in modelState by prefix and shift indexes down 1 unit to remove the gap</summary>
        public static void RemoveKey2(this ModelStateDictionary modelState, string prefix, ref Trie<Product.TrieItem> trie,
            int index = -1, int maxIndex = -1, bool matchExact = false, int? staticPrefixBackOffset = null)
        {

            var staticPbo = staticPrefixBackOffset;
            var staticPrefix = staticPbo == null ? null : prefix.Substring(0, prefix.Length - (staticPbo ?? 0));
            var keyList = staticPrefix == null ? modelState.Keys.ToList() : modelState.Keys.Where(x => x.StartsWith(staticPrefix)).ToList();
            if (trie == null)
            {
                trie = new Trie<Product.TrieItem>();
                if (staticPrefix == null)
                    foreach (var key in modelState.Keys) trie.Add(key, new Product.TrieItem(key));
                else
                    foreach (var key in modelState.Keys.Where(x => x.StartsWith(staticPrefix))) trie.Add(key, new Product.TrieItem(key));
            }
            var dkey = (index == -1 ? prefix : string.Format(prefix, index));
            int remCount = 0;
            var trieLst = matchExact 
                ? trie.Retrieve(dkey).Where(m => m.Value != null && m.Value.EqualsIgCase(dkey)).ToList() 
                : trie.Retrieve(dkey).Where(m => m.Value != null).ToList();
            foreach (var key in trieLst.OrderBy(x => x.Value).ToArray())
            {
                modelState.Remove(key.Value); key.Value = null; /*keyList.Remove(key);*/ remCount++;
            }

            //shift indexes down by 1
            if (remCount != 0 && index != -1 && maxIndex != -1)
                for (int i = index + 1; i <= maxIndex; i++)
                {
                    string d2Key = string.Format(prefix, i), n2Key = string.Format(prefix, i - 1);
                    var d2Lst = trie.Retrieve(d2Key).Where(m => m.Value != null).ToList();
                    foreach (var m2Key in d2Lst.ToArray())
                    {
                        var val = modelState[m2Key.Value];
                        if (val == null && Debugger.IsAttached) Debugger.Break();
                        var newKey = m2Key.Value.Replace(d2Key, n2Key);
                        modelState.Add(newKey, val); trie.Add(newKey, new Product.TrieItem(newKey)); //keyList.Add(newKey);
                        //modelState.RemoveKey(m2Key, -1, -1, true);
                        modelState.Remove(m2Key.Value);m2Key.Value = null; //keyList.Remove(m2Key);
                    }
                }
        }

        #endregion
    }
}

namespace Claw.Modules.Eshop.Product 
{
    public class TrieItem { public string Value; public TrieItem(string value) { Value = value; } }
}