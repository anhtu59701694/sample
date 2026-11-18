using Claw.Modules.Eshop.DAL;
using System.ComponentModel.DataAnnotations;

namespace Claw.Modules.Eshop.Models
{
    public class ImageLangModel : ILanguage
    {
        public int Id { get; set; }

        [StringLength(400, ErrorMessage = StringTable.ErrMsgLengthLimit)]
        public string Title { get; set; }

        public string LangCode { get; set; }

        public int ImageId { get; set; }

        #region IsDataModelEmpty
        public bool IsDataModelEmpty
        {
            get
            {
                return string.IsNullOrWhiteSpace(Title);
            }
        }
        #endregion
    }
}