using System;
using System.ComponentModel.DataAnnotations;
using Claw.Core;
using System.Linq;
using System.Collections.Generic;
using Claw.Modules.Eshop.DAL;
using Claw.Modules.Eshop.Domain;

namespace Claw.Modules.Eshop.Models
{
    public class ImageModel : BaseModel<Image, ImageLangModel>, IPhoto
    {
        public static string NoImg = ModuleUrl.ContentUrl("Images/Product/no-thumb-img.jpg", true);

        [Required(ErrorMessage = StringTable.ErrMsgRequiredField)]
        public string FileName { get; set; }

        [Required(ErrorMessage = StringTable.ErrMsgRequiredField)]
        public int DisplayOrder { get; set; }

        [Display(Name = "Image")]
        public string Thumb { get; set; }

        public string FsUrl { get; set; }

        public string Key
        {
            get { return Id == 0 ? FsUrl : FileName; }
        }

        #region Title

        public string Title
        {
            get { return GetLang(m => m.Title) ?? ""; }
        }

        #endregion

        #region IsDataModelEmpty

        public bool IsDataModelEmpty
        {
            get
            {
                return string.IsNullOrWhiteSpace(FileName);
            }
        }

        #endregion
    }
}