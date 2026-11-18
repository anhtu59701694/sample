using AutoMapper;
using Claw.Controls.Attributes;
using Claw.Controls.Modules;
using Claw.Core.Enumerations;
using Claw.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Claw.Core;
using Claw.Core.Configurations;
using Claw.Core.Constants;
using Claw.Core.DAL;
using Claw.Core.Domain;
using Claw.Core.Extensions;
using Claw.Core.Utility;
using Claw.Modules.Eshop.DAL;
using Claw.Modules.Eshop.Models;
using Component = Claw.Core.Domain.Component;
using Claw.Modules.Eshop.Domain;
using Claw.Core.Logs;
using System.IO;
using System.Xml.Serialization;
using System.Xml;
using System.Text;
using Claw.Modules.Eshop.Helper;
using System.Text.RegularExpressions;
using System.Globalization;

namespace Claw.Modules.Eshop.Controllers.Admin
{
    [AdminPerm]
    public class SettingController : BaseController
    {
        private const string SettingsView = "Admin/SettingsIndex.cshtml";

        #region SettingController
        public SettingController(IDataService ds) :
            base(ds)
        {
            InitImageSettings(c => c.Display.Product.LogoSite, c => c.Display.Product.LogoSite);
        }

        static SettingController()
        {
            Mapper.CreateMap<Models.AdminModel.BringShippingModel, SettingsBiz.BringShippingBiz>();
            Mapper.CreateMap<SettingsBiz.BringShippingBiz, Models.AdminModel.BringShippingModel>();

            Mapper.CreateMap<Models.SizeModel, SettingsBiz.SizeBiz>();
            Mapper.CreateMap<SettingsBiz.SizeBiz, Models.SizeModel>();


            Mapper.CreateMap<Models.AdminModel, SettingsBiz.AdminBiz>();
            Mapper.CreateMap<SettingsBiz.AdminBiz, Models.AdminModel>();

            Mapper.CreateMap<Models.CategorySModel, SettingsBiz.CategoryBiz>();
            Mapper.CreateMap<SettingsBiz.CategoryBiz, Models.CategorySModel>();

            Mapper.CreateMap<Models.ProductSModel, SettingsBiz.ProductBiz>();
            Mapper.CreateMap<SettingsBiz.ProductBiz, Models.ProductSModel>();

            Mapper.CreateMap<Models.ProductSModel, SettingsBiz.ProductBiz>();
            Mapper.CreateMap<SettingsBiz.ProductBiz, Models.ProductSModel>();

            Mapper.CreateMap<Models.AdminModel.ImageModel, SettingsBiz.AdminBiz.ImageBiz>();
            Mapper.CreateMap<SettingsBiz.AdminBiz.ImageBiz, Models.AdminModel.ImageModel>();

            Mapper.CreateMap<Models.AdminModel.MsTranslatorModel, SettingsBiz.MsTranslatorBiz>();
            Mapper.CreateMap<SettingsBiz.MsTranslatorBiz, Models.AdminModel.MsTranslatorModel>();

            Mapper.CreateMap<Models.AdminModel.PowerOfficeModel, SettingsBiz.PowerOfficeBiz>();
            Mapper.CreateMap<SettingsBiz.PowerOfficeBiz, Models.AdminModel.PowerOfficeModel>();

            Mapper.CreateMap<Models.AdminModel.SocialModel, SettingsBiz.SocialBiz>();
            Mapper.CreateMap<SettingsBiz.SocialBiz, Models.AdminModel.SocialModel>();

            Mapper.CreateMap<Models.GeneralModel, SettingsBiz.GeneralBiz>();
            Mapper.CreateMap<SettingsBiz.GeneralBiz, Models.GeneralModel>();

            Mapper.CreateMap<Models.DisplayModel, SettingsBiz.DisplayBiz>();
            Mapper.CreateMap<SettingsBiz.DisplayBiz, Models.DisplayModel>();

            Mapper.CreateMap<Models.OthersModel, SettingsBiz.OthersBiz>();
            Mapper.CreateMap<SettingsBiz.OthersBiz, Models.OthersModel>();

            Mapper.CreateMap<Models.BarCodeModel, SettingsBiz.BarCodeBiz>();
            Mapper.CreateMap<SettingsBiz.BarCodeBiz, Models.BarCodeModel>();

            Mapper.CreateMap<Models.QrCodeModel, SettingsBiz.QrCodeBiz>();
            Mapper.CreateMap<SettingsBiz.QrCodeBiz, Models.QrCodeModel>();

            Mapper.CreateMap<Models.EcommerceTrackingModel, SettingsBiz.EcommerceTrackingBiz>();
            Mapper.CreateMap<SettingsBiz.EcommerceTrackingBiz, Models.EcommerceTrackingModel>();

            Mapper.CreateMap<Models.GoogleAnalytics4Model, SettingsBiz.GoogleAnalytics4Biz>();
            Mapper.CreateMap<SettingsBiz.GoogleAnalytics4Biz, Models.GoogleAnalytics4Model>();

            Mapper.CreateMap<Models.AdminModel.LogistraModel, SettingsBiz.LogistraBiz>();
            Mapper.CreateMap<SettingsBiz.LogistraBiz, Models.AdminModel.LogistraModel>();

            Mapper.CreateMap<Models.AdminModel.ProteriaModel, SettingsBiz.ProteriaBiz>();
            Mapper.CreateMap<SettingsBiz.ProteriaBiz, Models.AdminModel.ProteriaModel>();

            Mapper.CreateMap<Models.AdminModel.PostnordModel, SettingsBiz.PostnordBiz>();
            Mapper.CreateMap<SettingsBiz.PostnordBiz, Models.AdminModel.PostnordModel>();

            Mapper.CreateMap<Models.AdminModel.ShippingModel, SettingsBiz.ShippingBiz>();
            Mapper.CreateMap<SettingsBiz.ShippingBiz, Models.AdminModel.ShippingModel>();

            Mapper.CreateMap<SettingsBiz, SettingModel>();
            Mapper.CreateMap<SettingModel, SettingsBiz>();

            Mapper.CreateMap<SettingsBiz.AccountingBiz, AccountingModel>();
            Mapper.CreateMap<AccountingModel, SettingsBiz.AccountingBiz>();

            Mapper.CreateMap<SettingsBiz.UniMicroBiz, UniMicroModel>();
            Mapper.CreateMap<UniMicroModel, SettingsBiz.UniMicroBiz>();
        }
        #endregion

