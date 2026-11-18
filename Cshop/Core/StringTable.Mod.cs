
namespace Claw.Modules.Eshop { internal class StringTable : Core.StringTable {} }

namespace Claw.Modules.Eshop.Core
{
    public partial class StringTable
    {
        public const string ErrMsgInvalidCodeFormat = "eshop_adm_errmsg_invalid_code_format,The valid characters are letter, digit, '%' or '-'";
        public const string ErrMsgInvalidCouponCode = "eshop_adm_errmsg_invalid_coupon_code,The value is not a valid coupon code";

        public static string MenuLeftKey
        {
            get { return "LeftMenuEshop"; }
        }

        public static string AdminHomeTitle
        {
            get { return "Manage eshop".ModLocalize(); }
        }

        public static string StatusDeleteFailedMsg
        {
            get { return "status_delete_failed_msg".ModLocalize("This item cannot be deleted because it is in use! (product: {0})"); }
        }

        public static string SelectLanguage
        {
            get { return "Select language".ModLocalize(); }
        }

        public static string ExportExcel
        {
            get { return "Export to excel".ModLocalize(); }
        }

        public static string ExcelFileIsOpen
        {
            get { return "Excel file is open".ModLocalize(); }
        }

        public static string CancelExport
        {
            get { return "Cancel export".ModLocalize(); }
        }

        public static string ExportData
        {
            get { return "Export data".ModLocalize(); }
        }

        public static string ImportExport
        {
            get { return "Import Export".ModLocalize("Import/Export"); }
        }

        public static string ManageImportExport
        {
            get { return "Manage import export".ModLocalize("Manage import/export"); }
        }

        public static string Import
        {
            get { return "Import".ModLocalize(); }
        }

        public static string Export
        {
            get { return "Export".ModLocalize(); }
        }

        public static string ErrDateMinLimit
        {
            get { return "err_date_min_limit".ModLocalize("The value can not less than {0}"); }
        }

        public static string ErrMsgInvalidFieldValue
        {
            get { return "errmsg_invalid_field_value".ModLocalize("Value '{0}' can not assign to field"); }
        }

        public static string ErrMsgInvalidImageValue
        {
            get { return "errmsg_invalid_image_value".ModLocalize("Can not find image with name : '{0}'"); }
        }

        public static string Clone
        {
            get { return "Clone".ModLocalize(); }
        }

        public static string Featured
        {
            get { return "Featured".ModLocalize("Featured product"); }
        }

        public static string ErrMsgInvalidPostalCode
        {
            get { return "The value is not a postal code".ModLocalize(); }
        }

        public static string Back
        {
            get { return "Back".ModLocalize(); }
        }

        public static string Mob
        {
            get { return "Mob".ModLocalize(); }
        }

        public static string Date
        {
            get { return "Date".ModLocalize(); }
        }

        public static string UserNo
        {
            get { return "User_".ModLocalize("User #"); }
        }

        public static string User
        {
            get { return "User".ModLocalize(); }
        }

        public static string Status
        {
            get { return "Status".ModLocalize(); }

        }

        public static string LoadExcelFile
        {
            get { return "Load excel file".ModLocalize(); }
        }

        public static string SupportExcelOnly
        {
            get { return "support _xls_xlsx".ModLocalize("(support .xls/.xlsx)"); }
        }

        public static string SelectMapping
        {
            get { return "Select mapping".ModLocalize(); }
        }

        public static string ImportRequiredField
        {
            get { return "Import required field".ModLocalize(); }
        }

        public static string ImportSuccess
        {
            get { return "Import successfully".ModLocalize(); }
        }

        public static string PageTitle
        {
            get { return "Page title".ModLocalize(); }
        }

        public static string MetaDescription
        {
            get { return "Meta description".ModLocalize(); }
        }

        public static string URL
        {
            get { return "URL and handle".ModLocalize(); }
        }

        public static string InvalidImportExcelFile
        {
            get { return "Invalid import excel file!".ModLocalize(); }
        }

        public static string MappingFirst
        {
            get { return "Please mapping required columns:".ModLocalize(); }
        }

        public static string DuplicateMapping
        {
            get { return "Duplicate mapping:".ModLocalize(); }
        }

        public static string CancelImport
        {
            get { return "Cancel import".ModLocalize(); }
        }

        public static string ImportData
        {
            get { return "Import data".ModLocalize(); }
        }

        public static string ValidateBeforeImport
        {
            get { return "Validate the file before import".ModLocalize("Validate the file before import!"); }
        }

        public static string ImportMoneyField
        {
            get { return "Import money field".ModLocalize(); }
        }

        public static string ImportNumberField
        {
            get { return "Import number field".ModLocalize(); }
        }

        public static string ImportTaxField
        {
            get { return "Value must be from 0 to 100".ModLocalize(); }
        }

