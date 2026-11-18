using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Mvc;
using Claw.Core.Extensions;

namespace Claw.Modules.Eshop.Core
{
    /// <summary>
    /// shared values
    /// </summary>
    public partial class StringTable
    {
        public const string MaxInt = "1000000000"; //int.MaxValue - 1 
        public const string MaxOMoney = "100000000000000";
        public const string MaxOFloat = "1000000000";

        public const string RegexAlias = @"([a-z0-9]|[_-])*";
        public static string RegexEmail = string.Format(@"[^<>@]*(\<?{0}\>?|{0})", @"[a-zA-Z0-9.!#$%&'*+\/=?^_`{|}~-]+@[a-zA-Z0-9](?:[a-zA-Z0-9-]{0,61}[a-zA-Z0-9])?(?:\.[a-zA-Z0-9](?:[a-zA-Z0-9-]{0,61}[a-zA-Z0-9])?)*");
        public const string ErrMsgInvalidAlias = "eshop_adm_errmsg_invalid_alias,The field contains only lower case english letter, digit, '-' or '_'";
        public const string ErrMsgRequiredField = "eshop_adm_errmsg_required_field,The field is required";
        public const string ErrMsgLengthLimit = "eshop_adm_errmsg_invalid_length,The length must be less than or equal to {1}";
        public const string ErrMsgUniqueConstraint = "eshop_adm_errmsg_unique_constraint,The value already exists! Please select new value!";
        public const string ErrMsgUniqueAliasConstraint = "eshop_adm_errmsg_unique_alias_constraint,An URL with this name already exists in shop! Please select another name!";

        public const string ErrMsgLengthRangeLimit = "eshop_adm_errmsg_invalid_length_range,The length must be between {2} and {1}";
        public const string ErrMsgRangeLimit = "eshop_adm_errmsg_invalid_range,The value must be between {1} and {2}";
        public const string ErrMsgMinLimit = "eshop_adm_errmsg_invalid_min_value,The value must be greater or equal to {1}";
        public const string ErrMsgRequireNumber = "eshop_adm_errmsg_require_number,The value must be a number";
        public const string ErrMsgValidPhone = "eshop_adm_errmsg_valid_phone,Phone number is invalid!";
        public const string ErrMsgValidFax = "eshop_adm_errmsg_valid_fax,Fax number is invalid!";
        public const string ErrMsgInvalidCurrencyCode = "eshop_adm_errmsg_invalid_currency_code,The value is not a valid currency code";
        public const string ErrMsgInvalidCurrencyToken = "eshop_adm_errmsg_invalid_currency_token,The value is not a valid currency token";
        public const string ErrMsgInvalidEmail = "eshop_adm_errmsg_invalid_email,The value is not a email address";
        public const string ErrMsgInvalidUrl = "eshop_adm_errmsg_invalid_url,The value is not a url";
        public const string ErrMsgRequireIntegerNumber = "eshop_adm_errmsg_require_enteger_number,The value must be a integer number";

        public const string DorderTempl = "<input type='text' class='numeric-text-box sort-order' value='#:DisplayOrder#' min='0' max= '9999' maxlength='4' />";

        public const string BoolTempl =
            "<div style='text-align:center;'><div style='display:inline-block;'><div style='display:none;height: 13px; background: url(&quot;/areas/admin/styles/images/icon_btn.png&quot;) no-repeat scroll -5px -180px / 20px auto; position: absolute; width: 10px; left: 11px; top: -1px;'></div><a href='javascript:void(0);' /></div></div>";

        public static string NewWinEditTempl(HtmlHelper htmlHelper, string field)
        {
            return string.Format("<a class='grid-row-edit' href='javascript:void(0)' title='{2}' recontroller='{0}' target='_blank'>#:{1}#</a>", htmlHelper.GetController(), field, Edit);
        }

        public static string SimpImgTempl(string fsUrl = "ImageFsUrl", string thumb = "ImageThumb")
        {
            return string.Format("<a href='#:ifnull({0})#' class='preview'><img class='#=({1}?\"\":\"empty\")#' src='#:ifnullImg({1})#'/></a>", fsUrl, thumb);
        }