        #region ViewSetting
        private ActionResult ViewSetting()
        {
            var settings = SettingsBiz.GetSettings(true);

            var model = Mapper.Map<SettingsBiz, SettingModel>(settings);
            model.General.ImageFsUrl = model.General.Logo;

            return ClawView(SettingsView, model);
        }
        #endregion

        #region Index

        [ValidateInput(false)]
        public ActionResult Index()
        {
            return RedirectToAction("General");
        }

        #endregion

        #region General

        [ValidateInput(false)]
        public ActionResult General()
        {
            ViewBag.TabSelect = "0";

            return ViewSetting();
        }

        [HttpPost]
        [ValidateInput(false)]
        public ActionResult General(GeneralModel model)
        {
            var generalBiz = Mapper.Map<GeneralModel, SettingsBiz.GeneralBiz>(model);
            var dbValues = SettingsBiz.GetSettings(true);
            var settings = new SettingsBiz()
            {
                General = generalBiz,
                Display = dbValues.Display,
                Hidden = dbValues.Hidden,
            };

            if (ModelState.ModLocalize().IsValid)
            {
                #region MoveFile
                try
                {
                    var tmpPath = ModuleUrl.TempFolder.ResolveClientUrl();
                    if (model.ImageFsUrl != null && model.ImageFsUrl.StartsWith(tmpPath))
                    {
                        model.Logo = ModuleUrl.ImageThumbPath(MoveFile(ModuleUrl.ImageFolder, ModuleUrl.TempFolder, model.Logo)).TrimStart('~');
                        //model.ImageFsUrl = ModuleUrl.ImagePath(MoveFile(ModuleUrl.ImageFolder, ModuleUrl.TempFolder, model.Logo)).TrimStart('~');
                        settings.General.Logo = model.Logo;
                    }
                }
                catch (Exception er)
                {
                    LogErr(er);
                }
                #endregion

                #region DownloadImage
                if (model.Logo != null && Regex.IsMatch(model.Logo, @"^http(s)?://"))
                {
                    var url = model.Logo;
                    if (SaveUrl("logo_" + Regex.Replace(model.StoreName ?? "", "[^a-zA-Z0-9]+", ""), ref url, ModuleUrl.ImageFolder, ModuleUrl.ThumbFolder))
                    {
                        model.Logo = ModuleUrl.ImageThumbPath(url).TrimStart('~');
                        settings.General.Logo = model.Logo;
                    }
                    else
                    {
                        ModelState.AddModelError(m => m.Logo, model, StringTable.ErrMsgSaveImageUrl(model.Logo));
                        //goto LB_ERR;
                    }
                }
                #endregion

                var retCode = SettingMgr.Save(settings);
                settings = SettingsBiz.GetSettings(true);

                SetViewMsg(retCode);
            }
            else SetViewMsg(false, StringTable.Save);

            ViewBag.TabSelect = "0";

            var modelView = Mapper.Map<SettingsBiz, SettingModel>(settings);
            return ClawView(SettingsView, modelView);
        }

        #endregion

        #region Display

        [ValidateInput(false)]
        public ActionResult Display()
        {
            ViewBag.TabSelect = "1";
            return ViewSetting();
        }

