using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Claw.Modules.Eshop.Controllers.Admin;
using Claw.Modules.Eshop.Helper;
using Claw.Core;
using System.ComponentModel.DataAnnotations;

namespace Claw.Modules.Eshop.Models
{
    #region SizeModel
    public class SizeModel
    {
        #region SizeModel

        public SizeModel()
        {
        }

        public SizeModel(SettingsBiz.SizeBiz obj)
        {
            Width = obj.Width;
            Height = obj.Height;
        }

        #endregion

        [Display(Name = "Width")]
        [Required(ErrorMessage = StringTable.ErrMsgRequiredField)]
        [Range(10, 9999, ErrorMessage = StringTable.ErrMsgRangeLimit)]
        public int Width { get; set; }

        [Display(Name = "Height")]
        [Required(ErrorMessage = StringTable.ErrMsgRequiredField)]
        [Range(10, 9999, ErrorMessage = StringTable.ErrMsgRangeLimit)]
        public int Height { get; set; }
    }
    #endregion

    #region SettingModel
    public class SettingModel
    {
        public GeneralModel General { get; set; }
        public DisplayModel Display { get; set; }
        public AccountingModel Accounting { get; set; }
    }

    #endregion
    #region setting general model

    public class GeneralModel
    {
        [Display(Name = "Search Url")]
        public string SearchUrl { get; set; }

        [Display(Name = "Currency")]
        public string Currency { get; set; }

        public int DefaultTaxId { get; set; }

        [Display(Name = "Store name")]
        [Required(ErrorMessage = StringTable.ErrMsgRequiredField)]
        public string StoreName { get; set; }

        [Display(Name = "Org#")]
        public string OrgNumber { get; set; }

        [StringLength(15, ErrorMessage = StringTable.ErrMsgLengthLimit)]
        //[RegularExpression(@"^\s*\+?[0-9]\d?[-. ]?([0-9]{4})[-. ]?([0-9]{3})$", ErrorMessage = StringTable.ErrMsgValidPhone)]
        [Display(Name = "Contact phone")]
        public string ContactPhone { get; set; }

        [StringLength(15, ErrorMessage = StringTable.ErrMsgLengthLimit)]
        //[RegularExpression(@"^\s*\+?[0-9]\d?[-. ]?([0-9]{4})[-. ]?([0-9]{3})$", ErrorMessage = StringTable.ErrMsgValidFax)]
        [Display(Name = "Fax")]
        public string Fax { get; set; }

        [Required(ErrorMessage = StringTable.ErrMsgRequiredField)]
        [Display(Name = "Address")]
        public string Address { get; set; }

        [Display(Name = "Postnr")]
        [Required(ErrorMessage = StringTable.ErrMsgRequiredField)]
        public string Postnr { get; set; }

        [Display(Name = "Poststed")]
        [Required(ErrorMessage = StringTable.ErrMsgRequiredField)]
        public string Poststed { get; set; }

        [Display(Name = "City")]
        public string City { get; set; }

        [Display(Name = "Country")]
        public string Country { get; set; }

        [Display(Name = "Logo")]
        public string Logo { get; set; }
        public string ImageFsUrl { get; set; }


        [Required(ErrorMessage = StringTable.ErrMsgRequiredField)]
        [StringLength(150, ErrorMessage = StringTable.ErrMsgLengthLimit)]
        [EmailAddress(ErrorMessage = StringTable.ErrMsgInvalidEmail)]
        [Display(Name = "Customer email")]
        public string PublicEmail { get; set; }

        [Required(ErrorMessage = StringTable.ErrMsgRequiredField)]
        [StringLength(500, ErrorMessage = StringTable.ErrMsgLengthLimit)]
        //[EmailAddress(ErrorMessage = StringTable.ErrMsgInvalidEmail)]
        [Display(Name = "Email will receive new orders confirmation")]
        public string NotifyEmail { get; set; }
    }
    #endregion

    #region setting display model
    public class DisplayModel
    {
        [Display(Name = "Category product list")]
        public CategorySModel Category { get; set; }

        [Display(Name = "Product setting")]
        public ProductSModel Product { get; set; }

        [Display(Name = "Administration side setting")]
        public AdminModel Admin { get; set; }

        [Display(Name = "Others setting")]
        public OthersModel Others { get; set; }

