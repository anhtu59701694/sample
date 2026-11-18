using System.Collections.Generic;
using System.Text;
using System.Web.Mvc;
using Claw.Core.Enumerations;
using Claw.Core.Interfaces;
using Claw.Modules.Eshop.DAL;
using Claw.Modules.Eshop.Helper;
using Claw.Modules.Eshop.Models;
using Eshop.Shipping.Bring;
using Kendo.Mvc.UI;
using System.Linq;
using Newtonsoft.Json;
using Eshop.Shipping.BaseProvider;
using System;

namespace Claw.Modules.Eshop.Controllers.Admin
{
    [AdminPerm("OrderBits", byte.MaxValue)]
    public class BringShippingController : BaseController
    {
        public BringShippingController(IDataService ds) : base(ds) { }

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

            if (order == null) return HttpNotFound();

            if (ids.SafeCount() == 0 && order.ShippingService != null && order.GetRaw() != null)
            {
                switch ((ShippingServiceType)order.ShippingService)
                {
                    case ShippingServiceType.Proteria:
                        if (!order.GetRaw().ShippingData.IsNullOrEmpty())
                            return RedirectToAction("Booking", "Proteria", new {id, lang, mode});
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

            ViewBag.Settings = GetSettings();
            ViewBag.ErrorMsg = errorMsg;
            ViewBag.Customers = BringShippingMgr.GetCustomers(Request.Url);
            ViewBag.PickupPoints = BringShippingMgr.GetPickupPoints(order.DeliverCountryCode, order.DeliverPostnr);
            ViewBag.Order = order;

            order.InitMandatoryInfo();
            order.InitOrderDetails(this);

            var defSetting = GetSettings().Display.Admin.Shipping;
            var settings = GetSettings().Display.Admin.BringShipping;
            var dom = order.GetRaw();
            var shipping = (Domain.Shipping)dom.Shipping;

            var goodsDesc = order.OrderDetails.Select(x => string.Format("{0} [{1}]", x.ProductName, x.Quantity)).ToString("; ");

            var data = dom.BringBookingData.IsNullOrEmpty() ? null : JsonConvert.DeserializeObject<BookingResult>(dom.BringBookingData);

            if ((data != null && data.ConsignmentNumber.IsNullOrEmpty()) || !dom.BringMailBoxData.IsNullOrEmpty())
                return RedirectToAction("MailBox", new { id, lang, optlang, mode });

            var pickupPoint = dom.ShippingPickupPoint.IsNullOrEmpty()
                ? null : dom.ShippingPickupPoint.JsonDeserializeSafe<PickupPoint>();

            var obj = jsonModel.IsNullOrEmpty() ? new BringBookingModel()
            {
                OrderId = order.Id, Amount = order.FinalTotal, Currency = order.Currency ?? "NOK",
                WidthInCm = defSetting.WidthInCm,
                HeightInCm = defSetting.HeightInCm,
                LengthInCm = defSetting.LengthInCm,
                WeightInKg = (dom.GetTotalWeight() ?? 0).NullIf(m => m <= 0) ?? (defSetting.DefWeightInGram/1000.0m),
                NumberOfItems = order.OrderDetails.Sum(m => m.Quantity) ?? 0,
                GoodsDescription = goodsDesc.ToString().TrimLength(35),
                ProductId = shipping == null || !shipping.ServiceProvider.InList(ShippingService.Bring) ? "" : (shipping.ShippingProducts.Select(x => x.Code).FirstOrDefault() ?? ""),
                PickupPointId = pickupPoint == null || ids.SafeCount() != 0 ? null : pickupPoint.Id,
                CustomerId = "",
                CodAccountNumber = settings.CodAccountNumber,
                ServiceCod = ids.SafeCount() == 0 && ((Domain.PaymentMethod)dom.PaymentMethod).PaymentSetting.SafeProp(m => m.Id).InList(GatewayID.COD),
                RecipientNoti = true, RecipientNotiEmail = order.Customer.Email,
                RecipientNotiMobile = order.DeliverMobile.IfNull(order.DeliverPhone).TrimPhone(true),

                ConsignmentNumber = data == null ? dom.ShippingTrackingId : data.ConsignmentNumber,
                LabelUrl = data == null ? null : data.LabelUrl,
                TrackingUrl = data == null ? null : data.TrackingUrl,
                WaybillUrl = data == null ? null : data.WaybillUrl,
                PackageNumber = data == null || data.Packages.SafeCount() == 0 ? "" : data.Packages[0].PackageNumber,
                EarliestPickup = data == null || (data.EarliestPickup ?? DateTime.MinValue) <= DateTime.MinValue ? null : data.EarliestPickup,
                ExpectedDelivery = data == null || (data.ExpectedDelivery ?? DateTime.MinValue) <= DateTime.MinValue ? null : data.ExpectedDelivery
            } : JsonConvert.DeserializeObject<BringBookingModel>(jsonModel);

            if (ids.SafeCount() != 0)
            {
                var errLst = jsonErrMsg.IsNullOrEmpty() ? null : JsonConvert.DeserializeAnonymousType(jsonErrMsg, new[] { new { Id = 0, Succeeded = false, ErrMsg = "" } });
                obj.Items = new List<BringBookingItemModel>();

                foreach (var itm in ids)
                {
                    var iorder = OrderMgr.Get(this, itm);
                    var idom = iorder.GetRaw();
                    var idata = idom.BringBookingData.IsNullOrEmpty() ? null : JsonConvert.DeserializeObject<BookingResult>(idom.BringBookingData);
                    var errItm = errLst == null ? null : errLst.FirstOrDefault(m => m.Id == itm || m.Id == 0);

                    var igoodsDesc = idom.OrderDetails.Select(x => string.Format("{0} [{1}]", x.ProductName, x.Quantity)).ToString("; ");

                    obj.Items.Add(new BringBookingItemModel()
                    {
                        OrderId = iorder.Id,
                        Amount = iorder.FinalTotal,
                        Currency = iorder.Currency ?? "NOK",
                        WeightInKg = (idom.GetTotalWeight() ?? 0).NullIf(m => m <= 0) ?? (defSetting.DefWeightInGram / 1000.0m),
                        NumberOfItems = idom.OrderDetails.Sum(m => m.Quantity),
                        GoodsDescription = igoodsDesc.ToString().TrimLength(35),
                        //PickupPointId = pickupPoint == null ? null : pickupPoint.Id,
                        ServiceCod = ((Domain.PaymentMethod)idom.PaymentMethod).PaymentSetting.SafeProp(m => m.Id).InList(GatewayID.COD),
                        RecipientNotiEmail = iorder.Customer.Email,
                        RecipientNotiMobile = iorder.DeliverMobile.IfNull(order.DeliverPhone).TrimPhone(true),

                        ConsignmentNumber = idata == null ? idom.ShippingTrackingId : idata.ConsignmentNumber,
                        LabelUrl = idata == null ? null : idata.LabelUrl,
                        ErrMsg = errItm == null ? null : errItm.ErrMsg.IfNull((errItm.Succeeded ? StringTable.Succeeded : StringTable.Failed)),
                    });
                }
            }

            return ClawView("Admin/_OrdersBringBooking.cshtml", obj);
        }

        #endregion

        #region Booking.Post

        [HttpPost]
        [ValidateInput(false)]
        //[ValidateAntiForgeryToken]
        [AdminPerm("OrderBits", PermissionMask.Edit)]
        public ActionResult Booking([DataSourceRequest] DataSourceRequest request, BringBookingModel obj, string lang, string mode)
        {
            string errMsg = null, jsonErrMsg = null;
            int[] ids = null;

            if (obj == null) return HttpNotFound();

            ModelState.RemoveKey(GetFieldName<BringBookingModel, object>(m => m.PickupPointId));
            ModelState.RemoveKey(GetFieldName<BringBookingModel, object>(m => m.Amount));
            ModelState.RemoveKey(GetFieldName<BringBookingModel, object>(m => m.Currency));
            ModelState.RemoveKey(GetFieldName<BringBookingModel, object>(m => m.CodAccountNumber));
            ModelState.RemoveKey(GetFieldName<BringBookingModel, object>(m => m.RecipientNotiEmail));
            ModelState.RemoveKey(GetFieldName<BringBookingModel, object>(m => m.RecipientNotiMobile));

            if (ModelState.ModLocalize().IsValid)
            {
                if (obj.Items.SafeCount() == 0)
                {
                    var ret = BringShippingMgr.Booking(this, obj, Request);
                    errMsg = ret.Item1 ? null : ret.Item2;
                }
                else
                {
                    var ret = BringShippingMgr.BookingBundle(this, obj, Request);
                    ids = obj.Items.Select(m => m.OrderId).ToArray();
                    jsonErrMsg = ret == null ? null : JsonConvert.SerializeObject(ret.Select(m => new {Id = m.Item1, Succeeded = m.Item2, ErrMsg = m.Item3}));
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

        #region MailBox.Get

        [ValidateInput(false)]
        [AdminPerm("OrderBits", PermissionMask.View)]
        public ActionResult MailBox(int id, string jsonModel, string errorMsg, string lang, string optlang, string mode)
        {
            var order = OrderMgr.Get(this, id);

            if (order == null) return HttpNotFound();

            var settings = GetSettings().Display.Admin.BringShipping;
            var dom = order.GetRaw();

            var data = dom.BringBookingData.IsNullOrEmpty() ? null : JsonConvert.DeserializeObject<ToMailboxResult>(dom.BringBookingData);

            if ((data != null && data.Id.IsNullOrEmpty()) || !dom.ShippingTrackingId.IsNullOrEmpty())
                return RedirectToAction("Booking", new {id, lang, optlang, mode});

            var model = dom.BringMailBoxData.IsNullOrEmpty() || data != null ? null : JsonConvert.DeserializeObject<BringMailBoxModel>(dom.BringMailBoxData);
            var obj = jsonModel.IsNullOrEmpty() ? (model ?? new BringMailBoxModel()
            {
                Rfid = false,
                OrderId = order.Id,
                WeightInGram = (int)Math.Round(1000*(dom.GetTotalWeight() ?? 0)),
                RecipientMobile = order.DeliverMobile.IfNull(order.DeliverPhone).TrimPhone(true),

                MailboxId = data == null ? null : data.Id,
                LabelUri = data == null ? null : data.LabelUri,
                RfidLabelUri = data == null ? null : data.RfidLabelUri,
                WaybillUri = data == null ? null : data.WaybillUri,
            }) : JsonConvert.DeserializeObject<BringMailBoxModel>(jsonModel);

            ViewBag.Settings = GetSettings();
            ViewBag.ErrorCode = model == null || !errorMsg.IsNullOrEmpty() ? null : ActionResultStatus.Success.Val();
            ViewBag.ErrorMsg = ViewBag.ErrorCode == null ? errorMsg : Order.StringTable.BringMailBoxSaved;

            return ClawView("Admin/_OrdersBringMailBox.cshtml", obj);
        }

        #endregion

        #region MailBox.Post

        [HttpPost]
        [ValidateInput(false)]
        //[ValidateAntiForgeryToken]
        [AdminPerm("OrderBits", PermissionMask.Edit)]
        public ActionResult MailBox([DataSourceRequest] DataSourceRequest request, BringMailBoxModel obj, string lang, string mode)
        {
            string errMsg;

            if (obj == null) return HttpNotFound();

            if (ModelState.ModLocalize().IsValid)
            {
                var ret = BringShippingMgr.MailBox(this, new[] { obj }, true, Request);
                errMsg = ret.Item1 ? null : ret.Item2;
            }
            else errMsg = ModelState
                .SelectMany(x => x.Value.Errors.Select(e => string.Format("[{0}]: {1}", x.Key, e.ErrorMessage)))
                .ToString(";\r\n");

            return RedirectToAction("MailBox", new
            {
                id = obj.OrderId, lang, mode,
                jsonModel = errMsg.IsNullOrEmpty() ? null : JsonConvert.SerializeObject(obj),
                errorMsg = errMsg
            });
        }

        #endregion

        #region MailBoxCommit

        [AdminPerm("OrderBits", PermissionMask.Edit)]
        public virtual ActionResult MailBoxCommit(int[] ids)
        {
            var models = (ids ?? new int[0]).Select(x => OrderMgr.GetDom(this, x))
                .Where(x => x != null && !x.BringMailBoxData.IsNullOrEmpty() && x.ShippingTrackingId.IsNullOrEmpty() && x.BringBookingData.IsNullOrEmpty())
                .Select(x => JsonConvert.DeserializeObject<BringMailBoxModel>(x.BringMailBoxData)).ToArray();

            var retCode = models.Length != 0 ? BringShippingMgr.MailBox(this, models, false, Request) : null;

            //SetViewMsg(retCode != null && retCode.Item1, Order.StringTable.BringMailBoxSubmit, retCode == null ? null : retCode.Item2);

            return JsonStatus(Order.StringTable.BringMailBoxSubmit, retCode != null && retCode.Item1, retCode == null ? null : retCode.Item2);
        }

        #endregion

        #region ListPickupPointD2

        public ActionResult ListPickupPointD2(string countryCode, string postalCode, string pickupPointType, string searchForText, int count = 30)
        {
            if (postalCode.IsNullOrEmpty() || countryCode.IsNullOrEmpty()) 
                return Json(new DDListItem[0], JsonRequestBehavior.AllowGet);

            var dimFmt = StringTable.ShippingMaxParcelDimensionFormat;
            Func<ParcelDimension, string> fDim = m =>  m == null ? null : string.Format(", " + dimFmt, new OFloat(m.Height), new OFloat(m.Length), new OFloat(m.Width));
            var lst = BringShippingMgr.GetPickupPoints(countryCode, postalCode, searchForText, null, null, count, pickupPointType).Select(m => new DDListItem()
            {
                Text = string.Format("{0}, {1}, {3} {4} {6}", m.Name, m.Address, null, m.City, m.PostalCode, m.CountryCode, m.PickupPointType.InList("Locker") ? string.Format("({0}{1})", m.PickupPointType, fDim(m.MaxParcelDimensions)) : null),
                Value = m.Id,
                Model = m,
                Active = true,
                LangVisible = true
            }).ToArray();

            return Json(lst, JsonRequestBehavior.AllowGet);
        }

        #endregion

    }
}