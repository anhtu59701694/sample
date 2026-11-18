using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Web.Mvc;
using Claw.Core.Interfaces;
using Claw.Modules.Eshop.DAL;
using Claw.Modules.Eshop.Models;
using Newtonsoft.Json;
using Postnord = Eshop.Shipping.Postnord;

namespace Claw.Modules.Eshop.Controllers.Admin
{
    [AdminPerm("OrderBits", byte.MaxValue)]
    public class PostnordController : OrderController
    {
        private const string EditView = "Admin/_OrdersPostnordBooking.cshtml";

        public PostnordController(IDataService ds) : base(ds) { }

        #region Booking.Get

        [ValidateInput(false)]
        [AdminPerm("OrderBits", PermissionMask.View)]
        public ActionResult Booking(int[] ids, int id, string jsonModel, string errorMsg, string jsonErrMsg,
            string jsonIds, string lang, string optlang, string mode)
        {
            if (ids.SafeCount() == 0 && !jsonIds.IsNullOrEmpty())
                ids = JsonConvert.DeserializeObject<int[]>(jsonIds);

            if (id == 0 && ids != null) id = ids.FirstOrDefault();

            var order = OrderMgr.Get(this, id);
            var odom = order == null ? null : order.GetRaw();
            var shipping = odom == null ? null : ((Domain.Shipping) odom.Shipping);  
            var paymentMethod = odom == null ? null : ((Domain.PaymentMethod) odom.PaymentMethod);
            var pickupPoint = odom == null || odom.ShippingPickupPoint.IsNullOrEmpty()
                ? null : odom.ShippingPickupPoint.JsonDeserializeSafe<Postnord.Data.PickupPoint>();

            if (order == null) return Content("Invalid action!");

            if (ids.SafeCount() == 0 && order.ShippingService != null && order.GetRaw() != null)
            {
                switch ((ShippingServiceType)order.ShippingService)
                {
                    case ShippingServiceType.Proteria:
                        if (!order.GetRaw().ShippingData.IsNullOrEmpty())
                            return RedirectToAction("Booking", "Proteria", new { id, lang, mode });
                        break;
                    case ShippingServiceType.Logistra:
                        if (!order.GetRaw().ShippingData.IsNullOrEmpty())
                            return RedirectToAction("Booking", "Logistra", new { id, lang, mode });
                        break;
                    case ShippingServiceType.Bring:
                        if (!order.GetRaw().BringBookingData.IsNullOrEmpty())
                            return RedirectToAction("Booking", "BringShipping", new { id, lang, mode });
                        break;
                    case ShippingServiceType.PorterBuddy:
                        if (!order.GetRaw().ShippingData.IsNullOrEmpty())
                            return RedirectToAction("Booking", "PorterBuddy", new { id, lang, mode });
                        break;
                }
            }

            var defSetting = GetSettings().Display.Admin.Shipping;
            var settings = GetSettings();
            ViewBag.Settings = settings;

            order.InitMandatoryInfo();
            order.InitOrderDetails(this);

            var data = odom.ShippingData.IsNullOrEmpty() ? null : JsonConvert.DeserializeObject<Postnord.BookingResult>(odom.ShippingData);
            var obj = jsonModel.IsNullOrEmpty() ? new PostnordBookingModel()
            {
                OrderId = id,
                Amount = order.FinalTotal,
                Currency = order.Currency ?? "NOK",
                CodAccountNumber = settings.Display.Admin.Postnord.CodAccountNumber,
                CountryCode = order.DeliverCountryCode ?? "NO",
                WidthInCm = defSetting.WidthInCm,
                HeightInCm = defSetting.HeightInCm,
                LengthInCm = defSetting.LengthInCm,
                WeightInKg = (odom.GetTotalWeight() ?? 0).NullIf(m => m <= 0) ?? (defSetting.DefWeightInGram / 1000.0m),
                ProductId = shipping == null || !shipping.ServiceProvider.InList(ShippingService.PostNord) 
                            ? null : shipping.ShippingProducts.FirstOrDefault().SafeProp(x => x.Code),
                PickupPointId = pickupPoint == null || ids.SafeCount() != 0 ? null : pickupPoint.servicePointId,
                Description = order.OrderDetails.Select(x => string.Format("{0} [{1}]", x.ProductName, x.Quantity))
                            .ToString(";\r\n").SubstringSafe(0, 35),
                NotiSms = true,
                NotiEmail = true,
                ServiceCod = ids.SafeCount() == 0 && paymentMethod.SafeProp(x => x.PaymentSetting).SafeProp(x => x.Id).InList(Helper.GatewayID.COD),
                BookingId = data == null ? null : data.ShipmentId,
                BookingNumber = data == null ? odom.ShippingTrackingId : data.TrackingNumber,
                PrintId = data == null ? null : data.PrintId,
                BookingTrackingUrl = odom.GetShippingTrackingUrl(),
            } : JsonConvert.DeserializeObject<PostnordBookingModel>(jsonModel);

            var hasResult = !obj.BookingNumber.IsNullOrEmpty();
            ViewBag.ErrorMsg = errorMsg;

            if (ids.SafeCount() != 0)
            {
                var errLst = jsonErrMsg.IsNullOrEmpty() ? null : JsonConvert.DeserializeAnonymousType(jsonErrMsg, new[] { new { Id = 0, Succeeded = false, ErrMsg = "" } });
                obj.Items = new List<PostnordBookingItemModel>();

                foreach (var itm in ids)
                {
                    var iorder = OrderMgr.Get(this, itm);
                    var idom = iorder.GetRaw();
                    var idata = idom.ShippingData.IsNullOrEmpty() ? null : JsonConvert.DeserializeObject<Postnord.BookingResult>(idom.ShippingData);
                    var errItm = errLst == null ? null : errLst.FirstOrDefault(m => m.Id == itm || m.Id == 0);

                    obj.Items.Add(new PostnordBookingItemModel()
                    {
                        OrderId = itm,
                        Amount = iorder.FinalTotal,
                        Currency = iorder.Currency ?? "NOK",
                        WeightInKg = (idom.GetTotalWeight() ?? 0).NullIf(m => m <= 0) ?? (defSetting.DefWeightInGram / 1000.0m),
                        //PickupPointId = null,
                        Description = idom.OrderDetails.Select(x => string.Format("{0} [{1}]", x.ProductName, x.Quantity))
                                    .ToString(";\r\n").SubstringSafe(0, 35),
                        ServiceCod = ((Domain.PaymentMethod)idom.PaymentMethod).SafeProp(x => x.PaymentSetting).SafeProp(x => x.Id).InList(Helper.GatewayID.COD),

                        BookingId = idata == null ? null : idata.ShipmentId,
                        BookingNumber = idata == null ? idom.ShippingTrackingId : idata.TrackingNumber,
                        PrintId = idata == null ? null : idata.PrintId,
                        BookingTrackingUrl = idata == null ? null : idata.TrackingUrl,
                        ErrMsg = errItm == null ? null : errItm.ErrMsg.IfNull((errItm.Succeeded ? StringTable.Succeeded : StringTable.Failed)),
                    });
                }
            }

            return ClawView(EditView, obj);
        } 

