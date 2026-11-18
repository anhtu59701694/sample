using System.ComponentModel.DataAnnotations;

namespace Claw.Modules.Eshop.Models
{
    public class AddressModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = StringTable.ErrMsgRequiredField)]
        [StringLength(100, ErrorMessage = StringTable.ErrMsgLengthLimit)]
        public string Name { get; set; }

        [StringLength(40, ErrorMessage = StringTable.ErrMsgLengthLimit)]
        public string Phone { get; set; }

        [StringLength(40, ErrorMessage = StringTable.ErrMsgLengthLimit)]
        public string Mobile { get; set; }

        [Required(ErrorMessage = StringTable.ErrMsgRequiredField)]
        [StringLength(100, ErrorMessage = StringTable.ErrMsgLengthLimit)]
        public string Street { get; set; }

        [Required(ErrorMessage = StringTable.ErrMsgRequiredField)]
        [StringLength(20, ErrorMessage = StringTable.ErrMsgLengthLimit)]
        public string Postnr { get; set; }

        [Required(ErrorMessage = StringTable.ErrMsgRequiredField)]
        [StringLength(50, ErrorMessage = StringTable.ErrMsgLengthLimit)]
        public string Posted { get; set; }

        [StringLength(50, ErrorMessage = StringTable.ErrMsgLengthLimit)]
        public string City { get; set; }

        [Display(Name = "Country")]
        [Required(ErrorMessage = StringTable.ErrMsgRequiredField)]
        public int? CountryId { get; set; }

        [Display(Name = "Created date")]
        public ODateTime CreatedDate { get; set; }

        [Display(Name = "Default")]
        public bool? IsDefault { get; set; }

        public int? CustomerId { get; set; }

        public bool IsDataModelEmpty
        {
            get
            {
                return Name.IsNullOrEmpty() && Phone.IsNullOrEmpty() && Mobile.IsNullOrEmpty() && Street.IsNullOrEmpty() &&
                       Postnr.IsNullOrEmpty() && Posted.IsNullOrEmpty() && City.IsNullOrEmpty();
            }
        }
    }
}