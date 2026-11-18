using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Claw.Modules.Eshop.Domain;
using Claw.Modules.Eshop.Helper;
using Claw.Modules.Eshop.Models;
using Logistra = Eshop.Shipping.Logistra;
using Newtonsoft.Json;

namespace Claw.Modules.Eshop.DAL
{
    public class LogistraMgr
    {
        public static volatile Func<string, string, string> FuncFixBookNumber = null;
        public static volatile Func<Domain.Order, Logistra.Address, string, Logistra.Address> FuncFilterAddress = null;

        #region GetPrinters

        public static Logistra.Printer[] GetPrinters(ref string errorMsg)
        {
            try
            {
                var settings = SettingsBiz.GetSettings().Display.Admin.Logistra;
                var api = new Logistra.Cargonizer(settings.ApiKey, settings.SenderId, settings.TestMode);
                return api.GetPrinters() ?? new Logistra.Printer[0];
            }
            catch (Exception err)
            {
                if (errorMsg.IsNullOrEmpty()) errorMsg = err.Message;
                return new Logistra.Printer[0];
            }
        }

        #endregion

        #region GetTransportAgreements

        public static Logistra.Agreement[] GetTransportAgreements()
        {
            return Helper.DataCache<Helper.DefaultScope, Logistra.Agreement[]>.Get(null,
                string.Format("LogistraMgr.GetTransportAgreements"), db =>
                {
                    var settings = SettingsBiz.GetSettings().Display.Admin.Logistra;
                    var api = new Logistra.Cargonizer(settings.ApiKey, settings.SenderId, settings.TestMode);
                    return api.GetTransportAgreements();
                }, 24 * 60 * 60, true, 60 * 60);
        }

        public static Logistra.Agreement GetTransportAgreement(int id)
        {
            return GetTransportAgreements().FirstOrDefault(x => x.Id == id);
        }

        public static Logistra.Agreement GetTransportAgreement(string name)
        {
            return GetTransportAgreements().FirstOrDefault(x => x.CarrierName.EqualsIgCase(name));
        }

        #endregion

        #region GetServicePartners

        public static Logistra.ServicePartner[] GetServicePartners(string country, string postcode, string carrier)
        {
            if (!carrier.IsNullOrEmpty())
            {
                var carriers = GetTransportAgreements().Distinct(m => m.CarrierId).ToList();
                var tmpcarrier = carriers.FirstOrDefault(m => m.CarrierId.EqualsIgCase(carrier) || m.CarrierName.StartsWith(carrier, StringComparison.OrdinalIgnoreCase)).SafeProp(m => m.CarrierId);
                carrier = tmpcarrier;
            }

            return Helper.DataCache<Helper.DefaultScope, Logistra.ServicePartner[]>.Get(null,
                string.Format("LogistraMgr.GetServicePartners::{0}::{1}::{2}", country, postcode, carrier), db =>
                {
                    var settings = SettingsBiz.GetSettings().Display.Admin.Logistra;
                    var api = new Logistra.Cargonizer(settings.ApiKey, settings.SenderId, settings.TestMode);
                    return api.GetServicePartners(country, postcode, carrier) ?? new Logistra.ServicePartner[0];
                }, 24 * 60 * 60, true, 60 * 60);
        }

        public static Logistra.ServicePartner GetServicePartner(Domain.Order order, string carrierName,
            string pickupPointId)
        {
            if (order == null) return null;

            if (!carrierName.IsNullOrEmpty())
                carrierName = GetTransportAgreements()
                    .FirstOrDefault(m => m.CarrierName.EqualsIgCase(carrierName))
                    .SafeProp(m => m.CarrierId);

            return GetServicePartners(order.DeliverCountry.Code, order.DeliverPostnr, carrierName)
                .FirstOrDefault(x => x.Number.EqualsIgCase(pickupPointId));
        }

        #endregion

        #region GetProduct
        public static Logistra.Product GetProduct(string id)
        {
            return GetTransportAgreements()
                    .SelectMany(x => x.Products.Where(p => p.Identifier.EqualsIgCase(id)))
                    .FirstOrDefault();
        } 
        #endregion

        #region Booking