        [Display(Name = "Barcode")]
        public BarCodeModel BarCode { get; set; }

        [Display(Name = "QrCode")]
        public QrCodeModel qrCode { get; set; }

        [Display(Name = "Google E-Commerce Tracking")]
        public EcommerceTrackingModel GoogleEcommerce { get; set; }

        [Display(Name = "Google analytics 4")]
        public GoogleAnalytics4Model GoogleAnalytics4 { get; set; }
    }
    #endregion

    #region CategorySModel

    public class CategorySModel
    {
        [Display(Name = "Inc Sub cate")]
        public bool IncSubCate { get; set; }

        [Display(Name = "Category icon size")]
        [Required(ErrorMessage = StringTable.ErrMsgRequiredField)]
        public SizeModel Icon { get; set; }

        [Display(Name = "Image size")]
        [Required(ErrorMessage = StringTable.ErrMsgRequiredField)]
        public SizeModel Image { get; set; }

        [Display(Name = "Banner 0 size")]
        [Required(ErrorMessage = StringTable.ErrMsgRequiredField)]
        public SizeModel Banner0 { get; set; }

        [Display(Name = "Banner 1 size")]
        [Required(ErrorMessage = StringTable.ErrMsgRequiredField)]
        public SizeModel Banner1 { get; set; }

        [Display(Name = "Banner 2 size")]
        [Required(ErrorMessage = StringTable.ErrMsgRequiredField)]
        public SizeModel Banner2 { get; set; }

    }

    #endregion
    #region ProductSModel

    public class ProductSModel
    {
        [Display(Name = "Sort order for products")]
        public int Sorting { get; set; }

        [Display(Name = "Enable rounding product prices on front-end store")]
        public bool PriceRound { get; set; }

        [Display(Name = "Category display mode")]
        public bool GridMode { get; set; }

        [Display(Name = "Grid cols")]
        public int GridCols { get; set; }

        [Display(Name = "Front-end page size")]
        [Required(ErrorMessage = StringTable.ErrMsgRequiredField)]
        [Range(1, 1000, ErrorMessage = StringTable.ErrMsgRangeLimit)]
        public int PageSize { get; set; }

        [Display(Name = "Product image full size")]
        [Required(ErrorMessage = StringTable.ErrMsgRequiredField)]
        public SizeModel Image { get; set; }

        [Display(Name = "Product image thumbnail size")]
        [Required(ErrorMessage = StringTable.ErrMsgRequiredField)]
        public SizeModel Thumb { get; set; }

        [Display(Name = "Product image size on mobile devices")]
        [Required(ErrorMessage = StringTable.ErrMsgRequiredField)]
        public SizeModel ThumbMobile { get; set; }

        [Display(Name = "Product image size on tablet devices")]
        [Required(ErrorMessage = StringTable.ErrMsgRequiredField)]
        public SizeModel ThumbTablet { get; set; }

        [Display(Name = "Manufacturer logo size")]
        [Required(ErrorMessage = StringTable.ErrMsgRequiredField)]
        public SizeModel LogoManu { get; set; }

        [Display(Name = "Status icon size")]
        [Required(ErrorMessage = StringTable.ErrMsgRequiredField)]
        public SizeModel IconStatus { get; set; }

        [Display(Name = "Company/Store logo size (using for invoce, order confirmation,..)")]
        [Required(ErrorMessage = StringTable.ErrMsgRequiredField)]
        public SizeModel LogoSite { get; set; }

        [Display(Name = "Tech spec icon")]
        public SizeModel TechSpecIcon { get; set; }

    }

    #endregion
    #region AdminModel

    public class AdminModel
    {
        #region ImageBiz
        public class ImageModel
        {
            [Display(Name = "Auto-scalling of image")]
            public bool AutoResize { get; set; }

            [Display(Name = "Image quality")]
            [Required(ErrorMessage = StringTable.ErrMsgRequiredField)]
            [Range(0, int.MaxValue, ErrorMessage = StringTable.ErrMsgMinLimit)]
            public int Quality { get; set; }

            [Required(ErrorMessage = StringTable.ErrMsgRequiredField)]
            [Display(Name = "Image format supports (separate by ;)")]
            public string Extensions { get; set; }

