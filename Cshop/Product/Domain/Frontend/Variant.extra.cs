using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Claw.Core;
using Claw.Modules.Eshop.Controllers.Admin;
using Claw.Modules.Eshop.DAL;
using Claw.Modules.Eshop.DAL.Interfaces;
using Claw.Modules.Eshop.Helper;
using Claw.Modules.Eshop;

namespace Claw.Modules.Eshop.Domain
{
    public partial class VariantBase : Tuple<VariantPropVal, ProductBundle, IDom, IDom, IDom, VariantLang, VariantImage, IDom, IDom>, IDisplayOrder, IStatus
    {

        public virtual bool ValidToShow()
        {
            int errQty;
            return ValidToShow(0, out errQty);  
        }

        public virtual bool ValidToShow(int minQty, out int errQty)
        {
            if (Product.AllowBackorder || minQty == 0) errQty = 0;
            else errQty = minQty - Quantity - (DropshipQty ?? 0);

            if (Active && ProductStatus.ShowFrontEnd)
            {
                var hidden = Settings.Hidden;
                return (Quantity + (DropshipQty ?? 0)) >= (minQty > 0 ? 0 : 1) || Product.AllowBackorder || (minQty == 0 && hidden.ShowZeroQtyProduct && !(ProductStatus.GetId(0) ?? 0).InList(hidden.HideZeroQtyProdStatusIds()));
            }
            return false;
        }

        public virtual string GetImageFrist
        {
            get
            {
                var fileName = "/Claw/Sites/Default/Modules/Eshop/Images/Variant/no-img.jpg";
                if (Image != null)
                {
                    fileName = Image;
                }
                return ModuleUrl.ImagePath(fileName).TrimStart('~');
            }
        }
        public virtual string GetThumbFrist
        {
            get
            {
                var fileName = "/Claw/Sites/Default/Modules/Eshop/Images/Variant/no-thumb-img.jpg";
                if (Image != null)
                {
                    fileName = Image;
                }
                return ModuleUrl.ImageThumbPath(fileName).TrimStart('~');
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

        public virtual bool SelfDiscountApplied
        {
            get { return Math.Abs(SalePriceSelfDisc - (SalePrice ?? 0)) >= 0.1m; }
        }

        public virtual decimal SelfDiscountPrice { get { return GetSelfDiscountPrice(); } }

        public virtual decimal GetSelfDiscountPrice(bool skipCustDisc = false)
        {
            var settings = Settings.Hidden;
            var combinedPrice = SalePrice > 0 ? Math.Min(SalePrice ?? 0, Price ?? 0) : (Price ?? 0);
            var combinedPriceCust = settings.CustomerDiscountUseSalePrice ? combinedPrice : (Price ?? 0);
            var combinedPriceSelf = settings.CustomerDiscountUseSalePrice ? combinedPrice : (Price ?? 0);

            decimal newSalePriceSelf = combinedPrice, newSalePriceCust = combinedPrice;

            if (Product.CanApplySelfDiscount(false, true))
                newSalePriceSelf = Math.Max(0m, combinedPriceSelf - (Product.SelfDiscValue > 0 ? (Product.ExcVat(Product.SelfDiscValue) ?? 0) : Math.Abs(Product.SelfDiscValue * combinedPrice)));

            if (!skipCustDisc && Product.CanApplySelfDiscount(true))
                newSalePriceCust = Math.Max(0m, combinedPriceCust * (1 - Product.GetCustDiscountRate()));

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
            get { return Product.CanSelfDiscount ? SelfDiscountPrice : (SalePrice ?? 0); }
        }

        private decimal SalePriceSelfDiscInc
        {
            get { return this.IncVat(SalePriceSelfDisc) ?? 0; }
        }

        private decimal? SalePriceCGroupDisc
        {
            get
            {
                var settings = Settings.Hidden;

                if (!settings.EnableCustomerDiscount) return null;

                var cgDiscRate = Product.CGroupDiscRate(this);

                var newSalePriceSelf = GetSelfDiscountPrice(true);

                var combinedPrice = SalePrice > 0 ? Math.Min(SalePrice ?? 0, Price ?? 0) : (Price ?? 0);
                var combinedPriceCgDisc = settings.CustomerDiscountUseSalePrice ? combinedPrice : (Price ?? 0);

                var newSalePriceCgDisc = cgDiscRate == null ? 0 : combinedPriceCgDisc - (cgDiscRate.Item2 >= 0 ? (Product.ExcVat(cgDiscRate.Item2) ?? 0) : combinedPriceCgDisc * Math.Abs(cgDiscRate.Item2));

                var newSalePrice = newSalePriceSelf > 0 && newSalePriceCgDisc > 0
                    ? Math.Min(newSalePriceSelf, newSalePriceCgDisc)
                    : Math.Max(newSalePriceSelf, newSalePriceCgDisc);

                return newSalePriceCgDisc > 0 && newSalePrice > 0 && newSalePrice < combinedPrice ? newSalePrice : (decimal?)null;
            }
        }

        public virtual decimal PriceFEInc
        {
            get { return (decimal)this.IncVat(PriceFE); }
        }

        public virtual decimal? PriceFEInc2
        {
            get { return SalePrice2 > 0 && SalePrice2 < Price ? SalePriceInc2 : (decimal?)null; }
        }

        public virtual decimal NonMemberPriceFE
        {
            get { return SalePriceCGroupDisc ?? (HasSalePrice ? SalePriceSelfDisc : (Price ?? 0)); }
        }

        public virtual decimal NonMemberPriceFEInc
        {
            get { return (decimal)this.IncVat(NonMemberPriceFE); }
        }

        public virtual decimal PriceFE
        {
            get
            {
                var coupon = ShoppingCart.SafeProp(m => m.Coupon);
                return coupon != null && coupon.Exclusive && coupon.DiscountType == 1 /*MoneyTotal*/ ? (Price ?? Product.Price) : NonMemberPriceFE * (1 - Singleton<IMembershipMgr>.Default.GetDiscountRate(ShoppingCart));
            }
        }

        public virtual decimal PriceInc
        {
            get { return (decimal)this.IncVat(Price); }
        }

        public virtual decimal SalePriceInc
        {
            get { return (decimal)this.IncVat(SalePrice); }
        }

        public virtual decimal? SalePriceInc2
        {
            get { return this.IncVat(SalePrice2); }
        }

        public virtual decimal PercentDiscount
        {
            get { return HasSalePrice || Product.HasMembershipPrice ? Eshop.Product.Helper.EShopHelper.PercentDiscount(PriceInc, PriceFEInc) : 0; }
        }

        public virtual string Description
        {
            get
            {
                var lang = ClawContext.Current.CurrentLanguage.LanguageCode;

                var ret = Languages.MatchOrFirst(x => x.LangCode.EqualsIgCase(lang));
                return ret == null ? "" : ret.Description;

            }
        }
    }
}