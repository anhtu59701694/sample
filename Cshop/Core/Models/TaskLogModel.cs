namespace Claw.Modules.Eshop.Models
{
    public class TaskLogModel
    {
        public int Id { get; set; }

        public int TaskId { get; set; }

        public string Data { get; set; }

        public ODateTime CreatedDate { get; set; }
    }
}