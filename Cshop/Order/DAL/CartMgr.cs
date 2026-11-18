using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Hosting;
using System.Web.Mvc;
using System.Web.UI;
using AutoMapper;
using Claw.Controls.Modules;
using Claw.Core;
using Claw.Modules.Eshop.Product.Helper;
using Newtonsoft.Json;
using NHibernate;
using System.Web.UI.WebControls;
using Claw.Core.DAL;
using Claw.Modules.Eshop.Controllers.Admin;
using Claw.Modules.Eshop.Domain;
using Claw.Modules.Eshop.Models;

namespace Claw.Modules.Eshop.DAL
{
    public class CartMgr : BaseMgr
    {
        static volatile ODateTime _lastRun = null;

        #region GetAll
        /// <summary>CPOS</summary>
        public static List<OrderModel> GetAll(IDB db, int pageIndex, int pageSize, out int totalRows,
            SortDirection sortDirection = SortDirection.Descending, string sortField = "Id", int? customerId = null,
            bool offer = false, bool? cpos = null)
        {
            var hSql = string.Format(@"
            select cart from {0} as cart where 1 = 1 ", typeof(Cart).FullName);

            if (customerId != null) hSql += " and cart.Customer.Id = " + customerId;

            if (offer) hSql += " and cart.Token is not null";
            else hSql += " and cart.Token is null";

            if (cpos != null)
                if (cpos.Value) hSql += " and cart.DataCpos is not null ";
                else hSql += " and cart.DataCpos is null ";

            #region Sorting

            switch (sortField)
            {
                case "CreatedDate.Text":
                    sortField = "CreatedDate";
                    break;
            }

            hSql += string.Format(" order by {0} {1}", "cart." + sortField, GetDir(sortDirection));

            #endregion

            var ret = GetList<Cart>(db, hSql, pageIndex, pageSize, out totalRows);

            var jsonSettings = OrderMgr.GetJsonSettings();
            return ret.Select(x =>
            {
                try
                {
                    var order = JsonConvert.DeserializeObject<OrderModel>(x.Data, jsonSettings);
                    order.OfferOrderId = x.RefOrderId;
                    order.OfferLastView = x.LastView;
                    order.OfferEmailView = x.EmailView;
                    order.CartNotified = x.Notified;
                    return order;
                }
                catch (Exception err) { LogErr(new Exception(String.Format("[CartId={0}]",x.Id), err), section: "CartMgr.JsonConvert.DeserializeObject OrderModel"); return null;  }
            }).Where(m => m != null).ToList();
        }

        #endregion

        #region GetEx

        public static OrderModel GetEx(IDB db, int customerId, bool? cpos = false)
        {
            int total;
            return GetAll(db, 1, 1, out total, SortDirection.Descending, "Id", customerId, cpos: cpos).FirstOrDefault();
        } 

        #endregion

        #region DoJob

        public static void DoJob(IDB db, int minHour = 12, int activeHour = 6, int maxHour = 60)
        {
            if (Singleton<IOrderMgr2>.Default == null || Singleton<ISendEmailMgr>.Default == null || EagerLoadHttpModule.SiteUrl.IsNullOrEmpty()) return;

            if (DateTime.Now.Hour != activeHour ||
                (_lastRun != null && _lastRun.Value != null && (DateTime.Now - _lastRun.Value.Value).TotalHours < 1)) return;

            //LogErr(new Exception(string.Format("CartMgr.DoJob at {0}, LastRun:{1}", DateTime.Now, _lastRun)));

            db.GetSession().CacheMode = CacheMode.Refresh;

            lock (Sync)
            {
                _lastRun = DateTime.Now.Date.AddHours(activeHour);

                var hSql = string.Format(@"
                select cart from {0} as cart
                where cart.Notified = 0 
                and cart.Token is null
                and cart.CreatedDate <= '{1:yyyy-MM-dd HH:mm}'
                and cart.CreatedDate > '{2:yyyy-MM-dd HH:mm}' ", typeof(Cart).FullName, DateTime.Now.AddHours(-1 * minHour), DateTime.Now.AddHours(-1 * maxHour));

                var lst = GetList<Cart>(db, hSql);

                if (lst.Count == 0) return;

                var updLst = new List<Cart>();

                foreach (var itm in lst)
                {
                    try
                    {
                        var lang = LanguageMgr.Get(itm.LangCode, db).SafeProp(m => m.ShortLangCode);
                        var query = string.Format("{0}={1}", CurrencyModel.CurCode, itm.Currency);
                        var request = new SimpleWorkerRequest(lang, query, new StreamWriter(new MemoryStream()));
                        HttpContext.Current = new HttpContext(request);
                        HttpContext.Current.Request.RequestContext.RouteData.Values.Add("lang", lang);
                        HttpContext.Current.Request.RequestContext.RouteData.Values.Add(CurrencyModel.CurCode, itm.Currency);
                        var controller = new BaseController(new DefaultDb().DataService);
                        controller.ControllerContext = new ControllerContext(HttpContext.Current.Request.RequestContext, controller);
                        ClawContext.Current.InitRequestData();

                        var model = OrderMgr.Get(db, itm.OrderId);
                        model.Raw = OrderMgr.GetDom(db, itm.OrderId);

                        if (model.Customer == null || model.Customer.Email.IsNullOrEmpty() || model.CartIdOffered > 0) 
                            continue;

                        model.InitOrderDetails(db);
                        model.InitMandatoryInfo(db);

                        //LogErr(new Exception(string.Format("CartId={0} send email={1}", itm.Id, model.Customer.Email)));

                        Singleton<ISendEmailMgr>.Default.Send(controller, model, (byte)OrderStatus.PaymentWaiting, "", false);

                        itm.Notified = true;
                        if (!Update(new DefaultDb(), new List<Cart>() { itm })) LogErr(new Exception("CartMgr.DoJob update failed for cartid = " + itm.Id));
                    }
                    catch (Exception err)
                    {
                        LogErr(err);
                    }
                }
            }
        }

        #endregion

    }
}