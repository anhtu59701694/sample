using AutoMapper;
using Claw.Controls.Attributes;
using Claw.Controls.Modules;
using Claw.Core;
using Claw.Core.Enumerations;
using Claw.Core.Extensions;
using Claw.Core.Interfaces;
using Claw.Core.Logs;
using Claw.Core.Utility;
using Claw.Modules.Eshop.DAL;
using Claw.Modules.Eshop.DAL.Interfaces;
using Claw.Modules.Eshop.Domain;
using Claw.Modules.Eshop.Domain.Interfaces;
using Claw.Modules.Eshop.Helper;
using Claw.Modules.Eshop.Models;
using Gma.DataStructures.StringSearch;
using Kendo.Mvc.Extensions;
using Kendo.Mvc.UI;
using NHibernate;
using NPOI.SS.Formula.Functions;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Mvc;
using System.Web.UI.WebControls;

namespace Claw.Modules.Eshop.Controllers.Admin
{
    [AdminPerm("CatalogBits", byte.MaxValue)]
    public class ProductController : BaseController<ProductModel, Domain.Product>
    {
        private const string IndexView = "Admin/ProductsIndex.cshtml";
        private const string EditView = "Admin/ProductsEdit.cshtml";

        #region ProductController
        public ProductController(IDataService ds) :
            base(ds)
        {
            InitImageSettings(x => x.Display.Product.Image, x => x.Display.Product.Thumb);

            QParam.AddTrivialArgs("selfDiscount", "location", "cgroupDiscount", "download", "barCode", "hasQtyOnly", "noCateOnly");
        }
        #endregion

        #region Index
        [ValidateInput(false)]
        [AdminPerm("CatalogBits", PermissionMask.View)]
        public ActionResult Index(ProductModel.AdvSearchModel searchForm, int? type, string lang, string mode, bool topcate = false, bool noload = false, bool noloadcate = false)
        {
            ViewBag.Settings = GetSettings();

            var sform = searchForm;
            ViewBag.EagerData = new GridEagerLoadDataHandler((page, pageSize, sortField, sortDir) => noload ? new DataSourceResult(){ Data = new ProductModel[0] } : List(page, pageSize, sortField, sortDir, sform));

            if (this is PriceCheckProductController)
                return ClawView("Admin/ProductsPriceCheck.cshtml");

            if (this is FeaturedProductController)
                return ClawView("Admin/ProductsFeature.cshtml");

            if (this is PrisguideProductController)
                return ClawView("Admin/ProductsPrisguide.cshtml");

            if (this is PrisjaktProductController)
                return ClawView("Admin/ProductsPrisjakt.cshtml");

            if (this is FBMarketingController)
                return ClawView("Admin/FBMarketing.cshtml");

            if (this is GoogleShoppingController)
                return ClawView("Admin/GoogleShopping.cshtml");

            if (this is BackorderProductController)
                return ClawView("Admin/ProductsBackorder.cshtml");

            if (this is EmailMarketProductController)
                return ClawView("Admin/ProductsEmailMarket.cshtml");

            if (this is RecurringProductController)
                return ClawView("Admin/ProductsRecurring.cshtml");

            if (type == (byte)ProductType.Bundle)
                return RedirectToAction("Index", "Product", new { lang, mode, type = (byte?)null });

            searchForm = searchForm ?? new ProductModel.AdvSearchModel();
            //var listImportFields = new List<Product>();

            //ViewBag.ListDataFields = null;  
            ViewBag.SearchInfo = searchForm;
            ViewBag.Categories = noloadcate ? new CategoryModel[0].ToList() : CategoryMgr.GetAll(this, 0, topcate ? -1 : (int?)null);//CategoryMgr.GetAll(this, 0, null).Where(x => x.AnyChild).ToList();
            ViewBag.Statuses = StatusMgr.GetAll(this, 1).ToList();//StatusMgr.GetAll(this, 1).Where(m => m.Active).ToList();
            ViewBag.Manufactureres = ManufacturerMgr.GetAll(this, 0, true).ToList();

            if ((searchForm.CategoryId ?? 0) != 0)
                ViewBag.Category = CategoryMgr.Get(this, Math.Abs(searchForm.CategoryId ?? 0));

            if ((searchForm.Status ?? 0) != 0)
                ViewBag.Status = StatusMgr.Get(this, Math.Abs(searchForm.Status ?? 0));

            return ClawView(IndexView);
        }
        #endregion

        #region List

        [ValidateInput(false)]
        [AdminPerm("CatalogBits", PermissionMask.View)]
        public ActionResult List([DataSourceRequest] DataSourceRequest request, ProductModel.AdvSearchModel searchForm,
            bool countVariant = false)
        {
            TrimReq(request);

            string sortField = "Id";
            var sortDir = SortDirection.Descending;

            GetSortField(request, ref sortField, ref sortDir);

            return Json(List(request.Page, request.PageSize, sortField, sortDir, searchForm, countVariant));
        }

        private DataSourceResult List(int page, int pageSize, string sortField, SortDirection sortDir,
            ProductModel.AdvSearchModel searchForm, bool countVariant = false)
        {
            searchForm = searchForm ?? new ProductModel.AdvSearchModel();

            int totalRows;
            var name = searchForm.ProductName;
            var sku = searchForm.Sku;
            var barcode = searchForm.BarCode;
            var type = GetParam<int?>("type");
            var prodType = GetParam<int?>("prodtype");
            var minQty = GetParam<int?>("minQty");
            var visibility = searchForm.VisibilityWeb;
            var categoryid = searchForm.CategoryId;
            var manufacturerId = searchForm.ManufacturerId;
            var exceptIds = GetParam<string>("exceptids");
            var exceptVariantIds = GetParam<string>("exceptVariantIds");
            var nameorsku = GetParam<string>("nameorsku");
            var forceNameSku = GetParam<bool?>("forcenamesku");
            var featured = GetParam<bool?>("featured") ?? (this is FeaturedProductController ? true : (bool?)null);
            var prisguide = GetParam<bool?>("prisguide") ?? (this is PrisguideProductController ? true : (bool?)null);
            var prisjakt = GetParam<bool?>("Prisjakt") ?? (this is PrisjaktProductController ? true : (bool?)null);
            var fbMarketing = GetParam<bool?>("FBMarketing") ?? (this is FBMarketingController ? true : (bool?)null);
            var googleShopping = GetParam<bool?>("GoogleShopping") ?? (this is GoogleShoppingController ? true : (bool?)null);
            var backorder = GetParam<bool?>("backorder") ?? (this is BackorderProductController ? true : (bool?)null);
            var emailMarketing = GetParam<bool?>("emailMarketing") ?? (this is EmailMarketProductController ? true : (bool?)null);
            var recurring = GetParam<bool?>("recurring") ?? (this is RecurringProductController ? true : (bool?)null);
            var status = searchForm.Status;
            var techSpecValId = GetParam<int?>("techSpecValId");
            var priceCheck = GetParam<bool?>("priceCheck");
            var notifyMe = GetParam<bool>("notifyMe");
            var download = searchForm.Download;
            var cgroupDiscount = searchForm.CGroupDiscount;

            string keyword = forceNameSku == true ? nameorsku : GetKeyword();

            var urlCurent = Request.RawUrl;
            if (Request.IsAjaxRequest())
                urlCurent = HttpContext.Request.UrlReferrer.LocalPath;

            if (urlCurent.ToString().ToLower().TrimEnd('/').Contains("/eshop/product"))
            {
                type = 0;
            }
            if (prodType.HasValue)
            {
                type = (prodType == -1 ? null : prodType);
            }

            if (this is BundleProductController) type = 1;

            if (this is PriceCheckProductController)
                priceCheck = true;

            if (this is NotifyMeProductController) notifyMe = true;

            if (notifyMe) EmailMarketUserMgr.BindStats(this);

            var items = ProductMgr.GetAll(this, LanguageMgr.FocusLangCode, categoryid, null, page,
                pageSize, out totalRows, sortDir, sortField, (keyword ?? "").Trim(), exceptIds, type,
                status, manufacturerId, visibility, name, sku, featured, backorder, minQty, exceptVariantIds,
                emailMarketing, prisguide, prisjakt, techSpecValId, recurring, searchForm.SelfDiscount, barcode,
                priceCheck, fbMarketing, true, null, googleShopping, GetSettings().Hidden.ShoppingProduct, searchForm.Location, notifyMe, download,
                cgroupDiscount, searchForm.HasQtyOnly, searchForm.NoCateOnly, countVariant && keyword?.Trim()?.Length > 1 ? true : (bool?)null);

            if (this is BackorderProductController) Singleton<IOrderMgr>.Default.BindStats(this, items.Cast<IProduct2>().ToList());
            else Singleton<IOrderMgr>.Default.BindStats(this, items.Select(x => (IProduct)x.GetRaw()).ToList());

            EmailMarketUserMgr.BindStats(this, items);

            items.Action(m =>
            {
                var raw = m.GetRaw();
                m.InlineEdit = true;
                //m.InitVariants(this);
                m.Information = raw.GetStatistics();
                m.IsBundle = (raw != null && raw.Type.InList(ProductType.Bundle));//||raw.GetBundleCount() > 0;
                if (countVariant) m.VariantCount = raw.GetVariantCount();
                m.AnyBundleRef = raw.AnyBundleRef();
                var activeLang = m.ActiveLang;
                if (activeLang != null)
                {
                    m.Languages.RemoveAll(z => z.Id != activeLang.Id);
                    activeLang.Description = activeLang.MetaDesc = null;
                    activeLang.ExtraInfo4 = activeLang.ExtraInfo3 = activeLang.ExtraInfo2 = activeLang.ExtraInfo = null;
                }
            });

            return new DataSourceResult() { Data = items, Total = totalRows };
        }

