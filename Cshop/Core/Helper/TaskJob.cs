using System;
using System.Linq;
using Claw.Core;
using Claw.Core.DAL;
using Claw.Core.Logs;
using Claw.Modules.Eshop.Models;
using NHibernate;
using Ninject;
using Quartz;
using Claw.Modules.Eshop.DAL;

namespace Claw.Modules.Eshop.Helper
{
    #region ITaskJob
    public interface ITaskJob
    {
        bool Run(IDB db, TaskModel task);
    } 
    #endregion

    [DisallowConcurrentExecution]
    public class TaskJob : IJob
    {
        public static volatile bool Enable = true;

        #region Execute

        public void Execute(IJobExecutionContext context)
        {
            if (!Enable) return;
            try
            {
                var db = new DefaultDb(new DataService(IoC.Kernel.Get<ISession>()));
                var tasks = TaskMgr.GetAll(db);

                foreach (var tsk in tasks.Where(x => x.Active && !x.IsService))
                {
                    if (tsk.LastDoneTime == null || tsk.LastRunTime == null ||
                        tsk.LastRunTime <= DateTime.Now.AddMinutes(-1*tsk.Interval))
                    {
                        tsk.LastRunTime = DateTime.Now;

                        try
                        {
                            var type = Type.GetType(tsk.ExeClassInfo, true, true);
                            var jExec = Activator.CreateInstance(type) as ITaskJob;
                            if (jExec == null)
                                tsk.LastRunMsg = string.Format("ITaskJob required for {0}", type.FullName);
                            else
                            {
                                tsk.LastRunMsg = null;
                                var retCode = jExec.Run(db, tsk);
                                tsk.LastRunMsg = tsk.LastRunMsg ?? (retCode ? "OK" : "FAILED");
                            }
                        }
                        catch (Exception ter)
                        {
                            tsk.LastRunMsg = ter.Message + "\r\n" + ter.StackTrace;
                        }

                        tsk.LastDoneTime = DateTime.Now;

                        TaskMgr.Update(db, tsk);
                    }
                }
            }
            catch (Exception err)
            {
                LogHelper.Log(err, err.Message);
            }
        }

        #endregion
    }
}