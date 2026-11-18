using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Web;
using Claw.Core;
using Claw.Core.Logs;
using Claw.Modules.Eshop.Helper;
using Claw.Modules.Eshop.Models;
using Eshop.Shipping.Bring;
using Eshop.Shipping.BaseProvider;
using Bring = Eshop.Shipping.BaseProvider;
using Newtonsoft.Json;
using BringDataV2 = Eshop.Shipping.Bring.Data.V2;

namespace Claw.Modules.Eshop.DAL
{
    public class BringShippingMgr
    {
        public static volatile Func<Domain.Order, string, string, string> FuncFilterAddress = null;

        #region GetPickupPoints

        public static PickupPoint[] GetPickupPoints(string countryCode, string postCode, string searchForText = null, string street = null, string streetNumber = null, int count = 30, string pickupPointType = null)
        {
            var settings = SettingsBiz.GetSettings().Display.Admin.BringShipping;
            return new BringStandardShippingProcessor().GetPickupPoints(settings.ApiUid, settings.ApiKey, countryCode, postCode, searchForText, street, streetNumber, count, pickupPointType);
        }

        #endregion

        #region GetCustomers

        public static Bring.Customer[] GetCustomers(Uri url)
        {
            return Helper.DataCache<Helper.DefaultScope, Bring.Customer[]>.Get(null,
                string.Format("LogistraMgr.GetTransportAgreements"), db =>
                {
                    var settings = SettingsBiz.GetSettings().Display.Admin.BringShipping;
                    return new BringStandardShippingProcessor().GetCustomers(settings.ApiUid, settings.ApiKey, url == null ? null : url.AbsoluteUri);
                }, 24 * 60 * 60, true, 60 * 60);
        } 

        #endregion

        #region Booking.Bundle