        public static string AjaxLoadErrorMessage
        {
            get { return "eshop_global_loading_error".Localize("Failure occurred on loading!"); }
        }

        public static string AjaxCommandErrorMessage
        {
            get { return "eshop_global_command_error".Localize("The command failed to execute!"); }
        }

        public static string AjaxDeleteErrorMessage
        {
            get { return "eshop_global_delete_error".Localize("The operation is unsuccessful!"); }
        }

        public static string ErrLimitCountMessage
        {
            get { return "eshop_global_err_limit_count_msg".Localize("The collection has reached the limit size"); }
        }

        public static string ErrValidateFailedMessage
        {
            get { return "eshop_global_err_validate_failed_msg".Localize("Fields marked with red color are required/invalid!"); }
        }

        public static string ErrValidateRequireFieldMsg
        {
            get { return "eshop_global_err_validate_require_field_msg".Localize("Fields marked with red asterisk are required"); }
        }

        public static string ErrMsgFileExtension
        {
            get { return "errmsg_file_extension".ModLocalize("Invalid file format/extension!"); }
        }

        public static string ErrMsgRequireDate
        {
            get { return "errmsg_require_date".ModLocalize("The value must be a date"); }
        }

        public static string ErrMsgUniqueNameManuf
        {
            get { return "errmsg_unique_name_manuf".ModLocalize("The value already exists! Please select new value!"); }
        }

        public static string ErrMsgRequirePositiveNumber
        {
            get { return "errmsg_require_positive_number".ModLocalize("The value must be greater than 0"); }
        }

        public static string ErrMsgNoLangData
        {
            get { return "errmsg_no_lang_data".ModLocalize("The field has no data for '{0}'"); }
        }

        public static string ErrMsgInvalidDateTimeFormat
        {
            get { return "Invalid datetime format".ModLocalize(); }
        }

        public static string ErrMsgInvalidNumberFormat
        {
            get { return "Invalid number format".ModLocalize(); }
        }

        public static string ErrMsgSaveImageUrl(string url = null)
        {
            return string.Format("errmsg_save_image_url".ModLocalize("Can not save the image url '{0}'"),
                url ?? string.Empty);
        }

        public static string TemplActionSuccess
        {
            get { return "template_ActionSuccess".ModLocalize("The operation [{0}] was successfully"); }
        }

        public static string TemplActionUnsuccess
        {
            get { return "template_ActionUnsuccess".ModLocalize("The operation [{0}] failed"); }
        }

        public static string ConfirmAutoFillLangForm
        {
            get { return "eshop_global_Confirm_Auto_Fill_LangForm".Localize("Language(s) [{0}] have no data and will be filled with data copied from current language?"); }
        }

        public static string ConfirmSave
        {
            get { return "eshop_global_ConfirmSave".Localize("Are you sure you want to save this value?"); }
        }

        public static string ConfirmDelete
        {
            get { return "eshop_global_ConfirmDelete".Localize("Are you sure you want to delete this item? This item will be deleted immediately. You cannot undo this action!"); }
        }

        public static string AddNewSuccess
        {
            get { return "eshop_global_add_new_success".Localize("The information was successfully added!"); }
        }
        public static string UpdateSuccess
        {
            get { return "eshop_global_update_success".Localize("The information was successfully updated!"); }
        }
        public static string DeleteSuccess
        {
            get { return "eshop_global_add_delete_success".Localize("The information was successfully deleted!"); }
        }
        public static string Delete
        {
            get { return "Delete".GlobalLocalize(); }
        }
        public static string Deleted
        {
            get { return "Deleted".GlobalLocalize(); }
        }
        public static string Move
        {
            get { return "Move".GlobalLocalize(); }
        }
        public static string OK
        {
            get { return "Ok".GlobalLocalize(); }
        }

        public static string Cancel
        {
            get { return "Cancel".GlobalLocalize(); }
        }
        public static string Return
        {
            get { return "Return".GlobalLocalize(); }
        }

