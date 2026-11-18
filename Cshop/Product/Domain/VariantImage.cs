using System.Collections.Generic;

namespace Claw.Modules.Eshop.Domain
{
    public partial class VariantImage : Tuple<VariantImageLang>
    {

        public virtual int Id { get; set; }
        public virtual VariantBase Variant { get; set; }
        public virtual string FileName { get; set; }
        public virtual int DisplayOrder { get; set; }

        public virtual IList<VariantImageLang> Languages
        {
            get { return Get(ref _list0); }
            set { _list0 = value; }
        }
    }
}