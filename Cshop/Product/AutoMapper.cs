using System;
using System.Collections.Generic;
using AutoMapper;
using Claw.Core.Interfaces;
using Claw.Modules.Eshop.DAL;
using Claw.Modules.Eshop.Models;
using Claw.Modules.Eshop.Domain;

namespace Claw.Modules.Eshop.Product
{
    public class AutoMapper
    {
        public static void Configuration()
        {
            System.Threading.Tasks.Task.Run(() =>
            {
                new CategoryMgr();
                new LocationMgr();
                new ManufacturerMgr();
                new ProductMgr();
                new PropertyMgr();
                new PropValMgr();
                new StatusMgr();
                new SupplierMgr();
                new VariantMgr();
                Controllers.Admin.BaseController.StartJob<Eshop.Helper.ProductCronJob>(5);     
            });

            Mapper.CreateMap<Category, CategoryModel>()
                .ForMember(m => m.Raw, conf => conf.MapFrom(m => m))
                .ForMember(m => m.ImageThumb, conf => conf.MapFrom(m => ModuleUrl.ImageThumbPath(m.Image).ResolveClientUrl()))
                .ForMember(m => m.ImageFsUrl, conf => conf.MapFrom(m => ModuleUrl.ImagePath(m.Image).ResolveClientUrl()))
                .ForMember(m => m.Banner0Thumb, conf => conf.MapFrom(m => ModuleUrl.ImageThumbPath(m.Banner0).ResolveClientUrl()))
                .ForMember(m => m.Banner0FsUrl, conf => conf.MapFrom(m => ModuleUrl.ImagePath(m.Banner0).ResolveClientUrl()))
                .ForMember(m => m.Banner1Thumb, conf => conf.MapFrom(m => ModuleUrl.ImageThumbPath(m.Banner1).ResolveClientUrl()))
                .ForMember(m => m.Banner1FsUrl, conf => conf.MapFrom(m => ModuleUrl.ImagePath(m.Banner1).ResolveClientUrl()))
                .ForMember(m => m.Banner2Thumb, conf => conf.MapFrom(m => ModuleUrl.ImageThumbPath(m.Banner2).ResolveClientUrl()))
                .ForMember(m => m.Banner2FsUrl, conf => conf.MapFrom(m => ModuleUrl.ImagePath(m.Banner2).ResolveClientUrl()));
            Mapper.CreateMap<CategoryLang, CategoryLangModel>()
                .ForMember(m => m.ParentId, conf => conf.MapFrom(m => m.Category.Parent.GetId(null)))
                .ForMember(m => m.CategoryId, conf => conf.MapFrom(m => m.Category.Id));

            Mapper.CreateMap<CategoryProduct, CategoryProductModel>()
                .ForMember(m => m.CategoryId, conf => conf.MapFrom(m => m.Category.Id))
                .ForMember(m => m.ProductId, conf => conf.MapFrom(m => m.Product.Id))
                .ForMember(m => m.ProductTotalSales, conf => conf.MapFrom(m => m.Product.TotalSales))
                .ForMember(m => m.ProductPublishedDate, conf => conf.MapFrom(m => m.Product.PublishedDate))
                .ForMember(m => m.ProductModifiedDate, conf => conf.MapFrom(m => m.Product.ModifiedDate))
                .ForMember(m => m.ProductCreatedDate, conf => conf.MapFrom(m => m.Product.CreatedDate))
                .ForMember(m => m.ProductCostPrice, conf => conf.MapFrom(m => m.Product.CostPrice.ToOMoney()))
                .ForMember(m => m.ProductCostPriceInc, conf => conf.MapFrom(m => m.Product.IncVat(m.Product.CostPrice).NormalizePriceInc(null, false)))
                .ForMember(m => m.ProductPrice, conf => conf.MapFrom(m => m.Product.Price))
                .ForMember(m => m.ProductPriceInc, conf => conf.MapFrom(m => m.Product.IncVat(m.Product.Price)))
                .ForMember(m => m.ProductSalePrice, conf => conf.MapFrom(m => m.Product.SalePrice.NullIfZero(true)))
                .ForMember(m => m.ProductSalePriceInc, conf => conf.MapFrom(m => m.Product.IncVat(m.Product.SalePrice).NullIfZero(true)))
                .ForMember(m => m.ProductCombinedSalePrice, conf => conf.MapFrom(m => m.Product.CombinedSalePrice(null).NullIfZero(true)))
                .ForMember(m => m.ProductCombinedSalePriceInc, conf => conf.MapFrom(m => m.Product.CombinedSalePriceInc(null).NullIfZero(true)))
                .ForMember(m => m.ProductVat, conf => conf.MapFrom(m => m.Product.Vat.ToOFloat()))
                //.ForMember(m => m.ProductName, conf => conf.MapFrom(m => m.Product.GetName(null)))
                //.ForMember(m => m.ProductImage, conf => conf.MapFrom(m => m.Product.Image))
                .ForMember(m => m.ProductSKU, conf => conf.MapFrom(m => m.Product.SKU))
                .ForMember(m => m.DisplayOrder, conf => conf.MapFrom(m => m.Product.DisplayOrder));

            Mapper.CreateMap<ProductAccessory, ProductAccessoryModel>()
                .ForMember(m => m.ProductId, conf => conf.MapFrom(m => m.Product.Id))
                .ForMember(m => m.AccessoryId, conf => conf.MapFrom(m => m.Accessory.Id))
                .ForMember(m => m.AccessoryPrice, conf => conf.MapFrom(m => m.Accessory.CombinedPrice(m.AcceVariant)))
                .ForMember(m => m.AccessoryPriceInc, conf => conf.MapFrom(m => m.Accessory.IncVat(m.Accessory.CombinedPrice(m.AcceVariant))))
                .ForMember(m => m.AccessorySalePrice, conf => conf.MapFrom(m => m.Accessory.CombinedSalePrice(m.AcceVariant, false).NullIfZero(true)))
                .ForMember(m => m.AccessorySalePriceInc, conf => conf.MapFrom(m => m.Accessory.IncVat(m.Accessory.CombinedSalePrice(m.AcceVariant, false)).NullIfZero(true)))
                .ForMember(m => m.AccessoryName, conf => conf.MapFrom(m => m.Accessory.GetName(m.AcceVariant)))
                .ForMember(m => m.AccessoryImage, conf => conf.MapFrom(m => m.Accessory.GetImage(m.AcceVariant)))
                .ForMember(m => m.AccessoryVariantNum, conf => conf.MapFrom(m => m.AcceVariant != null ? 0 : m.Accessory.VariantNum))
                .ForMember(m => m.BundleGroupId, conf => conf.MapFrom(m => m.BundleGroup.SafeProp(z => (int?)z.Id, null)))
                .ForMember(m => m.BundleGroupName, conf => conf.MapFrom(m => m.BundleGroup.SafeProp(z => z.Name)));

            Mapper.CreateMap<ProductBundle, ProductBundleModel>()
                .ForMember(m => m.BundleId, conf => conf.MapFrom(m => m.Bundle.Id))
                .ForMember(m => m.ProductId, conf => conf.MapFrom(m => m.Product.Id))
                .ForMember(m => m.ProductQty, conf => conf.MapFrom(m => m.Variant == null ? m.Product.Quantity : m.Variant.Quantity))
                .ForMember(m => m.ProductAllowBackorder, conf => conf.MapFrom(m => m.Product.AllowBackorder))
                .ForMember(m => m.VariantId, conf => conf.MapFrom(m => m.Variant.GetId(null)))
                .ForMember(m => m.PropDesc, conf => conf.MapFrom(m => m.Variant.GetPropDesc()))
                .ForMember(m => m.ProductName, conf => conf.MapFrom(m => m.Product.GetName(null)))
                .ForMember(m => m.ProductImage, conf => conf.MapFrom(m => m.Product.GetImage(m.Variant)))
                .ForMember(m => m.ProductCostPrice, conf => conf.MapFrom(m => m.Variant != null ? m.Variant.CostPrice : m.Product.CostPrice))
                .ForMember(m => m.ProductCostPriceInc, conf => conf.MapFrom(m => m.Product.IncVat(m.Variant != null ? m.Variant.CostPrice : m.Product.CostPrice)))
                .ForMember(m => m.ProductPrice, conf => conf.MapFrom(m => m.Variant != null ? m.Variant.Price : m.Product.Price))
                .ForMember(m => m.ProductPriceInc, conf => conf.MapFrom(m => m.Product.IncVat(m.Variant != null ? m.Variant.Price : m.Product.Price)))
                .ForMember(m => m.ProductSalePrice, conf => conf.MapFrom(m => (m.Variant != null ? m.Variant.SalePrice : m.Product.SalePrice).NullIfZero(true)))
                .ForMember(m => m.ProductSalePriceInc, conf => conf.MapFrom(m => m.Product.IncVat(m.Variant != null ? m.Variant.SalePrice : m.Product.SalePrice).NullIfZero(true)))
                .ForMember(m => m.ProductVat, conf => conf.MapFrom(m => m.Product.Vat.ToOFloat()))
                .ForMember(m => m.ProductVariantNum, conf => conf.MapFrom(m => m.Product.VariantNum))
                .ForMember(m => m.ProductSKU, conf => conf.MapFrom(m => m.Variant != null ? m.Variant.SKU : m.Product.SKU))
                .ForMember(m => m.WUnit, conf => conf.MapFrom(m => m.Product.WUnit))
                .ForMember(m => m.Weight, conf => conf.MapFrom(m => m.Variant != null ? m.Variant.Weight : m.Product.Weight));

            Mapper.CreateMap<Status, StatusModel>()
                .ForMember(m => m.Raw, conf => conf.MapFrom(m => m))
                //.ForMember(m => m.ZeroQtyStatusId, conf => conf.MapFrom(m => m.ZeroQtyStatus.Id))
                .ForMember(m => m.ImageThumb, conf => conf.MapFrom(m => ModuleUrl.ImageThumbPath(m.Icon).ResolveClientUrl()))
                .ForMember(m => m.ImageFsUrl, conf => conf.MapFrom(m => ModuleUrl.ImagePath(m.Icon).ResolveClientUrl()));
            Mapper.CreateMap<StatusLang, StatusLangModel>()
                .ForMember(m => m.StatusId, conf => conf.MapFrom(m => m.Status.Id));

            Mapper.CreateMap<Domain.ProductBase, ProductModel>()
                .ForMember(m => m.Raw, conf => conf.MapFrom(m => m))
                .ForMember(m => m.VatId, conf => conf.MapFrom(m => m.Vat.Id))
                .ForMember(m => m.Vat, conf => conf.MapFrom(m => m.Vat.ToOFloat()))
                .ForMember(m => m.SecondVatId, conf => conf.MapFrom(m => m.SecondVat.GetId(null)))
                .ForMember(m => m.SecondVat, conf => conf.MapFrom(m => m.SecondVat.ToOFloat()))
                .ForMember(m => m.PriceInc, conf => conf.MapFrom(m => m.IncVat(m.Price).NormalizePriceInc(null, false)))
                .ForMember(m => m.CostPrice, conf => conf.MapFrom(m => m.CostPrice.ToOMoney()))
                .ForMember(m => m.CostPriceInc, conf => conf.MapFrom(m => m.IncVat(m.CostPrice).NormalizePriceInc(null, false)))
                .ForMember(m => m.SalePrice, conf => conf.MapFrom(m => m.SalePrice.NullIfZero(true)))
                .ForMember(m => m.SalePriceInc, conf => conf.MapFrom(m => m.IncVat(m.SalePrice).NormalizePriceInc(0, false).NullIfZero(true)))
                .ForMember(m => m.ProductStatusId, conf => conf.MapFrom(m => m.ProductStatus.Id))
                .ForMember(m => m.ManufacturerId, conf => conf.MapFrom(m => m.Manufacturer.GetId(null)))
                .ForMember(m => m.ManufacturerName, conf => conf.MapFrom(m => m.Manufacturer.GetName()))
                .ForMember(m => m.SupplierId, conf => conf.MapFrom(m => m.Supplier.GetId(null)))
                .ForMember(m => m.SupplierName, conf => conf.MapFrom(m => m.Supplier.GetName()))
                .ForMember(m => m.Accessories, conf => conf.MapFrom(m => null as List<ProductAccessoryModel>))
                .ForMember(m => m.Images, conf => conf.MapFrom(m => null as List<ImageModel>))
                .ForMember(m => m.Variants, conf => conf.MapFrom(m => null as List<VariantModel>))
                .ForMember(m => m.BackDate, conf => conf.MapFrom(m => (ODateTime)m.BackDate))
                .ForMember(m => m.IsAassortment, conf => conf.MapFrom(m => 0 != (m.Flags & (int)ProductFlags.Aassortment)))
                .ForMember(m => m.VisibleSku, conf => conf.MapFrom(m => 0 == (m.Flags & (int)ProductFlags.HideSku)))
                .ForMember(m => m.EmailMarketing, conf => conf.MapFrom(m => 0 != (m.Flags & (int)ProductFlags.EmailMarketing)))
                .ForMember(m => m.RecurringOrder, conf => conf.MapFrom(m => 0 != (m.Flags & (int)ProductFlags.RecurringOrder)))
                .ForMember(m => m.HidePrice, conf => conf.MapFrom(m => m.IsHidePrice(false, false)))
                .ForMember(m => m.SelfDiscType, conf => conf.MapFrom(m => m.SelfDiscValue > 0))
                .ForMember(m => m.SelfDiscMinQty, conf => conf.MapFrom(m => m.SelfDiscMinQty.NullIf(z => z == 0)))
                .ForMember(m => m.SelfDiscValue, conf => conf.MapFrom(m => (m.SelfDiscValue > 0 ? 1 : 100) * Math.Abs(m.SelfDiscValue).NullIf(z => z == 0)))
                .ForMember(m => m.PriceCkTimestamp, conf => conf.MapFrom(m => (ODateTime)m.PriceCkTimestamp))
                .ForMember(m => m.PriceCkDateFrom, conf => conf.MapFrom(m => (ODateTime)m.PriceCkDateFrom))
                .ForMember(m => m.PriceCkDateTo, conf => conf.MapFrom(m => (ODateTime)m.PriceCkDateTo))
                .ForMember(m => m.HideZeroQtyProd, conf => conf.MapFrom(m => 0 != (m.Flags & (int)ProductFlags.HideZeroQtyProd)))
                .ForMember(m => m.Price4LoginOnly, conf => conf.MapFrom(m => 0 != (m.Flags & (int)ProductFlags.Price4LoginOnly)))
                .ForMember(m => m.RegSerialNumber, conf => conf.MapFrom(m => 0 != (m.Flags & (int)ProductFlags.SerialNumber)))
                .ForMember(m => m.BundleHideSubItems, conf => conf.MapFrom(m => 0 != (m.Flags & (int)ProductFlags.BundleHideSubItems)))
                .ForMember(m => m.BundleNeedSelectAll, conf => conf.MapFrom(m => 0 != (m.Flags & (int)ProductFlags.BundleNeedSelectAll)))
                .ForMember(m => m.BundleShowProdInStock, conf => conf.MapFrom(m => 0 != (m.Flags & (int)ProductFlags.BundleShowProdInStock)))
                .ForMember(m => m.BundleFetchDescFromProducts, conf => conf.MapFrom(m => 0 != (m.Flags & (int)ProductFlags.BundleFetchDescFromProds)))
                .ForMember(m => m.Controlled, conf => conf.MapFrom(m => 0 != (m.Flags & (int)ProductFlags.Controlled)))
                .ForMember(m => m.BackOrderQty, conf => conf.MapFrom(m => m.Id == 1 ? 0 : m.BackOrderQty))
                .ForMember(m => m.BundleDiscountRate, conf => conf.MapFrom(m => m.BundleDiscountRate == null ? (decimal?) null : Math.Abs(m.BundleDiscountRate.Value * 100m)))
                .ForMember(m => m.BundleDiscProfit, conf => conf.MapFrom(m => m.BundleDiscountRate < 0));
            
            Mapper.CreateMap<ProductLang, ProductLangModel>()
                .ForMember(m => m.ProductId, conf => conf.MapFrom(m => m.Product.Id));

            Mapper.CreateMap<ImageLang, ImageLangModel>()
                .ForMember(m => m.ImageId, conf => conf.MapFrom(m => m.Image.Id));
            Mapper.CreateMap<Image, ImageModel>()
                .ForMember(m => m.Raw, conf => conf.MapFrom(m => m))
                .ForMember(m => m.Thumb, conf => conf.MapFrom(m => ModuleUrl.ImageThumbPath(m.FileName).ResolveClientUrl()))
                .ForMember(m => m.FsUrl, conf => conf.MapFrom(m => ModuleUrl.ImagePath(m.FileName).ResolveClientUrl()));

            Mapper.CreateMap<VariantBase, VariantModel>()
                .ForMember(m => m.Raw, conf => conf.MapFrom(m => m))
                .ForMember(m => m.Weight, conf => conf.MapFrom(m => new OFloat(m.Weight)))
                .ForMember(m => m.CombinedWeight, conf => conf.MapFrom(m => m.Product.CombinedWeight(m).ToOFloat()))
                .ForMember(m => m.Price, conf => conf.MapFrom(m => m.Price.ToOMoney(true)))
                .ForMember(m => m.PriceInc, conf => conf.MapFrom(m => m.IncVat(m.Price).NormalizePriceInc(null, true)))
                .ForMember(m => m.CostPrice, conf => conf.MapFrom(m => m.CostPrice.ToOMoney(true)))
                .ForMember(m => m.CostPriceInc, conf => conf.MapFrom(m => m.IncVat(m.CostPrice).NormalizePriceInc(null, true)))
                .ForMember(m => m.SalePrice, conf => conf.MapFrom(m => m.SalePrice.NullIfZero(true)))
                .ForMember(m => m.SalePriceInc, conf => conf.MapFrom(m => m.IncVat(m.SalePrice).NormalizePriceInc(0, true).NullIfZero(true)))
                .ForMember(m => m.ProductId, conf => conf.MapFrom(m => m.Product.Id))
                .ForMember(m => m.ProductName, conf => conf.MapFrom(m => m.Product.GetName(null)))
                .ForMember(m => m.ProductWeight, conf => conf.MapFrom(m => m.Product.Weight))
                .ForMember(m => m.ProductShippingType, conf => conf.MapFrom(m => m.Product.ShippingType))
                .ForMember(m => m.ProductStatusId, conf => conf.MapFrom(m => m.ProductStatus.Id))
                .ForMember(m => m.ProductStatusName, conf => conf.MapFrom(m => m.ProductStatus.GetName()))
                .ForMember(m => m.AllowBackorder, conf => conf.MapFrom(m => m.Product.AllowBackorder))
                .ForMember(m => m.ProductVisibility, conf => conf.MapFrom(m => m.Product.Visibility))
                .ForMember(m => m.PropDesc, conf => conf.MapFrom(m => m.GetPropDesc()))
                .ForMember(m => m.ImageThumb,
                    conf => conf.MapFrom(m => ModuleUrl.ImageThumbPath(m.Image).ResolveClientUrl()))
                    .ForMember(m => m.BackDate, conf => conf.MapFrom(m => (ODateTime)m.BackDate))
                .ForMember(m => m.ImageFsUrl, conf => conf.MapFrom(m => ModuleUrl.ImagePath(m.Image).ResolveClientUrl()));
            Mapper.CreateMap<VariantLang, VariantLangModel>()
                .ForMember(m => m.VariantId, conf => conf.MapFrom(m => m.Variant.Id))
                .ForMember(m => m.DescriptionV, conf => conf.MapFrom(m => m.Description));

            Mapper.CreateMap<VariantImage, VariantImageModel>()
                .ForMember(m => m.Raw, conf => conf.MapFrom(m => m))
                .ForMember(m => m.Thumb, conf => conf.MapFrom(m => ModuleUrl.ImageThumbPath(m.FileName).ResolveClientUrl()))
                .ForMember(m => m.FsUrl, conf => conf.MapFrom(m => ModuleUrl.ImagePath(m.FileName).ResolveClientUrl()));
            Mapper.CreateMap<VariantImageLang, VariantImageLangModel>()
                .ForMember(m => m.VariantImageId, conf => conf.MapFrom(m => m.VariantImage.Id));

            Mapper.CreateMap<Property, PropertyModel>()
                .ForMember(m => m.Raw, conf => conf.MapFrom(m => m))
                .ForMember(m => m.ImageThumb, conf => conf.MapFrom(m => ModuleUrl.ImageThumbPath(m.Icon).ResolveClientUrl()))
                .ForMember(m => m.ImageFsUrl, conf => conf.MapFrom(m => ModuleUrl.ImagePath(m.Icon).ResolveClientUrl()));

            Mapper.CreateMap<PropertyLang, PropertyLangModel>()
                .ForMember(m => m.PropertyId, conf => conf.MapFrom(m => m.Property.Id));

            Mapper.CreateMap<PropVal, PropValModel>()
                .ForMember(m => m.Raw, conf => conf.MapFrom(m => m))
                .ForMember(m => m.Icon, conf => conf.MapFrom(m => m.Icon))
                .ForMember(m => m.ImageThumb, conf => conf.MapFrom(m => ModuleUrl.ImageThumbPath(m.Icon).ResolveClientUrl()))
                .ForMember(m => m.ImageFsUrl, conf => conf.MapFrom(m => ModuleUrl.ImagePath(m.Icon).ResolveClientUrl()))
                .ForMember(m => m.PropertyName, conf => conf.MapFrom(m => m.Property.GetName(null)));

            Mapper.CreateMap<PropValLang, PropValLangModel>()
                .ForMember(m => m.PropertyId, conf => conf.MapFrom(m => m.PropVal.Property.Id))
                .ForMember(m => m.PropValId, conf => conf.MapFrom(m => m.PropVal.Id));

            Mapper.CreateMap<VariantPropVal, VariantPropValModel>()
                .ForMember(m => m.PropValId, conf => conf.MapFrom(m => m.PropVal.Id))
                .ForMember(m => m.PropValName, conf => conf.MapFrom(m => m.PropVal.GetName(null)))
                .ForMember(m => m.VariantId, conf => conf.MapFrom(m => m.Variant.Id))
                .ForMember(m => m.Property, conf => conf.MapFrom(m => m.PropVal.Property));

            Mapper.CreateMap<Manufacturer, ManufacturerModel>()
                .ForMember(m => m.Raw, conf => conf.MapFrom(m => m))
                .ForMember(m => m.Url, conf => conf.MapFrom(m => string.IsNullOrEmpty(m.Url) ? "" : m.Url))
                .ForMember(m => m.ImageThumb, conf => conf.MapFrom(m => ModuleUrl.ImageThumbPath(m.Logo).ResolveClientUrl()))
                .ForMember(m => m.ImageFsUrl, conf => conf.MapFrom(m => ModuleUrl.ImagePath(m.Logo).ResolveClientUrl()));

            Mapper.CreateMap<ManufacturerLang, ManufacturerLangModel>()
                .ForMember(m => m.ManufacturerId, conf => conf.MapFrom(m => m.Manufacturer.Id));

            Mapper.CreateMap<Vat, VatModel>()
                .ForMember(m => m.Raw, conf => conf.MapFrom(m => m))
                .ForMember(m => m.Default, conf => conf.MapFrom(m => m.IsDefault()));

            Mapper.CreateMap<Location, LocationModel>();
            Mapper.CreateMap<Supplier, SupplierModel>();
            Mapper.CreateMap<EmailMarketUser, EmailMarketUserModel>();
            Mapper.CreateMap<ProductExtraInformation, ProductExtraInformationModel>();
            Mapper.CreateMap<ProdQtyLog, ProdQtyLogModel>()
                .ForMember(m => m.VariantName, conf => conf.MapFrom(m => m.Variant.GetPropDesc()));

            Mapper.CreateMap<TechSpec, TechSpecModel>()
                .ForMember(m => m.Raw, conf => conf.MapFrom(m => m))
                .ForMember(m => m.Icon, conf => conf.MapFrom(m => m.Icon))
                .ForMember(m => m.IconThumb, conf => conf.MapFrom(m => ModuleUrl.ImageThumbPath(m.Icon).ResolveClientUrl()))
                .ForMember(m => m.IconFsUrl, conf => conf.MapFrom(m => ModuleUrl.ImagePath(m.Icon).ResolveClientUrl()));

            Mapper.CreateMap<TechSpecVal, TechSpecValModel>()
                .ForMember(m => m.Raw, conf => conf.MapFrom(m => m))
                .ForMember(m => m.TechSpecName, conf => conf.MapFrom(m => m.TechSpec.Name))
                .ForMember(m => m.TechSpecDOrder, conf => conf.MapFrom(m => m.TechSpec.DisplayOrder))
                .ForMember(m => m.TechSpecActive, conf => conf.MapFrom(m => m.TechSpec.Active));

            Mapper.CreateMap<ProductWaiting, ProductWaitingModel>()
                .ForMember(m => m.ProductName, conf => conf.MapFrom(m => m.Product.GetName(m.Variant)));

            Mapper.CreateMap<ShoppingProduct, ShoppingProductModel>()
                .ForMember(m => m.Raw, conf => conf.MapFrom(m => m))
                .ForMember(m => m.ProductId, conf => conf.MapFrom(m => m.Product.Id))
                .ForMember(m => m.ProductQty, conf => conf.MapFrom(m => m.Variant == null ? m.Product.Quantity : m.Variant.Quantity))
                .ForMember(m => m.VariantId, conf => conf.MapFrom(m => m.Variant.GetId(null)))
                .ForMember(m => m.PropDesc, conf => conf.MapFrom(m => m.Variant.GetPropDesc()))
                .ForMember(m => m.ProductName, conf => conf.MapFrom(m => m.Product.GetName(m.Variant)))
                .ForMember(m => m.ProductImage, conf => conf.MapFrom(m => m.Product.GetImage(m.Variant)))
                .ForMember(m => m.ProductCostPrice, conf => conf.MapFrom(m => m.Variant != null ? m.Variant.CostPrice : m.Product.CostPrice))
                .ForMember(m => m.ProductCostPriceInc, conf => conf.MapFrom(m => m.Product.IncVat(m.Variant != null ? m.Variant.CostPrice : m.Product.CostPrice)))
                .ForMember(m => m.ProductPrice, conf => conf.MapFrom(m => m.Variant != null ? m.Variant.Price : m.Product.Price))
                .ForMember(m => m.ProductPriceInc, conf => conf.MapFrom(m => m.Product.IncVat(m.Variant != null ? m.Variant.Price : m.Product.Price)))
                .ForMember(m => m.ProductSalePrice, conf => conf.MapFrom(m => (m.Variant != null ? m.Variant.SalePrice : m.Product.SalePrice).NullIfZero(true)))
                .ForMember(m => m.ProductSalePriceInc, conf => conf.MapFrom(m => m.Product.IncVat(m.Variant != null ? m.Variant.SalePrice : m.Product.SalePrice).NullIfZero(true)))
                .ForMember(m => m.ProductVat, conf => conf.MapFrom(m => m.Product.Vat.ToOFloat()))
                .ForMember(m => m.ProductSKU, conf => conf.MapFrom(m => m.Variant != null ? m.Variant.SKU : m.Product.SKU))
                .ForMember(m => m.ProductBarcode, conf => conf.MapFrom(m => m.Variant != null ? m.Variant.BarCode : m.Product.BarCode))
                .ForMember(m => m.SupplierName, conf => conf.MapFrom(m => m.Product.Supplier.GetName()))
                .ForMember(m => m.WUnit, conf => conf.MapFrom(m => m.Product.WUnit))
                .ForMember(m => m.Weight, conf => conf.MapFrom(m => m.Variant != null ? m.Variant.Weight : m.Product.Weight));

            Mapper.CreateMap<ProductBundleGroup, ProductBundleGroupModel>();

        }
    }

}