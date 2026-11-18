using System.Linq;
using System.Web.Mvc;
using AutoMapper;
using Claw.Core.DAL;
using Claw.Modules.Eshop.DAL;
using Claw.Modules.Eshop.Domain;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;
using Claw.Modules.Eshop.Helper;
using System.Web;
using System.Web.Script.Serialization;
using System.ComponentModel;

namespace Claw.Modules.Eshop.Models
{
    public enum CustStatus { Pending = 0, Active = 1, Inactive = 2, Deleted = 3, Disapproved = 4 }

    public enum CustomerFlagType 
    { 
        MembershipNotiBlue = 1, MembershipNotiSilver = 2, MembershipNotiGold = 4, 
        CustCompanyInited = 8, EHFInvoice = 16, OrderAutoRegister = 32, EmailAccWelcomeSent = 64, 

        /// <summary>NoEmail, Email from OwnerId</summary>
        Dependent = 128 
    }

    [CustomValidation(typeof(CustomerModel), "Validate")]
    public class CustomerModel
    {
        #region StatsType

        public class StatsType
        {
            public int? Orders { get; set; }

            [Display(Name = "Total Sales")]
            public OMoney TotalSales { get; set; }
        }

        #endregion

        #region Raw
        protected Customer _raw;

        [JsonIgnore]
        [ScriptIgnore(ApplyToOverrides = true)]
        public Customer Raw { get { return null; } set { _raw = value; } }

        public Customer GetRaw() { return _raw = _raw ?? new Domain.Customer(); }
        #endregion

        public CustomerModel()
        {
            CreatedDate = DateTime.Now;
        }

        [Exportable(false, Name = "Kundenr.")]
        public int Id { get; set; }

        public bool InlineEdit { get; set; }

        [Display(Name = "Customer type")]
        public bool? Individual { get; set; }

        [Remote("ValidateEmail", "Customer", AdditionalFields = "Id", HttpMethod = "POST", ErrorMessage = StringTable.ErrMsgUniqueConstraint)]
        [Exportable(false, Name = "Epost")]
        [Required(ErrorMessage = StringTable.ErrMsgRequiredField)]
        [StringLength(150, ErrorMessage = StringTable.ErrMsgLengthLimit)]
        [EmailAddress(ErrorMessage = StringTable.ErrMsgInvalidEmail)]
        public string Email { get; set; }

        [StringLength(100, ErrorMessage = StringTable.ErrMsgLengthLimit)]
        public string Password { get; set; }

        [StringLength(100, ErrorMessage = StringTable.ErrMsgLengthLimit)]
        public string PasswordSalt { get; set; }

        [StringLength(50, ErrorMessage = StringTable.ErrMsgLengthLimit)]
        //[RegularExpression("[0-9]+", ErrorMessage = StringTable.ErrMsgRequireNumber)]
        [Display(Name = "OrgNr")]
        public string OrgNumber { get; set; }

        [StringLength(20, ErrorMessage = StringTable.ErrMsgLengthLimit)]
        public string Telephone { get; set; }

        [StringLength(20, ErrorMessage = StringTable.ErrMsgLengthLimit)]
        public string Mobile { get; set; }

        [StringLength(100, ErrorMessage = StringTable.ErrMsgLengthLimit)]
        [Display(Name = "C/O name")]
        public string ContactName { get; set; }

        [Exportable(false, Name = "Kontakt-fornavn")]
        public string ContactFirstName { get {return ContactName.GetFirstName(); } }

        [Exportable(false, Name = "Kontakt-etternavn")]
        public string ContactLastName { get { return ContactName.GetLastName(); } }

        [StringLength(100, ErrorMessage = StringTable.ErrMsgLengthLimit)]
        [Display(Name = "Company name")]
        public string CompanyName { get; set; }

