namespace Claw.Modules.Eshop.Models
{
    public class Price4LoginCustGroupModel
    {
        public int CustGroupId { get; set; }

        public string CustGroupName { get; set; }

        public bool Selected { get; set; }

        public bool IsDataModelEmpty
        {
            get { return !Selected; }
        }
    }
}