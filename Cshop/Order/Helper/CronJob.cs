using Claw.Core;
using Claw.Core.DAL;
using Claw.Core.Logs;
using Claw.Modules.Eshop.Controllers.Admin;
using Claw.Modules.Eshop.DAL;
using NHibernate;
using Ninject;
using Quartz;
using System;
using System.Net;
using System.Text;

namespace Claw.Modules.Eshop.Helper
{
    [DisallowConcurrentExecution]
    public class OrderCronJob : IJob
    {
        public static volatile bool Enable = true;

        public void Execute(IJobExecutionContext context)
        {
            if (!Enable) return;

            try
            {
                var dataMap = context.JobDetail.JobDataMap;
                var db = new DefaultDb(new DataService(IoC.Kernel.Get<ISession>()));
                var site = db.DataService.GetCurrentSite();
                var settings = Helper.SettingsBiz.GetSettings();

                if (settings.Hidden.RecurringOrder) { 
                    RecurOrderMgr.DoJob(db);
                    RecurOrderMgr.RecurJobRetryEx(db, new StringBuilder());
                }
                if (settings.Hidden.CartJobEnable && settings.Hidden.CartJobActiveHour > 0 && settings.Hidden.CartJobMinHour > 0 && settings.Hidden.CartJobMaxHour > settings.Hidden.CartJobMinHour)
                    CartMgr.DoJob(db, settings.Hidden.CartJobMinHour, settings.Hidden.CartJobActiveHour, settings.Hidden.CartJobMaxHour);
            }
            catch (Exception err)
            {
                LogHelper.Log(err, err.Message);
            }
        }
    }
}