        public static List<Tuple<int, bool, string>> BookingBundle(IDB db, BringBookingModel model, HttpRequestBase request)
        {
            if (model == null || model.Items.SafeCount() == 0) return null;

            if (model.Items.Any(m => OrderMgr.GetDom(db, m.OrderId) == null)) return null;

            var settingsbiz = SettingsBiz.GetSettings();
            var shopInfo = settingsbiz.General;
            var settings = settingsbiz.Display.Admin.BringShipping;
            var recvNoti = BringStandardShippingProcessor.IsRecvNotiProduct(model.ProductId);
            var cod = BringStandardShippingProcessor.IsCodProduct(model.ProductId);
            var service1081 = BringStandardShippingProcessor.HasService(model.ProductId, "1081");
            var service9650 = BringStandardShippingProcessor.HasService(model.ProductId, "9650");

            var url = request == null ? null : request.Url;
            var shippingDate = DateTime.Now.AddMinutes(15);
            var api = new BookingApi(settings.TestMode(), settings.ApiUid, settings.ApiKey, url == null ? null : url.AbsoluteUri);
            var dicOrder = new Dictionary<int, Domain.Order>();

            var consignments = model.Items.Select(m => 
            {
                var order = OrderMgr.GetDom(db, m.OrderId);

                if (order == null || !order.ShippingTrackingId.IsNullOrEmpty() || !order.BringBookingData.IsNullOrEmpty() || !order.ShippingData.IsNullOrEmpty()) 
                    return null;

                dicOrder.Add(m.OrderId, order);

                var fAddr = FuncFilterAddress == null ? (Func<string, string, string>)((x, t) => x) : (x, t) => FuncFilterAddress(order, x, t);
                var contactName = order.PaymentMethod.PaymentSystemName.InList(GatewayID.Svea.ToString())
                    ? order.DeliverName.GetSveaCoName().IfNull(order.Customer.ContactName)
                    : order.Customer.ContactName;
                return api.Consignment(m.OrderId.ToString(), shippingDate, model.ProductId, model.CustomerId, new[]
			    {
				    new BookingPackage()
				    {                                
					    WidthInCm = model.WidthInCm,
					    HeightInCm = model.HeightInCm,
					    LengthInCm = model.LengthInCm,

					    WeightInKg = m.WeightInKg,
					    NumberOfItems = m.NumberOfItems,
					    GoodsDescription = m.GoodsDescription,
				    },
			    }, shopInfo.StoreName, shopInfo.Address, shopInfo.Postnr, shopInfo.Poststed, shopInfo.Country, shopInfo.NotifyEmail,
                shopInfo.ContactPhone.TrimPhone(), m.OrderId.ToString(),
                fAddr(JsonConvert.SerializeObject(new
                {
                    Name = !order.CompanyName.IsNullOrEmpty() && !contactName.IsNullOrEmpty() ? contactName : order.DeliverName,                  
                    Company = order.CompanyName,
                    //CO = order.CompanyName.IsNullOrEmpty() ? null : order.Customer.ContactName
                }), "recipientName"), order.DeliverAddress, order.DeliverPostnr, order.DeliverPosted, order.DeliverCountry.Code, order.Customer.Email,
                order.DeliverMobile.IfNull(order.DeliverPhone).TrimPhone(), string.Format("Ordre {0} kjøpt den {1:dd.MM.yyyy}", m.OrderId, order.CreatedDate).TrimLength(35), null,
                cod && m.ServiceCod ? new CashOnDelivery()
                {
                    AccountNumber = model.CodAccountNumber,
                    Amount = (double)Math.Round(m.Amount, 2),
                    CustomerId = string.Format("{0:D7}{1:D8}", order.Customer.Id, order.Invoice.SafeProp(z => z.Id)).ToKid(),
                    CurrencyCode = m.Currency,
                } : null,
                recvNoti && model.RecipientNoti ? new RecipientNotification()
                {
                    Email = m.RecipientNotiEmail,
                    Mobile = m.RecipientNotiMobile.TrimPhone(),
                } : null, service1081 && model.Service1081, service9650 && model.Service9650, model.Service9650A1073, null/*m.PickupPointId*/,
                settingsbiz.Hidden.BringNvitCustoms ? new BringDataV2.CustomsInformation()
                {
                    consent = true,
                    natureOfCargo = new BringDataV2.NatureOfCargo { type = "SALE_OF_GOODS" },
                    customsDeclarations = order.GetNvitItems().ToArray().Select(z => new BringDataV2.CustomsDeclaration()
                    {
                        customsArticleNumber = z.NvitCode,
                        quantity = z.Quantity,
                        currency = order.Currency,
                        goodsDescription = z.Description,
                        amount = z.Amount,
                        grossWeight = z.WeightInKg ?? 0,
                        countryCodeOrigin = z.OriginCountry
                    }).ToArray(),
                } : null);
            }).Where(m => m != null).ToArray();

            Func<BookingResult,Tuple<int, bool, string>> fOk = resp =>
            {
                var order = dicOrder[resp.OrderId];
                var orderStatusTilRegnskap = SettingsBiz.GetSettings().Hidden.OrderStatusTilRegnskap;
                var bTilRegnskap = (0 == (order.Flags & (int)Domain.Interfaces.OrderFlagType.TilRegnskap));

                order.ShippingService = (int)ShippingServiceType.Bring;
                order.BringBookingData = JsonConvert.SerializeObject(resp);
                order.ShippingTrackingId = resp.ConsignmentNumber;

                var pickupPoint = model.PickupPointId.IsNullOrEmpty()
                    ? null
                    : new BringStandardShippingProcessor().GetPickupPoint(settings.ApiUid, settings.ApiKey, order.DeliverCountry.Code, model.PickupPointId);
                order.ShippingPickupPoint = order.ShippingPickupPoint ?? (pickupPoint == null ? null : JsonConvert.SerializeObject(pickupPoint));

                if (orderStatusTilRegnskap)
                    order.Flags |= (int)Domain.Interfaces.OrderFlagType.TilRegnskap;

                model.ConsignmentNumber = resp.ConsignmentNumber;
                model.LabelUrl = resp.LabelUrl;
                model.TrackingUrl = resp.TrackingUrl;
                model.WaybillUrl = resp.WaybillUrl;
                model.PackageNumber = resp.Packages == null || resp.Packages.Length == 0 ? "" : resp.Packages[0].PackageNumber;
                model.EarliestPickup = resp.EarliestPickup == null || resp.EarliestPickup <= DateTime.MinValue ? null : resp.EarliestPickup;
                model.ExpectedDelivery = resp.ExpectedDelivery == null || resp.ExpectedDelivery <= DateTime.MinValue ? null : resp.ExpectedDelivery;

                var ret = BaseMgr.SaveOrUpdate(db, true, (sess, obj) => true, order);

                if (ret) OrderNoteMgr.SaveorUpdate(db, new OrderNoteModel()
                {
                    Subject = String.Format(StringTable.ShippingBookingResultSubjectOk, typeof(ShippingServiceType).LocalizeEnumValue(order.ShippingService ?? 0)),
                    Body = string.Format(Order.StringTable.BringBookingLogTempl, resp.ConsignmentNumber, resp.LabelUrl, model.PackageNumber, model.EarliestPickup, model.ExpectedDelivery),
                    Private = true,
                    Flags = 0,
                    OrderId = model.OrderId,
                    OrderStatus = order.OrderStatus,
                }, null, request);

                if (ret && bTilRegnskap && orderStatusTilRegnskap) OrderNoteMgr.SaveorUpdate(db, new OrderNoteModel()
                {
                    Subject = Order.StringTable.OrderTilRegnskapSet,
                    Body = "",
                    OrderId = order.Id,
                    OrderStatus = order.OrderStatus,
                    Private = true
                }, null, request);

                return new Tuple<int, bool, string>(resp.OrderId, ret, null);
            };
            
            Func<BookingResult, Tuple<int, bool, string>> fErr = resp =>
            {
                var order = dicOrder[resp.OrderId];
                var errMsg = resp.Errors.ToString(".<br/>");

                if (order != null)
                    OrderNoteMgr.SaveorUpdate(db, new OrderNoteModel()
                    {
                        Subject = String.Format(StringTable.ShippingBookingResultSubjectFailed, typeof (ShippingServiceType).LocalizeEnumValue((int) ShippingServiceType.Bring)),
                        Body = String.Format(StringTable.ShippingBookingResultBodyFailed, errMsg),
                        OrderId = order.Id,
                        OrderStatus = order.OrderStatus,
                        Private = true
                    }, null, request);

                return new Tuple<int, bool, string>(resp.OrderId, false, errMsg);
            };

            var retLst = consignments.Length == 0 ? null : api.Booking(settings.ApiUid, settings.ApiKey, url == null ? null : url.AbsoluteUri,
                settings.TestMode(), 1, consignments);

            return retLst == null ? null : retLst.Select(resp =>
            {
                try
                {
                    if(resp.OrderId > 0) return resp.Errors == null || resp.Errors.Length == 0 ? fOk(resp) : fErr(resp);
                    else return new Tuple<int, bool, string>(0, false, resp.Errors.SafeCount() == 0 ? "Unknown error!" : resp.Errors.ToString("; "));
                }
                catch (Exception err)
                {
                    if (Debugger.IsAttached) Debugger.Break();
                    LogHelper.Log4net(err, ClawExceptionType.DataProvider, "");

                    return new Tuple<int, bool, string>(resp.OrderId, false, err.Message);
                }
            }).ToList();
        }