        #endregion

        #region ListBackOrder
        [ValidateInput(false)]
        public ActionResult ListBackOrder(int id, string lang, string optlang, string mode)
        {
            return RedirectToAction("Index", "Order", new { backproductid = id, lang, optlang, mode });
        }
        #endregion

        #region ListEmailMUser
        [ValidateInput(false)]
        public ActionResult ListEmailMUser(int id, string lang, string optlang, string mode)
        {
            return RedirectToAction("Index", "EmailMarketUser", new { productid = id, lang, optlang, mode });
        }
        #endregion

        #region Input.Get
        [ValidateInput(false)]
        [AdminPerm("CatalogBits", PermissionMask.View)]
        public ActionResult Input(int id, string lang, string optlang, string mode, int type = 0)
        {
            if (this is BundleProductController) type = 1;
            var obj = ProductMgr.Get(this, id);

            ViewBag.Languages = LanguageMgr.GetAll();
            ViewBag.Settings = GetSettings();
            ViewBag.Statuses = StatusMgr.GetAll(this, id);
            ViewBag.Vats = id > 0 ? VatMgr.GetAll(this, null) : VatMgr.GetAll(this, true);
            ViewBag.Properties = PropertyMgr.GetAll(this, true);
            ViewBag.Manufacturers = ManufacturerMgr.GetAll(this, id, true);
            ViewBag.Currencies = GetSettings().Hidden.SingleCurrency ? new List<CurrencyModel>() : CurrencyMgr.GetAll(this);

            //var lst = LocationMgr.GetAll(this).Select(x => new { x.Id, x.Name });

            if (obj == null)
            {
                obj = new ProductModel()
                {
                    Type = (ProductType)type,
                    Visibility = (VisibilityType)GetSettings().Hidden.DefaultVisibility,
                    Languages = new List<ProductLangModel>(),
                    Images = new List<ImageModel>(),
                };
                if (obj.Type == ProductType.Normal) obj.Variants = new List<VariantModel>();
                else obj.Bundlings = new List<ProductBundleModel>();
            }
            else
            {
                EmailMarketUserMgr.BindStats(this, new[] {obj}.ToList());
                ShoppingProductMgr.BindStats(this, new List<ProductBase>() { obj.GetRaw() });
                obj.ShoppingQty = obj.GetRaw().ShoppingQty;

                obj.InitImages();
                obj.AnyBundleRef = obj.GetRaw().AnyBundleRef();
                if (obj.Type == ProductType.Normal)
                {
                    EmailMarketUserMgr.BindStats(this, obj.GetRaw().Variants.ToList());
                    obj.InitVariants(this, false, true);
                }
                else obj.InitBundlings(this);
            }

            if (obj.Type == ProductType.Normal)
            {
                var templVar = ViewBag.TemplVariant = new VariantModel()
                {
                    Languages = new List<VariantLangModel>(),
                    Images = new List<VariantImageModel>()
                };

                var props = ViewBag.Properties as List<PropertyModel>;
                obj.Variants.Action(x => { x.InitProperties(this, props); x.InitImages(); });
                templVar.InitProperties(this, props);
            }
            else ViewBag.TemplBundling = new ProductBundleModel();

            ViewBag.TemplAccessory = new ProductAccessoryModel();
            ViewBag.TemplSerial = new ProductSerialModel();

            obj.InitRelationIds();
            obj.InitAccessories(this);
            obj.InitExtraInformation();
            obj.InitTechSVals(this);
            obj.InitRecurringDiscounts(this);
            obj.InitPrice4LoginCustGroups(this);
            obj.InitCGroupDiscounts(this);
            obj.InitSerialNumbers(this);
            obj.ImageKey = obj.Image == null ? null : obj.Image.Key;

            return ClawView(EditView, obj);
        }
        #endregion

        #region Input.Post