        public static System.Tuple<bool, string, Logistra.BookingResult> Booking(IDB db, LogistraBookingModel model, HttpRequestBase request = null)
        {
            if (model == null) return new System.Tuple<bool, string, Logistra.BookingResult>(false, null, null);

            var order = OrderMgr.GetDom(db, model.OrderId);

            if (order == null || !order.ShippingTrackingId.IsNullOrEmpty() ||
                !order.ShippingData.IsNullOrEmpty()) return new System.Tuple<bool, string, Logistra.BookingResult>(false, null, null);

            var fAddr = FuncFilterAddress == null ? (Func<Logistra.Address, string, Logistra.Address>)((x, t) => x) : (x, t) => FuncFilterAddress(order, x, t);

            var settingsbiz = SettingsBiz.GetSettings();
            var settings = settingsbiz.Display.Admin.Logistra;
            var shopInfo = settingsbiz.General;
            var api = new Logistra.Cargonizer(settings.ApiKey, settings.SenderId, settings.TestMode);
            var transAgreement = GetTransportAgreements().FirstOrDefault(x => x.Id == model.TransportAgreementId);
            var product = GetProduct(model.ProductId);
            var sPartner = model.PickupPointId.IsNullOrEmpty() ? null : GetServicePartner(order, model.CarrierName, model.PickupPointId);
            var resp = api.Booking(order.Id, model.TransportAgreementId, model.ProductId, model.Amount, model.Currency, model.GetServices(transAgreement), 
                new[] 
                { 
                    new Logistra.Package() 
                    { 
                        Type = "package", Amount = Math.Max(1, (int)model.PackageCount), WeightInKg = model.WeightInKg, Description = model.Description,
                        Items = settingsbiz.Hidden.LogistraNvitCustoms ? order.GetNvitItems().ToArray() : null
                    } 
                },
                sPartner == null ? null : new Logistra.AddressBase()
                {
                    Number = sPartner.Number, Name = sPartner.Name, Address1 = sPartner.Address1, City = sPartner.City, Postcode = sPartner.Postcode, Country = sPartner.Country
                }, fAddr(new Logistra.Address()
                {
                    Name = order.CompanyName.IfNull(order.DeliverName),
                    ContactPerson = order.Customer.ContactName.IfNull(order.DeliverName), 
                    Address1  = order.DeliverAddress, Address2 = null, 
                    City = order.DeliverPosted, Postcode = order.DeliverPostnr, Country = order.DeliverCountry.Code,
                    Email = order.Customer.Email,
                    Mobile = order.DeliverMobile,
                    Phone = order.DeliverPhone.IsNullOrEmpty() ? order.DeliverMobile : order.DeliverPhone,
                    CustomFieldBool = product.SafeProp(m => m.ConsigneeFreightPayerRequired),
                    CustomerNumber = model.ConsigneeCustomerNumber,
                }, "consignee"), null,
                !(product.SafeProp(m => m.ThirdPartyFreightPayerRequired) ?? false) ? null : new Logistra.Address()
                {
                    Name = shopInfo.StoreName, ContactPerson = null,
                    Address1 = shopInfo.Address, Address2 = null,
                    City = shopInfo.Poststed, Postcode = shopInfo.Postnr, Country = shopInfo.Country,
                    Email = shopInfo.NotifyEmail, Mobile = null, Phone = shopInfo.ContactPhone,  
                    CustomerNumber = model.FreightPayerCustomerNumber,
                },
                !(product.SafeProp(m => m.RequiresPickupAddress) ?? false) ? null : fAddr(new Logistra.Address()
                {
                    Name = order.CompanyName.IfNull(order.DeliverName),
                    ContactPerson = order.Customer.ContactName.IfNull(order.DeliverName), 
                    Address1 = order.DeliverAddress, Address2 = null,
                    City = order.DeliverPosted, Postcode = order.DeliverPostnr, Country = order.DeliverCountry.Code,
                    Email = order.Customer.Email, Mobile = order.DeliverMobile, Phone = order.DeliverPhone,
                }, "pickupAddress"), termsOfDeliveryCode: model.TermsOfDelivery);

            if (resp != null && !resp.Number.IsNullOrEmpty())
            {
                var orderStatusTilRegnskap = SettingsBiz.GetSettings().Hidden.OrderStatusTilRegnskap;
                var dom = OrderMgr.GetDom(db, order.Id);
                var bTilRegnskap = (0 == (order.Flags & (int)Domain.Interfaces.OrderFlagType.TilRegnskap));

                dom.ShippingService = (int)ShippingServiceType.Logistra;
                dom.ShippingTrackingId = FuncFixBookNumber == null ? resp.Number: FuncFixBookNumber(resp.TrackingUrl, resp.Number);
                dom.ShippingData = JsonConvert.SerializeObject(resp);
                dom.ShippingPickupPoint = dom.ShippingPickupPoint.IfNull(null) ?? (sPartner == null ? null : JsonConvert.SerializeObject(sPartner));
                
                if (orderStatusTilRegnskap) 
                    dom.Flags |= (int)Domain.Interfaces.OrderFlagType.TilRegnskap;

                var ret = dom.Update(db.DataService);

                if(ret) OrderNoteMgr.SaveorUpdate(db, new OrderNoteModel()
                {
                    Subject = String.Format(StringTable.ShippingBookingResultSubjectOk, typeof(ShippingServiceType).LocalizeEnumValue(dom.ShippingService ?? 0)),
                    Body = String.Format(StringTable.ShippingBookingResultBodyOk, resp.Number, resp.TrackingUrl),
                    OrderId = order.Id,
                    OrderStatus = order.OrderStatus,
                    Private = true
                }, null, request);

                if (ret && bTilRegnskap && orderStatusTilRegnskap) 
                    OrderNoteMgr.SaveorUpdate(db, new OrderNoteModel()
                {
                    Subject = Order.StringTable.OrderTilRegnskapSet,
                    Body = "",
                    OrderId = order.Id,
                    OrderStatus = order.OrderStatus,
                    Private = true
                }, null, request);

                return new System.Tuple<bool, string, Logistra.BookingResult>(true, null, null);
            }

            var errMsg = resp == null || resp.Errors == null ? null : resp.Errors.ToString(";\r\n");

            OrderNoteMgr.SaveorUpdate(db, new OrderNoteModel()
            {
                Subject = String.Format(StringTable.ShippingBookingResultSubjectFailed, typeof(ShippingServiceType).LocalizeEnumValue((int)ShippingServiceType.Logistra)),
                Body = String.Format(StringTable.ShippingBookingResultBodyFailed, errMsg),
                OrderId = order.Id,
                OrderStatus = order.OrderStatus,
                Private = true
            }, null, request);

            return new System.Tuple<bool, string, Logistra.BookingResult>(false, errMsg, resp);
        }

