using System;

namespace Claw.Modules.Eshop.Models
{
    [Flags]
    public enum VisibilityType : byte
    {
        Web = 1, Pos = 2
    }

    public enum ProductType : byte
    {
        Normal = 0, Bundle = 1, Deleted = 128
    }

    [Flags]
    public enum ProductFlags
    {
        NotifySent = 1,
        NoDelete = 2,
        HideSku = 4,
        CGroupDiscount = 8,
        SerialNumber = 16,
        BundleHideSubItems = 32,
        EmailMarketing = 64,
        BundleNeedSelectAll = 128,
        //xxx = 256,
        Custom1 = 512,
        Custom2 = 1024,
        Price4LoginOnly = 2048,
        Featured = 4096,
        ModelMask = EmailMarketing,
        Prisguide = 8192,
        Prisjakt = 16384,
        RecurringOrder = 32768,
        HidePrice = 65536,
        Accessory = 131072,
        HideZeroQtyProd = 262144,
        FBMarketing = 524288,
        GoogleShopping = 1048576,
        BundleShowProdInStock = 2097152,
        BundleFetchDescFromProds = 4194304,
        Controlled = 8388608,
        CGroupDiscountNoVariant = 16777216,
        CGroupDiscountScope = 33554432,
        Aassortment = 67108864,
    }
}