        [HttpPost]
        [ValidateInput(false)]
        public ActionResult Display(DisplayModel model)
        {
            var displayBiz = Mapper.Map<DisplayModel, SettingsBiz.DisplayBiz>(model);
            var dbValues = SettingsBiz.GetSettings(true);
            var settings = new SettingsBiz()
            {
                Display = displayBiz,
                General = dbValues.General,
                Hidden = dbValues.Hidden,
            };

            settings.Display.Admin.BringShipping = dbValues.Display.Admin.BringShipping;
            settings.Display.Admin.Logistra = dbValues.Display.Admin.Logistra;

            if (ModelState.ModLocalize().IsValid)
            {
                var retCode = SettingMgr.Save(settings);
                settings = SettingsBiz.GetSettings(true);

                SetViewMsg(retCode);
            }
            else SetViewMsg(false, StringTable.Save);

            ViewBag.TabSelect = "1";

            var modelView = Mapper.Map<SettingsBiz, SettingModel>(settings);
            return ClawView(SettingsView, modelView);
        }

        #endregion

        #region Logistics

        [ValidateInput(false)]
        public ActionResult Logistics()
        {
            var settings = SettingsBiz.GetSettings(true);

            return ClawView("Admin/SettingsLogistics.cshtml", new LogisticsModel()
            {
                Shipping = Mapper.Map<SettingsBiz.ShippingBiz, AdminModel.ShippingModel>(settings.Display.Admin.Shipping),
                Logistra = Mapper.Map<SettingsBiz.LogistraBiz, AdminModel.LogistraModel>(settings.Display.Admin.Logistra),
                BringShipping = Mapper.Map<SettingsBiz.BringShippingBiz, AdminModel.BringShippingModel>(settings.Display.Admin.BringShipping),
                Proteria = Mapper.Map<SettingsBiz.ProteriaBiz, AdminModel.ProteriaModel>(settings.Display.Admin.Proteria),
                Postnord = Mapper.Map<SettingsBiz.PostnordBiz, AdminModel.PostnordModel>(settings.Display.Admin.Postnord),
            });
        }

        [HttpPost]
        [ValidateInput(false)]
        public ActionResult Logistics(LogisticsModel model)
        {
            var shippingBiz = Mapper.Map<AdminModel.ShippingModel, SettingsBiz.ShippingBiz>(model.Shipping);
            var logistraBiz = Mapper.Map<AdminModel.LogistraModel, SettingsBiz.LogistraBiz>(model.Logistra);
            var bringShippingBiz = Mapper.Map<AdminModel.BringShippingModel, SettingsBiz.BringShippingBiz>(model.BringShipping);
            var proteriaBiz = Mapper.Map<AdminModel.ProteriaModel, SettingsBiz.ProteriaBiz>(model.Proteria);
            var postnordBiz = Mapper.Map<AdminModel.PostnordModel, SettingsBiz.PostnordBiz>(model.Postnord);
            var dbValues = SettingsBiz.GetSettings(true);
            var settings = new SettingsBiz()
            {
                Display = dbValues.Display,
                General = dbValues.General,
                Hidden = dbValues.Hidden,
            };

            settings.Display.Admin.Shipping = shippingBiz;
            settings.Display.Admin.Logistra = logistraBiz;
            settings.Display.Admin.BringShipping = bringShippingBiz;
            settings.Display.Admin.Proteria = proteriaBiz;
            settings.Display.Admin.Postnord = postnordBiz;

            if (ModelState.ModLocalize().IsValid)
            {
                var retCode = SettingMgr.Save(settings);
                settings = SettingsBiz.GetSettings(true);

                SetViewMsg(retCode);
            }
            else SetViewMsg(false, StringTable.Save);

            return ClawView("Admin/SettingsLogistics.cshtml", new LogisticsModel()
            {
                Shipping = Mapper.Map<SettingsBiz.ShippingBiz, AdminModel.ShippingModel>(settings.Display.Admin.Shipping),
                Logistra = Mapper.Map<SettingsBiz.LogistraBiz, AdminModel.LogistraModel>(settings.Display.Admin.Logistra),
                BringShipping = Mapper.Map<SettingsBiz.BringShippingBiz, AdminModel.BringShippingModel>(settings.Display.Admin.BringShipping),
                Proteria = Mapper.Map<SettingsBiz.ProteriaBiz, AdminModel.ProteriaModel>(settings.Display.Admin.Proteria),
                Postnord = Mapper.Map<SettingsBiz.PostnordBiz, AdminModel.PostnordModel>(settings.Display.Admin.Postnord),
            });
        }

        #endregion

        #region Hidden

        [ValidateInput(false)]
        public ActionResult Hidden()
        {
            var settings = SettingsBiz.GetSettings(true);

            return ClawView("Admin/SettingsHidden.cshtml", settings.Hidden);
        }