            [Display(Name = "Background color")]
            public string BackgroundColor { get; set; }
        }
        #endregion
        #region MsTranslatorModel
        public class MsTranslatorModel
        {
            [Required(ErrorMessage = StringTable.ErrMsgRequiredField)]
            [Display(Name = "Name")]
            public string ClientId { get; set; }

            [Required(ErrorMessage = StringTable.ErrMsgRequiredField)]
            [Display(Name = "Key")]
            public string ClientSecret { get; set; }
        }
        #endregion
        #region PowerOfficeModel

        public class PowerOfficeModel
        {
            [Required(ErrorMessage = StringTable.ErrMsgRequiredField)]
            public string Mode { get; set; }

            [Required(ErrorMessage = StringTable.ErrMsgRequiredField)]
            [Display(Name = "Application key")]
            public string ApplicationKey { get; set; }

            [Required(ErrorMessage = StringTable.ErrMsgRequiredField)]
            [Display(Name = "Client key")]
            public string ClientKey { get; set; }
        }

        #endregion
        #region Social
        public class SocialModel
        {
            [Required(ErrorMessage = StringTable.ErrMsgRequiredField)]
            [Display(Name = "Facebook AppId")]
            public string FaceAppId { get; set; }

            [Required(ErrorMessage = StringTable.ErrMsgRequiredField)]
            [Display(Name = "Facebook App Secret")]
            public string FaceAppSecret { get; set; }


            [Required(ErrorMessage = StringTable.ErrMsgRequiredField)]
            [Display(Name = "Google ClientId")]
            public string GoogleClientId { get; set; }

            [Required(ErrorMessage = StringTable.ErrMsgRequiredField)]
            [Display(Name = "Google Client Secret")]
            public string GoogleClientSecret { get; set; }
        }
        #endregion
        #region LogistraModel
        public class LogistraModel
        {
            [Display(Name = "Test mode")]
            public bool TestMode { get; set; }

            [Display(Name = "Api key")]
            public string ApiKey { get; set; }

            [Display(Name = "Sender id")]
            public string SenderId { get; set; }
        }
        #endregion
        #region BringShippingModel
        public class BringShippingModel
        {
            [Display(Name = "Mode")]
            public string Mode { get; set; }

            [Display(Name = "Api uid")]
            public string ApiUid { get; set; }

            [Display(Name = "Api key")]
            public string ApiKey { get; set; }

            [Display(Name = "Mailbox cust id")]
            public string MailBoxCustId { get; set; }

            [Display(Name = "Cod account#")]
            public string CodAccountNumber { get; set; }
        }
        #endregion
        #region ProteriaModel
        public class ProteriaModel
        {
            [Display(Name = "Api key")]
            public string ApiKey { get; set; }

            [Display(Name = "Cod account number")]
            public string CodAccountNumber { get; set; }

            [Display(Name = "Print format (A4/Label)")]
            public string PrintFormat { get; set; }

            [Display(Name = "Auto send")]
            public bool AutoSend { get; set; }

            [Display(Name = "Auto print")]
            public bool AutoPrint { get; set; }
        }
        #endregion
        #region PostnordModel
        public class PostnordModel
        {
            [Display(Name = "Test mode")]
            public bool TestMode { get; set; }

            [Display(Name = "Api key")]
            public string ApiKey { get; set; }

            [Display(Name = "Application id")]
            public int ApplicationId { get; set; }

            [Display(Name = "Application name")]
            public string ApplicationName { get; set; }

            [Display(Name = "Customer id")]
            public string CustomerId { get; set; }

            [Display(Name = "Cod account number")]
            public string CodAccountNumber { get; set; }

            [Display(Name = "Label type")]
            public string LabelType { get; set; }

            [Display(Name = "Paper size")]
            public string PaperSize { get; set; }
        }
        #endregion
        #region ShippingModel
        public class ShippingModel
        {
            [Display(Name = "Width (cm)")]
            public int WidthInCm { get; set; }

            [Display(Name = "Height (cm)")]
            public int HeightInCm { get; set; }

            [Display(Name = "Length (cm)")]
            public int LengthInCm { get; set; }

            [Display(Name = "Default weight (gram)")]
            public int DefWeightInGram { get; set; }

            [Display(Name = "Bundle booking provider")]
            public int ShippingBundleBookingProvider { get; set; }
        }
        #endregion

