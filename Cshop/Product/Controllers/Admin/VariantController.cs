using System.ComponentModel.DataAnnotations;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using Claw.Controls.Attributes;
using Claw.Controls.Modules;
using Claw.Core;
using Claw.Core.Enumerations;
using Claw.Core.Extensions;
using Claw.Core.Interfaces;
using Claw.Core.Logs;
using Claw.Core.Utility;
using System;
using System.Collections.Generic;
using System.Web.UI.WebControls;
using System.Linq;
using System.Web;
using Kendo.Mvc.Extensions;
using Kendo.Mvc.UI;
using System.Web.Mvc;
using Claw.Modules.Eshop.DAL;
using Claw.Modules.Eshop.Domain;
using Claw.Modules.Eshop.Models;

namespace Claw.Modules.Eshop.Controllers.Admin
{
    #region VariantController

    [AdminPerm("CatalogBits", byte.MaxValue)]
    public class VariantController : BaseController<VariantModel, Variant>
    {
        #region VariantController
        public VariantController(IDataService ds) :
            base(ds)
        {
            InitImageSettings(x => x.Display.Product.Image, x => x.Display.Product.Thumb);
        }
        #endregion

        #region List

        [ValidateInput(false)]
        [AdminPerm("CatalogBits", PermissionMask.View)]
        public ActionResult List([DataSourceRequest] DataSourceRequest request, int productid, bool fullMode = false)
        {
            int totalRows;
            var exceptIds = GetParam<string>("exceptids");
            var minQty = GetParam<int?>("minQty");
            var active = GetParam<bool?>("active");

            TrimReq(request);

            string sortField = "Id";
            var sortDir = SortDirection.Descending;

            GetSortField(request, ref sortField, ref sortDir);

            var items = VariantMgr.GetAll(this, LanguageMgr.FocusLangCode, productid, request.Page,
                request.PageSize, out totalRows, sortDir, sortField, exceptIds, minQty, active);

            if (fullMode) items.Action(m =>
            {
                var raw = m.GetRaw();
                m.VariantName = raw.Product.GetName(raw);
                m.CombinedImage = raw.Product.GetImage(raw);
                m.ProdVat = raw.Product.Vat.Value;
            });

            return Json(new DataSourceResult() { Data = items, Total = totalRows });
        }

        #endregion

        #region Input
        [ValidateInput(false)]
        [AdminPerm("CatalogBits", PermissionMask.View)]
        public ActionResult Input(VariantModel obj, string lang, string optlang, string mode)
        {
            ViewBag.Languages = LanguageMgr.GetAll();
            ViewBag.Settings = GetSettings();
            ViewBag.Statuses = StatusMgr.GetAll(this, 1);

            if (obj == null) obj = new VariantModel();

            if (obj.Languages == null) obj.Languages = new List<VariantLangModel>();
            if (obj.Images == null) obj.Images = new List<VariantImageModel>();

            ModelState.ModLocalize();

            return ClawView("Admin/_VariantsEdit.cshtml", obj);
        }

        #endregion

        #region UploadImageVar

        [HttpPost]
        [AdminPerm("CatalogBits", PermissionMask.Edit)]
        public ActionResult UploadImageVar(int id, HttpPostedFileBase variantImageUpload)
        {
            return base.UploadImage(id, variantImageUpload, false, true);
        } 

        #endregion

        #region GetListForm

        [AdminPerm("CatalogBits", PermissionMask.View)]
        public ActionResult GetListForm(ProductModel model, int id, string langCode)
        {
            var obj = model ?? ProductMgr.Get(this, id);

            ModelState.RemoveKey(GetFieldName<ProductModel, object>(m => m.Opt.PropertyNameInput));
            ModelState.RemoveKey(GetFieldName<ProductModel, object>(m => m.Opt.PropValNameInput));

            ViewBag.Languages = LanguageMgr.GetAll();
            ViewBag.Settings = GetSettings();
            ViewBag.Statuses = StatusMgr.GetAll(this, id);
            ViewBag.Properties = PropertyMgr.GetAll(this);

            var templVar = ViewBag.TemplVariant = new VariantModel()
            {
                Languages = new List<VariantLangModel>(),
                Images = new List<VariantImageModel>()
            };
            var props = ViewBag.Properties as List<PropertyModel>;

            templVar.InitProperties(this, props);

            if (model == null)
            {
                if (obj == null)
                    obj = new ProductModel()
                    {
                        Languages = new List<ProductLangModel>(),
                        Variants = new List<VariantModel>()
                    };
                else obj.InitVariants(this);
            }
            else obj.InitVariants(this, true, true);

            #region Variants

            if (obj.Variants != null)
            {
                for (int i = 0; i < obj.Variants.Count; i++)
                {
                    var vari = obj.Variants[i];
                    var pix = i;

                    #region Languages

                    if (vari.Languages != null)
                        foreach (var itm in vari.Languages
                            .Select((x, idx) => new { Val = x, Index = idx })
                            .Where(x => x.Val.IsDataModelEmpty).OrderByDescending(x => x.Index).ToList())
                        {
                            vari.Languages.Remove(itm.Val);
                            ModelState.RemoveKey(string.Format("{0}[{{0}}]",
                                GetFieldName<ProductModel, object>(m => m.Variants[pix].Languages)),
                                itm.Index, vari.Languages.Count);
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
                            ModelState.RemoveKey(string.Format("{0}[{{0}}]",
                                GetFieldName<ProductModel, object>(m => m.Variants[pix].Images)),
                                itm.Index, vari.Images.Count);
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
                            ModelState.RemoveKey(string.Format("{0}[{{0}}]",
                                GetFieldName<ProductModel, object>(m => m.Variants[pix].Properties)),
                                itm.Index, vari.Properties.Count);
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

            obj.Variants.Action(vari =>
            {
                vari.Properties.Action(x =>
                {
                    if (x.Property == null) x.Property = PropertyMgr.Get(this, x.PropId);
                });
                vari.InitProperties(this, props, true);
            });

            return ClawView("Admin/_ProductsVariant.cshtml", obj);
        }

        #endregion

        #region GetSelPropForm

        [AdminPerm("CatalogBits", PermissionMask.View)]
        public ActionResult GetSelPropForm(int id, string lang)
        {
            ViewBag.Settings = GetSettings();
            ViewBag.Properties = PropertyMgr.GetAll(this);

            return ClawView("Admin/_ProductsSelProp.cshtml");
        }

        #endregion

        #region ListPropValDD

        public ActionResult ListPropValDD(int? propertyid)
        {
            var obj = PropertyMgr.Get(this, propertyid ?? 0);

            if (obj == null) return Json(new object[0], JsonRequestBehavior.AllowGet);

            var lst = obj.GetPropVals(true)
                .Select(x => new { x.Id, x.Name })
                .Union(new[] { new { Id = -1, Name = "[" + StringTable.AddNew + "]" } })
                .Select(x => new DDListItem()
                {
                    Value = x.Id.ToString(),
                    Text = x.Name,
                    Active = true,
                    LangVisible = true
                });

            return Json(lst, JsonRequestBehavior.AllowGet);
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
    }
 
    #endregion
}