        public static string Detail
        {
            get { return "Detail".ModLocalize(); }
        }

        public static string Edit
        {
            get { return "Edit".ModLocalize(); }
        }

        public static string Done
        {
            get { return "Done".GlobalLocalize(); }
        }

        public static string Yes
        {
            get { return "Yes".GlobalLocalize(); }
        }

        public static string No
        {
            get { return "No".GlobalLocalize(); }
        }

        public static string UpdateStatus
        {
            get { return "Update status".ModLocalize(); }
        }

        public static string CanNotDisableObjectDefault
        {
            get { return "Can not disable object default".ModLocalize(); }
        }

        public static string UpdateVisible
        {
            get { return "Update visible".GlobalLocalize("Update visible"); }
        }

        public static string Search
        {
            get { return "Search".GlobalLocalize(); }
        }

        public static string Settings
        {
            get { return "Settings".ModLocalize(); }
        }
        public static string General
        {
            get { return "General".ModLocalize(); }
        }
        public static string SettingsGeneral
        {
            get { return "Settings general".ModLocalize("General setting"); }
        }

        public static string Display
        {
            get { return "Display".ModLocalize(); }
        }

        public static string UploadCert
        {
            get { return "UploadCert".ModLocalize("Upload cert file"); }
        }

        public static string Accounting
        {
            get { return "Accounting".ModLocalize(); }
        }

        public static string SettingsAccounting
        {
            get { return "Settings_accounting".ModLocalize("Accounting settings"); }
        }

        public static string SettingsDisplay
        {
            get { return "Settings display".ModLocalize("Display setting"); }
        }

        public static string Notification
        {
            get { return "Notification".ModLocalize(); }
        }

        public static string EditSettings
        {
            get { return "Edit settings".GlobalLocalize(); }
        }

        public static string FileUploading
        {
            get { return "Loading".GlobalLocalize(); }
        }

        public static string SelectItem
        {
            get { return "Select item".ModLocalize(); }
        }

        public static string SelectIcon
        {
            get { return "Select icon".ModLocalize(); }
        }

        public static string ExtensionSeparatorHint
        {
            get { return "extension_separator_hint".ModLocalize("Separated by ';'"); }
        }

        public static string ItemLanguageDisabled
        {
            get { return "item_language_disabled".ModLocalize("item is not available for this language"); }
        }

        public static string ItemDeactivated
        {
            get { return "item_deactivated".ModLocalize("item is deactivated"); }
        }

        public static string UpdateDOrder
        {
            get { return "Update ordering".GlobalLocalize(); }
        }

        public static string UpdateTitle
        {
            get { return "Update title".GlobalLocalize(); }
        }

        public static string UploadImage
        {
            get { return "eshop_global_upload_image".Localize("Upload image"); }
        }

        public static string UploadImageErrExt
        {
            get { return "eshop_global_upload_img_err_extension".Localize("Invalid image extension. Supported extensions (*.jpg, *.jpeg, *.gif, *.png, *.bmp)"); }
        }

        public static string ItemEnableTitle
        {
            get { return "Click to enable"; }
        }

        public static string ItemDisableTitle
        {
            get { return "Click to disable"; }
        }

        public static string SearchCountMsg
        {
            get { return "eshop_global_search_count_msg".Localize("Found {0} result(s)"); }
        }

        public static string Confirmation
        {
            get { return "Confirmation".GlobalLocalize(); }
        }

        public static string DeleteSelected
        {
            //get { return "Delete selected".GlobalLocalize(); }
            get { return "eshop_global_delete_selected".Localize("Delete selected items"); }
        }

        public static string FilterLanguage
        {
            get { return "eshop_global_filter_language".Localize("Filter by languages"); }
        }

        public static string PromptSelectLanguage
        {
            get { return "eshop_global_prompt_select_language".Localize("Select language"); }
        }

        public static string AddNewItem
        {
            get { return "Add new item".GlobalLocalize(); }
        }

