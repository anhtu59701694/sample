using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using AutoMapper;
using Claw.Controls.Modules;
using Claw.Core;
using Claw.Modules.Eshop.Helper;
using NHibernate;
using System.Web.UI.WebControls;
using Claw.Core.DAL;
using Claw.Modules.Eshop.Controllers.Admin;
using Claw.Modules.Eshop.Domain;
using Claw.Modules.Eshop.Models;

namespace Claw.Modules.Eshop.DAL
{
    public class ShippingBookingMgr : BaseMgr<ShippingBookingModel, ShippingBooking>
    {
        static ShippingBookingMgr() { Init(typeof(ShippingBookingMgr)); }

        #region GetAll

        public static List<ShippingBookingModel> GetAll(IDB db, int pageIndex, int pageSize, out int totalRows,
            SortDirection sortDirection = SortDirection.Descending, string sortField = "Id", string keyword = null)
        {
            var args = new List<System.Tuple<string, string>>();
            var hSql = string.Format(@"
            select booking from {0} as booking
            where 1 = 1 ", typeof(ShippingBooking).FullName);

            if (!string.IsNullOrEmpty(keyword))
            {
                hSql += string.Format(@" and (booking.RecipientName like :keyword or booking.RecipientNotiEmail like :keyword or booking.RecipientNotiMobile like :keyword or booking.BookingId like :keyword)");
                args.Add(new System.Tuple<string, string>("keyword", "%" + keyword + "%"));
            }

            hSql += string.Format(" order by {0} {1}", "booking." + sortField, GetDir(sortDirection));

            var ret = GetList<ShippingBooking>(db, hSql, pageIndex, pageSize, out totalRows, args.ToArray());

            return Mapper.Map<List<ShippingBooking>, List<ShippingBookingModel>>(ret);
        }

        #endregion

        #region SaveorUpdate
        public static bool SaveorUpdate(IDB db, ShippingBookingModel model)
        {
            if (model == null) return false;

            var obj = GetDom(db, model.Id) ?? new ShippingBooking(){ CreatedDate = DateTime.Now };

            obj.Service = model.Service;
            obj.OrderId = model.OrderId;
            obj.Amount = model.Amount;
            obj.Currency = model.Currency;
            obj.ProductId = model.ProductId;
            obj.CustomerId = model.CustomerId;
            obj.CustomerNum = model.CustomerNum;
            obj.WidthInCm = model.WidthInCm;
            obj.HeightInCm = model.HeightInCm;
            obj.WeightInKg = model.WeightInKg;
            obj.LengthInCm = model.LengthInCm;
            obj.NumberOfItems = model.NumberOfItems;
            obj.GoodsDescription = model.GoodsDescription;
            obj.RecipientName = model.RecipientName;
            obj.RecipientAddress = model.RecipientAddress;
            obj.RecipientPostalCode = model.RecipientPostalCode;
            obj.RecipientCity = model.RecipientCity;
            obj.RecipientCountryCode = model.RecipientCountryCode;
            obj.RecipientNoti = model.RecipientNoti;
            obj.RecipientNotiEmail = model.RecipientNotiEmail;
            obj.RecipientNotiMobile = model.RecipientNotiMobile;
            obj.Rfid = model.Rfid;
            obj.ServiceCod = model.ServiceCod;
            obj.CodAccountNumber = model.CodAccountNumber;
            obj.Data = model.Data;
            obj.Result = model.Result;
            obj.BookingId = model.BookingId;
            obj.LabelUrl = model.LabelUrl;
            obj.TrackingUrl = model.TrackingUrl;
            obj.WaybillUrl = model.WaybillUrl;

            if (!obj.SaveorUpdate(db.DataService)) return false;

            model.Id = obj.Id;
            new ShippingBookingMgr().OnUpdate(obj);

            return true;
        }
        #endregion

        #region OnUpdate
        protected override void OnUpdate(ShippingBooking obj)
        {
        }
        #endregion

        #region OnDelete
        protected override void OnDelete(ShippingBooking obj)
        {
        }
        #endregion
    }
}