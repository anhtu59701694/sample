using System.ComponentModel.DataAnnotations;

namespace Claw.Modules.Eshop.Models
{
    public class DistrictModel
    {
        public int Id { get; set; }

        public bool InlineEdit { get; set; }

        public bool QuickAdd { get; set; }

        [Required(ErrorMessage = StringTable.ErrMsgRequiredField)]
        [StringLength(100, ErrorMessage = StringTable.ErrMsgLengthLimit)]
        public string Name { get; set; }

        public int? CityId { get; set; }

        public string CityName { get; set; }
    }
}