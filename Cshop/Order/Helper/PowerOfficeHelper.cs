using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Web;
using Claw.Core;
using Claw.Modules.Eshop.Core;
using Claw.Modules.Eshop.DAL;
using Claw.Modules.Eshop.Domain;
using Claw.Modules.Eshop.Models;
using GoApi;
using GoApi.Common;
using GoApi.Core;
using GoApi.Import;
using GoApi.Invoices;
using GoApi.SalesOrders;

namespace Claw.Modules.Eshop.Helper
{
    public class PowerOfficeHelper
    {
        private static volatile int _minCustId = 0;

        private readonly Go _api;
        private readonly IDB _db;

        #region PowerOfficeHelper

        static PowerOfficeHelper()
        {
            System.Net.ServicePointManager.SecurityProtocol |= System.Net.SecurityProtocolType.Tls12;
        }

        public PowerOfficeHelper(IDB db, string applicationKey, string clientKey, string tokenStoreFile, GoApi.Core.Global.Settings.EndPointMode mode = GoApi.Core.Global.Settings.EndPointMode.Demo)
        {
            GoApi.Core.Global.Settings.Mode = mode;

            var folder = System.IO.Path.GetDirectoryName(tokenStoreFile);
            if (folder != null && !System.IO.Directory.Exists(folder)) System.IO.Directory.CreateDirectory(folder);

            _db = db;
            _api = new Go(new AuthorizationSettings
            {
                ApplicationKey = applicationKey,
                ClientKey = clientKey,
                TokenStore = new BasicTokenStore(tokenStoreFile)
            });
        }

        public PowerOfficeHelper(IDB db, SettingsBiz.PowerOfficeBiz settings)
            : this(db, settings.ApplicationKey, settings.ClientKey, Globals.MapPath(Core.ModuleUrl.PowerOfficeFolder.CombineWPath("/TokenStore")),
                (GoApi.Core.Global.Settings.EndPointMode) Enum.Parse(typeof (GoApi.Core.Global.Settings.EndPointMode), settings.Mode))
        {
        }

        #endregion

        #region Sync.Customer

        public GoApi.Party.Customer Sync(Domain.Customer obj, Domain.Order order, out long? deliveryAddrId)
        {
            deliveryAddrId = null;

            if (obj == null) return null;

            if (_minCustId == 0)
                _minCustId = _db.DataService.GetList<int>("select min(Id) from " + typeof(Domain.Customer).FullName).FirstOrDefault();

            var code = obj.Id - _minCustId + 10000;
            var ret = _api.Customer.Get().FirstOrDefault(x => x.Code == code) ?? new GoApi.Party.Customer() { Code = code, LegalName = obj.Id.ToString() };

            ret.Name = obj.BillingName;
            ret.VatNumber = obj.OrgNumber;
            ret.EmailAddress = obj.Email;
            ret.PhoneNumber = !string.IsNullOrEmpty(obj.BillingMobile) ? obj.BillingMobile : obj.BillingPhone;

            ret.ContactPersonId = null;
            ret.Since = obj.CreatedDate;

            var addresses = (List<GoApi.Common.Address>) (ret.StreetAddresses = ret.StreetAddresses ?? new List<GoApi.Common.Address>());
            GoApi.Common.Address deliveryAddr; 

            foreach (var addr in new[]
            {
                new GoApi.Common.Address() { Address1 = order.BillingAddress, City = order.BillingCity, ZipCode = order.BillingPostnr, CountryCode = order.BillingCountry.SafeProp(z => z.Code)},
                deliveryAddr = new GoApi.Common.Address() {Address1 = order.DeliverAddress, City = order.DeliverCity, ZipCode = order.DeliverPostnr, CountryCode = order.DeliverCountry.SafeProp(z => z.Code)}
            })
            {
                var key = GetKey(addr);
                if (!addresses.Any(x => GetKey(x).EqualsIgCase(key))) addresses.Add(addr);
            }

            ret.ContactGroups = obj.CustomerCGroups.Select(x => x.Group.Name).ToList();

            ret = _api.Customer.Save(ret);

            if (addresses.Any(x => x.Id == null))
                ret = _api.Customer.Get().FirstOrDefault(x => x.Code == code) ?? ret;

            deliveryAddrId = ret.StreetAddresses.FirstOrDefault(x => GetKey(x).EqualsIgCase(GetKey(deliveryAddr))).SafeProp(x => x.Id);

            return ret;
        }

        #endregion

        #region Sync.Product