        [HttpPost]
        [ValidateInput(false)]
        //[ValidateAntiForgeryToken]
        [AdminPerm("CatalogBits", PermissionMask.Edit)]
        public ActionResult Input([DataSourceRequest]DataSourceRequest request, ProductModel obj,
            string name, string lang, string mode)
        {
            if (obj == null) return RedirectToAction("Index", new { lang = lang, mode = mode });

            if (!obj.InlineEdit)
            {
                var validResult = ProductModel.Validate(obj, null);
                if (validResult != null && validResult.MemberNames != null)
                    foreach (var itm in validResult.MemberNames)
                        ModelState.AddModelError(itm, validResult.ErrorMessage.ModErrLocalize());
            }

            #region RemoveInputValidate

            ModelState.RemoveKey(GetFieldName<ProductModel, object>(m => m.Opt.ManufacturerInput));
            ModelState.RemoveKey(GetFieldName<ProductModel, object>(m => m.Opt.ProductStatusInput));
            ModelState.RemoveKey(GetFieldName<ProductModel, object>(m => m.Opt.VatInput));
            ModelState.RemoveKey(GetFieldName<ProductModel, object>(m => m.Opt.PropertyNameInput));
            ModelState.RemoveKey(GetFieldName<ProductModel, object>(m => m.Opt.PropValNameInput));

            if (!obj.SelfDiscActive)
            {
                ModelState.RemoveKey(GetFieldName<ProductModel, int?>(m => m.SelfDiscMinQty));
                ModelState.RemoveKey(GetFieldName<ProductModel, object>(m => m.SelfDiscValue));
            }

            if (!obj.PriceCkActive)
                ModelState.RemoveKey(GetFieldName<ProductModel, OMoney>(m => m.PriceCkMinVal));

            if (!obj.PriceCkActive || !obj.PriceCkVariant)
                ModelState.RemoveKey(GetFieldName<ProductModel, string>(m => m.PriceCkUrl));

            var settings = Helper.SettingsBiz.GetSettings();

            if (settings.Hidden.Storage2Enabled)
            {
                ModelState.RemoveKey(GetFieldName<ProductModel, int>(m => m.Quantity));
            }
            else 
            {
                ModelState.RemoveKey(GetFieldName<ProductModel, int>(m => m.Quantity1));
                ModelState.RemoveKey(GetFieldName<ProductModel, int>(m => m.Quantity2));
            }

            #endregion

            #region Languages

            foreach (var itm in obj.Languages
                .Select((x, idx) => new { Val = x, Index = idx })
                .Where(x => x.Val.IsDataModelEmpty).OrderByDescending(x => x.Index).ToList())
            {
                obj.Languages.Remove(itm.Val);
                ModelState.RemoveKey(string.Format("{0}[{{0}}]",
                    GetFieldName<ProductModel, object>(m => m.Languages)),
                    itm.Index, obj.Languages.Count);
            }

            #endregion

            #region Variants

            if (obj.Variants != null)
            {
                Trie<Product.TrieItem> trie = null;
                for (int i = 0; i < obj.Variants.Count; i++)
                {
                    var vari = obj.Variants[i];
                    var pix = i;

                    if (settings.Hidden.Storage2Enabled)
                    {
                        ModelState.RemoveKey2(GetFieldName<ProductModel, int>(m => m.Variants[pix].Quantity),  trie: ref trie);
                    }
                    else
                    {
                        ModelState.RemoveKey2(GetFieldName<ProductModel, int>(m => m.Variants[pix].Quantity1), trie: ref trie);
                        ModelState.RemoveKey2(GetFieldName<ProductModel, int>(m => m.Variants[pix].Quantity2), trie: ref trie);
                    }

                    #region Languages

                    if (vari.Languages != null)
                        foreach (var itm in vari.Languages
                            .Select((x, idx) => new { Val = x, Index = idx })
                            .Where(x => x.Val.IsDataModelEmpty).OrderByDescending(x => x.Index).ToList())
                        {
                            vari.Languages.Remove(itm.Val);
                            ModelState.RemoveKey2(string.Format("{0}[{{0}}]",
                                GetFieldName<ProductModel, object>(m => m.Variants[pix].Languages)),
                                ref trie, itm.Index, vari.Languages.Count);
                        }
                    else vari.Languages = new List<VariantLangModel>();

                    #endregion

                    #region Images

                    if (vari.Images != null)
                        foreach (var itm in vari.Images
                            .Select((x, idx) => new { Val = x, Index = idx })
                            .Where(x => x.Val.IsDataModelEmpty).OrderByDescending(x => x.Index).ToList())
                        {
                            vari.Images.Remove(itm.Val);
                            ModelState.RemoveKey2(string.Format("{0}[{{0}}]",
                                GetFieldName<ProductModel, object>(m => m.Variants[pix].Images)),
                                ref trie, itm.Index, vari.Images.Count);
                        }
                    else vari.Images = new List<VariantImageModel>();

                    #endregion

                    #region Properties
                    if (vari.Properties != null)
                        foreach (var itm in vari.Properties
                            .Select((x, idx) => new { Val = x, Index = idx })
                            .Where(x => x.Val.IsDataModelEmpty(this)).OrderByDescending(x => x.Index).ToList())
                        {
                            vari.Properties.Remove(itm.Val);
                            ModelState.RemoveKey2(string.Format("{0}[{{0}}]",
                                GetFieldName<ProductModel, object>(m => m.Variants[pix].Properties)),
                                ref trie, itm.Index, vari.Properties.Count);
                        }
                    else vari.Properties = new List<VariantPropValModel>();
                    #endregion
                }

                foreach (var itm in obj.Variants
                    .Select((x, idx) => new { Val = x, Index = idx })
                    .Where(x => x.Val.IsDataModelEmpty).OrderByDescending(x => x.Index).ToList())
                {
                    obj.Variants.Remove(itm.Val);
                    ModelState.RemoveKey(string.Format("{0}[{{0}}]",
                        GetFieldName<ProductModel, object>(m => m.Variants)),
                        itm.Index, obj.Variants.Count);
                }
            }
            else obj.Variants = new List<VariantModel>();

            #endregion

            #region Images

            if (obj.Images != null)
            {
                for (int i = 0; i < obj.Images.Count; i++)
                {
                    var img = obj.Images[i];
                    if (img.Languages != null)
                        foreach (var itm in img.Languages
                            .Select((x, idx) => new { Val = x, Index = idx })
                            .Where(x => x.Val.IsDataModelEmpty).OrderByDescending(x => x.Index).ToList())
                        {
                            var pix = i;
                            img.Languages.Remove(itm.Val);
                            ModelState.RemoveKey(string.Format("{0}[{{0}}]",
                                GetFieldName<ProductModel, object>(m => m.Images[pix].Languages)),
                                itm.Index, img.Languages.Count);
                        }
                    else img.Languages = new List<ImageLangModel>();
                }

                foreach (var itm in obj.Images
                    .Select((x, idx) => new { Val = x, Index = idx })
                    .Where(x => x.Val.IsDataModelEmpty).OrderByDescending(x => x.Index).ToList())
                {
                    obj.Images.Remove(itm.Val);
                    ModelState.RemoveKey(string.Format("{0}[{{0}}]",
                        GetFieldName<ProductModel, object>(m => m.Images)),
                        itm.Index, obj.Images.Count);
                }
            }
            else obj.Images = new List<ImageModel>();

            #endregion

            #region Bundlings
            if (obj.Bundlings != null)
                foreach (var itm in obj.Bundlings
                    .Select((x, idx) => new { Val = x, Index = idx })
                    .Where(x => x.Val.IsDataModelEmpty).OrderByDescending(x => x.Index).ToList())
                {
                    obj.Bundlings.Remove(itm.Val);
                    ModelState.RemoveKey(string.Format("{0}[{{0}}]",
                        GetFieldName<ProductModel, object>(m => m.Bundlings)),
                        itm.Index, obj.Bundlings.Count);
                }

            #endregion

            #region Accessories

            if (obj.Accessories != null)
                foreach (var itm in obj.Accessories
                    .Select((x, idx) => new { Val = x, Index = idx })
                    .Where(x => x.Val.IsDataModelEmpty).OrderByDescending(x => x.Index).ToList())
                {
                    obj.Accessories.Remove(itm.Val);
                    ModelState.RemoveKey(string.Format("{0}[{{0}}]",
                        GetFieldName<ProductModel, object>(m => m.Accessories)),
                        itm.Index, obj.Accessories.Count);
                }
            else obj.Accessories = new List<ProductAccessoryModel>();

            #endregion

            #region SerialNumbers

            if (obj.SerialNumbers != null)
                foreach (var itm in obj.SerialNumbers
                    .Select((x, idx) => new { Val = x, Index = idx })
                    .Where(x => x.Val.IsDataModelEmpty).OrderByDescending(x => x.Index).ToList())
                {
                    obj.SerialNumbers.Remove(itm.Val);
                    ModelState.RemoveKey(string.Format("{0}[{{0}}]",
                        GetFieldName<ProductModel, object>(m => m.SerialNumbers)),
                        itm.Index, obj.SerialNumbers.Count);
                }
            else obj.SerialNumbers = new List<ProductSerialModel>();

            #endregion

            #region ExtraInformation

            if (obj.ExtraInformation != null)
                foreach (var itm in obj.ExtraInformation
                    .Select((x, idx) => new { Val = x, Index = idx })
                    .Where(x => x.Val.Id == 0).OrderByDescending(x => x.Index).ToList())
                {
                    obj.ExtraInformation.Remove(itm.Val);
                    ModelState.RemoveKey(string.Format("{0}[{{0}}]",
                        GetFieldName<ProductModel, object>(m => m.ExtraInformation)),
                        itm.Index, obj.ExtraInformation.Count);
                }
            else obj.ExtraInformation = new List<ProductExtraInformationModel>();

            #endregion

            #region TechSVals

            if (obj.TechSVals != null)
            {
                foreach (var itm in obj.TechSVals
                    .Select((x, idx) => new { Val = x, Index = idx })
                    .Where(x => x.Val.IsDataModelEmpty).OrderByDescending(x => x.Index).ToList())
                {
                    obj.TechSVals.Remove(itm.Val);
                    ModelState.RemoveKey(string.Format("{0}[{{0}}]",
                        GetFieldName<ProductModel, object>(m => m.TechSVals)),
                        itm.Index, obj.TechSVals.Count);
                }
            }
            else obj.TechSVals = new List<ProductTechSValModel>();

            #endregion

            #region CGroupDiscounts

            if (obj.CGroupDiscounts != null)
                foreach (var itm in obj.CGroupDiscounts
                    .Select((x, idx) => new {Val = x, Index = idx})
                    .Where(x => x.Val.IsDataModelEmpty).OrderByDescending(x => x.Index).ToList())
                {
                    ModelState.RemoveKey(string.Format("{0}[{{0}}]",
                        GetFieldName<ProductModel, object>(m => m.CGroupDiscounts)),
                        itm.Index, obj.Languages.Count);
                }
            else obj.CGroupDiscounts = new List<ProductCGroupDiscModel>();

            #endregion

            #region InlineEdit

            if (obj.InlineEdit && obj.Id != 0)
            {
                obj = CopyValidate(obj, ProductMgr.Get(this, obj.Id), (nVal, oVal) =>
                {
                    nVal.InlineEdit = true;
                    nVal.SetName(name);
                    TryValidateModel(nVal.GetLang());
                });
                //obj.Information.InitLink(obj, Request);
            }

            #endregion

            #region Main

            if (!obj.InlineEdit && !QParam.IsFontEnd) obj.ValidateVariantDelete(this, ModelState);

            if (ModelState.ModLocalize().IsValid)
            {
                var isFontEnd = QParam.IsFontEnd;
                obj.Image = obj.Images.FirstOrDefault(m => m.Key == obj.ImageKey);

                #region MoveFile

                try
                {
                    var tmpPath = ModuleUrl.TempFolder.ResolveClientUrl();
                    if (obj.AttachFile != null && obj.AttachFile.StartsWith(tmpPath))
                        obj.AttachFile = MoveFile(ModuleUrl.AttachmentFolder, ModuleUrl.TempFolder, obj.AttachFile);

                    foreach (var img in obj.Images)
                    {
                        if (img.FsUrl.StartsWith(tmpPath))
                            img.FileName = MoveFile(ModuleUrl.ImageFolder, ModuleUrl.TempFolder, img.FsUrl);
                    }

                    foreach (var vari in obj.Variants)
                    {
                        if (SettingsBiz.GetSettings().Hidden.VariantMultiImage)
                        {
                            foreach (var img in vari.Images)
                            {
                                if (img.FsUrl.StartsWith(tmpPath))
                                    img.FileName = MoveFile(ModuleUrl.ImageFolder, ModuleUrl.TempFolder, img.FsUrl);
                            }

                            var fImg = vari.Images.OrderBy(x => x.DisplayOrder).FirstOrDefault();
                            vari.Image = fImg == null ? null : fImg.FileName;
                        }
                        else
                            if (vari.ImageFsUrl != null && vari.ImageFsUrl.StartsWith(tmpPath))
                                vari.Image = MoveFile(ModuleUrl.ImageFolder, ModuleUrl.TempFolder, vari.ImageFsUrl);
                    }
                }
                catch (Exception er)
                {
                    LogErr(er); goto LB_ERR;
                }

                #endregion

                #region DownloadImage

                #region Images
                for (int i = 0; i < obj.Images.Count; i++)
                {
                    var img = obj.Images[i];
                    if (Regex.IsMatch(img.FsUrl, @"^http(s)?://"))
                    {
                        var url = img.FsUrl;
                        if (SaveUrl(obj.Id + "-" + img.Id + "-" + Regex.Replace(obj.Alias ?? "", "[^a-zA-Z0-9]+", ""),
                            ref url, ModuleUrl.ImageFolder, ModuleUrl.ThumbFolder))
                        {
                            img.FileName = url;
                        }
                        else
                        {
                            ModelState.AddModelError(m => m.Images[i].FileName, obj, StringTable.ErrMsgSaveImageUrl(img.FileName));
                            goto LB_ERR;
                        }
                    }
                }
                #endregion

                #region Variants
                for (int k = 0; k < obj.Variants.Count; k++)
                {
                    var vari = obj.Variants[k];
                    for (int i = 0; i < vari.Images.Count; i++)
                    {
                        var img = vari.Images[i];
                        if (Regex.IsMatch(img.FsUrl, @"^http(s)?://"))
                        {
                            var url = img.FsUrl;
                            if (SaveUrl(obj.Id + "-" + vari.Id + "-" + img.Id + "-" + Regex.Replace(obj.Alias ?? "", "[^a-zA-Z0-9]+", ""),
                                ref url, ModuleUrl.ImageFolder, ModuleUrl.ThumbFolder))
                            {
                                img.FileName = url;
                            }
                            else
                            {
                                ModelState.AddModelError(m => m.Variants[k].Images[i].FileName, obj, StringTable.ErrMsgSaveImageUrl(img.FileName));
                                goto LB_ERR;
                            }
                        }
                    }
                }
                #endregion

                #endregion

                var db = new SeparatedDb();
                db.GetSession().FlushMode = FlushMode.Commit;
                var trans = db.GetSession().BeginTransaction();

                foreach (var itm in obj.TechSVals.Where(x => (x.TechSpecValId ?? 0) == 0))
                {
                    var tsVal = new TechSpecValModel() { Value = itm.TechSpecValName, TechSpecId = itm.TechSpecId };
                    TechSpecValMgr.SaveorUpdate(db, tsVal, false);
                    itm.TechSpecValId = tsVal.Id;
                }

                if (ProductMgr.SaveorUpdate(db, obj, isFontEnd, Request, false))
                {
                    trans.Commit();

                    if (!obj.InlineEdit) SetViewMsgInput(true);
                    obj.Variants.Select(x => x.Location).Concat(new[] { obj.Location })
                        .Where(x => !string.IsNullOrWhiteSpace(x))
                        .Distinct(x => x.ToLower().Trim())
                        .Action(x => LocationMgr.SaveorUpdate(this, new LocationModel() { Name = x }));
                }
                else goto LB_ERR;

                var selfReload = QParam.GetField<bool>("selfReload", false, true);
                var refPartId = QParam.GetField<string>("refPartId", false, true);
                var query = new NameValueCollection(Request.QueryString);

                query.Remove("selfReload");
                query.Remove("refPartId");

                if (!obj.InlineEdit && selfReload)
                    return Redirect(ModuleUrl.AdminUrl("bundleproduct", "Input", obj.Id) + "?" + query.AllKeys.Select(m => string.Format("{0}={1}", m, query[m])).ToString("&") + (refPartId.IsNullOrEmpty() ? "" : ("#" + refPartId)));

                return obj.InlineEdit
                    ? Json(new[] { obj }.ToDataSourceResult(request, ModelState))
                    : RedirectToAction(isFontEnd ? "Input" : "Index",
                        new { id = isFontEnd ? (int?)obj.Id : null, lang, mode });
            }
            #endregion

        LB_ERR:
            #region Error
            if (obj.InlineEdit)
                ModelState.TransformModelError<ProductModel>((field, dispName, modelErr) =>
                    string.Format("[{0}] {1}", dispName, modelErr.ErrorMessage));
            else
            {
                SetViewMsgInput(false);
                ViewBag.Languages = LanguageMgr.GetAll();
                ViewBag.Settings = GetSettings();
                ViewBag.Statuses = StatusMgr.GetAll(this, obj.Id);
                ViewBag.Vats = VatMgr.GetAll(this, true);
                var props = ViewBag.Properties = PropertyMgr.GetAll(this, true);
                ViewBag.Manufacturers = ManufacturerMgr.GetAll(this, obj.Id, true);
                ViewBag.Currencies = GetSettings().Hidden.SingleCurrency ? new List<CurrencyModel>() : CurrencyMgr.GetAll(this);

                if (obj.Type == ProductType.Normal)
                {
                    obj.InitVariants(this, true, true);
                    obj.Variants.Action(x => x.InitProperties(this, props, true));

                    var templVar = ViewBag.TemplVariant = new VariantModel()
                    {
                        Languages = new List<VariantLangModel>(),
                        Images = new List<VariantImageModel>()
                    };
                    templVar.InitProperties(this, props);
                }
                else
                {
                    obj.InitBundlings(this, true);
                    ViewBag.TemplBundling = new ProductBundleModel();
                }

                ViewBag.TemplAccessory = new ProductAccessoryModel();
                ViewBag.TemplSerial = new ProductSerialModel();

                obj.InitRelationIds();
                obj.InitAccessories(this, true);
                obj.InitTechSVals(this, true);
                obj.InitRecurringDiscounts(this);
                //obj.InitPrice4LoginCustGroups(this);
                //obj.InitCGroupDiscounts(this);
                //obj.InitSerialNumbers(this);
            }
            #endregion

            return obj.InlineEdit
                ? Json(new[] { obj }.ToDataSourceResult(request, ModelState))
                : ClawView(EditView, obj);
        }

