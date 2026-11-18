using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Web;
using System.Web.Mvc;
using Claw.Modules.Eshop.SmsServiceReference;
using System.Configuration;
using Claw.Core.Logs;
using Claw.Core;

namespace Claw.Modules.Eshop.Helper
{
    public static class SmsHelper
    {
        public static bool SendSMS(string countryCode, string smsFrom, string smsTo, string smsMessage, string orderId = "", string note = "") 
        {
            return SendSMSEx(false, countryCode, smsFrom, smsTo, smsMessage, orderId, note);
        }
        public static bool SendSMSEx(bool prefix, string countryCode, string smsFrom, string smsTo, string smsMessage, string orderId = "", string note = "")
        {
            try
            {
                if (prefix)
                {
                    countryCode = countryCode.IfNull("47");

                    if (smsTo.TrimStart('+', '0').Length <= 8) smsTo = "+" + countryCode.TrimStart('+') + smsTo.TrimStart('+', '0');
                    else if (!smsTo.StartsWith("00") && !smsTo.StartsWith("+")) smsTo = "+" + smsTo;
                }

                orderId = !string.IsNullOrEmpty(orderId) ? orderId : ClawContext.Current.User.Id.ToString();

                var authSoapHd = new AuthSoapHd();
                authSoapHd.UserName = ConfigurationManager.AppSettings["Sms_AuthUser"] ?? "CatSMSService2";
                authSoapHd.Password = ConfigurationManager.AppSettings["Sms_AuthPass"] ?? "6fd62cf86d401905e7011ea34610c4d0";

                var smsUsername = ConfigurationManager.AppSettings["Sms_Username"] ?? "CatenDemo";
                var smsPassword = ConfigurationManager.AppSettings["Sms_Password"] ?? "demo-Cat!2008";

                var serviceSoap = new ServiceSoapClient();
                return serviceSoap.CatSMSsend(authSoapHd, orderId, countryCode, smsMessage, smsFrom, smsTo, smsPassword, smsUsername, note);
            }
            catch (Exception exp)
            {
                var msg = string.Format("orderId: {0}, code: {1}, from: {2}, to: {3}, message: {4}, note: {5}",
                                        orderId, countryCode, smsFrom, smsTo, smsMessage, note);
                LogHelper.LogText(new Exception(msg, exp), Claw.Core.ClawExceptionType.UnknownError);
                return false;
            }
        }
    }
}