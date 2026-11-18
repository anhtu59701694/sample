using Claw.Modules.Eshop.Domain.Interfaces;
using System;
using System.Collections.Generic;

namespace Claw.Modules.Eshop.Domain
{
    public partial class Customer : Tuple<Order, CustomerCGroup, WishProduct, CategoryCustomer, CustomerProduct, Address, Customer>, ICustomer
    {
        public virtual int Id { get; set; }

        string _email;
        public virtual string Email 
        {
            get { return 0 != (Flags & (int)Models.CustomerFlagType.Dependent) ? Owner?.Email : _email; }
            set { _email = value; } 
        }
        public virtual string Password { get; set; } 
        public virtual string PasswordSalt { get; set; }
        public virtual string OrgNumber { get; set; }
        public virtual string Telephone { get; set; }
        public virtual string Mobile { get; set; }
        public virtual string ContactName { get; set; }
        public virtual string CompanyName { get; set; }

        public virtual string DeliverName { get; set; }
        public virtual string DeliverPhone { get; set; }
        public virtual string DeliverMobile { get; set; }
        public virtual string DeliverAddress { get; set; }
        public virtual string DeliverPostnr { get; set; }
        public virtual string DeliverPosted { get; set; }
        public virtual string DeliverCity { get; set; }
        public virtual Country DeliverCountry { get; set; }

        public virtual string BillingName { get; set; }
        public virtual string BillingPhone { get; set; }
        public virtual string BillingMobile { get; set; }
        public virtual string BillingAddress { get; set; }
        public virtual string BillingPostnr { get; set; }
        public virtual string BillingPosted { get; set; }
        public virtual string BillingCity { get; set; }
        public virtual Country BillingCountry { get; set; }

        public virtual DateTime CreatedDate { get; set; }
        public virtual DateTime ModifiedDate { get; set; }

        public virtual string TokenId { get; set; }
        public virtual DateTime? TokenExpired { get; set; }

        public virtual string OneTimeToken { get; set; }
        public virtual DateTime? OneTimeExpiry { get; set; }
        public virtual DateTime? OneTimeLastSms { get; set; }

        public virtual int? OrderNum { get; set; }   
        public virtual int Status { get; set; }
        public virtual string StatsInfo { get; set; }
        public virtual string Comments { get; set; }
        public virtual string Data { get; set; }

        public virtual bool? RecvOfferInfo { get; set; }
        public virtual string InvoiceEmail { get; set; }
        public virtual int Flags { get; set; }
        public virtual decimal? DiscountRate { get; set; }
        public virtual int? AddressNum { get; set; }
        public virtual Address Address { get; set; }
        public virtual Company Company { get; set; }
        public virtual Customer Owner { get; set; }

        public virtual IList<Order> Orders
        {
            get { return Get(ref _list0); }
            set { _list0 = value; }
        }

        public virtual IList<CustomerCGroup> CustomerCGroups
        {
            get { return Get(ref _list1); }
            set { _list1 = value; }
        }

        public virtual IList<WishProduct> WishProducts
        {
            get { return Get(ref _list2); }
            set { _list2 = value; }
        }

        public virtual IList<CategoryCustomer> CategoryCustomers
        {
            get { return Get(ref _list3); }
            set { _list3 = value; }
        }

        public virtual IList<CustomerProduct> CustomerProducts
        {
            get { return Get(ref _list4); }
            set { _list4 = value; }
        }

        public virtual IList<Address> Addresses
        {
            get { return Get(ref _list5); }
            set { _list5 = value; }
        }
        public virtual IList<Customer> Dependants
        {
            get { return Get(ref _list6); }
            set { _list6 = value; }
        }
    }
}