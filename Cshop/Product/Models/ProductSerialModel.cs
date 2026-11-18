using System;
using System.ComponentModel.DataAnnotations;
using Claw.Modules.Eshop.DAL;
using Claw.Modules.Eshop.Domain;
using Claw.Modules.Eshop.Helper;

namespace Claw.Modules.Eshop.Models
{
    public class ProductSerialModel
    {
        public int Id { get; set; }

        [Display(Name = "Serial nr")]
        [Required(ErrorMessage = StringTable.ErrMsgRequiredField)]
        public string SerialNumber { get; set; }

        public bool? Sold { get; set; }

        public int? ProductId { get; set; }

        public string Key { get { return SerialNumber; } }

        public int Iseq { get; set; }

        public bool IsDataModelEmpty
        {
            get { return SerialNumber.IsNullOrEmpty(); }
        }

    }
}