        #endregion	

        #region Booking

        public static System.Tuple<bool, string> Booking(IDB db, BringBookingModel model, HttpRequestBase request)
        {
            var order = OrderMgr.GetDom(db, model == null ? 0 : model.OrderId);

            if (order == null || model == null) return new Tuple<bool, string>(false, null);

            var settingsbiz = SettingsBiz.GetSettings();
            var shopInfo = settingsbiz.General;
            var settings = settingsbiz.Display.Admin.BringShipping;
            var recvNoti = BringStandardShippingProcessor.IsRecvNotiProduct(model.ProductId);
            var cod = BringStandardShippingProcessor.IsCodProduct(model.ProductId);
            var service1081 = BringStandardShippingProcessor.HasService(model.ProductId, "1081");
            var service9650 = BringStandardShippingProcessor.HasService(model.ProductId, "9650");

            var url = request == null ? null : request.Url;
            var shippingDate = DateTime.Now.AddMinutes(15);

            var fAddr = FuncFilterAddress == null ? (Func<string, string, string>)((x, t) => x) : (x, t) => FuncFilterAddress(order, x, t);
            var contactName = order.PaymentMethod.PaymentSystemName.InList(GatewayID.Svea.ToString())
                ? order.DeliverName.GetSveaCoName().IfNull(order.Customer.ContactName)
                : order.Customer.ContactName;
            var resp = new BringStandardShippingProcessor().BookingEx3(settings.ApiUid, settings.ApiKey, url == null ? null : url.AbsoluteUri,
                settings.TestMode(), 1, model.OrderId.ToString(), shippingDate, model.ProductId, model.CustomerId, new[]
				{
					new BookingPackage()
					{                                
						WidthInCm = model.WidthInCm,
						HeightInCm = model.HeightInCm,
						LengthInCm = model.LengthInCm,
						WeightInKg = (double)model.WeightInKg,
						NumberOfItems = model.NumberOfItems,
						GoodsDescription = model.GoodsDescription,
					},
				}, shopInfo.StoreName, shopInfo.Address, shopInfo.Postnr, shopInfo.Poststed, shopInfo.Country, shopInfo.NotifyEmail,
                shopInfo.ContactPhone.TrimPhone(), model.OrderId.ToString(),
                fAddr(JsonConvert.SerializeObject(new
                {
                    Name = !order.CompanyName.IsNullOrEmpty() && !contactName.IsNullOrEmpty() ? contactName : order.DeliverName,
                    Company = order.CompanyName,
                    //CO = order.CompanyName.IsNullOrEmpty() ? null : order.Customer.ContactName
                }), "recipientName"), order.DeliverAddress, order.DeliverPostnr, order.DeliverPosted, order.DeliverCountry.Code, order.Customer.Email,
                order.DeliverMobile.IfNull(order.DeliverPhone).TrimPhone(), string.Format("Ordre {0} kjøpt den {1:dd.MM.yyyy}", model.OrderId, order.CreatedDate).TrimLength(35), null,
                cod && model.ServiceCod ? new CashOnDelivery()
                {
                    AccountNumber = model.CodAccountNumber,
                    Amount = (double)Math.Round(model.Amount, 2),
                    CustomerId = string.Format("{0:D7}{1:D8}", order.Customer.Id, order.Invoice.SafeProp(m => m.Id)).ToKid(),
                    CurrencyCode =  model.Currency,
                } : null,
                recvNoti && model.RecipientNoti ? new RecipientNotification()
                {
                    Email = model.RecipientNotiEmail,
                    Mobile = model.RecipientNotiMobile.TrimPhone(),
                } : null, service1081 && model.Service1081, service9650 && model.Service9650, model.Service9650A1073, model.PickupPointId,
                settingsbiz.Hidden.BringNvitCustoms ? new BringDataV2.CustomsInformation()
                {
                    consent = true, voec = settingsbiz.Hidden.BringNvitVoec,
                    natureOfCargo = new BringDataV2.NatureOfCargo { type = "SALE_OF_GOODS"  },
                    customsDeclarations = order.GetNvitItems().Select(m => new BringDataV2.CustomsDeclaration() 
                    { 
                        customsArticleNumber = m.NvitCode,
                        quantity = m.Quantity,
                        currency = order.Currency,
                        goodsDescription = m.Description,
                        amount = m.Amount,
                        grossWeight = m.WeightInKg ?? 0,
                        countryCodeOrigin = m.OriginCountry,
                    }).ToArray(),
                } : null);
          
            if (resp.Errors == null || resp.Errors.Length == 0)
            {
                var orderStatusTilRegnskap = SettingsBiz.GetSettings().Hidden.OrderStatusTilRegnskap;
                var bTilRegnskap = (0 == (order.Flags & (int)Domain.Interfaces.OrderFlagType.TilRegnskap));

                order.ShippingService = (int)ShippingServiceType.Bring;
                order.BringBookingData = JsonConvert.SerializeObject(resp);
                order.ShippingTrackingId = resp.ConsignmentNumber;

                var pickupPoint = model.PickupPointId.IsNullOrEmpty()
                    ? null
                    : new BringStandardShippingProcessor().GetPickupPoint(settings.ApiUid, settings.ApiKey, order.DeliverCountry.Code, model.PickupPointId);
                order.ShippingPickupPoint = order.ShippingPickupPoint ?? (pickupPoint == null ? null : JsonConvert.SerializeObject(pickupPoint));
                    
                if (orderStatusTilRegnskap) 
                    order.Flags |= (int)Domain.Interfaces.OrderFlagType.TilRegnskap;

                model.ConsignmentNumber = resp.ConsignmentNumber;
                model.LabelUrl = resp.LabelUrl;
                model.TrackingUrl = resp.TrackingUrl;
                model.WaybillUrl = resp.WaybillUrl;
                model.PackageNumber = resp.Packages == null || resp.Packages.Length == 0 ? "" : resp.Packages[0].PackageNumber;
                model.EarliestPickup = resp.EarliestPickup == null || resp.EarliestPickup <= DateTime.MinValue ? null : resp.EarliestPickup;
                model.ExpectedDelivery = resp.ExpectedDelivery == null || resp.ExpectedDelivery <= DateTime.MinValue ? null : resp.ExpectedDelivery;

                var ret = BaseMgr.SaveOrUpdate(db, true, (sess, obj) => true, order);

                if(ret) OrderNoteMgr.SaveorUpdate(db, new OrderNoteModel()
                {
                    Subject = String.Format(StringTable.ShippingBookingResultSubjectOk, typeof(ShippingServiceType).LocalizeEnumValue(order.ShippingService ?? 0)),
                    Body = string.Format(Order.StringTable.BringBookingLogTempl, resp.ConsignmentNumber, resp.LabelUrl, model.PackageNumber, model.EarliestPickup, model.ExpectedDelivery),
                    Private = true, Flags = 0,
                    OrderId = model.OrderId,
                    OrderStatus = order.OrderStatus,
                }, null, request);

                if (ret && bTilRegnskap && orderStatusTilRegnskap) OrderNoteMgr.SaveorUpdate(db, new OrderNoteModel()
                {
                    Subject = Order.StringTable.OrderTilRegnskapSet,
                    Body = "",
                    OrderId = order.Id,
                    OrderStatus = order.OrderStatus,
                    Private = true
                }, null, request);

                return new Tuple<bool, string>(ret, null);
            }
            else
            {
                var errMsg = resp.Errors.ToString(".<br/>");
                OrderNoteMgr.SaveorUpdate(db, new OrderNoteModel()
                {
                    Subject = String.Format(StringTable.ShippingBookingResultSubjectFailed, typeof(ShippingServiceType).LocalizeEnumValue((int)ShippingServiceType.Bring)),
                    Body = String.Format(StringTable.ShippingBookingResultBodyFailed, errMsg),
                    OrderId = order.Id,
                    OrderStatus = order.OrderStatus,
                    Private = true
                }, null, request);

                return new Tuple<bool, string>(false, errMsg);
            }
        }

