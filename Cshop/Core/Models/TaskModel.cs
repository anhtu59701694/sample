namespace Claw.Modules.Eshop.Models
{
    public class TaskModel
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public bool Active { get; set; }

        public bool IsService { get; set; }

        public ODateTime LastRunTime { get; set; }

        public ODateTime LastDoneTime { get; set; }

        public string LastRunMsg { get; set; }

        public string ExeClassInfo { get; set; }

        /// <summary>minutes</summary>
        public int Interval { get; set; }
    }
}