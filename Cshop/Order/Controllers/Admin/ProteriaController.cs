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
using NPOI.SS.Formula.Functions;
using Proteria = Eshop.Shipping.Proteria;

namespace Claw.Modules.Eshop.Controllers.Admin
{
    [AdminPerm("OrderBits", byte.MaxValue)]
    public class ProteriaController : OrderController
    {
        private const string EditView = "Admin/_OrdersProteriaBooking.cshtml";

        public ProteriaController(IDataService ds) : base(ds) { }

        #region Booking.Get

        [ValidateInput(false)]
        [AdminPerm("OrderBits", PermissionMask.View)]
        public ActionResult Booking(int id, string transportName, bool? checkbooking, string jsonModel, string errorMsg, string lang, string optlang, string mode)
        {
            var order = OrderMgr.Get(this, id);
            var odom = order == null ? null : order.GetRaw();
            var shipping = odom == null ? null : ((Domain.Shipping) odom.Shipping);  
            var paymentMethod = odom == null ? null : ((Domain.PaymentMethod) odom.PaymentMethod);
            var settings = GetSettings();

            if (order == null || odom == null) return Content("Invalid action!");

            if (order.ShippingService != null && order.GetRaw() != null)
            {
                switch ((ShippingServiceType) order.ShippingService)
                {
                    case ShippingServiceType.Bring:
                        if (!order.GetRaw().BringBookingData.IsNullOrEmpty())
                            return RedirectToAction("Booking", "BringShipping", new { id, lang, mode });   
                        break;
                    case ShippingServiceType.Logistra:
                        if (!order.GetRaw().ShippingData.IsNullOrEmpty())
                            return RedirectToAction("Booking", "Logistra", new { id, lang, mode });
                        break;
                    case ShippingServiceType.PorterBuddy:
                        if (!order.GetRaw().ShippingData.IsNullOrEmpty())
                            return RedirectToAction("Booking", "PorterBuddy", new { id, lang, mode });
                        break;
                    case ShippingServiceType.Postnord:
                        if (!order.GetRaw().ShippingData.IsNullOrEmpty())
                            return RedirectToAction("Booking", "Postnord", new { id, lang, mode });
                        break;
                }
            }

            ViewBag.Settings = settings;

            order.InitMandatoryInfo();
            order.InitOrderDetails(this);

            var data = odom.ShippingData.IsNullOrEmpty() ? null : JsonConvert.DeserializeObject<Proteria.BookingResult>(odom.ShippingData);
            var obj = jsonModel.IsNullOrEmpty() ? new ProteriaBookingModel()
            {
                OrderId = id,
                Amount = order.FinalTotal,
                WidthInCm = 23, HeightInCm = 13, LengthInCm = 10,
                WeightInKg = order.TotalWeight,
                ProductId = shipping == null || !shipping.ServiceProvider.InList(ShippingService.LogistraPostNord) 
                            ? null : shipping.ShippingProducts.FirstOrDefault().SafeProp(x => x.Code),
                PickupPointId = null,
                CarrierName = transportName,
                CodAccountNumber = settings.Display.Admin.Proteria.CodAccountNumber,
                Description = order.OrderDetails.Select(x => string.Format("{0} [{1}]", x.ProductName, x.Quantity))
                            .ToString(";\r\n").SubstringSafe(0, 35),
                NotiSms = true,
                ServiceCod = paymentMethod.SafeProp(x => x.PaymentSetting).SafeProp(x => x.Id).InList(Helper.GatewayID.COD),
                BookingShipmentId = data == null ? null : data.ShipmentId,
                BookingTrackingNumber = data == null ? odom.ShippingTrackingId : data.TrackingNumber,
                BookingCreatedAt = data == null ? null : data.CreatedAt,
                BookingTrackingUrl = data == null ? null : data.TrackingUrl,
            } : JsonConvert.DeserializeObject<ProteriaBookingModel>(jsonModel);

            if (errorMsg.IsNullOrEmpty() && data != null && (!data.Error.IsNullOrEmpty() || data.Valid == false))
                errorMsg = !data.Error.IsNullOrEmpty() && !data.Error.EqualsIgCase(";failed") ? data.Error : StringTable.ProteriaBookingValidFailed;

            var hasResult = !obj.BookingTrackingNumber.IsNullOrEmpty();

            ViewBag.Printers = hasResult ? ProteriaMgr.GetPrinters(ref errorMsg) : null;
            ViewBag.PickUpPoints = hasResult ? null : ProteriaMgr.GetPickUpPoints(odom.DeliverCountry.Code, odom.DeliverPostnr, ref errorMsg, transportName.IfNull("Bring"));
            ViewBag.ErrorMsg = errorMsg;
            
            return ClawView(EditView, obj);
        } 

