using Claw.Modules.Eshop.Domain.Interfaces;
using Claw.Modules.Eshop.Models;
using NPOI.HPSF;
using System;
using System.Collections.Generic;
using System.Text;
using static ICSharpCode.SharpZipLib.Zip.ExtendedUnixData;

namespace Claw.Modules.Eshop.Domain
{
    public partial class VariantBase : Tuple<VariantPropVal, ProductBundle, IDom, IDom, IDom, VariantLang, VariantImage, IDom, IDom>, IDisplayOrder, IStatus, IVariant
    {
        private bool _active;

        public virtual int Id { get; set; }
        public virtual bool Deleted { get; set; }
        public virtual string SKU { get; set; }
        public virtual string BarCode { get; set; }
        //public virtual string NvitCode { get; set; }

        /// <summary>Quantity1 + Quantity2</summary>
        public virtual int Quantity { get; set; }
        public virtual int Quantity1 { get; set; }
        public virtual int Quantity2 { get; set; }
        public virtual int BackOrderQty { get; set; }
        public virtual int? ShoppingQty { get; set; }
        public virtual decimal? Price { get; set; }
        public virtual decimal? SalePrice { get; set; }

        /// <summary>Cpos only</summary>
        public virtual decimal? SalePrice2 { get; set; }
        public virtual decimal? SalePriceBk { get; set; }
        public virtual decimal? CostPrice { get; set; }
        public virtual decimal? Weight { get; set; }
        public virtual Status ProductStatus { get; set; }
        public virtual int DisplayOrder { get; set; }
        public virtual string Image { get; set; }
        public virtual int ISeq { get; set; }
        
        public virtual bool Active { get { return _active && !Deleted; } set { _active = value; } }

        public virtual string Location { get; set; }
        public virtual IDiscount Discount { get; set; }

        /// <summary>Cpos only</summary>
        public virtual IDiscount Discount2 { get; set; }
        public virtual ProductBase Product { get; set; }
        public virtual DateTime? BackDate { get; set; }

        public virtual string PriceCkUrl { get; set; }
        public virtual decimal? PriceCkMinVal { get; set; }
        public virtual int? DropshipQty { get; set; }
        public virtual int? EmailMUsers { get; set; }

        public virtual int? CGroupDisc3Id { get; set; }
        public virtual decimal? CGroupDisc3Rate { get; set; }

        public virtual bool HidePrice
        {
            get { return IsHidePrice(true, true); }
        }

        public virtual bool IsHidePrice(bool price, bool login = false)
        {
            return 0 != (Product.Flags & (int)Models.ProductFlags.HidePrice) ||
                (price && PriceFE <= 0m) ||
                (login && Product.IsPrice4LoginOnly());
        }

        public virtual void SetCGroupDiscountValue(int defCGroupId, decimal newSaleprice, IDiscount discount = null)
        {
            if (defCGroupId != 0 && newSaleprice > 0 && newSaleprice < Price)
            {
                //SalePrice = 0;
                //SalePriceBk = null;
                //Product.CalculateSalePrice();

                CGroupDisc3Id = defCGroupId;
                CGroupDisc3Rate = -1 * (Price - newSaleprice) / Price;

                Product.UpdateCGroupDiscountFlag();
            }
        }
        public virtual bool ClearDirDiscount(IDiscount discount)
        {
            if (discount.LoggedInUserOnly)
            {
                this.Discount = null;

                CGroupDisc3Id = null;
                CGroupDisc3Rate = null;

                Product.UpdateCGroupDiscountFlag();

                return true;
            }
            else if (Discount.GetId() == discount.GetId())
            {
                this.Discount = null;

                SalePrice = 0;//SalePriceBk ?? 0;
                SalePriceBk = null;
                Product.CalculateSalePrice(Settings);

                return true;
            }

            return false;
        }

        public virtual IList<VariantPropVal> VariantPropVals
        {
            get { return Get(ref _list0); }
            set { _list0 = value; }
        }

        public virtual IList<ProductBundle> ProductBundles
        {
            get { return Get(ref _list1); }
            set { _list1 = value; }
        }

        public virtual IList<IDom> OrderDetails
        {
            get { return Get(ref _list2); }
            set { _list2 = value; }
        }

        public virtual IList<IDom> OrderDetailBundles
        {
            get { return Get(ref _list3); }
            set { _list3 = value; }
        }

        public virtual IList<IDom> OrderDetailGifts
        {
            get { return Get(ref _list4); }
            set { _list4 = value; }
        }

        public virtual IList<VariantLang> Languages
        {
            get { return Get(ref _list5); }
            set { _list5 = value; }
        }

        public virtual IList<VariantImage> Images
        {
            get { return Get(ref _list6); }
            set { _list6 = value; }
        }

        public virtual IList<IDom> BongerProducts
        {
            get { return Get(ref _list7); }
            set { _list7 = value; }
        }

        public virtual IList<IDom> DiscountProducts
        {
            get { return Get(ref _list8); }
            set { _list8 = value; }
        }
    }

    public partial class VariantDel : VariantBase
    {
        public VariantDel() { }
        public VariantDel(VariantBase _base)
        {
            foreach (System.Reflection.FieldInfo prop in _base.GetType().GetFields())
                GetType().GetField(prop.Name).SetValue(this, prop.GetValue(_base));

            foreach (System.Reflection.PropertyInfo prop in _base.GetType().GetProperties())
                if (GetType().GetProperty(prop.Name).CanWrite)
                    GetType().GetProperty(prop.Name).SetValue(this, prop.GetValue(_base, null), null);
        }
    }

    public partial class Variant : VariantBase
    {
        public Variant() { }
        public Variant(VariantBase _base)
        {
            foreach (System.Reflection.FieldInfo prop in _base.GetType().GetFields())
                GetType().GetField(prop.Name).SetValue(this, prop.GetValue(_base));

            foreach (System.Reflection.PropertyInfo prop in _base.GetType().GetProperties())
                if (GetType().GetProperty(prop.Name).CanWrite)
                    GetType().GetProperty(prop.Name).SetValue(this, prop.GetValue(_base, null), null);
        }
    }
}