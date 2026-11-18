using System.ComponentModel.DataAnnotations;
using Claw.Core.DAL;
using Claw.Modules.Eshop.DAL;
using Claw.Modules.Eshop.Domain;

namespace Claw.Modules.Eshop.Models
{
    public class InvoiceModel
    {
        public int Id { get; set; }

        public string Data { get; set; }

        public int? OrderId { get; set; }

        public ODateTime CreatedDate { get; set; }

        public int? pos_ShopID { get; set; }
    }
}