        #endregion

        #region Booking.Post

        [HttpPost]
        [ValidateInput(false)]
        [AdminPerm("OrderBits", PermissionMask.Edit)]
        public ActionResult Booking(ProteriaBookingModel obj, bool? checkbooking, string lang, string mode)
        {
            string errMsg;

            if (obj == null) return Content("");

            ModelState.RemoveKey(GetFieldName<ProteriaBookingModel, object>(m => m.PickupPointId));
            ModelState.RemoveKey(GetFieldName<ProteriaBookingModel, object>(m => m.Amount));
            ModelState.RemoveKey(GetFieldName<ProteriaBookingModel, object>(m => m.WeightInKg));
            ModelState.RemoveKey(GetFieldName<ProteriaBookingModel, object>(m => m.CodAccountNumber));
            ModelState.RemoveKey(GetFieldName<ProteriaBookingModel, object>(m => m.BookingPrinter));

            if (ModelState.ModLocalize().IsValid)
            {
                var ret = ProteriaMgr.Booking(this, obj, this);
                errMsg = ret.Item1 ? null : ret.Item2;
            }
            else errMsg = ModelState
                .SelectMany(x => x.Value.Errors.Select(e => string.Format("[{0}]: {1}", x.Key, e.ErrorMessage)))
                .ToString(";\r\n");

            return RedirectToAction("Booking", new
            {
                id = obj.OrderId, checkbooking, lang, mode,
                jsonModel = errMsg.IsNullOrEmpty() ? null : JsonConvert.SerializeObject(obj),
                errorMsg = errMsg
            });
        }

        #endregion

        #region GetShippingState
        [AdminPerm("OrderBits", PermissionMask.View)]
        public ActionResult GetShippingState(int id, string lang, string optlang, string mode)
        {
            var order = OrderMgr.GetDom(this, id);
            var data = order == null || order.ShippingData == null || !(order.ShippingService ?? 0).InList(ShippingServiceType.Proteria)
                ? null : JsonConvert.DeserializeObject<Proteria.BookingResult>(order.ShippingData);

            return Json(new
            {
                Data = data ?? new Proteria.BookingResult() { TrackingNumber = order == null ? null : order.ShippingTrackingId }
            }, JsonRequestBehavior.AllowGet);
        }
        #endregion

        #region DownloadPdf
        [AdminPerm("OrderBits", PermissionMask.View)]
        public ActionResult DownloadPdf(string shipmentId)
        {
            if (shipmentId.IsNullOrEmpty()) return new HttpNotFoundResult();

            var settings = GetSettings().Display.Admin.Proteria;
            var api = new Proteria.CloudConnect(settings.ApiKey);
            var ret = api.GetLabel(settings.PrintFormat, new[] { shipmentId });

            if (ret == null || ret.Item1 == null) return HttpNotFound();

            ret.Item1.Seek(0,SeekOrigin.Begin);
            Response.Headers["Content-Disposition"] = ret.Item2.IfNull(string.Format("attachment; filename=\"{0}.pdf\"", shipmentId));

            return File(ret.Item1, "application/pdf");
        }
        #endregion

        #region PrintBooking
        [AdminPerm("OrderBits", PermissionMask.View)]
        public ActionResult PrintBooking(string shipmentId, string printId, string printType, string printName)
        {
            if (shipmentId.IsNullOrEmpty() || printId.IsNullOrEmpty() || printType.IsNullOrEmpty()) return new HttpNotFoundResult();

            try
            {
                var settings = GetSettings().Display.Admin.Proteria;
                var api = new Proteria.CloudConnect(settings.ApiKey);
                var ret = api.Print(new[] {shipmentId}, new[] {new Proteria.Printer() {Id = printId, Type = printType, Name = printName}});

                return JsonStatus(StringTable.BookingPrint, ret);
            }
            catch (Exception err)
            {
                return JsonStatus(StringTable.BookingPrint, false, err.Message);
            }
            
        }
        #endregion
    }
}