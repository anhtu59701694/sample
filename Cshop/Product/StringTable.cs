using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Claw.Modules.Eshop;
using Claw.Modules.Eshop.Helper;
using Claw.Modules.Eshop.Models;

namespace Claw.Modules.Eshop { internal class StringTable : Product.StringTable { } }

namespace Claw.Modules.Eshop.Product
{
    /// <summary>
    /// module values
    /// </summary>
    public partial class StringTable : Eshop.Core.StringTable
    {
        public static string QtyPos
        {
            get { return "Storage position".ModLocalize(); }
        }

        public static string GetQtyPosName(Domain.QtyPos qtyPos, SettingsBiz settings, bool admin = true)
        {
            switch (qtyPos)
            {
                case Domain.QtyPos.Qty1:
                    return "QtyPos1 name".ModLocalize("Main", admin);
                case Domain.QtyPos.Qty2:
                    return (settings ?? SettingsBiz.GetSettings()).Hidden.Storage2Name ?? "";
                default:
                    throw new InvalidOperationException($"StringTable.GetQtyPosName: qtyPos = {qtyPos}");
            }
        }

        public static string CustGroupDiscType(bool amount)
        {
            return amount ? "cust group disc type Amount".ModLocalize("Amount") : "cust group disc type Percent".ModLocalize("Percent");
        }

        public static string Banners
        {
            get { return "Banners".ModLocalize(); }
        }

        public static string UpdateLocation
        {
            get { return "Update location".GlobalLocalize(); }
        }

        public static string SetIsAccessory
        {
            get { return "Set is accessory".ModLocalize(); }
        }

        public static string SetShowVariant
        {
            get { return "Set show variant".ModLocalize(); }
        }

        public static string SetVisibleWeb
        {
            get { return "Set visible web".ModLocalize(); }
        }

        public static string DuplicateColumnName
        {
            get { return "Duplicate column name".ModLocalize(); }
        }

        public static string ProductDeleteFailedMsg
        {
            get { return "product_delete_failed_msg".ModLocalize("The product [{0}] is associated with orders [{1}]. Please process these orders to SENT/CANCELLED status before continue with this action."); }
        }

        public static string VariantDeleteFailedMsg
        {
            get { return "variant_delete_failed_msg".ModLocalize("Variant [{0}] tồn tại trong đơn hàng [{1}]. vui lòng xử lý trạng thái đơn hàng là Gửi/Hủy, rồi sau đó thực hiện xóa variant này."); }
        }

        public static string Category
        {
            get { return "Category".ModLocalize(); }
        }

        public static string CategoryTopLevel
        {
            get { return "category_top_level".ModLocalize("[Top level]"); }
        }

        public static string CategoryRoot
        {
            get { return "category_root".ModLocalize("[Orphan]"); }
        }

        public static string SelectCategory
        {
            get { return "Select category".ModLocalize(); }
        }

        public static string SelectVat
        {
            get { return "Select tax".ModLocalize("Select value"); }
        }

        public static string SelectCurrency
        {
            get { return "Select currency".ModLocalize(); }
        }

        public static string SelectStatus
        {
            get { return "Select status".ModLocalize(); }
        }

        public static string SelectProdEditQtyReason
        {
            get { return "Select reason".ModLocalize(); }
        }

        public static string SelectUnit
        {
            get { return "Select unit".ModLocalize(); }
        }

        public static string SelectProperty
        {
            get { return "Select variant".ModLocalize(); }
        }

        public static string SelectGroup
        {
            get { return "Select group".ModLocalize(); }
        }

        public static string NameDescHint
        {
            get { return "name_desc_hint".ModLocalize("Write a name and description for this category"); }
        }

        public static string NameDescHintProduct
        {
            get { return "name_desc_hint_product".ModLocalize("Write a name and description for this product"); }
        }

        public static string PercentValueHint
        {
            get { return "percent_value_hint".ModLocalize("Enter value in range from 0.0 to 1.0 "); }
        }

        public static string ViewInYourStore
        {
            get { return "View in your store".ModLocalize(); }
        }

        public static string CategoryIcon
        {
            get { return "Category icon".ModLocalize(); }
        }

        public static string PriceIncVat
        {
            get { return "Price incl vat".ModLocalize("Price incl. vat"); }
        }