        public GoApi.Products.Product Sync(Domain.ProductBase obj, Domain.Order order, Domain.VariantBase vari = null)
        {
            if (obj == null) return null;

            var sId = string.Format("{0}{1}", obj.Id, vari != null ? "-" + vari.Id : null);
            var ret = _api.Product.Get().FirstOrDefault(x => x.Code == sId) ?? new GoApi.Products.Product()
            {
                Code = sId,
                Gtin = null,
                Type = GoApi.Products.ProductType.Product,
                Unit = "ea",
                SalesAccount = 3000,
                ProductGroupId = null
            };

            ret.Name = obj.GetName(vari, order.LangCode ?? "");
            ret.CostPrice = obj.CostPrice;
            ret.SalesPrice = obj.CombinedSalePrice(vari);
            ret.IsActive = (obj.Visibility & 1) != 0;
            ret.ProductsOnHand = obj.Quantity;
            ret.Description = obj.SKU;

            ret = _api.Product.Save(ret);

            return ret;
        }

        #endregion 
        
        #region Sync.Shipping

        public GoApi.Products.Product Sync(Domain.Shipping obj, Domain.Order order)
        {
            if (obj == null) return null;

            var sId = string.Format("shipping-{0}", obj.Id);
            var ret = _api.Product.Get().FirstOrDefault(x => x.Code == sId) ?? new GoApi.Products.Product()
            {
                Code = sId,
                Gtin = null,
                Type = GoApi.Products.ProductType.Service,
                Unit = "ea",
                ProductGroupId = null,
                Description = null,
                SalesAccount = 3000,
            };

            ret.Name = order.ShippingName;
            ret.SalesPrice = null;
            ret.IsActive = obj.Active;

            ret = _api.Product.Save(ret);

            return ret;
        }

        #endregion 
      
        #region Sync.Payment

        public GoApi.Products.Product Sync(Domain.PaymentMethod obj, Domain.Order order)
        {
            if (obj == null) return null;

            var sId = string.Format("payment-{0}", obj.Id);
            var ret = _api.Product.Get().FirstOrDefault(x => x.Code == sId) ?? new GoApi.Products.Product()
            {
                Code = sId,
                Gtin = null,
                Type = GoApi.Products.ProductType.Service,
                Unit = "ea",
                ProductGroupId = null,
                Description = null,
                SalesAccount = 3000,
            };

            ret.Name = order.PaymentName;
            ret.SalesPrice = null;
            ret.IsActive = obj.Active;

            ret = _api.Product.Save(ret);

            return ret;
        }

        #endregion 

        #region Sync.Coupon

        public GoApi.Products.Product Sync(Domain.Coupon obj, Domain.Order order)
        {
            if (obj == null) return null;

            var sId = string.Format("coupon-{0}", obj.Id);
            var ret = _api.Product.Get().FirstOrDefault(x => x.Code == sId) ?? new GoApi.Products.Product()
            {
                Code = sId,
                Gtin = null,
                Type = GoApi.Products.ProductType.Service,
                Unit = "ea",
                ProductGroupId = null,
                Description = null,
                SalesAccount = 3000,
            };

            ret.Name = obj.GetName(order.LangCode ?? "");
            ret.SalesPrice = null;
            ret.IsActive = obj.Active;

            ret = _api.Product.Save(ret);

            return ret;
        }

        #endregion 
           
        #region Sync.Discount

        public GoApi.Products.Product Sync(Domain.Discount obj, Domain.Order order)
        {
            if (obj == null) return null;

            var sId = string.Format("discount-{0}", obj.Id);
            var ret = _api.Product.Get().FirstOrDefault(x => x.Code == sId) ?? new GoApi.Products.Product()
            {
                Code = sId,
                Gtin = null,
                Type = GoApi.Products.ProductType.Service,
                Unit = "ea",
                ProductGroupId = null,
                Description = null,
                SalesAccount = 3000,
            };

            ret.Name = obj.GetName(order.LangCode ?? "");
            ret.SalesPrice = null;
            ret.IsActive = obj.Active;

            ret = _api.Product.Save(ret);

            return ret;
        }

        #endregion 

        #region Save.Order

        public Import SaveOrder(Domain.Order obj)
        {
            var import = new Import
            {
                Description = string.Format("{0} {1}", "Order", obj.Id),
                Date = DateTime.Now
            };

            var salesOrder = new SalesOrder(obj.Id, obj.CreatedDate, obj.Customer.Id + 10000)
            {
                Reference = obj.Comments,
                PaymentTerms = null,
                Currency = obj.Currency,  
            };
             
            foreach (var od in obj.OrderDetails)
                salesOrder.AddSalesOrderLine(new SalesOrderLine(
                    string.Format("{0}{1}", od.Product.Id, od.Variant != null ? "-" + od.Variant.Id : null),
                    od.Quantity, od.CouponValue + od.DiscountValue)
                {
                    SalesOrderLineUnitPrice = od.Price,
                    Description = od.ProductName, 
                });

            import.SalesOrders.Add(salesOrder);

            _api.Import.Save(import);

            return import;
        }