        public static string Generate
        {
            get { return "generate".ModLocalize("Generate"); }
        }

        public static string SelectFieldsToExport
        {
            get { return "Select fields to export".ModLocalize(); }
        }

        public static string DownLoadA
        {
            get { return "Down load a".ModLocalize(); }
        }

        public static string SampleExcelFile
        {
            get { return "sample excel file".ModLocalize(); }
        }

        public static string ToSeeFileFormatRequire
        {
            get { return "too see file format required".ModLocalize("Too see file format required."); }
        }

        public static string ChangeValue
        {
            get { return "Change value".ModLocalize("Change value"); }
        }

        public static string SelectFont
        {
            get { return "Select font".ModLocalize("Select font"); }
        }

        public static string Comment
        {
            get { return "Comment".ModLocalize("Comment"); }
        }

        public static string CreatedDate
        {
            get { return "Created date".ModLocalize("Created date"); }
        }

        public static string UserID
        {
            get { return "UserID".ModLocalize("UserID"); }
        }

        public static string FromDate
        {
            get { return "From date".ModLocalize("From date"); }
        }

        public static string ToDate
        {
            get { return "To date".ModLocalize("To date"); }
        }

        public static string ActionLogs
        {
            get { return "Action logs".ModLocalize("Activities"); }
        }

        public static string ManageActionLogs
        {
            get { return "Manage action logs".ModLocalize("Manage activities"); }
        }

        public static string ManageTaskLogs
        {
            get { return "Manage task logs".ModLocalize(); }
        }

        public static string ManageTraces
        {
            get { return "Manage traces".ModLocalize(); }
        }

        public static string FromSendmail
        {
            get { return "From".ModLocalize(); }
        }

        public static string FromSendSMS
        {
            get { return "FromSendSMS".ModLocalize("From"); }
        }

        public static string SmsPackages
        {
            get { return "SmsPackages".ModLocalize("SMS packages"); }
        }

        public static string SmsSubmitOrder
        {
            get { return "SmsSubmitOrder".ModLocalize("Bestill"); }
        }

        public static string SelectSmsPackages
        {
            get { return "SelectSmsPackages".ModLocalize("Select SMS packages"); }
        }

        public static string ToSendSMS
        {
            get { return "ToSendSMS".ModLocalize("To"); }
        }

        public static string ToSendmail
        {
            get { return "To".ModLocalize(); }
        }

        public static string SubjectSendmail
        {
            get { return "Subject".ModLocalize(); }
        }

        public static string MessageSendmail
        {
            get { return "Message".ModLocalize(); }
        }

        public static string MessageSendSMS
        {
            get { return "MessageSendSMS".ModLocalize("SMS message"); }
        }

        public static string OrderSmsPackage
        {
            get { return "OrderSmsPackage".ModLocalize("Bestill flere SMS"); }
        }

        public static string OrderSmsLogTemplate
        {
            get { return "OrderSmsLogTemplate".ModLocalize("{0}. Previous amount: {1}. Order amount: {2}. New amount: {3}."); }
        }

        //public static string SendMailSuccess
        //{
        //    get { return "Send mail success".ModLocalize("Send mail to customer success"); }
        //}

        //public static string SendMailUnSuccess
        //{
        //    get { return "Send mail unsuccess".ModLocalize("Send mail to customer unsuccess"); }
        //}

        public static string ManageCurrency
        {
            get { return "Manage currency".ModLocalize(); }
        }
        public static string AddEditCurrency(int id)
        {
            if (id == 0) return "Add currency".ModLocalize("Add new currency");
            else return "Edit currency".ModLocalize("Edit currency");
        }
        public static string CurrencyDetail
        {
            get { return "Currency detail".ModLocalize(); }
        }

        public static string CurrencyDisplay
        {
            get { return "Currency display".ModLocalize(); }
        }

        public static string MultiCurrencyPriceTitle
        {
            get { return "MultiCurrency Price Title".ModLocalize("{0} in currencies (incl. VAT): "); }
        }

        public static string MultiCurrencyVariantPriceTitle
        {
            get { return "MultiCurrency Variant Price Title".ModLocalize("Variant price in currencies (incl. VAT): "); }
        }

        public static string Permission
        {
            get { return "Permission".ModLocalize(); }
        }

        public static string ManagePermissions
        {
            get { return "Manage permissions".ModLocalize(); }
        }

        public static string AddEditPermission(int id)
        {
            if (id == 0) return "Add permission".ModLocalize("Add new permission");
            else return "Edit permission".ModLocalize("Edit permission");
        }

        public static string ManageUsers
        {
            get { return "Manage users".ModLocalize(); }
        }

        public static string ErrMsgImportUpdateFailed
        {
            get { return "errmsg_import_update_failed".ModLocalize("Save data failed at row {0} [Id = {1}]"); }
        }

