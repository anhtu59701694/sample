using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using Claw.Modules.Eshop.Domain.Interfaces;
using Claw.Modules.Eshop.Models;

namespace Claw.Modules.Eshop.Domain
{
    public enum QtyPos { Qty1 = 1, Qty2 = 2 }
    public partial class ProductBase : Tuple<CategoryProduct, Image, ProductLang, VariantBase, ProductBundle, ProductBundle, IDom, IDom, IDom, IDom, IDom, IDom, ProductAccessory, ProductAccessory, ProductEmailMUser, ProductExtraInformation, ProductTechSVal, ProductRecurDisc, IDom>, IProduct
    {                                                          
        private List<ProductLang> _languages = null;
        private List<CategoryProduct> _categoryProducts = null;
        private IList<ProductSerial> _list19;
        private IList<ProductBundleGroup> _list20;
        
        public virtual IList<ProdQtyLog> QtyLogs { get; set; }
        public virtual int Id { get; set; }
        public virtual string Alias { get; set; }
        public virtual byte Type { get; set; }
        public virtual string SKU { get; set; }
        public virtual string BarCode { get; set; }
        public virtual string NvitCode { get; set; }
        public virtual string Materials { get; set; }
        public virtual Country OriginCountry { get; set; }

        public virtual bool Deleted { get { return Type >= 128; } set{} }
                
        /// <summary>Variants.Count</summary>
        public virtual int? VariantNum { get; set; }

        /// <summary>OrderDetails.Count</summary>
        public virtual int? OrderDetailNum { get; set; }

        /// <summary>ProductBundles.Count</summary>
        public virtual int? BundleNum { get; set; }

        /// <summary>Quantity1 + Quantity2</summary>
        public virtual int Quantity { get; set; }
        public virtual int Quantity1 { get; set; }
        public virtual int Quantity2 { get; set; }
        public virtual int BackOrderQty { get; set; }
        public virtual int? ShoppingQty { get; set; }
        public virtual Vat Vat { get; set; }
        public virtual Vat SecondVat { get; set; }
        public virtual Status ProductStatus { get; set; }
        public virtual decimal Price { get; set; }
        public virtual decimal? SalePrice { get; set; }

        /// <summary>Cpos only</summary>
        public virtual decimal? SalePrice2 { get; set; }
        public virtual decimal? SalePriceBk { get; set; }
        public virtual decimal? CostPrice { get; set; }
        public virtual decimal? CombinedSalePrice { get; set; }
        public virtual decimal? TechSpecCostExtra { get; set; }
        public virtual decimal Weight { get; set; }

        public virtual decimal WeightInKg(Helper.SettingsBiz settings = null, VariantBase variant = null)
        {
            if (Id == 1 || ShippingType.InList(Models.ShippingType.FreeShipping, Models.ShippingType.Download)) return 0;

            return (this.CombinedWeight(variant) ?? 0).ToKg((((WeightUnit?)WUnit) ?? WeightUnit.Kg)).NullIf(m => m == 0) 
                ?? ((settings ?? Settings).Hidden.DefaultProductWeightInGram / 1000m);
        }

        public virtual byte? WUnit { get; set; }
        public virtual decimal? ShippingCostExtra { get; set; }   
        public virtual decimal? DepositAmount { get; set; }
        public virtual byte Visibility { get; set; }
        public virtual int ShippingType { get; set; }
        public virtual string AttachFile { get; set; }
        public virtual string VideoUrls { get; set; }
        public virtual string Location { get; set; }
        public virtual bool AllowBackorder { get; set; }
        public virtual string BackorrderMsg { get; set; }
        public virtual int MinQuantity { get; set; }
        public virtual int NotifyQtyThreshold { get; set; }
        public virtual int Flags { get; set; }
        public virtual int? TotalSales { get; set; }
        public virtual int? EmailMUsers { get; set; }
        public virtual Image Image { get; set; }
        public virtual Manufacturer Manufacturer { get; set; }
        public virtual Supplier Supplier { get; set; }
        public virtual DateTime PublishedDate { get; set; }
        public virtual DateTime ModifiedDate { get; set; }
        public virtual DateTime CreatedDate { get; set; }
        public virtual DateTime? BackDate { get; set; }
        public virtual int DisplayOrder { get; set; }
        public virtual IDiscount Discount { get; set; }

        /// <summary>Cpos only</summary>
        public virtual IDiscount Discount2 { get; set; }

        public virtual IDiscount DiscountFe { get { return Discount != null && Discount.Active ? Discount : null; }}

        public virtual bool SelfDiscActive { get; set; }
        public virtual int SelfDiscMinQty { get; set; }