        #endregion

        #region Delete

        [HttpPost]
        [AdminPerm("CatalogBits", PermissionMask.Edit)]
        public override ActionResult Delete(GridId[] ids)
        {
            var retCode = ids != null && ids.Length > 0;
            var lstOrder = new List<int>();
            bool existBundle = false;
            string nameProduct = "";

            if (retCode)
                retCode = BaseMgr<ProductModel, Domain.Product>.Action(this, false, (sess, obj) =>
                {
                    if (obj == null || obj.Id == 1 || obj.AnyBundleRef()) { existBundle = true; return false; }

                    var lst = Singleton<IOrderMgr>.Default.GetDependOrders(this, 5, obj.Id, null, (byte)OrderStatus.New, (byte)OrderStatus.Processing, (byte)OrderStatus.PayBeforeDelivery, (byte)OrderStatus.RestOrder);

                    if (lst.Count > 0) { lstOrder.AddRange(lst); nameProduct = obj.Name; return false; }
                    else
                    {
                        if (obj.ProductBundles.SafeCount() != 0)
                        {
                            foreach (var itm in obj.ProductBundles.ToList())
                            {
                                //ProductMgr.UpdateQuantity(new DefaultDb(sess), itm.Product, itm.Variant, itm.Quantity * obj.Quantity,
                                //    false, false, true, false, new ProdQtyLogModel() { Reason = (int)ProdQtyLogReason.Bundled, RefId = obj.Id });
                                obj.ProductBundles.Remove(itm);
                            }
                            sess.SaveOrUpdate(obj);
                        }

                        obj.SKU = obj.BarCode = null;
                        obj.Alias = Guid.NewGuid().ToString("N");
                        obj.Languages.Action(m => m.Alias = obj.Alias);
                        obj.Variants.Action(m => m.BarCode = m.SKU = null);

                        obj.ClearStatistics(); sess.SaveOrUpdate(obj);

                        obj.CategoryProducts.Clear();
                        obj.Accessories.Clear();
                        obj.AccessoryRefs.Clear();
                        obj.CouponProducts.Clear();
                        obj.ProductEmailMUsers.Clear();

                        Singleton<IDiscountMgr>.Default.OnProductRemoved(new DefaultDb(sess), new List<IProduct>() { obj });

                        obj.Type |= (byte)ProductType.Deleted;
                        obj.BundlingRefs.Action(m => m.Bundle.CalculateQty(sess));

                        if (Singleton<IOrderMgr>.Default.AnyOrder(new DefaultDb(sess), obj) || obj.BongerProducts.Any()) sess.Update(obj);
                        else sess.Delete(obj);

                        return true;
                    }
                }, ids.Select(x => x.Id).ToArray());

            string errMsg = existBundle ? Product.StringTable.BelongToBundleProduct : null;

            if (!retCode && lstOrder.Count != 0)
                errMsg = string.Format(Product.StringTable.ProductDeleteFailedMsg, nameProduct, lstOrder.Take(3).ToString(", ") + (lstOrder.Count > 3 ? " ..." : ""));

            if (!IndexAction) SetViewMsg(retCode, StringTable.Delete, errMsg);
            return JsonStatus(StringTable.Delete, retCode, errMsg);
        }

