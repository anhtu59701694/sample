namespace Claw.Modules.Eshop.Domain
{
    public partial class TechSpec
    {
        public virtual string GetIcon
        {
            get
            {
                if (!string.IsNullOrEmpty(Icon))
                    return ModuleUrl.ImagePath(Icon).TrimStart('~');

                return "/Claw/Sites/Default/Modules/eshop/Images/TechSpec/no-img.jpg";
            }
        }

        public virtual string GetImageThumb
        {
            get
            {
                if (!string.IsNullOrEmpty(Icon))
                    return ModuleUrl.ImageThumbPath(Icon).TrimStart('~');

                return "/Claw/Sites/Default/Modules/eshop/Images/TechSpec/no-thumb-img.jpg";

            }
        }

        public virtual string Code
        {
            get { return string.Format("tspec{0}", Id); }
        }
    }
}