        [HttpPost]
        [ValidateInput(false)]
        public ActionResult Hidden(SettingsBiz.HiddenBiz model)
        {
            if (ModelState.ModLocalize().IsValid)
            {
                var settings = SettingsBiz.GetSettings(true);

                foreach (var itm in settings.Hidden.GetType().GetProperties())
                {
                    itm.SetValue(settings.Hidden, itm.GetValue(model));
                }

                var retCode = SettingMgr.Save(settings);

                SetViewMsg(retCode);
            }
            else SetViewMsg(false, StringTable.Save);

            return ClawView("Admin/SettingsHidden.cshtml", SettingsBiz.GetSettings(true).Hidden);
        }

        #endregion

        #region HiddenOther

        [ValidateInput(false)]
        public ActionResult HiddenOther(string prefix)
        {
            var settings = SettingsBiz.GetSettings(true);

            var cmp = StringComparison.OrdinalIgnoreCase;
            var total = 0;
            var dicVal = settings.ToDictionary(true, false);
            var lst = DataService.GetList<Domain.Setting>(0, 1, int.MaxValue, out total)
                .Where(m => !dicVal.ContainsKey(m.Name) && !m.Name.StartsWith("Saft.", cmp) && !m.Name.StartsWith("Pos.", cmp) && !m.Name.StartsWith("Hidden.", cmp))
                .ToList();

            var prefixes = lst.Where(m => m.Name.Split('.').Length > 1)
                .Select(m => m.Name.Substring(0, m.Name.LastIndexOf('.')))
                .Distinct().ToList();

            ViewBag.Prefix = prefix;
            ViewBag.Prefixes = prefixes;

            return ClawView("Admin/SettingsHiddenOther.cshtml", lst);
        }

        [HttpPost]
        [ValidateInput(false)]
        public ActionResult HiddenOther(FormCollection form, string prefix)
        {
            var settings = SettingsBiz.GetSettings(true);

            var cmp = StringComparison.OrdinalIgnoreCase;
            var total = 0;
            var dicVal = settings.ToDictionary(true, false);
            var lst = DataService.GetList<Domain.Setting>(0, 1, int.MaxValue, out total)
                .Where(m => !dicVal.ContainsKey(m.Name) && !m.Name.StartsWith("Saft.", cmp) && !m.Name.StartsWith("Pos.", cmp) && !m.Name.StartsWith("Hidden.", cmp)).ToList();

            if (form == null || form.Count == 0) SetViewMsg(false);
            else
            {
                foreach (var itm in lst)
                {
                    var modItm = form[itm.Name];
                    if (modItm != null && itm.Value != (modItm ?? ""))
                    {
                        itm.Value = modItm ?? "";
                        itm.Update(DataService);
                    }
                }
            }

            SetViewMsg(true);

            return HiddenOther(prefix);
        }

        #endregion

        #region Accounting

        [ValidateInput(false)]
        public ActionResult Accounting()
        {
            ViewBag.TabSelect = "2";
            return ViewSetting();
        }

        [HttpPost]
        [ValidateInput(false)]
        public ActionResult Accounting(AccountingModel model)
        {
            var accountingBiz = Mapper.Map<AccountingModel, SettingsBiz.AccountingBiz>(model);
            var dbValues = SettingsBiz.GetSettings(true);

            var settings = new SettingsBiz()
            {
                Accounting = accountingBiz,
                Display = dbValues.Display,
                General = dbValues.General,
                Hidden = dbValues.Hidden,
            };

            if (ModelState.ModLocalize().IsValid)
            {
                var retCode = SettingMgr.Save(settings);
                settings = SettingsBiz.GetSettings(true);

                SetViewMsg(retCode);
            }
            else SetViewMsg(false, StringTable.Save);

            ViewBag.TabSelect = "2";

            var modelView = Mapper.Map<SettingsBiz, SettingModel>(settings);
            return ClawView(SettingsView, modelView);
        }

        [HttpPost]
        public ActionResult CertUpload()
        {
            if (Request.Files.Count > 0)
            {
                try
                {
                    var file = Request.Files[0];
                    var filePath = string.Format("~/Config/UniMicro/{0}-{1}{2}", Path.GetFileNameWithoutExtension(file.FileName), DateTime.Now.ToString("dd-MM-yyyy", CultureInfo.InvariantCulture), Path.GetExtension(file.FileName));
                    var path = Globals.MapPath(filePath);


                    file.SaveAs(path);
                    return Json(new { error = 0, filePath, msg = StringTable.UploadSuccess });
                }
                catch (Exception exp)
                {
                    LogHelper.LogText(exp);
                    return Json(new { error = 1, msg = exp.ToString() });
                }
            }

            return Json(new { error = 1, msg = "Missing upload file" });
        }

        #endregion
    }
}