        #endregion

        #region Clone

        [HttpPost]
        [AdminPerm("CatalogBits", PermissionMask.Edit)]
        public ActionResult Clone(int id)
        {
            var obj = ProductMgr.Clone(this, id);
            if (obj == null) goto LB_ERR;
            var model = Mapper.Map<Domain.Product, ProductModel>(obj);

            if (model.SalePrice.IsNull()) model.SalePrice = null;
            if (model.SalePriceInc.IsNull()) model.SalePriceInc = null;
            if (model.BundleDiscountRate.IsNull()) model.BundleDiscountRate = null;

            TryValidate(model, true);

            #region RemoveInputValidate

            ModelState.RemoveKey(GetFieldName<ProductModel, object>(m => m.Opt.ManufacturerInput));
            ModelState.RemoveKey(GetFieldName<ProductModel, object>(m => m.Opt.ProductStatusInput));
            ModelState.RemoveKey(GetFieldName<ProductModel, object>(m => m.Opt.VatInput));
            ModelState.RemoveKey(GetFieldName<ProductModel, object>(m => m.Opt.PropertyNameInput));
            ModelState.RemoveKey(GetFieldName<ProductModel, object>(m => m.Opt.PropValNameInput));
            ModelState.RemoveKey(GetFieldName<ProductModel, object>(m => m.TechSpecCostExtra));
            ModelState.RemoveKey(GetFieldName<ProductModel, object>(m => m.DepositAmount));

            if (!obj.SelfDiscActive)
            {
                ModelState.RemoveKey(GetFieldName<ProductModel, object>(m => m.SelfDiscMinQty));
                ModelState.RemoveKey(GetFieldName<ProductModel, object>(m => m.SelfDiscValue));
            }

            if (!obj.PriceCkActive)
                ModelState.RemoveKey(GetFieldName<ProductModel, OMoney>(m => m.PriceCkMinVal));

            if (!obj.PriceCkActive || !obj.PriceCkVariant)
                ModelState.RemoveKey(GetFieldName<ProductModel, string>(m => m.PriceCkUrl));

            #endregion

            if (ModelState.ModLocalize().IsValid)
            {
                #region CloneFile
                if (!string.IsNullOrEmpty(ModuleUrl.AttachmentPath(obj.AttachFile)))
                {
                    string newAttach;
                    if (TryCloneFile(ModuleUrl.AttachmentPath(obj.AttachFile), out newAttach))
                        obj.AttachFile = newAttach;
                    else obj.AttachFile = null;
                }

                foreach (var image in obj.Images)
                {
                    string newImgFile;
                    if (TryCloneImage(ModuleUrl.ImagePath(image.FileName), out newImgFile,
                        ModuleUrl.ImageThumbPath, ModuleUrl.ImageThumbPath(image.FileName)))
                        image.FileName = newImgFile;
                    else image.FileName = null;
                }

                obj.Images.RemoveAll(x => x.FileName.IsNullOrEmpty());
                if (obj.Image != null && obj.Image.FileName.IsNullOrEmpty()) 
                    obj.Image = obj.Images.FirstOrDefault();

                foreach (var variant in obj.Variants)
                {
                    if (!string.IsNullOrEmpty(variant.Image))
                    {
                        string newImgFile;
                        if (TryCloneImage(ModuleUrl.ImagePath(variant.Image), out newImgFile, ModuleUrl.ImageThumbPath))
                            variant.Image = newImgFile;
                        else variant.Image = null;
                    }
                }
                #endregion

                var retCode = BaseMgr<ProductModel, Domain.Product>.SaveOrUpdate(this, obj);
                if (retCode && !obj.Type.InList(ProductType.Bundle))
                {
                    if (obj.VariantNum == 0) ProdQtyLogMgr.SaveorUpdate(this,
                        new ProdQtyLogModel() { Reason = (int)(ProdQtyLogReason.FirstCreation), Quantity = obj.Quantity, ProductId = obj.Id }, false);
                    else if (obj.Variants.SafeCount() != 0)
                        foreach (var itm in obj.Variants)
                        {
                            ProdQtyLogMgr.SaveorUpdate(this, new ProdQtyLogModel() { Reason = (int)ProdQtyLogReason.FirstCreation, Quantity = itm.Quantity, ProductId = obj.Id, VariantId = itm.Id }, false);
                        }

                    obj.Update(DataService);
                }

                if (!IndexAction) SetViewMsg(true, StringTable.Clone);
                return JsonStatus(StringTable.Clone, retCode, null, x => x.Id = obj.Id);
            }

        LB_ERR:
            ModelState.TransformModelError<ProductModel>((field, dispName, modelErr) =>
                string.Format("[{0}] {1}", dispName, modelErr.ErrorMessage));

            if (!IndexAction) SetViewMsg(false, StringTable.Clone);
            return JsonStatus(StringTable.Clone, false, ModelState.FirstErrorMsg());
        }

        #endregion

        #region UpdateQuantity

