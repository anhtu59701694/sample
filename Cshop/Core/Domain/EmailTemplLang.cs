namespace Claw.Modules.Eshop.Domain
{
    public partial class EmailTemplLang
    {
        public virtual int Id { get; set; }

        public virtual EmailTempl EmailTempl { get; set; }

        public virtual string Subject { get; set; }

        public virtual string Body { get; set; }

        public virtual string LangCode { get; set; }
    }
}