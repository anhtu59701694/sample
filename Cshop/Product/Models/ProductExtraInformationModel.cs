using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Claw.Modules.Eshop.Models
{
    public class ProductExtraInformationModel
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public string FieldName { get; set; }
        public string FieldValue { get; set; }
    }
}