using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Claw.Modules.Eshop.Domain;
using Claw.Modules.Eshop.Helper;
using Claw.Modules.Eshop.Models;
using NHibernate.Mapping;
using Postnord = Eshop.Shipping.Postnord;
using Newtonsoft.Json;

namespace Claw.Modules.Eshop.DAL
{
    public class PostnordMgr
    {
        #region ServiceCode

        public class ServiceCode
        {
            public string Code { get; set; }
            public string Name { get; set; }
            public Postnord.Data.AdnlServiceCodeCombDetail[] AdditonalServices { get; set; }

            public bool IsOptionalServicePoint
            {
                get { return AdditonalServices != null && AdditonalServices.Any(m => m.adnlServiceCode.InList("A7")); }
            }

            public string[] Services
            {
                get { return AdditonalServices == null ? new string[0] : AdditonalServices.Select(m => m.adnlServiceCode).ToArray(); }
            }
        } 

        #endregion

        #region GetServices

        public static ServiceCode[] GetServices(string countryCode = null)
        {
            return Helper.DataCache<Helper.DefaultScope, ServiceCode[]>.Get(null,
                string.Format("PostnordMgr.GetServices::{0}", countryCode), db =>
                {
                    var settings = SettingsBiz.GetSettings().Display.Admin.Postnord;
                    var api = new Postnord.PostnordApi(settings.ApiKey, settings.TestMode, settings.CustomerId);
                    var services = api.GetServices();

                    return services == null ? new ServiceCode[0] : services.SelectMany(m => m.adnlServiceCodeCombDetails)
                        .Where(m => countryCode.IsNullOrEmpty() || (m.allowedConsigneeCountry.InList(countryCode, "ALL") && m.allowedConsignorCountry.InList(countryCode, "ALL")))
                        .GroupBy(m => m.serviceCode, (key, lst) =>
                        {
                            var tmp = lst.Distinct(z => z.adnlServiceCode).ToArray();
                            return new ServiceCode() { Code = key, Name = tmp[0].serviceName, AdditonalServices = tmp };
                        })
                        .ToArray();
                }, 24 * 60 * 60, true, 60 * 60);
        }

        #endregion

        #region GetPickupPoints

        public static Postnord.Data.PickupPoint[] GetPickupPoints(string countryCode, string postalCode = null, string streetName = null,
            string typeId = null, int count = 20, string id = null)
        {
            return Helper.DataCache<Helper.DefaultScope, Postnord.Data.PickupPoint[]>.Get(null,
                string.Format("PostnordMgr.GetPickupPoints::{0}::{1}::{2}::{3}::{4}::{5}", countryCode, postalCode, streetName, typeId, count, id), db =>
                {
                    var settings = SettingsBiz.GetSettings().Display.Admin.Postnord;
                    var api = new Postnord.PostnordApi(settings.ApiKey, settings.TestMode, settings.CustomerId);
                    return api.GetServicePoints(countryCode, postalCode, streetName, null, typeId, count, id) ?? new Postnord.Data.PickupPoint[0];
                }, 24 * 60 * 60, true, 60 * 60);
        }

        #endregion

        #region Booking

