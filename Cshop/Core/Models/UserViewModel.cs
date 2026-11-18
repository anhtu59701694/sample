using System;

namespace Claw.Modules.Eshop.Models
{
    public class UserViewModel 
    {
        public Guid Id { get; set; }

        public Guid UserId { get; set; }

        public int RefId { get; set; }

        public int Type { get; set; }

        public int Count { get; set; }

        public DateTime Timestamp { get; set; }

        public static string GetKey(Guid userid, UserViewType type, int refId) { return string.Format("{0}:{1}:{2}", userid, type.Val(), refId); }
    }
}