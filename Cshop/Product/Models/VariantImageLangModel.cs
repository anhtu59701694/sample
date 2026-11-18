using System.ComponentModel.DataAnnotations;

namespace Claw.Modules.Eshop.Models
{
    public class VariantImageLangModel : ILanguage
    {
        public int Id { get; set; }

        [StringLength(500, ErrorMessage = StringTable.ErrMsgLengthLimit)]
        public string Title { get; set; }

        public string LangCode { get; set; }

        public int VariantImageId { get; set; }

        public bool IsDataModelEmpty {get { return string.IsNullOrWhiteSpace(Title); }}
    }
}