        [HttpPost]
        [AdminPerm("CatalogBits", PermissionMask.Edit)]
        public ActionResult UpdateQuantity(int id, int? variantid, int delta, int reason, OMoney costPrice, string note, int iSeq, int? qtyPos = null)
        {
            var domProd = ProductMgr.GetDom(this, id);
            var domVari = VariantMgr.GetDom(this, variantid ?? 0);

            if (delta == 0 || domProd == null || domProd.Type.InList(ProductType.Bundle)) 
                return JsonStatus(Product.StringTable.UpdateQuantity, false, Product.StringTable.UpdateQuantityInvalidData);

            var errMsg = new StringBuilder();
            var retCode = domProd != null && delta != 0 && (domVari != null || domProd.Variants.SafeCount() == 0) && typeof(ProdQtyLogReason).IsEnumDefined(reason);
            int? quantity = null, quantity1 = null, quantity2 = null, varQuantity = null, varQuantity1 = null, varQuantity2 = null, backOrderQty = null, varBackOrderQty = null;
            int? productStatusId = null, varProductStatusId = null;
            string productStatusName = null, varProductStatusName = null;
            OMoney productCostPrice = null, productCostPriceInc = null, varProductCostPrice = null, varProductCostPriceInc = null;

            if (retCode)
            {
                retCode = ProductMgr.UpdateQuantity(this, id, variantid, delta, new ProdQtyLogModel() { Reason = reason, Note = note, QtyPos = qtyPos }, true, errMsg, costPrice, (QtyPos?)qtyPos);

                if (retCode)
                {
                    domProd = ProductMgr.GetDom(this, id);
                    quantity = domProd.Quantity;
                    quantity1 = domProd.Quantity1;
                    quantity2 = domProd.Quantity2;
                    backOrderQty = domProd.BackOrderQty;
                    productStatusId = domProd.ProductStatus.Id;
                    productStatusName = domProd.ProductStatus.GetName();
                    productCostPrice = new OMoney(domProd.CostPrice);
                    productCostPriceInc = new OMoney(domProd.IncVat(domProd.CostPrice));

                    if (null != (domVari = VariantMgr.GetDom(this, variantid ?? 0)))
                    {
                        varQuantity = domVari.Quantity;
                        varQuantity1 = domVari.Quantity1;
                        varQuantity2 = domVari.Quantity2;
                        varBackOrderQty = domVari.BackOrderQty;
                        varProductStatusId = domVari.ProductStatus.Id;
                        varProductStatusName = domVari.ProductStatus.GetName();
                        varProductCostPrice = new OMoney(domVari.CostPrice);
                        varProductCostPriceInc = new OMoney(domProd.IncVat(domVari.CostPrice));
                    }

                    var settings = SettingsBiz.GetSettings();
                    if (settings.Hidden.ProductWaiting && ((domVari == null && domProd.Quantity > 0) || (domVari != null && domVari.Quantity > 0))) 
                    {
                        var model = ProductMgr.Get(this, id);
                        var session = this.GetSession();
                        var emailTempl = EmailTemplMgr.GetAll(this, (int?)EmailTemplateType.ProductWaiting).FirstOrDefault();
                        var site = DataService.GetCurrentSite();
                        var lst = BaseMgr.GetList<ProductWaiting>(this, string.Format("from {0} where Product.Id = {1} and Status = {2} {3}",
                            typeof(ProductWaiting).FullName, id, ProductWaitingStatus.Pending.Val(), domVari == null ? null : " and Variant.Id = " + domVari.Id));

                        if (model != null && emailTempl != null && lst.Count != 0)
                        {
                            var n = 0;
                            var data = new
                            {
                                shop_logo = site.SiteUrl + ModuleUrl.ImagePath(settings.General.Logo).ResolveClientUrl(),
                                shop_url = ClawContext.Current.CurrentSite.SiteUrl,
                                shop_name = settings.General.StoreName,
                                shop_email = settings.General.PublicEmail,
                                shop_phonenumber = settings.General.ContactPhone,
                                Product = model,
                                ProductName = domProd.GetName(domVari),
                                Url = site.SiteUrl + Core.ModuleUrl.GetFrontEndViewUrl("Product", model.ActiveLang.Alias, model, LanguageMgr.FocusLang),
                            };

                            lst.Action(m =>
                            {
                                if (SendEmailReplyTo(emailTempl.Subject.ReplaceEx(data), emailTempl.Body.ReplaceEx(data), emailTempl.From.IfNull(settings.General.PublicEmail), m.Email, null, emailTempl.ReplyTo.IfNull(settings.General.NotifyEmail), settings.General.StoreName, null, settings.General.StoreName))
                                {
                                    m.Status = (byte) ProductWaitingStatus.Sent;
                                    session.Update(m);
                                    if (++n % 10 == 0) session.Flush();
                                };
                            });
                            session.Flush();
                        }
                    }
                }
            }

            return JsonStatus(Product.StringTable.UpdateQuantity, retCode, errMsg.Length == 0 ? null : errMsg.ToString(), m =>
            {
                m.Quantity = quantity; m.Quantity1 = quantity1; m.Quantity2 = quantity2; m.BackOrderQty = backOrderQty;
                m.ProductStatusId = productStatusId;
                m.ProductStatusName = productStatusName;
                m.ProductCostPrice = productCostPrice == null ? null : new { productCostPrice.Value, productCostPrice.Text };
                m.ProductCostPriceInc = productCostPriceInc == null ? null : new { productCostPriceInc.Value, productCostPriceInc.Text };
                m.VarQuantity = varQuantity; m.VarQuantity1 = varQuantity1; m.VarQuantity2 = varQuantity2; m.VarBackOrderQty = varBackOrderQty;
                m.VarProductStatusId = varProductStatusId;
                m.VarProductStatusName = varProductStatusName;
                m.VarProductCostPrice = varProductCostPrice == null ? null : new { varProductCostPrice.Value, varProductCostPrice.Text };
                m.VarProductCostPriceInc = varProductCostPriceInc == null ? null : new { varProductCostPriceInc.Value, varProductCostPriceInc.Text };
            });
        }

        #endregion

        #region UpdateEmailMarketing

        [HttpPost]
        [AdminPerm("CatalogBits", PermissionMask.Edit)]
        public ActionResult UpdateEmailMarketing(int id, bool value)
        {
            return UpdateEmailMarketing(new[] { id }, value);
        }

        protected ActionResult UpdateEmailMarketing(int[] ids, bool value)
        {
            var doms = (ids ?? new int[0]).Select(x => ProductMgr.GetDom(this, x)).ToList();
            var retCode = doms.All(x => x != null);

            retCode = retCode && doms.All(x =>
            {
                var model = ProductMgr.Get(this, x.Id);

                model.EmailMarketing = value;

                model.Variants = null;
                model.Images = null;
                model.Bundlings = null;
                model.CategoryIds = null;
                model.Accessories = null;

                return ProductMgr.SaveorUpdate(this, model);
            });

            return JsonStatus(Product.StringTable.UpdateEmailMarketing, retCode, null, x => x.EmailMarketing = value);
        }

        #endregion

        #region DeleteEmailMarketing
        [AdminPerm("CatalogBits", PermissionMask.Edit)]
        public ActionResult DeleteEmailMarketing(GridId[] ids)
        {
            return UpdateEmailMarketing((ids ?? new GridId[0]).Select(x => x.Id).ToArray(), false);
        }
        #endregion

        #region ChangeDisplayOrder

        [HttpPost]
        [AdminPerm("CatalogBits", PermissionMask.Edit)]
        public ActionResult ChangeDisplayOrder(int id, int value)
        {
            return ChangeDOrder(id, value);
        }

        #endregion

        #region ChangeLocation

        [HttpPost]
        [AdminPerm("CatalogBits", PermissionMask.Edit)]
        public ActionResult ChangeLocation(int id, string value)
        {
            var obj = ProductMgr.Get(this, id);

            if (obj != null && TryValidate(obj, m => m.Location) && ProductMgr.SaveOrUpdate(this, id, d => d.Location = value))
                return JsonStatus(StringTable.UpdateLocation, true);

            return JsonStatus(StringTable.UpdateLocation, false);
        }

        #endregion

        #region SetAccessory

        [HttpPost]
        [AdminPerm("CatalogBits", PermissionMask.Edit)]
        public ActionResult SetAccessory(int id, bool value)
        {
            var retCode = BaseMgr<ProductModel, Domain.Product>.SaveOrUpdate(this, id, obj =>
                    obj.Flags = value ? (obj.Flags | (int)ProductFlags.Accessory) : (obj.Flags & ~(int)ProductFlags.Accessory));

            return JsonStatus(StringTable.SetIsAccessory, retCode);
        }

        #endregion

        #region SetVisibleWeb

        [HttpPost]
        [AdminPerm("CatalogBits", PermissionMask.Edit)]
        public ActionResult SetVisibleWeb(int id, bool value)
        {
            var retCode = ProductMgr.SaveOrUpdate(this, id, obj =>
                    obj.Visibility = value ? (byte)(obj.Visibility | (byte)VisibilityType.Web) : (byte)(obj.Visibility & ~(byte)VisibilityType.Web));

            if (retCode) ProductScope.Invalidate();

            return JsonStatus(StringTable.SetVisibleWeb, retCode);
        }

        #endregion

        #region AddImageForm

        public ActionResult AddImageForm(int id, string langCode, string thumbUrl, string fsUrl, int maxIndex)
        {
            var fileName = (thumbUrl == null ? null : Path.GetFileName(thumbUrl)); maxIndex++;

            if (string.IsNullOrEmpty(fileName) || maxIndex >= ProductModel.MaxImages || maxIndex < 0) return new EmptyResult();

            var obj = ProductMgr.Get(this, id);

            if (obj == null)
                obj = new ProductModel()
                {
                    Languages = new List<ProductLangModel>(),
                    Images = new List<ImageModel>()
                };
            else obj.InitImages();

            maxIndex = Math.Max(obj.Images.Count, maxIndex);
            if (string.IsNullOrEmpty(langCode)) langCode = ClawContext.Current.GetDefaultOrCurrentLangCode();

            ViewData["Languages"] = LanguageMgr.GetAll();
            ViewData["ImageIndex"] = maxIndex;
            ViewData["LangCode"] = langCode;

            while (maxIndex >= obj.Images.Count)
            {
                obj.Images.Add(new ImageModel()
                {
                    Languages = new List<ImageLangModel>()
                });
            }

            var img = obj.Images[maxIndex];
            img.Thumb = thumbUrl;
            img.FsUrl = fsUrl;
            img.FileName = fileName;

            return ClawView("Admin/_ProductsImage.cshtml", obj);
        }

        #endregion

        #region GetAssignForm
        public ActionResult GetAssignForm(int id, string categoryIds)
        {
            var obj = ProductMgr.Get(this, id);

            if (obj == null) obj = new ProductModel();
            if (categoryIds == null) obj.InitRelationIds();
            else obj.InitRelationIds(this, categoryIds);

            return ClawView("Admin/_ProductsRelation.cshtml", obj);
        }
        #endregion

        #region ListCate
        public ActionResult ListCate(int? id)
        {
            var lst = CategoryMgr.GetAll(this, 0, id ?? -1).Select(x => new
            {
                Id = x.Id,
                Name = x.Name,
                Deep = x.Deep(),
                Active = true,
                ParentId = x.ParentId,
                AnyChild = x.AnyChild,
                HierOrder = x.GetHierOrderTxt(),
                LangVisible = x.LangVisible,
                AncestorActive = true,
                AncestorLangVisible = x.AncestorLangVisible(),
            });

            return Json(lst, JsonRequestBehavior.AllowGet);
        }
        #endregion

