namespace Claw.Modules.Eshop.Domain.Interfaces
{
    public interface ICustomer : IDom
    {
        string ContactName { get; set; }
        string Mobile { get; set; }
        string Telephone { get; set; }
        string Email { get; set; }

        string BillingName { get; set; }
        string BillingAddress { get; set; }
        string BillingMobile { get; set; }
        string BillingPosted { get; set; }
        string BillingPostnr { get; set; }
        Country BillingCountry { get; set; }

        string DeliverName { get; set; }
        string DeliverAddress { get; set; }
        string DeliverMobile { get; set; }
        string DeliverPosted { get; set; }
        string DeliverPostnr { get; set; }
        Country DeliverCountry { get; set; }      
    }
}