        /// <summary>positive: amount, negative: pecent between 0.0 and 1.0</summary>
        public virtual decimal SelfDiscValue { get; set; }

        public virtual string PriceCkUrl { get; set; }
        public virtual bool PriceCkActive { get; set; }
        public virtual bool PriceCkVariant { get; set; }
        public virtual decimal? PriceCkMinVal { get; set; }
        public virtual DateTime? PriceCkTimestamp { get; set; }
        public virtual DateTime? PriceCkDateFrom { get; set; }
        public virtual DateTime? PriceCkDateTo { get; set; }

        /// <summary>CustGroupId</summary>
        public virtual int? CGroupDisc0Id { get; set; }

        /// <summary>positive: amount, negative: pecent between 0.0 and 1.0</summary>
        public virtual decimal? CGroupDisc0Rate { get; set; }

        public virtual int? CGroupDisc1Id { get; set; }
        public virtual decimal? CGroupDisc1Rate { get; set; }

        public virtual int? CGroupDisc2Id { get; set; }
        public virtual decimal? CGroupDisc2Rate { get; set; }

        /// <summary>DirectDiscount:LoggedInUserOnly</summary>
        public virtual int? CGroupDisc3Id { get; set; }
        public virtual decimal? CGroupDisc3Rate { get; set; }

        public virtual int? CGroupDisc4Id { get; set; }
        public virtual decimal? CGroupDisc4Rate { get; set; }

        /// <summary>Positive: %discount, Negative: %profit</summary>
        public virtual decimal? BundleDiscountRate { get; set; }

        public virtual int? DropshipQty { get; set; }

        public virtual int? AassortmentThreshold { get; set; }

        public virtual bool HideSku
        {
            get { return 0 != (Flags & (int)ProductFlags.HideSku); }
            set { Flags = (Flags & ~(int)ProductFlags.HideSku) | (value ? (int)ProductFlags.HideSku : 0); }
        }

        public virtual bool HidePrice
        {
            get { return IsHidePrice(true, true); } 
            set { Flags = (Flags & ~(int) ProductFlags.HidePrice) | (value ? (int) ProductFlags.HidePrice : 0); }
        }

        public virtual bool IsHidePrice(bool price, bool login = false)
        {
            return 0 != (Flags & (int)ProductFlags.HidePrice) || 
                (price && DomExtensions.CombinedSalePrice(this) <= 0m) ||
                (login && IsPrice4LoginOnly());
        }

        public virtual bool IsPrice4LoginOnly()
        {
            if (0 != (Flags & (int) ProductFlags.Price4LoginOnly))
            {
                bool showPrice;
                decimal? discountRate;
                Dictionary<int, decimal> cateDiscountRates;
                if (0 == (DAL.Singleton<Helper.ISessionHelper>.Default.GetUserId(out showPrice, out  discountRate, out cateDiscountRates) ?? 0)) return true;
                else return !showPrice;
            }
            return false;
        }

        public virtual void UpdateCGroupDiscountFlag() 
        {
            if (CGroupDisc0Id != null || CGroupDisc1Id != null || CGroupDisc2Id != null || CGroupDisc4Id != null || CGroupDisc3Id != null || Variants.Any(m => !m.Deleted && m.CGroupDisc3Id != null))
                Flags |= (int)ProductFlags.CGroupDiscount;
            else Flags = Flags & ~(int)(ProductFlags.CGroupDiscount);
        }

        public virtual List<VariantBase> SetCGroupDiscountValueVari(int defCGroupId, decimal newSaleprice, IDiscount discount, decimal? earnRate = null)
        {
            CGroupDisc3Id = defCGroupId;
            CGroupDisc3Rate = newSaleprice > 0 && newSaleprice < Price ? -1 * (Price - newSaleprice) / Price : (decimal?)null;
            Flags |= (int)ProductFlags.CGroupDiscountNoVariant;

            //SalePrice = 0; SalePriceBk = null;

            return Variants.Where(m => !m.Deleted && CGroupDisc3Rate != null && (m.Quantity > 0 || !discount.Apply2InstockOnly) && (earnRate == null || m.CostPrice != null)).ToList().Action(vari =>
            {
                var newSalePrice2 = earnRate != null ? vari.CostPrice * (1 + earnRate) : vari.Price * (1 + CGroupDisc3Rate);

                newSalePrice2 = Vat.NormalizePrice(newSalePrice2, this.IncVat(newSalePrice2));

                //vari.SalePrice = 0; vari.SalePriceBk = null;

                vari.SetCGroupDiscountValue(defCGroupId, newSalePrice2 ?? 0, discount); 
            });
        }

