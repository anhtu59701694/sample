using System;

namespace Claw.Modules.Eshop.Models
{
    public class ProductSaleModel
    {
        public long Id { get; set; }

        public DateTime DateMin { get; set; }

        public int Year { get; set; }

        public int Month { get; set; }

        public int? ProductId { get; set; }

        public int? Quantity { get; set; }

        public int? Orders { get; set; }

        public DateTime Timestamp { get; set; }
    }
}