        //[Required(ErrorMessage = StringTable.ErrMsgRequiredField)]
        [Exportable(Name = "Deliver name")]
        [StringLength(100, ErrorMessage = StringTable.ErrMsgLengthLimit)]
        [Display(Name = "Name")]
        public string DeliverName { get; set; }

        //[Required(ErrorMessage = StringTable.ErrMsgRequiredField)]
        [Exportable(Name = "Deliver phone")]
        [StringLength(20, ErrorMessage = StringTable.ErrMsgLengthLimit)]
        [Display(Name = "Phone")]
        public string DeliverPhone { get; set; }

        //[Required(ErrorMessage = StringTable.ErrMsgRequiredField)]
        [Exportable(Name = "Deliver mobile")]
        [StringLength(20, ErrorMessage = StringTable.ErrMsgLengthLimit)]
        [Display(Name = "Mobile")]
        public string DeliverMobile { get; set; }

        //[Required(ErrorMessage = StringTable.ErrMsgRequiredField)]
        [Exportable(Name = "Deliver address")]
        [StringLength(100, ErrorMessage = StringTable.ErrMsgLengthLimit)]
        [Display(Name = "Address")]
        public string DeliverAddress { get; set; }

        //[Required(ErrorMessage = StringTable.ErrMsgRequiredField)]
        [Exportable(Name = "Deliver postnr")]
        [StringLength(10, ErrorMessage = StringTable.ErrMsgLengthLimit)]
        [Display(Name = "Postnr")]
        public string DeliverPostnr { get; set; }

        //[Required(ErrorMessage = StringTable.ErrMsgRequiredField)]
        [Exportable(Name = "Deliver posted")]
        [StringLength(50, ErrorMessage = StringTable.ErrMsgLengthLimit)]
        [Display(Name = "Posted")]
        public string DeliverPosted { get; set; }

        //[Required(ErrorMessage = StringTable.ErrMsgRequiredField)]
        [StringLength(50, ErrorMessage = StringTable.ErrMsgLengthLimit)]
        [Display(Name = "City")]
        public string DeliverCity { get; set; }

        //[Required(ErrorMessage = StringTable.ErrMsgRequiredField)]
        [Display(Name = "Country")]
        public int? DeliverCountryId { get; set; }

        [Exportable(Name = "Deliver country name")]
        [Display(Name = "Country name")]
        public string DeliverCountryName { get { return _raw == null ? "" : _raw.DeliverCountry.GetName(); } }

        [Exportable(Name = "Deliver country code")]
        [Display(Name = "Country code")]
        public string DeliverCountryCode { get { return _raw == null || _raw.DeliverCountry == null ? "NO" : _raw.DeliverCountry.Code; } }

        //[Required(ErrorMessage = StringTable.ErrMsgRequiredField)]
        [Exportable(Name = "Billing name")]
        [StringLength(100, ErrorMessage = StringTable.ErrMsgLengthLimit)]
        [Display(Name = "Name")]
        public string BillingName { get; set; }

        //[Required(ErrorMessage = StringTable.ErrMsgRequiredField)]
        [Exportable(Name = "Billing phone")]
        [StringLength(20, ErrorMessage = StringTable.ErrMsgLengthLimit)]
        [Display(Name = "Phone")]
        public string BillingPhone { get; set; }

        //[Required(ErrorMessage = StringTable.ErrMsgRequiredField)]
        [Exportable(Name = "Billing mobile")]
        [StringLength(20, ErrorMessage = StringTable.ErrMsgLengthLimit)]
        [Display(Name = "Mobile")]
        public string BillingMobile { get; set; }

        //[Required(ErrorMessage = StringTable.ErrMsgRequiredField)]
        [Exportable(Name = "Billing address")]
        [StringLength(100, ErrorMessage = StringTable.ErrMsgLengthLimit)]
        [Display(Name = "Address")]
        public string BillingAddress { get; set; }