        #endregion

        #region Booking.Post

        [HttpPost]
        [ValidateInput(false)]
        [AdminPerm("OrderBits", PermissionMask.Edit)]
        public ActionResult Booking(PostnordBookingModel obj, string lang, string mode)
        {
            string errMsg = null, jsonErrMsg = null;
            int[] ids = null;

            if (obj == null) return Content("");

            ModelState.RemoveKey(GetFieldName<PostnordBookingModel, object>(m => m.PickupPointId));
            ModelState.RemoveKey(GetFieldName<PostnordBookingModel, object>(m => m.Amount));
            ModelState.RemoveKey(GetFieldName<PostnordBookingModel, object>(m => m.Currency));
            ModelState.RemoveKey(GetFieldName<PostnordBookingModel, object>(m => m.CodAccountNumber));
            ModelState.RemoveKey(GetFieldName<PostnordBookingModel, object>(m => m.WeightInKg));

            if (ModelState.ModLocalize().IsValid)
            {
                if (obj.Items.SafeCount() == 0)
                {
                    var ret = PostnordMgr.Booking(this, obj, Request);
                    errMsg = ret.Item1 ? null : ret.Item2;
                }
                else
                {
                    var ret = PostnordMgr.BookingBundle(this, obj, Request);
                    ids = obj.Items.Select(m => m.OrderId).ToArray();
                    jsonErrMsg = ret == null ? null : JsonConvert.SerializeObject(ret.Select(m => new { Id = m.Item1, Succeeded = m.Item2, ErrMsg = m.Item3 }));
                }
            }
            else errMsg = ModelState
                .SelectMany(x => x.Value.Errors.Select(e => string.Format("[{0}]: {1}", x.Key, e.ErrorMessage)))
                .ToString(";\r\n");

            return RedirectToAction("Booking", new
            {
                id = obj.OrderId, lang, mode,
                jsonModel = errMsg.IsNullOrEmpty() ? null : JsonConvert.SerializeObject(obj),
                errorMsg = errMsg, jsonErrMsg,
                jsonIds = ids.SafeCount() == 0 ? null : JsonConvert.SerializeObject(ids),
            });
        }

        #endregion

        #region GetShippingState
        [AdminPerm("OrderBits", PermissionMask.View)]
        public ActionResult GetShippingState(int id, string lang, string optlang, string mode)
        {
            var order = OrderMgr.GetDom(this, id);
            var data = order == null || order.ShippingData == null || !(order.ShippingService ?? 0).InList(ShippingServiceType.Postnord)
                ? null : Newtonsoft.Json.JsonConvert.DeserializeObject<Postnord.BookingResult>(order.ShippingData);

            return Json(new
            {
                Data = data ?? new Postnord.BookingResult() { TrackingNumber = order == null ? null : order.ShippingTrackingId }
            }, JsonRequestBehavior.AllowGet);
        }
        #endregion

        #region DownloadPdf

        [AdminPerm("OrderBits", PermissionMask.View)]
        public ActionResult DownloadPdf(string id)
        {
            if (id.IsNullOrEmpty()) return new HttpNotFoundResult();

            var settings = GetSettings().Display.Admin.Postnord;
            var api = new Postnord.PostnordApi(settings.ApiKey, settings.TestMode, settings.CustomerId);
            var ret = api.LabelPdf(id, settings.LabelType, settings.PaperSize);
            
            ret.Seek(0,SeekOrigin.Begin);
            Response.Headers["Content-Disposition"] = string.Format("inline; filename=\"{0}\"", id + ".pdf");
    
            return File(ret, "application/pdf");
        }

        #endregion

        #region ListProductD2
        public ActionResult ListProductD2()
        {
            var settings = GetSettings();
            var services = PostnordMgr.GetServices(settings.General.Country);

            var lst = services == null ? null : services
                .OrderBy(x => x.Name)
                .Select(x => new DDListItem()
                {
                    Text = x.Name,
                    Value = x.Code,
                    Active = true,
                    LangVisible = true,
                    Model = new
                    {
                        x.Services,
                        x.IsOptionalServicePoint,
                        x.AdditonalServices,
                    }
                });

            return Json(lst ?? new DDListItem[0], JsonRequestBehavior.AllowGet);
        }
        #endregion

    }
}