        [Display(Name = "General listing page size")]
        //[Required(ErrorMessage = StringTable.ErrMsgRequiredField)]
        //[Range(1, 1000, ErrorMessage = StringTable.ErrMsgRangeLimit)]
        public int PageSize { get; set; }

        [Display(Name = "Product listing page size")]
        [Required(ErrorMessage = StringTable.ErrMsgRequiredField)]
        [Range(1, 1000, ErrorMessage = StringTable.ErrMsgRangeLimit)]
        public int ProductPageSize { get; set; }

        [Display(Name = "Order listing page size")]
        [Required(ErrorMessage = StringTable.ErrMsgRequiredField)]
        [Range(1, 1000, ErrorMessage = StringTable.ErrMsgRangeLimit)]
        public int OrderPageSize { get; set; }

        [Display(Name = "File format supports (separate by ;)")]
        [Required(ErrorMessage = StringTable.ErrMsgRequiredField)]
        public string FileExtensions { get; set; }

        [Display(Name = "File upload setting")]
        public ImageModel Image { get; set; }

        [Display(Name = "Microsoft translator")]
        public MsTranslatorModel MsTranslator { get; set; }

        [Display(Name = "Google and facebook")]
        public SocialModel Social { get; set; }

        [Display(Name = "Icon size")]
        public SizeModel IconSize { get; set; }

        public string PrintLangCode { get; set; }

        [Display(Name = "Power office")]
        public PowerOfficeModel PowerOffice { get; set; }
    }

    #endregion
    #region LogisticsModel
    public class LogisticsModel
    {
        [Display(Name = "Shipping")]
        public AdminModel.ShippingModel Shipping { get; set; }

        [Display(Name = "Logistra")]
        public AdminModel.LogistraModel Logistra { get; set; }

        [Display(Name = "Bring shipping")]
        public AdminModel.BringShippingModel BringShipping { get; set; }

        [Display(Name = "Proteria")]
        public AdminModel.ProteriaModel Proteria { get; set; }

        [Display(Name = "Postnord")]
        public AdminModel.PostnordModel Postnord { get; set; }
    }
    #endregion

    #region OthersModel
    public class OthersModel
    {
        [Display(Name = "Date time format")]
        [Required(ErrorMessage = StringTable.ErrMsgRequiredField)]
        public string DateTimeFormat { get; set; }

        [Display(Name = "Price formatt")]
        public string PriceFormat { get; set; }

        [Display(Name = "Default measurement unit")]
        public byte? MUnit { get; set; }

        [Display(Name = "Go to cart after click button Buy")]
        public bool GoToCart { get; set; }

        [Required(ErrorMessage = StringTable.ErrMsgRequiredField)]
        [Display(Name = "Change stock status from New to In stock after x day")]
        [Range(0, int.MaxValue, ErrorMessage = StringTable.ErrMsgMinLimit)]
        public int ChangeStockStatus { get; set; }

        public int PriceRound { get; set; }
        /// <summary>price focus on VAT-included value</summary>
        public bool PriceFocusInc { get; set; }
        /// <summary>price step in kendo number input</summary>
        public int PriceStep { get; set; }
        public OFloat PriceResolution { get; set; }

        /// <summary>Time Ex</summary>
        [Display(Name = "Time change passwords expire (minutes)")]
        public int TimeExpiredResetPassword { get; set; }
    }
    #endregion

    #region BarCode Model
    public class BarCodeModel
    {
        [Display(Name = "Standard")]
        public string Standard { get; set; }

        [Display(Name = "Font family")]
        public string FontFamily { get; set; }

        [Display(Name = "Font size")]
        [Required(ErrorMessage = StringTable.ErrMsgRequiredField)]
        [Range(0, int.MaxValue, ErrorMessage = StringTable.ErrMsgMinLimit)]
        public int FontSize { get; set; }

        [Display(Name = "Top margin")]
        [Required(ErrorMessage = StringTable.ErrMsgRequiredField)]
        [Range(0, int.MaxValue, ErrorMessage = StringTable.ErrMsgMinLimit)]
        public int TopMargin { get; set; }

        [Display(Name = "Bottom margin")]
        [Required(ErrorMessage = StringTable.ErrMsgRequiredField)]
        [Range(0, int.MaxValue, ErrorMessage = StringTable.ErrMsgMinLimit)]
        public int BottomMargin { get; set; }

