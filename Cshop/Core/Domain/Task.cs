using System;
using System.Collections.Generic;

namespace Claw.Modules.Eshop.Domain
{
    public partial class Task : Tuple<TaskSetting, TaskLog>
    {
        public virtual int Id { get; set; }
        public virtual string Name { get; set; }
        public virtual bool Active { get; set; }
        public virtual bool IsService { get; set; }
        public virtual DateTime? LastRunTime { get; set; }
        public virtual DateTime? LastDoneTime { get; set; }
        public virtual string LastRunMsg { get; set; }
        public virtual string ExeClassInfo { get; set; }

        /// <summary>minutes</summary>
        public virtual int Interval { get; set; }

        public virtual IList<TaskSetting> TaskSettings
        {
            get { return Get(ref _list0); }
            set { _list0 = value; }
        }

        public virtual IList<TaskLog> TaskLogs
        {
            get { return Get(ref _list1); }
            set { _list1 = value; }
        }
    }
}