        #region ListLocation

        public ActionResult ListLocation()
        {
            var lst = LocationMgr.GetAll(this);
            return Json(lst, JsonRequestBehavior.AllowGet);
        }

        #endregion

        #region ListSupplier

        public ActionResult ListSupplier()
        {
            var lst = SupplierMgr.GetAll(this);
            return Json(lst, JsonRequestBehavior.AllowGet);
        }

        #endregion

        #region ListSerial

        public ActionResult ListSerial(int id)
        {
            var obj = ProductMgr.GetDom(this, id);

            var lst = obj == null
                ? null
                : obj.ProductSerials.Select(m => new DDListItem() {Text = m.SerialNumber, Value = m.SerialNumber, Active = true, LangVisible = true });

            return Json(lst, JsonRequestBehavior.AllowGet);
        }

        #endregion

        #region GenerateBarCode

        [AdminPerm("CatalogBits", PermissionMask.View)]
        public ActionResult GenerateBarCode(int id, int? variantId)
        {
            var ret = ProductMgr.GenerateBarCode(this, id, variantId);
            var img = GenerateBarCode(ret);
            var s = new MemoryStream();

            img.Save(s, ImageFormat.Bmp);
            s.Seek(0, SeekOrigin.Begin);
            ret = Spire.Barcode.BarcodeScanner.ScanOne(s, true);

            s.Close(); img.Dispose();

            return Json(new { BarCode = ret }, JsonRequestBehavior.AllowGet);
        }

        #endregion

        #region ViewBarCode

        [AdminPerm("CatalogBits", PermissionMask.View)]
        public ActionResult ViewBarCode(int id, int? variantId, string barcode, bool pdf = false, int? marginTop = null, string pageSizeName = null)
        {
            const decimal m2ToPx = 2.8346456692913385826771653543307m;

            var settings = GetSettings();
            var w = (int)(settings.Display.BarCode.Image.Width * m2ToPx);
            var h = (int)(settings.Display.BarCode.Image.Height * m2ToPx);

            if (pdf)
            {
                var html = this.RenderToString("ViewBarCode", new { id, variantId, barcode, pdf = false, marginTop, pageSizeName }, false, "Product");

                if (html.IsNullOrEmpty()) return HttpNotFound();

                var doc = PrintToPdf(html, 0, 0, false, pageSizeName ?? string.Format("0 0 {0} {1}", w, h), true);

                return File(doc.GetStream(), "application/pdf", string.Format("Barcode{0}.pdf", variantId == null ? id.ToString() : id + "-" + variantId));
            }
            else
            {
                var product = ProductMgr.GetDom(this, id);
                var variant = VariantMgr.GetDom(this, variantId ?? 0);

                if (product == null && variant == null) return Content("");

                if (barcode.IsNullOrEmpty())
                    barcode = variantId == null ? product.SafeProp(m => m.BarCode) : variant.SafeProp(m => m.BarCode);

                System.Drawing.Image img = null;

                marginTop = marginTop ?? Math.Abs(settings.Hidden.BarCodeMarginTop * settings.Display.BarCode.DpiY) / 300;

                if (!barcode.IsNullOrEmpty())
                {
                    var mB = (int)(settings.Display.BarCode.BottomMargin * m2ToPx);
                    var txtH = (5 * settings.Display.BarCode.FontSize) / 2;
                    var barH = Math.Min((int)(settings.Display.BarCode.BarHeight * m2ToPx), h - txtH);
                    img = GenerateBarCode(barcode, width: w, height: barH, barHeight: barH, showText: false, bottomMargin: 0, topMargin: 0, leftMargin: 2, rightMargin: 1);
                    img = CropImage(img, 0, Math.Max(marginTop ?? 0, img.Height - ((barH - mB) * img.Width) / w), img.Width, ((barH - mB) * img.Width) / w);
                }

                ViewBag.Settings = settings;
                ViewBag.Width = w;
                ViewBag.Height = h;

                return ClawView("Admin/_ProductsBarCode.cshtml", new System.Tuple<System.Drawing.Image, Domain.ProductBase, Variant>(img, product ?? variant.SafeProp(m => m.Product), variant));
            }
        }

        #endregion

        #region ViewQrCode

        [AdminPerm("CatalogBits", PermissionMask.View)]
        public ActionResult ViewQrCode(int id, int? variantId, string barcode)
        {
            const decimal m2ToPx = 2.8346456692913385826771653543307m;

            if (barcode.IsNullOrEmpty())
            {
                var obj = ProductMgr.GetDom(this, id);
                if (obj != null) barcode = obj.BarCode;
            }

            var settings = GetSettings();
            System.Drawing.Image img = null;
            Domain.ProductBase product = ProductMgr.GetDom(this, id);
            Domain.Variant variant = VariantMgr.GetDom(this, variantId ?? 0);

            if (!barcode.IsNullOrEmpty())
            {
                var url = Request.Url;
                var targetpath = ModuleUrl.FrontUrl("QrCode", id, "Id");
                img = GenerateQrCode(url.Scheme + "://" + url.Authority + targetpath);
            }

            ViewBag.Settings = settings;
            ViewBag.Width = (int)(settings.Display.BarCode.Image.Width * m2ToPx);
            ViewBag.Height = (int)(settings.Display.BarCode.Image.Height * m2ToPx);

            return ClawView("Admin/_ProductsBarCode.cshtml", new System.Tuple<System.Drawing.Image, Domain.ProductBase, Variant>(img, product ?? variant.SafeProp(m => m.Product), variant));
        }

        #endregion

        #region ValidateSku

        public virtual ActionResult ValidateSku(string sku, string[] otherSkus, int? id)
        {
            if (otherSkus != null && !sku.IsNullOrEmpty() && otherSkus.Any(x => x.EqualsIgCase(sku)))
            {
                return Json(false, JsonRequestBehavior.AllowGet);
            }
            return ValidateField("SKU", sku, id);
        }

        #endregion

        #region ValidateBarcode

        public virtual ActionResult ValidateBarcode(string barcode, string[] otherBarcodes, int? id)
        {
            if (otherBarcodes != null && !barcode.IsNullOrEmpty() && otherBarcodes.Any(x => x.EqualsIgCase(barcode)))
            {
                return Json(false, JsonRequestBehavior.AllowGet);
            }
            return ValidateField("BarCode", barcode, id);
        }

        #endregion

        #region ImportSerialNumbers

        public ActionResult ImportSerialNumbers(int id, HttpPostedFileBase serialFile)
        {
            if (id == 0 || serialFile == null) goto LB_ERR;

            var obj = ProductMgr.GetDom(this, id);

            if (obj == null) goto LB_ERR;

            var sr = new StreamReader(serialFile.InputStream);

            var data = sr.ReadToEnd()
                .Split(new[] {"\r\n"}, StringSplitOptions.None)
                .Select(m => m.Trim()).Where(m => !m.IsNullOrEmpty()).ToList(); 

            sr.Close();

            if (data.Count != 0) {
                data.RemoveAt(0); //header

                data = data
                    .Select(m => new {Key = m.ToLower(), Value = m})
                    .Distinct(m => m.Key).Select(m => m.Value).ToList();
            }

            var lst = data
                .Where(m => obj.ProductSerials.All(z => !z.SerialNumber.EqualsIgCase(m)))
                .Select(m => new ProductSerial() {Product = obj, SerialNumber = m}).ToList();

            obj.ProductSerials.AddRange(lst);

            var ret = obj.Update(DataService);

            return JsonStatus(StringTable.Import, ret, ret ? StringTable.ImportSerialNumberSucceeded.Replace("{0}", lst.Count.ToString()) : null, x => x.Items = lst.Select(m => new { m.Id, Value = m.SerialNumber }));

        LB_ERR:
            return HttpNotFound();
        }

        #endregion

    }

    #region BundleController
    [ClawAuthorize(AdminPermissionType.Eshop, null)]
    public class BundleController : BaseController
    {
        public BundleController(IDataService ds) : base(ds) { }

        [ValidateInput(false)]
        public ActionResult Input(int id, string lang, string optlang, string mode)
        {
            return RedirectToAction("Input", "BundleProduct",
                new { id = 0, lang = lang, optlang = optlang, mode = mode, type = (int)ProductType.Bundle });
        }

    }
    #endregion

    #region PriceCheckProductController

    [ClawAuthorize(AdminPermissionType.Eshop, null)]
    public class PriceCheckProductController : ProductController
    {
        public PriceCheckProductController(IDataService ds) : base(ds) { }

