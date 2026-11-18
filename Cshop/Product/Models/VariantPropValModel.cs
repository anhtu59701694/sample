using Claw.Modules.Eshop.DAL;

namespace Claw.Modules.Eshop.Models
{
    public class VariantPropValModel
    {
        private int? _propId;

        public int Id { get; set; }

        public int? PropValId { get; set; }

        public string PropValName { get; set; }

        public int VariantId { get; set; }

        public PropertyModel Property { get; set; }

        public int PropId
        {
            get { return _propId ?? (Property == null ? 0 : Property.Id); }
            set { _propId = value; }
        }

        #region IsDataModelEmpty
        public bool IsDataModelEmpty(IDB db)
        {
            return PropValId == null /*|| PropValMgr.GetDom(db, PropValId ?? 0) == null*/;
        }
        #endregion
    }
}