        #endregion

        #region BookingBundle

        public static List<System.Tuple<int, bool, string, Logistra.BookingResult>> BookingBundle(IDB db, LogistraBookingModel model, HttpRequestBase request = null)
        {
            if (model == null || model.Items.SafeCount() == 0) return null;

            var settingsbiz = SettingsBiz.GetSettings();
            var settings = settingsbiz.Display.Admin.Logistra;
            var shopInfo = settingsbiz.General;
            var api = new Logistra.Cargonizer(settings.ApiKey, settings.SenderId, settings.TestMode);
            var transAgreement = GetTransportAgreements().FirstOrDefault(x => x.Id == model.TransportAgreementId);
            var product = GetProduct(model.ProductId);
            var dicOrder = new Dictionary<int, System.Tuple<Domain.Order, Logistra.ServicePartner>>();

            var consignments = model.Items.Select(item =>
            {
                var order = OrderMgr.GetDom(db, item.OrderId);

                if (order == null || !order.ShippingTrackingId.IsNullOrEmpty() || !order.ShippingData.IsNullOrEmpty()) return null;

                var sPartner = item.PickupPointId.IsNullOrEmpty() ? null : GetServicePartner(order, model.CarrierName, item.PickupPointId);

                dicOrder.Add(item.OrderId, new System.Tuple<Domain.Order, Logistra.ServicePartner>(order, sPartner));

                var fAddr = FuncFilterAddress == null ? (Func<Logistra.Address, string, Logistra.Address>)((x, t) => x) : (x, t) => FuncFilterAddress(order, x, t);

                return api.Consignment(order.Id, model.TransportAgreementId, model.ProductId, item.Amount,
                    item.Currency, model.GetServices(transAgreement),
                    new[] 
                    { 
                        new Logistra.Package() 
                        { 
                            Type = "package", Amount = Math.Max(1, (int)model.PackageCount), WeightInKg = item.WeightInKg, Description = item.Description,
                            Items = settingsbiz.Hidden.LogistraNvitCustoms ? order.GetNvitItems().ToArray() : null
                        }
                    },
                    sPartner == null ? null : new Logistra.AddressBase()
                    {
                        Number = sPartner.Number, Name = sPartner.Name,
                        Address1 = sPartner.Address1, City = sPartner.City,
                        Postcode = sPartner.Postcode, Country = sPartner.Country
                    }, fAddr(new Logistra.Address()
                    {
                        Name = order.CompanyName.IfNull(order.DeliverName),
                        ContactPerson = order.Customer.ContactName.IfNull(order.DeliverName), 
                        Address1 = order.DeliverAddress, Address2 = null, City = order.DeliverPosted,
                        Postcode = order.DeliverPostnr, Country = order.DeliverCountry.Code,
                        Email = order.Customer.Email, Mobile = order.DeliverMobile,
                        Phone = order.DeliverPhone.IsNullOrEmpty() ? order.DeliverMobile : order.DeliverPhone,
                        CustomFieldBool = product.SafeProp(m => m.ConsigneeFreightPayerRequired),
                        CustomerNumber = model.ConsigneeCustomerNumber,
                    }, "consignee"), null,
                    !(product.SafeProp(m => m.ThirdPartyFreightPayerRequired) ?? false) ? null : new Logistra.Address()
                    {
                        Name = shopInfo.StoreName, ContactPerson = null,
                        Address1 = shopInfo.Address, Address2 = null, City = shopInfo.Poststed,
                        Postcode = shopInfo.Postnr, Country = shopInfo.Country,
                        Email = shopInfo.NotifyEmail, Mobile = null, Phone = shopInfo.ContactPhone,
                        CustomerNumber = model.FreightPayerCustomerNumber,
                    },
                    !(product.SafeProp(m => m.RequiresPickupAddress) ?? false) ? null : fAddr(new Logistra.Address()
                    {
                        Name = order.CompanyName.IfNull(order.DeliverName),
                        ContactPerson = order.Customer.ContactName.IfNull(order.DeliverName), 
                        Address1 = order.DeliverAddress, Address2 = null, City = order.DeliverPosted,
                        Postcode = order.DeliverPostnr, Country = order.DeliverCountry.Code,
                        Email = order.Customer.Email, Mobile = order.DeliverMobile, Phone = order.DeliverPhone,
                    }, "pickupAddress"), termsOfDeliveryCode: model.TermsOfDelivery);
            }).Where(m => m != null).ToArray();

            var lst = api.Booking(consignments);

            Func<Logistra.BookingResult, System.Tuple<int, bool, string, Logistra.BookingResult>> fOk = resp =>
            {
                var data = dicOrder[resp.OrderId.TryParseInt() ?? 0];
                var order = data.Item1;
                var sPartner = data.Item2;
                var orderStatusTilRegnskap = SettingsBiz.GetSettings().Hidden.OrderStatusTilRegnskap;
                var dom = OrderMgr.GetDom(db, order.Id);
                var bTilRegnskap = (0 == (order.Flags & (int) Domain.Interfaces.OrderFlagType.TilRegnskap));

                dom.ShippingService = (int) ShippingServiceType.Logistra;
                dom.ShippingTrackingId = FuncFixBookNumber == null ? resp.Number : FuncFixBookNumber(resp.TrackingUrl, resp.Number);
                dom.ShippingData = JsonConvert.SerializeObject(resp);
                dom.ShippingPickupPoint = dom.ShippingPickupPoint.IfNull(null) ??  (sPartner == null ? null : JsonConvert.SerializeObject(sPartner));

                if (orderStatusTilRegnskap)
                    dom.Flags |= (int) Domain.Interfaces.OrderFlagType.TilRegnskap;

                var ret = dom.Update(db.DataService);

                if (ret) OrderNoteMgr.SaveorUpdate(db, new OrderNoteModel()
                {
                    Subject = String.Format(StringTable.ShippingBookingResultSubjectOk, typeof (ShippingServiceType).LocalizeEnumValue(dom.ShippingService ?? 0)),
                    Body = String.Format(StringTable.ShippingBookingResultBodyOk, resp.Number, resp.TrackingUrl),
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

                return new System.Tuple<int, bool, string, Logistra.BookingResult>(order.Id, true, null, null);
            };

            Func<Logistra.BookingResult, System.Tuple<int, bool, string, Logistra.BookingResult>> fFailed = resp =>
            {
                var data = dicOrder[resp.OrderId.TryParseInt() ?? 0];
                var order = data.Item1;
                var errMsg = resp.Errors == null ? null : resp.Errors.ToString(";\r\n");

                OrderNoteMgr.SaveorUpdate(db, new OrderNoteModel()
                {
                    Subject = String.Format(StringTable.ShippingBookingResultSubjectFailed, typeof (ShippingServiceType).LocalizeEnumValue((int) ShippingServiceType.Logistra)),
                    Body = String.Format(StringTable.ShippingBookingResultBodyFailed, errMsg),
                    OrderId = order.Id,
                    OrderStatus = order.OrderStatus,
                    Private = true
                }, null, request);

                return new System.Tuple<int, bool, string, Logistra.BookingResult>(order.Id, false, errMsg, resp);
            };

            return lst == null ? null : lst.Select(resp =>
            {
                try
                {
                    return !resp.Number.IsNullOrEmpty() 
                        ? fOk(resp) 
                        : (resp.OrderId.IsNullOrEmpty() && resp.Errors.SafeCount() != 0
                            ? new System.Tuple<int, bool, string, Logistra.BookingResult>(0, false, resp.Errors.ToString("; "), resp) 
                            : fFailed(resp));
                }
                catch (Exception err)
                {
                    return new System.Tuple<int, bool, string, Logistra.BookingResult>(resp.OrderId.TryParseInt() ?? 0, false, err.Message, resp);
                }
            }).ToList();
        }

        #endregion
   
    }
}