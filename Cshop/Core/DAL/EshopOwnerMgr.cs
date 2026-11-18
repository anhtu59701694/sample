using Claw.Core.DAL;
using Claw.Modules.Eshop.Domain;
using Claw.Modules.Eshop.Helper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Claw.Modules.Eshop.DAL
{
    public class EshopOwnerMgr : BaseMgr
    {
        public static void Insert(IDB db, Domain.EshopOwner objEshopOwner)
        {
//            DataService ds = new DataService(db.GetSession());
//            var sql = @"SET IDENTITY_INSERT eshop.EshopOwner ON; 
//            INSERT INTO eshop.EshopOwner(Id, CompanyName, CompanyOrgNumber, WebsiteURL, Address, Postnr, Posted, City, Country, ContactName, ContactEmail, ContactPhone, Information, CshopVersion, RegisteredDate, EffectiveDate, ExpiredDate) 
//            Values (:id, :CompanyName, :CompanyOrgNumber, :WebsiteURL, :Address, :Postnr, :Posted, :City, :Country, :ContactName, :ContactEmail, :ContactPhone, :Information, :CshopVersion, :RegisteredDate, :EffectiveDate, :ExpiredDate)
//            SET IDENTITY_INSERT eshop.EshopOwner OFF;";
//            ds.Session.CreateSQLQuery(sql)
//              .SetInt32("id", objEshopOwner.Id)
//              .SetString("CompanyName", objEshopOwner.CompanyName)
//              .SetString("CompanyOrgNumber", objEshopOwner.CompanyOrgNumber)
//              .SetString("WebsiteURL", objEshopOwner.WebsiteURL)
//              .SetString("Address", objEshopOwner.Address)
//              .SetString("Postnr", objEshopOwner.Postnr)
//              .SetString("Posted", objEshopOwner.Posted)
//              .SetString("City", objEshopOwner.City)
//              .SetString("Country", objEshopOwner.Country)
//              .SetString("ContactName", objEshopOwner.ContactName)
//              .SetString("ContactEmail", objEshopOwner.ContactEmail)
//              .SetString("ContactPhone", objEshopOwner.ContactPhone)
//              .SetString("Information", objEshopOwner.Information)
//              .SetInt32("CshopVersion", objEshopOwner.CshopVersion)
//              .SetDateTime("RegisteredDate", objEshopOwner.RegisteredDate)
//              .SetDateTime("EffectiveDate", objEshopOwner.EffectiveDate)
//              .SetDateTime("ExpiredDate", objEshopOwner.ExpiredDate)
//              .ExecuteUpdate();
        }
        public static List<Domain.EshopOwner> GetAllRaw(IDB db)
        {
            db = db ?? new DefaultDb();
            var hSql = string.Format(@"
            select eow from {0} as eow", typeof(Domain.EshopOwner).FullName);
            return GetList<Domain.EshopOwner>(db, hSql)
               .ToList();
        }
        public static List<Domain.EshopOwner> GetAll(IDB db)
        {
            return CacheHelper<EshopOwnerScope, List<Domain.EshopOwner>>.GetDataCache("Claw.Modules.Eshop.Domain.EshopOwner",
                () => GetAllRaw(db),24*60*60);
        }
    }
}