        public static System.Tuple<bool, string> Booking(IDB db, ShippingBookingModel model, HttpRequestBase request)
        {           
            if (model == null) return new Tuple<bool, string>(false, null);

            var shopInfo = SettingsBiz.GetSettings().General;
            var settings = SettingsBiz.GetSettings().Display.Admin.BringShipping;
            var recvNoti = BringStandardShippingProcessor.IsRecvNotiProduct(model.ProductId);
            var cod = BringStandardShippingProcessor.IsCodProduct(model.ProductId);

            var url = request == null ? null : request.Url;
            var shippingDate = DateTime.Now.AddMinutes(15);

            var resp = new BringStandardShippingProcessor().Booking(settings.ApiUid, settings.ApiKey, url == null ? null : url.AbsoluteUri,
                settings.TestMode(), 1, model.OrderId.ToString(), shippingDate, model.ProductId, model.CustomerNum, new[]
				{
					new BookingPackage()
					{                                
						WidthInCm = model.WidthInCm ?? 0,
						HeightInCm = model.HeightInCm ?? 0,
						LengthInCm = model.LengthInCm ?? 0,
						WeightInKg = (double)model.WeightInKg,
						NumberOfItems = model.NumberOfItems ?? 0,
						GoodsDescription = model.GoodsDescription,
					},
				}, shopInfo.StoreName, shopInfo.Address, shopInfo.Postnr, shopInfo.Poststed, shopInfo.Country, shopInfo.NotifyEmail,
                shopInfo.ContactPhone.TrimPhone(), model.OrderId.ToString(),
                model.RecipientName, model.RecipientAddress, model.RecipientPostalCode, model.RecipientCity, model.RecipientCountryCode, model.RecipientNotiEmail,
                model.RecipientNotiMobile, string.Format("Ordre kjøpt den {0:dd.MM.yyyy}", DateTime.Now).TrimLength(35), null,
                cod && model.ServiceCod == true ? new CashOnDelivery()
                {
                    AccountNumber = model.CodAccountNumber,
                    Amount = (double)Math.Round(model.Amount, 2),
                    CustomerId = string.Format("{0:D7}{1:D8}", model.CustomerId ?? 0, model.OrderId ?? 0).ToKid(),
                    CurrencyCode = model.Currency,
                } : null,
                recvNoti && model.RecipientNoti == true ? new RecipientNotification()
                {
                    Email = model.RecipientNotiEmail,
                    Mobile = model.RecipientNotiMobile.TrimPhone(),
                } : null);

            if (resp.Errors == null || resp.Errors.Length == 0)
            {
                model.Data = JsonConvert.SerializeObject(resp);

                model.BookingId = resp.ConsignmentNumber;
                model.LabelUrl = resp.LabelUrl;
                model.TrackingUrl = resp.TrackingUrl;
                model.WaybillUrl = resp.WaybillUrl;

                return new Tuple<bool, string>(true, null);
            }
            else return new Tuple<bool, string>(false, resp.Errors.ToString(".<br/>"));
        }
  