        public static string AddNew
        {
            get { return "Add new".GlobalLocalize(); }
        }
        public static string Add
        {
            get { return "Add".GlobalLocalize(); }
        }


        public static string Save
        {
            get { return "Save".GlobalLocalize(); }
        }

        public static string Reset
        {
            get { return "Reset".GlobalLocalize(); }
        }

        public static string Update
        {
            get { return "eshop_global_update".Localize("Update"); }
        }
        public static string Updated
        {
            get { return "eshop_global_updated".Localize("Updated"); }
        }

        public static string Admin
        {
            get { return "Admin".GlobalLocalize(); }
        }

        public static string Inherit
        {
            get { return "Inherit".ModLocalize("Inheritance"); }
        }

        public static string TranslateTo
        {
            get { return "eshop_global_translate_to".Localize("Translate text to"); }
        }

        public static string TranslateInfo
        {
            get { return "eshop_global_translate_info".Localize("Translate information"); }
        }

        public static string UseTextTranslator
        {
            get { return "eshop_global_use_text_translator".Localize("Use Microsoft translator"); }
        }

        public static string NumTxtBoxUpArrow
        {
            get { return "eshop_global_num_txt_box_up_arrow".Localize("Increasing"); }
        }

        public static string NumTxtBoxDownArrow
        {
            get { return "eshop_global_num_txt_box_down_arrow".Localize("Decreasing"); }
        }

        public static string ExtensionAccept
        {
            get { return "extension_accept".ModLocalize("Accept:"); }
        }

        public static string ModuleDisabled
        {
            get { return "The module was disabled".ModLocalize(); }
        }

        #region GridPaging

        public static string GridPagingItemsPPage(string itemPhrase = null)
        {
            return itemPhrase == null
                ? "grid_paging_items_per_page".ModLocalize("items per page")
                : string.Format("grid_paging_items_per_page_ex".ModLocalize("{0} per page"), itemPhrase.ToLower());
        }

        public static string GridPagingDisplay(string itemPhrase = null, int? pageSize = null)
        {
            if ((pageSize ?? 0) < short.MaxValue)
                return itemPhrase == null
                    ? "grid_paging_display".ModLocalize("{0} - {1} of {2} items")
                    : string.Format("grid_paging_display_ex".ModLocalize("{{0}} - {{1}} of {{2}} {0}"),
                        itemPhrase.ToLower());
            else
                return itemPhrase == null
                    ? "grid_paging_short_display".ModLocalize("{2} items")
                    : string.Format("grid_paging_short_display_ex".ModLocalize("{{2}} {0}"), itemPhrase.ToLower());
        }

        public static string GridPagingFirst
        {
            get { return "eshop_global_grid_paging_first".Localize("first"); }
        }

        public static string GridPagingPrevious
        {
            get { return "eshop_global_grid_paging_previous".Localize("previous"); }
        }

        public static string GridPagingLast
        {
            get { return "eshop_global_grid_paging_last".Localize("last"); }
        }

        public static string GridPagingNext
        {
            get { return "eshop_global_grid_paging_next".Localize("next"); }
        }

        public static string GridPagingRefresh
        {
            get { return "eshop_global_grid_paging_refresh".Localize("refresh"); }
        }

        #endregion


        public static string ErrRegex
        {
            get { return "err_regex".ModLocalize("Invalid regular expression syntax"); }
        }

        public static string ErrFileExt
        {
            get { return "err_file_ext".ModLocalize("Invalid file extension"); }
        }

        public static string InlineEditHint
        {
            get { return "inline_edit_hint".ModLocalize("Double-click to edit"); }
        }

        public static string SaveUnsuccess
        {
            get { return "eshop_global_SaveUnsuccess".Localize("The operation can’t be completed because an item with the name already exists!"); }
        }

        public static string SaveSuccess
        {
            get { return "eshop_global_SaveSuccess".Localize("The operation is successful!"); }
        }

        public static string CharLeftHint
        {
            get { return "eshop_global_CharLeftHint".Localize("{0} of {1} characters used"); }
        }