        public static string ErrMsgImportCellFormatMustBeString
        {
            get { return "errmsg_import_cell_format_must_be_string".ModLocalize("Cell format must be Text"); }
        }

        public static string InputCountry
        {
            get { return "Input country".ModLocalize("Type country name or choose country/region from list"); }
        }

        public static string InputCity
        {
            get { return "Input city".ModLocalize("Type city name or choose from list"); }
        }

        public static string InputDistrict
        {
            get { return "Input district".ModLocalize("Type district name or choose from list"); }
        }

        public static string ProvincesPerStates
        {
            get { return "Provinces states".ModLocalize("Province/State"); }
        }

        public static string Districts
        {
            get { return "Districts".ModLocalize(); }
        }

        public static string UpdateTakeAway
        {
            get { return "Update takeaway".ModLocalize(); }
        }

        public static string DBChartTitle
        {
            get { return "DBChartTitle".ModLocalize("Order chart from {0} - {1}"); }
        }

        public static string ManageNotifications
        {
            get { return "Manage notification".ModLocalize(); }
        }

        public static string AddEditNotifications(int id)
        {
            if (id == 0) return "Add notification".ModLocalize("Add new notification");
            else return "Edit notification".ModLocalize();
        }

        public static string Icon
        {
            get { return "Icon".ModLocalize(); }
        }

        public static string DropHere
        {
            get { return "Drop here".ModLocalize(); }
        }

        public static string ChooseImage
        {
            get { return "Choose image".ModLocalize(); }
        }

        public static string UploadFileExcel
        {
            get { return "Upload file excel".ModLocalize("Upload file excel"); }
        }

        public static string UploadImagesImport
        {
            get { return "Upload imported images".ModLocalize("Upload images (in zip format)"); }
        }

        public static string UploadImages
        {
            get { return "Upload images".ModLocalize(); }
        }

        public static string VisibilityInLanguages
        {
            get { return "Visibility in languages".ModLocalize(); }
        }

        public static string UrlHandle
        {
            get { return "URL Handle".ModLocalize("URL name"); }
        }

        public static string SearchEngines
        {
            get { return "Search Engines".ModLocalize("Search engine setting"); }
        }

        public static string SearchEngineHint
        {
            get { return "Title Search Engine Hint".ModLocalize("Set up the page title, meta description and URL name. This etting defines how this product shows up on search site such as google, bring…."); }
        }

        public static string OnTheLeft
        {
            get { return "On the left".ModLocalize(); }
        }

        public static string OnTheRight
        {
            get { return "On the right".ModLocalize(); }
        }

        public static string Visible
        {
            get { return "Visible".ModLocalize(); }
        }

        public static string Hidden
        {
            get { return "Hidden".ModLocalize(); }
        }

        public static string Dashboard
        {
            get { return "Dashboard".ModLocalize("Dashboard"); }
        }

        public static string SelectCountry
        {
            get { return "Select country".ModLocalize(); }
        }

        public static string SelectValue
        {
            get { return "Select value".ModLocalize(); }
        }

        public static string AsOf
        {
            get { return "as of".ModLocalize("from"); }
        }

        public static string LanguagePhrase
        {
            get { return "Language".ModLocalize(); }
        }

        public static string AppliesTo
        {
            get { return "Applies to".ModLocalize(); }
        }

        public static string UploadSuccess
        {
            get { return "Upload successfully".ModLocalize(); }
        }

        public static string RequiredField
        {
            get { return "Import required field".ModLocalize("The field is required"); }
        }

        public static string ErrMsg
        {
            get { return "errmsg_Require_Atleast_One_Item".ModLocalize("No items selected"); }
        }

        public static string CurrencyRateHint
        {
            get { return "Currency Rate Hint".ModLocalize("Exchange rate: the conversion rate based again {0} is"); }
        }

        public static string Report
        {
            get { return "Report".ModLocalize("Report"); }
        }

        public static string SelectType
        {
            get { return "Select type".ModLocalize("Select template description"); }
        }

        public static string Payment
        {
            get { return "Payment".ModLocalize(); }
        }

        public static string Shipping
        {
            get { return "Shipping".ModLocalize(); }
        }

        public static string ShippingZone
        {
            get { return "Shipping zone".ModLocalize(); }
        }

        public static string Backorder
        {
            get { return "Backorder".ModLocalize("Backorder"); }
        }

        public static string Customers
        {
            get { return "Customers".ModLocalize("Customers"); }
        }

        public static string CustomerGroups
        {
            get { return "CustomerGroups".ModLocalize("Customer groups"); }
        }

        public static string NoAuthorizeMsg
        {
            get { return "No authorize msg".ModLocalize("Your action is not authorized"); }
        }

        public static string Logistics
        {
            get { return "Logistics".ModLocalize(); }
        }  
    }
}