        //[Required(ErrorMessage = StringTable.ErrMsgRequiredField)]
        [Exportable(Name = "Billing postnr")]
        [StringLength(10, ErrorMessage = StringTable.ErrMsgLengthLimit)]
        [Display(Name = "Postnr")]
        public string BillingPostnr { get; set; }

        //[Required(ErrorMessage = StringTable.ErrMsgRequiredField)]
        [Exportable(Name = "Billing posted")]
        [StringLength(50, ErrorMessage = StringTable.ErrMsgLengthLimit)]
        [Display(Name = "Posted")]
        public string BillingPosted { get; set; }

        //[Required(ErrorMessage = StringTable.ErrMsgRequiredField)]
        [StringLength(50, ErrorMessage = StringTable.ErrMsgLengthLimit)]
        [Display(Name = "City")]
        public string BillingCity { get; set; }

        //[Required(ErrorMessage = StringTable.ErrMsgRequiredField)]
        [Display(Name = "Country")]
        public int? BillingCountryId { get; set; }

        [Exportable(Name = "Billing country code")]
        public string BillingCountryCode { get { return _raw == null || _raw.BillingCountry == null ? "NO" : _raw.BillingCountry.Code; } }

        [Exportable(Name = "Billing country name")]
        public string BillingCountryName { get { return _raw == null ? "" : _raw.BillingCountry.GetName(); } }

        [Display(Name = "Created date")]
        [JsonConverter(typeof(ODateTime.JsonConverter))]
        public ODateTime CreatedDate { get; set; }

        [JsonConverter(typeof(ODateTime.JsonConverter))]
        public ODateTime ModifiedDate { get; set; }

        public string TokenId { get; set; }
        
        public DateTime? TokenExpired { get; set; }

        public string OneTimeToken { get; set; }
        public DateTime? OneTimeExpiry { get; set; }
        public DateTime? OneTimeLastSms { get; set; }


        [Display(Name = "Receive offers and information")]
        public bool? RecvOfferInfo { get; set; }

        [Display(Name = "Invoice email")]
        [EmailAddress(ErrorMessage = StringTable.ErrMsgInvalidEmail)]
        public string InvoiceEmail { get; set; }

        [Display(Name = "Discount rate")]
        [Range(typeof(OFloat), "0", "100", ErrorMessage = StringTable.ErrMsgRangeLimit)]
        public OFloat DiscountRate { get; set; }

        [Display(Name = "Groups")]
        public List<CustomerCGroupModel> CustGroups { get; set; }

        [Display(Name = "Discount rate for categories")]
        public List<CategoryCustomerModel> CustCategories { get; set; }

        [Display(Name = "Addresses")]
        public List<AddressModel> CustAddresses { get; set; }

        public int? AddressId { get; set; }

        public int? CompanyId { get; set; }

        [Display(Name = "Company owner")]
        public bool? IsCompanyOwner
        {
            get { return _raw == null || _raw.OrgNumber.IsNullOrEmpty() || _raw.CompanyName.IsNullOrEmpty() ? (bool?)null : _raw.Company != null && _raw.Company.Owner.SafeProp(m => m.Id) == Id; }
        }

        public int? OwnerId { get; set; }

        public int? Orders { get; set; }

        public int? Addresses { get; set; }         
 
        public int? Status { get; set; }

        public string StatusTxt { get { return typeof (CustStatus).LocalizeEnumValue(Status ?? 0); } }

        public StatsType StatsInfo { get; set; }

        public int Flags { get; set; }

        [StringLength(4000, ErrorMessage = StringTable.ErrMsgLengthLimit)]
        public string Comments { get; set; }

        #region Fill