        public virtual void SetCGroupDiscountValue(int defCGroupId, decimal newSaleprice, IDiscount discount = null)
        {
            if (defCGroupId != 0 && newSaleprice > 0 && newSaleprice < Price)
            {
                //SalePrice = 0;
                //SalePriceBk = null;
                //this.ApplySalePriceToVariants(true);

                CGroupDisc3Id = defCGroupId;
                CGroupDisc3Rate = -1 * (Price - newSaleprice) / Price;
                Flags = Flags & ~(int)(ProductFlags.CGroupDiscountNoVariant);

                UpdateCGroupDiscountFlag();
            }
        }

        public virtual bool ClearDirDiscount(IDiscount discount) 
        {
            return ClearDirDiscount(discount, false);
        }

        public virtual bool ClearDirDiscount(IDiscount discount, bool force)
        {
            if (discount.LoggedInUserOnly)
            {    
                if ((discount.IsScopePos ? Discount2 : Discount).GetId() == discount.GetId() || force || discount.CreatedDate < new DateTime(2025, 11, 15))
                {
                    if (discount.IsScopePos) 
                    { 
                        Discount2 = null;
                        SalePrice2 = 0;
                    }
                    else
                    {
                        Discount = null;

                        CGroupDisc3Id = null;
                        CGroupDisc3Rate = null;
                        SalePrice = 0;
                        SalePriceBk = null;

                        Variants.Where(m => m.CGroupDisc3Id != null).Action(m =>
                        {
                            m.CGroupDisc3Id = null;
                            m.CGroupDisc3Rate = null;
                        });

                        UpdateCGroupDiscountFlag();
                        Flags &= ~(int)(ProductFlags.CGroupDiscountScope | ProductFlags.CGroupDiscountNoVariant);
                    }

                    return true;
                }
            }
            else if ((discount.IsScopePos ? Discount2 : Discount).GetId() == discount.GetId() || force)
            {
                if (discount.IsScopePos)
                {
                    Discount2 = null;
                    SalePrice2 = 0;
                }
                else
                {
                    Discount = null;

                    SalePrice = 0;//SalePriceBk ?? 0;
                    SalePriceBk = null;
                    CGroupDisc3Id = null;
                    CGroupDisc3Rate = null;
                    Flags &= ~(int)(ProductFlags.CGroupDiscountScope | ProductFlags.CGroupDiscountNoVariant);
                }

                this.ApplySalePriceToVariantsEx(false, true, discount: discount);

                return true;
            }

            return false;
        }

        public virtual bool ShowPriceExc(Helper.SettingsBiz settings)
        {
            bool showPrice;
            decimal? discountRate;
            Dictionary<int, decimal> cateDiscountRates;
            if (settings != null && settings.Hidden.Fe4LoginShowPriceExcVat && 0 != (Flags & (int)ProductFlags.Price4LoginOnly)
                && DAL.Singleton<Helper.ISessionHelper>.Default.GetUserId(out showPrice, out  discountRate, out cateDiscountRates) > 0)
            {
                return showPrice; 
            }
            return false;
        }

        public virtual bool ShowPriceInc(Helper.SettingsBiz settings)
        {
            if (settings != null)
            {
                if (!settings.Hidden.Fe4LoginHidePriceIncVat || 0 == (Flags & (int) ProductFlags.Price4LoginOnly)) return true;

                bool showPrice;
                decimal? discountRate;
                Dictionary<int, decimal> cateDiscountRates;
                if (0 == (DAL.Singleton<Helper.ISessionHelper>.Default.GetUserId(out showPrice, out  discountRate, out cateDiscountRates) ?? 0)) return true;
                else return !showPrice;
            }

            return true;
        }

        public virtual IList<ProductLang> LanguagesFe
        {
            get { return _languages ?? (_languages = Languages.ToList()); }
        }

        public virtual IList<ProductLang> Languages
        {
            get { return Get(ref _list2); }
            set { _list2 = value; }
        }

        public virtual IList<Image> Images
        {
            get { return Get(ref _list1); }
            set { _list1 = value; }
        }

        public virtual IList<VariantBase> Variants
        {
            get { return Get(ref _list3); }
            set { _list3 = value; }
        }

        public virtual IEnumerable<VariantBase> VariantsFe
        {
            get { return Variants.Where(x => !x.Deleted && x.ProductStatus.ShowFrontEnd); }
            set { }
        }

        public virtual IList<CategoryProduct> CategoryProductsFe
        {
            get { return _categoryProducts ?? (_categoryProducts = CategoryProducts.ToList()); }
        }