        public static string PriceExcVat
        {
            get { return "Price eks vat".ModLocalize("Price excl. vat"); }
        }

        public static string Products
        {
            get { return "Products".ModLocalize(); }
        }
        public static string HeaderGoogleShopping
        {
            get { return "Header googleshopping".ModLocalize("Generate xml file"); }
        }
        public static string HeaderFBMarketing
        {
            get { return "HeaderFBMarketing".ModLocalize("Generate xml file"); }
        }
        public static string AddAccessories
        {
            get { return "Add accessories".ModLocalize(); }
        }

        public static string AddProducts
        {
            get { return "Add products".ModLocalize("Add new product"); }
        }

        public static string AddProductToCategory
        {
            get { return "Add product to category".ModLocalize("Add product to category"); }
        }

        public static string UpdateProductPrice
        {
            get { return "Update product price ".ModLocalize("Update product price"); }
        }

        public static string CateAssignProductHint
        {
            get { return "Manage the products that belong to this category".ModLocalize("Manage the product that belong to this category"); }
        }

        public static string ManageCategories
        {
            get { return "Manage categories".ModLocalize("Manage category"); }
        }

        public static string DBOrderText
        {
            get { return "DBOrderText".ModLocalize("Order"); }
        }

        public static string DBRefresh
        {
            get { return "DBRefresh".ModLocalize("Refresh"); }
        }

        public static string DBInvalidFromDate
        {
            get { return "DBInvalidFromDate".ModLocalize("Invalid from date"); }
        }

        public static string DBInvalidToDate
        {
            get { return "DBInvalidToDate".ModLocalize("Invalid to date"); }
        }

        public static string DBInvalidFromDateToDate
        {
            get { return "DBInvalidFromDateToDate".ModLocalize("To date must be greater than from date"); }
        }

        public static string DBFromDate
        {
            get { return "DBFromDate".ModLocalize("From date"); }
        }

        public static string DBToDate
        {
            get { return "DBToDate".ModLocalize("To date"); }
        }

        public static string DBOrders
        {
            get { return "DBOrders".ModLocalize("Orders"); }
        }

        public static string DBRecentOrder
        {
            get { return "DBRecentOrder".ModLocalize("Recent order"); }
        }

        public static string DBStatistics
        {
            get { return "DBStatistics".ModLocalize("Statistics"); }
        }

        public static string DBReturnedOrders
        {
            get { return "DBReturnedOrders".ModLocalize("Returned Orders"); }
        }

        public static string DBCancelledOrders
        {
            get { return "DBCancelledOrders".ModLocalize("Cancelled Orders"); }
        }

        public static string DBActiveProducts
        {
            get { return "DBActiveProducts".ModLocalize("Active products"); }
        }

        public static string DBOutOfStockProducts
        {
            get { return "DBOutOfStockProducts".ModLocalize("Out-of-stock products"); }
        }

        public static string DBCategories
        {
            get { return "DBCategories".ModLocalize("Categories"); }
        }

        public static string DBRegisteredMembers
        {
            get { return "DBRegisteredMembers".ModLocalize("Registered members"); }
        }

        public static string AddEditCategory(int id)
        {
            if (id == 0) return "Add category".ModLocalize();
            else return "Edit category".ModLocalize();
        }

        public static string AllCategories
        {
            get { return "All categories".ModLocalize(); }
        }

        public static string ManageInventories
        {
            get { return "Manage inventories".ModLocalize(); }
        }

        public static string ManageFeaturedProducts
        {
            get { return "Manage featured products".ModLocalize(); }
        }

        public static string ManagePrisguideProducts
        {
            get { return "Manage prisguide products".ModLocalize(); }
        }

        public static string ManageRecurringProducts
        {
            get { return "Manage recurring products".ModLocalize(); }
        }

        public static string RecurringProduct
        {
            get { return "Recurring product".ModLocalize(); }
        }

        public static string PrisguideProduct
        {
            get { return "Prisguide product".ModLocalize(); }
        }

        public static string ManagePrisjaktProducts
        {
            get { return "Manage prisjakt products".ModLocalize(); }
        }

        public static string PrisjaktProduct
        {
            get { return "Prisjakt product".ModLocalize(); }
        }

        public static string ManageBackOrderProducts
        {
            get { return "Manage back-ordered products".ModLocalize(); }
        }

