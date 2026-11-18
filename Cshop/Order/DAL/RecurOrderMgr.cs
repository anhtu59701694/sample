using AutoMapper;
using Claw.Controls.Modules;
using Claw.Core;
using Claw.Core.DAL;
using Claw.Core.Logs;
using Claw.Modules.Eshop.Controllers.Admin;
using Claw.Modules.Eshop.Domain;
using Claw.Modules.Eshop.Helper;
using Claw.Modules.Eshop.Models;
using Eshop.Payments.BasePayment;
using Kendo.Mvc.UI;
using NHibernate;
using NPOI.SS.Formula.Functions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Hosting;
using System.Web.Mvc;
using System.Web.UI.WebControls;

namespace Claw.Modules.Eshop.DAL
{
    public class RecurOrderMgr : BaseMgr<RecurOrderModel, RecurOrder>
    {
        protected static new readonly object Sync = new object();
        public static volatile Func<IDB, List<RecurOrder>> FuncJobList = null;
        public static volatile Action<IDB, Domain.Order> FuncRecur = null;
        public static volatile Func<Domain.Order, bool> FuncIsRecurOrder = null;
        public static volatile ODateTime LastRetryTime = DateTime.Now.AddYears(-1);
        public static Func<IDB, string, string, List<System.Tuple<string, string>>, System.Tuple<string, string>> FuncHsqlList = null;

        static RecurOrderMgr() { Init(typeof(RecurOrderMgr)); }

        #region GetAll

