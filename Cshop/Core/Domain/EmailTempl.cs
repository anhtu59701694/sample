using System.Collections.Generic;

namespace Claw.Modules.Eshop.Domain
{
    public partial class EmailTempl : Tuple<EmailTemplLang>
    {
        public const string EmailtemplIDEqual = "9";

        public virtual int Id { get; set; }

        public virtual string Code { get; set; }

        public virtual string Name { get; set; }

        public virtual int? Type { get; set; }

        public virtual string AttachFile { get; set; }

        public virtual string From { get; set; }

        public virtual string ReplyTo { get; set; }

        public virtual IList<EmailTemplLang> Languages
        {
            get { return Get(ref _list0); }
            set { _list0 = value; }
        }
    }
}