using System;
using System.Collections.Generic;
using System.Linq;
using Claw.Modules.Eshop.Domain;
using Claw.Modules.Eshop.Helper;
using Claw.Modules.Eshop.Models;
using Proteria = Eshop.Shipping.Proteria;
using Newtonsoft.Json;

namespace Claw.Modules.Eshop.DAL
{
    public class ProteriaMgr
    {
        #region GetPrinters

        public static Proteria.Printer[] GetPrinters(ref string errorMsg)
        {
            try
            {
                var settings = SettingsBiz.GetSettings().Display.Admin.Proteria;
                var api = new Proteria.CloudConnect(settings.ApiKey);
                return api.GetPrinters() ?? new Proteria.Printer[0];
            }
            catch (Exception err)
            {
                if (errorMsg.IsNullOrEmpty()) errorMsg = err.Message;
                return new Proteria.Printer[0];
            }
        }

        #endregion

        #region GetPickUpPoints

        /// <summary>Bring, PostNord</summary>
        public static Proteria.PickUpPoint[] GetPickUpPoints(string countryCode, string postCode, ref string errorMsg, string carrier = "Bring")
        {
            if ((!carrier.EqualsIgCase("Bring") || !countryCode.InList("SE", "NO", "DK", "FI")) &&
                (!carrier.EqualsIgCase("PostNord") || !countryCode.InList("SE", "NO", "DK", "FI", "DE", "BE", "NL", "LU")))
                return new Proteria.PickUpPoint[0];

            try
            {
                var settings = SettingsBiz.GetSettings().Display.Admin.Proteria;
                var api = new Proteria.CloudConnect(settings.ApiKey);
                return api.GetPickUpPoints(countryCode, postCode, carrier) ?? new Proteria.PickUpPoint[0];
            }
            catch (Exception err)
            {
                if (errorMsg.IsNullOrEmpty()) errorMsg = err.Message;
                return new Proteria.PickUpPoint[0];
            }
        }
 
        #endregion

        #region Booking

        public static System.Tuple<bool, string> Booking(IDB db, ProteriaBookingModel model, Controllers.Admin.BaseController controller)
        {
            if (model == null) 
                return new System.Tuple<bool, string>(false, null);

            var order = OrderMgr.GetDom(db, model.OrderId);

            var settings = SettingsBiz.GetSettings().Display.Admin.Proteria;
            var shopInfo = SettingsBiz.GetSettings().General;
            var api = new Proteria.CloudConnect(settings.ApiKey);
            var url = controller == null ? null : controller.Request.Url;
            var host = url == null ? null : url.Scheme + "://" + url.Authority;
            var webhookUrl = host == null ? null : host + "/modules/eshop/Proteria/Webhook";

            var resp = api.Booking(order.Id, model.Amount, model.ProductId,
                new[] 
                { 
                    new Proteria.Package()
                    {
                        HeightCM = model.HeightInCm, LengthCM = model.LengthInCm, WidthCM = model.WidthInCm, 
                        WeightKG = model.WeightInKg, Reference = model.Description.TrimLength(35)
                    } 
                },
                new Proteria.Address()
                {
                    Name = order.DeliverName,
                    Address1  = order.DeliverAddress, Address2 = null, 
                    City = order.DeliverPosted, PostCode = order.DeliverPostnr, Country = order.DeliverCountry.Code,
                    IsCompany = !order.CompanyName.IsNullOrEmpty(),
                    ContactPerson = new Proteria.ContactPerson()
                    {
                        Name = order.DeliverName,
                        Email = order.Customer.Email,
                        PhoneNumber = order.DeliverMobile.IfNull(order.DeliverPhone),
                    }
                },
                new Proteria.Address()
                {
                    Name = shopInfo.StoreName, 
                    Address1 = shopInfo.Address, 
                    City = shopInfo.Poststed, PostCode = shopInfo.Postnr, Country = shopInfo.Country,
                    ContactPerson = new Proteria.ContactPerson()
                    {
                        Name = shopInfo.StoreName,
                        Email = shopInfo.PublicEmail,
                        PhoneNumber = shopInfo.ContactPhone,
                    }
                }, null, model.PickupPointId, codAccountNumber: (model.ServiceCod ? model.CodAccountNumber : null),
                webhookUrl: webhookUrl, autoSend: settings.AutoSend, autoPrint: settings.AutoPrint);

            if (resp.IsNullOrEmpty())
            {
                var dom = OrderMgr.GetDom(db, order.Id);
                dom.ShippingService = (int)ShippingServiceType.Proteria;
                //dom.ShippingTrackingId = "";
                dom.ShippingData = JsonConvert.SerializeObject(new Proteria.BookingResult() { OrderId = order.Id, ShipmentId = order.Id.ToString() });
                //dom.ShippingPickupPoint = dom.ShippingPickupPoint ?? ;
                dom.Update(db.DataService);

                return new System.Tuple<bool, string>(true, null);
            }

            OrderNoteMgr.SaveorUpdate(db, new OrderNoteModel()
            {
                Subject = String.Format(StringTable.ShippingBookingResultSubjectFailed, typeof(ShippingServiceType).LocalizeEnumValue((int)ShippingServiceType.Proteria)),
                Body = String.Format(StringTable.ShippingBookingResultBodyFailed, resp),
                OrderId = order.Id,
                OrderStatus = order.OrderStatus,
                Private = true
            }, null, controller == null ? null : controller.Request);

            return new System.Tuple<bool, string>(false, resp); 
        }

        #endregion
    }
}