        public static List<RecurOrderModel> GetAll(IDB db, int? orderId, int? recurring, int pageIndex, int pageSize, out int totalRows,
            SortDirection sortDirection = SortDirection.Descending, string sortField = "Id", int? customerId = null, int? recurId = null)
        {
            var hSql = string.Format(@"
            select recur from {0} as recur join recur.Order as ord
            where recur.Status not in ({1}, {2}) ", typeof(RecurOrder).FullName, RecurOrderStatus.Pending.Val(), RecurOrderStatus.Deleted.Val());

            if (customerId != null)
                hSql += string.Format(" and ord.Customer.Id = {0}", customerId);

            if (recurId != null)
                hSql += " and recur.Id = " + recurId;

            hSql += string.Format(" order by {0} {1}", "recur." + sortField, GetDir(sortDirection));

            if (FuncHsqlList != null)
            {
                var tmp = FuncHsqlList(db, hSql, null, null);
                hSql = tmp.Item1;
            }

            var ret = GetList<RecurOrder>(db, hSql, pageIndex, pageSize, out totalRows);

            OrderMgr.BindStats(db, ret);

            return Mapper.Map<List<RecurOrder>, List<RecurOrderModel>>(ret);
        }

        #endregion

        #region SaveorUpdate

        public static bool SaveorUpdate(IDB db, RecurOrderModel model, ISession session = null, HttpRequestBase request = null, string userNameLog = null)
        {
            RecurOrder obj;

            if (model == null || null == (obj = GetDom(db, model.Id, session))) return false;

            var monFields = new[]
            {
                Expr2(m => m.FromDate), Expr2(m => m.ToDate), Expr2(m => m.RecurringTxt), Expr2(m => m.RecurEach), Expr2(m => m.StatusTxt)
            }.ToFieldName();
            var originVals = obj.Id == 0 ? null : Mapper.Map<RecurOrder, RecurOrderModel>(obj).Extract(monFields);

            var changedItems = new List<System.Tuple<string, string, Domain.OrderDetail>>();
            var odMonFields = new[] { BaseMgr<OrderDetailModel, Domain.OrderDetail>.Expr(m => m.RecurQty) }.ToFieldName();

            if (obj.Recurring != (model.Recurring ?? 0) || obj.RecurEach != (model.RecurEach ?? 0))
            {
                obj.SrcRecurring = obj.SrcRecurring ?? obj.Recurring;
                obj.SrcRecurEach = obj.SrcRecurEach ?? obj.RecurEach;

                foreach (var odetail in obj.Order.OrderDetails
                    .Where(m => (m.SrcRecurType ?? m.RecurType) == obj.SrcRecurring && (m.SrcRecurEach ?? m.RecurEach) == obj.SrcRecurEach))
                {
                    odetail.SrcRecurType = odetail.SrcRecurType ?? odetail.RecurType;
                    odetail.SrcRecurEach = odetail.SrcRecurEach ?? odetail.RecurEach;

                    odetail.RecurType = model.Recurring;
                    odetail.RecurEach = model.RecurEach;
                }
            }

            obj.FromDate = (DateTime)model.FromDate;
            obj.ToDate = model.ToDate;
            obj.Recurring = model.Recurring ?? 0;
            obj.RecurEach = model.RecurEach ?? 0;
            obj.RecurData = model.RecurData ?? 0;
            obj.Status = model.Status ?? 0;

            var finalVals = obj.Id == 0 ? null : model.Extract(monFields);

            #region OrderDetails

            if (model.NextOrder != null && model.NextOrder.OrderDetails != null)
            {
                foreach (var item in model.NextOrder.OrderDetails)
                {
                    int mProductId = item.ProductId ?? 0, mVariantId = item.VariantId ?? 0;
                    var ditm = obj.Order.OrderDetails
                        .Where(m => m.RecurType == obj.Recurring && m.RecurEach == obj.RecurEach)
                        .FirstOrDefault(x => x.Product.Id == mProductId && x.Variant.GetId(0) == mVariantId);

                    if (ditm == null) continue;

                    var odOriginVals = ditm.Extract(odMonFields);

                    if (ditm.RecurQty != item.Quantity && item.Quantity >= 0) ditm.RecurQty = item.Quantity;

                    var odFinalVals = ditm.Extract(odMonFields);
                    var odChanges = odOriginVals.GetChanges(odFinalVals);
                    if (odChanges.Count != 0)
                        changedItems.AddRange(odChanges.Select(m => new System.Tuple<string, string, Domain.OrderDetail>(m.Key, m.Value, ditm)));
                }
            }

            #endregion

            if (obj.Status == 1 && 0 == obj.Order.OrderDetails.Count(m => m.RecurType == obj.Recurring && m.RecurEach == obj.RecurEach && m.RecurQty > 0))
                obj.Status = 2;

            if (obj.Status.InList(RecurOrderStatus.Deleted))
                obj.Order.OrderDetails.Where(m => m.RecurQty > 0).Action(m => m.RecurQty = 0);
            
           if (session != null) session.SaveOrUpdate(obj);
            else if (!obj.SaveorUpdate(db.DataService)) return false;

            new RecurOrderMgr().OnUpdate(obj);

            var changes = originVals.GetChanges(finalVals); 
            changedItems.AddRange(changes.Select(m => new System.Tuple<string, string, Domain.OrderDetail>(m.Key, m.Value, null)));

            if (changedItems.Count != 0)
                RecurOrderLogMgr.SaveorUpdate(db, new RecurOrderLogModel()
                {
                    Content = changedItems.Select(x => string.Format("Updated '{0}', former value: '{1}'{2}", x.Item1.GetDisplayName(model), x.Item2, x.Item3 == null ? "" : string.Format(", Product: '{0}'", x.Item3.ProductName))).ToString(". "),
                    RecurringId = obj.Id,
                }, session, request, userNameLog);

            return true;
        }

        #endregion

        #region DoJob

        public static void DoJob(IDB db)
        {
            if (Singleton<IOrderMgr2>.Default == null || EagerLoadHttpModule.SiteUrl.IsNullOrEmpty()) return;

            db.GetSession().CacheMode = CacheMode.Refresh;

            lock (Sync)
            {
                var hSql = string.Format(@"
                select recur from {0} as recur join recur.Order as ord join ord.Customer as cust
                where recur.Status = {2} and cust.Status in ({4}, {5})
                and recur.FromDate <= '{1:yyyy-MM-dd}'
                and (recur.ToDate is null or recur.ToDate >= '{1:yyyy-MM-dd}') 
                and recur.NextRunning <= '{3:yyyy-MM-dd HH:mm}'
                order by recur.NextRunning ", typeof(RecurOrder).FullName, DateTime.Now, RecurOrderStatus.Active.Val(), DateTime.Now, CustStatus.Active.Val(), CustStatus.Pending.Val());

                var lst = FuncJobList == null ? GetList<RecurOrder>(db, hSql) : (FuncJobList(db) ?? GetList<RecurOrder>(db, hSql));
                  
                if (lst.Count == 0) return;

                var updLst = new List<RecurOrder>();
                var errLst = new List<System.Tuple<RecurOrder, Domain.Order, string>>();
                var settings = SettingsBiz.GetSettings();

                foreach (var itm in lst.Where(x => FuncJobList != null || x.GetNextRunning() <= DateTime.Now))
                {
                    Domain.Order order = null;
                    try
                    {
                        #region Processing

                        order = OrderMgr.Recur(itm, db, true);
                        itm.LastRunMsg = StringTable.RecurOrderResult((itm.LastRunOk = order != null).Value);

                        if (order != null)
                        {
                            if (FuncRecur != null) FuncRecur(db, order);

                            string errMsg = null;
                            PaymentData data = null;
                            var log = new OrderNoteModel() { Subject = Order.StringTable.RecurOrder, OrderStatus = order.OrderStatus, OrderId = order.Id, Private = true };
                            if (!order.PaymentMethod.IsOnlinePayment || Payment.Helper.PaymentHelper.Recur(db, order, itm.Order.Recurring.Order, out errMsg, out data, out var isStopped, out var isExpired))
                            {
                                #region Processing

                                var paySetting = ((PaymentMethod)order.PaymentMethod).PaymentSetting;

                                log.OrderStatus = order.OrderStatus = 
                                    paySetting != null && paySetting.Id.InList(GatewayID.BankTransfer)
                                        ? (byte) OrderStatus.PayBeforeDelivery
                                        : (byte) OrderStatus.New;

                                foreach (var ditm in order.OrderDetails)
                                {
                                    if (Product.DAL.ProductMgr.UpdateQuantity(db, ditm.Product, ditm.Variant, -1 * ditm.QuantityOri,
                                        false, false, true, false,
                                        new ProdQtyLogModel() { Reason = (int)ProdQtyLogReason.Ordered, RefIdFunc = () => order.Id }))
                                    {
                                        ditm.Product.OrderDetailNum = null;
                                        ditm.Product.TotalSales = null;
                                        ditm.Product.Update(db.DataService);
                                    }
                                }

                                OrderMgr.ProcessCustomerProduct(order, order.Customer, db, db.GetSession());
                                db.GetSession().Flush();

                                order.Update(db.DataService);

                                log.Body = string.Format(Order.StringTable.RecurOrderLogTemplSucceeded, new ODateTime(DateTime.Now), data.SafeProp(m => m.TransactionId));
                                OrderNoteMgr.SaveorUpdate(db, log);

                                if (!BaseController.ExecWebMethod(string.Format("/modules/eshop/WebJobOrder/SendEmail/{0}?securityCode={1}", 
                                    order.Id, Controllers.FrontEnd.WebJobOrderController.SecurityCode), out errMsg))
                                    OrderNoteMgr.SaveorUpdate(db, new OrderNoteModel()
                                    {
                                        Subject = Order.StringTable.RecurOrder, 
                                        Body = string.Format(Order.StringTable.SendEmailFailedTempl, order.Id, errMsg), 
                                        OrderStatus = order.OrderStatus, OrderId = order.Id, Private = true
                                    });

                                #endregion
                            }
                            else
                            {
                                itm.LastRunOk = false;
                                itm.LastRunMsg = errMsg;

                                if (isStopped) itm.Status = (byte)RecurOrderStatus.Inactive;

                                log.Body = string.Format(Order.StringTable.RecurOrderLogTemplFailed, new ODateTime(DateTime.Now), errMsg);
                                OrderNoteMgr.SaveorUpdate(db, log);

                                if (!isStopped && (errMsg ?? "").IndexOf("[NoAttempt]", StringComparison.OrdinalIgnoreCase) == -1)
                                {
                                    order.PaymentRetryCount = Math.Max(0, settings.Hidden.PaymentRetryCount - 1);
                                    order.PaymentRetryStage = (int)PaymentRetryStageType.Done;
                                    order.PaymentRetryTimestamp = DateTime.Now;

                                    order.Update(db.DataService);
                                }

                                if ((!isStopped || isExpired) && settings.Hidden.RecurOrderFailedSendNoti) errLst.Add(new System.Tuple<RecurOrder, Domain.Order, string>(itm, order, errMsg));
                            }

                            itm.LastRunMsg = itm.LastRunMsg ?? StringTable.RecurOrderResult(itm.LastRunOk ?? false);
                            itm.LastRunOrderId = order.Id;
                            itm.OrderNum = null;

                        }

                        #endregion

                        itm.LastRunTime = DateTime.Now; 
                    }
                    catch (Exception err)
                    {
                        itm.LastRunOk = false;
                        itm.LastRunMsg = err.Message;

                        LogErr(err);

                        if (order != null)
                        {
                            itm.LastRunTime = DateTime.Now;
                            itm.LastRunOrderId = order.Id;
                            itm.OrderNum = null;

                            var log = new OrderNoteModel() { Subject = Order.StringTable.RecurOrder, OrderStatus = order.OrderStatus, OrderId = order.Id, Private = true };
                            log.Body = string.Format(Order.StringTable.RecurOrderLogTemplFailed, new ODateTime(DateTime.Now), string.Format("Message: {0}\r\nStackTrace: {1}", err.Message, err.StackTrace.TrimLength(2000)));
                            OrderNoteMgr.SaveorUpdate(db, log);

                            if (settings.Hidden.RecurOrderFailedSendNoti) errLst.Add(new System.Tuple<RecurOrder, Domain.Order, string>(itm, order, err.Message));                   
                        }
                    }

                    updLst.Add(itm);
                }

                if (Update(db, updLst))
                {
                    lst.Action(x => new RecurOrderMgr().OnUpdate(x));
                    errLst.Action(itm =>
                    {
                        try
                        {
                            var lang = LanguageMgr.Get(itm.Item2.LangCode, db).SafeProp(m => m.ShortLangCode).IfNull("no");
                            var query = string.Format("{0}={1}", CurrencyModel.CurCode, itm.Item2.Currency);
                            var request = new SimpleWorkerRequest(lang, query, new StreamWriter(new MemoryStream()));
                            HttpContext.Current = new HttpContext(request);
                            HttpContext.Current.Request.RequestContext.RouteData.Values.Add("lang", lang);
                            HttpContext.Current.Request.RequestContext.RouteData.Values.Add(CurrencyModel.CurCode, itm.Item2.Currency);
                            var controller = new OrderController(db.DataService);
                            controller.ControllerContext = new ControllerContext(HttpContext.Current.Request.RequestContext, controller);
                            ClawContext.Current.InitRequestData();

                            var typeSendMailHelper = Type.GetType("Claw.Modules.Eshop.Helper.SendMailHelpers, Claw.Modules.Eshop.FrontEnd");
                            if (typeSendMailHelper != null)
                            {
                                var sendMailHelper = Activator.CreateInstance(typeSendMailHelper, controller);
                                var model = OrderMgr.Get(db, itm.Item2.Id);

                                model.InitOrderDetails(db);
                                model.InitMandatoryInfo(db);

                                var emailList = sendMailHelper.SafeInvokeMethod<string>("EmailList", model, null);

                                var retSend = Singleton<ISendEmailMgr>.Default.Send(controller, itm.Item2.Id, itm.Item2.OrderStatus, itm.Item3, true, emailList);
                                
                                var log = new OrderNoteModel()
                                {
                                    OrderStatus = itm.Item2.OrderStatus, OrderId = itm.Item2.Id, Private = true, Body = retSend ? "" : "Send failed" 
                                };
                        
                                if (settings.Hidden.RecurOneTimeEnabled)
                                {
                                    itm.Item2.Flags |= (int)(Domain.Interfaces.OrderFlagType.NotUsePaymentDeliverAddr | Domain.Interfaces.OrderFlagType.NotUsePaymentBillingAddr | Domain.Interfaces.OrderFlagType.RenewPayment);

                                    if (!settings.Hidden.KlarnaRecurFailedActive && itm.Item2.PaymentMethod.PaymentSystemName.InList(GatewayID.KlarnaCheckout.ToString(), GatewayID.KlarnaPayments.ToString()))
                                    {
                                        itm.Item1.Status = (byte)RecurOrderStatus.Inactive;
                                        itm.Item1.Update(db.DataService);
                                    }

                                    log.Subject = StringTable.RecurOrderFailedSendNotiOkRecurOneTime;

                                    itm.Item2.Update(db.DataService); 
                                }
                                else
                                {
                                    itm.Item1.Status = (byte) RecurOrderStatus.Inactive;
                                    
                                    log.Subject = StringTable.RecurOrderFailedSendNotiOk;
                                    
                                    itm.Item1.Update(db.DataService);
                                }

                                OrderNoteMgr.SaveorUpdate(db, log); 
                            }
                            else LogErr(new Exception("RecurOrderMgr.DoJob send email failed, typeSendMailHelper is null"));
                        }
                        catch (Exception err)
                        {
                            LogErr(err); 
                        }
                        finally { HttpContext.Current = null; }
                    });
                }
                else LogErr(new Exception("RecurOrderMgr.DoJob failed"));
            }

        }

        #endregion

        #region RecurJobRetry
        public static void RecurJobRetryEx(IDB db, StringBuilder errMsg)
        {
            if (Singleton<IOrderMgr2>.Default == null || EagerLoadHttpModule.SiteUrl.IsNullOrEmpty()) return;

            string rdErrMsg;

            lock (Sync)
            {
                RecurJobRetry(db, out rdErrMsg);
            }

            if (errMsg != null && rdErrMsg != null) errMsg.Append(rdErrMsg);
        }

        public static void RecurJobRetry(IDB db, out string rdErrMsg)
        {
            rdErrMsg = null;

            if (DateTime.Now.AddMinutes(-15) < LastRetryTime) return;

            try
            {
                var settings = SettingsBiz.GetSettings(); 
                var errLst = new List<System.Tuple<RecurOrder, Domain.Order, string>>();

                var hSql = string.Format(@"
                select ord.Id
                from eshop.Orders as ord
                where ord.OrderStatus = {0} and ord.RecurringId is not null 
                and len(isnull(ord.TransactionId, '')) = 0 
                and ord.PaymentRetryCount > 0
                and ord.PaymentRetryStage = {1}
                and ord.PaymentRetryTimestamp < '{2:yyyy-MM-dd HH:mm:ss}' ",
                OrderStatus.PaymentWaiting.Val(), PaymentRetryStageType.Done.Val(), DateTime.Now.AddHours(-1 * settings.Hidden.PaymentRetryHours));

                var lst = db.GetSession().CreateSQLQuery(hSql).List<int>();

                foreach (var oid in lst)
                {
                    var order = db.DataService.GetObject<Domain.Order>(oid);
                    var itm = order.Recurring;
                    try
                    {
                        if (order == null || !order.PaymentMethod.IsOnlinePayment || !order.OrderStatus.InList(OrderStatus.PaymentWaiting) ||
                            order.TransactionId?.Length > 0) { rdErrMsg = "Invalid order status"; continue; }

                        LogHelper.EventLog($"RecurJobRetry: ProcessId = {Process.GetCurrentProcess()?.Id}, OrderId = {oid}, RetryHours = {settings.Hidden.PaymentRetryHours}, OldTimeStamp = {order.PaymentRetryTimestamp}, TotalHours = {(DateTime.Now - order.PaymentRetryTimestamp)?.TotalHours}");

                        order.PaymentRetryStage = (int)PaymentRetryStageType.Pending;
                        order.PaymentRetryTimestamp = DateTime.Now;

                        order.Update(db.DataService);

                        string errMsg = null;
                        PaymentData data = null;
                        var log = new OrderNoteModel() { Subject = Order.StringTable.RecurOrder, OrderStatus = order.OrderStatus, OrderId = order.Id, Private = true };
                        if (Payment.Helper.PaymentHelper.Recur(db, order, order.Recurring.Order, out errMsg, out data, out var isStopped, out var isExpired))
                        {
                            #region Processing

                            var paySetting = ((PaymentMethod)order.PaymentMethod).PaymentSetting;

                            log.OrderStatus = order.OrderStatus =
                                paySetting != null && paySetting.Id.InList(GatewayID.BankTransfer)
                                    ? (byte)OrderStatus.PayBeforeDelivery
                                    : (byte)OrderStatus.New;

                            foreach (var ditm in order.OrderDetails)
                            {
                                if (Product.DAL.ProductMgr.UpdateQuantity(db, ditm.Product, ditm.Variant, -1 * ditm.QuantityOri,
                                    false, false, true, false,
                                    new ProdQtyLogModel() { Reason = (int)ProdQtyLogReason.Ordered, RefIdFunc = () => order.Id }))
                                {
                                    ditm.Product.OrderDetailNum = null;
                                    ditm.Product.TotalSales = null;
                                    ditm.Product.Update(db.DataService);
                                }
                            }

                            OrderMgr.ProcessCustomerProduct(order, order.Customer, db, db.GetSession());
                            db.GetSession().Flush();

                            order.PaymentRetryCount = null;
                            order.PaymentRetryStage = null;
                            order.PaymentRetryTimestamp = null;
                            order.Flags &= ~(int)Domain.Interfaces.OrderFlagType.RenewPayment;

                            order.Update(db.DataService);

                            log.Body = string.Format(Order.StringTable.RecurOrderLogTemplSucceeded, new ODateTime(DateTime.Now), data.SafeProp(m => m.TransactionId));
                            OrderNoteMgr.SaveorUpdate(db, log);

                            if (!BaseController.ExecWebMethod(string.Format("/modules/eshop/WebJobOrder/SendEmail/{0}?securityCode={1}",
                                order.Id, Controllers.FrontEnd.WebJobOrderController.SecurityCode), out errMsg))
                                OrderNoteMgr.SaveorUpdate(db, new OrderNoteModel()
                                {
                                    Subject = Order.StringTable.RecurOrder,
                                    Body = string.Format(Order.StringTable.SendEmailFailedTempl, order.Id, errMsg),
                                    OrderStatus = order.OrderStatus,
                                    OrderId = order.Id,
                                    Private = true
                                });

                            #endregion   

                            itm.LastRunOk = true;
                            itm.LastRunMsg = null;
                        }
                        else
                        {
                            itm.LastRunOk = false;
                            itm.LastRunMsg = errMsg;

                            if (isStopped) itm.Status = (byte)RecurOrderStatus.Inactive;

                            log.Body = string.Format(Order.StringTable.RecurOrderLogTemplFailed, new ODateTime(DateTime.Now), errMsg);
                            OrderNoteMgr.SaveorUpdate(db, log);

                            if (isStopped)
                            {
                                order.PaymentRetryCount = null;
                                order.PaymentRetryStage = null;
                                order.PaymentRetryTimestamp = null;
                            }
                            else
                            {
                                order.PaymentRetryCount--;
                                order.PaymentRetryStage = (int)PaymentRetryStageType.Done;
                            }

                            order.Update(db.DataService);

                            if ((!isStopped || isExpired) && settings.Hidden.RecurOrderFailedSendNoti && (order.PaymentRetryCount ?? 0) <= 0) errLst.Add(new System.Tuple<RecurOrder, Domain.Order, string>(itm, order, errMsg));
                        }

                        itm.LastRunTime = DateTime.Now;
                    }
                    catch (Exception err)
                    {
                        itm.LastRunOk = false;
                        itm.LastRunMsg = err.Message;
                        itm.LastRunTime = DateTime.Now;

                        LogErr(err);

                        if (order != null)
                        {
                            var log = new OrderNoteModel() { Subject = Order.StringTable.RecurOrder, OrderStatus = order.OrderStatus, OrderId = order.Id, Private = true };
                            log.Body = string.Format(Order.StringTable.RecurOrderLogTemplFailed, new ODateTime(DateTime.Now), string.Format("Message: {0}\r\nStackTrace: {1}", err.Message, err.StackTrace.TrimLength(2000)));
                            OrderNoteMgr.SaveorUpdate(db, log);

                            order.PaymentRetryCount--;
                            order.PaymentRetryStage = (int)PaymentRetryStageType.Done;

                            order.Update(db.DataService);

                            if (settings.Hidden.RecurOrderFailedSendNoti && (order.PaymentRetryCount ?? 0) <= 0) errLst.Add(new System.Tuple<RecurOrder, Domain.Order, string>(itm, order, err.Message));
                        }
                    }

                    itm.Update(db.DataService);

                    new RecurOrderMgr().OnUpdate(itm);
                }

                #region SendEmail

                errLst.Action(itm =>
                {
                    try
                    {
                        var lang = LanguageMgr.Get(itm.Item2.LangCode, db).SafeProp(m => m.ShortLangCode).IfNull("no");
                        var query = string.Format("{0}={1}", CurrencyModel.CurCode, itm.Item2.Currency);
                        var request = new SimpleWorkerRequest(lang, query, new StreamWriter(new MemoryStream()));
                        HttpContext.Current = new HttpContext(request);
                        HttpContext.Current.Request.RequestContext.RouteData.Values.Add("lang", lang);
                        HttpContext.Current.Request.RequestContext.RouteData.Values.Add(CurrencyModel.CurCode, itm.Item2.Currency);
                        var controller = new OrderController(db.DataService);
                        controller.ControllerContext = new ControllerContext(HttpContext.Current.Request.RequestContext, controller);
                        ClawContext.Current.InitRequestData();

                        var typeSendMailHelper = Type.GetType("Claw.Modules.Eshop.Helper.SendMailHelpers, Claw.Modules.Eshop.FrontEnd");
                        if (typeSendMailHelper != null)
                        {
                            var sendMailHelper = Activator.CreateInstance(typeSendMailHelper, controller);
                            var model = OrderMgr.Get(db, itm.Item2.Id);

                            model.InitOrderDetails(db);
                            model.InitMandatoryInfo(db);

                            var emailList = sendMailHelper.SafeInvokeMethod<string>("EmailList", model, null);

                            var retSend = Singleton<ISendEmailMgr>.Default.Send(controller, itm.Item2.Id, itm.Item2.OrderStatus, itm.Item3, true, emailList);

                            var log = new OrderNoteModel()
                            {
                                OrderStatus = itm.Item2.OrderStatus,
                                OrderId = itm.Item2.Id,
                                Private = true,
                                Body = retSend ? "" : "Send failed"
                            };

                            if (settings.Hidden.RecurOneTimeEnabled)
                            {
                                log.Subject = StringTable.RecurOrderFailedSendNotiOkRecurOneTime;
                            }
                            else
                            {
                                log.Subject = StringTable.RecurOrderFailedSendNotiOk;
                            }

                            OrderNoteMgr.SaveorUpdate(db, log);
                        }
                        else LogErr(new Exception("RecurOrderMgr.DoJobRetry send email failed, typeSendMailHelper is null"));
                    }
                    catch (Exception err)
                    {
                        LogErr(err);
                    }
                    finally { HttpContext.Current = null; }

                });

                #endregion

            }
            catch (Exception err)
            {
                rdErrMsg = err.Message;
                LogHelper.Log4net(err, ClawExceptionType.DataProvider, "RecurJobRetry");
            }

            LastRetryTime = DateTime.Now;
        }
        #endregion

        #region OnUpdate
        protected override void OnUpdate(RecurOrder obj)
        {
        }
        #endregion

        #region OnDelete
        protected override void OnDelete(RecurOrder obj)
        {
        }
        #endregion
    }
}