        [Display(Name = "Left margin")]
        [Required(ErrorMessage = StringTable.ErrMsgRequiredField)]
        [Range(0, int.MaxValue, ErrorMessage = StringTable.ErrMsgMinLimit)]
        public int LeftMargin { get; set; }

        [Display(Name = "Right margin")]
        [Required(ErrorMessage = StringTable.ErrMsgRequiredField)]
        [Range(0, int.MaxValue, ErrorMessage = StringTable.ErrMsgMinLimit)]
        public int RightMargin { get; set; }

        [Display(Name = "Size (Millimeter)")]
        public SizeModel Image { get; set; }

        [Display(Name = "Bar height (Millimeter)")]
        [Required(ErrorMessage = StringTable.ErrMsgRequiredField)]
        [Range(0, int.MaxValue, ErrorMessage = StringTable.ErrMsgMinLimit)]
        public int BarHeight { get; set; }

        [Display(Name = "DPI X (dots per inch)")]
        [Required(ErrorMessage = StringTable.ErrMsgRequiredField)]
        [Range(0, int.MaxValue, ErrorMessage = StringTable.ErrMsgMinLimit)]
        public int DpiX { get; set; }

        [Display(Name = "DPI Y (dots per inch)")]
        [Required(ErrorMessage = StringTable.ErrMsgRequiredField)]
        [Range(0, int.MaxValue, ErrorMessage = StringTable.ErrMsgMinLimit)]
        public int DpiY { get; set; }

        [Display(Name = "Character length")]
        [Required(ErrorMessage = StringTable.ErrMsgRequiredField)]
        [Range(4, 50, ErrorMessage = StringTable.ErrMsgMinLimit)]
        public int CharLength { get; set; }
    }
    #endregion

    #region QrCodeModel
    public class QrCodeModel
    {
        [Display(Name = "DPI X (dots per inch)")]
        [Required(ErrorMessage = StringTable.ErrMsgRequiredField)]
        [Range(0, int.MaxValue, ErrorMessage = StringTable.ErrMsgMinLimit)]
        public int DpiX { get; set; }

        [Display(Name = "DPI Y (dots per inch)")]
        [Required(ErrorMessage = StringTable.ErrMsgRequiredField)]
        [Range(0, int.MaxValue, ErrorMessage = StringTable.ErrMsgMinLimit)]
        public int DpiY { get; set; }
    }
    #endregion
    #region Ecommerce Tracking
    public class EcommerceTrackingModel
    {
        [Display(Name = "Google account")]
        [Required(ErrorMessage = StringTable.ErrMsgRequiredField)]
        public string GoogleAccount { get; set; }

        [Display(Name = "Enable tracking")]
        public bool EnableTracking { get; set; }
    }
    #endregion
    #region GoogleAnalytics4
    public class GoogleAnalytics4Model
    {
        [Display(Name = "Code")]
        [Required(ErrorMessage = StringTable.ErrMsgRequiredField)]
        public string Code { get; set; }

        [Display(Name = "Enable")]
        public bool Enable { get; set; }
    }
    #endregion

    #region Accounting model
    public class AccountingModel
    {
        [Display(Name = "UniMicro")]
        public UniMicroModel UniMicro { get; set; }
    }
    #endregion

    #region UniMicro Model

    public class UniMicroModel
    {
        [Required(ErrorMessage = StringTable.ErrMsgRequiredField)]
        [Display(Name = "Client token url")]
        public string LoginUrl { get; set; }

        [Required(ErrorMessage = StringTable.ErrMsgRequiredField)]
        [Display(Name = "Base address")]
        public string BaseUrl { get; set; }

        [Required(ErrorMessage = StringTable.ErrMsgRequiredField)]
        [Display(Name = "ClientId")]
        public string ClientId { get; set; }

        [Required(ErrorMessage = StringTable.ErrMsgRequiredField)]
        [Display(Name = "Certificate file (*.p12)")]
        public string CertPath { get; set; }

        [Required(ErrorMessage = StringTable.ErrMsgRequiredField)]
        [Display(Name = "Certificate password")]
        public string CertPassword { get; set; }

        [Required(ErrorMessage = StringTable.ErrMsgRequiredField)]
        [Display(Name = "Client Access/API Resources")]
        public string Scope { get; set; }
    }

    #endregion
}