        public static string ManageEmailMarketProducts
        {
            get { return "Manage email marketing products".ModLocalize(); }
        }

        public static string ManageProducts
        {
            get { return "Manage products".ModLocalize("Manage product"); }
        }

        public static string AddEditProduct(int id, ProductType type)
        {
            if (type == ProductType.Normal)
                return (id == 0 ? "Add product" : "Edit product").ModLocalize();
            else return (id == 0 ? "Add product bundle" : "Edit product bundle").ModLocalize();
        }

        public static string AssignToProducts
        {
            get { return "Assign to products".ModLocalize("Select product"); }
        }

        public static string SelectProduct
        {
            get { return "Select product".ModLocalize(); }
        }

        public static string SelectLocation
        {
            get { return "Select location".ModLocalize("Select shipping zones"); }
        }

        public static string ManageManufacturers
        {
            get { return "Manage manufacturer".ModLocalize(); }
        }

        public static string AddEditManufacturer(int id)
        {
            if (id == 0) return "Add manufacturer".ModLocalize("Add new manufacturer");
            else return "Edit manufacturer".ModLocalize();
        }

        public static string ManageSuppliers
        {
            get { return "Manage suppliers".ModLocalize(); }
        }

        public static string AddEditSupplier(int id)
        {
            if (id == 0) return "Add new supplier".ModLocalize();
            else return "Edit supplier".ModLocalize();
        }

        public static string ManufacturerLogo
        {
            get { return "Manufacturer logo".ModLocalize("Logo"); }
        }

        public static string SelectManufacturer
        {
            get { return "Select manufacturer".ModLocalize(); }
        }

        public static string Inventory
        {
            get { return "Inventory".ModLocalize(); }
        }

        public static string ProductPrices
        {
            get { return "Product prices".ModLocalize("Product price"); }
        }

        public static string ManageStatus
        {
            get { return "Manage status".ModLocalize(); }
        }

        public static string AddEditStatus(int id)
        {
            if (id == 0) return "Add status".ModLocalize("Add stock status");
            else return "Edit status".ModLocalize("Edit stock status");
        }

        public static string ManageProperties
        {
            get { return "Manage variant".ModLocalize(); }
        }

        public static string AddEditProperty(int id)
        {
            if (id == 0) return "Add new variant".ModLocalize();
            else return "Edit variant".ModLocalize();
        }

        public static string ManagePropVals
        {
            get { return "Manage variant values".ModLocalize("Manage variant value"); }
        }

        public static string AddEditPropVal(int id)
        {
            if (id == 0) return "Add variant value".ModLocalize();
            else return "Edit variant value".ModLocalize();
        }

        public static string EmailMarketProducts
        {
            get { return "Email marketings".ModLocalize(); }
        }

        public static string ManageEmailMarketUsers
        {
            get { return "Manage email marketing users".ModLocalize(); }
        }

        public static string AddEditEmailMarketUser(int id)
        {
            if (id == 0) return "Add email marketing user".ModLocalize();
            else return "Edit email marketing user".ModLocalize();
        }

        public static string ProductWaitings
        {
            get { return "Product waitings".ModLocalize(); }
        }

        public static string ManageProductWaitings
        {
            get { return "Manage product waitings".ModLocalize(); }
        }

        public static string AddEditProductWaiting(int id)
        {
            if (id == 0) return "Add product waiting".ModLocalize();
            else return "Edit product waiting".ModLocalize();
        }

        public static string StatusIcon
        {
            get { return "Status icon maximum".ModLocalize("Icon (50 x 50 px)"); }
        }

        public static string SelectProductCondition
        {
            get { return "Select fetch products".ModLocalize(); }
        }

        public static string Manufacturers
        {
            get { return "Manufacturers".ModLocalize(); }
        }

        public static string Manufacturer
        {
            get { return "Manufacturer".ModLocalize(); }
        }

        public static string Categories
        {
            get { return "Categories".ModLocalize(); }
        }

        public static string Bundle
        {
            get { return "Bundle".ModLocalize(); }
        }

        public static string ProductStatus
        {
            get { return "Product status".ModLocalize("Stock status"); }
        }

        public static string Variants
        {
            get { return "Variants".ModLocalize("Variant"); }
        }