        #endregion

        #region MailBox

        public static System.Tuple<bool, string> MailBox(IDB db, BringMailBoxModel[] models, bool validate, HttpRequestBase request)
        {
            var orders = models == null 
                ? null 
                : models.Select(m => new { Model = m, Dom = OrderMgr.GetDom(db, m.OrderId) }).Where(m => m.Dom != null).ToList();

            if (models == null || orders.SafeCount() != models.SafeCount() || models.Length == 0 || (models.Length != 1 && validate)) 
                return new Tuple<bool, string>(false, null);

            var shopInfo = SettingsBiz.GetSettings().General;
            var settings = SettingsBiz.GetSettings().Display.Admin.BringShipping;

            var packages = orders.Select(m => new MailboxPackage()
            {
                Rfid = m.Model.Rfid,
                WeightInGram = m.Model.WeightInGram,
                PhoneNumber = m.Model.RecipientMobile,
                Email = m.Dom.Customer.Email,
                PostalCode = m.Dom.DeliverPostnr,
                RecipientName = m.Dom.DeliverName,
                StreetAddress = m.Dom.DeliverAddress,
            }).ToArray();

            var resp = new BringStandardShippingProcessor().ToMailbox(settings.ApiUid, settings.ApiKey, settings.TestMode(), settings.MailBoxCustId,
                shopInfo.StoreName, shopInfo.NotifyEmail, shopInfo.Postnr, shopInfo.Address, models.ToList().ToString(m => m.OrderId.ToString(),";"),
                packages, validate);

            if (resp.Errors == null || resp.Errors.Length == 0)
            {
                orders.Action(m =>
                {
                    if (validate) m.Dom.BringMailBoxData = JsonConvert.SerializeObject(models[0]);
                    else
                    {
                        m.Dom.BringBookingData = JsonConvert.SerializeObject(resp);

                        m.Model.MailboxId = resp.Id;
                        m.Model.LabelUri = resp.LabelUri;
                        m.Model.RfidLabelUri = resp.RfidLabelUri;
                        m.Model.WaybillUri = resp.WaybillUri;
                    }
                });

                var ret = BaseMgr.SaveOrUpdate(db, true, (sess, obj) => true, orders.Select(m => m.Dom).ToArray());

                if (ret && !validate) orders.Select(m => m.Dom).Action(obj => OrderNoteMgr.SaveorUpdate(db, new OrderNoteModel()
                {
                    Subject = Order.StringTable.BringMailBox,
                    Body = string.Format(Order.StringTable.BringMailBoxLogTempl, resp.Id, resp.LabelUri, resp.RfidLabelUri, resp.WaybillUri),
                    Private = true, Flags = 0, OrderId = obj.Id, OrderStatus = obj.OrderStatus,
                }, null, request));

                return new Tuple<bool, string>(ret, null);
            }
            else return new Tuple<bool, string>(false, resp.Errors.ToString(".<br/>"));
        }

        #endregion
    }
}