        public virtual IList<CategoryProduct> CategoryProducts
        {
            get { return Get(ref _list0); }
            set { _list0 = value; }
        }

        public virtual IList<ProductBundle> ProductBundles
        {
            get { return Get(ref _list4); }
            set { _list4 = value; }
        }

        public virtual IList<ProductBundle> BundlingRefs
        {
            get { return Get(ref _list5); }
            set { _list5 = value; }
        }

        public virtual IList<IDom> DiscountProducts
        {
            get { return Get(ref _list6); }
            set { _list6 = value; }
        }

        public virtual IList<IDom> BonusDiscounts
        {
            get { return Get(ref _list7); }
            set { _list7 = value; }
        }

        public virtual IList<IDom> CouponProducts
        {
            get { return Get(ref _list8); }
            set { _list8 = value; }
        }

        public virtual IList<IDom> OrderDetails
        {
            get { return Get(ref _list9); }
            set { _list9 = value; }
        }

        public virtual IList<IDom> OrderDetailBundles
        {
            get { return Get(ref _list10); }
            set { _list10 = value; }
        }

        public virtual IList<IDom> OrderDetailGifts
        {
            get { return Get(ref _list11); }
            set { _list11 = value; }
        }

        public virtual IList<ProductAccessory> Accessories
        {
            get { return Get(ref _list12); }
            set { _list12 = value; }
        }

        public virtual IList<ProductAccessory> AccessoryRefs
        {
            get { return Get(ref _list13); }
            set { _list13 = value; }
        }

        public virtual IList<ProductEmailMUser> ProductEmailMUsers
        {
            get { return Get(ref _list14); }
            set { _list14 = value; }
        }

        public virtual IList<ProductExtraInformation> ExtraInformation
        {
            get { return Get(ref _list15); }
            set { _list15 = value; }
        }

        public virtual IList<ProductTechSVal> ProductTechSVals
        {
            get { return Get(ref _list16); }
            set { _list16 = value; }
        }

        /// <summary>admin only</summary>
        public virtual IList<ProductRecurDisc> ProductRecurDiscs
        {
            get { return Get(ref _list17); }
            set { _list17 = value; }
        }

        public virtual IList<IDom> BongerProducts
        {
            get { return Get(ref _list18); }
            set { _list18 = value; }
        }

        public virtual IList<ProductSerial> ProductSerials
        {
            get {return _list19 ?? (_list19 = Activator.CreateInstance<List<ProductSerial>>()); }
            set { _list19 = value; }
        }

        public virtual IList<ProductBundleGroup> ProductBundleGroups
        {
            get { return _list20 ?? (_list20 = Activator.CreateInstance<List<ProductBundleGroup>>()); }
            set { _list20 = value; }
        }

        public virtual bool IsRecurringDiscount
        {
            get
            {
                var paymentMgr = DAL.Singleton<DAL.Interfaces.IPaymentMethodMgr>.Default;
                return 0 != (Flags & (int)Models.ProductFlags.RecurringOrder) && paymentMgr != null &&
                       paymentMgr.GetAll(new DAL.DefaultDb(), Models.RecurringOrderMode.SpecificProducts, true).SafeCount() != 0;
            }
        }

        /// <summary>Front end</summary>
        public virtual IList<ProductRecurDisc> ProductRecurDiscsFe
        {
            get { return IsRecurringDiscount ? ProductRecurDiscs : new ProductRecurDisc[0]; }
        }
    }

    public partial class ProductDel : ProductBase
    {
        public ProductDel() { }
        public ProductDel(ProductBase _base)
        {
            foreach (System.Reflection.FieldInfo prop in typeof(ProductDel).GetFields())
                GetType().GetField(prop.Name).SetValue(this, prop.GetValue(_base));

            foreach (System.Reflection.PropertyInfo prop in typeof(ProductDel).GetProperties())
                if (GetType().GetProperty(prop.Name).CanWrite)
                    GetType().GetProperty(prop.Name).SetValue(this, prop.GetValue(_base, null), null);
        }
    }

    public partial class Product : ProductBase
    {
        public Product() { }
        public Product(ProductBase _base)
        {
            foreach (System.Reflection.FieldInfo prop in typeof(Product).GetFields())
                GetType().GetField(prop.Name).SetValue(this, prop.GetValue(_base));

            foreach (System.Reflection.PropertyInfo prop in typeof(Product).GetProperties())
                if (GetType().GetProperty(prop.Name).CanWrite)
                    GetType().GetProperty(prop.Name).SetValue(this, prop.GetValue(_base, null), null);
        }
    }

}