        public static string Variant
        {
            get { return "Variant".ModLocalize(); }
        }

        public static string ThisProductHasVideos
        {
            get { return "This product has videos".ModLocalize("This product has video"); }
        }

        public static string VideoSupportLink
        {
            get { return "Video support link".ModLocalize("Accept: vimeo.com, youtube.com. Ex: https://vimeo.com/144892147, https://www.youtube.com/watch?v=jV2msksfkfQ."); }
        }

        public static string ThisProductRequiresShipping
        {
            get { return "This product requires shipping".ModLocalize(); }
        }

        public static string ThisProductIsFreeShipping
        {
            get { return "This product is free shipping".ModLocalize(); }
        }

        public static string ThisIsDownloadablePoduct
        {
            get { return "This is downloadable product".ModLocalize(); }
        }

        public static string Reservation
        {
            get { return "Reservation".ModLocalize(); }
        }
        public static string GenerateFile
        {
            get { return "Path file is not exists".ModLocalize("Path file is not exists"); }
        }
        public static string GenerateNote
        {
            get { return "Generate note".ModLocalize("Note: Only generate product with status is enable and in stock"); }
        }
        public static string ListFilesGenerate
        {
            get { return "List file".ModLocalize("List file are generated (file under 100 MB in size)"); }
        }

        //public static string Hold
        //{
        //    get { return "Hold".ModLocalize(); }
        //}

        //public static string Ship
        //{
        //    get { return "Ship".ModLocalize(); }
        //}

        //public static string Reorder
        //{
        //    get { return "Reorder".ModLocalize("Register restorder"); }
        //}

        public static string VisibleFrontend
        {
            get { return "Visible frontend".ModLocalize("Visible on frontend"); }
        }

        public static string MinimalWeight
        {
            get { return "shippingcost_minimal_weight".ModLocalize("Minimal weight (kg)"); }
        }

        public static string UploadProductImages
        {
            get { return "Upload product images".ModLocalize(); }
        }

        public static string ProductFailToImportAtRow
        {
            get { return "product(s) fail to import at row:".ModLocalize(); }
        }

        public static string ImportTotal
        {
            get { return "Import Total".ModLocalize("Total"); }
        }

        public static string ProductImportedToSystem
        {
            get { return "products have been imported to system.".ModLocalize(); }
        }

        public static string ProductName
        {
            get { return "Name".ModLocalize(); }
        }

        public static string ProductDescription
        {
            get { return "Description".ModLocalize(); }
        }

        public static string ProductTag
        {
            get { return "Tag".ModLocalize(); }
        }

        public static string ProductCategory
        {
            get { return "Category".ModLocalize(); }
        }

        public static string VisibleWeb
        {
            get { return "Visible on web: yes/no".ModLocalize(); }
        }

        public static string VisiblePOS
        {
            get { return "Visible on POS: yes/no".ModLocalize(); }
        }

        public static string ErrPriceMustGreaterSalePrice
        {
            get { return "err_price_must_greater_saleprice".ModLocalize("Listing price must be greater than sale price"); }
        }

        public static string ErrCostPriceMustLessOrEqSalePrice
        {
            get { return "err_costprice_must_lessorequal_saleprice".ModLocalize("Buying price must be less than or equal to sale price"); }
        }

        public static string ErrCostPriceMustLessOrEqPrice
        {
            get { return "err_costprice_must_lessorequal_price".ModLocalize("Buying price must be less than or equal to listing price"); }
        }

        public static string PriceExcl
        {
            get { return "Price (excl VAT)".ModLocalize(); }
        }

        public static string SalePriceExcl
        {
            get { return "Sale price (excl VAT)".ModLocalize(); }
        }

        public static string CostPriceExcl
        {
            get { return "Cost price (excl VAT)".ModLocalize(); }
        }

        public static string ProductManufacturer
        {
            get { return "Manufacturer".ModLocalize(); }
        }

        public static string ProductImage
        {
            get { return "Product image".ModLocalize(); }
        }

        public static string BarCodeImage
        {
            get { return "barcode-image".ModLocalize("Image"); }
        }

        public static string QrCodeImage
        {
            get { return "qrcode-image".ModLocalize("QR Code"); }
        }

        public static string ProductTax
        {
            get { return "Tax".ModLocalize(); }
        }