        public static System.Tuple<bool, string, Postnord.BookingResult> Booking(IDB db, PostnordBookingModel model, HttpRequestBase request = null)
        {
            if (model == null) return new System.Tuple<bool, string, Postnord.BookingResult>(false, null, null);

            var order = OrderMgr.GetDom(db, model.OrderId);

            if (order == null || !order.ShippingTrackingId.IsNullOrEmpty() ||
                !order.ShippingData.IsNullOrEmpty()) return new System.Tuple<bool, string, Postnord.BookingResult>(false, null, null);

            var settings = SettingsBiz.GetSettings().Display.Admin.Postnord;
            var shopInfo = SettingsBiz.GetSettings().General;
            var api = new Postnord.PostnordApi(settings.ApiKey, settings.TestMode, settings.CustomerId, null, settings.ApplicationId, settings.ApplicationName);
            var sPartner = model.PickupPointId.IsNullOrEmpty() ? null : api.GetServicePoints(order.DeliverCountry.Code, id: model.PickupPointId).SafeProp(m => m.FirstOrDefault());
            var contactName = order.PaymentMethod.PaymentSystemName.InList(GatewayID.Svea.ToString())
                 ? order.DeliverName.GetSveaCoName().IfNull(order.Customer.ContactName).IfNull(order.DeliverName)
                 : order.Customer.ContactName.IfNull(order.DeliverName);
            var resp = api.Booking(order.DeliverCountry.Code, order.Id, model.Amount, model.ProductId,
                new[] { model.ServiceCod ? "A1" : null, model.ServiceInsurance ? "A5" : null, model.NotiSms ? "A3" : null, model.NotiEmail ? "A4" : null }.Where(m => m != null).ToArray(), 
                new Postnord.Package()
                {
                    WidthCM = model.WidthInCm, HeightCM = model.HeightInCm, LengthCM = model.LengthInCm,
                    WeightKG = model.WeightInKg, Description = model.Description
                }, new Postnord.Data.Party()
                {
                    address = new Postnord.Data.Address()
                    {
                        streets = new[] { order.DeliverAddress },
                        postalCode = order.DeliverPostnr,
                        city = order.DeliverPosted,
                        countryCode = order.DeliverCountry.Code
                    },
                    nameIdentification = new Postnord.Data.NameId() { name = order.CompanyName.IfNull(order.DeliverName) },
                    contact = new Postnord.Data.Contact()
                    {
                        contactName = contactName, emailAddress = order.Customer.Email, 
                        phoneNo = order.DeliverPhone.IfNull(order.DeliverMobile), 
                        smsNo = model.NotiSms ? order.DeliverMobile : null
                    },
                },
                new Postnord.Data.Party()
                {
                    address = new Postnord.Data.Address()
                    {
                        streets = new[] { shopInfo.Address },
                        postalCode = shopInfo.Postnr,
                        city = shopInfo.Poststed,
                        countryCode = shopInfo.Country
                    },
                    nameIdentification = new Postnord.Data.NameId() { name = shopInfo.StoreName },
                    contact = new Postnord.Data.Contact()
                    {
                        emailAddress = shopInfo.NotifyEmail,
                        phoneNo = shopInfo.ContactPhone,
                    },
                }, model.PickupPointId, model.ServiceCod ? model.CodAccountNumber : null, model.Currency);

            if (resp != null && !resp.ShipmentId.IsNullOrEmpty())
            {
                var orderStatusTilRegnskap = SettingsBiz.GetSettings().Hidden.OrderStatusTilRegnskap;
                var dom = OrderMgr.GetDom(db, order.Id);
                var bTilRegnskap = (0 == (order.Flags & (int)Domain.Interfaces.OrderFlagType.TilRegnskap));

                dom.ShippingService = (int)ShippingServiceType.Postnord;
                dom.ShippingTrackingId =  resp.TrackingNumber;
                dom.ShippingData = JsonConvert.SerializeObject(resp);
                dom.ShippingPickupPoint = dom.ShippingPickupPoint.IfNull(null) ?? (sPartner == null ? null : JsonConvert.SerializeObject(sPartner));
                
                if (orderStatusTilRegnskap) 
                    dom.Flags |= (int)Domain.Interfaces.OrderFlagType.TilRegnskap;

                var ret = dom.Update(db.DataService);

                if(ret) OrderNoteMgr.SaveorUpdate(db, new OrderNoteModel()
                {
                    Subject = String.Format(StringTable.ShippingBookingResultSubjectOk, typeof(ShippingServiceType).LocalizeEnumValue(dom.ShippingService ?? 0)),
                    Body = String.Format(StringTable.ShippingBookingResultBodyOk, resp.ShipmentId, resp.TrackingUrl),
                    OrderId = order.Id,
                    OrderStatus = order.OrderStatus,
                    Private = true
                }, null, request);

                if (ret && bTilRegnskap && orderStatusTilRegnskap) OrderNoteMgr.SaveorUpdate(db, new OrderNoteModel()
                {
                    Subject = Order.StringTable.OrderTilRegnskapSet,
                    Body = "",
                    OrderId = order.Id,
                    OrderStatus = order.OrderStatus,
                    Private = true
                }, null, request);

                return new System.Tuple<bool, string, Postnord.BookingResult>(true, null, null);
            }

            var errMsg = resp == null ? null : resp.Error;

            OrderNoteMgr.SaveorUpdate(db, new OrderNoteModel()
            {
                Subject = String.Format(StringTable.ShippingBookingResultSubjectFailed, typeof(ShippingServiceType).LocalizeEnumValue((int)ShippingServiceType.Postnord)),
                Body = String.Format(StringTable.ShippingBookingResultBodyFailed, errMsg),
                OrderId = order.Id,
                OrderStatus = order.OrderStatus,
                Private = true
            }, null, request);

            return new System.Tuple<bool, string, Postnord.BookingResult>(false, errMsg, resp);
        }

        #endregion

        #region BookingBundle

        public static List<System.Tuple<int, bool, string, Postnord.BookingResult>> BookingBundle(IDB db, PostnordBookingModel model, HttpRequestBase request = null)
        {
            //if (model == null || model.Items.SafeCount() == 0) 
                return null;
        }

        #endregion
    }
}