namespace Claw.Modules.Eshop.Models
{
    public class ProductTechSValModel
    {
        public int Id { get; set; }

        public int? ProductId { get; set; }

        public int? TechSpecId { get; set; }
        
        public string TechSpecName { get; set; }

        public bool TechSpecActive { get; set; }
        
        public int? TechSpecValId { get; set; }

        public string TechSpecValName { get; set; }

        public bool Selected { get; set; }

        public bool IsDataModelEmpty
        {
            get { return !Selected || ((TechSpecValId ?? 0) == 0 && TechSpecValName.IsNullOrEmpty()); }
        }
    }
}