        public static string ProductQuantity
        {
            get { return "Quantity".ModLocalize(); }
        }

        public static string ProductSKU
        {
            get { return "Product sku".ModLocalize(); }
        }

        public static string ProductBarcode
        {
            get { return "Barcode".ModLocalize(); }
        }

        public static string ProductWeight
        {
            get { return "Weight".ModLocalize(); }
        }

        public static string PriceIncl
        {
            get { return "Price (incl VAT)".ModLocalize(); }
        }

        public static string SalePriceIncl
        {
            get { return "Sale price (incl VAT)".ModLocalize(); }
        }

        public static string CostPriceIncl
        {
            get { return "Cost price (incl VAT)".ModLocalize(); }
        }

        public static string ProductNameImex
        {
            get { return "Product name".ModLocalize(); }
        }

        public static string SelectProductForExport
        {
            get { return "Select products for export".ModLocalize(); }
        }
        public static string SelectProductForGenerate
        {
            get { return "Select products for generate".ModLocalize(); }
        }

        public static string SelectCategoryForExport
        {
            get { return "Select categories for export".ModLocalize(); }
        }

        public static string SearchCategory
        {
            get { return "Search categories by name".ModLocalize(); }
        }

        public static string AllProduct
        {
            get { return "All products".ModLocalize(); }
        }

        public static string Product
        {
            get { return "Product".ModLocalize(); }
        }

        public static string SelectProducts
        {
            get { return "Select products".ModLocalize(); }
        }

        public static string FromCategory
        {
            get { return "From category".ModLocalize(); }
        }

        public static string SelectCategories
        {
            get { return "Select categories".ModLocalize(); }
        }

        public static string ChangePrice
        {
            get { return "Change price".ModLocalize("Change price"); }
        }

        public static string ChangeType
        {
            get { return "Change type".ModLocalize("Change type"); }
        }

        public static string ChangePriceIncreased
        {
            get { return "ChangePriceIncreased".ModLocalize("Increased (+)"); }
        }

        public static string UpdateQuantity
        {
            get { return "Update quantity".ModLocalize(); }
        }

        public static string UpdateQuantityInvalidData
        {
            get { return "Invalid data".ModLocalize(); }
        }

        public static string UpdateEmailMarketing
        {
            get { return "Update email marketing".ModLocalize(); }
        }

        public static string ChangePriceDescreased
        {
            get { return "ChangePriceDescreased".ModLocalize("Descreased (-)"); }
        }

        public static string ChangeTypeFixAmount
        {
            get { return "ChangeTypeFixAmount".ModLocalize("Fix amount $"); }
        }

        public static string ChangeTypeFixPercentage
        {
            get { return "ChangeTypeFixPercentage".ModLocalize("Percentage %"); }
        }

        public static string ChangePriceReivew
        {
            get { return "Change price reivew".ModLocalize("Reivew"); }
        }

        public static string Weight
        {
            get { return "Weight".ModLocalize("Weight"); }
        }

        public static string RegisteredOn
        {
            get { return "Registered on".ModLocalize("Registered on"); }
        }

        public static string ErrMsgNotEnoughQuantity
        {
            get { return "errmsg_not_enough_quantity".ModLocalize("The quantity is not enough"); }
        }

        public static string SearchByCategoryName
        {
            get { return "search_category_placeholder".ModLocalize("Search by category name"); }
        }

        public static string QuickEditProduct
        {
            get { return "QuickEditProduct".ModLocalize("Quick edit product", false); }
        }

        public static string EditVariant
        {
            get { return "Edit variant".ModLocalize(); }
        }

        public static string EditProductQuantity
        {
            get { return "Edit quantity".ModLocalize(); }
        }
        public static string AddProductQuantity
        {
            get { return "Add quantity".ModLocalize(); }
        }
        public static string UnSubscribeBig
        {
            get { return "Unsubscribe selected".ModLocalize(); }
        }
        public static string DefaultNoteLogProduct
        {
            get { return "Create new product".ModLocalize(); }
        }
        public static string DefaultNoteLogProductBundle
        {
            get { return "Create bundle product".ModLocalize(); }
        }
        public static string DefaultNoteLogVariant
        {
            get { return "Create new variant".ModLocalize(); }
        }
        public static string UnSubscribe
        {
            get { return "Unsubscribe".ModLocalize(); }
        }