        #region Delete

        [HttpPost]
        [AdminPerm("CatalogBits", PermissionMask.Edit)]
        public override ActionResult Delete(GridId[] ids)
        {
            var retCode = ids != null && ids.Length > 0;

            if (retCode)
            {
                var list = ids.Select(x => ProductMgr.GetDom(this, x.Id))
                    .Where(x => x != null)
                    .Action(x => x.PriceCkActive = false);

                try
                {
                    list.Update(DataService);
                    ProductScope.Invalidate();
                }
                catch (Exception)
                {
                    retCode = false;
                }
            }

            return JsonStatus(StringTable.Delete, retCode);
        }

        #endregion

    }

    #endregion

    #region FeaturedProductController

    [ClawAuthorize(AdminPermissionType.Eshop, null)]
    public class FeaturedProductController : ProductController
    {
        public FeaturedProductController(IDataService ds) : base(ds) { }

        #region Delete

        [HttpPost]
        [AdminPerm("CatalogBits", PermissionMask.Edit)]
        public override ActionResult Delete(GridId[] ids)
        {
            return SetValue((ids ?? new GridId[0]).Select(x => x.Id).ToArray(), false);
        }

        #endregion

        #region SetValue

        [HttpPost]
        [AdminPerm("CatalogBits", PermissionMask.Edit)]
        public ActionResult SetValue(int[] ids, bool value)
        {
            var retCode = ids != null && ids.Length > 0;

            if (retCode)
            {
                var list = ids.Select(x => ProductMgr.GetDom(this, x))
                    .Where(x => x != null)
                    .Action(x =>
                    {
                        if (value) x.Flags |= (int)ProductFlags.Featured;
                        else x.Flags = x.Flags & (~(int)ProductFlags.Featured);
                    });

                try
                {
                    list.Update(DataService);
                    ProductScope.Invalidate();
                }
                catch (Exception)
                {
                    retCode = false;
                }
            }

            if (!IndexAction) SetViewMsg(retCode, value ? StringTable.AddNew : StringTable.Delete);
            return JsonStatus(value ? StringTable.AddNew : StringTable.Delete, retCode);
        }

        #endregion
    }

    #endregion

    #region BackorderProductController

    [ClawAuthorize(AdminPermissionType.Eshop, null)]
    public class BackorderProductController : ProductController
    {
        public BackorderProductController(IDataService ds) : base(ds) { }
    }

    #endregion

    #region EmailMarketProductController

    [ClawAuthorize(AdminPermissionType.Eshop, null)]
    public class EmailMarketProductController : ProductController
    {
        public EmailMarketProductController(IDataService ds) : base(ds) { }
    }

    #endregion

    #region BundleProductController

    [ClawAuthorize(AdminPermissionType.Eshop, null)]
    public class BundleProductController : ProductController
    {
        public BundleProductController(IDataService ds) : base(ds) { }
    }

    #endregion

    #region PrisguideProductController

    [ClawAuthorize(AdminPermissionType.Eshop, null)]
    public class PrisguideProductController : ProductController
    {
        public PrisguideProductController(IDataService ds) : base(ds) { }

        #region Delete

        [HttpPost]
        [AdminPerm("CatalogBits", PermissionMask.Edit)]
        public override ActionResult Delete(GridId[] ids)
        {
            return SetValue((ids ?? new GridId[0]).Select(x => x.Id).ToArray(), false);
        }

        #endregion

        #region SetValue

        [HttpPost]
        [AdminPerm("CatalogBits", PermissionMask.Edit)]
        public ActionResult SetValue(int[] ids, bool value)
        {
            var retCode = ids != null && ids.Length > 0;

            if (retCode)
            {
                var list = ids.Select(x => ProductMgr.GetDom(this, x))
                    .Where(x => x != null)
                    .Action(x =>
                    {
                        if (value) x.Flags |= (int)ProductFlags.Prisguide;
                        else x.Flags = x.Flags & (~(int)ProductFlags.Prisguide);
                    });

                try
                {
                    list.Update(DataService);
                    ProductScope.Invalidate();
                }
                catch (Exception)
                {
                    retCode = false;
                }
            }

            if (!IndexAction) SetViewMsg(retCode, value ? StringTable.AddNew : StringTable.Delete);
            return JsonStatus(value ? StringTable.AddNew : StringTable.Delete, retCode);
        }

        #endregion

        #region Save PrisguideExportAll setting

        [HttpPost]
        public ActionResult Setting(bool? exportAll, string statusIds)
        {
            try
            {
                var settings = SettingsBiz.GetSettings(true);
                settings.Hidden.PrisguideExportAll = exportAll.HasValue ? exportAll.Value : false;
                settings.Hidden.PrisguideExportStatusIds = statusIds;
                SettingMgr.Save(settings);

                return Json(new { error = 0 });
            }
            catch (Exception exp)
            {
                LogHelper.LogText(exp);

                return Json(new { error = 1 });
            }
        }

        #endregion
    }

    #endregion

    #region PrisjaktProductController

    [ClawAuthorize(AdminPermissionType.Eshop, null)]
    public class PrisjaktProductController : ProductController
    {
        public PrisjaktProductController(IDataService ds) : base(ds) { }

        #region Delete

        [HttpPost]
        [AdminPerm("CatalogBits", PermissionMask.Edit)]
        public override ActionResult Delete(GridId[] ids)
        {
            return SetValue((ids ?? new GridId[0]).Select(x => x.Id).ToArray(), false);
        }

        #endregion

        #region SetValue

        [HttpPost]
        [AdminPerm("CatalogBits", PermissionMask.Edit)]
        public ActionResult SetValue(int[] ids, bool value)
        {
            var retCode = ids != null && ids.Length > 0;

            if (retCode)
            {
                var list = ids.Select(x => ProductMgr.GetDom(this, x))
                    .Where(x => x != null)
                    .Action(x =>
                    {
                        if (value) x.Flags |= (int)ProductFlags.Prisjakt;
                        else x.Flags = x.Flags & (~(int)ProductFlags.Prisjakt);
                    });

                try
                {
                    list.Update(DataService);
                    ProductScope.Invalidate();
                }
                catch (Exception)
                {
                    retCode = false;
                }
            }

            if (!IndexAction) SetViewMsg(retCode, value ? StringTable.AddNew : StringTable.Delete);
            return JsonStatus(value ? StringTable.AddNew : StringTable.Delete, retCode);
        }

        #endregion

        #region Save prisjaktExportAll setting

        [HttpPost]
        public ActionResult Setting(bool? exportAll, string statusIds)
        {
            try
            {
                var settings = SettingsBiz.GetSettings(true);
                settings.Hidden.PrisjaktExportAll = exportAll.HasValue ? exportAll.Value : false;
                settings.Hidden.PrisjaktExportStatusIds = statusIds;
                SettingMgr.Save(settings);

                return Json(new { error = 0 });
            }
            catch (Exception exp)
            {
                LogHelper.LogText(exp);

                return Json(new { error = 1 });
            }
        }

        #endregion
    }

    #endregion

    #region RecurringProductController

    [ClawAuthorize(AdminPermissionType.Eshop, null)]
    public class RecurringProductController : ProductController
    {
        public RecurringProductController(IDataService ds) : base(ds) { }

        #region Delete

        [HttpPost]
        [AdminPerm("CatalogBits", PermissionMask.Edit)]
        public override ActionResult Delete(GridId[] ids)
        {
            return SetValue((ids ?? new GridId[0]).Select(x => x.Id).ToArray(), false);
        }

        #endregion

        #region SetValue

        [HttpPost]
        [AdminPerm("CatalogBits", PermissionMask.Edit)]
        public ActionResult SetValue(int[] ids, bool value)
        {
            var retCode = ids != null && ids.Length > 0;

            if (retCode)
            {
                var list = ids.Select(x => ProductMgr.GetDom(this, x))
                    .Where(x => x != null)
                    .Action(x =>
                    {
                        if (value) x.Flags |= (int)ProductFlags.RecurringOrder;
                        else x.Flags = x.Flags & (~(int)ProductFlags.RecurringOrder);
                    });

                try
                {
                    list.Update(DataService);
                    ProductScope.Invalidate();
                }
                catch (Exception)
                {
                    retCode = false;
                }
            }

            if (!IndexAction) SetViewMsg(retCode, value ? StringTable.AddNew : StringTable.Delete);
            return JsonStatus(value ? StringTable.AddNew : StringTable.Delete, retCode);
        }

        #endregion
    }

    #endregion

    #region NotifyMeProductController

    [ClawAuthorize(AdminPermissionType.Eshop, null)]
    public class NotifyMeProductController : ProductController
    {
        public NotifyMeProductController(IDataService ds) : base(ds) { }
    }

    #endregion
}



