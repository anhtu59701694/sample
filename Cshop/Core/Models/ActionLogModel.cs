using Claw.Modules.Eshop.Models;
using System;
using System.ComponentModel.DataAnnotations;

namespace Claw.Modules.Eshop.Models
{
    public class ActionLogModel
    {
        public int Id { get; set; }

        [Display(Name = "Reference id")]
        public int? RefId { get; set; }

        public int Action { get; set; }

        public string Function { get { return typeof (LogActionType).LocalizeEnumValue(Action & 0x1F); } }

        [Display(Name = "Activity")]
        public string Activity { get; set; }

        [Display(Name = "Account information")]
        public string AuditInfo { get; set; }

        [Display(Name = "User ip")]
        public string UserIp { get; set; }

        [Display(Name = "Username")]
        public string UserName { get; set; }

        [Display(Name = "User host")]
        public string UserHost { get; set; }

        [Display(Name = "User agent")]
        public string UserAgent { get; set; }

        [Display(Name = "Created date")]
        public ODateTime CreatedDate { get; set; }
    }
}