        public void Fill(OrderModel order)
        {
            Mobile = order.BillingMobile;
            Telephone = order.BillingPhone;

            DeliverName = order.DeliverName;
            DeliverPhone = order.DeliverPhone;
            DeliverMobile = order.DeliverMobile;
            DeliverAddress = order.DeliverAddress;
            DeliverPostnr = order.DeliverPostnr;
            DeliverPosted = order.DeliverPosted;
            DeliverCity = order.DeliverCity;
            DeliverCountryId = order.DeliverCountryId;

            BillingName = order.BillingName;
            BillingPhone = order.BillingPhone;
            BillingMobile = order.BillingMobile;
            BillingAddress = order.BillingAddress;
            BillingPostnr = order.BillingPostnr;
            BillingPosted = order.BillingPosted;
            BillingCity = order.BillingCity;
            BillingCountryId = order.BillingCountryId;
        }

        #endregion

        #region InitCustAddresses

        public void InitCustAddresses()
        {
            CustAddresses = _raw == null ? new List<AddressModel>() : Mapper.Map<List<Address>, List<AddressModel>>(_raw.Addresses.ToList());
            CustAddresses.Where(m => m.Id != 0 && m.Id == AddressId).Action(m => m.IsDefault = true);
        }

        #endregion

        #region InitCustGroups

        public void InitCustGroups(IDB db, bool mergeable = false)
        {
            if (mergeable)
            {
                foreach (var itm in CustGroups)
                {
                    var group = CustGroupMgr.GetDom(db, itm.GroupId ?? 0);
                    if (group == null) continue;
                    itm.GroupName = group.GetName();
                }
            }
            else CustGroups = _raw.GetCustGroups(db);
        }

        #endregion

        #region InitCustCategories

        public void InitCustCategories(IDB db, string categories = null, bool mergeable = false)
        {
            if (mergeable)
            {
                CustCategories = CustCategories ?? new List<CategoryCustomerModel>();

                foreach (var itm in CustCategories)
                {
                    var cate = CategoryMgr.GetDom(db, itm.CategoryId ?? 0);
                    if (cate == null) continue;
                    itm.CategoryName = cate.GetName();
                    itm.UpPath = cate.HierOrder(false);
                }
            }
            else
                CustCategories = _raw.CategoryCustomers.Select(m => new CategoryCustomerModel(m.Category)
                {
                    Id = m.Id,
                    CustomerId = m.Customer.Id,
                    DiscountRate = m.DiscountRate * 100m,
                    Notes = m.Notes
                }).ToList();

            if (!categories.IsNullOrEmpty())
            {
                var cates = JsonConvert.DeserializeAnonymousType(categories, new[] { new { Id = 0, Rate = (double?)0.0, Notes = "" } });
                if(cates.SafeCount() != 0)
                    foreach (var cate in cates)
                    {
                        var cateId = cate.Id;
                        var itm = CustCategories.FirstOrDefault(z => z.CategoryId == cateId);

                        if (itm != null) itm.DiscountRate = cate.Rate == 0 ? null : cate.Rate;
                        else CustCategories.Add(new CategoryCustomerModel(CategoryMgr.GetDom(db, cate.Id))
                        {
                            CustomerId = Id,
                            DiscountRate = cate.Rate == 0 ? null : cate.Rate,
                            Notes = cate.Notes
                        });
                    }
            }
        }

        #endregion

        #region Validate
        public static ValidationResult Validate(CustomerModel obj, ValidationContext cxt)
        {
            if (obj == null) return null;

            var httpCxt = HttpContext.Current;
            var controller = httpCxt == null ? null : httpCxt.Request.RequestContext.RouteData.Values["controller"];

            if (!obj.Email.IsNullOrEmpty() && (obj.Id != 0 || controller.InList("Customer")) && BaseMgr.Exists<Domain.Customer>(m => m.Id, obj.Id, null,
                new KeyValuePair() { Key = "Email", Value = obj.Email }))
            {
                var cust = obj.Id != 0 ? CustomerMgr.GetDom(new DefaultDb(), obj.Id) : null;
                if (cust?.Owner == null) return new ValidationResult(StringTable.ErrMsgUniqueConstraint, new[] { "Email" });
            }

            return null;
        }
        #endregion
    }
}