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
using Logistra = Eshop.Shipping.Logistra;

namespace Claw.Modules.Eshop.Controllers.Admin
{
    [AdminPerm("OrderBits", byte.MaxValue)]
    public class LogistraController : OrderController
    {
        private const string EditView = "Admin/_OrdersLogistraBooking.cshtml";

        public LogistraController(IDataService ds) : base(ds) {}

        #region Booking.Get

        [ValidateInput(false)]
        [AdminPerm("OrderBits", PermissionMask.View)]
        public ActionResult Booking(int[] ids, int id, string transportName, string jsonModel, string errorMsg, string jsonErrMsg,
            string jsonIds, string lang, string optlang, string mode)
        {
            if (ids.SafeCount() == 0 && !jsonIds.IsNullOrEmpty())
                ids = JsonConvert.DeserializeObject<int[]>(jsonIds);

            if (id == 0 && ids != null) id = ids.FirstOrDefault();

            var order = OrderMgr.Get(this, id);
            var odom = order == null ? null : order.GetRaw();
            var transAgreement = LogistraMgr.GetTransportAgreement(transportName.IfNull("PostNord")) ?? LogistraMgr.GetTransportAgreement("Bring") ?? LogistraMgr.GetTransportAgreements()?.FirstOrDefault();
            var shipping = odom == null ? null : ((Domain.Shipping) odom.Shipping);  
            var paymentMethod = odom == null ? null : ((Domain.PaymentMethod) odom.PaymentMethod);
            var pickupPoint = odom == null || odom.ShippingPickupPoint.IsNullOrEmpty()
                ? null : odom.ShippingPickupPoint.JsonDeserializeSafe<Logistra.ServicePartner>();

            if (order == null || transAgreement == null) return Content("Invalid action!");

            if (ids.SafeCount() == 0 && order.ShippingService != null && order.GetRaw() != null)
            {
                switch ((ShippingServiceType)order.ShippingService)
                {
                    case ShippingServiceType.Proteria:
                        if (!order.GetRaw().ShippingData.IsNullOrEmpty())
                            return RedirectToAction("Booking", "Proteria", new { id, lang, mode });   
                        break;
                    case ShippingServiceType.Bring:
                        if (!order.GetRaw().BringBookingData.IsNullOrEmpty())
                            return RedirectToAction("Booking", "BringShipping", new { id, lang, mode });
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

            var defSetting = GetSettings().Display.Admin.Shipping;
            ViewBag.Settings = GetSettings();
            ViewBag.TransportAgreement = transAgreement;
            ViewBag.ServicePartners = LogistraMgr.GetServicePartners(odom.DeliverCountry.Code, odom.DeliverPostnr, transAgreement.CarrierId);

            order.InitMandatoryInfo();
            order.InitOrderDetails(this);

            var data = odom.ShippingData.IsNullOrEmpty() ? null : JsonConvert.DeserializeObject<Logistra.BookingResult>(odom.ShippingData);
            var obj = jsonModel.IsNullOrEmpty() ? new LogistraBookingModel()
            {
                OrderId = id,
                Amount = order.FinalTotal,
                Currency = order.Currency ?? "NOK",
                WeightInKg = (odom.GetTotalWeight() ?? 0).NullIf(m => m <= 0) ?? (defSetting.DefWeightInGram / 1000.0m),
                ProductId = shipping == null || !shipping.ServiceProvider.InList(ShippingService.LogistraPostNord) 
                            ? null : shipping.ShippingProducts.FirstOrDefault().SafeProp(x => x.Code),
                PickupPointId = pickupPoint == null || ids.SafeCount() != 0 ? null : pickupPoint.Number,
                CarrierId = transAgreement.CarrierId,
                CarrierName = transAgreement.CarrierName,
                Description = order.OrderDetails.Select(x => string.Format("{0} [{1}]", x.ProductName, x.Quantity))
                            .ToString(";\r\n").SubstringSafe(0, 35),
                NotiSms = true,
                ServiceCod = ids.SafeCount() == 0 && paymentMethod.SafeProp(x => x.PaymentSetting).SafeProp(x => x.Id).InList(Helper.GatewayID.COD),
                TransportAgreementId = transAgreement.Id,
                BookingId = data == null ? null : data.Id,
                BookingNumber = data == null ? odom.ShippingTrackingId : data.Number,
                BookingCreatedAt = data == null ? null : new ODateTime(DateTime.Parse(data.CreatedAt, CultureInfo.InvariantCulture)),
                BookingConsignmentPdf = data == null ? null : data.ConsignmentPdf,
                BookingTrackingUrl = odom.GetShippingTrackingUrl(),
                BookingWaybillPdf = data == null ? null : data.WaybillPdf,
            } : JsonConvert.DeserializeObject<LogistraBookingModel>(jsonModel);

            var hasResult = !obj.BookingNumber.IsNullOrEmpty();
            ViewBag.Printers = hasResult ? LogistraMgr.GetPrinters(ref errorMsg) : null;
            ViewBag.ErrorMsg = errorMsg;

            if (ids.SafeCount() != 0)
            {
                var errLst = jsonErrMsg.IsNullOrEmpty() ? null : JsonConvert.DeserializeAnonymousType(jsonErrMsg, new[] { new { Id = 0, Succeeded = false, ErrMsg = "" } });
                obj.Items = new List<LogistraBookingItemModel>();

                foreach (var itm in ids)
                {
                    var iorder = OrderMgr.Get(this, itm);
                    var idom = iorder.GetRaw();
                    var idata = idom.ShippingData.IsNullOrEmpty() ? null : JsonConvert.DeserializeObject<Logistra.BookingResult>(idom.ShippingData);
                    var errItm = errLst == null ? null : errLst.FirstOrDefault(m => m.Id == itm || m.Id == 0);

                    obj.Items.Add(new LogistraBookingItemModel()
                    {
                        OrderId = itm,
                        Amount = iorder.FinalTotal,
                        Currency = iorder.Currency ?? "NOK",
                        WeightInKg = (idom.GetTotalWeight() ?? 0).NullIf(m => m <= 0) ?? (defSetting.DefWeightInGram / 1000.0m),
                        //PickupPointId = null,
                        Description = idom.OrderDetails.Select(x => string.Format("{0} [{1}]", x.ProductName, x.Quantity))
                                    .ToString(";\r\n").SubstringSafe(0, 35),
                        ServiceCod = ((Domain.PaymentMethod)idom.PaymentMethod).SafeProp(x => x.PaymentSetting).SafeProp(x => x.Id).InList(Helper.GatewayID.COD),

                        BookingId = idata == null ? null : idata.Id,
                        BookingNumber = idata == null ? idom.ShippingTrackingId : idata.Number,
                        BookingTrackingUrl = idata == null ? null : idata.TrackingUrl,
                        BookingConsignmentPdf = idata == null ? null : idata.ConsignmentPdf,
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
        public ActionResult Booking(LogistraBookingModel obj, string lang, string mode)
        {
            string errMsg = null, jsonErrMsg = null;
            int[] ids = null;

            if (obj == null) return Content("");

            ModelState.RemoveKey(GetFieldName<LogistraBookingModel, object>(m => m.PickupPointId));
            ModelState.RemoveKey(GetFieldName<LogistraBookingModel, object>(m => m.Amount));
            ModelState.RemoveKey(GetFieldName<LogistraBookingModel, object>(m => m.Currency));
            ModelState.RemoveKey(GetFieldName<LogistraBookingModel, object>(m => m.WeightInKg));
            ModelState.RemoveKey(GetFieldName<LogistraBookingModel, object>(m => m.ConsigneeCustomerNumber));
            ModelState.RemoveKey(GetFieldName<LogistraBookingModel, object>(m => m.FreightPayerCustomerNumber));

            if (ModelState.ModLocalize().IsValid)
            {
                if (obj.Items.SafeCount() == 0)
                {
                    var ret = LogistraMgr.Booking(this, obj, Request);
                    errMsg = ret.Item1 ? null : ret.Item2;
                }
                else
                {
                    var ret = LogistraMgr.BookingBundle(this, obj, Request);
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
                transportName = obj.CarrierName,
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
            var data = order == null || order.ShippingData == null || !(order.ShippingService ?? 0).InList(ShippingServiceType.Logistra)
                ? null : Newtonsoft.Json.JsonConvert.DeserializeObject<Logistra.BookingResult>(order.ShippingData);

            return Json(new
            {
                Data = data ?? new Logistra.BookingResult() { Number = order == null ? null : order.ShippingTrackingId }
            }, JsonRequestBehavior.AllowGet);
        }
        #endregion

        #region DownloadPdf
        [AdminPerm("OrderBits", PermissionMask.View)]
        public ActionResult DownloadPdf(string url)
        {
            if (url.IsNullOrEmpty()) return new HttpNotFoundResult();

            var settings = GetSettings().Display.Admin.Logistra;
            var api = new Logistra.Cargonizer(settings.ApiKey, settings.SenderId, settings.TestMode);
            var ret = (System.Tuple<Stream, string>)api.ApiInvoke(url, null, false);
            
            ret.Item1.Seek(0,SeekOrigin.Begin);
            Response.Headers["Content-Disposition"] = ret.Item2 ?? string.Format("attachment; filename=\"{0}\"", Path.GetFileName(url));
    
            return File(ret.Item1, "application/pdf");
        }
        #endregion

        #region ListProductD2
        public ActionResult ListProductD2(int serviceProvider)
        {
            var transAgreement = serviceProvider.InList(ShippingService.LogistraPostNord)
                ? LogistraMgr.GetTransportAgreement("PostNord")
                : (serviceProvider.InList(ShippingService.LogistraBring) ? LogistraMgr.GetTransportAgreement("Bring") : null);

            var lst = transAgreement == null ? null : transAgreement.Products
                .Where(x => x.ItemTypes.Any(z => z.Id.EqualsIgCase("package")))
                .Select(x => new DDListItem()
                {
                    Text = x.Name,
                    Value = x.Identifier,
                    Active = true,
                    LangVisible = true,
                    Model = new
                    {
                        x.Services, x.ItemTypes, x.ServicePartnerPossible, x.RequiresServicePartner,
                        x.RequiresConsigneeFreightPayerCustomerNumber, x.RequiresPickupAddress, x.ThirdPartyFreightPayerRequired
                    }
                });

            return Json(lst ?? new DDListItem[0], JsonRequestBehavior.AllowGet);
        } 
        #endregion

        #region PrintBooking
        [AdminPerm("OrderBits", PermissionMask.View)]
        public ActionResult PrintBooking(string consignmentId, string[] consignmentIds, string printerId, string pieceId = null)
        {
            if ((consignmentId.IsNullOrEmpty() && consignmentIds.SafeCount() == 0) || printerId.IsNullOrEmpty()) 
                return new HttpNotFoundResult();

            try
            {
                var settings = GetSettings().Display.Admin.Logistra;
                var api = new Logistra.Cargonizer(settings.ApiKey, settings.SenderId, settings.TestMode);
                var ret = consignmentIds.SafeCount() == 0
                    ? api.LabelDirect(printerId, consignmentId, pieceId)
                    : api.LabelDirect(printerId, consignmentIds);

                if(!ret.IsNullOrEmpty()) LogErr(new Exception(ret));

                return JsonStatus(StringTable.BookingPrint, true, ret);
            }
            catch (Exception err)
            {
                return JsonStatus(StringTable.BookingPrint, false, err.Message);
            }

        }
        #endregion
    }
}