        public static string ConfirmUnsubscribeEmailMUser
        {
            get { return "Confirm Unsubscribe email market user msg".ModLocalize("Are you sure you want to unsubscribe this email? You cannot undo this action!"); }
        }

        public static string ExportMarketEmail2Excel
        {
            get { return "Export emails to excel".ModLocalize(); }
        }

        public static string EmailsSubscribed2Product
        {
            get { return "Emails subscribed to product".ModLocalize(); }
        }

        public static string EnableEmailMarketing(bool enable)
        {
            return ((enable ? "Enable" : "Disable") + " email marketing").ModLocalize();
        }

        public static string ImportProductOkMsg
        {
            get { return "Import_Product_Ok_Msg".ModLocalize("Import {0} products, {1} variants successfully"); }
        }

        public static string ImportProductPartialOkMsg
        {
            get { return "Import_product_partial_ok_msg".ModLocalize("Found {2} rows. Import {0} products, {1} variants successfully with error messages: {3}"); }
        }
        public static string ProductExtraInfor
        {
            get { return "ProductExtraInfor".ModLocalize("Product extra information"); }
        }
        public static string ExtraInfoAddingHint
        {
            get { return "ExtraInfoAddingHint".ModLocalize("Extra information adding hint"); }
        }
        public static string RemoveExtra
        {
            get { return "Remove from extra information".ModLocalize(); }
        }
        public static string AddExtraInformation
        {
            get { return "AddExtraInformation".ModLocalize("Add new extra information"); }
        }
        public static string EditExtraInformation
        {
            get { return "EditExtraInformation".ModLocalize("Edit extra information"); }
        }

        public static string DuplicateExtraInformation
        {
            get { return "DuplicateExtraInformation".ModLocalize("The value already exists! Please select new value!"); }
        }

        public static string NameExtraInformation
        {
            get { return "NameExtraInformation".ModLocalize("Name"); }
        }

        public static string ValueExtraInformation
        {
            get { return "ValueExtraInformation".ModLocalize("Value"); }
        }

        public static string UpdateDisplayOrder
        {
            get { return "Update display order".ModLocalize(); }
        }

        public static string BelongToBundleProduct
        {
            get { return "BelongToBundleProduct".ModLocalize("Can not delete this product. Because it is used in bundle product!"); }
        }

        public static string History
        {
            get { return "History"; }
        }

        public static string ProductQtyHistory
        {
            get { return "Product quantity history"; }
        }

        public static string UpdateFeature
        {
            get { return "Update feature".ModLocalize(); }
        }

        public static string TechSpec
        {
            get { return "Tech spec".ModLocalize(); }
        }

        public static string ManageTechSpecs
        {
            get { return "Manage tech specs".ModLocalize(); }
        }

        public static string AddEditTechSpec(int id)
        {
            return (id == 0 ? "Add tech spec" : "Edit tech spec").ModLocalize();
        }

        public static string TechSpecVal
        {
            get { return "Tech spec value".ModLocalize(); }
        }

        public static string ManageTechSpecVals
        {
            get { return "Manage tech spec values".ModLocalize(); }
        }

        public static string AddEditTechSpecVal(int id)
        {
            return (id == 0 ? "Add tech spec value" : "Edit tech spec value").ModLocalize();
        }

        public static string SelectTechSpec
        {
            get { return "Select tech spec".ModLocalize(); }
        }

        public static string DefaultProdDiscount
        {
            get { return "Default".ModLocalize(); }
        }

        public static string GoogleShopping
        {
            get { return "AGoogle shopping".ModLocalize("Google shopping"); }
        }

        public static string FBMarketing
        {
            get { return "FBMarketing".ModLocalize("Facebook marketing"); }
        }

        public static string Supplier
        {
            get { return "Supplier".ModLocalize("Supplier"); }
        }

        public static string ManageFBMarketing
        {
            get { return "ManageFBMarketing".ModLocalize("Manage Facebook marketing"); }
        }

        public static string ManageGoogleShopping
        {
            get { return "Manage google shopping".ModLocalize("Manage google shopping"); }
        }