        public static string PoweredbyCLAW
        {
            get { return "eshop_global_PoweredbyCLAW".Localize("<span>Powered by CLAW</span><img src=\"/areas/admin/styles/images/claw.png\" alt=\"Copyright\" />"); }
        }
        public static string Top
        {
            get { return "Top".ModLocalize("Top"); }
        }
        public static string Bottom
        {
            get { return "Bottom".ModLocalize("Bottom"); }
        }
        public static string Before
        {
            get { return "Before".ModLocalize("Before"); }
        }
        public static string After
        {
            get { return "After".ModLocalize("After"); }
        }
        public static string SortOrder
        {
            get { return "SortOrder".ModLocalize("Sort order"); }
        }
        public static string SearchProducts
        {
            get { return "SearchProducts".ModLocalize("Search product by name"); }
        }

        public static string CategorySetting
        {
            get { return "Category_setting".ModLocalize("Category setting"); }
        }

        public static string ShowProductsCategoryOnly
        {
            get { return "Show_products_category_only".ModLocalize("Show products from the current category only"); }
        }
        public static string ShowProductsChildrenyOnly
        {
            get { return "Show_products_childreny_only".ModLocalize("Show products from the current category and its children"); }
        }
        public static string ShowProductsGrid
        {
            get { return "Show_products_grid".ModLocalize("Show products in a grid"); }
        }
        public static string ShowProductsList
        {
            get { return "Show_products_list".ModLocalize("Show products in a list"); }
        }
        public static string SettingAllowsRoundPrices
        {
            get
            {
                return "Setting_Allows_round_prices".ModLocalize(@"This setting allows to round the product prices in the front-end store to force integer values 
                                                                    instead of ugly decimal values. For example: 95,21$ rounds to 95$. 90,58$ rounds to 91$");
            }
        }

        public static string StoreDetail
        {
            get { return "Store_detail".ModLocalize("Store detail"); }
        }
        public static string EditStoreInformation
        {
            get { return "Edit_store_information".ModLocalize("Edit store information: name ( will appear on invoice send to customers, address,….)"); }
        }
        public static string StoreEmail
        {
            get { return "Store_email".ModLocalize("Store contact email"); }
        }

        public static string EditStoreEmailWhichUser
        {
            get { return "Edit_store_email_which_user".ModLocalize("Edit store email which user for contact customer…."); }
        }
        public static string YouCustomerWillSee
        {
            get { return "You_customer_will_see".ModLocalize("Your customer will see this when you email them (e.g.: order confirmations…)"); }
        }
        public static string RemoveFromAccessory
        {
            get { return "Remove from accessories".ModLocalize(); }
        }
        public static string RemoveFromBundle
        {
            get { return "Remove from bundle".ModLocalize("Remove from bundle"); }
        }
        public static string RemoveVariant
        {
            get { return "Remove variant".ModLocalize("Remove variant"); }
        }
        public static string RemoveFromCategory
        {
            get { return "Remove from category".ModLocalize("Remove from category"); }
        }

        public static string AddValue
        {
            get { return "Add value".ModLocalize("Add value"); }
        }
        public static string AddProperty
        {
            get { return "Add property".ModLocalize("Add variant"); }
        }
        public static string SelectVariants
        {
            get { return "Select variants".ModLocalize("Select variant"); }
        }
        public static string SelectAnOptionToMoveThisCategory
        {
            get { return "Select an option to move this category".ModLocalize("Select an option in order to move this category"); }
        }
        public static string MoveCategoryTo
        {
            get { return "Move category to".ModLocalize(); }
        }
        public static string TargetCategory
        {
            get { return "Target category".ModLocalize(); }
        }
        public static string MoveCategories
        {
            get { return "Move categories".ModLocalize("Move this category"); }
        }
        public static string ProductImages
        {
            get { return "Product images".ModLocalize("Product image"); }
        }
        public static string TheLargestImageShowInYourProduct
        {
            get
            {
                return "The_largest_image_show_in_your_product".ModLocalize(@"The largest image show in your product’s main image.
                                                                            You can set a new one by selecting radio button underneath image and it becomes main image of product");
            }
        }

        public static string UpdateDefault
        {
            get { return "Update default".ModLocalize(); }
        }

        public static string ProductVariants
        {
            get { return "Product variants".ModLocalize("Product variant"); }
        }

        public static string VariantsAreTheIndividualStyles
        {
            get
            {
                return "Variants are the individual styles".ModLocalize(@"Variants are the individual styles of a product in your store.If you have a product that your customers 
                                                                        would choose in a different size, colour, or with add-on, you should list that product with a variant combination");
            }
        }

        public static string ErrMsgDuplicateVariant
        {
            get { return "error_duplicate_variant".ModLocalize("The variant is duplicated! Action was cancelled"); }
        }

        public static string ErrMsgDuplicateTag
        {
            get { return "error_duplicate_tag".ModLocalize("The tag is duplicated! Action was cancelled"); }
        }

        public static string AddVariants
        {
            get { return "Add variants".ModLocalize("Add variant"); }
        }

        public static string Accessories
        {
            get { return "Accessories".ModLocalize(); }
        }

        public static string AccessoryAddingHint
        {
            get { return "Accessory Adding Hint".ModLocalize(" "); }
        }

        public static string AddingSeparateProducts
        {
            get { return "Adding separate products".ModLocalize("Adding separate products to this bundle to make it become sigle product"); }
        }

        public static string Items
        {
            get { return "Items".ModLocalize(); }
        }
        public static string HiddenWeb
        {
            get { return "Hidden web".ModLocalize("Hidden (web)"); }
        }
        public static string ProductNameOrSku
        {
            get { return "Product name or sku".ModLocalize("Search by product name or SKU"); }
        }
        public static string SearchOrderPlaceHolder
        {
            get { return "search_order_placeholder".ModLocalize("order# or invoice#"); }
        }
        public static string AdvancedSearch
        {
            get { return "Advanced search".ModLocalize(); }
        }
        public static string Name
        {
            get { return "Name".ModLocalize("Search by product name"); }
        }
        public static string SearchByCategory
        {
            get { return "Search by category".ModLocalize(); }
        }
        public static string SearchByManufacturer
        {
            get { return "Search by manufacturer".ModLocalize(); }
        }
        public static string SearchByProductsStatus
        {
            get { return "Search by products status".ModLocalize(); }
        }
        public static string SearchByProductsVisibleHidden
        {
            get { return "Search by products visible_hidden".ModLocalize("Search by products visible/hidden"); }
        }
        public static string SearchBySKU
        {
            get { return "Search by SKU".ModLocalize(); }
        }
        public static string SelectAnItem
        {
            get { return "Please select an item".ModLocalize("Please select an item!"); }
        }
        public static string ManageProductTaxes
        {
            get { return "Manage product taxe".ModLocalize("Manage tax"); }
        }
        public static string Taxes
        {
            get { return "Tax".ModLocalize(); }
        }
        public static string CurrencyText
        {
            get { return "Currency".ModLocalize(); }
        }

        public static string Assigncategory
        {
            get { return "Assign category".ModLocalize("Add product to category"); }
        }

        public static string AssignProduct
        {
            get { return "Assign product".ModLocalize(); }
        }

        public static string AddNewVariant
        {
            get { return "Add new variant".ModLocalize(); }
        }

        public static string AddOneVariant
        {
            get { return "Add one variant".ModLocalize(); }
        }

        public static string AssignToCates
        {
            get { return "Assign to categories".ModLocalize("This product belongs to category"); }
        }

        public static string AddOrDropImage
        {
            get { return "Add image or drag and drop to upload".ModLocalize("Click Upload or drag and drop image"); }

        }

        public static string AddTag
        {
            get { return "Add tag".ModLocalize(); }
        }
        public static string ViewChangeValues
        {
            get { return "view change values".ModLocalize("Edit variant value"); }
        }

        public static string Close
        {
            get { return "Close".ModLocalize(); }
        }
    }
}
