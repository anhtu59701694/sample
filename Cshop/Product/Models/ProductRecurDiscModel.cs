using System;
using Claw.Modules.Eshop;

namespace Claw.Modules.Eshop.Models
{
    public class ProductRecurDiscModel
    {
        public int Id { get; set; }

        public int? Type { get; set; }

        public int? RecurEach { get; set; }

        public string TypeTxt
        {
            get
            {
                return (RecurEach ?? 1) == 1 || Type == null 
                    ? typeof (RecurringType).LocalizeEnumValue(Type ?? 0)
                    : (Enum.GetName(typeof(RecurringType), Type) + " each " + RecurEach).ModLocalize();
            }
        }

        public OFloat Value { get; set; }

        public bool DefOpt { get; set; }

        public int? ProductId { get; set; }

        public int? DiscountId { get; set; }

        #region IsDataModelEmpty

        public bool IsDataModelEmpty
        {
            get { return !(Value > 0m) && !DefOpt; }
        }

        #endregion

    }
}