using System;
using System.Text;
using System.Collections.Generic;

namespace Claw.Modules.Eshop.Domain
{
    public partial class ImageLang
    {
        public virtual int Id { get; set; }
        public virtual string Title { get; set; }
        public virtual string LangCode { get; set; }
        public virtual Image Image { get; set; }
    }
}