        public static string ProdBundleNotEnoughQty
        {
            get { return "Prod Bundle Not Enough Qty".ModLocalize("Item [Name: '{0}', Quantity: {1}] is not enough for required quantity = {2}"); }
        }

        public static string PriceCheck
        {
            get { return "Price check".ModLocalize(); }
        }

        public static string ManagePriceCheck
        {
            get { return "Manage price check".ModLocalize(); }
        }

        public static string PriceCkNotFound
        {
            get { return "Price Ck Not Found".ModLocalize("Not found"); }
        }

        public static string PriceCkTitle
        {
            get { return "Price Ck Title".ModLocalize("Check price"); }
        }

        public static string PriceCkApply
        {
            get { return "Price Ck Apply".ModLocalize("Apply"); }
        }

        public static string PriceCkMinPrice
        {
            get { return "Price Ck MinPrice".ModLocalize("Min price"); }
        }

        public static string ProdSelfDiscount
        {
            get { return "Prod Self Discount".ModLocalize("Discount on quantity"); }
        }

        public static string SelfDiscType(bool amount)
        {
            return amount ? "Self Disc Type Amount".ModLocalize("Amount") : "Self Disc Type Percent".ModLocalize("Percent");
        }

        public static string ProfitAmountExc
        {
            get { return "Profit Amount Exc".ModLocalize("Profit (Excl. VAT)"); }
        }

        public static string ProfitPercentExc
        {
            get { return "Profit percent Exc".ModLocalize("Profit in % (Excl. VAT)"); }
        }

        public static string ProfitAmountInc
        {
            get { return "Profit Amount Inc".ModLocalize("Max rebate (Incl. VAT)"); }
        }

        public static string ProfitPercentInc
        {
            get { return "Profit percent Inc".ModLocalize("Max rebate in % (Incl. VAT)"); }
        }

        public static string GoogleShoppingPriceFormat
        {
            get { return "GoogleShopping_PriceFormat".ModLocalize("{0:N2} NOK", false); }
        }

        public static string CopyLink
        {
            get { return "CopyLink".ModLocalize("Copy link"); }
        }

        public static string DeleteLink
        {
            get { return "DeleteLink".ModLocalize("Delete link"); }
        }

        public static string EmailMUserSend
        {
            get { return "EmailMUser Send".ModLocalize("Send email"); }
        }

        public static string EmailMUserListEmpty
        {
            get { return "EmailMUser list empty".ModLocalize("The list is empty"); }
        }

        public static string EmailMUserNotifyMe
        {
            get { return "EmailMUser NotifyMe".ModLocalize("Notify me"); }
        }

        public static string Digital
        {
            get { return "Digital".ModLocalize(); }
        }

        public static string ProductDownloadNotAllow
        {
            get { return "Product Download Not Allowed".ModLocalize("Download product is not allowed"); }
        }

        public static string ProductDownloadLinkExpired
        {
            get { return "Product Download link expired".ModLocalize("Download link is expired"); }
        }

        public static string SerialNumbers
        {
            get { return "Serial numbers".ModLocalize(); }
        }

        public static string RemoveSerialNumber
        {
            get { return "Remove serial number".ModLocalize(); }
        }

        public static string AddSerialNumber
        {
            get { return "Add serial number".ModLocalize(); }
        }

        public static string SelectSerial
        {
            get { return "Select serial nr".ModLocalize(); }
        }

        public static string ImportSerialNumberSucceeded
        {
            get { return "Import Serial Number Succeeded".ModLocalize("Found {0} serial number(s)"); }
        }

        public static string ProductBundleGroups
        {
            get { return " Product Bundle Groups".ModLocalize("Groups"); }
        }

        public static string AddBundleGroup
        {
            get { return "add new Product Bundle Groups".ModLocalize("Add new"); }
        }

        public static string RemoveFromBundleGroup
        {
            get { return "remove from Product Bundle Groups".ModLocalize("Remove"); }
        }

        public static string UpdateSelectItemsToBundleGroup
        {
            get { return "Update selected items to bundle group".ModLocalize("Update group"); }
        }

        public static string BundleDiscountTypeRate
        {
            get { return "Bundle Discount Type Rate".ModLocalize("Rate %"); }
        }

        public static string BundleDiscountTypeProfit
        {
            get { return "Bundle Discount Type profit".ModLocalize("Profit %"); }
        }
    }
}