        #endregion

        #region Save.Invoice

        public OutgoingInvoice SaveInvoice(Domain.Order obj)
        {
            if (obj == null) return null;

            long? deliveryAddrId;
            var cust = Sync(obj.Customer, obj, out deliveryAddrId);

            var ret = new OutgoingInvoice()
            {
                CustomerCode = cust.Code,
                CurrencyCode = obj.Currency,
                PurchaseOrderNo = obj.Id.ToString(),
                ImportedOrderNo = obj.Id,
                DeliveryDate = obj.DeliveredDate,
                OrderDate = obj.CreatedDate,
                DeliveryAddressId = deliveryAddrId
            };

            #region OrderDetails
            foreach (var od in obj.OrderDetails)
            {
                var prod = Sync(od.Product, obj, od.Variant);
                ret.OutgoingInvoiceLines.Add(new OutgoingInvoiceLine()
                {
                    LineType = VoucherLineType.Normal,
                    ProductCode = prod.Code,
                    Description = od.ProductName,
                    Quantity = od.Quantity,
                    UnitPrice = od.Price,
                    UnitOfMeasure = prod.Unit,
                    DiscountPercent = (od.PriceInc + od.CouponValue + od.DiscountValue == 0m) ? 0m : (od.CouponValue + od.DiscountValue) / (od.PriceInc + od.CouponValue + od.DiscountValue),
                    SortOrder = 0,
                    ExemptVat = false,
                });
            } 
            #endregion

            #region Shipping
            var ship = Sync((Domain.Shipping)obj.Shipping, obj);
            if (ship != null)
                ret.OutgoingInvoiceLines.Add(new OutgoingInvoiceLine()
                {
                    LineType = VoucherLineType.Normal,
                    ProductCode = ship.Code,
                    Description = obj.ShippingName,
                    Quantity = 1,
                    UnitPrice = obj.ShippingCost / (1 + obj.ShippingVat),
                    UnitOfMeasure = ship.Unit,
                    SortOrder = 0,
                    ExemptVat = false,
                }); 
            #endregion

            #region PaymentMethod
            var payment = Sync((Domain.PaymentMethod)obj.PaymentMethod, obj);
            ret.OutgoingInvoiceLines.Add(new OutgoingInvoiceLine()
            {
                LineType = VoucherLineType.Normal,
                ProductCode = payment.Code,
                Description = obj.PaymentName,
                Quantity = 1,
                UnitPrice = obj.PaymentCost / (1 + obj.PaymentVat),
                UnitOfMeasure = payment.Unit,
                SortOrder = 0,
                ExemptVat = false,
            }); 
            #endregion

            #region Coupon

            var coupon = Sync(obj.Coupon, obj);
            if (coupon != null && obj.TotalCoupon != 0) 
                ret.OutgoingInvoiceLines.Add(new OutgoingInvoiceLine()
                {
                    LineType = VoucherLineType.Normal,
                    ProductCode = coupon.Code,
                    Description = coupon.Name,
                    Quantity = 1,
                    UnitPrice = -1*obj.TotalCoupon,
                    UnitOfMeasure = coupon.Unit,
                    SortOrder = 0,
                    ExemptVat = false,
                });

            #endregion

            #region Discount

            var discount = Sync(obj.Discount, obj);
            if (discount != null && obj.TotalDiscount != 0)
                ret.OutgoingInvoiceLines.Add(new OutgoingInvoiceLine()
                {
                    LineType = VoucherLineType.Normal,
                    ProductCode = discount.Code,
                    Description = discount.Name,
                    Quantity = 1,
                    UnitPrice = -1 * obj.TotalDiscount,
                    UnitOfMeasure = discount.Unit,
                    SortOrder = 0,
                    ExemptVat = false,
                });

            #endregion

            ret = _api.OutgoingInvoice.Save(ret);

            return ret;
        }

        #endregion

        #region GetKey

        private string GetKey(GoApi.Common.Address addr)
        {
            if (addr == null) return string.Empty;

            return string.Format("{0},{1},{2},{3}", addr.Address1, addr.City, addr.ZipCode, addr.CountryCode);
        }

        #endregion
    }
}
